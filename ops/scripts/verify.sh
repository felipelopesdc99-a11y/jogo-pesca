#!/usr/bin/env bash
# Runs the checks that must pass before committing: server build + tests, and the
# Development Console typecheck + build.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

echo "==> Server: build and test"
cd "$REPO_ROOT/server"
dotnet build FishingIdle.sln --nologo
dotnet test FishingIdle.sln --nologo

echo
echo "==> Development Console: typecheck and build"
cd "$REPO_ROOT/web/dev-console"
if [[ ! -d node_modules ]]; then
  npm ci
fi
npm run typecheck
npm run build

echo
echo "All checks passed."
