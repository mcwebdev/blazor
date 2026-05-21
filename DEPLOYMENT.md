# FlowBoard Deployment Guide

This document outlines the deployment processes and infrastructure setup for the FlowBoard Blazor Web Application.

## Infrastructure Overview
- **Hosting**: Google Cloud Run
- **Container Registry**: Google Artifact Registry
- **Database**: SQLite (currently stored in the container or mounted volume)
- **Build System**: Docker multi-stage build (SDK 10.0 for build, ASP.NET 10.0 for runtime)

## Deployment Command
The simplest way to deploy the application from source is using the Google Cloud CLI (`gcloud`). This command handles building the Docker image using Cloud Build, pushing it to the registry, and deploying it to Cloud Run.

```bash
gcloud run deploy flowboard \
  --source . \
  --region us-central1 \
  --allow-unauthenticated \
  --project blazor-5c3d4
```

### Explanation of Flags:
- `flowboard`: The name of the Cloud Run service.
- `--source .`: Instructs `gcloud` to build the container from the current directory (using the `Dockerfile` in the root).
- `--region us-central1`: Specifies the Google Cloud region for deployment.
- `--allow-unauthenticated`: Makes the web service publicly accessible over the internet.
- `--project blazor-5c3d4`: The target Google Cloud project ID.

## Dockerfile Architecture
The project uses a standard multi-stage Docker build to keep the final image size small and secure:

1. **Build Stage (`mcr.microsoft.com/dotnet/sdk:10.0`)**: 
   - Restores dependencies using `dotnet restore`
   - Compiles and publishes the code in `Release` configuration
   - Skips the AppHost generation for a leaner output

2. **Runtime Stage (`mcr.microsoft.com/dotnet/aspnet:10.0`)**:
   - Copies the compiled binaries from the build stage
   - Binds Kestrel to port `8080` (Standard for Cloud Run)
   - Exposes port `8080` and sets the entrypoint to `FlowBoard.Web.dll`

## Environment Variables
The following environment variables are set during deployment or in the Dockerfile:
- `ASPNETCORE_URLS=http://+:8080` (Ensures the app binds to the correct port for Cloud Run)
- `ASPNETCORE_ENVIRONMENT=Production` (Automatically set by Cloud Run or can be manually injected)

## Continuous Integration / Continuous Deployment (CI/CD)
To automate this, you can integrate the above `gcloud run deploy` command into GitHub Actions or Google Cloud Build triggers whenever code is pushed to the `main` branch.
