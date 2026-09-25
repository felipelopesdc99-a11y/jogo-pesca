#!/usr/bin/env bash
# Aplica as migrations do EF Core no PostgreSQL local.
#
# As migrations são aplicadas deliberadamente, nunca como efeito colateral de o servidor subir,
# para que um deploy jamais remodele o schema em silêncio.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT/server"

if [[ -f "$REPO_ROOT/.env" ]]; then
  # shellcheck disable=SC1091
  set -a; source "$REPO_ROOT/.env"; set +a
fi

# Dentro do Compose o PostgreSQL é alcançado pelo nome do serviço; a partir da sua máquina é localhost.
HOST_CONNECTION_STRING="Host=localhost;Port=${POSTGRES_HOST_PORT:-5432};Database=${POSTGRES_DB:-fishing_idle};Username=${POSTGRES_USER:-fishing_idle};Password=${POSTGRES_PASSWORD:-fishing_idle_dev}"
export FISHINGIDLE_ConnectionStrings__Postgres="${MIGRATION_CONNECTION_STRING:-$HOST_CONNECTION_STRING}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "O SDK do .NET não está instalado, então as migrations não podem ser aplicadas desta máquina." >&2
  echo "Instale o .NET 10 LTS, ou rode o comando dentro do container da api." >&2
  exit 1
fi

echo "Restaurando as ferramentas locais do dotnet..."
dotnet tool restore >/dev/null

echo "Aplicando as migrations em ${POSTGRES_DB:-fishing_idle} em localhost:${POSTGRES_HOST_PORT:-5432}..."
dotnet ef database update --project src/FishingIdle.Api

echo "Migrations aplicadas."
