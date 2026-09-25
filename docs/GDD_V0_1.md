# FISHING IDLE — V0.1
## Game Design Document + Technical Specification for Claude

**Target engine:** Unity 6.3 LTS / C#  
**Game style:** relaxing 2.5D idle collectible fishing game with asynchronous PvP  
**Primary distribution:** direct download from official website + Steam  
**Architecture principle:** server-authoritative for every action with economic, progression or competitive value  
**Status:** V0.1 vertical slice — this is a small version of the real game, not a disposable prototype.

---

# 0. HOW CLAUDE SHOULD USE THIS DOCUMENT

You are the technical executor for this project. The game designer has already made the structural decisions in this document. Your job is to implement them faithfully, keep systems data-driven, and surface technical issues without inventing additional gameplay systems.

## Non-negotiable working rules

1. **Do not invent new mechanics to “improve” the game.** Simplicity is intentional.
2. **Do not hardcode balance values that can reasonably live in data/config.** Fish, maps, rods, XP, prices, chances, timers, rarity weights, size ranges, stat multipliers and fees must be editable.
3. **Client is not authoritative.** Unity displays intent and presentation. The server validates, calculates, persists and returns authoritative results.
4. **Every security-sensitive action must be server-side.** Assume modified clients and bots exist.
5. **Keep V0.1 small but complete.** Do not build future systems unless explicitly listed as V0.1.
6. **When a task is finished, update the Development Console roadmap/status.** The owner wants to see what has been completed, what is in progress, what is blocked and what is next.
7. **Do not silently change design decisions.** If a technical limitation requires a design change, log it as `Needs Owner Decision` in the Development Console.
8. **Prefer clean, maintainable code over clever code.** The owner is not a programmer and must be able to continue the project with AI assistance and future developers.
9. **All critical mutations need idempotency/transaction protection.** Duplicate requests must not duplicate coins, XP, fish, market sales, bids or rewards.
10. **The game must remain visually pleasant even when the player is doing nothing.** Visual presentation is part of the gameplay.

---

# 1. PRODUCT VISION

The game is a relaxing idle fishing game where the player automatically fishes, collects individual fish, improves selected fish, forms a six-fish team called a **Cardume**, participates in asynchronous PvP, sends the Cardume on Expeditions, and trades fish/items in a player-driven market.

The experience has two complementary pillars:

- **Relaxing collection / idle progression:** beautiful fishing scenes, real-looking fish size variation, collection, aquarium management, passive/offline fishing, expeditions.
- **Competitive progression:** Cardume composition/order, asynchronous Arena rank, Honor economy, market, rare catches capable of disrupting the competitive hierarchy.

Luck matters. A player can gain a major competitive upgrade by catching an exceptionally good fish. Arena consistency and progression matter, but the top player should never become permanently untouchable simply because they are already on top.

The game should feel simple to play even though the backend is secure and robust.

---

# 2. V0.1 SCOPE

V0.1 must contain a playable vertical slice of the real game.

## Required in V0.1

- Unity 2.5D fishing scene
- 2 maps
- 20 fish species (10 per map)
- Automatic online fishing
- Automatic offline fishing
- Fishing Box
- Aquarium (fixed 100 fish)
- Fish level 1–10
- Feeding fish to fish for XP
- One Cardume per player, 1–6 fish
- Complete Cardume bonus (6/6)
- Asynchronous Arena PvP
- Ranking, Energy, Honor, PvP history
- Cardume Expeditions
- Normal player market (fixed price)
- Auction tab
- Profile
- Inventory inside Profile
- Encyclopedia inside Profile
- Starter rod + Rod 1
- Rod upgrades 1–10 for purchasable rods
- Main coin currency
- Shells as an earnable future-progression resource
- Basic tutorial
- Taskbar / compact fishing mode
- Server-authoritative backend
- Internal Development Console / Admin Portal
- Public website for game presentation and direct download
- Build/version pipeline designed so Steam distribution can be added cleanly

## Explicitly out of V0.1

- Fish skills/abilities
- Healers/support classes
- Fish equipment/armor
- Fisher equipment beyond the fishing rod
- Equipment refining system
- PvP seasons
- Battle pass
- Large social/guild systems
- Maps 3–10 as playable content
- Full monetization implementation
- Endgame systems
- Advanced events/tournaments
- More rarity tiers unless explicitly added later

Do not add these during V0.1 “because they would be useful.”

---

# 3. TECHNOLOGY DIRECTION

## Client

- Unity 6.3 LTS
- C#
- 2.5D presentation: layered 2D/3D, parallax, animated water, lighting, particles and UI overlays
- Client is presentation + player intent only

## Backend — recommended V0.1 stack

Use a straightforward server-authoritative stack:

- **ASP.NET Core Web API** (C#)
- **PostgreSQL** for persistent relational data
- **Redis optional** for short-lived cache/rate-limit/session acceleration; do not make Redis mandatory until it provides measurable value
- **Docker Compose** for local development
- Structured logs
- Migration-based schema management

Rationale: shared C# ecosystem with Unity, mature server stack, strong transactional support, easy to hand to future developers.

## Web products

Two separate web surfaces:

1. **Development Console / Admin Portal** — private/internal
2. **Public Game Website** — player-facing

Recommended web stack:

- Next.js + TypeScript for both sites, or a single Next.js project with separated authenticated admin routes and public routes.
- The game backend remains the source of truth; admin tools call authenticated backend/admin APIs.

Production hosting provider is intentionally not locked in V0.1. Local/dev architecture must be containerized and portable.

---

# 4. REPOSITORY STRUCTURE

Recommended monorepo:

```text
/fishing-idle
  /client-unity
  /server
  /web
    /public-site
    /dev-console
  /shared-contracts
  /config
    fish_catalog.json
    maps.json
    rods.json
    progression.json
    economy.json
    arena.json
    expeditions.json
  /docs
    GDD_V0_1.md
    DECISIONS.md
    ROADMAP.md
    CHANGELOG.md
    API.md
    SECURITY.md
  /ops
    docker-compose.yml
    scripts/
```

If technical constraints justify a different directory layout, keep the conceptual separation.

---

# 5. DEVELOPMENT CONSOLE / ADMIN PORTAL

This is part of the project workflow from the beginning. The owner wants to feel like Claude is working for him and reporting progress through a visual panel.

## Purpose

The Development Console is a private website that shows:

- current V0.1 roadmap
- completed tasks
- in-progress tasks
- blocked tasks
- next tasks
- current build/version
- recent development changes
- editable game configuration
- server health/log summaries during development
- content tables such as fish/maps/rods

## Required V0.1 sections

### Dashboard

Show:

- V0.1 completion percentage based on completed roadmap tasks
- current milestone
- current build version
- most recently completed tasks
- current in-progress task
- blockers / owner decisions needed
- next 5 queued tasks

### Roadmap

Statuses:

- `TODO`
- `IN_PROGRESS`
- `DONE`
- `BLOCKED`
- `NEEDS_OWNER_DECISION`

Each roadmap item includes:

- id
- title
- subsystem
- description
- status
- dependencies
- completion notes
- updated_at

Claude must update roadmap metadata whenever it completes meaningful work.

### Game Config

Editable configuration for:

- fish species
- rarity
- fish base stats
- size ranges
- catch weights
- sale values
- feed XP
- maps and fish pools
- rod bonuses and upgrade costs
- XP progression
- fishing timers
- Arena values
- Market fees
- Auction values
- Expedition values

Use validation. Invalid values must not be deployable.

### Content Browser

Visual tables/cards for:

- Fish
- Maps
- Rods
- Expeditions

### Build / Version

Show:

- client version
- backend version
- web version
- build date
- environment
- changelog
- direct link to latest internal Windows build when available

### Logs / Audit

Development-friendly views for:

- server errors
- suspicious/rejected requests
- economy mutations
- admin configuration changes
- market/auction transaction audit

Do not expose secrets/tokens in UI.

## How Claude updates this panel

Do not attempt magical “AI background status.” The panel reflects **repository/project state**.

Maintain a machine-readable file such as:

`/docs/roadmap.json`

and/or a database table seeded from it. Claude must update task status as part of each implementation change. The Development Console reads the same source and displays it.

This makes progress auditable and independent of one chat session.

---

# 6. PUBLIC GAME WEBSITE

The game will be distributed outside Steam as well as on Steam.

## V0.1 public-site requirements

- attractive landing page matching the game visual identity
- hero section with fishing scene/game branding
- short explanation of the game
- screenshots/media section placeholder
- `Download for Windows` button once builds are available
- Steam button/link placeholder until Steam page exists
- version + release notes
- minimum requirements placeholder
- privacy/terms placeholders
- status/support/contact links can be added later

## Distribution model

Direct-download build and Steam build should come from the same game codebase.

Do not implement a complex custom launcher in the first milestone unless auto-updating becomes necessary. For early V0.1, a versioned downloadable installer/build is enough.

Design version APIs so a lightweight launcher/updater can be added later.

---

# 7. CORE PLAYER NAVIGATION

Main game menus:

1. **Fishing**
2. **Map**
3. **Aquarium**
4. **Arena**
5. **Expedition**
6. **Market**
7. **Shop**
8. **Profile**

Secondary/sub-sections:

- Fishing Box → inside Fishing
- Cardume management → inside Profile
- Encyclopedia → inside Profile
- Inventory → inside Profile
- Arena Shop → inside Arena
- Notifications → global bell
- Settings → secondary menu
- Taskbar Mode → global control

Do not add Inventory as a ninth main menu in V0.1.

---

# 8. MAIN FISHING SCENE / VISUAL DIRECTION

## Core composition

The main screen is a living scene, not a dashboard.

- top: thin primary navigation
- left: retractable player/profile summary card
- center: water + boat + fisherman + environment
- minimal fishing-cycle UI overlay
- quick Fishing Box access
- global notifications in non-blocking toasts

## Fishing animation

The fisherman should continuously perform a pleasant visual loop:

1. prepare / idle motion
2. cast
3. wait
4. subtle bite/rod reaction
5. reel
6. actual caught species appears
7. capture feedback
8. store and return to idle/cast

The animation does not need to literally last exactly 30 seconds. It is presentation synchronized to the authoritative server cycle.

## Environmental motion

Use subtle randomized visual-only events:

- boat bobbing
- water motion
- light/wind variation
- small particles
- distant fish jump
- bird pass
- clouds
- vegetation motion if present

No gameplay value. These do not require server authority.

## Important catch feedback

Important catches must visually stand out before the player reads text.

Possible effects:

- soft glow/aura
- pulsing light
- subtle particles
- stronger presentation for Exceptional/new/rare fish

Keep elegant, not visually exhausting.

## Design rule

If almost all UI is hidden, the fishing scene should still be pleasant to leave open on a second monitor.

---

# 9. TASKBAR / COMPACT MODE

Player can switch to a compact window intended to remain visible while working.

Show only:

- water
- small boat
- fisherman
- fishing animation
- minimal catch feedback

The backend treats this exactly as normal online fishing. Taskbar Mode is purely presentation.

---

# 10. FISHING — AUTHORITATIVE LOOP

## Online

- 1 catch every **30 seconds**
- Player must explicitly start fishing
- Once started, fishing continues while browsing other in-game menus as long as the application is open and fishing has not been stopped by a state that explicitly pauses it (e.g. map travel)

## Offline

- 1 catch every **60 seconds**
- maximum accumulated offline duration: **24 hours**
- after 24 hours, no further offline catches accumulate until the player returns
- on login/reconnect, server calculates eligible cycles using timestamps

## Server authority

The client must never say “I caught fish X.”

Client intent is closer to:

- start fishing
- stop fishing
- synchronize results/state

The server owns:

- active fishing session
- map
- equipped rod
- server timestamp
- eligible cycle count
- RNG
- species
- rarity
- size
- Fisher XP
- Shell drop
- resulting Fishing Box state

Do not run a heavy per-player timer when unnecessary. Use server timestamps/cycle indices and process eligible cycles efficiently.

## Idempotency

Persist a session/cycle cursor so reconnects or repeated requests cannot process the same fishing cycle twice.

---

# 11. FISHING BOX

All catches enter the Fishing Box first.

Fishing Box is separate from Aquarium.

## Purpose

A high-volume catch buffer where fish have not yet become full persistent owned entities.

## Technical storage rule

**Catching a fish does not create a full `FishInstance`.**

Fishing Box catch representation should be compact, for example:

- catch_id / compact id
- species_id
- size encoded efficiently
- required flags only

Values derivable from species/config should not be duplicated per capture unless there is a strong persistence reason.

Use batching/checkpointing where safe.

Offline catches are generated in aggregate on return, then represented compactly.

## Player actions

From Fishing Box player can:

- keep → creates full persistent FishInstance in Aquarium if slot available
- sell to game
- select as feed source where applicable

## Filtering

Keep V0.1 simple: one primary filter at a time.

Examples:

- All
- Common
- Rare
- Small
- Adult
- Large
- Exceptional

Bulk actions are allowed.

## Valuable-item confirmation

When bulk action includes important fish (rare / Exceptional / otherwise protected by rules), show one clear confirmation with `Review fish` and `Confirm`.

No typed confirmations.

---

# 12. AQUARIUM

Aquarium is persistent owned-fish storage.

## Hard capacity

**100 fish maximum. Permanent fixed limit in current design.**

No Aquarium expansion system.

If 100/100:

- Fishing Box continues receiving catches
- player cannot move another fish into Aquarium until space is freed

## Aquarium UX

Visual card inventory, not spreadsheet-first.

Card minimum data:

- large fish art
- species name
- size/category
- level
- rarity styling

Click opens full fish detail.

Default ordering:

1. Exceptional
2. Large
3. Adult
4. Small

Within category: larger specimens first.

---

# 13. FISH IDENTITY AND PERSISTENCE

A fish becomes a full persistent entity when kept in Aquarium or when ownership requires an individual persistent fish.

Persistent fish retains identity permanently until consumed/destroyed.

Changing owner through Market does not reset anything.

Persisted fish data should include at minimum:

- fish_instance_id
- owner_id
- species_id
- rarity (if individual rarity is part of species/catch state)
- exact size
- size category/percentile
- level
- XP
- ownership/state fields
- created/caught metadata needed for audit

Stats should be derived from deterministic rules when possible rather than stored redundantly.

## No hidden IV/genetics

Two fish with the same:

- species
- rarity
- exact size
- level

must have the same combat attributes.

No random hidden Attack/Defense rolls.

---

# 14. SIZE SYSTEM

Size is individual and permanent.

Use realistic plausible species-relative ranges.

Categories:

- Small — initial distribution target 20%
- Adult — 60%
- Large — 19%
- Exceptional — 1%

Category thresholds and exact size distribution are data-driven.

Size influences:

- desirability
- sale value
- combat attributes modestly
- Fisher XP slightly
- feed XP modestly

Visual UI should show both number and visual representation where useful, e.g. `250 / 300 cm` plus progress bar.

Fish rendered size may visually reflect specimen size when practical.

---

# 15. RARITY

V0.1 contains:

- 19 Common fish species
- 1 Rare fish species

Do not define additional named rarity tiers yet.

Rarity is a power hierarchy: comparable fish in a higher rarity band should generally be stronger.

Rarity is distinct from size category. A Common fish can be Exceptional-sized.

Starter rod cannot catch Rare fish.

---

# 16. INITIAL FISH CATALOG — V0.1

These species/map assignments are the V0.1 content baseline.

## Map 1 — Lago Sereno

All Common.

1. Lambari
2. Tilápia
3. Carpa
4. Piau
5. Traíra
6. Pacu
7. Cascudo
8. Curimbatá
9. Matrinxã
10. Tambaqui

## Map 2 — Rio Selvagem

Common unless stated.

1. Tucunaré
2. Piranha
3. Dourado
4. Pintado
5. Cachara
6. Jaú
7. Peixe-cachorra
8. Piracanjuba
9. Pirarucu
10. Aruanã — **Rare**

## Content-data instruction

Do not embed these directly in gameplay code. Seed them through catalog data.

Initial exact base stats, catch weights, real-size ranges and values are **provisional balance data** and must be editable in Development Console without code changes.

For first playable build, create reasonable placeholder-balanced values with clear hierarchy and then tune through simulation/playtesting.

---

# 17. FISHER XP / MAP PROGRESSION

Max Fisher level target: **100**.

Long-term total progression target: roughly 90 days to Lv.100 under normal play assumptions, to be calibrated later.

Map unlock baseline:

- Map 1: start
- Map 2: Lv.10
- Map 3: Lv.20
- Map 4: Lv.30
- Map 5: Lv.40
- Map 6: Lv.50
- Map 7: Lv.60
- Map 8: Lv.70
- Map 9: Lv.80
- Map 10: Lv.90

Only Maps 1–2 exist in V0.1.

Initial target: Lv.1→10 around **2 hours of online fishing** on average (~240 catches).

XP primarily varies by species + rarity.

Size XP multipliers initial configurable values:

- Small ×1.00
- Adult ×1.02
- Large ×1.05
- Exceptional ×1.10

Luck may legitimately accelerate progression.

---

# 18. MAPS AND TRAVEL

## Map 1 — Lago Sereno

Visual theme:

- calm freshwater lake
- vegetation
- soft water motion
- warm relaxing light
- safe starter atmosphere

## Map 2 — Rio Selvagem

Visual theme:

- larger river
- visible current
- rocks
- denser vegetation
- waterfall/mist possibilities
- deeper/darker water
- still relaxing, but clearly more adventurous

## Travel

- player opens Map menu
- selects unlocked/eligible map
- manually presses Travel
- travel does not happen automatically on level-up
- visual boat transition duration: **30 seconds**
- fishing pauses during travel
- no fuel, travel fee or cooldown
- new map becomes active on arrival

Maps are places, not automatic level states.

---

# 19. RODS

Long-term purchasable rods: 5, plus Starter Rod (Tier 0).

## Tier unlock / minimum map compatibility

- Map 1 → Starter Rod allowed
- Maps 2–3 → Rod 1 minimum
- Maps 4–5 → Rod 2 minimum
- Maps 6–7 → Rod 3 minimum
- Maps 8–9 → Rod 4 minimum
- Map 10 → Rod 5 minimum

Purchasable rod availability milestones:

- Rod 1 → Fisher Lv.10
- Rod 2 → Lv.30
- Rod 3 → Lv.50
- Rod 4 → Lv.70
- Rod 5 → Lv.90

V0.1 implements Starter Rod + Rod 1.

## Starter Rod

- free
- Tier 0
- no bonuses
- cannot catch Rare fish
- does not generate Shells

## Purchasable rod internal levels

Each purchasable rod: **Lv.1–10**.

Upgrade by paying Coins to buy the next level directly.

- no XP bar
- no failure chance
- no extra material in V0.1
- each level costs more
- later rod tiers cost more to upgrade than earlier rods

## Rod bonus axes

Only fishing-related bonuses:

1. improved chance toward higher available rarity
2. improved chance toward better size outcomes
3. increased Shell yield/drop

Bonuses should be modest. Small percentage advantages compound over thousands of catches.

Important: rarity bonus only works within rarities available on that map/rod eligibility. It never creates a rarity that the map does not support.

Exact bonus math is configurable and should preserve rarity scarcity. A +5% “rarity efficiency” must not naïvely turn a 1% chance into 6%.

## Rod persistence / trading

Old rods remain owned.

Player may:

- keep
- re-equip
- sell on Market
- sell back to game/NPC for partial value
- destroy

NPC resale value should include part of historical upgrade investment.

Market-traded rod retains tier, internal level and bonuses.

---

# 20. INVENTORY / EQUIPMENT

Inventory is separate from fish systems and lives inside Profile in V0.1.

## V0.1 Equipment

Only one active equipment slot exists:

- Fishing Rod

Future conceptual slots exist but must NOT be implemented now:

- hat
- vest
- gloves
- boots
- pants
- necklace
- ring

Do not show dead/locked slots just to fill UI.

Profile sections, own-player view:

1. Equipment
2. Inventory
3. Cardume
4. Encyclopedia
5. Personal highlights

---

# 21. SHELLS

Shells exist in V0.1 as a fishing-earned resource.

- primarily dropped by fishing
- Starter Rod does not generate them
- better rods can modestly improve Shell acquisition

Current uses are intentionally NOT implemented yet.

Future intended directions:

- refining equipment
- upgrading equipment
- other progression uses that make sense later

Do not use Shells to upgrade rods or fish in V0.1.

---

# 22. FISH LEVEL / FEEDING

Fish start Lv.1.

Max fish level: **Lv.10**.

No player-distributed stat points.

Level applies a percentage bonus to deterministic fish attributes.

## Feeding

Player opens fish in Aquarium → Feed.

Feed fish are consumed permanently.

Feed XP depends on:

- species base feed XP
- rarity influence
- size influence
- plus recovered invested XP if consuming an already-leveled fish

If a fish already had XP invested, sacrificing it transfers **50% of previously invested XP** plus its normal feed value.

No lossless XP recycling.

UI should show:

- current XP
- XP to next level
- selected feed XP
- resulting level(s)

Warnings for consuming valuable fish.

If food fish belongs to Cardume, show a simple confirmation; on confirm remove from Cardume and consume, unless activity lock applies.

---

# 23. CARDUME

Exactly one Cardume per player.

- 1–6 fish
- fish are references to owned Aquarium FishInstances
- same Cardume is used for Arena, Expeditions and future Cardume activities
- no separate offense/defense teams

## Complete Cardume bonus

If exactly 6/6 slots are filled:

- +3% HP
- +3% Attack
- +3% Defense
- +3% Speed

This applies in **all activities using Cardume**.

It is dynamic, not permanently written to fish.

1–5 fish: no complete-team bonus.

No artificial composition bonus such as “3 tanks + 3 attackers.”

---

# 24. COMBAT STATS

Only four combat attributes in V0.1:

- HP / Vida
- Attack / Ataque
- Defense / Defesa
- Speed / Velocidade

Do not add Crit, Dodge, Accuracy, Penetration, Luck stat or abilities.

## Attribute origin

Stats derive deterministically from:

- species
- rarity
- size
- level

No hidden RNG.

## Speed

Speed controls:

- time until first attack from t=0
- interval between subsequent attacks

Speed differences should be relatively narrow so Speed does not dominate balance.

## Defense

Use diminishing returns, not 1 Attack − 1 Defense subtraction.

Initial implementation should use configurable constants, e.g. a mitigation-style formula. Keep minimum damage so defense cannot create immunity.

## Offensive bias

Combat should moderately favor offense to keep battles moving.

Attack/HP/Defense are the main balance axes. Speed is secondary refinement.

---

# 25. COMBAT MICRO-RNG

Combat is not perfectly deterministic.

Use **small damage variation only**.

No crit/dodge systems.

Initial recommended configurable variation:

`damage_roll_multiplier ∈ [0.97, 1.03]`

This is intentionally small.

Stats decide the fight; luck slightly perturbs close fights.

Server generates all combat RNG when resolving the battle.

---

# 26. PVP FORMATION

Visual formation is a compact mirrored **3 front + 3 back** layout.

Positions:

Front line:

- 1
- 2
- 3

Back line:

- 4
- 5
- 6

Front/back depth difference should be small but visually obvious.

Target priority is fixed:

`1 → 2 → 3 → 4 → 5 → 6`

When a fish reaches 0 HP:

- it is defeated/removed visually
- its slot remains empty
- formation does NOT recompute/reposition
- targeting proceeds to next living position in priority order

This intentionally lets players put defensive fish earlier and protected offensive fish later.

No extra formation mechanics.

---

# 27. PVP VISUAL REPLAY

Backend resolves the full battle immediately when attack is accepted.

Client receives authoritative battle result + replay event data.

Replay options:

- 1x
- 2x
- Skip to result

Target battle duration: most battles should visually finish within roughly **60 seconds at 1x**, but there is no hard battle timeout.

If normal fights consistently exceed target, tune damage/HP/defense rather than adding timeout rules.

## Attack animation

Keep simple:

- tiny forward movement/tic
- simple species-neutral water/splash attack effect for V0.1
- hit impact
- damage number
- HP bar decreases

No long traversal across screen.

---

# 28. ARENA — CORE RULES

Asynchronous PvP.

Fishing continues while player uses Arena.

## Energy

- max Energy: **24**
- regeneration: **1 Energy per hour**
- every initiated attack consumes **1 Energy**, win or lose

No extra defeat penalty beyond Honor loss defined below.

## Opponent selection

When player needs a new opponent set:

- server selects 3 unique opponents
- only from a window up to roughly **10% above current rank**
- player cannot target themselves
- one reroll allowed per opponent set
- after reroll, set is locked
- leaving Arena/restarting client does not reroll
- new set becomes available only after player performs an attack

All selection state is server-side.

Near rank #1, use sensible rounding/valid candidate logic.

## Arena opponent cards

Show:

- player name
- rank
- Cardume fish
- each fish level
- each fish rarity

Do NOT show:

- Cardume Strength
- predicted win percentage
- easy/hard label

Player can open public profile for deeper inspection.

---

# 29. ARENA RANKING

Ranking is positional.

If attacker at #1000 defeats #900:

- attacker becomes #900
- defender becomes #1000
- positions #901–#999 do not shift

It is a direct swap.

If attacker loses:

- rank unchanged

Incoming attacks can move player while they are offline.

---

# 30. HONOR

Honor is Arena-earned currency/score used in Arena Shop.

It can provide real gameplay progression; it is not cosmetic-only.

Rules:

- attacker victory → gains Honor
- successful defense → gains Honor
- PvP defeat → loses a small amount of Honor
- loss amount is much smaller than win gain
- Honor cannot go below 0

Exact values are configurable.

Do not artificially cap Honor gained from successful defenses in V0.1.

Arena Shop contents can influence gameplay, but avoid implementing future item systems only to populate it. Seed a minimal configurable shop inventory and expand later.

---

# 31. PRIVATE CARDUME STRENGTH METRIC

Cardume Strength is an internal/player-private summary metric used mainly for Expeditions and own-profile feedback.

It is NOT shown to opponents in Arena or on public profile.

Initial fish-strength formula:

`FishStrengthRaw = Attack×2 + Defense×1.5 + HP÷10 + Speed×0.5`

Cardume raw strength = sum of participating fish after active modifiers such as the 6/6 +3% bonus.

Apply a configurable display scaling factor so values live in a readable range.

This formula does NOT determine PvP outcome.

Later calibrate weights using combat simulation.

---

# 32. EXPEDITIONS

Cardume can be sent on one Expedition activity.

Available durations:

- 30 minutes
- 1 hour
- 3 hours
- 6 hours

Works online or offline.

Fishing continues normally.

## Activity lock

While Cardume is on Expedition:

Owner cannot:

- initiate Arena attack
- change formation
- feed/sell/list participating fish
- start another Cardume activity

Other players **can still attack this player asynchronously** in Arena using the same Cardume state.

No separate defense Cardume.

## Recommended Strength

Each Expedition has a Recommended Strength.

- below recommended → reduced reward efficiency
- at recommended → 100% base reward
- above recommended → bonus reward
- superiority bonus may have a configurable cap

No fish death/loss risk.

## Rewards

V0.1:

- Coins (moderate, guaranteed)
- very small chance to find fish

No XP.

No Shell reward.

Found fish goes to Fishing Box.

Expeditions are complementary, not a replacement for fishing.

## Completion presentation

If online: small completion popup/toast → open reward result.

If offline: pending completion shown on next login.

Show found fish prominently when one exists.

---

# 33. NORMAL MARKET — FIXED PRICE

Market style: inspired by clear MMO marketplaces such as Albion, but heavily simplified for an idle game and visually card-based.

Tabs:

- Buy
- Sell
- My Listings
- Auction
- Items to Withdraw

## Fixed-price rules

- max active listings per player: **5**
- max listing duration: **7 days**
- no listing fee
- seller chooses any price
- sale fee: **3% on completed fixed-price sale**
- no artificial minimum market value
- fixed-price immediate purchase

## Listing state

Listed fish/item is unavailable for use while listed.

If fixed-price listing is:

- purchased → buyer receives item in Items to Withdraw
- cancelled → seller receives item in Items to Withdraw
- expired → seller receives item in Items to Withdraw

Nothing returns directly to Aquarium/Inventory from Market.

Seller proceeds from completed fixed-price sale are deposited automatically after 3% fee.

## Items to Withdraw

Unified Market custody area.

Fish/item in withdrawal:

- belongs to player
- does not occupy Aquarium/Inventory usable slot as applicable
- cannot be used/traded/fed/equipped until withdrawn
- retains all data
- has no expiration in V0.1

If Aquarium is full, fish purchase is still allowed. Withdrawal to Aquarium is blocked until player frees a slot.

Market storage limits must not block commerce.

---

# 34. MARKET FILTERS / UX

Buy search may use multiple simultaneous filters because search is core.

Fish filters may include:

- species
- rarity
- size category
- min/max size
- level
- min/max price

Sort examples:

- lowest price
- highest price
- largest size
- smallest size
- newest

Results should be visual compact cards, not a raw spreadsheet.

Click card → detail side panel → Buy.

---

# 35. AUCTION

Auction is a separate Market tab.

## Seller rules

- maximum **1 active auction per seller**
- supports fish or items
- duration fixed at **6 hours**
- seller defines starting/minimum bid
- once submitted, seller cannot cancel simply because they dislike demand

If auction receives no bid:

- seller must wait full 6 hours
- item goes to Items to Withdraw after expiry

If auction receives at least one valid bid:

- highest bid at completion wins
- seller may optionally `End Now` and accept current highest bid
- early end charges seller **3% of final/current bid value**
- seller receives 97% when early-ending

At normal timed auction completion, do not apply the 3% early-end fee.

## Bid rules

Each bid:

- must be at least **3% higher** than current highest bid
- bidder pays a **1% non-refundable bid fee** every time they place a bid
- full bid principal is reserved/locked server-side while bidder is highest bidder
- if outbid, principal is returned immediately
- 1% fee is never returned

Example:

- current bid = 10,000
- next minimum = 10,300
- bidder places 10,300
- bid fee = 103
- 10,300 principal becomes reserved
- if outbid, 10,300 returns; 103 remains burned

No artificial limit on number of auctions a buyer can participate in; available balance naturally limits participation.

## Anti-sniping

If a valid new bid arrives with less than **1 minute** remaining:

- remaining time resets to **1:00**

This may repeat during continued bidding.

## Server safety

Bid placement and auction closing must be transactional/locked to prevent race conditions between:

- simultaneous bids
- timeout completion
- seller early-end action

---

# 36. SELLING FISH TO GAME / NPC

Fish system sale value is based primarily on:

- species base value
- exact size
- rarity influence as configured

Size price changes continuously, not just by category.

NPC sale does not reimburse invested fish XP.

A leveled fish can command extra value on player Market, not necessarily from NPC.

---

# 37. PROFILE

Profile is both identity/showcase and own-player management surface.

## Own Profile

Show:

- player name/avatar
- Fisher level
- current Arena rank
- private Cardume Strength
- Equipment section
- Inventory section
- Cardume section
- Encyclopedia section
- personal records/highlights

## Public Profile

Other players may see:

- player name
- Fisher level
- current PvP rank
- Cardume and fish details
- Encyclopedia/discovery progress
- personal records

Do NOT expose:

- Cardume Strength
- Coin balance
- Shell balance
- Fishing Box
- Inventory contents
- Aquarium contents as a whole
- private economic/market operational data

Aquarium itself remains private.

---

# 38. ENCYCLOPEDIA

Lives inside Profile.

One global Encyclopedia, not one per map.

Persistent historical discovery.

Before discovery:

- fully dark silhouette
- no species name/details

After first catch:

- reveal species
- retain discovery forever even if all owned copies are sold/fed

Track per species:

- discovered status
- largest specimen ever caught by that player

Record persists even if fish is no longer owned.

---

# 39. NOTIFICATIONS

Use non-blocking toasts and a global Notification Center/bell.

Relevant examples:

- new species
- new personal record
- Exceptional catch
- Market sale
- outbid auction
- auction won
- Expedition complete
- incoming PvP/result
- game update announcements

Do not persist infinite history. Keep a recent bounded set (e.g. 30–50 relevant entries) unless later changed.

Many normal catch toasts can be generated client-side from authoritative response data; do not send separate server request for every visual notification.

---

# 40. TUTORIAL

Keep short. Teach by doing.

Suggested flow:

1. welcome in first map
2. open Shop
3. claim Starter Rod for free
4. auto-equip Starter Rod
5. start fishing
6. receive first catch
7. inspect basic fish information
8. open Fishing Box
9. sell a fish to game and receive Coins
10. keep a fish in Aquarium
11. add a fish to Cardume
12. briefly introduce Expedition
13. tutorial ends / player is free

Do not force tutorial explanations of advanced PvP, Market, Auction or future systems.

---

# 41. SERVER-AUTHORITATIVE SECURITY MODEL

Core rule:

**CLIENT = intent + presentation**  
**SERVER = truth + validation + RNG + persistence**

## Never trust client for

- current time
- fishing cycle completion
- caught fish
- fish size/rarity
- XP
- Coins/Shells/Honor/Energy
- rod level
- fish level
- market ownership
- auction bid result
- battle outcome
- Expedition completion/rewards

## Required protections

- authenticated requests
- authorization/ownership checks
- server timestamps
- rate limiting
- idempotency keys on economic mutations
- database transactions
- row/version locking where race conditions matter
- replay/double-spend protection
- server-side RNG
- immutable audit records for important economy/market/admin operations
- admin role separation
- validated config deployment

## Example fishing exploit prevention

Modified client requests 500 cycles instantly → server computes eligible cycle cursor from authoritative session timestamps → rejects/ignores ineligible cycles.

## Example fish double-use prevention

Two simultaneous requests try to sell/feed same FishInstance → transaction/state version allows exactly one valid mutation.

## Example auction race

Two bids arrive together → transaction lock/order produces one authoritative highest bid and returns/rejects appropriately.

---

# 42. DATA MODEL — HIGH LEVEL

Use normalized relational schema where practical.

Suggested major entities:

- UserAccount
- PlayerProfile
- PlayerWallet
- PlayerProgression
- FishingSession
- FishingBoxBatch / FishingBoxCatch storage representation
- FishSpeciesConfig
- FishInstance
- AquariumState
- Cardume
- CardumeSlot
- RodConfig
- RodInstance
- InventoryItem
- MapConfig
- PlayerMapState
- EncyclopediaEntry
- ArenaState
- ArenaOpponentSet
- PvPBattle
- PvPReplayEvent
- PvPHistory
- ExpeditionConfig
- ExpeditionRun
- MarketListing
- MarketWithdrawal
- Auction
- AuctionBid
- Notification
- GameConfigVersion
- AdminAuditLog
- RoadmapTask (or repository-backed equivalent)

Exact schema is Claude's implementation responsibility, but behavior must match this document.

---

# 43. CONFIGURATION / LIVE BALANCE

All these values must be configurable without changing core gameplay code:

- online fishing interval
- offline fishing interval
- offline cap
- fish pools and weights
- rarity modifiers
- size distribution
- species size ranges
- species base stats
- species sale values
- feed XP
- Fisher XP
- fish level bonus curve
- rod bonuses
- rod upgrade costs
- Shell rates
- map unlock levels
- travel duration
- full Cardume bonus
- combat RNG range
- defense constants
- speed interval mapping
- Energy max/regeneration
- opponent window
- Honor gain/loss
- Market listing limit/duration/fee
- Auction duration/min increment/bid fee/anti-snipe/early-close fee
- Expedition durations/strength/rewards

Maintain config versioning so we can identify which balance version produced important outcomes.

---

# 44. UI/UX STYLE RULES

- visual-first, numbers second
- fish artwork is prominent
- avoid spreadsheet UI except search-heavy Market functions
- clear card hierarchy
- minimal permanent HUD
- important progression can use bars where useful
- do not place progress bars on everything
- distinct visual language for species rarity vs size category
- Exceptional fish should look special without being confused with rarity tier
- UI should feel like a polished game, not a web admin dashboard
- Development Console can be dashboard-like; game client should not

---

# 45. V0.1 IMPLEMENTATION ROADMAP FOR CLAUDE

The Development Console must mirror these milestones and their task statuses.

## Milestone 0 — Repository / workflow foundation

Deliver:

- monorepo structure
- Unity project bootstrapped
- ASP.NET Core server bootstrapped
- PostgreSQL + migrations
- local Docker Compose
- shared environment config
- docs folder
- roadmap.json
- base Development Console shell reading roadmap
- versioning convention

Completion criteria:

- developer can launch server + database + web locally
- Unity client can call a health endpoint
- Dev Console displays project/build/roadmap status

## Milestone 1 — Accounts / player state / config pipeline

Deliver:

- basic auth suitable for development
- player profile state
- wallet
- progression
- config loading/versioning
- admin config read/edit workflow
- audit logs for config changes

Completion criteria:

- player can authenticate
- server creates player state
- admin can change safe config value and server uses new validated config version

## Milestone 2 — Fishing vertical loop

Deliver:

- Map 1 scene
- fishing start/stop
- authoritative 30s cycle
- fishing animation loop
- catch generation
- Fisher XP
- Fishing Box compact storage
- offline fishing 60s up to 24h
- important-catch visual feedback
- basic notifications

Completion criteria:

- player can leave fishing running
- modified/repeated requests cannot accelerate catches
- reconnect resolves correct state
- offline rewards work

## Milestone 3 — Aquarium / fish identity / feeding

Deliver:

- keep catch → FishInstance
- 100 Aquarium cap
- visual Aquarium cards
- deterministic stat calculation
- size categories
- fish detail
- fish Lv.1–10
- feeding + 50% invested-XP recovery
- NPC fish sale

Completion criteria:

- fish identity persists correctly
- same deterministic inputs produce same stats
- duplicate consume/sell exploit prevented

## Milestone 4 — Profile / Cardume / Inventory / Rods

Deliver:

- own Profile
- public Profile
- private/public field separation
- Equipment rod slot
- Inventory inside Profile
- Cardume 1–6
- 6/6 +3% modifier
- Starter Rod
- Rod 1
- Rod upgrade 1–10
- rod buy/equip/resale/destroy hooks
- Shell accumulation

Completion criteria:

- Cardume and equipment state persists
- private Strength is not exposed publicly

## Milestone 5 — Map 2 / travel / content

Deliver:

- Map menu
- level-based unlock
- Rod 1 minimum for Map 2
- 30s travel
- Map 2 scene
- all 20 species seeded
- Rare catch eligibility

Completion criteria:

- manual travel only
- Starter Rod cannot fish Map 2
- Map 2 Rare cannot be generated through Starter Rod exploits

## Milestone 6 — Expeditions

Deliver:

- 30m/1h/3h/6h
- Recommended Strength
- efficiency/bounded superiority bonus
- Coins + rare fish-find chance
- offline completion
- activity locks
- completion popup/notification

Completion criteria:

- same Cardume cannot perform conflicting owner actions while away
- incoming Arena attack remains possible

## Milestone 7 — Arena / PvP

Deliver:

- private Strength metric
- Energy 24 + 1/h regen
- 3 opponent cards
- 10% upward rank window
- one reroll lock
- 1 Energy per attack
- direct rank swap
- Honor gain/loss
- deterministic stats + micro-RNG combat
- 3+3 formation
- fixed target order 1–6
- server instant resolution
- replay 1x/2x/skip
- PvP history
- basic Arena Shop surface/config

Completion criteria:

- battle result cannot be altered client-side
- opponent reroll cannot be abused by reopening client
- rank swap is atomic

## Milestone 8 — Fixed-price Market

Deliver:

- Buy/Sell/My Listings/Items to Withdraw
- 5 listings
- 7 days
- no listing fee
- 3% completed-sale fee
- visual cards + filters
- withdrawals
- Aquarium-full purchase behavior

Completion criteria:

- no double-sell/double-buy
- Market cannot bypass Aquarium usable cap

## Milestone 9 — Auction

Deliver:

- Auction tab
- one seller auction active
- 6h duration
- starting minimum bid
- 3% min bid increment
- 1% fee per bid
- bid-fund reservation
- principal refund on outbid
- 1m anti-sniping reset
- early close only with bid
- 3% seller early-close fee
- no cancellation without bids; wait for expiry
- withdrawal integration

Completion criteria:

- simultaneous bid race handled transactionally
- fees/principal are correct under retry/reconnect

## Milestone 10 — Tutorial / taskbar / UX polish

Deliver:

- tutorial flow
- taskbar mode
- catch effects
- toast polish
- audio placeholders
- menu transitions
- visual optimization

## Milestone 11 — Public website / direct distribution

Deliver:

- landing page
- screenshots/media placeholders
- Windows download section
- version/release notes
- Steam placeholder/link system
- build metadata endpoint

## Milestone 12 — V0.1 stabilization

Deliver:

- economy simulation
- combat simulation
- security tests
- API load smoke tests
- save/reconnect tests
- market/auction race tests
- bug pass
- balance pass
- Windows build
- versioned release notes

---

# 46. BALANCE APPROACH

Do not stall implementation waiting for perfect values.

Use provisional data, run simulations, then tune through Development Console.

## Combat simulation

Build a small headless/server test harness capable of running thousands of Cardume battles to:

- estimate average duration
- inspect offense/defense balance
- verify Speed is secondary
- calibrate private Strength metric
- identify dominant species/stats

## Economy simulation

Simulate:

- catches/hour
- NPC sale inflow
- rod upgrade sinks
- fixed-market 3% sink
- auction bid-fee sink
- auction early-close sink
- Honor progression

Do not optimize for a perfect economy before player testing. The goal is visibility and controllability.

---

# 47. OWNER DECISION LOG — LOCKED V0.1 PRINCIPLES

Treat these as locked unless the owner explicitly reopens them:

- Unity 6.3 LTS + C#
- 2.5D beautiful visual presentation
- server authoritative
- 30s online fishing
- 60s offline fishing
- 24h offline cap
- Fishing Box separate from Aquarium
- Aquarium hard cap 100
- fish stats deterministic from species/rarity/size/level
- fish max Lv.10
- feeding consumes fish
- 50% invested XP recovery when consuming leveled fish
- one Cardume per player
- Cardume 1–6
- 6/6 = +3% all four stats in all Cardume activities
- four combat stats only
- no skills
- micro damage RNG only
- Speed secondary
- target order 1→6
- 3 front + 3 back visual formation
- dead slot stays empty
- most battles target ≤ ~60s but no hard timeout
- replay 1x/2x/skip
- Arena Energy max 24, +1/hour
- every Arena attack costs 1 Energy
- rank direct swap on attacker victory
- 3 Arena opponents in ~10% upward window
- one reroll, then locked until attack
- Honor gains on wins/defenses and small losses on defeat
- Cardume Strength private
- Expedition durations 30m/1h/3h/6h
- Expeditions reward Coins + very rare fish chance, no XP/Shells
- Market fixed-price 5 listings, 7 days, 3% sale fee, no listing fee
- unified Items to Withdraw
- Auction one active seller listing, 6h
- auction +3% minimum next bid
- 1% bidder fee every bid
- 1m anti-snipe reset
- seller early close only when a bid exists, 3% early-close fee
- maps change manually
- map travel 30s
- 10-map / 100-level long-term structure
- rod tiers 1/3/5/7/9 map pairing pattern via Lv.10/30/50/70/90 purchase milestones
- V0.1 includes Starter Rod + Rod 1
- rod bonuses: rarity chance, size quality, Shell acquisition
- rod internal Lv.1–10 by Coin purchase
- Shells exist but no V0.1 spending use
- Inventory lives inside Profile
- Encyclopedia lives inside Profile
- Development Console/admin panel is part of project workflow
- public website + direct download is part of product direction

---

# 48. DO NOT DO LIST

Claude: do not add these without explicit owner instruction:

- extra currencies because “the economy needs them”
- crit chance
- dodge
- accuracy
- active skills
- fish classes
- healer role
- formation synergies
- artificial balanced-team bonuses
- Aquarium expansion
- random fish IVs
- multiple Cardumes
- separate defense Cardume
- daily PvP attack cap beyond Energy
- Auction cancellation convenience
- random extra equipment slots in V0.1
- complicated crafting
- daily quests
- achievements just to fill UI
- social features just to fill Profile
- automatic map travel on level-up

Simplicity is a feature.

---

# 49. FIRST INSTRUCTION TO CLAUDE

Start with **Milestone 0 only**.

Before implementing gameplay, create:

1. repository structure
2. Unity client shell
3. server shell
4. PostgreSQL local environment
5. health endpoint
6. Development Console shell
7. roadmap.json populated with Milestones 0–12
8. docs/DECISIONS.md containing locked principles
9. docs/CHANGELOG.md
10. a small README explaining how the owner launches each part locally

Then update Milestone 0 tasks in the Development Console as each item is completed.

Do not proceed to Milestone 1 until Milestone 0 is working and the owner can see the roadmap/status panel.

