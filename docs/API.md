# API

Endereço base em desenvolvimento local: `http://localhost:5080`.

O formato na rede é `snake_case`. Os carimbos de tempo são UTC em ISO 8601. Os erros usam
`application/problem+json`.

## A regra em torno da qual esta API foi construída

> **Cliente = intenção + apresentação. Servidor = verdade + validação + sorteio + persistência.**

O cliente nunca declara um resultado. Ele diz o que o jogador quer fazer e lê o que o servidor
decidiu. O Milestone 0 ainda não tem endpoints de jogo, então este arquivo documenta o que existe
hoje e as convenções que todo endpoint futuro vai seguir.

---

## Saúde

### `GET /health`

Estado da API com detalhamento por dependência. **Sempre HTTP 200** — o corpo carrega o veredito,
para que um cliente consiga saber qual dependência falhou em vez de ver apenas um erro de
transporte.

```json
{
  "status": "healthy",
  "service": "fishing-idle-api",
  "version": "0.1.0-m0.1",
  "environment": "Development",
  "server_time_utc": "2026-09-25T21:13:33.2161844+00:00",
  "dependencies": [
    { "name": "database", "status": "healthy", "latency_ms": 12.4, "detail": null }
  ]
}
```

`status` é `healthy` quando todas as dependências estão saudáveis, e `degraded` quando a API está
servindo mas alguma dependência não está. O campo `detail` nomeia a causa em português claro e nunca
contém credenciais.

### `GET /health/live`

Só sinal de vida — não toca em nenhuma dependência.

```json
{
  "status": "healthy",
  "service": "fishing-idle-api",
  "version": "0.1.0-m0.1",
  "server_time_utc": "2026-09-25T21:13:33.2769703+00:00"
}
```

**Por que os dois.** O overlay de diagnóstico do Unity tem três estados para mostrar, não dois:
*inacessível* (nada responde), *degradado* (`/health/live` responde e `/health` reporta o banco
fora) e *conectado*.

---

## Endpoints do Painel de Desenvolvimento

> **Sem autenticação no Milestone 0.** São telas administrativas. O ambiente sobe apenas em
> localhost e não pode ser exposto publicamente até a autenticação e a separação de papéis chegarem
> (`M1-T01`, `M1-T08`). Veja `docs/SEGURANCA.md`.

### `GET /api/dev/roadmap`

O roadmap validado mais o resumo que a Visão geral exibe.

```json
{
  "summary": {
    "project": "Fishing Idle",
    "target_version": "0.1.0",
    "current_milestone": "M0",
    "current_milestone_title": "Fundação do repositório e do fluxo de trabalho",
    "total_tasks": 126,
    "done_tasks": 13,
    "completion_percent": 10.3,
    "status_counts": { "TODO": 112, "IN_PROGRESS": 0, "DONE": 13, "BLOCKED": 1, "NEEDS_OWNER_DECISION": 0 },
    "milestones": [ { "milestone_id": "M0", "done_tasks": 13, "total_tasks": 14, "completion_percent": 92.9, "status_counts": {} } ],
    "in_progress": [],
    "recently_completed": [ { "milestone_id": "M0", "milestone_title": "…", "task": {} } ],
    "needs_attention": [],
    "next_up": [],
    "open_owner_decisions": []
  },
  "roadmap": { "milestones": [] },
  "source_path": "/repo/docs/roadmap.json",
  "loaded_at_utc": "2026-09-25T21:13:33Z"
}
```

Sobre o resumo, para que os números não precisem ser adivinhados:

- `completion_percent` é `done_tasks / total_tasks` sobre **todas** as tarefas dos Milestones 0 a 12,
  arredondado para uma casa decimal.
- `next_up` traz no máximo 5 tarefas `TODO`, **ordenadas de modo que as tarefas cujas dependências
  já estão `DONE` venham primeiro** — ou seja, o que realmente pode ser começado agora.
- `recently_completed` traz no máximo 5 tarefas `DONE`, da `updated_at` mais recente para a mais
  antiga.
- `needs_attention` traz todas as tarefas `BLOCKED` e `NEEDS_OWNER_DECISION`, na ordem do roadmap.
- `status_counts` sempre inclui os cinco status, inclusive os zerados, para que a legenda do painel
  não mude de forma.

**503** quando o roadmap não pode ser lido ou falha na validação — status desconhecido, id de tarefa
duplicado, dependência apontando para o nada, ou `current_milestone` que não corresponde a nenhum
milestone. O campo `detail` diz qual foi o caso. Exibir um roadmap inválido como se fosse verdade
seria pior do que admitir a falha.

A resposta fica em cache apenas até o arquivo mudar de data de modificação, então editar o roadmap
como parte de uma mudança aparece sem precisar reiniciar o servidor.

### `GET /api/dev/version`

As versões dos componentes vindas de `version.json`, mais o que só o processo em execução sabe:
`environment`, `server_time_utc`, `process_started_utc`, `uptime_seconds`, `dotnet_version`. As
chaves do próprio arquivo são repassadas como estão, para que o painel não precise de uma segunda
camada de mapeamento que poderia divergir.

Quando o arquivo não pode ser lido, `version_file_available` vem `false` e `version_file_error` diz
o motivo.

### `GET /api/dev/config`

Lista os arquivos de balanceamento de `/config` com `balance_status`, `schema_version`,
`description`, tamanho e data da última modificação. O campo `editable` é `false` e `editing_note`
explica por quê: a edição validada, o versionamento de configuração e a auditoria chegam nas tarefas
`M1-T06` a `M1-T08`.

### `GET /api/dev/config/{fileName}`

O conteúdo bruto de um arquivo de balanceamento. Só aceita um nome simples terminado em `.json`,
para que um nome forjado não consiga escapar da pasta `/config`. **404** para qualquer outra coisa.

---

## Convenções que todo endpoint futuro segue

Estas são as regras com que a API de jogo se compromete, declaradas antes de existir jogo a que
aplicá-las.

### Autenticação e autorização

Toda requisição de jogo é autenticada. Toda requisição que toca uma entidade verifica a posse antes
de agir. Endpoints administrativos exigem um papel de administrador, separado da identidade de
jogador.

### O servidor é dono do tempo

Nenhum corpo de requisição pode conter um carimbo de tempo do cliente que afete um resultado. Ciclos
de pesca, regeneração de Energia, acúmulo offline, conclusão de expedição e expiração de leilão são
todos derivados de carimbos de tempo do servidor.

### Idempotência nas mutações econômicas

Toda requisição que movimenta Moedas, Conchas, Honra, XP, peixes, anúncios, lances ou recompensas
carrega uma chave de idempotência. Uma requisição repetida devolve o resultado original em vez de
aplicar a mudança duas vezes. Isso cobre reconexões e reenvios, que são normais, não excepcionais.

```http
POST /api/fishing/sync
Idempotency-Key: 9f1c8a5e-3b42-4a7d-8f10-2c6b9e4d1a03
```

### Concorrência

Tudo em que duas requisições poderiam competir — vender e alimentar o mesmo peixe, dois lances no
mesmo leilão, uma troca de posições, um leilão fechando enquanto chega um lance — roda dentro de uma
transação de banco com trava de linha ou de versão. Exatamente uma mutação vence e a outra recebe
uma rejeição clara.

### Limite de requisições

Aplicado por conta. Um cliente modificado enviando mil sincronizações de pesca por segundo não ganha
nada por causa do cursor de ciclo, e ainda assim é limitado.

### Auditoria

Operações de economia, mercado e administração gravam registros imutáveis de auditoria. Linhas de
auditoria nunca são atualizadas nem apagadas pelo código da aplicação.

### Erros

`application/problem+json` com um `title` que uma pessoa consegue ler e um `detail` que diz o que
fazer. Corpos de erro nunca contêm credenciais, strings de conexão ou tokens.
