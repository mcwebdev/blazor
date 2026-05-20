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

The foundational backend is built and seeding data to the dashboard.

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
- Cloud Run currently serves the placeholder Google hello image.
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
- Solution restructured to Clean Architecture (Domain, Application, Infrastructure, Web).
- Database context with SQLite and ASP.NET Core Identity integrated.
- Initial seed data populating dashboard via IDashboardService.

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
- Cloud Run latest ready revision: `flowboard-00003-wt9`
- Artifact Registry repository: `flowboard`
- Cloud SQL instance: `blazor-fdc`
- Cloud SQL connection name: `blazor-5c3d4:us-central1:blazor-fdc`
- Application database: `flowboard`
- Application database user: `flowboard_app`

## Next Steps

Begin Phase 2 - Core Features:

1. Build Login / Registration pages for Identity.
2. Build Board View data integration (fetch columns, tasks, labels from IBoardService).
3. Build Task Panel / Off-canvas drawer.
4. Implement board state mutations (drag and drop, edit tasks).
5. Prepare for PostgreSQL integration on Cloud Run deployment.

## Decisions

- Keep ASP.NET Core Identity as the app authentication system.
- Use Firebase Hosting only as the public hosting/CDN/custom-domain layer.
- Run the Blazor app on Cloud Run.
- Use Cloud SQL PostgreSQL for production persistence.
- Use Firestore only if a later feature explicitly needs it.
- Keep Cloud Run `max-instances=1` until SignalR multi-instance fan-out is added.
- Keep gcloud project isolation through the `blazor` configuration to avoid affecting DeepSpeed.

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
