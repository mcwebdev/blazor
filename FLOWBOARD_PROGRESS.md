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

Current phase: Phase 2 - Features in progress.

The foundational backend is built and the demo board now supports live moves, edits, and task creation from the app shell.

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
- Board drag/drop moves persist to the database, update task status/completed state, record activity log entries, and refresh active Blazor Server circuits.
- Board page now includes a compact live status badge and `Board pulse` activity panel backed by persisted activity logs.
- Task drawer status edits now move cards to the matching board column when possible.
- Favicon added through the app head to keep local browser verification console-clean.
- Top-bar `New task` action opens the board drawer through a scoped `AppActionDispatcher`.
- Task drawer supports create and edit modes with title, description, priority, status, assignee, due date, validation, and accessible labels.
- Task creation persists to the correct status column, records `TaskCreated` activity, refreshes active board circuits, and updates the live activity panel.
- Board task due dates render as date-only values to avoid timezone date shifts in the UI.
- Board loading uses EF Core split queries to avoid multi-collection include warnings.

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

Next recommended task: build the board filter/search surface and start extracting the board page into smaller components (`BoardColumn`, `TaskCard`, `TaskFilterBar`) before adding comments/checklists.

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
- For the first Cloud Run demo, use in-process board notifications while Cloud Run remains capped at `max-instances=1`.
- Use a Redis/Memorystore/backplane strategy before scaling real-time board events beyond one Cloud Run instance.
- Do not apply `@rendermode InteractiveServer` directly to `MainLayout`; layout `Body` is a `RenderFragment` and cannot be serialized across an interactive boundary. Use isolated interactive child components for shell actions.

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

Next recommended task: build the board filter/search surface and start extracting the board page into smaller components (`BoardColumn`, `TaskCard`, `TaskFilterBar`) before adding comments/checklists.
