# API

Base URL in local development: `http://localhost:5080`.

Wire format is `snake_case`. Timestamps are UTC ISO 8601. Errors use
`application/problem+json`.

## The rule this API is built around

> **Client = intent + presentation. Server = truth + validation + RNG + persistence.**

The client never states an outcome. It says what the player wants to do and reads what the server
decided. Milestone 0 has no gameplay endpoints yet, so this file documents what exists and the
conventions every later endpoint follows.

---

## Health

### `GET /health`

API status with a per-dependency breakdown. **Always HTTP 200** — the body carries the verdict, so a
client can tell which dependency failed rather than only seeing a transport error.

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

`status` is `healthy` when every dependency is healthy, and `degraded` when the API is serving but a
dependency is not. `detail` names the cause in plain words and never contains credentials.

### `GET /health/live`

Liveness only — touches no dependency.

```json
{
  "status": "healthy",
  "service": "fishing-idle-api",
  "version": "0.1.0-m0.1",
  "server_time_utc": "2026-09-25T21:13:33.2769703+00:00"
}
```

**Why both.** The Unity diagnostic overlay has three states to show, not two: *unreachable* (nothing
answers), *degraded* (`/health/live` answers, `/health` reports the database down), and *connected*.

---

## Development Console endpoints

> **Unauthenticated in Milestone 0.** These are admin surfaces. The stack binds to localhost and must
> not be exposed publicly until authentication and admin role separation land (`M1-T01`, `M1-T08`).
> See `docs/SECURITY.md`.

### `GET /api/dev/roadmap`

The validated roadmap plus the summary the Dashboard renders.

```json
{
  "summary": {
    "project": "Fishing Idle",
    "target_version": "0.1.0",
    "current_milestone": "M0",
    "current_milestone_title": "Repository / workflow foundation",
    "total_tasks": 126,
    "done_tasks": 13,
    "completion_percent": 10.3,
    "status_counts": { "TODO": 112, "IN_PROGRESS": 0, "DONE": 13, "BLOCKED": 0, "NEEDS_OWNER_DECISION": 1 },
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

Notes on the summary, so the numbers are not guessed at:

- `completion_percent` is `done_tasks / total_tasks` over **every** task in Milestones 0–12, rounded
  to one decimal.
- `next_up` is at most 5 `TODO` tasks, **ordered so tasks whose dependencies are all `DONE` come
  first** — that is, what can actually be started now.
- `recently_completed` is at most 5 `DONE` tasks, newest `updated_at` first.
- `needs_attention` is every `BLOCKED` and `NEEDS_OWNER_DECISION` task, in roadmap order.
- `status_counts` always includes all five statuses, including zeros, so a legend stays stable.

**503** when the roadmap cannot be read or fails validation — an unknown status, a duplicate task
id, a dependency pointing nowhere, or a `current_milestone` that matches no milestone. The
`detail` says which. Rendering an invalid roadmap as if it were true would be worse than saying so.

The response is cached only until the file's timestamp changes, so editing the roadmap as part of a
change shows up without restarting the server.

### `GET /api/dev/version`

Component versions from `version.json`, plus what only the running process knows: `environment`,
`server_time_utc`, `process_started_utc`, `uptime_seconds`, `dotnet_version`. The file's own keys are
passed through verbatim so the console needs no second mapping layer.

When the file cannot be read, `version_file_available` is `false` and `version_file_error` says why.

### `GET /api/dev/config`

Lists the `/config` balance files with `balance_status`, `schema_version`, `description`, size and
last-modified time. `editable` is `false` and `editing_note` says why: validated editing, config
versioning and the audit trail arrive in `M1-T06` to `M1-T08`.

### `GET /api/dev/config/{fileName}`

The raw contents of one balance file. Only a bare `*.json` file name is accepted, so a crafted name
cannot escape the `/config` directory. **404** for anything else.

---

## Conventions every later endpoint follows

These are the rules the gameplay API is committed to, so they are stated before there is any
gameplay to apply them to.

### Authentication and authorization

Every gameplay request is authenticated. Every request touching an entity checks ownership before
acting. Admin endpoints require an admin role, separate from a player identity.

### The server owns time

No request body may contain a client timestamp that affects an outcome. Fishing cycles, Energy
regeneration, offline accumulation, expedition completion and auction expiry are all derived from
server timestamps.

### Idempotency on economic mutations

Any request that moves Coins, Shells, Honor, XP, fish, listings, bids or rewards carries an
idempotency key. A repeated request returns the original result instead of applying the change twice.
This covers reconnects and retries, which are normal, not exceptional.

```http
POST /api/fishing/sync
Idempotency-Key: 9f1c8a5e-3b42-4a7d-8f10-2c6b9e4d1a03
```

### Concurrency

Anything where two requests could race — selling and feeding the same fish, two bids on one auction,
a rank swap, an auction closing while a bid arrives — runs in a database transaction with row or
version locking. Exactly one mutation wins and the other gets a clear rejection.

### Rate limiting

Applied per account. A modified client sending a thousand fishing syncs a second gains nothing from
the cycle cursor, and is throttled anyway.

### Audit

Economy, market and admin operations write immutable audit records. Audit rows are never updated or
deleted by application code.

### Errors

`application/problem+json` with a `title` a person can read and a `detail` that says what to do.
Error bodies never contain credentials, connection strings or tokens.
