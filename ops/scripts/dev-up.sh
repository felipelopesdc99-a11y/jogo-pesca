#!/usr/bin/env bash
# Starts the local development stack: PostgreSQL, the backend API and the Development Console.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/ops/docker-compose.yml"

cd "$REPO_ROOT"

if [[ ! -f .env ]]; then
  echo "No .env found. Creating one from .env.example."
  cp .env.example .env
fi

# shellcheck disable=SC1091
set -a; source .env; set +a

echo "Building and starting the stack..."
docker compose --env-file .env -f "$COMPOSE_FILE" up --build -d

echo
echo "Waiting for PostgreSQL to report healthy..."
for _ in $(seq 1 60); do
  if [[ "$(docker inspect -f '{{.State.Health.Status}}' fishing-idle-postgres 2>/dev/null || echo starting)" == "healthy" ]]; then
    echo "PostgreSQL is healthy."
    break
  fi
  sleep 2
done

echo
echo "Applying database migrations..."
"$REPO_ROOT/ops/scripts/migrate.sh"

echo
echo "Stack is up:"
echo "  API                  http://localhost:${API_HOST_PORT:-5080}/health"
echo "  Development Console  http://localhost:${DEV_CONSOLE_HOST_PORT:-3000}"
echo "  PostgreSQL           localhost:${POSTGRES_HOST_PORT:-5432}"
echo
echo "Then open client-unity in Unity Hub and press Play; the overlay reports the connection."
echo "Stop everything with ops/scripts/dev-down.sh"
