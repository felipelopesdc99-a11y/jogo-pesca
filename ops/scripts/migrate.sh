#!/usr/bin/env bash
# Applies EF Core migrations to the local PostgreSQL database.
#
# Migrations are applied deliberately, never as a side effect of the server starting, so a
# deployment can never silently reshape the schema.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT/server"

if [[ -f "$REPO_ROOT/.env" ]]; then
  # shellcheck disable=SC1091
  set -a; source "$REPO_ROOT/.env"; set +a
fi

# The compose file reaches PostgreSQL by service name; from the host it is localhost.
HOST_CONNECTION_STRING="Host=localhost;Port=${POSTGRES_HOST_PORT:-5432};Database=${POSTGRES_DB:-fishing_idle};Username=${POSTGRES_USER:-fishing_idle};Password=${POSTGRES_PASSWORD:-fishing_idle_dev}"
export FISHINGIDLE_ConnectionStrings__Postgres="${MIGRATION_CONNECTION_STRING:-$HOST_CONNECTION_STRING}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "The .NET SDK is not installed, so migrations cannot be applied from this machine." >&2
  echo "Install .NET 10 LTS, or run the command inside the api container." >&2
  exit 1
fi

echo "Restoring local dotnet tools..."
dotnet tool restore >/dev/null

echo "Applying migrations to ${POSTGRES_DB:-fishing_idle} on localhost:${POSTGRES_HOST_PORT:-5432}..."
dotnet ef database update --project src/FishingIdle.Api

echo "Migrations applied."
