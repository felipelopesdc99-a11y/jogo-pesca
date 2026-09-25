# /shared-contracts

Definições com que o servidor, o cliente Unity e as telas web precisam concordar.

## Por que isto existe

O servidor é autoritativo, então todo contrato é escrito do lado dele e o cliente o espelha. Manter
os formatos aqui — em vez de apenas dentro de cada projeto — transforma uma divergência em uma
pergunta de revisão, e não em uma surpresa em tempo de execução.

## O que existe hoje

| Arquivo | Descreve |
|---|---|
| `health.schema.json` | As respostas de `/health` e `/health/live` |

## Convenções

- **O formato na rede é `snake_case`**, igual aos arquivos JSON do repositório. Os registros em C#
  usam propriedades em PascalCase e a API aplica uma política de conversão; os DTOs do Unity e os
  tipos TypeScript do painel usam a grafia da rede diretamente. Um campo, um nome.
- **Carimbos de tempo são UTC, ISO 8601.** O cliente nunca envia uma hora que importe.
- **Dinheiro, XP, níveis e contagens são inteiros.** Proporções e multiplicadores são decimais e
  moram em `/config`, nunca em um contrato.
- **Os textos dos contratos ficam em inglês**, porque descrevem campos técnicos lidos por quem
  programa. O que o usuário lê na tela está em PT-BR. Veja `docs/DECISOES.md`, TD-014.

## O que vem depois

- Esquemas dos arquivos de configuração, quando a validação de configuração chegar (`M1-T06`). O
  painel não pode ser capaz de publicar um balanceamento inválido.
- Contratos de requisição e resposta do jogo, cada um adicionado pelo milestone dono da
  funcionalidade. Veja `docs/roadmap.json`.
