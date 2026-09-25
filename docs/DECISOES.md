# Decisões

Este arquivo guarda dois tipos de decisão.

**A Parte 1** é o design travado da V0.1, definido pelo dono. Foi copiada da seção 47 do GDD e não
está aberta a reinterpretação. Trate cada linha como fechada, a menos que o dono reabra
explicitamente.

**A Parte 2** são as decisões técnicas tomadas durante a implementação, nos pontos em que o GDD
deixa margem. Cada uma registra o que foi escolhido, por quê, e o que faria valer a pena rever.
Conforme a instrução inicial do projeto: escolhas puramente técnicas, que não mudam a experiência
do jogador, são tomadas e documentadas aqui; qualquer coisa que mudaria a experiência, a economia,
a progressão ou o escopo vira uma tarefa `NEEDS_OWNER_DECISION` no `roadmap.json`.

---

# Parte 1 — Princípios travados da V0.1

- Unity 6.3 LTS + C#
- Apresentação visual 2.5D caprichada
- Servidor autoritativo
- Pesca online a cada 30s
- Pesca offline a cada 60s
- Limite de 24h de acúmulo offline
- Caixa de Pesca separada do Aquário
- Limite rígido de 100 no Aquário
- Atributos do peixe determinísticos a partir de espécie, raridade, tamanho e nível
- Nível máximo do peixe: 10
- Alimentar consome o peixe
- Recuperação de 50% do XP investido ao consumir um peixe evoluído
- Um Cardume por jogador
- Cardume de 1 a 6 peixes
- 6/6 = +3% nos quatro atributos, em todas as atividades de Cardume
- Apenas quatro atributos de combate
- Sem habilidades
- Apenas micro variação aleatória de dano
- Velocidade como eixo secundário
- Ordem de alvo de 1 a 6
- Formação visual de 3 na frente e 3 atrás
- Vaga de peixe derrotado continua vazia
- A maioria das batalhas mira ≤ ~60s, sem tempo limite rígido
- Replay em 1x, 2x e pular
- Energia máxima de 24 na Arena, +1 por hora
- Todo ataque na Arena custa 1 de Energia
- Troca direta de posições quando o atacante vence
- 3 oponentes na Arena dentro de uma janela de ~10% acima
- Uma rerrolagem, depois travado até o ataque
- Honra ganha em vitórias e defesas, com perdas pequenas na derrota
- Força do Cardume é privada
- Durações de Expedição: 30m, 1h, 3h e 6h
- Expedições dão Moedas + chance muito rara de peixe, sem XP e sem Conchas
- Mercado de preço fixo: 5 anúncios, 7 dias, taxa de 3% na venda, sem taxa de anúncio
- Área unificada de Itens a Retirar
- Leilão: um anúncio ativo por vendedor, 6h
- Lance mínimo seguinte de +3% no leilão
- Taxa de 1% do participante a cada lance
- Reinício de 1 minuto contra lance de última hora
- Encerramento antecipado pelo vendedor só com lance existente, com taxa de 3%
- Mapas mudam manualmente
- Viagem entre mapas leva 30s
- Estrutura de longo prazo de 10 mapas e 100 níveis
- Padrão de pareamento de varas tier 1/3/5/7/9 com marcos de compra nos níveis 10/30/50/70/90
- A V0.1 inclui a Vara Inicial e a Vara 1
- Bônus das varas: chance de raridade, qualidade de tamanho e obtenção de Conchas
- Níveis internos da vara de 1 a 10, comprados com Moedas
- Conchas existem, mas não têm onde ser gastas na V0.1
- O Inventário fica dentro do Perfil
- A Enciclopédia fica dentro do Perfil
- O Painel de Desenvolvimento faz parte do fluxo de trabalho do projeto
- Site público + download direto fazem parte da direção do produto

## Lista do que não fazer

Não adicionar sem instrução explícita do dono: moedas extras, chance de crítico, esquiva, precisão,
habilidades ativas, classes de peixe, papel de curandeiro, sinergias de formação, bônus artificiais
de time equilibrado, expansão de Aquário, atributos ocultos aleatórios nos peixes, múltiplos
Cardumes, Cardume separado de defesa, limite diário de ataques no PvP além da Energia, conveniência
de cancelar leilão, slots extras de equipamento na V0.1, sistemas complicados de criação, missões
diárias, conquistas só para preencher tela, recursos sociais só para preencher o Perfil, viagem
automática ao subir de nível.

---

# Parte 2 — Decisões técnicas

## TD-001 — Backend em .NET 10 LTS

**Decisão.** A API usa `net10.0`.

**Por quê.** O GDD especifica ASP.NET Core em C# sem fixar uma versão. O .NET 10 é o LTS atual,
então o período de suporte ultrapassa com folga a V0.1, e ele compartilha o ecossistema C# com o
Unity, como o GDD pretende.

**Rever se.** Alguma biblioteca necessária ou algum provedor de hospedagem não suportar o .NET 10.

## TD-002 — EF Core com Npgsql, migrations aplicadas deliberadamente

**Decisão.** O schema é gerenciado apenas por migrations do EF Core. O servidor nunca migra ao
subir; quem aplica é `ops/scripts/migrate.sh`.

**Por quê.** Gerenciar schema por migrations é exigência do GDD. Migrar automaticamente ao subir
significa que um deploy pode remodelar um banco em produção silenciosamente — exatamente o tipo de
acidente que uma economia com servidor autoritativo não pode se dar ao luxo de ter.

**Rever se.** Nunca por conveniência. Um pipeline gerenciado de deploy pode rodar o mesmo comando,
desde que como um passo explícito.

## TD-003 — `snake_case` em toda a comunicação e no banco

**Decisão.** O JSON que trafega na rede e as colunas do PostgreSQL usam `snake_case`. O C# mantém
PascalCase internamente e uma política de nomes converte na fronteira.

**Por quê.** Os próprios arquivos de configuração e de roadmap do repositório usam `snake_case`. Uma
grafia por campo significa que os DTOs do Unity, os tipos TypeScript do painel e os arquivos JSON
nunca divergem, e que SQL escrito à mão continua legível.

## TD-004 — Pesos de captura ficam em `maps.json`, não em `fish_catalog.json`

**Decisão.** `fish_catalog.json` guarda só dados próprios da espécie. Os pesos de captura por mapa
ficam junto do mapa.

**Por quê.** Um pool de peixes é uma propriedade do lugar. Manter os pesos no mapa faz com que uma
espécie presente em dois mapas seja definida uma única vez, e permite rebalancear o pool de um mapa
sem tocar na espécie.

## TD-005 — O arquivo do roadmap é a fonte de verdade, e o servidor deriva o resumo

**Decisão.** `docs/roadmap.json` é o que vale. O servidor o lê e calcula o resumo da visão geral
(percentual de conclusão, ordenação das próximas tarefas, lista de atenção). O painel exibe o que o
servidor calculou.

**Por quê.** Derivar os números num único lugar faz com que todos os consumidores concordem sobre o
que significa "42% concluído". O arquivo ser um JSON simples dentro do repositório mantém o
progresso auditável e independente de qualquer conversa, como o GDD exige.

## TD-006 — O painel lê o arquivo do roadmap diretamente quando a API cai

**Decisão.** Quando a API está inacessível, o painel lê `docs/roadmap.json` por conta própria e
calcula o resumo em TypeScript, marcando na página qual fonte respondeu.

**Por quê.** O dono precisa conseguir ver o status do projeto mesmo com o servidor fora — é a razão
de existir do painel. O custo é uma função espelhada (`web/dev-console/src/lib/summary.ts`) que
precisa ser alterada junto com o `RoadmapService.BuildSummary` do backend. A marca visível da fonte
faz com que uma divergência entre as duas apareça em vez de se esconder.

**Rever se.** A lógica do resumo crescer a ponto de espelhá-la virar um risco real de manutenção. A
alternativa é um painel que não mostra nada quando a API cai.

## TD-007 — Roadmap inválido é rejeitado, não exibido

**Decisão.** O carregador recusa um roadmap com status desconhecido, id de tarefa duplicado,
dependência apontando para o nada, ou `current_milestone` que não existe. O endpoint então responde
503 com o motivo.

**Por quê.** Um painel que mostra status errado como se fosse verdade é pior que um painel que diz
que não conseguiu ler o arquivo. O `RepositoryRoadmapIntegrityTests` roda a mesma validação contra o
arquivo real, então um erro de digitação quebra a suíte de testes.

## TD-008 — O cliente Unity inicializa por código, não por uma cena versionada

**Decisão.** O `ClientBootstrap` instala o verificador de saúde e o overlay de diagnóstico via
`RuntimeInitializeOnLoadMethod`. Nenhum arquivo `.unity` de cena é versionado no Milestone 0.

**Por quê.** Dar Play em qualquer cena mostra o estado da conexão, e um arquivo de cena escrito à
mão — cujo formato depende do editor e da versão — não pode ser gerado de forma confiável fora do
Unity. A primeira cena de verdade é construída no editor como parte da tarefa `M2-T01`.

## TD-009 — Escolha do pipeline de renderização adiada para a primeira cena

**Decisão.** Nenhum pacote de pipeline de renderização foi adicionado no Milestone 0.

**Por quê.** O Universal RP é a escolha esperada para a apresentação 2.5D, mas o asset do pipeline,
a configuração da câmera e a montagem em camadas da cena são uma decisão só. Adicionar o pacote
agora entregaria uma pilha gráfica meio configurada. A escolha acontece dentro da `M2-T01`.

**Rever se.** Nada — isto é uma questão de momento, não uma rejeição do URP.

## TD-010 — Só o `ProjectVersion.txt` é versionado dentro de `ProjectSettings/`

**Decisão.** O Unity gera o resto de `ProjectSettings/` com valores padrão na primeira abertura.

**Por quê.** Esses arquivos são assets serializados, grandes e específicos da versão. Escrevê-los à
mão fora do editor arriscaria versionar um projeto que não abre. O dono define o Nome do Produto e o
Nome da Empresa uma vez e versiona o que o editor gerou; veja `client-unity/README.md`.

## TD-011 — `/health` sempre responde 200; o corpo carrega o veredito

**Decisão.** `/health` devolve HTTP 200 com um campo `status` e o detalhamento por dependência.
`/health/live` não toca em nada.

**Por quê.** A tela de diagnóstico do Unity precisa distinguir três estados, não dois: "nada está
escutando", "o servidor está no ar mas o banco não está", e "tudo certo". Uma resposta de saúde com
código de erro junta os dois últimos numa falha de transporte sem corpo legível.

## TD-012 — Edição de configuração é somente leitura até o Milestone 1

**Decisão.** `GET /api/dev/config` serve os arquivos de balanceamento somente para leitura. Não
existe caminho de escrita ainda.

**Por quê.** Editar balanceamento sem validação, versionamento e trilha de auditoria permitiria que
um balanceamento inválido ou não rastreável chegasse ao jogo. Os três chegam juntos nas tarefas
`M1-T06` a `M1-T08`. O painel diz isso na página de Configuração, em vez de mostrar controles
desabilitados.

## TD-013 — Os endpoints do painel não têm autenticação por enquanto

**Decisão.** `/api/dev/*` não tem autenticação no Milestone 0. O ambiente sobe apenas em localhost e
não pode ser exposto publicamente.

**Por quê.** Autenticação e separação de papéis administrativos são as tarefas `M1-T01` e `M1-T08`.
Escrever um esquema de autenticação descartável agora resultaria em algo substituído imediatamente,
ou naquilo que ninguém revisita depois. A restrição está registrada em `docs/SEGURANCA.md` e nos
próprios comentários do endpoint.

**Rever se.** O painel precisar ser acessível de algum lugar além da máquina do dono. Aí a `M1-T01`
precisa vir primeiro.

## TD-014 — PT-BR em tudo que uma pessoa lê; inglês no que só programador lê

**Decisão.** Está em **PT-BR**: a interface do painel, a interface do jogo, os nomes das espécies,
varas, expedições, raridades e categorias de tamanho, as mensagens de erro exibidas na tela, os
títulos e descrições das tarefas do roadmap, as descrições dos arquivos de configuração e toda a
documentação do repositório.

Fica em **inglês**: comentários de código, nomes de variáveis, classes e arquivos, mensagens de log
estruturado do servidor, e os valores-chave técnicos gravados em dados — os status
(`TODO`, `IN_PROGRESS`, `DONE`, `BLOCKED`, `NEEDS_OWNER_DECISION`), os subsistemas (`server`,
`client`, `web`…) e os identificadores (`lambari`, `rod_01`, `map_02`).

**Por quê.** O dono do projeto não é programador e precisa conseguir ler tudo que o projeto lhe
apresenta. Ao mesmo tempo, nomes de código e chaves de dados em inglês mantêm o projeto legível para
futuros desenvolvedores e para as ferramentas, e uma chave técnica não deve mudar quando o idioma
muda. Por isso os status são armazenados em inglês e traduzidos apenas na hora de exibir
(`web/dev-console/src/lib/strings.ts`).

O texto da interface do painel está concentrado em um único arquivo, `strings.ts`, de modo que um
eventual segundo idioma seja uma mudança pequena, e não uma caçada pelo código.

**Exceção deliberada.** `docs/GDD_V0_1.md` e `docs/CLAUDE_START_HERE_V0_1.md` permanecem no original
em inglês. São os documentos de design travados; traduzi-los correria o risco de deslocar o sentido
de uma decisão já fechada, e o GDD é citado como fonte de verdade em todo o restante do projeto. Se
o dono quiser uma versão em PT-BR, ela deve ser criada como um arquivo separado de tradução, nunca
substituindo o original.

**Rever se.** O projeto ganhar jogadores fora do Brasil. Aí o painel troca `strings.ts` e o jogo
precisa de um sistema de textos de verdade — que seria um sistema novo, e portanto uma decisão do
dono, não algo a inventar durante a V0.1.
