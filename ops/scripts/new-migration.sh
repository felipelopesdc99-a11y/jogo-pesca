#!/usr/bin/env bash
# Cria uma nova migration do EF Core.  Uso: ops/scripts/new-migration.sh AddPlayerProfile
#
# O nome da migration fica em inglês por ser um identificador técnico que vira nome de classe C#.
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Uso: $0 <NomeDaMigration>" >&2
  echo "Exemplo: $0 AddPlayerProfile" >&2
  exit 1
fi

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT/server"

dotnet tool restore >/dev/null
dotnet ef migrations add "$1" \
  --project src/FishingIdle.Api \
  --output-dir Persistence/Migrations

echo
echo "Migration '$1' criada. Revise o Up/Down gerado antes de versionar,"
echo "e depois aplique com ops/scripts/migrate.sh"
