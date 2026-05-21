using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowBoard.Tests.Helpers;

internal sealed class StubCurrentUserService : ICurrentUserService
{
    public string UserId => "test-user-1";
    public string DisplayName => "Test User";
    public bool IsAuthenticated => true;
}

internal static class TestDbFactory
{
    public static FlowBoardDbContext Create()
    {
        var options = new DbContextOptionsBuilder<FlowBoardDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var db = new FlowBoardDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    public static async Task<SeedData> SeedAsync(FlowBoardDbContext db)
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test Workspace", OwnerUserId = "test-user-1" };
        var board = new Board { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, Name = "Test Board" };

        var todoColumn = new BoardColumn { Id = Guid.NewGuid(), BoardId = board.Id, Name = "To Do", SortOrder = 0 };
        var inProgressColumn = new BoardColumn { Id = Guid.NewGuid(), BoardId = board.Id, Name = "In Progress", SortOrder = 1 };
        var doneColumn = new BoardColumn { Id = Guid.NewGuid(), BoardId = board.Id, Name = "Done", SortOrder = 2 };

        db.Workspaces.Add(workspace);
        db.Boards.Add(board);
        db.BoardColumns.AddRange(todoColumn, inProgressColumn, doneColumn);
        await db.SaveChangesAsync();

        return new SeedData(board, todoColumn, inProgressColumn, doneColumn);
    }
}

internal record SeedData(Board Board, BoardColumn TodoColumn, BoardColumn InProgressColumn, BoardColumn DoneColumn);