# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f backend/src/Lodge.Gateway/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY backend/src/Lodge.Gateway/Lodge.Gateway.csproj backend/src/Lodge.Gateway/
# No --mount=type=cache: packages must live in this layer, since cache mounts are not exported to the CI layer
# cache and a cached restore layer would leave --no-restore publish without packages (NETSDK1064).
RUN dotnet restore backend/src/Lodge.Gateway/Lodge.Gateway.csproj

COPY backend/src/Lodge.Gateway/ backend/src/Lodge.Gateway/
RUN dotnet publish backend/src/Lodge.Gateway/Lodge.Gateway.csproj \
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
HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/gateway/health || exit 1
ENTRYPOINT ["dotnet", "Lodge.Gateway.dll"]
