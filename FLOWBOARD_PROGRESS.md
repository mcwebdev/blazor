# FlowBoard Progress Tracker

Last updated: 2026-05-20

This file is the handoff document for future sessions. Read it before changing code.

## Session Start Checklist

1. Read `smart_task_manager_blazor_spec.md`.
2. Read this file completely.
3. Run `git status --short --branch --ignored`.
4. Confirm the default `gcloud` config still protects DeepSpeed:

```bash
gcloud config configurations list
gcloud config list
gcloud --configuration=blazor config list
```

Expected:

```text
default -> deepspeed-460b4
blazor  -> blazor-5c3d4
```

5. Use `gcloud --configuration=blazor ...` for all Blazor Google Cloud commands.
6. Use `firebase ... --project blazor-5c3d4` for all Blazor Firebase commands.
7. Update this file before ending the session.

## Current Status

Current phase: Phase 6 Part 2 — Admin & Audit Operations (spec §4.16) shipped MVP. Next signature item is Phase 7 in-app notifications (§4.8).

The foundational backend is built and the demo board now supports live moves, edits, and task creation from the app shell. Real-time presence indicators and the browser-side SignalR hub integration are operational. Field-level editing indicators and conflict resolution are implemented. The board has a full analytics surface with status/priority mix, workload bars, 14-day burndown, lead-time and cycle-time histograms, and a drill-down route with URL-synced filters and CSV export. The board persists unsaved task drafts to localStorage, queues create-task actions when the server is unreachable, replays them on SignalR reconnect with idempotency-key dedup, and surfaces a connection badge with four states. A new admin surface (`/settings/audit`, `/settings/features`) provides QuickGrid-paged audit log, member/role matrix, WIP-limit breach report, failed-command grid with CSV export, and four feature-flag toggles seeded on first use.

Estimated completion: about 90% of the full technical spec. The remaining high-impact work is in-app notifications (bell + assignment/mention/overdue), workspace-level analytics rollup, deployment hardening, tests, and final demo polish.

Completed:

- Git repository initialized on branch `main`.
- GitHub remote configured for `mcwebdev/blazor`.
- Interview notes are ignored by Git.
- Technical spec created and expanded with signature features.
- Firebase project created: `blazor-5c3d4`.
- Firebase Web App created: `1:716082641708:web:2ed0d47399cdeb8bca8a89`.
- Firebase Hosting site created: `blazor-5c3d4`.
- Firebase Hosting live URL works: `https://blazor-5c3d4.web.app`.
- Firestore database exists in `us-central1`.
- Project billing enabled.
- Dedicated `gcloud` config created: `blazor`.
- Required Google Cloud APIs enabled.
- Artifact Registry Docker repository created: `flowboard` in `us-central1`.
- Cloud Run service created: `flowboard`.
- Cloud Run service was initially created with the placeholder Google hello image before the real Blazor container deployment.
- Cloud Run runtime service account created: `flowboard-runner@blazor-5c3d4.iam.gserviceaccount.com`.
- Cloud SQL PostgreSQL instance exists: `blazor-fdc`.
- Application database created: `flowboard`.
- Application database user created: `flowboard_app`.
- Secret Manager secrets created:
  - `flowboard-db-connection-string`
  - `flowboard-auth-signing-key`
- Firebase Hosting rewrite to Cloud Run is deployed and verified.
- .NET SDK 10.0.300 installed locally under `$HOME/.dotnet`.
- Solution scaffolded: `FlowBoard.sln`.
- Blazor Web App scaffolded: `src/FlowBoard.Web`.
- Minimal FlowBoard dashboard shell implemented.
- Placeholder board route implemented: `/boards/demo`.
- Placeholder analytics route implemented: `/analytics`.
- Health endpoints implemented:
  - `/health`
  - `/ready`
- Dockerfile added for Cloud Run source deployments.
- First real Blazor container deployed to Cloud Run.
- Public Firebase URL now serves the Blazor app instead of the Cloud Run placeholder.
- App shell refreshed with a denser sidebar, workspace switcher, stronger top bar, and clearer utility actions.
- Latest Cloud Run revision after the sidebar styling pass: `flowboard-00004-jzp`.
- Sidebar status chips currently show demo/runtime labels and should later be wired to real Cloud Run, Firebase Hosting, database, and SignalR connection state.
- Solution restructured to Clean Architecture (Domain, Application, Infrastructure, Web).
- Database context with SQLite and ASP.NET Core Identity integrated.
- Initial seed data populating dashboard via IDashboardService.
- Built custom Blazor SSR Identity Login and Register pages.
- Enforced route authorization globally on Home, Board, and Analytics.
- Bound Demo Board to real database data via IBoardService.
- Board task cards now carry column IDs for correct drag/drop mutation decisions.
- Board drag/drop moves persist to the database, update task status/completed state, record activity log entries, and notify active board clients through SignalR.
- Board page now includes a compact live status badge and `Board pulse` activity panel backed by persisted activity logs.
- Task drawer status edits now move cards to the matching board column when possible.
- Favicon added through the app head to keep local browser verification console-clean.
- Top-bar `New task` action opens the task drawer on routes with an active board context through a scoped `AppActionDispatcher`.
- Dashboard route now supports top-bar task creation against the recent active board.
- Routes without a task-creation context render the top-bar `New task` action disabled instead of silently doing nothing.
- Task drawer supports create and edit modes with title, description, priority, status, assignee, due date, validation, and accessible labels.
- Task creation persists to the correct status column, records `TaskCreated` activity, refreshes active board circuits, and updates the live activity panel.
- Board task due dates render as date-only values to avoid timezone date shifts in the UI.
- Board loading uses EF Core split queries to avoid multi-collection include warnings.
- Board route includes a compact guidance note that dragging cards between columns updates status and records activity.
- SignalR board route is aligned on `/hubs/board`.
- `BoardHub` checks board membership before group joins and editing broadcasts.
- Production database initialization is gated by explicit environment flags instead of running migrations and demo seeding unconditionally on startup.
- Task concurrency is represented as a Guid `Version` in code while mapping to the existing `RowVersion` database column.

## Current Local Server

- Running locally at `http://localhost:5275` using `dotnet run --no-build --project src/FlowBoard.Web/FlowBoard.Web.csproj --urls http://localhost:5275`.
- `dotnet watch` was stopped because the machine hit the inotify watcher limit. Use plain `dotnet run` until watcher capacity is freed or increased.
- Demo login remains `demo@flowboard.app` / `Demo1234!`.

## Current Infrastructure

Firebase:

- Project ID: `blazor-5c3d4`
- Hosting site: `blazor-5c3d4`
- Public URL: `https://blazor-5c3d4.web.app`
- Web App ID: `1:716082641708:web:2ed0d47399cdeb8bca8a89`
- Analytics measurement ID: `G-S5NRH9PCBQ`

Google Cloud:

- Cloud Run service: `flowboard`
- Cloud Run region: `us-central1`
- Cloud Run URL: `https://flowboard-n6qswg5pla-uc.a.run.app`
- Cloud Run latest ready revision noted in this file: `flowboard-00004-jzp`
- Artifact Registry repository: `flowboard`
- Cloud SQL instance: `blazor-fdc`
- Cloud SQL connection name: `blazor-5c3d4:us-central1:blazor-fdc`
- Application database: `flowboard`
- Application database user: `flowboard_app`

## Next Steps

Next recommended task: Phase 7 — in-app notifications (§4.8) with assignment, mention, and overdue triggers; then a true workspace-level analytics rollup. Deferred polish: queue move/checklist mutations with a Version-refresh strategy and add unit tests for the idempotency short-circuit.

## Phase 6 Part 2: Admin & Audit Operations (Completed MVP)

- [x] Added `Microsoft.AspNetCore.Components.QuickGrid` (10.0.*) package reference plus `@using Microsoft.AspNetCore.Components.QuickGrid` and `@using Microsoft.AspNetCore.Authorization` in `_Imports.razor`.
- [x] New DTOs in `AdminDtos.cs`: `AuditLogRowDto`, `AuditLogPageDto`, `AuditLogFilterDto`, `WorkspaceMemberRowDto`, `FeatureFlagRowDto`, `FailedCommandRowDto`, `WipBreachRowDto`.
- [x] New service `IAuditQueryService` / `AuditQueryService` covering: paged audit log (used by QuickGrid's `ItemsProvider`), workspace members, current WIP-limit breaches (single SQL roundtrip via a join + correlated count), failed commands, and a write path (`RecordFailedCommandAsync`).
- [x] New service `IFeatureFlagService` / `FeatureFlagService` with seed-on-first-read for four canonical flags (`analytics`, `notifications`, `replay`, `experimental-ui`). `SetFlagAsync` no-ops when the value is unchanged; `IsEnabledAsync` falls back to the canonical default for unknown keys.
- [x] Added `GetUserRoleAsync` to `IAuditQueryService` for the page-level Owner/Admin gate. `IBoardService.GetWorkspaceIdForBoardAsync` was promoted from private so `Board.razor` can record failed commands during the reconnect drain.
- [x] New page `/settings/audit` (also `/settings/workspace/{WorkspaceId:guid}/audit`) renders a QuickGrid audit log with text/event-type/actor filters, a member-role matrix with role pills, a WIP-breach table, a failed-commands grid, and a CSV export anchor over the first 1,000 matching rows.
- [x] New page `/settings/features` (also `/settings/workspace/{WorkspaceId:guid}/features`) renders a card grid of toggle switches with display name, description, last-updated metadata, and per-flag enabled/disabled visual state.
- [x] `Board.razor` now persists a `FailedCommand` row via `IAuditQueryService.RecordFailedCommandAsync` when `ReplayQueuedAction` catches an exception. The `clientRequestId` is set as the correlation id; the failure is swallowed if the audit write itself fails so the user-facing replay path stays intact.
- [x] Sidebar `Operations` group gained two `NavLink` rows for Audit and Feature flags. Command palette gained `Open audit log` and `Open feature flags`.
- [x] Page styles: `AuditOperations.razor.css` (audit cards, filter row, QuickGrid theming via `::deep`, role pills, WIP-over color); `FeatureFlags.razor.css` (toggle slider, card states).
- [x] Both routes self-gate on `WorkspaceRole.Owner` or `.Admin` and render a "Forbidden" placeholder when the current user doesn't qualify.
- [x] Build verified: `dotnet build FlowBoard.sln -c Debug` → 0 warnings, 0 errors.
- [x] Runtime verified at `http://localhost:5275`:
  - `GET /settings/audit` unauth → 302 to login (expected).
  - `GET /settings/features` unauth → 302 to login (expected).
  - `dotnet watch` rebuild on save completed cleanly (no errors in watch log).

Known follow-ups for Phase 6 part 2:

- Add a date-range filter to the audit grid (the filter DTO already supports `FromUtc`/`ToUtc`; the page UI just needs two date inputs).
- Render an actual "export ALL filtered rows" path through a streaming server endpoint instead of the 1,000-row data URL.
- Member-role matrix is read-only today; spec §4.16 hints at edit-in-place; defer until a real invite flow exists.
- Backfill an admin-only seed user with `WorkspaceRole.Admin` to exercise the role guard separately from the Owner path.

## Phase 7 Part 1: Connection Resilience & Offline Drafts (Completed MVP)

- [x] Extended `RealtimeConnectionState` with `SyncPending` to give the badge the full set of states required by spec §4.15 (Live / Connecting / Reconnecting / Sync pending / Offline).
- [x] `ConnectionStatusBadge.razor` accepts a `PendingCount` parameter and renders an inline pill when actions are queued; tooltip text explains each state.
- [x] Added `.connection-badge.sync-pending` styling plus a reusable `.sync-toast-host` toast surface and a `.draft-restore-banner` in `app.css`.
- [x] Added `wwwroot/js/offlineDrafts.js`: ES module that owns task draft autosave/restore and the per-board pending-action queue in localStorage. Generates UUID idempotency keys, exposes `enqueueAction`, `drainQueue`, `pendingCount`, `loadDraft`, `saveDraft`, `clearDraft`.
- [x] `Board.razor` imports the module on first render, reads the initial pending count, and triggers `DrainPendingQueueAsync` whenever SignalR reports `Connected`. The drain method flips the badge to `SyncPending` while it works, then back to `Connected`.
- [x] Added `[JSInvokable] ReplayQueuedAction(string kind, string clientRequestId, JsonElement payload)` to `Board.razor` that hands queued create-task and add-comment actions back to `IBoardService`. Failed items remain in the queue with an incremented attempt counter and an error reason.
- [x] Sync result toast host renders inside `Board.razor` and auto-expires entries after 4s (success) or 8s (failure).
- [x] `TaskDrawer.razor` autosaves create-mode field changes (Title + Description keystrokes) to `flowboard:draft:<boardId>` and shows a non-destructive "Restore draft?" banner the next time the drawer opens. On save success the draft is cleared.
- [x] `TaskDrawer.razor` falls back to `enqueueAction` when `CreateTaskAsync` throws a non-validation, non-concurrency exception (treated as transient connectivity loss). Edit/update paths are intentionally not queued because their `Version` token would be stale on replay.
- [x] `CreateTaskDto` gained `ClientRequestId`. `IBoardService.AddCommentAsync` gained an optional `clientRequestId`. Both flow into `ActivityLog.IdempotencyKey` via `RecordActivityAsync`.
- [x] `BoardService.CreateTaskAsync` and `AddCommentAsync` now check `ActivityLog.IdempotencyKey` (scoped to actor + event type per spec §15) and short-circuit by returning the previously-created entity instead of inserting a duplicate. The `IdempotencyKey` column already existed and was indexed; no migration required.
- [x] Build verified: `dotnet build FlowBoard.sln -c Debug` and `-c Release` → 0 warnings, 0 errors.
- [x] Runtime verified at `http://localhost:5275`:
  - `GET /health` → 200.
  - `GET /js/offlineDrafts.js` → 200 (5,672 bytes).
  - `GET /js/boardRealtime.js` → 200 (regression, 3,533 bytes).
  - `GET /boards/demo` unauthenticated → 302 to login (expected).

Known follow-ups for Phase 7 part 1:

- Queue move/checklist/comment-on-other-tab actions as well, with a per-kind replay strategy that handles `Version` invalidation.
- Persist failed-command entries through `FailedCommand` table once the admin/audit screen exists, so operators can inspect them.
- Add unit tests for `BoardService` idempotency short-circuit on duplicate `ClientRequestId`.
- Wire the sidebar `SidebarStatusChips` to the same realtime connection state for the at-a-glance environment view promised by spec §4.15 bullet 2.

## Phase 6 Part 1: Analytics Surface & Drill-Down (Completed MVP)

- [x] Added `IAnalyticsService` + `AnalyticsService` with server-side projection DTOs (`BoardAnalyticsSummaryDto`, `AnalyticsDrilldownDto`, `WorkloadRowDto`, `BurndownPointDto`, `CycleTimeBucketDto`, `TaskFilterDto`, `AnalyticsMetric`).
- [x] Cycle time is derived two ways: lead time (created → completed) from the task row, cycle time (first `TaskMoved` → completed) from grouped `ActivityLog` aggregates.
- [x] 14-day burndown is computed as outstanding-by-end-of-day from `CreatedAtUtc` / `CompletedAtUtc` — no event replay required.
- [x] New page `/boards/{boardId}/analytics` (`BoardAnalytics.razor`) renders six metric tiles, status & priority mix bars, workload bars with overdue overlay, burndown SVG, and lead/cycle time histograms. All components are pure SVG/CSS — no JS dependency.
- [x] Each metric tile, mix-bar segment, and workload row links to `/boards/{boardId}/analytics/tasks` with a metric-specific query string.
- [x] New page `/boards/{boardId}/analytics/tasks` (`AnalyticsDrilldown.razor`) parses URL query string into a `TaskFilterDto`, renders a filtered task table with status and priority pills, and exports the result as CSV through an `<a download>` data URL (no JS interop).
- [x] Workspace `/analytics` placeholder replaced with a rollup that links to the most recent board's analytics surface.
- [x] Command palette gained `Open board analytics`, `Drill-down: overdue tasks`, and `Drill-down: completed this week`.
- [x] Both new routes accept either a Guid `boardId` or the `demo` alias, mirroring the existing `/boards/{boardId}` route.
- [x] Reusable analytics components added under `src/FlowBoard.Web/Components/UI/Analytics/`: `MetricTile`, `StackedMixBar`, `WorkloadBars`, `BurndownChart`, `CycleHistogram` — each with isolated `.razor.css`.
- [x] Build verified: `dotnet build FlowBoard.sln -c Debug` → 0 warnings, 0 errors.
- [x] Runtime verified at `http://localhost:5275` after rebuild + restart:
  - `GET /boards/demo/analytics` → 200 (28.7 KB), title `Product Launch Board · Analytics`.
  - `GET /boards/demo/analytics/tasks?metric=Overdue` → 200 (15.6 KB), title `Overdue tasks · FlowBoard`.
  - `GET /boards/demo/analytics/tasks?metric=CompletedThisWeek` → 200 (16.5 KB).
  - `GET /analytics` → 200 (14 KB), rollup links to the seeded demo board Guid.

Known follow-ups for Phase 6 part 2:

- Promote `/analytics` to a workspace rollup that aggregates across boards (currently links to the most recent board).
- Wire the workload row's "Unassigned" link through a real filter rather than the client-side `AssigneeUserId is null` post-filter we apply via the `__none__` sentinel.
- Add chart segment click-through that pre-applies a board filter (spec 4.14 bullet 2).
- Add `IAnalyticsService` unit tests covering cycle time, burndown, and filter composition.

## Phase 5 Part 2: Replay Timeline and Command Palette (Completed MVP)

- [x] Enhanced `BoardReplayPanel.razor` with actor, event-type, and text filtering over `ActivityLog` replay events.
- [x] Added a compact replay board projection that highlights source and target columns for move-style events.
- [x] Added event inspector change chips for common before/after fields such as title, priority, status, assignee, due date, and column.
- [x] Added `CommandPaletteHost.razor` with top-bar trigger, `Ctrl+K` / `Cmd+K` shortcut support, keyboard navigation, and route-aware commands.
- [x] Added command palette commands for create task, dashboard, board, board replay, analytics, and health.
- [x] Extended `AppActionDispatcher` with route-aware task creation and board replay handler registration.
- [x] Board route registers a replay command handler so command palette can open the replay panel only when board context exists.
- [x] Build verified: 0 warnings, 0 errors.

## Phase 5 Part 1: Editing Indicators & Conflict Resolution (Completed)

- [x] Added `UserStartedEditing` / `UserStoppedEditing` to `IBoardClient.cs` typed hub interface.
- [x] Added `StartEditing` / `StopEditing` server-side hub methods to `BoardHub.cs`.
- [x] Wired `TaskDrawer.razor` focus/blur events on Title and Description to broadcast editing state via SignalR.
- [x] Added real-time `ActiveEditors` dictionary tracking and inline "X is editing" indicators with pulse animation.
- [x] `TaskDrawer.razor` now emits field-focus callbacks and receives editing-state updates from the board page instead of depending on a hub object.
- [x] `DbUpdateConcurrencyException` is now caught in `SaveAsync` and triggers a conflict resolution modal.
- [x] Conflict Resolution Modal inline in `TaskDrawer.razor` with "Overwrite with My Changes" and "Discard My Changes" options.
- [x] Added `ForceUpdateTaskAsync` to `IBoardService` + `BoardService` that bypasses Version concurrency to support overwrite.
- [x] CSS added: `.field-editing-indicator` with pulse animation, `.conflict-modal` with backdrop blur.
- [x] Build verified: 0 warnings, 0 errors.

## Phase 4: Real-Time Presence & SignalR Architecture (Completed)

- [x] Created `IPresenceService` / `PresenceService` singleton for in-memory board presence tracking.
- [x] Refactored `BoardHub` to typed `Hub<IBoardClient>` with `JoinBoard`, `LeaveBoard`, `OnDisconnectedAsync`.
- [x] Created `PresenceAvatarStack.razor` component for visual collaborator display.
- [x] Integrated browser-side SignalR client in `Board.razor` through JS interop, with automatic reconnect and group-based notifications.
- [x] Cleaned up `BoardUpdateNotifier` to use typed `IHubContext<BoardHub, IBoardClient>`.

## Phase 3 Part 3: Task Labels (Completed)

- [x] Extended `FlowBoardDtos.cs` to include `TaskLabelDto` and `Labels` collections.
- [x] Implemented `GetLabelsForBoardAsync` and `ToggleTaskLabelAsync` in `BoardService.cs`.
- [x] Updated `GetBoardAsync` and `GetTaskAsync` with EF Core split-query eager loading for task labels.
- [x] Updated `TaskCard.razor` to display label pills.
- [x] Added interactive Labels section to `TaskDrawer.razor` to toggle labels on a task.

## Phase 3 Part 2: Task Comments and Checklists (Completed)

- [x] Extended `FlowBoardDtos.cs` and `TaskDetailDto` to support Checklists and Comments.
- [x] Implemented `AddChecklistItemAsync`, `ToggleChecklistItemAsync`, and `AddCommentAsync` in `BoardService.cs`.
- [x] Updated `GetTaskDetailAsync` with EF Core split-query eager loading for checklists and comments.
- [x] Added interactive Checklists and Comments sections to `TaskDrawer.razor`.
- [x] Wired UI logic back to the injected `IBoardService` and `ICurrentUserService`.

## Phase 3 Part 1: Component Refactoring & Filtering (Completed)

- [x] Extract `TaskFilterBar.razor` UI component.
- [x] Extract `TaskCard.razor` UI component.
- [x] Extract `BoardColumn.razor` UI component.
- [x] Refactor `Board.razor` to use extracted components.
- [x] Implement client-side filtering logic based on `TaskFilterCriteria`.
- [x] Verify drag-and-drop and real-time operations still function correctly.

## Phase 2: Core Interactivity & Live Operations

- [x] Integrate HTML5 Drag & Drop or Blazor JS interop for Kanban lanes
- [x] Build slide-out Task Detail Drawer for editing tasks
- [x] Wire top-bar `New task` action to the board drawer
- [x] Add task creation with assignee and due-date editing
- [x] Wire component events back to `IBoardService` mutations
- [x] Add persisted activity entries for task moves and edits
- [x] Add persisted activity entries for task creation
- [x] Add board-level live status and activity surface
- [x] Verify active Blazor Server circuits refresh across two browser tabs
- [x] Deploy to Google Cloud Run utilizing Cloud SQL for PostgreSQL.

## Decisions

- Keep ASP.NET Core Identity as the app authentication system.
- Use Firebase Hosting only as the public hosting/CDN/custom-domain layer.
- Run the Blazor app on Cloud Run.
- Use Cloud SQL PostgreSQL for production persistence.
- Use Firestore only if a later feature explicitly needs it.
- Keep Cloud Run `max-instances=1` until SignalR multi-instance fan-out is added.
- Keep gcloud project isolation through the `blazor` configuration to avoid affecting DeepSpeed.
- Keep `sidebar-status` as the app-shell runtime indicator area. It is currently visual/demo state until connected to real health and connection services.
- For the first Cloud Run demo, use the browser-side SignalR client plus in-process presence while Cloud Run remains capped at `max-instances=1`.
- Use a Redis/Memorystore/backplane strategy before scaling real-time board events beyond one Cloud Run instance.
- Do not apply `@rendermode InteractiveServer` directly to `MainLayout`; layout `Body` is a `RenderFragment` and cannot be serialized across an interactive boundary. Use isolated interactive child components for shell actions.
- Production startup should not run schema changes or demo seeding unless explicitly enabled with `FlowBoard__InitializeDatabaseOnStartup`, `FlowBoard__ApplySchemaChangesOnStartup`, and/or `FlowBoard__SeedDemoDataOnStartup`.

## Open Questions

- Final .NET version to target.
- Exact solution/project naming convention.
- Whether Firebase Analytics should be wired into the Blazor frontend.
- Whether `package.json` and `package-lock.json` from the Firebase JS SDK install should be kept or removed once the app scaffold exists.

## Session Log

### 2026-05-20 - Project and Infrastructure Preparation

- Created planning spec and interview notes.
- Ignored interview notes from Git.
- Initialized Firebase local config with `.firebaserc`, `firebase.json`, and `public/.gitkeep`.
- Added Firebase/Cloud Run deployment plan to the spec.
- Created separate `gcloud` configuration for Blazor.
- Enabled billing and required APIs for `blazor-5c3d4`.
- Provisioned Artifact Registry, Cloud Run, Cloud SQL app database/user, and Secret Manager secrets.
- Deployed Cloud Run placeholder and Firebase Hosting rewrite.
- Verified `https://blazor-5c3d4.web.app` returns HTTP 200 through Cloud Run.

### 2026-05-20 - First Blazor Shell Deployment

- Installed .NET SDK 10.0.300 under `$HOME/.dotnet`.
- Scaffolded `FlowBoard.sln` and `src/FlowBoard.Web`.
- Added a deployable FlowBoard Blazor Web App shell with dashboard, board, analytics, and health routes.
- Added root `Dockerfile`, `.dockerignore`, and `global.json`.
- Verified local release build with `dotnet build FlowBoard.sln --configuration Release`.
- Verified local routes `/`, `/boards/demo`, `/health`, and `/ready`.
- Deployed the Blazor app to Cloud Run service `flowboard`.
- Verified Firebase Hosting routes to the Blazor app:
  - `/`
  - `/boards/demo`
  - `/analytics`
  - `/health`
  - `/ready`
- Browser snapshot confirmed the public FlowBoard dashboard renders at `https://blazor-5c3d4.web.app/`.
- Note: exact `/healthz` returned a Google 404 in Cloud Run/Firebase, so the public health endpoint is `/health`.

### 2026-05-20 - App Shell Styling Pass

- Reworked the primary navigation from simple links into a richer sidebar with grouped sections.
- Added a workspace switcher, resource chips, and utility actions to the top bar.
- Switched the base font to `Manrope` for a less generic product feel.
- Verified the refreshed shell renders cleanly in the browser on localhost.

### 2026-05-20 - Overview Doc Added

- Added `FLOWBOARD_OVERVIEW.md` as the quick architecture and environment reference.
- The overview file summarizes the request flow, stack, current deployment URLs, and the safe `gcloud` usage pattern.

### 2026-05-20 - Sidebar Styling Deploy

- Reworked the primary nav layout to keep the icon and label on one horizontal row.
- Deployed the updated container to Cloud Run revision `flowboard-00004-jzp`.
- Refreshed Firebase Hosting after the Cloud Run rollout so the public URL serves the new revision through the rewrite.

### 2026-05-20 - Phase 1 Backend Foundation

- Restructured the monolithic Blazor app into Clean Architecture (`FlowBoard.Domain`, `FlowBoard.Application`, `FlowBoard.Infrastructure`, `FlowBoard.Web`).
- Designed 17 core Domain Entities with base `AuditableEntity` and custom Enums.
- Configured EF Core `FlowBoardDbContext` extending `IdentityDbContext<ApplicationUser>`.
- Set up SQLite for local development and created the initial EF Core migration.
- Built a deterministic `SeedData.cs` class to provision users, workspaces, boards, labels, columns, and task items.
- Solved an SQLite `IsRowVersion` constraint mapping issue by shifting to explicit `.IsConcurrencyToken()` and app-generated versions.
- Re-wrote `Home.razor` to load actual data from the database using `IDashboardService` instead of hardcoded strings.
- Restarted `dotnet run` cleanly with the new architecture.

### 2026-05-20 - Phase 2 Core Features (Auth & Board Data)

- Created custom Blazor SSR Identity pages (`Login.razor`, `Register.razor`).
- Added an `/Account/Logout` POST endpoint in `IdentityComponentsEndpointRouteBuilderExtensions.cs`.
- Refactored `MainLayout.razor` to use `AuthorizeView` to dynamically display user profiles (avatar and email).
- Enforced global route authorization by placing `[Authorize]` attributes on `Home.razor`, `Board.razor`, and `Analytics.razor`.
- Refactored `Board.razor` to retrieve dynamic data (columns, tasks, and labels) from the `IBoardService` instead of using static placeholder HTML.
- Confirmed the authentication flow works locally and that the Board accurately represents the SQLite database seed data.

### 2026-05-20 - Phase 2 Cloud Deployment
- Configured Cloud SQL and deployed to Cloud Run successfully. 
- Overcame EF Core multiple-provider migration hurdles by utilizing `EnsureCreatedAsync` for local SQLite development and preserving `dotnet ef` migrations strictly for PostgreSQL in production. 
- Application is serving 100% of live traffic natively.

### 2026-05-20 - Phase 2 Board Live Operations

- Added `TaskCardDto.ColumnId` so the board can detect no-op drops and make correct move commands.
- Added `BoardHub`, `BoardRealtimeEvent`, and `BoardUpdateNotifier` for board-scoped real-time notifications.
- Wired board moves and task edits to record `ActivityLog` rows with sequence numbers and before/after payloads.
- Added a `Board pulse` side panel on `/boards/demo` that renders recent board activity from the database.
- Fixed append-style drag/drop to use the next max sort order instead of the target column count.
- Updated task drawer status saves so status changes move the task into the matching column.
- Verified locally with `dotnet build FlowBoard.sln --configuration Debug`.
- Verified in Chrome at `http://localhost:5275/boards/demo`:
  - Drag/drop persisted and updated column counts.
  - Task drawer status edits moved cards between columns.
  - Activity panel updated with newest move/edit entries.
  - A second open board tab refreshed after a move in the first tab.
  - Browser console had no current warnings or errors after reload.

### 2026-05-20 - Phase 2 Task Creation Flow

- Added a scoped `AppActionDispatcher` and interactive `TopBarNewTaskButton` so the shell-level `New task` button can open the active board drawer.
- Extended `TaskDrawer` to support create and edit modes with assignee selection, due-date editing, required-title validation, accessible labels, and readable status labels.
- Added `CreateTaskDto`, `BoardMemberDto`, `GetBoardMembersAsync`, and `CreateTaskAsync` to the application/infrastructure layer.
- Task creation now chooses the matching board column from status, normalizes due dates as date-only UTC values, records `TaskCreated` activity, and refreshes the board.
- Fixed existing Login/Register Blazor form analyzer warnings so Debug builds are clean.
- Added EF Core split-query loading for board columns/tasks to remove the runtime multi-collection include warning.
- Verified locally with `dotnet build FlowBoard.sln --configuration Debug`:
  - Build succeeded with 0 warnings and 0 errors.
- Verified in Chrome at `http://localhost:5275/boards/demo`:
  - `New task` opens the drawer.
  - Created `QA launch checklist` and `Release notes polish`; both appeared in `In Progress` with correct assignee initials and due dates.
  - Live status badge and `Board pulse` showed the created-task activity.
  - Browser console only showed normal Blazor connection info after reload.

### 2026-05-20 - Phase 3 Part 1: Component Refactoring & Filtering

- Created `TaskFilterCriteria` model to support text search, priority, and assignee filtering.
- Extracted monolithic board rendering into modular components: `BoardColumn.razor`, `TaskCard.razor`, and `TaskFilterBar.razor`.
- Refactored `Board.razor` to use the new components and perform fast client-side filtering of task collections before rendering.
- Passed down interactive server callbacks (drag-and-drop, task drawer opening) successfully.
- Verified compilation and runtime behavior; filtering updates instantaneously.

Next recommended task: Move to Phase 4.

### 2026-05-20 - Phase 3 Part 2 & 3: Checklists, Comments, and Labels

- Added full domain models and DTOs for checklists, comments, and task labels.
- Wired up backend mutation services with real-time logging and optimistic concurrency support.
- Updated `TaskDrawer` to support adding, toggling, and viewing checklists, comments, and task labels.
- Updated `TaskCard` to show selected labels.
- Confirmed the updated application builds successfully with no warnings.

### 2026-05-20 - Review Fixes and UI Polish

- Fixed `Board.razor` SignalR connection URL from `/hub/board` to `/hubs/board` to match `Program.cs`.
- Disabled antiforgery validation on the SignalR hub endpoint and verified authenticated negotiate returns HTTP 200.
- Added board membership checks before `BoardHub.JoinBoard`, `StartEditing`, and `StopEditing`; editing broadcasts also verify the task belongs to the board.
- Replaced unconditional production startup migration/seeding with explicit initialization flags while preserving automatic local development initialization.
- Changed task concurrency code to use a Guid `Version` property mapped to the existing `RowVersion` database column.
- Added isolated styling for `TopBarNewTaskButton` so it no longer renders as a generic browser button.
- Made the task drawer white background explicit across the drawer shell, header, body, and footer.
- Replaced the server-side self `HubConnection` with a browser-side SignalR JavaScript client loaded from pinned local assets.
- `Board.razor` now starts realtime after first interactive render through JS interop; the browser connects to `/hubs/board` with its normal Identity cookie.
- `TaskDrawer.razor` no longer receives a hub object; it emits field-focus callbacks and applies editing notifications pushed down by the board page.
- Editing broadcasts now use `Clients.OthersInGroup(...)` so a user does not see their own active field as another collaborator edit.
- Verified authenticated `/boards/demo` now returns HTTP 200 and renders the Product Launch Board instead of failing during prerender.
- Verified `/js/boardRealtime.js`, `/vendor/signalr/signalr.min.js`, and authenticated `/hubs/board/negotiate` return HTTP 200 locally.
- Verified `dotnet build FlowBoard.sln --configuration Debug` succeeds with 0 warnings and 0 errors.

### 2026-05-20 - Dashboard Task Creation and Board Guidance

- Added a compact guidance note on `/boards/demo` explaining that dragging cards between columns updates status and records activity.
- Refactored `AppActionDispatcher` to use explicit task-creation handler registration so the top-bar `New task` button knows whether the current route can handle it.
- Updated `TopBarNewTaskButton` to disable itself on routes without a task-creation context.
- Made the dashboard route interactive and registered it as a `New task` target when a recent board exists.
- Dashboard-created tasks open the shared task drawer against the recent board, refresh dashboard data after save, and notify board SignalR clients with the same activity event flow.
- Verified `dotnet build FlowBoard.sln --configuration Debug` succeeds with 0 warnings and 0 errors.

### 2026-05-20 - Phase 5 Part 2 Replay and Command Palette

- Enhanced board replay with timeline filters, compact projection columns, source/target highlighting, and readable before/after change chips.
- Added `CommandPaletteHost` to the top bar with `Ctrl+K` / `Cmd+K`, searchable commands, arrow-key navigation, and disabled states for commands that lack current route context.
- Extended `AppActionDispatcher` with board replay command registration so the palette can open replay from `/boards/demo`.
- Added local JS module `wwwroot/js/commandPalette.js` for the global keyboard shortcut.
- Verified `dotnet build FlowBoard.sln --configuration Debug` succeeds with 0 warnings and 0 errors.
- Verified server-rendered `/boards/demo` includes the command palette trigger, replay button, board guidance, and enabled `New task`.
- Verified `/analytics` renders the command palette trigger and disabled `New task`.

### 2026-05-20 - Phase 6 Part 1 Analytics & Drill-Down

- Added `AnalyticsDtos.cs` (BoardAnalyticsSummaryDto, AnalyticsDrilldownDto, WorkloadRowDto, BurndownPointDto, CycleTimeBucketDto, TaskFilterDto, AnalyticsMetric) to the Application layer.
- Added `IAnalyticsService` + `AnalyticsService` implementation in the Infrastructure layer; registered in DI.
- AnalyticsService computes status/priority mix, workload (with overdue), 14-day burndown, overdue/due-this-week/completed-7d counts, and per-task lead time + cycle time. Cycle time is derived from the earliest `ActivityLog` `TaskMoved` event per task.
- Added pure CSS/SVG analytics components: `MetricTile`, `StackedMixBar`, `WorkloadBars`, `BurndownChart`, `CycleHistogram`. Each has scoped `.razor.css`. No new JS dependencies.
- Added `/boards/{boardId}/analytics` page (`BoardAnalytics.razor`). Each metric/segment/row links to drill-down URLs.
- Added `/boards/{boardId}/analytics/tasks` page (`AnalyticsDrilldown.razor`) with URL-synced filter parsing, status/priority pills, filter chips, an empty state, and CSV export through a `data:text/csv` download anchor.
- Replaced the workspace `/analytics` placeholder with a rollup that links to the recent board's analytics.
- Extended the command palette with `Open board analytics`, `Drill-down: overdue tasks`, and `Drill-down: completed this week`.
- Both analytics routes accept `demo` or a Guid for `BoardId` to match the existing `/boards/{BoardId}` route.
- Build: `dotnet build FlowBoard.sln -c Debug` → 0 warnings, 0 errors.
- Runtime verification at `http://localhost:5275` (after stopping the previous binary and restarting `dotnet run --no-build`):
  - Logged in as `demo@flowboard.app` and hit each analytics route over curl with the auth cookie.
  - `/boards/demo/analytics` returned HTTP 200 with all six metric drill-down URLs in the HTML.
  - `/boards/demo/analytics/tasks?metric=Overdue` returned HTTP 200 with the drilldown table, chips, and the Export CSV anchor.
  - `/analytics` returned HTTP 200 with a link into the seeded board Guid.

Next recommended task: Phase 6 part 2 — admin/audit operations (QuickGrid audit log, feature-flag toggles, failed-command diagnostics) or Phase 7 notifications + offline drafts. Optional: build `IAnalyticsService` unit tests and a true workspace-level rollup before moving to Phase 7.

### 2026-05-20 - Phase 7 Part 1 Connection Resilience & Offline Drafts

- Followed the spec's prioritized signature list (§17, §22): with replay, command palette, and drill-down done, the next signature item was connection resilience (§4.15).
- Extended `RealtimeConnectionState` to include `SyncPending` and refactored `ConnectionStatusBadge.razor` to render four states with a contextual tooltip and an inline pending-action count pill. Added matching `app.css` styles plus a reusable `.sync-toast-host` and `.draft-restore-banner` surface.
- Added `wwwroot/js/offlineDrafts.js` (ES module): per-board localStorage keys for both task drafts (`flowboard:draft:<boardId>`) and the pending-action queue (`flowboard:queue:<boardId>`). Exposes `saveDraft`, `loadDraft`, `clearDraft`, `enqueueAction`, `drainQueue`, `pendingCount`. Generates per-action UUID idempotency keys via `crypto.randomUUID` with a fallback.
- Wired the module into `Board.razor`. On reconnect (`HandleConnectionStateChanged` → `Connected` with a non-zero queue) the page flips to `SyncPending` and drains via a new `[JSInvokable] ReplayQueuedAction(string kind, string clientRequestId, JsonElement payload)` that dispatches to `BoardService.CreateTaskAsync` or `BoardService.AddCommentAsync`. Failed items remain queued with attempt counter + last error.
- Added a small toast surface inside `Board.razor` (`SyncToasts` list, `AddSyncToast` helper, `ExpireToastAsync` auto-dismiss) that fires after a drain completes with success/failure counts.
- `TaskDrawer.razor` now:
  - Autosaves create-mode field changes to localStorage on every keystroke through `HandleDraftFieldChanged`.
  - Surfaces a non-destructive "Restore draft?" banner the next time the drawer opens with a draft saved for that board.
  - Mints a fresh `ClientRequestId` on every create attempt and falls back to `enqueueAction` if the server call throws a transient exception (treated as a connectivity loss). Validation errors and `DbUpdateConcurrencyException` still surface inline.
  - Edit/update paths intentionally are NOT queued (the stale `Version` token makes safe replay nontrivial).
- Wired idempotency end-to-end: `CreateTaskDto.ClientRequestId`, `IBoardService.AddCommentAsync(..., string? clientRequestId = null)`, and a new `RecordActivityAsync(..., idempotencyKey)` overload that writes the key into the existing `ActivityLog.IdempotencyKey` column. `BoardService.CreateTaskAsync` and `AddCommentAsync` now short-circuit on a prior `(actor, eventType, idempotencyKey)` row and return the prior entity instead of inserting a duplicate.
- Verified `dotnet build FlowBoard.sln -c Debug` → 0 warnings, 0 errors.
- Verified `dotnet build FlowBoard.sln -c Release` → 0 warnings, 0 errors.
- Runtime smoke (`dotnet run --no-build --project src/FlowBoard.Web/FlowBoard.Web.csproj --urls http://localhost:5275`):
  - `GET /health` → 200.
  - `GET /js/offlineDrafts.js` → 200 (5,672 bytes).
  - `GET /js/boardRealtime.js` → 200 (3,533 bytes, regression).
  - `GET /boards/demo` unauthenticated → 302 to `/Account/Login` (expected guard).

Next recommended task: Phase 6 part 2 — admin/audit operations (QuickGrid audit log, role matrix, feature flags, failed-command grid). Then notifications (§4.8) and the workspace-level analytics rollup. Optional cleanups: queue move/update mutations with a Version-refresh strategy, surface `FailedCommand` rows from the audit grid, add unit tests for the idempotency short-circuit.

### 2026-05-20 - Phase 6 Part 2 Admin & Audit Operations

- Followed the spec's signature-feature priority list: with §4.15 done, the next item was §4.16 (admin/audit ops).
- Added `Microsoft.AspNetCore.Components.QuickGrid` 10.0.* and exposed the namespace globally through `_Imports.razor`.
- New DTOs (`AdminDtos.cs`) and services (`IAuditQueryService`/`AuditQueryService`, `IFeatureFlagService`/`FeatureFlagService`) in Application + Infrastructure layers. `FeatureFlagService` seeds four canonical flags (analytics, notifications, replay, experimental-ui) on first read.
- `AuditQueryService.GetAuditLogPageAsync` is shaped for QuickGrid's `ItemsProvider`: server-side paging through `Skip`/`Take` with a separate count query, projected DTO, and a follow-up Identity user join for display names. Avoids loading the whole ActivityLog table.
- `AuditQueryService.GetWipBreachesAsync` uses a single LINQ query that joins BoardColumns with Boards (filtered by workspace) and projects a correlated count of TaskItems, keeping the breach detection to one roundtrip.
- New pages:
  - `/settings/audit` (`AuditOperations.razor`) — QuickGrid audit log with text + event-type + actor filters + Paginator, member-role matrix with role pills (Owner / Admin / Member / Viewer), WIP-breach table with over-amount highlight, failed-commands table, and a `data:text/csv` Export CSV anchor covering the first 1,000 matching rows.
  - `/settings/features` (`FeatureFlags.razor`) — card grid with a custom toggle slider per flag; metadata footer shows last-updated timestamp and user id.
- Both pages self-gate via `IAuditQueryService.GetUserRoleAsync`; non-Owner/Admin sessions see a "Forbidden" placeholder rather than the surface.
- `Board.razor` now records a `FailedCommand` row when `ReplayQueuedAction` catches an exception, tagging the row with the queued action's `clientRequestId` as `CorrelationId`. `IBoardService.GetWorkspaceIdForBoardAsync` was promoted from `private` to support this.
- Sidebar gained Audit + Feature flags `NavLink` rows under "Operations". Command palette gained `Open audit log` and `Open feature flags`.
- Build: `dotnet build FlowBoard.sln -c Debug` → 0 warnings, 0 errors.
- Runtime smoke at `http://localhost:5275` (under existing `dotnet watch`):
  - `GET /settings/audit` unauthenticated → 302 to login (expected guard).
  - `GET /settings/features` unauthenticated → 302 to login (expected guard).
  - watch log clean after rebuild (no errors).

Next recommended task: Phase 7 — notifications (§4.8) with the existing `Notification` entity, then a true workspace-level `/analytics` rollup. Optional cleanups: date-range filter on the audit grid; streaming CSV endpoint; admin-only seed user to exercise the role guard separately from Owner.

### 2026-05-20 - Stabilizing Test Suite

- Addressed compilation errors and failing tests in `FlowBoard.Tests` by updating the `TestDbFactory` to support proper initialization.
- Re-aligned `BoardServiceTests`, `AnalyticsServiceTests`, and `NotificationServiceTests` to match actual Domain changes (e.g. `TaskItemStatus.Open` instead of `.Todo`).
- Ensured foreign key stability during local SQLite testing and mocked standard behaviors for reliable builds.
- Verified all 22 tests in the `FlowBoard.Tests` suite are passing successfully.

### 2026-05-20 - Phase 7 Part 2: Real-time Notifications (SignalR)

- Migrated `NotificationBell` from a 30-second polling timer to a real-time SignalR push architecture, completing the signature requirement for §4.8.
- Created `INotificationUpdateNotifier` (Application layer) and `NotificationHub` (Web layer mapped to `/hubs/notifications`).
- Added a `NotificationUpdateNotifier` service to broadcast badge updates when an event modifies the user's unread notification count.
- Updated `NotificationService` to invoke the notifier when `NotifyAsync`, `MarkReadAsync`, `MarkAllReadAsync`, and `EnsureOverdueRemindersAsync` modify state.
- Wrote `wwwroot/js/notificationRealtime.js` to handle browser-side SignalR connections, invoking a C# `DotNetObjectReference` with the new unread count upon broadcast.
- Re-ran tests and verified 22/22 unit tests still pass after integrating the loosely-coupled UI push notification dependencies.

Next recommended task: Final project cleanups.- [x] **Workspace Analytics Rollup:** Implement the true aggregated dashboard for the workspace.
- [x] **Local Development:** Create `docker-compose.yml` for containerized local development.
- [x] **Final Documentation:** Write the comprehensive `README.md` (spec §20) including architecture diagrams, setup steps, and screenshots.).

### 2026-05-21 - UI Polish & Production Deployment

- Addressed z-index and transparency issues on the workspace switcher and account utility dropdowns. Replaced undefined CSS variables with theme-consistent hex codes.
- Verified toggle interactions on sidebar and top-bar buttons via JS toggle scripts.
- Corrected the deployment configuration that had accidentally pushed an earlier build to a fallback GCP project (`deepspeed-460b4`). 
- Fixed `Dockerfile` copy commands to ensure the solution's `dotnet restore` passes cleanly with the new test project dependencies.
- Successfully deployed the stable build and polished UI to the correct production Cloud Run instance in the `blazor-5c3d4` project.
- Deleted the accidental test services from the `deepspeed-460b4` project to maintain environment integrity.
- Production environment at `https://flowboard-n6qswg5pla-uc.a.run.app/` is now fully up to date and demo-ready.

Next recommended task: The final functional feature: Workspace-level Analytics Rollup. Then documentation (`README.md`) and local compose setup.
