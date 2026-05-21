# Smart Task Manager — Full-Stack Blazor Technical Build Spec

## Author: OpenAI Codex

## 1. Project Summary

Build a full-stack Blazor application that combines project/task management, real-time collaboration, analytics, replayable activity history, command-driven navigation, and polished UX. The app should feel like a lightweight Trello + Jira + operational dashboard experience.

**Working name:** FlowBoard

**One-liner:** A real-time collaborative task manager where teams create boards, manage tasks, replay board activity, track workload, and drill into productivity analytics.

**Technical goal:** Build an end-to-end Blazor application with component architecture, authentication, CRUD, state management, SignalR real-time updates, EF Core persistence, validation, authorization, reusable UI components, analytics, and testing.

**Signature capability:** A live board can be replayed from its activity timeline, showing task cards move, update, and change ownership over time while connected users see presence, editing indicators, and resilient connection state.

---

## 2. Tech Stack

### Frontend

- Blazor Web App
- Static SSR for public, login-adjacent, and low-interactivity pages
- Interactive Server for the real-time board, task drawer, notifications, and presence
- Optional Interactive Auto or Interactive WebAssembly for analytics-heavy and offline-friendly screens
- Razor components
- QuickGrid for admin and audit data views
- CSS isolation or Tailwind/Bootstrap
- JavaScript interop only where it adds value, such as drag-and-drop, charts, keyboard shortcuts, local storage, or animation timing

### Backend

- ASP.NET Core
- Minimal APIs or controllers
- SignalR hubs for real-time board updates
- EF Core
- PostgreSQL or SQL Server for the primary database
- SQLite only for local test fixtures or lightweight development
- Optional background job runner for overdue reminders, scheduled summaries, and activity snapshot generation

### Auth

- ASP.NET Core Identity
- Role-based authorization
- Optional OAuth provider for a production-style sign-in path

### Testing

- bUnit for Blazor component tests
- xUnit/NUnit for service tests
- EF Core SQLite/in-memory test database
- Playwright for critical user flows and real-time multi-browser checks

### Deployment

- Dockerfile and Docker Compose
- Firebase Hosting as the public entry point, CDN, preview-channel host, and custom-domain layer
- Cloud Run for the ASP.NET Core Blazor container
- Firebase Hosting rewrite from `/**` to the Cloud Run service
- Cloud SQL for PostgreSQL as the production database
- Secret Manager for connection strings and auth secrets
- Cloud Run `max-instances=1` for the simple demo deployment
- Memorystore Redis or another SignalR backplane when scaling beyond one Cloud Run instance
- Structured logging and OpenTelemetry traces for key commands

---

## 3. Core User Personas

### Individual User

Wants to organize personal projects and track progress.

### Team Member

Works inside shared boards, updates tasks, comments, and receives live updates.

### Team Lead / Admin

Manages members, views analytics, monitors overdue work, and tracks workload.

---

## 4. Core Features

## 4.1 Authentication and Authorization

### Requirements

- Users can register, sign in, sign out.
- Users belong to one or more workspaces.
- Workspace owners can invite/remove members.
- Roles:
  - Owner
  - Admin
  - Member
  - Viewer

### Implementation Focus

- Identity integration
- Authenticated pages
- Role-based UI rendering
- API authorization checks

---

## 4.2 Dashboard Home

### Requirements

After login, users land on a personal dashboard showing:

- My assigned tasks
- Overdue tasks
- Upcoming due dates
- Recently updated tasks
- Workspace switcher
- Quick-create task button
- Small analytics summary

The shell-level `New task` action should work from the dashboard when a recent or selected board is available. Dashboard-created tasks should open the shared task drawer in create mode, target the active board, refresh dashboard metrics after save, and broadcast the same board activity event used by the board route.

### Nice UI Touches

- Skeleton loading states
- Empty states
- Toast notifications
- Responsive cards

### Implementation Focus

- Data aggregation
- Conditional rendering
- Responsive design
- Async loading states

---

## 4.3 Workspace and Board Management

### Requirements

Users can:

- Create workspace
- Create boards inside workspace
- Rename/archive boards
- Add columns/lists to boards
- Reorder columns

Example board columns:

- Backlog
- Ready
- In Progress
- Review
- Done

### Implementation Focus

- Parent/child data modeling
- Nested routing
- Permissions
- Complex UI composition

---

## 4.4 Kanban Task Board

### Requirements

Users can:

- Create task cards
- Edit title, description, priority, status, assignee, due date
- Drag cards between columns
- Reorder cards within a column
- Filter by assignee, tag, priority, due date, status
- Search tasks

### Optional Advanced Behavior

- Optimistic UI update when dragging cards
- Rollback if server update fails
- Task count badges by column
- Color-coded priorities

### Implementation Focus

- Advanced component interaction
- State management
- Drag-and-drop
- Optimistic updates
- Validation

---

## 4.5 Task Detail Drawer / Modal

### Requirements

Clicking a card opens a detail panel with:

- Editable fields
- Assignee selector
- Due-date editor with date-only display semantics
- Labels/tags
- Checklist items
- Comments
- Activity history
- Attachments placeholder or simple file metadata

The app-shell `New task` action should open the same drawer in create mode when the current route has an active board context. The dashboard and board routes are valid task-creation contexts. Routes with no board context should disable the action instead of leaving a no-op button. Task creation must validate title, select the correct board column from status, persist assignee and due date, record activity, and refresh connected board views.

### Implementation Focus

- Complex forms
- EditContext validation
- Nested components
- Child-to-parent event callbacks
- UX polish

---

## 4.6 Real-Time Collaboration

### Requirements

Using SignalR:

- When a user creates, edits, moves, or deletes a task, other users on the same board see the update live.
- Show presence indicators: “Sarah is viewing this board.”
- Show subtle activity notifications: “Matt moved API Integration to Review.”
- Notify users when they are assigned to a task.

### Hub Events

Client receives:

- TaskCreated
- TaskUpdated
- TaskMoved
- TaskDeleted
- CommentAdded
- UserJoinedBoard
- UserLeftBoard
- PresenceChanged
- UserStartedEditing
- UserStoppedEditing
- BoardActivityRecorded
- PendingCommandAccepted
- PendingCommandRejected

Server receives:

- JoinBoard(boardId)
- LeaveBoard(boardId)
- MoveTask(command)
- SendComment(command)
- StartEditing(taskId, fieldName)
- StopEditing(taskId, fieldName)
- AcknowledgeActivity(sequenceNumber)

### Implementation Focus

- Real-time architecture
- User-specific events
- Group-based SignalR messaging
- Concurrency awareness

---

## 4.7 Analytics Page

### Requirements

Create a dashboard with charts and metrics:

- Tasks completed this week
- Tasks by status
- Tasks by priority
- Average cycle time
- Overdue task count
- Workload by assignee
- Burndown-style chart for demo data

### Implementation Focus

- Aggregate queries
- Charts
- Server-side projection DTOs
- Performance-conscious loading

---

## 4.8 Notifications

### Requirements

- In-app notification bell
- Assignment notifications
- Comment mention notifications
- Overdue task reminders
- Mark as read/unread

### Implementation Focus

- Event-driven thinking
- Background-job-ready architecture
- User-specific data

---

## 4.9 Search and Filtering

### Requirements

- Search by task title/description
- Filter by:
  - Assignee
  - Priority
  - Tag
  - Due date
  - Status
- Save one custom filter per user as “My View”

### Implementation Focus

- Query composition
- Debounced input
- URL query string state
- Reusable filter components

---

## 4.10 Audit Trail / Activity Log

### Requirements

Record key events:

- Task created
- Task moved
- Assignee changed
- Due date changed
- Priority changed
- Comment added

Display activity on:

- Task detail view
- Board activity feed

### Implementation Focus

- Domain events
- Historical data modeling
- Domain-event architecture

---

## 4.11 Board Replay / Live Activity Timeline

### Requirements

Users can open a timeline panel from a board and replay recent activity:

- Replay the last 5 minutes, 30 minutes, day, or a custom range.
- Show task cards moving between columns based on recorded activity.
- Animate task title, priority, assignee, due date, and completion changes.
- Show markers for comments, assignments, WIP limit warnings, and task deletes.
- Provide play, pause, step backward, step forward, speed, and reset controls.
- Filter replay by actor, task, event type, priority, or assignee.
- Open an event inspector showing before/after values for the selected timeline event.
- Allow the user to exit replay mode and return to the current live board state.

### Implementation Focus

- Event-sourced projection from `ActivityLog`
- Deterministic replay state
- Animation-safe board updates
- Separation between live state and replay state
- Timeline virtualization for long histories

### Current MVP Implementation

- `BoardReplayPanel.razor` loads replay events from `IBoardService.GetReplayEventsAsync`.
- Timeline controls support 5 minute, 30 minute, 1 day, and all-history ranges.
- Timeline filtering supports text, actor, and event type.
- The event inspector shows raw before/after JSON plus readable change chips for common task fields.
- A compact projection preview highlights source and target columns for move-style events.
- Further replay polish should animate the full live board surface, not only the panel projection.

---

## 4.12 Command Palette

### Requirements

Pressing `Ctrl+K` or `Cmd+K` opens a command palette with keyboard navigation.

Commands include:

- Create task
- Jump to workspace
- Jump to board
- Search tasks
- Assign task to me
- Open my overdue tasks
- Open board analytics
- Start board replay
- Toggle theme
- Invite member

### Implementation Focus

- Reusable modal command surface
- Fuzzy search over commands, boards, tasks, and members
- Keyboard-first interaction
- URL-aware navigation
- Authorization-aware command visibility

### Current MVP Implementation

- `CommandPaletteHost.razor` is an isolated interactive top-bar component.
- `Ctrl+K` / `Cmd+K` opens the palette through `wwwroot/js/commandPalette.js`.
- Commands currently include create task, dashboard, board, board replay, analytics, and health.
- `AppActionDispatcher` controls route-aware availability for create-task and board-replay commands.
- Future expansion should add fuzzy task search, member actions, invite flows, and authorization-trimmed result sets.

---

## 4.13 Collaborative Editing and Conflict Resolution

### Requirements

When multiple users are working on the same board:

- Show presence avatars for users currently viewing the board.
- Show field-level editing indicators, such as "Sarah is editing the due date."
- Detect stale updates using a Guid `Version` concurrency token mapped to the `RowVersion` database column.
- If a conflict occurs, show a conflict resolution dialog with:
  - Current saved value
  - User's attempted value
  - Last editor
  - Last updated timestamp
  - Keep mine / Use latest / Merge manually actions
- Broadcast task edits, comments, assignments, and status changes in real time.

### Implementation Focus

- Optimistic concurrency
- Presence state separate from persisted data
- Clear conflict UX
- SignalR group events
- Safe retry behavior

---

## 4.14 Drill-Down Analytics

### Requirements

Analytics should be interactive, not just decorative:

- Clicking a metric card opens the exact filtered task list behind that number.
- Clicking a chart segment applies a board filter.
- Workload chart opens tasks grouped by assignee.
- Cycle time chart opens completed tasks used in the calculation.
- Overdue count opens a saved filter for overdue tasks.
- Analytics filters sync to the URL query string.
- Users can export analytics detail rows to CSV.

### Implementation Focus

- Server-side aggregate queries
- Filter DTO composition
- Click-through chart interactions
- Query-string state
- Reusable drill-down drawer

---

## 4.15 Connection Resilience and Offline Drafts

### Requirements

The app should make connection state visible and recover gracefully:

- Show a compact connection badge: connected, reconnecting, offline, or sync pending.
- Show app-shell status chips in the sidebar for environment/runtime signals such as Cloud Run, Firebase Hosting, and database readiness.
- Treat sidebar status chips as operational indicators, not as a replacement for board-level SignalR connection state.
- Retry SignalR connection with exponential backoff.
- Save unsent task and comment drafts in local storage.
- Queue pending client actions with idempotency keys.
- Replay pending actions after reconnect.
- Show a sync result toast after pending actions complete.
- Roll back or mark failed actions if the server rejects them.

### Implementation Focus

- SignalR lifecycle handling
- Runtime health projection for shell-level status chips
- Idempotent command processing
- Local storage interop
- Pending action queue
- Reconciliation between optimistic UI and server state

---

## 4.16 Admin and Audit Operations

### Requirements

Workspace owners and admins get an operations view:

- QuickGrid-based audit log with server-side paging, sorting, and filtering.
- Member and role matrix.
- Feature flag toggles for analytics, notifications, replay, and experimental UI.
- WIP limit breach report.
- Recent failed command log.
- Export audit log to CSV.

### Implementation Focus

- Data-heavy Blazor UI
- QuickGrid usage
- Authorization policy boundaries
- Server-side query projection
- Operational observability

---

## 5. Advanced Feature Set

Build these after the core board and signature collaboration features are stable.

### 5.1 AI-Like Prioritization Without External AI

Add a rules-based “Focus Suggestions” panel:

- High priority + due soon = urgent
- Blocked tasks appear first
- Tasks assigned to user and overdue appear first
- Warn if a user has too many active tasks

### 5.2 Dependency and Blocker Mapping

- Link tasks as blocked by / blocking
- Show dependency badges on cards
- Warn when a blocked task is moved to Done
- Add a dependency map view for a board

### 5.3 Export

- Export board report to CSV
- Export analytics summary to PDF
- Export replay event timeline to JSON

### 5.4 Feature Flags

- Enable/disable analytics, notifications, or experimental UI

### 5.5 Admin Settings

- Workspace-level task statuses
- Custom priority names
- Custom labels

### 5.6 Theme and Layout Personalization

- Per-user compact/comfortable density setting
- Light/dark/system theme
- Saved sidebar layout
- Saved board filters beyond the default “My View”

---

## 6. Suggested App Pages

| Route | Page | Purpose |
|---|---|---|
| `/` | Landing / redirect | Public intro or auth redirect |
| `/login` | Login | Sign in |
| `/register` | Register | Create account |
| `/dashboard` | User dashboard | Personal overview |
| `/workspaces/{workspaceId}` | Workspace overview | Boards and members |
| `/boards/{boardId}` | Kanban board | Main app experience |
| `/boards/{boardId}/timeline` | Activity timeline | Replayable board history |
| `/boards/{boardId}/analytics` | Analytics | Board metrics |
| `/boards/{boardId}/analytics/tasks` | Analytics drill-down | Filtered task evidence behind metrics |
| `/notifications` | Notifications | User alerts |
| `/settings/workspace/{workspaceId}` | Workspace settings | Members, roles, config |
| `/settings/workspace/{workspaceId}/audit` | Audit operations | Activity, command, and permission history |
| `/settings/workspace/{workspaceId}/features` | Feature flags | Workspace-level capability toggles |

---

## 7. Component Breakdown

### Layout Components

- `AppShell`
- `SidebarNav`
- `TopBar`
- `WorkspaceSwitcher`
- `SidebarStatusChips`
- `NotificationBell`
- `ThemeToggle`

### Board Components

- `BoardPage`
- `BoardColumn`
- `TaskCard`
- `TaskDetailDrawer`
- `TaskEditorForm`
- `TaskFilterBar`
- `AssigneeAvatar`
- `PriorityBadge`
- `LabelChip`
- `PresenceAvatarStack`
- `EditingIndicator`
- `ConnectionStatusBadge`
- `ConflictResolutionDialog`
- `PendingActionToast`

### Replay Components

- `BoardTimelinePanel`
- `TimelineScrubber`
- `TimelineEventMarker`
- `ReplayControls`
- `ReplayEventInspector`
- `ReplayModeBanner`

### Analytics Components

- `MetricCard`
- `StatusChart`
- `PriorityChart`
- `WorkloadChart`
- `CycleTimeChart`
- `AnalyticsDrilldownDrawer`
- `AnalyticsFilterChips`
- `AnalyticsExportButton`

### Admin Components

- `AuditLogGrid`
- `MemberRoleMatrix`
- `FeatureFlagToggle`
- `WipLimitReport`
- `FailedCommandGrid`

### Shared Components

- `ConfirmDialog`
- `EmptyState`
- `LoadingSkeleton`
- `ToastHost`
- `ValidationSummaryCard`
- `SearchBox`
- `CommandPalette`
- `KeyboardShortcutProvider`
- `VirtualizedList`
- `ThemeDensityToggle`

---

## 8. Data Model

### User

Use ASP.NET Core Identity user as the base.

Additional fields:

- DisplayName
- AvatarUrl
- CreatedAtUtc

### Workspace

- Id
- Name
- OwnerUserId
- CreatedAtUtc
- IsArchived

### WorkspaceMember

- Id
- WorkspaceId
- UserId
- Role
- JoinedAtUtc

### Board

- Id
- WorkspaceId
- Name
- Description
- CreatedAtUtc
- IsArchived

### BoardColumn

- Id
- BoardId
- Name
- SortOrder
- WipLimit nullable

### TaskItem

- Id
- BoardId
- ColumnId
- Title
- Description
- Priority
- Status
- AssigneeUserId nullable
- ReporterUserId
- DueDateUtc nullable
- SortOrder
- CreatedAtUtc
- UpdatedAtUtc
- LastModifiedByUserId nullable
- CompletedAtUtc nullable
- Version Guid concurrency token, mapped to the existing RowVersion database column

### TaskComment

- Id
- TaskItemId
- UserId
- Body
- CreatedAtUtc
- UpdatedAtUtc nullable

### TaskLabel

- Id
- WorkspaceId
- Name
- Color

### TaskItemLabel

- TaskItemId
- TaskLabelId

### ChecklistItem

- Id
- TaskItemId
- Text
- IsComplete
- SortOrder

### ActivityLog

- Id
- WorkspaceId
- BoardId nullable
- TaskItemId nullable
- ActorUserId
- SequenceNumber
- EventType
- EventCategory
- EntityType
- EntityId nullable
- Summary
- BeforeJson nullable
- AfterJson nullable
- MetadataJson
- CorrelationId
- IdempotencyKey nullable
- CreatedAtUtc

### BoardSnapshot

- Id
- BoardId
- ActivityLogSequenceNumber
- SnapshotJson
- CreatedAtUtc

### SavedView

- Id
- UserId
- WorkspaceId
- BoardId nullable
- Name
- FilterJson
- IsDefault
- CreatedAtUtc
- UpdatedAtUtc

### FeatureFlag

- Id
- WorkspaceId
- Key
- IsEnabled
- UpdatedByUserId
- UpdatedAtUtc

### FailedCommand

- Id
- WorkspaceId
- UserId
- CommandType
- PayloadJson
- ErrorSummary
- CorrelationId
- CreatedAtUtc

### TaskDependency

- Id
- BoardId
- BlockingTaskId
- BlockedTaskId
- CreatedByUserId
- CreatedAtUtc

### UserPreference

- Id
- UserId
- Theme
- Density
- SidebarCollapsed
- LastWorkspaceId nullable
- UpdatedAtUtc

### Notification

- Id
- UserId
- Type
- Title
- Body
- RelatedTaskId nullable
- IsRead
- CreatedAtUtc

### Ephemeral Presence State

Presence does not need to be persisted permanently. Track active board sessions in memory or a distributed cache when scaling beyond one server.

- ConnectionId
- UserId
- BoardId
- DisplayName
- AvatarUrl nullable
- EditingTaskId nullable
- EditingField nullable
- LastSeenUtc

---

## 9. API / Service Layer

### Recommended Pattern

Use a clean service layer between Blazor components and EF Core.

Example services:

- `IWorkspaceService`
- `IBoardService`
- `ITaskService`
- `ICommentService`
- `IAnalyticsService`
- `INotificationService`
- `IActivityLogService`
- `ICurrentUserService`
- `IBoardReplayService`
- `IPresenceService`
- `ICommandPaletteService`
- `IConflictResolutionService`
- `IAuditQueryService`
- `IFeatureFlagService`
- `ISavedViewService`

### Example Commands

```csharp
public sealed record CreateTaskCommand(
    Guid BoardId,
    Guid ColumnId,
    string Title,
    string? Description,
    string Priority,
    string? AssigneeUserId,
    DateTime? DueDateUtc,
    Guid ClientRequestId);

public sealed record MoveTaskCommand(
    Guid TaskId,
    Guid TargetColumnId,
    int NewSortOrder,
    Guid Version,
    Guid ClientRequestId);

public sealed record ResolveTaskConflictCommand(
    Guid TaskId,
    Guid ExpectedVersion,
    string ResolutionMode,
    string MergedFieldsJson,
    Guid ClientRequestId);

public sealed record ReplayBoardQuery(
    Guid BoardId,
    long? FromSequenceNumber,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string[] EventTypes);
```

### Example DTOs

```csharp
public sealed record BoardDto(
    Guid Id,
    string Name,
    IReadOnlyList<BoardColumnDto> Columns);

public sealed record TaskCardDto(
    Guid Id,
    string Title,
    string Priority,
    string? AssigneeName,
    DateTime? DueDateUtc,
    int CommentCount,
    int ChecklistCompleteCount,
    int ChecklistTotalCount);

public sealed record BoardReplayEventDto(
    long SequenceNumber,
    string EventType,
    string Summary,
    string? BeforeJson,
    string? AfterJson,
    DateTime CreatedAtUtc,
    string ActorDisplayName);

public sealed record CommandPaletteResultDto(
    string Kind,
    string Title,
    string? Subtitle,
    string Action,
    string? Route);

public sealed record AnalyticsDrilldownDto(
    string Title,
    TaskFilterDto Filter,
    IReadOnlyList<TaskCardDto> Tasks);

public sealed record TaskFilterDto(
    string? Search,
    string[] AssigneeUserIds,
    string[] Priorities,
    string[] LabelIds,
    string[] Statuses,
    DateTime? DueBeforeUtc,
    DateTime? DueAfterUtc);
```

---

## 10. SignalR Design

### Hub

`BoardHub`

Current local route: `/hubs/board`.

### Groups

Use one SignalR group per board:

```text
board:{boardId}
```

### Events

```csharp
public interface IBoardClient
{
    Task TaskCreated(TaskCardDto task);
    Task TaskUpdated(TaskCardDto task);
    Task TaskMoved(TaskMovedEvent evt);
    Task TaskDeleted(Guid taskId);
    Task CommentAdded(TaskCommentDto comment);
    Task UserJoinedBoard(BoardPresenceDto user);
    Task UserLeftBoard(string userId);
    Task PresenceChanged(BoardPresenceDto presence);
    Task UserStartedEditing(TaskEditingPresenceDto editing);
    Task UserStoppedEditing(TaskEditingPresenceDto editing);
    Task BoardActivityRecorded(BoardReplayEventDto evt);
    Task ReplaySnapshotAvailable(Guid boardId, long sequenceNumber);
    Task PendingCommandAccepted(Guid clientRequestId);
    Task PendingCommandRejected(Guid clientRequestId, string reason);
    Task NotificationReceived(NotificationDto notification);
}
```

### Design Note

SignalR groups prevent broadcasting every board update to every connected user. Only users currently viewing the board receive board updates.

`BoardHub` must validate board membership before adding a connection to `board:{boardId}`. Editing broadcasts such as `StartEditing` and `StopEditing` must also verify that the user can access the board and that the task belongs to that board before sending group messages.

For the Interactive Server board UI, the browser should own the custom board SignalR connection. Do not instantiate `Microsoft.AspNetCore.SignalR.Client.HubConnection` inside `Board.razor` to call back into the same ASP.NET Core app; that creates an avoidable server-to-server self-connection and complicates cookie/auth handling during prerender. The implemented pattern is:

- Load the pinned browser SignalR client from `wwwroot/vendor/signalr/signalr.min.js`.
- Import `wwwroot/js/boardRealtime.js` from `Board.razor` after first interactive render.
- Let the browser connect to `/hubs/board` with its normal Identity auth cookie.
- Pass hub events back to Blazor through `DotNetObjectReference<Board>`.
- Keep `TaskDrawer.razor` independent of SignalR by emitting field-focus callbacks to the board page.

### MVP Implementation Note

The first Cloud Run demo can use `BoardUpdateNotifier` with `IHubContext<BoardHub>` to broadcast board activity events to browser SignalR clients. Presence is currently in-process, so this is acceptable while Cloud Run is capped at `max-instances=1`; scaling beyond one instance requires Redis, Memorystore, or another fan-out/backplane strategy so every instance receives board events and presence updates.

The board should also surface a compact activity panel that shows the latest persisted activity log entries. This gives moves and edits an immediate visible trace and becomes the foundation for replay mode.

---

## 11. State Management Strategy

Use a lightweight scoped state container.

### `BoardState`

Responsibilities:

- Holds current board DTO
- Applies local updates
- Handles SignalR events
- Exposes events for UI refresh
- Prevents deeply nested components from over-fetching
- Tracks connection status and pending commands
- Tracks presence and field-level editing indicators
- Keeps live board state separate from replay board state
- Emits animation events when replay mode is active

### Example State Methods

- `LoadBoardAsync(boardId)`
- `ApplyTaskCreated(task)`
- `ApplyTaskUpdated(task)`
- `ApplyTaskMoved(event)`
- `SetFilters(filters)`
- `EnterReplayMode(range)`
- `ApplyReplayEvent(event)`
- `ExitReplayMode()`
- `TrackPendingCommand(clientRequestId)`
- `ApplyPresenceChanged(presence)`
- `Clear()`

### Additional State Containers

- `AppActionDispatcher` for scoped shell-to-page actions such as opening the active board's create-task drawer from the top bar.
- `CommandPaletteState` for command results, keyboard focus, and selected action.
- `NotificationState` for unread counts and toast coordination.
- `ConnectionState` for SignalR lifecycle, reconnect attempts, and offline/sync-pending badges.

### Design Rationale

The board state container keeps component state centralized and prevents page-level variables from becoming the source of truth across deeply nested components.

---

## 12. Blazor Render Mode Strategy

### Static SSR

Use static server-side rendering for pages that do not need live interaction:

- Public landing or redirect page
- Basic error pages
- Read-only help/about pages

### Interactive Server

Use Interactive Server for low-latency collaborative surfaces:

- Kanban board
- Task detail drawer
- Presence indicators
- Notifications
- Board replay controls
- Admin audit operations

### Interactive Auto / WebAssembly Candidates

Use Interactive Auto or Interactive WebAssembly only where client-side execution creates a clear benefit:

- Analytics exploration
- Offline draft editing
- Keyboard-heavy command palette interactions
- Local-only board replay playback after event data has loaded

### Design Rationale

Render modes should be intentional per page or component. The app should demonstrate static rendering, server interactivity, and optional client-side interactivity where each model has a clear technical reason.

Do not apply `@rendermode InteractiveServer` directly to `MainLayout`. The layout body is a `RenderFragment`, which cannot be serialized across that interactive boundary. Shell-level actions that need interactivity should be isolated into child components, then coordinate with the active page through scoped state or an action dispatcher.

---

## 13. Validation and Error Handling

### Validation

- Required task title
- Max title length
- Due date cannot be before creation date unless explicitly allowed
- WIP limit warning when moving into a full column
- Role checks before modifying board settings
- Idempotency key required for mutating commands from interactive UI
- Replay requests must stay within authorized board/workspace boundaries
- Conflict resolution must include the expected row version

### Error Handling

- Toast for recoverable errors
- Inline form validation
- Global error boundary
- Retry option for failed SignalR connection
- Optimistic update rollback for failed drag/drop
- Connection badge for reconnecting/offline/sync-pending states
- Conflict dialog for stale updates
- Failed command logging for admin diagnostics

---

## 14. Performance Considerations

### Techniques

- Load board summaries first, task details on demand
- Use DTO projections instead of loading full EF entities
- Paginate activity logs and comments
- Debounce search input
- Avoid unnecessary component re-renders
- Use `@key` for task cards and columns
- Cache lookup data like labels and members per workspace
- Virtualize long activity timelines and audit grids
- Use board snapshots as replay starting points for long histories
- Limit SignalR payload size by sending event deltas instead of full board snapshots
- Precompute expensive analytics where appropriate

### Design Note

The kanban board can become render-heavy, so task cards should stay separate from detail data and avoid loading every comment/checklist item upfront.

---

## 15. Security Considerations

- Authorize every workspace/board access server-side
- Do not rely only on hiding UI buttons
- Validate SignalR hub access before joining board groups
- Prevent users from moving/editing tasks in workspaces where they lack permission
- Sanitize or safely render comments
- Use anti-forgery protections where applicable
- Ensure command palette results only include authorized resources
- Validate replay timeline access server-side
- Never trust client-provided `BeforeJson` or `AfterJson`; generate activity records server-side
- Treat idempotency keys as scoped to user, workspace, and command type

---

## 16. Testing Plan

### Unit Tests

- Task creation rules
- Move task command behavior
- WIP limit validation
- Analytics calculations
- Authorization policy checks
- Replay projection from activity events
- Conflict resolution branches
- Idempotent command handling

### Component Tests

- Task card renders priority and due date correctly
- Filter bar raises expected filter state
- Task editor shows validation messages
- Empty board state displays correctly
- Command palette keyboard navigation works
- Replay controls update timeline position
- Conflict resolution dialog shows before/after values
- Connection badge reflects reconnecting/offline states

### Integration Tests

- Create board
- Create task
- Move task
- Add comment
- Verify activity log created
- Replay board from activity log
- Open analytics drill-down from metric
- Resolve stale update conflict
- Verify unauthorized users cannot access audit or replay data

### End-to-End Tests

- Two browser sessions see live task movement.
- One browser enters replay mode while another continues live edits.
- Offline draft is saved, restored, and synced after reconnect.
- Command palette can create a task and navigate to it.

---

## 17. Minimum Viable Version

Build this foundation first:

1. Auth
2. Workspace + board CRUD
3. Kanban columns
4. Task cards
5. Task create/edit/delete
6. Drag/move task between columns
7. SignalR update when task moves
8. Basic analytics page
9. Activity log
10. Polished loading/error/empty states

Then continue to the signature target:

1. Board replay from `ActivityLog`
2. Presence and editing indicators
3. Conflict resolution for stale task edits
4. Command palette
5. Drill-down analytics
6. Connection resilience and offline drafts
7. QuickGrid audit operations

---

## 18. Suggested Build Order

### Phase 1 — Foundation

- Create solution
- Add auth
- Add database
- Add base layout
- Add seed data

### Phase 2 — Workspace and Boards

- Workspace CRUD
- Board CRUD
- Columns
- Members and roles

### Phase 3 — Task Board

- Task cards
- Task detail drawer
- Create/edit/delete
- Filtering/search
- Drag/drop

### Phase 4 — Real-Time

- SignalR hub
- Join/leave board groups
- Task moved/updated events
- Presence indicators

### Phase 5 — Signature Collaboration

- Activity event schema
- Board replay service
- Timeline scrubber
- Replay animation
- Editing indicators
- Conflict resolution dialog
- Connection status and pending command queue

### Phase 6 — Analytics and Operations

- Charts
- Drill-down analytics
- Activity log
- QuickGrid audit log
- Member role matrix
- Feature flags
- Failed command diagnostics

### Phase 7 — Command and Polish

- Command palette
- Notifications
- Loading states
- Empty states
- Error handling
- Offline drafts
- Theme and density preferences

### Phase 8 — Tests and Deployment

- Component tests
- Service tests
- Playwright multi-browser flows
- Dockerfile and Docker Compose
- Cloud Run deployment
- Firebase Hosting rewrite configuration
- Cloud SQL migration path
- Secret Manager configuration
- README and demo GIF/screenshots

---

## 19. Cross-Session Progress Tracking

This project will be built across multiple sessions. Keep implementation state in `FLOWBOARD_PROGRESS.md`.

### Required Start-of-Session Workflow

At the start of every implementation session:

1. Read this technical spec.
2. Read `FLOWBOARD_PROGRESS.md`.
3. Run `git status --short --branch --ignored`.
4. Verify the active Google Cloud safety boundary:

```bash
gcloud config configurations list
gcloud config list
gcloud --configuration=blazor config list
```

5. Continue from the next unchecked or explicitly named item in `FLOWBOARD_PROGRESS.md`.

### Required End-of-Session Workflow

Before ending every implementation session:

1. Update `FLOWBOARD_PROGRESS.md` with completed work.
2. Add any important implementation notes, blockers, commands, URLs, or deployment state.
3. Record the next recommended task.
4. Run the relevant build/test/verification commands.
5. Commit progress when the repo is in a coherent state.

### Progress File Rules

- Keep `FLOWBOARD_PROGRESS.md` tracked in Git.
- Do not put secrets, tokens, passwords, or connection-string values in it.
- Store durable decisions, current phase, completed features, verification commands, and known risks.
- Use the session log at the bottom for append-only handoff notes.

---

## 20. README Structure

Include a strong README with enough information to build, run, test, and understand the application.

```markdown
# FlowBoard

A real-time collaborative task manager built with Blazor, ASP.NET Core, SignalR, EF Core, and Identity.

## Features

- Real-time kanban board
- Board replay timeline
- Workspace/member management
- Role-based authorization
- Drill-down analytics dashboard
- Activity log
- Notifications
- Command palette
- Presence and conflict resolution
- Offline drafts and connection recovery
- Audit operations
- Component and service tests

## Architecture

Explain projects, layers, render modes, SignalR flow, state management, replay architecture, and authorization boundaries.

## Deployment

Explain Firebase Hosting, Cloud Run, Cloud SQL, Secret Manager, and the Hosting rewrite path.

## Screenshots

Add dashboard, board, task drawer, replay timeline, analytics, and audit operations.

## Running Locally

Add setup instructions.

## Demo Credentials

Add seeded demo users.

## Testing

Add test command.

## Future Improvements

Add advanced feature ideas not yet implemented.
```

---

## 21. Firebase / Cloud Run Deployment Plan

### Target Architecture

```text
User
  -> Firebase Hosting
      -> rewrite /** to Cloud Run
          -> ASP.NET Core Blazor Web App
          -> SignalR hub
          -> ASP.NET Core Identity
          -> EF Core
          -> Cloud SQL PostgreSQL
```

### Firebase Setup

- Create a new Firebase project for FlowBoard.
- Firebase project ID: `blazor-5c3d4`.
- Firebase Web App ID: `1:716082641708:web:2ed0d47399cdeb8bca8a89`.
- Firebase Analytics measurement ID: `G-S5NRH9PCBQ`.
- Firebase Hosting site ID: `blazor-5c3d4`.
- Firebase Hosting live URL: `https://blazor-5c3d4.web.app`.
- Add a Firebase Web App registration for Hosting, preview channels, Analytics, and Performance Monitoring.
- Use Firebase Hosting for the public URL and custom domain.
- Do not replace ASP.NET Core Identity with Firebase Auth unless the auth architecture is intentionally redesigned.
- Firebase Web SDK usage should be limited to browser-side products such as Analytics or Performance Monitoring unless the auth/storage architecture changes.

### Local Google Cloud CLI Safety

Do not change the global/default `gcloud` project while working on FlowBoard. The default configuration is reserved for existing projects such as DeepSpeed.

Use the dedicated Blazor configuration for all Google Cloud commands:

```bash
gcloud --configuration=blazor <command>
```

Current intended configuration:

```text
default -> deepspeed-460b4
blazor  -> blazor-5c3d4
```

Verification commands:

```bash
gcloud config configurations list
gcloud config list
gcloud --configuration=blazor config list
```

Firebase commands should also be explicitly scoped:

```bash
firebase deploy --project blazor-5c3d4 --only hosting
```

### Cloud Run Setup

- Build the Blazor app as a Docker container.
- Deploy the container to Cloud Run.
- Configure environment variables through Cloud Run and Secret Manager.
- Set `ASPNETCORE_URLS=http://+:8080`.
- Configure Cloud Run request timeout high enough for SignalR/WebSocket connections.
- Use `max-instances=1` for the first demo deployment to avoid multi-instance SignalR synchronization issues.
- Add a Redis backplane or equivalent pub/sub strategy before increasing Cloud Run instance count.
- Do not run production migrations or demo seeding unconditionally on app startup. Use CI/CD, a one-off admin job, or explicit environment flags (`FlowBoard__InitializeDatabaseOnStartup`, `FlowBoard__ApplySchemaChangesOnStartup`, `FlowBoard__SeedDemoDataOnStartup`) when a controlled initialization run is required.

Provisioned Cloud Run baseline:

- Service name: `flowboard`.
- Region: `us-central1`.
- Runtime service account: `flowboard-runner@blazor-5c3d4.iam.gserviceaccount.com`.
- The placeholder image was replaced by the Blazor app container during the first app deployment.
- Public Cloud Run URL: `https://flowboard-n6qswg5pla-uc.a.run.app`.
- Max instances: `1`.
- Timeout: `3600`.
- Cloud SQL instance mounted: `blazor-5c3d4:us-central1:blazor-fdc`.
- Replace the placeholder image with the Blazor app container during the first app deployment.

Provisioned Google Cloud APIs:

- `run.googleapis.com`
- `artifactregistry.googleapis.com`
- `cloudbuild.googleapis.com`
- `secretmanager.googleapis.com`
- `sqladmin.googleapis.com`
- `firestore.googleapis.com`
- `firebasehosting.googleapis.com`

Provisioned Artifact Registry:

- Repository: `flowboard`.
- Location: `us-central1`.
- Format: Docker.

### Firebase Hosting Rewrite

Use Firebase Hosting to route all app traffic to Cloud Run:

```json
{
  "hosting": {
    "public": "public",
    "ignore": [
      "firebase.json",
      "**/.*",
      "**/node_modules/**"
    ],
    "rewrites": [
      {
        "source": "**",
        "run": {
          "serviceId": "flowboard",
          "region": "us-central1"
        }
      }
    ]
  }
}
```

### Database and Secrets

- Use Cloud SQL PostgreSQL for production-style persistence.
- Cloud SQL instance: `blazor-fdc`.
- Cloud SQL region: `us-central1`.
- Application database: `flowboard`.
- Application database user: `flowboard_app`.
- Use SQLite only for local development and tests.
- Store connection strings, auth signing keys, OAuth secrets, and SMTP/API secrets in Secret Manager.
- Provisioned Secret Manager secrets:
  - `flowboard-db-connection-string`
  - `flowboard-auth-signing-key`
- Run EF Core migrations during release or through a controlled migration command before shifting traffic.

### SignalR and WebSocket Notes

- Cloud Run supports WebSockets, but connections are still subject to request timeouts.
- The client must reconnect automatically.
- Presence state should tolerate disconnect/reconnect events.
- Single-instance deployment is enough for a polished demo.
- Multi-instance deployment requires shared presence and message fan-out.

### Firebase CLI Commands

Expected deployment commands after the app exists:

```bash
gcloud --configuration=blazor run deploy flowboard \
  --source . \
  --project blazor-5c3d4 \
  --region us-central1 \
  --allow-unauthenticated \
  --service-account flowboard-runner@blazor-5c3d4.iam.gserviceaccount.com \
  --add-cloudsql-instances blazor-5c3d4:us-central1:blazor-fdc \
  --set-secrets ConnectionStrings__DefaultConnection=flowboard-db-connection-string:latest,FlowBoard__AuthSigningKey=flowboard-auth-signing-key:latest \
  --max-instances 1 \
  --timeout 3600
firebase deploy --project blazor-5c3d4 --only hosting
```

Readiness verification:

```bash
curl -I https://blazor-5c3d4.web.app
gcloud --configuration=blazor run services describe flowboard --project blazor-5c3d4 --region us-central1
firebase hosting:channel:list --project blazor-5c3d4
```

---

## 22. Final Scope Recommendation

Prioritize depth, reliability, and one memorable signature experience over disconnected feature count.

**Foundation must-have:**

- Auth
- Kanban board
- Task CRUD
- SignalR live updates
- Analytics
- Activity log
- Clean architecture
- Great README

**Signature must-have:**

- Presence
- Field-level editing indicators
- Conflict resolution
- Board replay timeline
- Command palette
- Drill-down analytics
- Connection resilience
- QuickGrid audit operations

**Polish must-have:**

- Notifications
- Drag/drop polish
- Tests
- Docker
- Demo seed data with realistic multi-user activity
- Screenshots or short GIFs for README

