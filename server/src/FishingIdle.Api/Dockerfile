# Build context is the repository root, so the whole server tree is available.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore first, in its own layer, so a code-only change does not re-download packages.
COPY server/Directory.Build.props server/
COPY server/src/FishingIdle.Api/FishingIdle.Api.csproj server/src/FishingIdle.Api/
RUN dotnet restore server/src/FishingIdle.Api/FishingIdle.Api.csproj

COPY server/ server/
RUN dotnet publish server/src/FishingIdle.Api/FishingIdle.Api.csproj \
      -c Release \
      -o /app/publish \
      --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# wget backs the compose healthcheck; it is present in alpine's busybox.
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=1

# Run as a non-root user. The repository is mounted read-only, so no write access is needed.
RUN addgroup -S fishing && adduser -S -G fishing fishing
USER fishing

COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "FishingIdle.Api.dll"]
