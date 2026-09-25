# Decisions

Two kinds of decision live here.

**Part 1** is the owner's locked V0.1 design. It is copied from GDD section 47 and is not open for
reinterpretation. Treat every line as settled unless the owner explicitly reopens it.

**Part 2** is the technical decisions taken while implementing, in the cases the GDD leaves open.
Each one records what was chosen, why, and what would make it worth revisiting. Per the Start Here
instruction: purely technical choices that do not change player experience are made and documented
here; anything that would change player experience, economy, progression or scope becomes a
`NEEDS_OWNER_DECISION` task in `roadmap.json` instead.

---

# Part 1 — Locked V0.1 principles

- Unity 6.3 LTS + C#
- 2.5D beautiful visual presentation
- Server authoritative
- 30s online fishing
- 60s offline fishing
- 24h offline cap
- Fishing Box separate from Aquarium
- Aquarium hard cap 100
- Fish stats deterministic from species / rarity / size / level
- Fish max Lv.10
- Feeding consumes fish
- 50% invested XP recovery when consuming a levelled fish
- One Cardume per player
- Cardume 1–6
- 6/6 = +3% all four stats in all Cardume activities
- Four combat stats only
- No skills
- Micro damage RNG only
- Speed secondary
- Target order 1 → 6
- 3 front + 3 back visual formation
- Dead slot stays empty
- Most battles target ≤ ~60s but no hard timeout
- Replay 1x / 2x / skip
- Arena Energy max 24, +1/hour
- Every Arena attack costs 1 Energy
- Rank direct swap on attacker victory
- 3 Arena opponents in a ~10% upward window
- One reroll, then locked until attack
- Honor gains on wins/defenses and small losses on defeat
- Cardume Strength private
- Expedition durations 30m / 1h / 3h / 6h
- Expeditions reward Coins + a very rare fish chance, no XP or Shells
- Market fixed-price: 5 listings, 7 days, 3% sale fee, no listing fee
- Unified Items to Withdraw
- Auction: one active seller listing, 6h
- Auction +3% minimum next bid
- 1% bidder fee every bid
- 1m anti-snipe reset
- Seller early close only when a bid exists, 3% early-close fee
- Maps change manually
- Map travel 30s
- 10-map / 100-level long-term structure
- Rod tiers 1/3/5/7/9 map pairing pattern via Lv.10/30/50/70/90 purchase milestones
- V0.1 includes Starter Rod + Rod 1
- Rod bonuses: rarity chance, size quality, Shell acquisition
- Rod internal Lv.1–10 by Coin purchase
- Shells exist but have no V0.1 spending use
- Inventory lives inside Profile
- Encyclopedia lives inside Profile
- Development Console / admin panel is part of the project workflow
- Public website + direct download is part of the product direction

## Do-not-do list

Not to be added without explicit owner instruction: extra currencies, crit chance, dodge, accuracy,
active skills, fish classes, healer roles, formation synergies, artificial balanced-team bonuses,
Aquarium expansion, random fish IVs, multiple Cardumes, a separate defense Cardume, a daily PvP
attack cap beyond Energy, auction cancellation convenience, extra equipment slots in V0.1,
complicated crafting, daily quests, achievements to fill UI, social features to fill Profile,
automatic map travel on level-up.

---

# Part 2 — Technical decisions

## TD-001 — Backend on .NET 10 LTS

**Decision.** The API targets `net10.0`.

**Why.** The GDD specifies ASP.NET Core in C# without pinning a version. .NET 10 is the current LTS,
so its support window comfortably outlasts V0.1, and it shares the C# ecosystem with Unity as the
GDD intends.

**Revisit if.** A required library or a hosting provider does not support .NET 10.

## TD-002 — EF Core with Npgsql, migrations applied deliberately

**Decision.** Schema is managed only through EF Core migrations. The server never migrates on
startup; `ops/scripts/migrate.sh` applies them.

**Why.** Migration-based schema management is a GDD requirement. Auto-migrating on boot means a
deployment can silently reshape a live database, which is exactly the class of accident a
server-authoritative economy cannot afford.

**Revisit if.** Never for convenience. A managed deployment pipeline may run the same command as an
explicit step.

## TD-003 — `snake_case` everywhere on the wire and in the database

**Decision.** JSON on the wire and PostgreSQL columns both use `snake_case`. C# keeps PascalCase
internally and a naming policy converts at the boundary.

**Why.** The repository's own config and roadmap files are `snake_case`. One spelling per field means
the Unity DTOs, the console's TypeScript types and the JSON files never disagree, and hand-written
SQL stays readable.

## TD-004 — Catch weights live in `maps.json`, not `fish_catalog.json`

**Decision.** `fish_catalog.json` holds only species-intrinsic data. Per-map catch weights live with
the map.

**Why.** A fish pool is a property of a place. Keeping weights on the map means a species appearing
on two maps is defined once, and a map's pool can be rebalanced without touching the species.

## TD-005 — The roadmap file is the source of truth, and the server derives the summary

**Decision.** `docs/roadmap.json` is authoritative. The server reads it and computes the dashboard
summary (completion percentage, next-up ordering, attention list). The Development Console renders
what the server computed.

**Why.** Deriving the numbers in one place means every consumer agrees on what "42% complete" means.
The file being plain JSON in the repository keeps progress auditable and independent of any one chat
session, which the GDD requires.

## TD-006 — The console falls back to reading the roadmap file directly

**Decision.** When the API is unreachable, the console reads `docs/roadmap.json` itself and computes
the summary in TypeScript, labelling the page with which source answered.

**Why.** The owner should be able to see project status even when the server is down — that is the
panel's whole purpose. The cost is one mirrored function (`web/dev-console/src/lib/summary.ts`)
which must be changed alongside the backend's `RoadmapService.BuildSummary`. The visible source
label means a drift between the two shows up rather than hiding.

**Revisit if.** The summary logic grows enough that mirroring it becomes a real maintenance risk. The
alternative is a console that shows nothing when the API is down.

## TD-007 — Invalid roadmap data is rejected, not rendered

**Decision.** The loader refuses a roadmap with an unknown status, a duplicate task id, a dependency
pointing nowhere, or a `current_milestone` that does not exist. The endpoint then answers 503 with
the reason.

**Why.** A panel that renders wrong status as if it were true is worse than a panel that says it
cannot read the file. `RepositoryRoadmapIntegrityTests` runs the same validation against the real
file so a typo fails in the test run.

## TD-008 — The Unity client bootstraps from code, not from a committed scene

**Decision.** `ClientBootstrap` installs the health probe and the diagnostic overlay via
`RuntimeInitializeOnLoadMethod`. No `.unity` scene asset is committed in Milestone 0.

**Why.** Pressing Play in any scene shows the connection state, and a hand-written scene file — whose
format is editor- and version-specific — cannot be authored reliably outside the editor. The first
designed scene is built in the editor as part of `M2-T01`.

## TD-009 — Render pipeline selection deferred to the first scene

**Decision.** No render pipeline package is added in Milestone 0.

**Why.** Universal RP is the expected choice for the 2.5D presentation, but the pipeline asset, the
camera setup and the scene's layering are one decision. Adding the package now would ship a
half-configured graphics stack. It is selected as part of `M2-T01`.

**Revisit if.** Nothing — this is a scheduling choice, not a rejection of URP.

## TD-010 — Only `ProjectVersion.txt` is committed under `ProjectSettings/`

**Decision.** Unity generates the rest of `ProjectSettings/` with defaults on first open.

**Why.** Those files are large, version-specific serialised assets. Writing them by hand outside the
editor risks committing a project that will not open. The owner sets Product Name and Company Name
once and commits what the editor produced; see `client-unity/README.md`.

## TD-011 — `/health` always answers 200; the body carries the verdict

**Decision.** `/health` returns HTTP 200 with a `status` field and a per-dependency breakdown.
`/health/live` touches nothing.

**Why.** The Unity diagnostic surface has to distinguish "nothing is listening" from "the server is
up but its database is not" — three states, not two. A non-200 health response collapses the last two
into a transport failure with no readable body.

## TD-012 — Config editing is read-only until Milestone 1

**Decision.** `GET /api/dev/config` serves the balance files read-only. No write path exists yet.

**Why.** Editing balance without validation, config versioning and an audit trail would let an
invalid or untraceable balance reach the game. Those three arrive together in `M1-T06` to `M1-T08`.
The console states this on the Game Config page rather than showing disabled controls.

## TD-013 — Dev endpoints are unauthenticated and localhost-only for now

**Decision.** `/api/dev/*` has no authentication in Milestone 0. The stack binds to localhost and
must not be exposed publicly.

**Why.** Authentication and admin role separation are `M1-T01` and `M1-T08`. Writing a throwaway
auth scheme now would either be replaced immediately or become the thing nobody revisits. The
constraint is recorded in `docs/SECURITY.md` and in the endpoint's own comments.

**Revisit if.** The console needs to be reachable from anywhere but the owner's machine. Then
`M1-T01` must land first.
