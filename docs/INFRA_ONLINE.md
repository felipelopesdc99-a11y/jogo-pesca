# Infraestrutura online (adiada — Milestone 12)

> **Nada disto é necessário para jogar.** Pela prioridade atual (`docs/CLAUDE_START_HERE_V0_1_1.md`),
> o jogo roda inteiro no PC, dentro do Unity. Esta página guarda as instruções da infraestrutura
> online que já foi construída no primeiro Milestone 0, para quando ela voltar a ser usada.

## O que existe

| Parte | Pasta | Estado |
|---|---|---|
| API ASP.NET Core (.NET 10) com `/health` e endpoints do painel | `server/` | Funciona; 36 testes |
| PostgreSQL + migrations (EF Core) | `server/src/FishingIdle.Api/Persistence` | Funciona |
| Docker Compose (banco, API, painel) | `ops/docker-compose.yml` | Funciona |
| Painel web (Next.js) lendo o roadmap | `web/dev-console/` | Funciona; lê o mesmo `docs/roadmap.json` |
| Diagnóstico de conexão no Unity | `client-unity/Assets/Scripts/Core` e `Diagnostics` | Dormente (não é instalado ao dar Play) |

Quando o jogo for para a internet, o caminho é implementar versões remotas dos serviços do jogo
(`RemoteFishingService` etc.) sobre esta API. Veja `docs/BASE_TECNICA.md`, seção "Do local para o
online".

## Ferramentas necessárias

| Ferramenta | Para quê |
|---|---|
| Docker Desktop | PostgreSQL, API e painel |
| SDK do .NET 10 | Aplicar as migrations a partir da sua máquina |
| Node.js 22+ | Rodar o painel fora do Docker (opcional) |

## Subir tudo

```bash
cp .env.example .env          # só na primeira vez
./ops/scripts/dev-up.sh
```

| Serviço | Endereço |
|---|---|
| Painel web | <http://localhost:3000> |
| Saúde da API | <http://localhost:5080/health> |
| PostgreSQL | `localhost:5432` |

Para parar: `./ops/scripts/dev-down.sh` (com `--purge` também apaga o banco).

## Só o painel web, sem Docker

```bash
cd web/dev-console
npm install
npm run dev
```

Abra <http://localhost:3000>. Sem o servidor, uma faixa amarela avisa que a página foi montada
lendo `docs/roadmap.json` direto do repositório — isso não é erro.

## Painel publicado na internet

O passo a passo para publicar o painel web está em `docs/PAINEL_ONLINE.md`. **Adiado:** o START
HERE V0.1.1 pede para não usar Vercel, domínio nem hospedagem enquanto o MVP local não for validado
(tarefa `M12-T12`).

## Partes separadas

```bash
docker compose -f ops/docker-compose.yml up -d postgres   # só o banco
./ops/scripts/migrate.sh                                  # aplicar migrations
cd server && dotnet run --project src/FishingIdle.Api     # a API, a partir do código
./ops/scripts/new-migration.sh NomeDaMudanca              # criar migration
```

As migrations nunca são aplicadas sozinhas quando o servidor sobe — veja `docs/DECISOES.md`, TD-002.
