#!/usr/bin/env bash
# Sobe o ambiente local de desenvolvimento: PostgreSQL, a API do backend e o Painel de Desenvolvimento.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/ops/docker-compose.yml"

cd "$REPO_ROOT"

if [[ ! -f .env ]]; then
  echo "Nenhum arquivo .env encontrado. Criando um a partir de .env.example."
  cp .env.example .env
fi

# shellcheck disable=SC1091
set -a; source .env; set +a

echo "Construindo e subindo o ambiente..."
docker compose --env-file .env -f "$COMPOSE_FILE" up --build -d

echo
echo "Esperando o PostgreSQL ficar saudável..."
for _ in $(seq 1 60); do
  if [[ "$(docker inspect -f '{{.State.Health.Status}}' fishing-idle-postgres 2>/dev/null || echo starting)" == "healthy" ]]; then
    echo "PostgreSQL saudável."
    break
  fi
  sleep 2
done

echo
echo "Aplicando as migrations do banco..."
"$REPO_ROOT/ops/scripts/migrate.sh"

echo
echo "Ambiente no ar:"
echo "  Painel de Desenvolvimento  http://localhost:${DEV_CONSOLE_HOST_PORT:-3000}"
echo "  Saúde da API               http://localhost:${API_HOST_PORT:-5080}/health"
echo "  PostgreSQL                 localhost:${POSTGRES_HOST_PORT:-5432}"
echo
echo "Agora abra a pasta client-unity no Unity Hub e aperte Play; o painel no canto"
echo "superior esquerdo mostra o estado da conexão."
echo
echo "Para parar tudo: ops/scripts/dev-down.sh"
