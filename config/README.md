# /config — data-driven balance

Every value the game balances on lives here, never in gameplay code (GDD rules 2 and 43).

| File | Owns |
|---|---|
| `fish_catalog.json` | Species-intrinsic data: rarity, size range, base stats, sale value, feed XP, fisher XP |
| `maps.json` | Maps, per-map fish pools with catch weights, unlock level, minimum rod tier, travel rules |
| `progression.json` | Fisher XP curve, fish XP curve, size distribution, rarity/size/level modifier rules, feeding rules, fishing intervals |
| `rods.json` | Rod definitions, internal level bonuses, upgrade costs, resale rules |
| `economy.json` | Currencies, NPC sale formula, Shell rates, Market and Auction values, Aquarium cap |
| `arena.json` | Energy, opponent selection, Honor, combat constants, formation, Cardume rules, Strength metric, Arena Shop |
| `expeditions.json` | Durations, Recommended Strength, efficiency curves, rewards |

## Rules

- **Catch weights live in `maps.json`, not `fish_catalog.json`.** A pool is a property of a map. `fish_catalog.json` holds only what is true about a species regardless of where it is caught, so a species appearing on two maps is never duplicated.
- **`balance_status: "PROVISIONAL"`** in every file means the numbers are placeholder-balanced with a clear hierarchy, to be tuned through simulation and the Development Console (GDD section 46). The *structure* is not provisional.
- **Base stats are stated at level 1, size percentile 0.50, before rarity, size and level modifiers.** The server applies those modifiers using `progression.json`; it never stores derived stats redundantly.
- Every file carries `config_schema_version`. Bump it when a file's shape changes, not when a number changes.

## Loading

From Milestone 1 the server loads these files into a `GameConfigVersion` record so any outcome can be traced back to the balance version that produced it. Until then the server serves them read-only at `GET /api/dev/config`.

## Provisional calibration recorded

- Fisher XP 1 → 10 totals **2000 XP**, targeting ~240 Map 1 catches (~2 hours of online fishing).
- Fisher XP 1 → 100 totals **711,000 XP** against a ~90 day target. This is the value most likely to need recalibration once real play data exists.
- Fish XP 1 → 10 totals **995 XP**.
- Rod 1 costs **2500** Coins to buy and **138,050** Coins to take from internal level 1 to 10.
