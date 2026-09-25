#!/usr/bin/env bash
# Stops the local development stack. Database data survives in the named volume
# unless --purge is passed.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/ops/docker-compose.yml"

cd "$REPO_ROOT"

if [[ "${1:-}" == "--purge" ]]; then
  echo "Stopping the stack and DELETING the database volume."
  docker compose -f "$COMPOSE_FILE" down --volumes
  echo "Done. The next dev-up.sh starts from an empty database."
else
  docker compose -f "$COMPOSE_FILE" down
  echo "Stack stopped. Database data kept. Pass --purge to delete it as well."
fi
