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
infraestrutura online que já existia foi preservada no M12, adiada. Em 05/10/2026 o proprietário decidiu terminar o jogo inteiro: tudo que o GDD pede e ainda falta entrou no roadmap (M11, M12, M18 a M20) como **proposta para ele analisar** (`from_gdd_review`), e os testes dele ficam para o final (M19). Nenhuma proposta começa sem a aprovação dele.

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
| **M11** | Balanceamento do MVP | Simulador e relatório dos números, testes de save e de abuso, correções. As revisões de números (progressão, economia, raridades, combate, Expedições) esperam o proprietário decidir as metas |
| **M12** | Jogo online: servidor, contas e jogadores reais | Já construídos: servidor ASP.NET, PostgreSQL, Docker, painel web. A fazer: banco completo, serviços remotos, contas, segurança e auditoria, Arena, Mercado, Leilão, Ranking e comércio entre jogadores reais, perfil público, site público, testes online |
| **M13** | V0.2 · Áudio e ambiente | Sons de captura, recorde e subir de nível; mar e brisa calmos; sons como arquivos trocáveis |
| **M14** | V0.2 · Visual Lago Dourado | A Bíblia de Arte (`docs/ART_BIBLE_V0_1.md`) aplicada: tema central, fontes, card de peixe, cena em luz dourada, níveis de intensidade e momentos especiais |
| **M15** | V0.2 · Sucesso da pesca e equipamentos | O peixe que morde pode escapar; barcos e iscas na Loja, com Moedas e Conchas; chance visível; painel com simulador; rebalanceamento (`docs/SISTEMA_SUCESSO_PESCA.md`) |
| **M16** | V0.2 · Mapas 3 e 4 | Pantanal Dourado e Estuário das Marés, raridade Épico, Vara 2 e 20 espécies (`docs/PROGRESSAO_MAPAS_3_4.md`) |
| **M17** | V0.2 · Conchas, Dólares e Ranking | Conchas em todo equipamento, moeda Dólares, comércio de Conchas e Dólares, menu Ranking, versão para amigos |
| **M18** | Lacunas do GDD que dão para fazer agora (propostas a analisar) | Itens do GDD que ainda faltam e não dependem de servidor: posição na Arena no Perfil, perfil do adversário, nome e avatar, efeitos do replay, avisos guardados, Perfil com barco e isca, partes do Painel |
| **M19** | Site, distribuição e lançamento da V0.1 | Build oficial para Windows, download com instalador, notas de versão, Steam e a rodada final de bugs com os testes do proprietário |
| **M20** | Depois da V0.1: sistemas futuros do GDD | O que o GDD deixa para depois (habilidades, temporadas, guildas, mapas 5 a 10, monetização…), cada um começando como decisão do proprietário |
| **M21** | Progressão até o Nível 100: Mapas 5 a 10 | Aprovado pelo proprietário em 05/10/2026 e adaptado às regras atuais (`docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md`, números em `docs/propostas/mapas_5_10.json`): Lendário e Mítico, 60 espécies, Mapas 5 a 10, Varas 3 a 5, integração, simulação e arte |
| **M22** | Revisão de segurança, jogo mais leve e pesquisa sobre jogos idle | Brechas de relógio, Mercado e save fechadas; imagens com teto de tamanho e PNG sem perdas; propostas da pesquisa registradas para o proprietário analisar (OD-048 a OD-050) |
| **M23** | Ilha 2D (descartada) | Descartada pelo proprietário em 10/10/2026; fica só a página com as 3 visões, para a numeração seguir em ordem |
| **M24** | Novo escopo idle: números grandes, Tripulação e Melhorias | Feito em etapas, vendo no jogo (OD-054). Etapa 1: itens sem nível mínimo, Pescador até o Nv.1000 com curva nova e saves convertidos sem perder nível (A-153). Etapa 2: a Tripulação automática, com a tela, a renda aberta e fechada e o save v13 (A-154). Depois: economia até bilhões e Melhorias |

## Regras que o roadmap impõe a si mesmo

1. **Um milestone de cada vez, na ordem.** O proprietário joga e dá retorno antes de o projeto
   avançar muito além do que ele já testou.
2. **Os critérios de conclusão fazem parte do milestone, não são enfeite.** Um milestone não está
   pronto porque as tarefas foram marcadas; está pronto quando os critérios se sustentam.
3. **Nada fora do escopo da V0.1 ganha uma tarefa.** Se algo parece necessário e não está na lista da
   V0.1 do GDD, vira um `NEEDS_OWNER_DECISION`, não uma tarefa extra.
