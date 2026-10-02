#!/usr/bin/env bash
# Gera o relatório de simulação do balanceamento (docs/relatorios/SIMULACAO_BALANCEAMENTO.md).
#
# Joga as regras reais do jogo com um relógio simulado e mede os números atuais de /config:
# progressão, raridades e tamanhos, economia, duração das batalhas, Expedições, Mercado e Leilão.
# Não muda nenhum valor. Rode de novo depois de editar o balanceamento.
#
# Precisa do SDK do .NET 8 ou mais novo.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "O SDK do .NET não foi encontrado. Instale o .NET 8 (ou mais novo) para rodar a simulação."
  exit 1
fi

cd "$REPO_ROOT/tools/Simulador"
dotnet run -c Release -- "$@"
