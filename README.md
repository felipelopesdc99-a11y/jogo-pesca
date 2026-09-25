# Fishing Idle — V0.1

Um jogo idle 2.5D relaxante de pesca e coleção, com PvP assíncrono. Cliente em Unity 6.3 LTS,
backend autoritativo em ASP.NET Core, banco PostgreSQL.

**Situação atual: Milestone 0 concluído — a fundação, não o jogo.** Ainda não existe jogabilidade.
O que existe é um servidor funcionando, um banco com migrations, um cliente Unity que prova que
consegue falar com o servidor, e um Painel de Desenvolvimento onde você vê exatamente o que está
pronto e o que vem a seguir.

Progresso: **`docs/roadmap.json`** é a fonte de verdade, e o painel o exibe.

---

## Comece por aqui

| Se você quer… | Leia |
|---|---|
| Ver o que está pronto e o que vem a seguir | O painel, ou `docs/ROADMAP.md` |
| Entender o design do jogo | `docs/GDD_V0_1.md` — a fonte de verdade da V0.1 (original em inglês) |
| Saber por que algo foi feito de determinado jeito | `docs/DECISOES.md` |
| Mudar um número de balanceamento | `config/README.md` |
| Chamar a API | `docs/API.md` |
| Entender o modelo anti-trapaça | `docs/SEGURANCA.md` |
| Saber qual é a versão de cada coisa | `docs/VERSIONAMENTO.md` |

---

## Rodando na sua máquina

### O que você precisa

| Ferramenta | Para quê | Observação |
|---|---|---|
| **Docker Desktop** | Roda o PostgreSQL, a API e o painel | É o único requisito do backend |
| **SDK do .NET 10** | Aplica as migrations do banco a partir da sua máquina | [Baixar](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **Unity Hub + Unity 6.3 LTS** | Abre o cliente do jogo | Só necessário para o cliente |
| **Node.js 22+** | Rodar o painel fora do Docker | Opcional |

### Um comando

```bash
cp .env.example .env          # só na primeira vez
./ops/scripts/dev-up.sh
```

Isso constrói e sobe tudo, espera o PostgreSQL ficar saudável e aplica as migrations do banco.
Ao terminar:

| Serviço | Endereço |
|---|---|
| **Painel de Desenvolvimento** | <http://localhost:3000> |
| Saúde da API | <http://localhost:5080/health> |
| PostgreSQL | `localhost:5432` |

Para parar: `./ops/scripts/dev-down.sh` (com `--purge` também apaga o banco).

### Só o painel, sem Docker

Se você quer apenas ver o andamento do projeto, isso basta — e não precisa de Docker, .NET nem
banco de dados:

```bash
cd web/dev-console
npm install
npm run dev
```

Abra <http://localhost:3000>. Uma faixa amarela avisa que o servidor não está no ar e que a página
foi montada lendo `docs/roadmap.json` direto do repositório. **Isso não é erro**: as páginas de
Visão geral e Roadmap ficam completas; as de Configuração e Build dependem do servidor.

### Depois, abra o cliente do jogo

1. Abra o Unity Hub, **Adicionar projeto**, escolha a pasta `client-unity`.
2. O Unity vai oferecer atualizar o projeto para a sua versão 6.3 instalada — aceite.
3. Aperte **Play**.

Um painel aparece no canto superior esquerdo mostrando um destes estados:

- **conectado** — o servidor respondeu e o banco dele está saudável;
- **degradado** — o servidor respondeu, mas ele não alcança o PostgreSQL;
- **inacessível** — nada está escutando no endereço configurado.

Aperte **F1** para esconder. Ele é removido dos builds de lançamento.
Veja `client-unity/README.md`.

---

## Rodando as partes separadamente

Útil enquanto se desenvolve uma parte só.

### Só o banco

```bash
docker compose -f ops/docker-compose.yml up -d postgres
./ops/scripts/migrate.sh
```

### A API, a partir do código

```bash
cd server
dotnet run --project src/FishingIdle.Api
# http://localhost:5080/health
```

Ela lê `ConnectionStrings:Postgres` de `appsettings.Development.json`, que aponta para
`localhost:5432` — então suba o container do banco antes.

---

## Antes de versionar uma mudança

```bash
./ops/scripts/verify.sh
```

Compila e testa o servidor, depois faz o typecheck e o build do painel.

### Mudanças no banco

```bash
./ops/scripts/new-migration.sh AddPlayerProfile   # criar
./ops/scripts/migrate.sh                          # aplicar
```

As migrations nunca são aplicadas sozinhas quando o servidor sobe — veja `docs/DECISOES.md`, TD-002.

---

## Estrutura do repositório

```
fishing-idle/
├── client-unity/        Cliente Unity 6.3 LTS. Só apresentação e intenção do jogador.
├── server/              API ASP.NET Core. A autoridade sobre tudo que importa.
│   ├── src/FishingIdle.Api/
│   └── tests/FishingIdle.Api.Tests/
├── web/
│   ├── dev-console/     Painel de Desenvolvimento privado (Next.js). Este painel.
│   └── public-site/     Site voltado ao jogador. Milestone 11.
├── shared-contracts/    Formatos que servidor, cliente e web precisam combinar.
├── config/              Todos os valores de balanceamento. Nunca dentro do código.
├── docs/                GDD, decisões, roadmap, API, segurança, versionamento, histórico.
├── ops/                 docker-compose.yml e os scripts acima.
├── version.json         Fonte única da versão de cada componente.
└── .env.example         Modelo do ambiente local. Copie para .env.
```

---

## Como este projeto é construído

Quatro regras que explicam a maior parte da estrutura:

1. **O servidor decide; o cliente mostra.** Tudo com valor econômico, de progressão ou competitivo é
   calculado, validado e gravado no servidor. Um cliente modificado não ganha nada com isso.
   (`docs/SEGURANCA.md`)
2. **O balanceamento mora em dados, não em código.** Peixes, mapas, varas, curvas de XP, preços,
   chances, tempos e taxas — todos editáveis em `/config` sem tocar no código do jogo.
   (`config/README.md`)
3. **O escopo é pequeno de propósito.** A V0.1 é uma fatia vertical do jogo real. Sistemas fora dela
   não são construídos "porque seriam úteis". Quando algo parece realmente faltar, isso vira uma
   tarefa com status `NEEDS_OWNER_DECISION`, nunca um sistema inventado.
4. **O painel reflete o repositório, não uma promessa.** Uma tarefa vira concluída na mesma mudança
   que termina o trabalho. O progresso é auditável no histórico do git, independente de qualquer
   conversa.

### Idioma

Tudo que uma pessoa lê numa tela está em **PT-BR**: o painel, o jogo, as mensagens de erro, o
roadmap, os nomes dos peixes e a documentação. Comentários de código e logs técnicos ficam em
inglês, porque quem os lê é quem programa. A regra completa está em `docs/DECISOES.md`, TD-014.

A única exceção é `docs/GDD_V0_1.md`, mantido no original em inglês de propósito: é o documento de
design travado, e traduzi-lo correria o risco de mudar o sentido de uma decisão já fechada.

---

## Onde as coisas estão

O Milestone 0 está completo, com um item que depende da sua máquina: a tarefa **`M0-T14`** pede que
você rode `./ops/scripts/dev-up.sh` uma vez e abra o projeto no Unity uma vez, porque o Docker e o
Editor do Unity não existiam onde este código foi escrito. O painel mostra isso em *Bloqueios e
decisões suas*, e `docs/CHANGELOG.md` lista o que foi e o que não foi verificado.

O Milestone 1 — contas, estado do jogador e o pipeline de edição de configuração — começa depois disso.
