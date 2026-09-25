#!/usr/bin/env bash
# Creates a new EF Core migration.  Usage: ops/scripts/new-migration.sh AddPlayerProfile
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <MigrationName>" >&2
  exit 1
fi

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT/server"

dotnet tool restore >/dev/null
dotnet ef migrations add "$1" \
  --project src/FishingIdle.Api \
  --output-dir Persistence/Migrations

echo
echo "Migration '$1' created. Review the generated Up/Down before committing,"
echo "then apply it with ops/scripts/migrate.sh"
