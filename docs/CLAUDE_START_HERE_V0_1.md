> **Nota de idioma.** Este documento é mantido no original em inglês de propósito: é a fonte
> de verdade travada do design da V0.1, citada por todo o restante do projeto, e traduzi-lo
> correria o risco de deslocar o sentido de uma decisão já fechada. Todo o resto do projeto —
> painel, jogo, roadmap e documentação — está em PT-BR. Veja `docs/DECISOES.md`, TD-014.

# START HERE — INSTRUCTION TO CLAUDE

You are implementing a Unity 6.3 LTS / C# 2.5D idle fishing game.

Read `FISHING_IDLE_V0_1_GDD_TECH_SPEC.md` in full before writing gameplay code.

## Your role

Act as the technical lead/executor. The document contains the game designer's locked decisions. Do not redesign the game or add systems that are not requested.

## First task

Implement **Milestone 0 — Repository / workflow foundation only**.

Create the project foundation so the owner can visibly track development before gameplay implementation begins.

Required first deliverables:

1. Unity project shell.
2. ASP.NET Core server shell.
3. PostgreSQL local development environment via Docker Compose.
4. Server `/health` endpoint.
5. Unity client successfully calls `/health` and displays connection status in a dev-only diagnostic surface.
6. Private Development Console web app.
7. `roadmap.json` containing Milestones 0–12 and their tasks.
8. Development Console Dashboard + Roadmap pages that read this roadmap state.
9. `DECISIONS.md`, `CHANGELOG.md`, `README.md`.
10. Clear local run instructions for client/server/web/database.

## Development Console behavior

The owner wants a visual panel showing what you completed, what you are doing, blockers and what is next.

Every meaningful implementation task must update roadmap status:

- TODO
- IN_PROGRESS
- DONE
- BLOCKED
- NEEDS_OWNER_DECISION

Do not claim autonomous/background progress. The panel reflects actual repository/project state that you explicitly update as part of your work.

## Security philosophy

Never trust Unity client for anything economically or competitively valuable.

Client = intent + presentation.
Server = truth + validation + RNG + persistence.

## Important behavioral instruction

When a design detail is unspecified but is purely technical and does not change player experience, choose a reasonable implementation and document it.

When a missing decision materially changes player experience, economy, progression or scope, mark it `NEEDS_OWNER_DECISION` instead of inventing a mechanic.

Do not move past Milestone 0 until its completion criteria are met.
