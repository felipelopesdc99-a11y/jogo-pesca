# Security

## The core rule

> **CLIENT = intent + presentation**
> **SERVER = truth + validation + RNG + persistence**

Assume modified clients and bots exist. Every action with economic, progression or competitive value
is decided by the server.

## Never trusted from the client

Current time · fishing cycle completion · which fish was caught · fish size or rarity · XP ·
Coins, Shells, Honor or Energy balances · rod level · fish level · market ownership · auction bid
results · battle outcomes · expedition completion or rewards.

The client may compute freely for **presentation only**: animation timing, water motion, birds,
clouds, catch toasts generated from an authoritative response. None of that may feed back into a
result.

## Required protections

| Protection | Applies to |
|---|---|
| Authenticated requests | Every gameplay and admin endpoint |
| Ownership checks | Every request naming a fish, rod, listing, auction or Cardume |
| Server timestamps | Fishing cycles, Energy, offline accumulation, expeditions, auction expiry |
| Rate limiting | Per account, on every endpoint |
| Idempotency keys | Every mutation moving currency, XP, fish, listings, bids or rewards |
| Database transactions | Every multi-row mutation |
| Row / version locking | Fish state changes, auction bids, rank swaps, market purchases |
| Replay and double-spend protection | Every economic mutation |
| Server-side RNG | Catch generation, size, combat damage rolls, expedition fish finds |
| Immutable audit records | Economy, market and admin operations |
| Admin role separation | Every `/api/dev` and admin endpoint |
| Validated config deployment | Every balance change |

## Worked examples

**Accelerated fishing.** A modified client asks for 500 cycles at once. The server computes the
eligible cycle count from the session's authoritative start timestamp and the persisted cycle cursor,
and processes only cycles that have actually elapsed. Extra requests return the same state.

**Fish double-use.** Two requests try to sell and feed the same `FishInstance` at the same moment. Both
run in a transaction against the fish's state version; one commits, the other is rejected with a
clear conflict.

**Auction race.** Two bids arrive together. The auction row is locked, bids are ordered, one becomes
the highest bid and the other is rejected or refunded. The 1% bid fee is charged per accepted bid
attempt and never returned; the principal is reserved while a bidder leads and released immediately
on being outbid.

**Rare through the wrong rod.** A client claims a Rare catch on Map 1, or on Map 2 with the Starter
Rod. Rarity eligibility is the intersection of the map's `available_rarities` and the equipped rod's
`can_catch_rarities`, both read server-side from config. The client's claim is never an input.

**Private data leak.** A player requests another player's profile. The public projection is built
server-side and cannot include Cardume Strength, Coin or Shell balances, the Fishing Box, Inventory
contents or the Aquarium. Omission is enforced by the projection, not by the UI choosing not to show
it.

## Current state of the code

Milestone 0 has no gameplay endpoints, so there is nothing yet that could be exploited for value.
What is worth stating plainly:

### Open item — dev endpoints are unauthenticated

`/api/dev/roadmap`, `/api/dev/version` and `/api/dev/config` have no authentication in Milestone 0.

- **Impact if exposed.** Read access to project status, version information and balance data. No
  write path exists, no player data exists, and no credentials are served: the config endpoint
  accepts only a bare `*.json` file name under `/config`, and `version.json` and `appsettings.json`
  are both rejected by it.
- **Containment.** The Compose stack publishes ports on localhost. CORS allows only exact configured
  origins, never a wildcard, and only `GET`.
- **Fix.** `M1-T01` (authentication) and `M1-T08` (admin role separation and audit). Until then, do
  not expose this stack to a network you do not control.

### Already in place

- **Repository files are mounted read-only** (`..:/repo:ro`). The server reads project state; it
  cannot write it, so a compromised API cannot rewrite the roadmap or the balance files.
- **The API container runs as a non-root user.**
- **Migrations never run on startup.** A deployment cannot silently reshape the schema.
- **Timestamps are stored as `timestamptz`,** with UTC enforced by a model convention, so no
  ambiguous local time can enter the database.
- **`config_versions` has a unique filtered index** allowing at most one active balance version, so
  "which balance produced this outcome" always has one answer.
- **Audit and config tables exist from the first migration,** before there is any economy to audit.
- **Health and error bodies never contain credentials.** The database probe reports the exception type
  and a plain-language cause, not the connection string.
- **No secrets are committed.** `.env` is git-ignored; `.env.example` carries only local development
  placeholders.

## Reporting

This is a private project during V0.1. If a security issue is found, record it as a `BLOCKED` task in
`docs/roadmap.json` with the subsystem it affects, so it appears on the Dashboard's attention list
rather than living in a chat log.
