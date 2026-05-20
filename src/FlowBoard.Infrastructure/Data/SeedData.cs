using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowBoard.Infrastructure.Data;

public static class SeedData
{
    // Fixed IDs so services can reference seed data deterministically.
    public static readonly string DemoUserId = "demo-user-00000000-0000-0000-0000-000000000001";
    public static readonly string SarahUserId = "demo-user-00000000-0000-0000-0000-000000000002";
    public static readonly Guid WorkspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid BoardId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        // Skip seeding if data already exists.
        if (await db.Workspaces.AnyAsync())
            return;

        // ── Users ──────────────────────────────────────────────────
        var demo = new ApplicationUser
        {
            Id = DemoUserId,
            UserName = "demo@flowboard.app",
            Email = "demo@flowboard.app",
            DisplayName = "Matt Charlton",
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var sarah = new ApplicationUser
        {
            Id = SarahUserId,
            UserName = "sarah@flowboard.app",
            Email = "sarah@flowboard.app",
            DisplayName = "Sarah Patel",
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (await userManager.FindByIdAsync(DemoUserId) == null)
        {
            await userManager.CreateAsync(demo, "Demo1234!");
        }
        
        if (await userManager.FindByIdAsync(SarahUserId) == null)
        {
            await userManager.CreateAsync(sarah, "Demo1234!");
        }

        // ── Workspace ──────────────────────────────────────────────
        var workspace = new Workspace
        {
            Id = WorkspaceId,
            Name = "Product Launch",
            OwnerUserId = DemoUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Workspaces.Add(workspace);

        db.WorkspaceMembers.AddRange(
            new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = WorkspaceId,
                UserId = DemoUserId,
                Role = WorkspaceRole.Owner,
                JoinedAtUtc = DateTime.UtcNow
            },
            new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = WorkspaceId,
                UserId = SarahUserId,
                Role = WorkspaceRole.Member,
                JoinedAtUtc = DateTime.UtcNow
            });

        // ── Labels ─────────────────────────────────────────────────
        var labelFrontend = new TaskLabel { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "Frontend", Color = "#276ef1" };
        var labelBackend = new TaskLabel { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "Backend", Color = "#7c3aed" };
        var labelDevOps = new TaskLabel { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "DevOps", Color = "#4fb286" };
        var labelUrgent = new TaskLabel { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "Urgent", Color = "#dc2626" };
        db.TaskLabels.AddRange(labelFrontend, labelBackend, labelDevOps, labelUrgent);

        // ── Board ──────────────────────────────────────────────────
        var board = new Board
        {
            Id = BoardId,
            WorkspaceId = WorkspaceId,
            Name = "Product Launch Board",
            Description = "Track all launch deliverables from backlog to done.",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Boards.Add(board);

        // ── Columns ────────────────────────────────────────────────
        var colBacklog = new BoardColumn { Id = Guid.NewGuid(), BoardId = BoardId, Name = "Backlog", SortOrder = 0, WipLimit = null };
        var colInProgress = new BoardColumn { Id = Guid.NewGuid(), BoardId = BoardId, Name = "In Progress", SortOrder = 1, WipLimit = 4 };
        var colReview = new BoardColumn { Id = Guid.NewGuid(), BoardId = BoardId, Name = "Review", SortOrder = 2, WipLimit = 3 };
        var colDone = new BoardColumn { Id = Guid.NewGuid(), BoardId = BoardId, Name = "Done", SortOrder = 3, WipLimit = null };
        db.BoardColumns.AddRange(colBacklog, colInProgress, colReview, colDone);

        // ── Tasks ──────────────────────────────────────────────────
        var now = DateTime.UtcNow;

        db.TaskItems.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = BoardId,
                ColumnId = colBacklog.Id,
                Title = "Draft onboarding copy",
                Description = "Write the onboarding email sequence and in-app tooltips.",
                Priority = TaskPriority.Medium,
                Status = TaskItemStatus.Open,
                AssigneeUserId = SarahUserId,
                ReporterUserId = DemoUserId,
                DueDateUtc = now.AddDays(5),
                SortOrder = 0,
                CreatedAtUtc = now.AddHours(-8),
                RowVersion = Guid.NewGuid().ToByteArray()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = BoardId,
                ColumnId = colBacklog.Id,
                Title = "Map launch dependencies",
                Description = "Identify all cross-team dependencies for the public launch.",
                Priority = TaskPriority.High,
                Status = TaskItemStatus.Open,
                AssigneeUserId = DemoUserId,
                ReporterUserId = DemoUserId,
                DueDateUtc = now.AddDays(2),
                SortOrder = 1,
                CreatedAtUtc = now.AddHours(-6),
                RowVersion = Guid.NewGuid().ToByteArray()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = BoardId,
                ColumnId = colInProgress.Id,
                Title = "Wire board replay shell",
                Description = "Build the replay timeline panel scaffold and connect to the activity log.",
                Priority = TaskPriority.High,
                Status = TaskItemStatus.InProgress,
                AssigneeUserId = DemoUserId,
                ReporterUserId = DemoUserId,
                DueDateUtc = now.AddDays(3),
                SortOrder = 0,
                CreatedAtUtc = now.AddHours(-12),
                RowVersion = Guid.NewGuid().ToByteArray()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = BoardId,
                ColumnId = colInProgress.Id,
                Title = "Prepare Cloud Run deploy",
                Description = "Finalize Dockerfile, CI pipeline, and deployment scripts.",
                Priority = TaskPriority.Medium,
                Status = TaskItemStatus.InProgress,
                AssigneeUserId = null, // unassigned
                ReporterUserId = DemoUserId,
                DueDateUtc = now.AddDays(1),
                SortOrder = 1,
                CreatedAtUtc = now.AddHours(-4),
                RowVersion = Guid.NewGuid().ToByteArray()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = BoardId,
                ColumnId = colReview.Id,
                Title = "Validate analytics drill-down",
                Description = "Click-through testing for all analytics metric cards.",
                Priority = TaskPriority.Low,
                Status = TaskItemStatus.InReview,
                AssigneeUserId = SarahUserId,
                ReporterUserId = DemoUserId,
                DueDateUtc = now.AddDays(-1), // overdue
                SortOrder = 0,
                CreatedAtUtc = now.AddDays(-3),
                RowVersion = Guid.NewGuid().ToByteArray()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = BoardId,
                ColumnId = colDone.Id,
                Title = "Provision Firebase Hosting",
                Description = "Create Firebase project, hosting site, and rewrite rules.",
                Priority = TaskPriority.Low,
                Status = TaskItemStatus.Done,
                AssigneeUserId = DemoUserId,
                ReporterUserId = DemoUserId,
                CompletedAtUtc = now.AddHours(-2),
                SortOrder = 0,
                CreatedAtUtc = now.AddDays(-5),
                RowVersion = Guid.NewGuid().ToByteArray()
            }
        );

        // ── Activity Log ───────────────────────────────────────────
        db.ActivityLogs.AddRange(
            new ActivityLog
            {
                Id = Guid.NewGuid(),
                WorkspaceId = WorkspaceId,
                BoardId = BoardId,
                ActorUserId = SarahUserId,
                SequenceNumber = 1,
                EventType = ActivityEventType.TaskMoved,
                EventCategory = ActivityEventCategory.Task,
                Summary = "Sarah moved API contract to Review.",
                CreatedAtUtc = now.AddMinutes(-42)
            },
            new ActivityLog
            {
                Id = Guid.NewGuid(),
                WorkspaceId = WorkspaceId,
                BoardId = BoardId,
                ActorUserId = DemoUserId,
                SequenceNumber = 2,
                EventType = ActivityEventType.TaskAssigned,
                EventCategory = ActivityEventCategory.Task,
                Summary = "Matt assigned Launch checklist to Maya.",
                CreatedAtUtc = now.AddMinutes(-51)
            },
            new ActivityLog
            {
                Id = Guid.NewGuid(),
                WorkspaceId = WorkspaceId,
                BoardId = BoardId,
                ActorUserId = SarahUserId,
                SequenceNumber = 3,
                EventType = ActivityEventType.TaskUpdated,
                EventCategory = ActivityEventCategory.Task,
                Summary = "Jordan added analytics acceptance criteria.",
                CreatedAtUtc = now.AddMinutes(-66)
            }
        );

        await db.SaveChangesAsync();
    }
}
