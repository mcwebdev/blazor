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

Current phase: Phase 1 - Foundation in progress.

The first deployable Blazor shell is live.

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

Continue Phase 1 - Foundation:

1. Add project structure for domain/application services, infrastructure, and tests.
2. Add ASP.NET Core Identity.
3. Add EF Core with PostgreSQL provider for production and SQLite for tests.
4. Add local configuration that does not commit secrets.
5. Add seed data plan and initial migrations.
6. Add workspace/member/board/task domain models.
7. Replace placeholder dashboard data with seeded/read-model-backed data.

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
