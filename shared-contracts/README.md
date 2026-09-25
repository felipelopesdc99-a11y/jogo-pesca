# /shared-contracts

Definitions that the server, the Unity client and the web surfaces must agree on.

## Why this exists

The server is authoritative, so every contract is written from the server's side and the client
mirrors it. Keeping the shapes here — rather than only inside each project — makes a mismatch a
review question instead of a runtime surprise.

## What is here now

| File | Describes |
|---|---|
| `health.schema.json` | The `/health` and `/health/live` payloads |

## Conventions

- **Wire format is `snake_case`**, matching the repository's JSON files. The C# records use
  PascalCase properties and the API applies a snake_case naming policy; the Unity DTOs and the
  Development Console's TypeScript types use the wire spelling directly. One field, one name.
- **Timestamps are UTC, ISO 8601.** The client never sends a time that matters.
- **Money, XP, levels and counts are integers.** Ratios and multipliers are decimals and live in
  `/config`, never in a contract.

## What comes later

- Config file schemas, once configuration validation lands (`M1-T06`). The console must not be able
  to deploy an invalid balance file.
- Gameplay request/response contracts, each added by the milestone that owns the feature. See
  `docs/roadmap.json`.
