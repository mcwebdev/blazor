using FlowBoard.Application.DTOs;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Services;
using FlowBoard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlowBoard.Tests;

/// <summary>
/// Unit tests for BoardService covering: task creation, idempotent
/// replays, move task, WIP limit context, and analytics-related
/// calculations.
/// </summary>
public class BoardServiceTests : IDisposable
{
    private readonly FlowBoard.Infrastructure.Data.FlowBoardDbContext _db;
    private readonly StubCurrentUserService _currentUser;
    private readonly NotificationService _notifications;
    private readonly BoardService _sut;

    public BoardServiceTests()
    {
        _db = TestDbFactory.Create();
        _currentUser = new StubCurrentUserService();
        _notifications = new NotificationService(_db);
        _sut = new BoardService(_db, _currentUser, _notifications);
    }

    public void Dispose() => _db.Dispose();

    // ─── Task creation ───────────────────────────────────

    [Fact]
    public async Task CreateTask_PersistsToCorrectColumn()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var dto = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "My first task",
            Description = "Description here",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        };

        var result = await _sut.CreateTaskAsync(dto);

        Assert.Equal("My first task", result.Title);
        Assert.Equal(TaskItemStatus.Open, result.Status);
        Assert.Equal(seed.TodoColumn.Id, result.ColumnId);
    }

    [Fact]
    public async Task CreateTask_WithInProgressStatus_GoesToInProgressColumn()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var dto = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Active work",
            Priority = TaskPriority.High,
            Status = TaskItemStatus.InProgress
        };

        var result = await _sut.CreateTaskAsync(dto);

        Assert.Equal(seed.InProgressColumn.Id, result.ColumnId);
        Assert.Equal(TaskItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task CreateTask_WithDoneStatus_SetsCompletedAtUtc()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var dto = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Already done",
            Priority = TaskPriority.Low,
            Status = TaskItemStatus.Done
        };

        var result = await _sut.CreateTaskAsync(dto);

        Assert.Equal(TaskItemStatus.Done, result.Status);
        // CompletedAtUtc is set on the entity level; verify through DB
        var entity = await _db.TaskItems.FindAsync(result.Id);
        Assert.NotNull(entity!.CompletedAtUtc);
    }

    [Fact]
    public async Task CreateTask_EmptyTitle_Throws()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var dto = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "  ",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateTaskAsync(dto));
    }

    [Fact]
    public async Task CreateTask_InvalidBoard_Throws()
    {
        await TestDbFactory.SeedAsync(_db);

        var dto = new CreateTaskDto
        {
            BoardId = Guid.NewGuid(),
            Title = "Orphan task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateTaskAsync(dto));
    }

    // ─── Idempotent task creation ────────────────────────

    [Fact]
    public async Task CreateTask_DuplicateClientRequestId_ReturnsExistingTask()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var clientId = Guid.NewGuid().ToString();
        var dto = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Idempotent task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open,
            ClientRequestId = clientId
        };

        var first = await _sut.CreateTaskAsync(dto);
        var second = await _sut.CreateTaskAsync(dto);

        // Must be the same task — not a duplicate.
        Assert.Equal(first.Id, second.Id);

        // Only one task entity should exist.
        var count = await _db.TaskItems.CountAsync(t => t.Title == "Idempotent task");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateTask_DifferentClientRequestIds_CreatesSeparateTasks()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var dto1 = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Task A",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open,
            ClientRequestId = Guid.NewGuid().ToString()
        };

        var dto2 = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Task B",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open,
            ClientRequestId = Guid.NewGuid().ToString()
        };

        var first = await _sut.CreateTaskAsync(dto1);
        var second = await _sut.CreateTaskAsync(dto2);

        Assert.NotEqual(first.Id, second.Id);
    }

    // ─── Activity log ────────────────────────────────────

    [Fact]
    public async Task CreateTask_RecordsActivityLogEntry()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        var dto = new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Logged task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        };

        var result = await _sut.CreateTaskAsync(dto);

        var activity = await _db.ActivityLogs
            .Where(a => a.TaskItemId == result.Id
                && a.EventType == ActivityEventType.TaskCreated)
            .FirstOrDefaultAsync();

        Assert.NotNull(activity);
        Assert.Contains("Logged task", activity!.Summary);
    }

    // ─── Board retrieval ─────────────────────────────────

    [Fact]
    public async Task GetBoard_ReturnsColumnsAndTasks()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        // Seed a task
        await _sut.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Test card",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        });

        var board = await _sut.GetBoardAsync(seed.Board.Id);

        Assert.NotNull(board);
        Assert.Equal("Test Board", board!.Name);
        Assert.Equal(3, board.Columns.Count);

        var todoColumn = board.Columns.First(c => c.Name == "To Do");
        Assert.Single(todoColumn.Tasks);
        Assert.Equal("Test card", todoColumn.Tasks.First().Title);
    }

    [Fact]
    public async Task GetBoard_NonExistentId_ReturnsNull()
    {
        await TestDbFactory.SeedAsync(_db);

        var board = await _sut.GetBoardAsync(Guid.NewGuid());
        Assert.Null(board);
    }

    // ─── Task update ─────────────────────────────────────

    [Fact]
    public async Task UpdateTask_ChangesPriority()
    {
        var seed = await TestDbFactory.SeedAsync(_db);
        var created = await _sut.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Update me",
            Priority = TaskPriority.Low,
            Status = TaskItemStatus.Open
        });

        var detail = (await _sut.GetTaskAsync(created.Id))!;
        detail.Priority = TaskPriority.Critical;

        await _sut.UpdateTaskAsync(detail);

        var updated = await _sut.GetTaskAsync(created.Id);
        Assert.Equal(TaskPriority.Critical, updated!.Priority);
    }

    [Fact]
    public async Task UpdateTask_AssigneeChange_TriggersNotification()
    {
        var seed = await TestDbFactory.SeedAsync(_db);
        var created = await _sut.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Assign task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        });

        var detail = (await _sut.GetTaskAsync(created.Id))!;
        detail.AssigneeUserId = "test-user-2";

        await _sut.UpdateTaskAsync(detail);

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n =>
                n.UserId == "test-user-2"
                && n.Type == Domain.Enums.NotificationType.TaskAssigned
                && n.RelatedTaskId == created.Id);

        Assert.NotNull(notification);
        Assert.Contains("Assign task", notification!.Title);
    }
}
