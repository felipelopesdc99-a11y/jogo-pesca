#!/usr/bin/env bash
# Roda as verificações que precisam passar antes de versionar uma mudança.
#
#   1. Regras do jogo (serviço de jogo local): compila como o Unity compila e roda os testes.
#   2. Código do Unity (jogo e Painel de Desenvolvimento): checagem de compilação sem o Editor.
#   3. Servidor online (adiado, M12): build e testes, se o SDK do .NET 10 estiver instalado.
#   4. Painel web (adiado, M12): typecheck e build, se o Node.js estiver instalado.
#
# Os passos 1 e 2 precisam do SDK do .NET 8 ou mais novo. Eles são os que importam para o MVP local.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "O SDK do .NET não foi encontrado. Instale o .NET 8 (ou mais novo) para rodar as verificações."
  exit 1
fi

echo "==> Regras do jogo: compilação no formato do Unity e testes"
cd "$REPO_ROOT/tools/GameService.Tests"
dotnet test --nologo

echo
echo "==> Código do Unity: checagem de compilação do jogo e do Painel de Desenvolvimento"
cd "$REPO_ROOT/tools/UnityCheck.Game"
dotnet build --nologo
cd "$REPO_ROOT/tools/UnityCheck.Editor"
dotnet build --nologo

echo
if dotnet --list-sdks | grep -q '^10\.'; then
  echo "==> Servidor online (adiado): build e testes"
  cd "$REPO_ROOT/server"
  dotnet build FishingIdle.sln --nologo
  dotnet test FishingIdle.sln --nologo
else
  echo "==> Servidor online (adiado): pulado — o SDK do .NET 10 não está instalado."
fi

echo
if command -v npm >/dev/null 2>&1; then
  echo "==> Painel web (adiado): typecheck e build"
  cd "$REPO_ROOT/web/dev-console"
  if [[ ! -d node_modules ]]; then
    npm ci
  fi
  npm run typecheck
  npm run build
else
  echo "==> Painel web (adiado): pulado — o Node.js não está instalado."
fi

echo
echo "Todas as verificações passaram."
