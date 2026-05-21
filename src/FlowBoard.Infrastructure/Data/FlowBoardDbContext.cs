using FlowBoard.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Data;

public class FlowBoardDbContext : IdentityDbContext<ApplicationUser>
{
    public FlowBoardDbContext(DbContextOptions<FlowBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<TaskItemLabel> TaskItemLabels => Set<TaskItemLabel>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<BoardSnapshot> BoardSnapshots => Set<BoardSnapshot>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FailedCommand> FailedCommands => Set<FailedCommand>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── TaskItemLabel (many-to-many join) ──────────────────────
        builder.Entity<TaskItemLabel>()
            .HasKey(til => new { til.TaskItemId, til.TaskLabelId });

        builder.Entity<TaskItemLabel>()
            .HasOne(til => til.TaskItem)
            .WithMany(t => t.TaskLabels)
            .HasForeignKey(til => til.TaskItemId);

        builder.Entity<TaskItemLabel>()
            .HasOne(til => til.TaskLabel)
            .WithMany(l => l.TaskItems)
            .HasForeignKey(til => til.TaskLabelId);

        // ── TaskItem ───────────────────────────────────────────────
        builder.Entity<TaskItem>(entity =>
        {
            entity.HasOne(t => t.Board)
                .WithMany(b => b.Tasks)
                .HasForeignKey(t => t.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Column)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.ColumnId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(t => t.Version)
                .HasColumnName("RowVersion")
                .HasConversion(
                    version => version.ToByteArray(),
                    value => new Guid(value))
                .IsConcurrencyToken();

            entity.HasIndex(t => new { t.BoardId, t.ColumnId, t.SortOrder });
        });

        // ── BoardColumn ────────────────────────────────────────────
        builder.Entity<BoardColumn>()
            .HasOne(c => c.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BoardColumn>()
            .HasIndex(c => new { c.BoardId, c.SortOrder });

        // ── Board ──────────────────────────────────────────────────
        builder.Entity<Board>()
            .HasOne(b => b.Workspace)
            .WithMany(w => w.Boards)
            .HasForeignKey(b => b.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── WorkspaceMember ────────────────────────────────────────
        builder.Entity<WorkspaceMember>()
            .HasOne(wm => wm.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(wm => wm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkspaceMember>()
            .HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
            .IsUnique();

        // ── TaskDependency ─────────────────────────────────────────
        builder.Entity<TaskDependency>(entity =>
        {
            entity.HasOne(d => d.BlockingTask)
                .WithMany()
                .HasForeignKey(d => d.BlockingTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.BlockedTask)
                .WithMany()
                .HasForeignKey(d => d.BlockedTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(d => new { d.BlockingTaskId, d.BlockedTaskId })
                .IsUnique();
        });

        // ── ActivityLog ────────────────────────────────────────────
        builder.Entity<ActivityLog>(entity =>
        {
            entity.HasIndex(a => new { a.BoardId, a.SequenceNumber });
            entity.HasIndex(a => a.IdempotencyKey);
        });

        // ── TaskLabel ──────────────────────────────────────────────
        builder.Entity<TaskLabel>()
            .HasOne(l => l.Workspace)
            .WithMany(w => w.Labels)
            .HasForeignKey(l => l.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FeatureFlag ────────────────────────────────────────────
        builder.Entity<FeatureFlag>()
            .HasIndex(f => new { f.WorkspaceId, f.Key })
            .IsUnique();
    }
}
