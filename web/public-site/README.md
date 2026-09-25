# web/public-site — player-facing website

**Not built yet.** This is Milestone 11 (`M11-T01` to `M11-T06`), and the directory exists so the
monorepo layout from GDD section 4 is complete.

## What it will deliver

- An attractive landing page matching the game's visual identity, with a fishing-scene hero
- A short explanation of the game
- A screenshots and media section
- A **Download for Windows** button, wired to the current versioned build
- A Steam button and link system, placeholder until a Steam page exists
- Version and release notes, minimum requirements, privacy and terms placeholders

## Decisions already made

- **Next.js + TypeScript**, same stack as the Development Console (GDD section 3).
- **Direct download and Steam come from the same codebase and the same `version.json`.** The public
  build metadata endpoint (`M11-T05`) is designed so a lightweight launcher or updater can read it
  later without a redesign — which is why version information is an API response rather than a
  string baked into the client.
- **No custom launcher in the first milestone.** A versioned downloadable installer is enough until
  auto-updating is actually needed (GDD section 6).

Nothing here should be started before Milestone 11 unless the owner reprioritises it.
