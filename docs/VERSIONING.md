# Versioning

## One source of truth

`version.json` at the repository root holds the version of every component. Nothing else declares a
version of record. The server reads the file and serves it at `GET /api/dev/version`; the
Development Console's Build page renders it.

```json
{
  "components": { "client": "0.1.0-m0.1", "server": "0.1.0-m0.1", "web": "0.1.0-m0.1" },
  "build": { "number": 1, "channel": "internal-dev", "date": "2026-09-25", "commit": null }
}
```

`web/dev-console/package.json` carries the same string for npm's benefit. It follows
`version.json`; it does not lead it.

## Format

```
0.<minor>.<patch>-m<milestone>.<build>
```

| Part | Meaning |
|---|---|
| `0.` | Pre-release product. V0.1 is not 1.0. |
| `<minor>` | The product version: `1` for all of V0.1. |
| `<patch>` | Fixes within a released version. `0` until V0.1 ships. |
| `-m<milestone>` | The roadmap milestone the build comes from: `m0` … `m12`. |
| `.<build>` | Internal build counter, reset when the milestone changes. |

Examples: `0.1.0-m0.1` (first Milestone 0 build), `0.1.0-m7.3` (third Arena build),
`0.1.0` (V0.1 released), `0.1.1` (first fix after release).

## When to bump what

| Event | Change |
|---|---|
| Any internal build | `build.number` + 1 |
| A milestone closes | `-m<n>` on every component, `build.number` back to 1, `current_milestone` updated |
| V0.1 ships | Drop the `-m…` suffix entirely |
| A fix after release | `<patch>` + 1 |

Components may differ: shipping a client-only fix bumps `components.client` alone. They share the
`-m<milestone>` part because they are built and tested against each other.

## Channels

| Channel | For |
|---|---|
| `internal-dev` | The owner's machine and internal testing |
| `internal-test` | Wider internal builds once there is something to test |
| `public` | Builds on the public website |

## Release checklist

1. Run `ops/scripts/verify.sh` — the server builds and tests pass, the console typechecks and builds.
2. Update `version.json`: components, `build.number`, `build.date`, `build.commit`.
3. Add a `docs/CHANGELOG.md` entry under the new version.
4. Update the roadmap: mark the milestone's tasks `DONE` with completion notes, move
   `current_milestone` on.
5. Once builds exist (`M12-T09`), set `downloads.windows` so the console and the public site both
   pick it up.

## Steam

Steam and direct download come from the same codebase and the same `version.json`. `downloads.steam`
stays `null` until a Steam page exists (`M11-T06`). The build metadata endpoint (`M11-T05`) is
designed so a lightweight launcher or updater can read it later without a redesign — which is why
version information is an API response and not a string baked into the client.
