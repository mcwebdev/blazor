using FlowBoard.Application.DTOs;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Services;
using FlowBoard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlowBoard.Tests;

/// <summary>
/// Tests for AnalyticsService covering: summary computation, burndown
/// projection, cycle time, workload, and drill-down filtering.
/// </summary>
public class AnalyticsServiceTests : IDisposable
{
    private readonly FlowBoard.Infrastructure.Data.FlowBoardDbContext _db;
    private readonly StubCurrentUserService _currentUser;
    private readonly NotificationService _notifications;
    private readonly BoardService _boardService;
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        _db = TestDbFactory.Create();
        _currentUser = new StubCurrentUserService();
        _notifications = new NotificationService(_db);
        _boardService = new BoardService(_db, _currentUser, _notifications);
        _sut = new AnalyticsService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetBoardAnalytics_TotalTasks_MatchesCreatedCount()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Task 1",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        });

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Task 2",
            Priority = TaskPriority.High,
            Status = TaskItemStatus.InProgress
        });

        var summary = await _sut.GetBoardAnalyticsAsync(seed.Board.Id);

        Assert.NotNull(summary);
        Assert.Equal(2, summary!.TotalTasks);
    }

    [Fact]
    public async Task GetBoardAnalytics_StatusMix_ReflectsTaskStatuses()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Todo 1",
            Priority = TaskPriority.Low,
            Status = TaskItemStatus.Open
        });

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Done 1",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Done
        });

        var summary = await _sut.GetBoardAnalyticsAsync(seed.Board.Id);

        Assert.NotNull(summary);
        Assert.NotEmpty(summary!.StatusMix);
        Assert.Contains(summary.StatusMix,
            s => s.Status == TaskItemStatus.Open && s.Count == 1);
        Assert.Contains(summary.StatusMix,
            s => s.Status == TaskItemStatus.Done && s.Count == 1);
    }

    [Fact]
    public async Task GetBoardAnalytics_OverdueCount_Correct()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        // Create one overdue + one not overdue
        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Overdue",
            Priority = TaskPriority.High,
            Status = TaskItemStatus.Open,
            DueDateUtc = DateTime.UtcNow.AddDays(-2)
        });

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Future",
            Priority = TaskPriority.Low,
            Status = TaskItemStatus.Open,
            DueDateUtc = DateTime.UtcNow.AddDays(5)
        });

        var summary = await _sut.GetBoardAnalyticsAsync(seed.Board.Id);

        Assert.NotNull(summary);
        Assert.Equal(1, summary!.OverdueCount);
    }

    [Fact]
    public async Task GetBoardAnalytics_Burndown_Has14Points()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Any task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open
        });

        var summary = await _sut.GetBoardAnalyticsAsync(seed.Board.Id);

        Assert.NotNull(summary);
        Assert.Equal(14, summary!.Burndown.Count);
    }

    [Fact]
    public async Task GetBoardDrilldown_OverdueFilter_ReturnsOnlyOverdue()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "Past due",
            Priority = TaskPriority.High,
            Status = TaskItemStatus.Open,
            DueDateUtc = DateTime.UtcNow.AddDays(-1)
        });

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "On time",
            Priority = TaskPriority.Low,
            Status = TaskItemStatus.Open,
            DueDateUtc = DateTime.UtcNow.AddDays(10)
        });

        var filter = new TaskFilterDto();
        var result = await _sut.GetBoardDrilldownAsync(
            seed.Board.Id, AnalyticsMetric.Overdue, filter);

        Assert.NotNull(result);
        Assert.Single(result!.Tasks);
        Assert.Equal("Past due", result.Tasks[0].Title);
    }

    [Fact]
    public async Task GetBoardAnalytics_Workload_GroupsByAssignee()
    {
        var seed = await TestDbFactory.SeedAsync(_db);

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "User1 task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open,
            AssigneeUserId = "test-user-1"
        });

        await _boardService.CreateTaskAsync(new CreateTaskDto
        {
            BoardId = seed.Board.Id,
            Title = "User2 task",
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Open,
            AssigneeUserId = "test-user-2"
        });

        var summary = await _sut.GetBoardAnalyticsAsync(seed.Board.Id);

        Assert.NotNull(summary);
        Assert.True(summary!.Workload.Count >= 2);
    }

    [Fact]
    public async Task GetBoardAnalytics_NonExistentBoard_ReturnsNull()
    {
        await TestDbFactory.SeedAsync(_db);
        var result = await _sut.GetBoardAnalyticsAsync(Guid.NewGuid());
        Assert.Null(result);
    }
}
