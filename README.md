# Fishing Idle — V0.1

A relaxing 2.5D idle collectible fishing game with asynchronous PvP. Unity 6.3 LTS client,
server-authoritative ASP.NET Core backend, PostgreSQL.

**Current state: Milestone 0 complete — the foundation, not the game.** There is no gameplay yet.
What exists is a running server, a database with migrations, a Unity client that proves it can reach
the server, and a Development Console where you can see exactly what is done and what is next.

Progress: **`docs/roadmap.json`** is the source of truth, and the Development Console renders it.

---

## Start here

| If you want to… | Read |
|---|---|
| See what is done and what is next | The Development Console, or `docs/ROADMAP.md` |
| Understand the game design | `docs/GDD_V0_1.md` — the source of truth for V0.1 |
| Know why something was built a certain way | `docs/DECISIONS.md` |
| Change a balance number | `config/README.md` |
| Call the API | `docs/API.md` |
| Understand the anti-cheat model | `docs/SECURITY.md` |
| Know what version anything is | `docs/VERSIONING.md` |

---

## Running it locally

### What you need

| Tool | Why | Notes |
|---|---|---|
| **Docker Desktop** | Runs PostgreSQL, the API and the console | The only requirement for the backend |
| **.NET 10 SDK** | Applies database migrations from your machine | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **Unity Hub + Unity 6.3 LTS** | Opens the game client | Only needed for the client |
| **Node.js 22+** | Running the console outside Docker | Optional |

### One command

```bash
cp .env.example .env          # first time only
./ops/scripts/dev-up.sh
```

That builds and starts everything, waits for PostgreSQL to report healthy, and applies the database
migrations. When it finishes:

| Service | URL |
|---|---|
| **Development Console** | <http://localhost:3000> |
| API health | <http://localhost:5080/health> |
| PostgreSQL | `localhost:5432` |

Stop it with `./ops/scripts/dev-down.sh` (add `--purge` to also delete the database).

### Then open the game client

1. Open Unity Hub, **Add project**, pick the `client-unity` folder.
2. Unity will offer to upgrade the project to your installed 6.3 patch version — accept it.
3. Press **Play**.

A panel appears in the top-left showing one of:

- **connected** — the server answered and its database is healthy;
- **degraded** — the server answered but it cannot reach PostgreSQL;
- **unreachable** — nothing is listening at the configured URL.

Press **F1** to hide it. It is compiled out of release builds. See `client-unity/README.md`.

---

## Running the pieces separately

Useful while developing one part.

### Just the database

```bash
docker compose -f ops/docker-compose.yml up -d postgres
./ops/scripts/migrate.sh
```

### The API, from source

```bash
cd server
dotnet run --project src/FishingIdle.Api
# http://localhost:5080/health
```

It reads `ConnectionStrings:Postgres` from `appsettings.Development.json`, which points at
`localhost:5432` — so start the database container first.

### The Development Console, from source

```bash
cd web/dev-console
npm install
FISHING_IDLE_API_BASE_URL=http://localhost:5080 npm run dev
# http://localhost:3000
```

The console prefers the API and falls back to reading `docs/roadmap.json` directly if the server is
down, so you can always see project status. It tells you at the bottom of the page which source
answered.

---

## Before you commit

```bash
./ops/scripts/verify.sh
```

Builds and tests the server, then typechecks and builds the console.

### Database changes

```bash
./ops/scripts/new-migration.sh AddPlayerProfile   # create it
./ops/scripts/migrate.sh                          # apply it
```

Migrations are never applied automatically on server startup — see `docs/DECISIONS.md` TD-002.

---

## Repository layout

```
fishing-idle/
├── client-unity/        Unity 6.3 LTS client. Presentation and player intent only.
├── server/              ASP.NET Core API. The authority on everything that matters.
│   ├── src/FishingIdle.Api/
│   └── tests/FishingIdle.Api.Tests/
├── web/
│   ├── dev-console/     Private Development Console (Next.js). This panel.
│   └── public-site/     Player-facing website. Milestone 11.
├── shared-contracts/    Shapes the server, client and web must agree on.
├── config/              Every balance value. Never hardcoded in gameplay code.
├── docs/                GDD, decisions, roadmap, API, security, versioning, changelog.
├── ops/                 docker-compose.yml and the scripts above.
├── version.json         Single source of truth for every component version.
└── .env.example         Local environment template. Copy to .env.
```

---

## How this project is built

Four rules that explain most of the structure:

1. **The server decides; the client shows.** Anything with economic, progression or competitive value
   is computed, validated and persisted server-side. A modified client gains nothing.
   (`docs/SECURITY.md`)
2. **Balance lives in data, not code.** Fish, maps, rods, XP curves, prices, chances, timers, fees —
   all editable in `/config` without touching gameplay code. (`config/README.md`)
3. **Scope is deliberately small.** V0.1 is a vertical slice of the real game. Systems outside it are
   not built "because they would be useful". When something genuinely seems missing, it becomes a
   `NEEDS_OWNER_DECISION` task rather than an invented mechanic.
4. **The panel reflects the repository, not a claim.** A task becomes `DONE` in the same change that
   completes the work. Progress is auditable in git, independent of any one session.

---

## Where things stand

Milestone 0 is complete except for one item that needs your machine: **`M0-T14`** asks you to run
`./ops/scripts/dev-up.sh` once and open the Unity project once, because Docker and the Unity Editor
were not available where this code was written. The Dashboard shows it under *Blockers and owner
decisions*, and `docs/CHANGELOG.md` lists what was and was not verified.

Milestone 1 — accounts, player state and the config editing pipeline — starts after that.
