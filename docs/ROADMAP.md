# Roadmap

**`docs/roadmap.json` é a fonte de verdade.** Este arquivo explica como ele funciona e o que cada
milestone significa. Se os dois discordarem, o JSON está certo.

## Como o status funciona

Toda tarefa carrega exatamente um status:

| Status | Significa | No painel |
|---|---|---|
| `TODO` | Não começou | A fazer |
| `IN_PROGRESS` | Sendo trabalhada agora | Em andamento |
| `DONE` | Terminada, com `completion_notes` dizendo o que foi entregue | Concluído |
| `BLOCKED` | Não pode avançar por causa de uma dependência técnica | Bloqueado |
| `NEEDS_OWNER_DECISION` | Não pode avançar até o dono decidir algo | Precisa da sua decisão |

Os valores em si ficam em inglês porque são chaves técnicas gravadas no arquivo; o painel os traduz
na hora de exibir (veja `DECISOES.md`, TD-014).

Uma tarefa vira `DONE` **na mesma mudança que conclui o trabalho** — nunca como uma declaração
separada de progresso. O painel reflete o estado do repositório, não atividade em segundo plano.

Qualquer tarefa marcada como `NEEDS_OWNER_DECISION` também aparece na lista `owner_decisions` com a
pergunta completa, e a Visão geral mostra as duas coisas. Um teste automatizado garante esse
pareamento (`RepositoryRoadmapIntegrityTests`), então uma decisão não pode ser levantada numa tarefa
e depois se perder.

## Como o painel lê isso

```
docs/roadmap.json
      │
      ├── Painel de Desenvolvimento do Unity   ← caminho principal no MVP local
      │   (menu Fishing Idle → Painel de Desenvolvimento; lê o arquivo do repositório)
      │
      └── Painel web (adiado, M12)
          ├── GET /api/dev/roadmap   (o servidor valida e calcula o resumo)
          └── lido diretamente, do clone ou do GitHub, quando a API está fora
```

A integridade do arquivo é verificada por testes em `tools/GameService.Tests`
(`RoadmapIntegrityTests`) e, quando o .NET 10 está instalado, também pelos testes do servidor.

## Os milestones

A ordem segue `docs/CLAUDE_START_HERE_V0_1_1.md`: **primeiro um MVP local, jogável no PC**. A
infraestrutura online que já existia foi preservada no M12, adiada.

| # | Título | Entrega |
|---|---|---|
| **M0** | Jogo abre e é editável | Projeto Unity organizado em assemblies, cena principal, balanceamento por dados, save local, Painel de Desenvolvimento no Unity, roadmap em PT-BR, documentação, checagens sem o Unity |
| **M1** | Loop de pesca jogável | Cena 2.5D do Lago Sereno, animação sincronizada, iniciar/parar, ciclo de 30s pelo serviço local, sorteio de espécie e tamanho, Caixa de Pesca, venda, Moedas, XP do Pescador, avisos |
| **M2** | Aquário e peixe persistente | Aquário de 100, peixe persistente só ao guardar, ficha, atributos determinísticos, nível 1–10, alimentação com 50% de recuperação de XP, ordenação, confirmações |
| **M3** | Perfil, Cardume, Inventário e Vara | Perfil, slot de Vara, Inventário, Cardume 1–6 em 3+3, bônus 6/6, Força privada, Enciclopédia, recordes |
| **M4** | Mapas e Varas | Menu Mapa, Rio Selvagem, desbloqueio no Nv.10, viagem de 30s, Vara 1, melhorias 1–10, bônus, varas antigas no Inventário |
| **M5** | Pesca offline | 60s por captura, limite de 24h, cálculo por carimbo de tempo, tela de retorno |
| **M6** | Expedições | 30min/1h/3h/6h, Força Recomendada, recompensas, travas, aviso de conclusão |
| **M7** | Arena local | Bots locais, Energia, Honra, ranking com troca direta, 3 adversários, combate automático, replay |
| **M8** | Mercado local | Comprar, Vender, Meus Anúncios, Itens a Retirar, taxa de 3%, anúncios simulados |
| **M9** | Leilão local | Leilão de 6h, lances +3%, taxa de 1%, proteção de último minuto, encerramento antecipado |
| **M10** | Tutorial, UX e polimento | Tutorial, menus completos, modo compacto, áudio provisório, animações melhores |
| **M11** | Balanceamento do MVP | Progressão, economia, raridades, combate, expedições, testes de save e de abuso |
| **M12** | Infraestrutura online (adiada) | Servidor ASP.NET, PostgreSQL, Docker, painel web (já construídos), serviços remotos, contas, site público |

## Regras que o roadmap impõe a si mesmo

1. **Um milestone de cada vez, na ordem.** O proprietário joga e dá retorno antes de o projeto
   avançar muito além do que ele já testou.
2. **Os critérios de conclusão fazem parte do milestone, não são enfeite.** Um milestone não está
   pronto porque as tarefas foram marcadas; está pronto quando os critérios se sustentam.
3. **Nada fora do escopo da V0.1 ganha uma tarefa.** Se algo parece necessário e não está na lista da
   V0.1 do GDD, vira um `NEEDS_OWNER_DECISION`, não uma tarefa extra.
