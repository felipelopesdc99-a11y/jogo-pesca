# Versionamento

## Uma fonte única

O arquivo `version.json` na raiz do repositório guarda a versão de cada componente. Nada mais
declara uma versão oficial. O servidor lê o arquivo e o serve em `GET /api/dev/version`; a página
Build / Versão do painel o exibe.

```json
{
  "components": { "client": "0.1.0-m0.1", "server": "0.1.0-m0.1", "web": "0.1.0-m0.1" },
  "build": { "number": 1, "channel": "internal-dev", "date": "2026-09-25", "commit": null }
}
```

O `web/dev-console/package.json` repete a mesma string por exigência do npm. Ele segue o
`version.json`; nunca o contrário.

## Formato

```
0.<menor>.<correção>-m<milestone>.<build>
```

| Parte | Significa |
|---|---|
| `0.` | Produto pré-lançamento. A V0.1 não é 1.0. |
| `<menor>` | A versão do produto: `1` durante toda a V0.1. |
| `<correção>` | Correções dentro de uma versão lançada. `0` até a V0.1 sair. |
| `-m<milestone>` | O milestone do roadmap de onde o build saiu: `m0` a `m12`. |
| `.<build>` | Contador interno de build, zerado quando o milestone muda. |

Exemplos: `0.1.0-m0.1` (primeiro build do Milestone 0), `0.1.0-m7.3` (terceiro build da Arena),
`0.1.0` (V0.1 lançada), `0.1.1` (primeira correção depois do lançamento).

## Quando mudar o quê

| Evento | Mudança |
|---|---|
| Qualquer build interno | `build.number` + 1 |
| Um milestone fecha | `-m<n>` em todos os componentes, `build.number` volta a 1, `current_milestone` atualizado |
| A V0.1 é lançada | Remover o sufixo `-m…` por completo |
| Uma correção após o lançamento | `<correção>` + 1 |

Os componentes podem divergir: publicar uma correção só do cliente sobe apenas
`components.client`. Eles compartilham a parte `-m<milestone>` porque são construídos e testados uns
contra os outros.

## Canais

| Canal | Para |
|---|---|
| `internal-dev` | A máquina do dono e testes internos |
| `internal-test` | Builds internos mais amplos, quando houver o que testar |
| `public` | Builds no site público |

## Checklist de lançamento

1. Rodar `ops/scripts/verify.sh` — o servidor compila e os testes passam, o painel faz typecheck e
   build.
2. Atualizar `version.json`: componentes, `build.number`, `build.date`, `build.commit`.
3. Acrescentar uma entrada em `docs/CHANGELOG.md` sob a nova versão.
4. Atualizar o roadmap: marcar as tarefas do milestone como `DONE` com suas notas de conclusão e
   mover o `current_milestone`.
5. Quando existirem builds (`M12-T09`), preencher `downloads.windows` para que o painel e o site
   público o encontrem.

## Steam

A Steam e o download direto saem da mesma base de código e do mesmo `version.json`. O campo
`downloads.steam` fica `null` até existir uma página na Steam (`M11-T06`). O endpoint de dados do
build (`M11-T05`) foi projetado para que um atualizador leve possa lê-lo depois sem retrabalho — é
por isso que a informação de versão é uma resposta de API, e não uma string embutida no cliente.
