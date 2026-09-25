#!/usr/bin/env bash
# Roda as verificações que precisam passar antes de versionar uma mudança:
# build e testes do servidor, e typecheck e build do Painel de Desenvolvimento.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

echo "==> Servidor: build e testes"
cd "$REPO_ROOT/server"
dotnet build FishingIdle.sln --nologo
dotnet test FishingIdle.sln --nologo

echo
echo "==> Painel de Desenvolvimento: typecheck e build"
cd "$REPO_ROOT/web/dev-console"
if [[ ! -d node_modules ]]; then
  npm ci
fi
npm run typecheck
npm run build

echo
echo "Todas as verificações passaram."
