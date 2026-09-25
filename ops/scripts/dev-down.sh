#!/usr/bin/env bash
# Para o ambiente local. Os dados do banco sobrevivem no volume nomeado,
# a menos que --purge seja usado.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/ops/docker-compose.yml"

cd "$REPO_ROOT"

if [[ "${1:-}" == "--purge" ]]; then
  echo "Parando o ambiente e APAGANDO o volume do banco de dados."
  docker compose -f "$COMPOSE_FILE" down --volumes
  echo "Pronto. O próximo dev-up.sh vai começar com um banco vazio."
else
  docker compose -f "$COMPOSE_FILE" down
  echo "Ambiente parado. Os dados do banco foram mantidos."
  echo "Use --purge se quiser apagá-los também."
fi
