# Roadmap

**`docs/roadmap.json` is the source of truth.** This file explains how it works and what each
milestone means. If the two ever disagree, the JSON is right.

## How status works

Every task carries exactly one status:

| Status | Means |
|---|---|
| `TODO` | Not started |
| `IN_PROGRESS` | Being worked on right now |
| `DONE` | Finished, with `completion_notes` saying what was delivered |
| `BLOCKED` | Cannot proceed because of a technical dependency |
| `NEEDS_OWNER_DECISION` | Cannot proceed until the owner decides something |

A task moves to `DONE` **in the same change that completes the work** — never as a separate claim of
progress. The panel reflects repository state, not background activity.

Anything marked `NEEDS_OWNER_DECISION` also appears in the `owner_decisions` array with the full
question, and the Dashboard surfaces both. A test enforces that pairing
(`RepositoryRoadmapIntegrityTests`), so a decision cannot be raised on a task and then lost.

## How the console reads it

```
docs/roadmap.json
      │
      ├── GET /api/dev/roadmap   (server validates, computes the summary)
      │         │
      │         └── Development Console  ← normal path
      │
      └── read directly by the console  ← fallback when the API is down
```

The console labels which source answered. See `docs/DECISIONS.md` TD-005 and TD-006.

## Milestones

| # | Title | Delivers |
|---|---|---|
| **M0** | Repository / workflow foundation | Monorepo, Unity shell, server shell, PostgreSQL + migrations, Docker Compose, config files, docs, roadmap, Dev Console shell, versioning |
| **M1** | Accounts / player state / config pipeline | Dev auth, profile, wallet, progression, config loading and versioning, admin config editing, audit log, idempotency foundation |
| **M2** | Fishing vertical loop | Map 1 scene, authoritative 30s cycle, catch generation, Fisher XP, Fishing Box, offline fishing, catch feedback, notifications |
| **M3** | Aquarium / fish identity / feeding | Persistent FishInstance, 100 cap, deterministic stats, fish Lv.1–10, feeding with 50% XP recovery, NPC sale, Encyclopedia tracking |
| **M4** | Profile / Cardume / Inventory / Rods | Own and public Profile with field separation, rod slot, Inventory, Cardume 1–6 with the 6/6 bonus, Starter Rod, Rod 1 and upgrades, Shells, private Strength |
| **M5** | Map 2 / travel / content | Map menu, level unlocks, rod minimums, 30s travel, Rio Selvagem, all 20 species, Rare eligibility |
| **M6** | Expeditions | Four durations, Recommended Strength, efficiency curves, Coin and rare-fish rewards, offline completion, activity locks |
| **M7** | Arena / PvP | Energy, opponent selection with one reroll, combat engine with micro-RNG, 3+3 formation, atomic rank swap, Honor, replay, history, Arena Shop |
| **M8** | Fixed-price Market | Listing rules, state locks, Items to Withdraw, Aquarium-full purchases, filters, audit |
| **M9** | Auction | 6h auctions, bid rules with reserved principal, race safety, anti-sniping, early close, no-bid expiry |
| **M10** | Tutorial / taskbar / UX polish | Teach-by-doing tutorial, compact mode, catch effects, toasts, audio placeholders, transitions, optimization |
| **M11** | Public website / direct distribution | Landing page, media placeholders, Windows download, version and release notes, build metadata endpoint, Steam placeholder |
| **M12** | V0.1 stabilization | Combat and economy simulation, security tests, load smoke tests, reconnect and race tests, bug and balance passes, Windows build, release notes |

## Rules the roadmap enforces on itself

1. **Milestone 0 must be working before Milestone 1 starts.** The owner has to be able to see the
   panel first.
2. **Completion criteria are part of the milestone, not a nicety.** A milestone is not done because
   its tasks are ticked; it is done when its criteria hold.
3. **Nothing outside V0.1 scope gets a task.** If something seems necessary and is not in the GDD's
   V0.1 list, it becomes a `NEEDS_OWNER_DECISION`, not an extra task.
