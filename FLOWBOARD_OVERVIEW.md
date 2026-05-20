# FlowBoard Overview

This is the quick architecture reference for the FlowBoard app.

## Purpose

Build a polished Blazor task manager with real-time collaboration, analytics, replayable activity history, and a Firebase-hosted public entry point.

## Core Stack

- Frontend: Blazor Web App in C#
- Backend: ASP.NET Core
- Real-time: SignalR
- Database: Cloud SQL PostgreSQL via EF Core
- Hosting: Firebase Hosting
- Runtime: Google Cloud Run
- Secrets: Google Secret Manager
- Container images: Artifact Registry

## Request Flow

```text
Browser
  -> Firebase Hosting
      -> rewrite /** to Cloud Run
          -> ASP.NET Core Blazor Web App
          -> SignalR hub
          -> EF Core
          -> Cloud SQL PostgreSQL
```

## Current Environment

- Firebase project: `blazor-5c3d4`
- Firebase Hosting URL: `https://blazor-5c3d4.web.app`
- Cloud Run service: `flowboard`
- Cloud Run URL: `https://flowboard-n6qswg5pla-uc.a.run.app`
- Cloud SQL instance: `blazor-fdc`
- App database: `flowboard`
- App user: `flowboard_app`

## Local Development

- .NET SDK is installed locally under `$HOME/.dotnet`
- Solution file: `FlowBoard.sln`
- Web app project: `src/FlowBoard.Web`
- Local run URL: `http://localhost:5275`

## Google Cloud Safety

Use the dedicated `blazor` gcloud configuration for this app.

```bash
gcloud --configuration=blazor <command>
```

Do not switch the global/default configuration away from DeepSpeed.

## Current App Shape

- FlowBoard dashboard shell is deployed and live.
- `/boards/demo` is a placeholder board route.
- `/analytics` is a placeholder analytics route.
- `/health` and `/ready` return JSON status responses.
- The next build step is app foundation: Identity, EF Core, domain models, and migrations.
