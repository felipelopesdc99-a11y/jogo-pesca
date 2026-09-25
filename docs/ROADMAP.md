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
      ├── GET /api/dev/roadmap   (o servidor valida e calcula o resumo)
      │         │
      │         └── Painel de Desenvolvimento  ← caminho normal
      │
      └── lido diretamente pelo painel  ← quando a API está fora
```

O painel marca qual das duas fontes respondeu. Veja `DECISOES.md`, TD-005 e TD-006.

## Os milestones

| # | Título | Entrega |
|---|---|---|
| **M0** | Fundação do repositório e do fluxo de trabalho | Monorepo, esqueleto do Unity, esqueleto do servidor, PostgreSQL + migrations, Docker Compose, arquivos de configuração, documentação, roadmap, painel, versionamento |
| **M1** | Contas, estado do jogador e pipeline de configuração | Autenticação de desenvolvimento, perfil, carteira, progressão, carregamento e versionamento de configuração, edição pelo painel, auditoria, base de idempotência |
| **M2** | Ciclo vertical de pesca | Cena do Mapa 1, ciclo autoritativo de 30s, geração de captura, XP do Pescador, Caixa de Pesca, pesca offline, retorno visual de captura, notificações |
| **M3** | Aquário, identidade do peixe e alimentação | FishInstance persistente, limite de 100, atributos determinísticos, nível do peixe de 1 a 10, alimentação com 50% de recuperação de XP, venda ao NPC, Enciclopédia |
| **M4** | Perfil, Cardume, Inventário e Varas | Perfil próprio e público com separação de campos, slot da vara, Inventário, Cardume de 1 a 6 com bônus 6/6, Vara Inicial, Vara 1 e melhorias, Conchas, Força privada |
| **M5** | Mapa 2, viagem e conteúdo | Menu de mapas, desbloqueio por nível, vara mínima, viagem de 30s, Rio Selvagem, as 20 espécies, elegibilidade de Raros |
| **M6** | Expedições | Quatro durações, Força Recomendada, curvas de eficiência, recompensas em Moedas e peixe raro, conclusão offline, travas de atividade |
| **M7** | Arena e PvP | Energia, seleção de oponentes com uma rerrolagem, motor de combate com micro variação, formação 3+3, troca atômica de posições, Honra, replay, histórico, Loja da Arena |
| **M8** | Mercado de preço fixo | Regras de anúncio, travas de estado, Itens a Retirar, compra com Aquário cheio, filtros, auditoria |
| **M9** | Leilão | Leilões de 6h, regras de lance com valor reservado, segurança contra corrida, proteção de última hora, encerramento antecipado, expiração sem lances |
| **M10** | Tutorial, barra de tarefas e polimento | Tutorial que ensina fazendo, modo compacto, efeitos de captura, avisos, áudio provisório, transições, otimização |
| **M11** | Site público e distribuição | Página inicial, espaços de mídia, download para Windows, versão e notas de lançamento, endpoint de dados do build, espaço da Steam |
| **M12** | Estabilização da V0.1 | Simulação de combate e economia, testes de segurança, testes de carga, testes de reconexão e concorrência, rodadas de bug e balanceamento, build para Windows, notas de lançamento |

## Regras que o roadmap impõe a si mesmo

1. **O Milestone 0 precisa estar funcionando antes do Milestone 1 começar.** O dono tem que
   conseguir ver o painel primeiro.
2. **Os critérios de conclusão fazem parte do milestone, não são enfeite.** Um milestone não está
   pronto porque as tarefas foram marcadas; está pronto quando os critérios se sustentam.
3. **Nada fora do escopo da V0.1 ganha uma tarefa.** Se algo parece necessário e não está na lista da
   V0.1 do GDD, vira um `NEEDS_OWNER_DECISION`, não uma tarefa extra.
