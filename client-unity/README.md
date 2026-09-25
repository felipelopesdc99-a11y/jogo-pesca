# client-unity — Unity client shell

Unity 6.3 LTS / C#. The client is **presentation and player intent only**. The server owns every
rule, every random roll and every value with economic, progression or competitive meaning
(GDD section 41).

## What exists in Milestone 0

| Piece | File | Does |
|---|---|---|
| Bootstrap | `Assets/Scripts/Core/ClientBootstrap.cs` | Installs the persistent services on start, from code, so pressing Play in any scene works |
| Backend settings | `Assets/Scripts/Core/BackendSettings.cs` | Holds the backend URL and timeouts outside of code |
| API client | `Assets/Scripts/Core/ApiClient.cs` | Minimal GET access over `UnityWebRequest` |
| Health probe | `Assets/Scripts/Diagnostics/ServerHealthProbe.cs` | Polls `/health` and tracks the connection state |
| Diagnostic overlay | `Assets/Scripts/Diagnostics/DiagnosticOverlay.cs` | Dev-only panel showing connection status (F1 toggles) |

There is **no gameplay and no designed scene yet**. The first real scene — Map 1, Lago Sereno — is
roadmap task `M2-T01`.

## Opening the project

1. Install **Unity 6.3 LTS** through Unity Hub.
2. Add this folder (`client-unity`) as a project.
3. Start the backend first: `ops/scripts/dev-up.sh` from the repository root.
4. Press **Play**. The overlay in the top-left reports one of:
   - **connected** — the server answered and its database is healthy;
   - **degraded** — the server answered but PostgreSQL is not reachable *from the server*;
   - **unreachable** — nothing answered at the configured URL.

Press **F1** to hide or show the overlay. It is compiled out of release player builds.

### First-open notes

- `ProjectSettings/ProjectVersion.txt` pins `6000.3.0f1` as a placeholder for Unity 6.3 LTS. Unity
  Hub will offer to upgrade the project to your installed patch version; accept it and commit the
  updated file. This is tracked as roadmap task `M0-T14`.
- Only `ProjectVersion.txt` is committed under `ProjectSettings/`. Unity generates the rest with
  defaults on first open. Set **Product Name** and **Company Name** in
  *Edit > Project Settings > Player* once, then commit the generated files.
- The render pipeline is intentionally **not** chosen yet. Universal RP is the expected choice for
  the 2.5D presentation, and it is selected as part of building the first scene (`M2-T01`) so the
  pipeline asset and the scene are configured together rather than half-configured now.

## Pointing the client at a different server

Either set the `FISHING_IDLE_API_BASE_URL` environment variable (wins over everything), or create
`Assets/Resources/BackendSettings.asset` via *Assets > Create > Fishing Idle > Backend Settings*.
See `Assets/Resources/README.md`.

## Rules for code in this project

1. Never compute a catch, a stat, a price, a battle outcome or a reward here. Ask the server.
2. Never trust the local clock for anything that matters. The server's timestamp is the truth.
3. Never read a balance number from code. Balance lives in `/config` and reaches the client through
   the server.
4. Visual-only randomness (water motion, birds, clouds) needs no server authority and should not
   ask for any.
