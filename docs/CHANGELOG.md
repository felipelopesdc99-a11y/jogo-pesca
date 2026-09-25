# Histórico de mudanças

As versões seguem `docs/VERSIONAMENTO.md`. As versões de cada componente ficam em `version.json` na
raiz do repositório, que o servidor serve em `GET /api/dev/version`.

## [0.1.0-m0.3] — 25/09/2026

A regra de idioma passa a ser obrigatória e permanente, por definição do proprietário.

### Adicionado

- **`CLAUDE.md` na raiz do repositório**, lido automaticamente no início de toda sessão de trabalho.
  Ele carrega a regra obrigatória de idioma na íntegra, mais os outros princípios permanentes do
  projeto: o servidor é a autoridade, balanceamento mora em dados, o escopo da V0.1 é pequeno de
  propósito, o painel reflete o repositório, e `verify.sh` precisa passar antes de versionar.

### Corrigido — texto visível que ainda estava em inglês

- **As mensagens dos scripts de `ops/scripts`**, que são a primeira coisa que o proprietário lê ao
  subir o projeto: `dev-up.sh`, `dev-down.sh`, `migrate.sh`, `new-migration.sh` e `verify.sh`.
- **Os comentários de `.env.example`** na raiz e em `web/dev-console`, arquivos que o proprietário
  abre para editar.
- **Os valores técnicos que chegam à tela vindos da API**: o overlay do Unity agora traduz
  `database` para "banco de dados" e `healthy` para "saudável", em vez de exibir a chave crua.

### Corrigido — consistência das chaves técnicas

- `economy.json` tinha `shells.source` traduzido para `apenas_pesca_na_v0_1` enquanto todas as
  outras chaves técnicas seguiam em inglês. Revertido para `fishing_only_in_v0_1`. Chaves técnicas
  são armazenadas em inglês e traduzidas apenas na exibição; traduzir uma delas quebraria dados já
  gravados.

## [0.1.0-m0.2] — 25/09/2026

Tradução completa para PT-BR. Nenhum comportamento mudou.

### Alterado

- **Painel inteiro em PT-BR.** Todo o texto de interface está concentrado em
  `web/dev-console/src/lib/strings.ts`, de modo que um eventual segundo idioma seja uma mudança de
  um arquivo. Datas, horas e números passaram a ser formatados no padrão brasileiro
  (25/09/2026, 10,3%).
- **Roadmap em PT-BR** — os 13 milestones, suas metas e critérios, e os títulos, descrições e notas
  de conclusão das 126 tarefas.
- **Nomes que o jogador lê em PT-BR e com acentuação correta**: as 20 espécies (Tilápia, Traíra,
  Curimbatá, Matrinxã, Tucunaré, Jaú, Aruanã…), as varas (Vara Inicial, Vara 1), as expedições
  (Saída Rápida, Volta na Margem, Águas Profundas, Viagem Longa), as raridades (Comum, Raro), as
  categorias de tamanho (Pequeno, Adulto, Grande, Excepcional) e as moedas (Moedas, Conchas, Honra).
- **Mensagens do servidor que aparecem na tela em PT-BR**: falhas de validação do roadmap, causa da
  indisponibilidade do banco, erros dos endpoints e a mensagem de partida que orienta a configurar a
  string de conexão.
- **Overlay do Unity em PT-BR**: conectado, degradado, inacessível, e as mensagens de falha de
  conexão.
- **Documentação em PT-BR**, com os arquivos renomeados: `DECISIONS.md` → `DECISOES.md`,
  `SECURITY.md` → `SEGURANCA.md`, `VERSIONING.md` → `VERSIONAMENTO.md`.
- **Regra de idioma registrada** como decisão TD-014 em `docs/DECISOES.md`.

### Mantido em inglês, de propósito

- Comentários de código, nomes de variáveis, classes e arquivos, e logs técnicos do servidor.
- As chaves técnicas gravadas em dados: os status (`TODO`, `DONE`…), os subsistemas (`server`,
  `client`…) e os identificadores (`lambari`, `rod_01`, `map_02`). Uma chave técnica não deve mudar
  quando o idioma muda; o painel a traduz na hora de exibir.
- `docs/GDD_V0_1.md` e `docs/CLAUDE_START_HERE_V0_1.md`, os documentos de design travados. Traduzir
  a fonte de verdade correria o risco de deslocar o sentido de uma decisão já fechada.

## [0.1.0-m0.1] — 25/09/2026

Milestone 0 — fundação do repositório e do fluxo de trabalho. Ainda sem jogabilidade, de propósito.

### Adicionado

- **Estrutura do monorepo** conforme a seção 4 do GDD: `client-unity`, `server`, `web`,
  `shared-contracts`, `config`, `docs`, `ops`.
- **API do backend** (`server/src/FishingIdle.Api`, ASP.NET Core em .NET 10 LTS) com log estruturado
  em console, leitura de opções, erros em problem-details e CORS de origem exata para o painel.
  - `GET /health` — estado com detalhamento por dependência, sempre HTTP 200 para o corpo ser legível.
  - `GET /health/live` — sinal de vida sem dependências.
  - `GET /api/dev/roadmap` — roadmap validado mais o resumo calculado.
  - `GET /api/dev/version` — versões dos componentes mais dados de execução.
  - `GET /api/dev/config` e `GET /api/dev/config/{arquivo}` — arquivos de balanceamento, só leitura.
- **Persistência em PostgreSQL** via EF Core 10 e Npgsql, com a migration inicial criando
  `config_versions` e `admin_audit_log`. As migrations são aplicadas deliberadamente, nunca ao subir.
- **36 testes automatizados do servidor**, incluindo testes de validação que rodam contra o
  `docs/roadmap.json` real, para que um erro de digitação em um status quebre a suíte.
- **Painel de Desenvolvimento** (`web/dev-console`, Next.js 15 + TypeScript) com as páginas Visão
  geral, Roadmap, Configuração do jogo e Build / Versão. Lê o backend e, se ele cair, lê o
  `docs/roadmap.json` direto do repositório, marcando qual fonte respondeu.
- **Esqueleto do cliente Unity** (`client-unity`) com inicialização por código, cliente de API
  guiado por configuração, verificação de `/health` e um overlay de diagnóstico (F1) que distingue
  conectado, degradado e inacessível.
- **Ambiente local** — `ops/docker-compose.yml` (PostgreSQL com healthcheck e volume nomeado, API,
  painel) mais os scripts `dev-up.sh`, `dev-down.sh`, `migrate.sh`, `new-migration.sh` e `verify.sh`.
- **Configuração de balanceamento** — os sete arquivos de `/config` preenchidos com dados
  provisórios: 20 espécies, 2 mapas com pesos de captura, Vara Inicial e Vara 1 com custos de
  melhoria, as curvas de XP, a distribuição de tamanho e os valores de economia, arena e expedições.
- **Roadmap** — `docs/roadmap.json` com 13 milestones e 126 tarefas, mais um esquema JSON.
- **Documentação** — `GDD_V0_1.md` (fonte de verdade), `DECISOES.md`, `ROADMAP.md`, `API.md`,
  `SEGURANCA.md`, `VERSIONAMENTO.md`, este histórico, e um `README.md` na raiz com as instruções de
  execução local.

### Limitações conhecidas

- `/api/dev/*` não tem autenticação. O ambiente sobe em localhost e não pode ser exposto
  publicamente até a tarefa `M1-T01`. Veja `docs/SEGURANCA.md`.
- A edição de configuração é somente leitura até as tarefas `M1-T06` a `M1-T08` entregarem
  validação, versionamento e auditoria.
- O ambiente Docker Compose e a compilação do Unity não foram executados no ambiente de autoria
  (sem daemon do Docker, sem Editor do Unity). Registrado como a tarefa `M0-T14`.
