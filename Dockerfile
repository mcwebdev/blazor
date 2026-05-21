FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json FlowBoard.sln ./
COPY src/FlowBoard.Web/FlowBoard.Web.csproj src/FlowBoard.Web/
COPY src/FlowBoard.Application/FlowBoard.Application.csproj src/FlowBoard.Application/
COPY src/FlowBoard.Domain/FlowBoard.Domain.csproj src/FlowBoard.Domain/
COPY src/FlowBoard.Infrastructure/FlowBoard.Infrastructure.csproj src/FlowBoard.Infrastructure/
COPY tests/FlowBoard.Tests/FlowBoard.Tests.csproj tests/FlowBoard.Tests/
RUN dotnet restore FlowBoard.sln

COPY . .
RUN dotnet publish src/FlowBoard.Web/FlowBoard.Web.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FlowBoard.Web.dll"]
