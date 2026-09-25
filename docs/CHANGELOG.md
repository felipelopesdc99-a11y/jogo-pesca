# Changelog

Versions follow `docs/VERSIONING.md`. Component versions live in `version.json` at the repository
root, which the server serves at `GET /api/dev/version`.

## [0.1.0-m0.1] — 2026-09-25

Milestone 0 — repository and workflow foundation. No gameplay yet, by design.

### Added

- **Monorepo layout** as specified in GDD section 4: `client-unity`, `server`, `web`,
  `shared-contracts`, `config`, `docs`, `ops`.
- **Backend API** (`server/src/FishingIdle.Api`, ASP.NET Core on .NET 10 LTS) with structured
  console logging, options binding, problem-details errors and exact-origin CORS for the console.
  - `GET /health` — status with a per-dependency breakdown, always HTTP 200 so the body can be read.
  - `GET /health/live` — dependency-free liveness signal.
  - `GET /api/dev/roadmap` — validated roadmap plus the computed dashboard summary.
  - `GET /api/dev/version` — component versions from `version.json` plus runtime build facts.
  - `GET /api/dev/config` and `GET /api/dev/config/{file}` — read-only balance files.
- **PostgreSQL persistence** via EF Core 10 and Npgsql, with the initial migration creating
  `config_versions` and `admin_audit_log`. Migrations are applied deliberately, never on startup.
- **36 automated server tests**, including validation tests that run against the real
  `docs/roadmap.json` so a status typo fails the test run.
- **Development Console** (`web/dev-console`, Next.js 15 + TypeScript) with Dashboard, Roadmap,
  Game Config and Build / Version pages. It reads the backend and falls back to reading
  `docs/roadmap.json` directly, labelling which source answered.
- **Unity client shell** (`client-unity`) with a code-driven bootstrap, a settings-driven API client,
  a `/health` poller and a dev-only diagnostic overlay (F1) that distinguishes connected, degraded
  and unreachable.
- **Local stack** — `ops/docker-compose.yml` (PostgreSQL with a healthcheck and a named volume, API,
  console) plus `dev-up.sh`, `dev-down.sh`, `migrate.sh`, `new-migration.sh` and `verify.sh`.
- **Balance configuration** — all seven `/config` files seeded with provisional data: 20 species,
  2 maps with catch weights, Starter Rod and Rod 1 with upgrade costs, the XP curves, size
  distribution, economy, arena and expedition values.
- **Roadmap** — `docs/roadmap.json` with 13 milestones and 126 tasks, plus a JSON schema.
- **Docs** — `GDD_V0_1.md` (source of truth), `DECISIONS.md`, `ROADMAP.md`, `API.md`,
  `SECURITY.md`, `VERSIONING.md`, this changelog, and a root `README.md` with local run
  instructions.

### Known limitations

- `/api/dev/*` is unauthenticated. The stack binds to localhost and must not be exposed publicly
  until `M1-T01` lands. See `docs/SECURITY.md`.
- Config editing is read-only until `M1-T06` to `M1-T08` deliver validation, versioning and audit.
- The Docker Compose stack and the Unity compile have not been run in the authoring environment
  (no Docker daemon, no Unity Editor). Tracked as `M0-T14`.
