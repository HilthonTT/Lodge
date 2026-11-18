# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f backend/src/Lodge.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore first (only build/package metadata) so the NuGet layer is cached until a csproj or version changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY backend/src/Lodge.Api/Lodge.Api.csproj backend/src/Lodge.Api/
COPY backend/src/Lodge.Application/Lodge.Application.csproj backend/src/Lodge.Application/
COPY backend/src/Lodge.BackgroundTasks/Lodge.BackgroundTasks.csproj backend/src/Lodge.BackgroundTasks/
COPY backend/src/Lodge.Contracts/Lodge.Contracts.csproj backend/src/Lodge.Contracts/
COPY backend/src/Lodge.Domain/Lodge.Domain.csproj backend/src/Lodge.Domain/
COPY backend/src/Lodge.Infrastructure/Lodge.Infrastructure.csproj backend/src/Lodge.Infrastructure/
COPY backend/src/Lodge.Persistence/Lodge.Persistence.csproj backend/src/Lodge.Persistence/
COPY backend/src/Lodge.Presentation/Lodge.Presentation.csproj backend/src/Lodge.Presentation/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore backend/src/Lodge.Api/Lodge.Api.csproj

COPY backend/src/ backend/src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish backend/src/Lodge.Api/Lodge.Api.csproj \
        --configuration $BUILD_CONFIGURATION \
        --no-restore \
        --output /app/publish \
        -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
# curl is only used by the container health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Lodge.Api.dll"]
