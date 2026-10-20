# Decisões

Este arquivo guarda dois tipos de decisão.

**A Parte 1** é o design travado da V0.1, definido pelo dono. Foi copiada da seção 47 do GDD e não
está aberta a reinterpretação. Trate cada linha como fechada, a menos que o dono reabra
explicitamente.

Detalhes de **design** decididos durante a implementação (o que o jogador vê e sente) ficam em
`GDD_ADENDO.md`. Como o código funciona está em `BASE_TECNICA.md`.

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

**Origem.** Regra obrigatória definida pelo proprietário. Não é uma inferência técnica e não
pode ser relaxada sem pedido explícito dele. O texto completo da regra está em `CLAUDE.md`, na
raiz do repositório, que é lido no início de toda sessão de trabalho.

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

O texto da interface do painel está concentrado em um único arquivo,
`web/dev-console/src/lib/strings.ts`, de modo que um eventual segundo idioma seja uma mudança
pequena, e não uma caçada pelo código. Ao acrescentar uma tela ou um rótulo, o texto vai para
lá — nunca escrito direto no JSX.

Isso alcança também o que é fácil esquecer: as mensagens que o proprietário vê no terminal ao
rodar os scripts de `ops/scripts`, os comentários dos arquivos que ele abre para editar
(`.env.example`, os arquivos de `/config`), e os valores técnicos que chegam à tela vindos da
API — o overlay do Unity traduz `database` e `healthy` na hora de exibir, em vez de mostrar a
chave crua.

**Exceção deliberada.** `docs/GDD_V0_1.md` e `docs/CLAUDE_START_HERE_V0_1.md` permanecem no original
em inglês. São os documentos de design travados; traduzi-los correria o risco de deslocar o sentido
de uma decisão já fechada, e o GDD é citado como fonte de verdade em todo o restante do projeto. Se
o dono quiser uma versão em PT-BR, ela deve ser criada como um arquivo separado de tradução, nunca
substituindo o original.

**Rever se.** O projeto ganhar jogadores fora do Brasil. Aí o painel troca `strings.ts` e o jogo
precisa de um sistema de textos de verdade — que seria um sistema novo, e portanto uma decisão do
dono, não algo a inventar durante a V0.1.

## TD-015 — MVP local primeiro; as regras rodam num "serviço de jogo" dentro do Unity

**Origem.** Nova prioridade definida pelo proprietário em `docs/CLAUDE_START_HERE_V0_1_1.md`:
primeiro um MVP local, jogável no PC, sem servidor, sem Docker, sem hospedagem e sem custo.

**Decisão.** As regras do jogo ficam no assembly `FishingIdle.GameService`, dentro do projeto Unity,
com `noEngineReferences: true` (proibido usar `UnityEngine`). A apresentação (`FishingIdle.Game`)
fala com ele só por interfaces (`IFishingService`, `IPlayerService`), enviando intenção e recebendo
resultados. O servidor ASP.NET, o PostgreSQL, o Docker e o painel web continuam no repositório,
funcionando, guardados no Milestone 12.

**Por quê.** Mantém o princípio "cliente = intenção + apresentação" já no MVP, sem exigir nada
além do Unity. Por não depender do Unity, o mesmo código pode ser testado fora dele e, depois,
rodar dentro do servidor .NET como autoridade.

**Rever se.** O jogo for para a internet: aí entram os `Remote...Service` (tarefa `M12-T13`).

## TD-016 — A cena é montada por código; a cena salva só existe para o build

**Decisão.** Ao apertar Play, `GameBootstrap` (RuntimeInitializeOnLoadMethod) cria o `GameRoot`,
que monta céu, água, barco, pescador e interface por código. Na primeira abertura, o Editor cria
`Assets/Scenes/Principal.unity` (com o Unity salvando o arquivo, não escrito à mão) e a registra no
Build Settings. Continuação da TD-008.

**Por quê.** O proprietário aperta Play e joga, em qualquer cena. Nenhum arquivo de cena ou prefab
foi escrito fora do Editor, o que evitaria formatos quebrados. Toda a arte provisória também é
gerada por código (`Art.cs`), então o projeto não depende de nenhum asset importado.

**Rever se.** A arte final chegar: aí faz sentido montar a cena no Editor, com prefabs. A troca é
local: `FishingScene` e `Art` são os únicos pontos que mudam.

## TD-017 — Interface do MVP em IMGUI

**Decisão.** HUD, Caixa de Pesca e avisos usam o IMGUI do Unity (`OnGUI`), com estilos gerados por
código em `UiSkin` e uma tela virtual de 1080 px escalada.

**Por quê.** Funciona no instante do Play, sem prefabs, sem fontes importadas, sem configurar
EventSystem nem o sistema de input (que muda entre projetos do Unity 6). É inteiramente checável
fora do Editor. O visual fica aceitável como provisório.

**Rever se.** A interface final for desenhada (Milestone 10): o caminho natural é UI Toolkit. Todos
os estilos estão em `UiSkin` e as telas só leem visões do serviço, então a troca não toca nas regras.

## TD-018 — Pipeline de renderização padrão (Built-in) no MVP

**Decisão.** Nenhum pacote de pipeline é adicionado; o projeto usa o pipeline padrão, com sprites
e câmera ortográfica com paralaxe. Encerra a TD-009 para o MVP.

**Por quê.** O visual 2.5D do MVP é feito de camadas de sprites, que não precisam de luzes 2D nem de
pós-processamento. Adicionar o URP agora exigiria assets de pipeline criados no Editor.

**Rever se.** A arte final pedir luz dinâmica, bloom ou shaders de água: aí o URP entra junto com
ela.

## TD-019 — Tempo com o jogo fechado não é pesca online

**Decisão.** O serviço guarda a última vez em que viu o jogo rodando. Um intervalo maior que
`max(3 ciclos, 2 minutos)` sem ser visto (jogo fechado, PC em suspensão, relógio adiantado) não
gera capturas online: o serviço entrega o que foi pescado até ali e recomeça o ciclo.

**Por quê.** O GDD define 30s online e 60s offline com limite de 24h. Sem essa regra, fechar o jogo
por 10 horas renderia 1.200 capturas "online" ao voltar, e adiantar o relógio do PC também. O
intervalo detectado é justamente o que a pesca offline (Milestone 5) recompensa: desde então ele é
creditado no ritmo offline (60s, até 24h), numa passada só, continuando a mesma sequência de sorteio. Registrado
como design em `GDD_ADENDO.md`, A-001.

**Limite conhecido.** No MVP local, o relógio é o do PC; um jogador pode adiantá-lo. A regra acima
evita o abuso grosseiro, mas a proteção real só existe com o servidor (Milestone 12).

## TD-020 — Checagens sem o Unity: testes em .NET e compilação contra bibliotecas de referência

**Decisão.** `tools/` tem quatro projetos .NET: um compila o `GameService` exatamente como o Unity
(.NET Standard 2.1, C# 9), outro roda os testes, e dois compilam o código do jogo e do Editor contra
as bibliotecas de referência públicas do Unity (NuGet `UnityEngine.Modules` 2021.3 e `Unity3D.SDK`).
`ops/scripts/verify.sh` roda tudo; o servidor e o painel web só entram se o .NET 10 e o Node
estiverem instalados.

**Por quê.** O código é escrito num ambiente sem o Editor do Unity. Sem essas checagens, um erro de
digitação só apareceria quando o proprietário abrisse o projeto. As bibliotecas de referência são
2021.3, então o código evita de propósito APIs que só existem no Unity 6.

**Limite.** Compilar não é rodar: comportamento visual só se confirma no Editor (tarefa `M0-T13`).

## TD-021 — Sem Vercel

**Origem.** Pedido do proprietário (28/09/2026): "não quero usar o Vercel, quero simplificar".

**Decisão.** O projeto não usa a Vercel. O guia de publicação (`docs/PAINEL_ONLINE.md`), a tarefa de
publicar o painel e a decisão pendente sobre a conta foram removidos ou encerrados. O painel web
continua no repositório (Milestone 12) apenas para uso local.

Um projeto da Vercel já estava conectado a este repositório no GitHub e publicava uma prévia a cada
envio, mandando e-mail ao proprietário. Até ele ser desconectado, os arquivos `vercel.json` (na raiz
e em `web/dev-console`) com `git.deploymentEnabled: false` mandam a Vercel ignorar os envios. Nenhum
dos dois é usado pelo jogo.

**Rever se.** O projeto da Vercel for desconectado do GitHub ou apagado: aí os dois `vercel.json`
podem ser removidos.

## TD-022 — Partes arquivadas ficam fora do trabalho do dia a dia

**Origem.** Pedido do proprietário (28/09/2026): manter a infraestrutura online guardada, mas sem
gastar tokens com ela.

**Decisão.** `server/`, `web/`, `shared-contracts/`, os scripts do Docker e o cliente HTTP dormente
do Unity continuam versionados, mas: o `CLAUDE.md` (seção 8) proíbe lê-los ou alterá-los sem pedido
do proprietário; o arquivo `.ignore` faz as buscas pularem essas pastas; e `verify.sh` só os
compila com `--completo`.

**Rever se.** O jogo for para a internet (Milestone 12): aí essas regras saem.

## TD-023 — Mercado local com jogadores simulados e custódia dentro do save

**Origem.** Milestone 8 (START HERE V0.1.1: "o Mercado pode ser simulado com anúncios gerados
localmente/bots, mas o fluxo deve ser o real").

**Decisão.** O fluxo é o do GDD (anunciar, comprar, cancelar, vencer, Itens a Retirar) e mora em
`LocalMarketService`. Os outros jogadores são simulados: vendedores repõem anúncios de hora em hora e
compradores olham os seus anúncios a cada 20 minutos, sempre calculado a partir do relógio e do `Rng`
do jogador — nada roda "em segundo plano". Os valores da simulação ficam num arquivo separado,
`config/market_bots.json`, para que as regras reais (`economy.json → market_fixed_price`) não se
misturem com o que é só do MVP. Um item no Mercado sai do Aquário/Inventário e fica guardado dentro
do anúncio ou de Itens a Retirar.

**Por quê.** Com o item fisicamente em um só lugar, vender duas vezes, comprar duas vezes ou usar um
peixe anunciado deixam de ser possíveis por construção, e o limite do Aquário é checado num ponto só
(a retirada). Quando o Mercado for online, `IMarketService` ganha uma versão remota e
`market_bots.json` deixa de ser usado.

**Rever se.** O Mercado for para o servidor (Milestone 12).

## TD-024 — Áudio provisório sintetizado e preferências no PlayerPrefs

**Origem.** Milestone 10 (START HERE V0.1.1: "áudio provisório").

**Decisão.** Os sons do MVP são gerados por código na primeira vez que tocam (`SoundBank`), sem
arquivos de áudio no projeto. Som ligado/desligado, som ambiente e volume ficam no `PlayerPrefs`,
fora do save.

**Por quê.** Nada para importar nem licenciar agora, e o som final entra trocando só o `SoundBank`.
Preferências de apresentação não são estado do jogo: não mudam captura, preço nem batalha, então não
precisam da validação e das migrações do save.

**Rever se.** O áudio final chegar (arquivos em `Assets/Audio`) ou as preferências precisarem ir junto
com a conta, online.

## TD-025 — Sons como arquivos gerados por script (substitui parte da TD-024)

**Origem.** V0.2, Milestone 13 (29/09/2026): o proprietário achou o som ambiente anterior ruim e pediu
sons para captura, recorde, subir de nível e um ambiente de mar e vento.

**Decisão.** Os sons deixam de ser sintetizados dentro do jogo e passam a ser arquivos `.wav` em
`Assets/Resources/Sons`, carregados pelo nome. Os arquivos atuais são gerados por
`tools/Audio/gerar_sons.py` com uma semente fixa. As preferências de som continuam no `PlayerPrefs`
(TD-024).

**Por quê.** Com arquivos, qualquer som pode ser trocado por uma gravação sem mexer em código, o som
pode ser ouvido fora do Unity antes de testar, e a síntese fora do jogo pode ser bem mais cuidadosa
(filtros, estéreo, loops sem emenda) sem custo na hora de jogar.

**Rever se.** Chegar áudio final (gravado ou comprado): basta substituir os arquivos.

## TD-026 — Arte como arquivos trocáveis, pintados por script, e tema visual em dados

**Origem.** V0.2, Milestone 14 (29/09/2026): a Bíblia de Arte "Lago Dourado" e o documento de 185
referências visuais enviados pelo proprietário.

**Decisão.** Toda imagem do jogo passa a ser um arquivo PNG em `Assets/Resources/Arte`, carregado pelo
nome (`ArtAssets`), no lugar dos desenhos feitos por código na hora de jogar — que continuam como
reserva quando um arquivo falta. A arte provisória é pintada fora do jogo por scripts Python em
`tools/Arte` (sementes fixas). As cores, o brilho e os tempos de efeito ficam num arquivo único,
`Assets/Resources/Visual/tema_visual.json`, lido por `VisualTheme`. A interface continua em IMGUI
(TD-017), com as fontes Fredoka e Nunito (SIL OFL, gratuitas) dentro do projeto. Ícones em PNG, não
em SVG.

**Por quê.** Arquivos seguem o processo da Bíblia (seção 39): o proprietário gera a arte final e ela
entra trocando o arquivo, sem mexer em código nem em layout. Pintar fora do jogo permite uma arte bem
mais rica (sombreado, reflexos, camadas) sem custo ao abrir o jogo, e dá para ver o resultado antes de
abrir o Unity. O tema fica em `Resources`, e não em `/config`, porque é apresentação: não muda captura,
preço nem batalha, e não precisa da validação nem das migrações do balanceamento. PNG porque o Unity
não importa SVG sem um pacote extra.

**Rever se.** A arte final chegar (basta trocar os arquivos), ou a interface migrar para UI Toolkit
(o tema e os arquivos continuam valendo).

## TD-027 — Paisagem viva em dados de apresentação, com um diretor de cenário

**Origem.** V0.2, Milestone 14 (30/09/2026): as imagens "Vivo 01 a 18" do proprietário e o pedido dele
de não fazer "spam" de animação (intervalos realistas entre os animais).

**Decisão.** Onde fica cada planta e cada animal, o peso de cada animal e os tempos de descanso e de
vento ficam em `Assets/Resources/Visual/paisagem_viva.json`, ao lado do tema visual. Um único
`SceneDirector` por cena decide quando o próximo animal aparece: um por vez, descanso sorteado,
sorteio por peso sem repetir o último, pausa com janela aberta, celebração ou viagem. Os efeitos
pequenos que já existiam passam a pedir vez a ele. O comportamento de cada animal é código
(`LivingAnimals`), porque é movimento, não número de balanceamento. Os quadros de animação são
alinhados no script de recorte, não no jogo.

**Por quê.** Um relógio central é o jeito simples de garantir o ritmo pedido: cada efeito com o seu
próprio temporizador acaba coincidindo e parece "spam". Posições e frequências em arquivo deixam o
proprietário ajustar sem mexer em código, como o tema. É apresentação pura, então fica em
`Resources` e não em `/config` (mesma razão da TD-026). Alinhar os quadros no recorte mantém o jogo
simples: todo quadro de um animal tem o mesmo tamanho e o mesmo pivô.

**Rever se.** Entrarem muitos animais novos (talvez um animal por "faixa" da tela ao mesmo tempo) ou
se o proprietário quiser horários do dia (hoje o ritmo não depende da hora).

## TD-028 — Sucesso da Captura como segunda etapa do mesmo sorteio, com barcos e iscas em `equipment.json`

**Origem.** V0.2, Milestone 15 (30/09/2026): `docs/SISTEMA_SUCESSO_PESCA.md`, do proprietário.

**Decisão.** `CatchRules.Attempt` faz, na ordem: espécie, chance de sucesso, sorteio de puxar e, só se
puxou, tamanho, XP e Conchas. Usa o mesmo `Rng` do ciclo (semente do save + sessão + ciclo), então a
mesma tentativa sempre dá o mesmo resultado, online, offline ou refeita. Expedições continuam com
`CatchRules.Roll` (sempre acha o peixe), porque a especificação só fala da pesca. A chance-base fica
em cada raridade (`progression.json → rarity.tiers[].catch_success_base`), o piso e o teto em
`fishing`, o bônus da vara num novo eixo `catch_success` de `rods.json`, e barcos e iscas num arquivo
novo, `config/equipment.json`, que entrou em `GameConfigLoader.RequiredFiles`. O save passou para a
versão 10 (`BoatId`, `OwnedBoatIds`, `BaitCharges`, `ActiveBaitId`, `Stats.Escapes`); saves antigos
entram com o Barco Inicial e sem isca. A isca gasta a carga em `GearRules.SpendAttempt`, dentro da
própria tentativa, e a compra e a troca de equipamento sincronizam a pesca antes (as tentativas já
completas usam o equipamento antigo). O novo `IGearService` (`LocalGearService`) cuida de barcos e
iscas. `CatchSimulator` roda N tentativas de uma combinação sem save; o Painel de Desenvolvimento e
`tools/Simulador` usam o mesmo.

Nos testes, `TestSupport.NewGame()` usa por padrão o config real com 100% de sucesso
(`CertainCatchConfig`), porque os testes de tempo, Caixa, venda e demais sistemas contam um peixe por
ciclo. Os testes do sucesso (`CatchSuccessTests`) e o de "Nível 10 em cerca de 2 h" usam o config de
verdade.

**Por quê.** Ser a segunda etapa do mesmo sorteio mantém a regra auditável e determinística, sem
estado novo além do equipamento. Um arquivo próprio para o equipamento deixa barcos e iscas fáceis de
achar e editar, e prepara barcos e iscas futuros sem inchar `rods.json`.

**Rever se.** Entrar a raridade Épico (chance-base 24%, `OD-019`), bônus temporários
(`bonus_temporario_configuravel` da especificação) ou multiplicadores por mapa.

## TD-029 — Versão para testadores gerada por um menu do Editor, sem servidor

**Decisão.** O menu **Fishing Idle → Gerar versão para amigos (Windows)** (`Editor/FriendsBuild.cs`)
faz o build Windows 64 bits da cena principal em `/dist/FishingIdle_<versão>` (a versão vem de
`version.json`), apaga a pasta de símbolos de depuração, grava um `LEIA-ME.txt` em PT-BR para o
jogador (texto em `GameTexts.FriendsBuild`) e compacta tudo em `/dist/FishingIdle_<versão>.zip`, que é
o arquivo a enviar. O `/config` entra no build pelo `ConfigBuildStep` de sempre. `/dist` não é
versionado. O zip é feito com `ZipArchive`, para não depender de `ZipFile`.

**Por quê.** O proprietário quer amigos testando já, e o MVP é local: cada amigo roda o jogo no
próprio PC, com save próprio, sem servidor nem custo. Um clique evita errar a lista de cenas, o
destino ou o nome do arquivo. Distribuição (Drive, WeTransfer, itch.io) e custos estão em
`docs/ENVIAR_PARA_AMIGOS.md`.

**Rever se.** O jogo for online (M12), entrar Mac ou celular, ou chegar a hora da Steam (taxa de
US$ 100, decisão do proprietário).

## TD-030 — O relógio das regras nunca volta

**Decisão.** O `GameSession` entrega às regras um `SteadyClock`: "agora" é o maior horário em que este
save já foi visto (`ClockHighWaterMs`, novo campo do save, 0 nos saves antigos). Se o relógio do PC
voltar, o tempo do jogo fica parado até a hora real alcançar o ponto onde estava; se adiantar, continua
valendo a TD-019 e os limites de cada sistema (24 h de pesca offline, Energia máxima da Arena).

**Por quê.** A revisão de segurança de 06/10/2026 achou um truque grátis e repetível: voltar o relógio
24 h, parar e recomeçar a pesca (ou reabrir o jogo) e voltar o relógio para a hora certa dava 24 h de
pesca offline, sem custo. O mesmo valia para a Energia da Arena e as Expedições, porque cada sistema
reancorava o seu tempo no horário atrasado. Corrigir no relógio fecha os três de uma vez e é o mesmo
papel que o relógio do servidor fará na versão online.

**Rever se.** O jogo ficar online (o relógio do servidor substitui este) ou aparecer jogador com o
relógio do PC muito errado por engano (o jogo ficaria parado até a hora real alcançar o salto).

## TD-031 — Tamanho das imagens: cópia menor no build e PNG sem perdas

**Decisão.** `Editor/ArtImportSettings.cs` importa cada tipo de imagem no tamanho que a câmera mostra
numa tela 1440p (peixes, Expedições e pescador até 512 px; cantos, margens, fotos dos mapas e barcos até
1024 px; céu até 2048 px; o resto até 4096 px). `GetVersion()` sobe quando a regra muda, para o Unity
reimportar. Os arquivos continuam na resolução do proprietário. `tools/Arte/otimizar_png.py` recomprime
todos os PNG sem mudar nenhum pixel (oxipng) e deve rodar depois de qualquer script de arte.

**Por quê.** A auditoria de 06/10/2026 mediu ~127 MB de arte e ~209 MB de texturas no build, a maior
parte acima do que a tela mostra. O teto por tipo reduz o build e a memória para ~135–140 MB sem tocar
na arte (a Bíblia de Arte manda guardar os originais grandes e deixar o Unity gerar os menores). A
recompressão sem perdas tirou 20 MB do repositório.

**Rever se.** Alguma imagem ficar borrada ao apertar Play (subir o teto daquele tipo), ou o jogo for
para o celular (compressão ASTC e atlas).

## TD-032 — 15 quadros por segundo com a janela em segundo plano

**Decisão.** Com a janela sem foco, o `GameRoot` baixa para 15 quadros por segundo (desligando o vSync
só nesse período) e volta a 60 ao receber o foco. Ao pausar (celular), anota o horário como no fechar
do jogo.

**Por quê.** O jogo continua pescando com a janela atrás de outras; desenhar 60 quadros que ninguém vê
gasta CPU, GPU e bateria. No Android o `OnApplicationQuit` nem sempre é chamado.

**Rever se.** Alguma animação depender de quadros e não de tempo.

## TD-033 — Simulador de progressão em Python, por valor esperado

**Decisão.** `tools/Progressao/simular_progressao.py` simula a subida do Nv.1 ao Nv.100 lendo `/config` e
aplicando as fórmulas do `CatchRules` por valor esperado (sem sorteio), com perfis de jogador (aberto o dia
todo, 4, 2 ou 1 visita por dia) e um comprador de equipamento simples. `tools/Progressao/calibrar_xp.py`
usa esse simulador para gerar a tabela de XP do Pescador e gravá-la com `--gravar`.

**Por quê.** O simulador em C# (`tools/Simulador`) precisa do .NET, que não está disponível em todo
ambiente, e roda as regras completas devagar. Para decidir o ritmo de dias, o valor esperado dá o mesmo
resultado em segundos e é fácil de rodar de novo depois de mexer no balanceamento.

**Rever se.** As fórmulas do `CatchRules` mudarem (o simulador em Python copia as fórmulas), ou entrarem
fontes de XP do Pescador fora da pesca.

## TD-034 — Ferramentas de teste dentro do serviço de jogo

**Decisão.** As ferramentas de teste do proprietário (A-123) são um serviço do jogo (`GameService/Dev`,
`IDevToolsService`) que recusa tudo enquanto `GameSession.DevToolsEnabled` estiver desligado. O cliente liga
essa chave só quando `Application.isEditor || Debug.isDebugBuild`. Avançar o tempo soma um deslocamento
(`PlayerSave.DevTimeOffsetMs`) ao relógio real, por baixo do `SteadyClock` (`OffsetClock`).

**Por quê.** Assim a tela continua só pedindo e mostrando (regra 2 do projeto), os testes usam as mesmas regras
do jogo de verdade, e o build publicado não tem como dar recursos. Guardar o deslocamento no save evita que,
ao reabrir o jogo, o relógio pareça ter voltado (o que congelaria o tempo pelo TD-030).

**Rever se.** O jogo ganhar servidor: lá as ferramentas não existem (o servidor nunca liga a chave) e um
painel de administração próprio, com registro, substitui este.

## TD-035 — Arte compactada com crunch e mapa anterior liberado da memória

**Decisão.** No build, as camadas dos mapas (menos os céus), a paisagem viva, os barcos e as varas usam a
compressão com crunch (qualidade 90); céus e peixes continuam na compressão de alta qualidade. A arte desenhada
bem menor que o arquivo (peixes, ícones, varas, barcos, iscas, Expedições) usa o filtro Kaiser nas cópias
menores, que fica mais nítido. Ao viajar, as camadas do mapa anterior saem da memória (`ArtAssets.Release`).

**Por quê.** A arte é quase todo o tamanho do jogo. Pela conta das texturas, o build cai de ~160 MB para ~75 MB
sem mudar nenhum arquivo do proprietário (os PNG já estavam otimizados sem perda, TD-031). Os céus ficam fora
porque degradês formam faixas com o crunch; os peixes, porque aparecem de perto nos cards.

**Rever se.** Alguma camada aparecer com manchas no jogo: basta tirá-la de `ArtImportSettings.Crunched` ou subir a
qualidade. A primeira importação depois desta mudança demora mais (o crunch é lento só nessa hora).


## TD-036 — Som ambiente montado com gravações reais de terceiros (CC0 / Pixabay)

**Decisão.** O som ambiente sintetizado (`ambiente_mar` e `ambiente_vento`) saiu do jogo. Cada mapa tem uma
gravação de 2,5 minutos (`Resources/Sons/Ambiente/<mapa>_01.ogg`) montada por `tools/Audio/mixar_ambiente.py` com
gravações reais de natureza (rio, ondas, vento, pássaros, sapos, gaivotas, baleias) do projeto de código aberto
Moodist (github.com/remvze/moodist), que declara seus sons como CC0 ou Licença de Conteúdo Pixabay.

**Por quê.** O proprietário achou o loop do mar horrível e pediu um som ambiente normal de escutar. O ambiente de
trabalho não acessa sites de áudio (freesound, Pixabay), mas acessa o GitHub. As duas licenças permitem usar o som
dentro de um jogo comercial sem crédito (não permitem revender o arquivo de áudio sozinho).

**Rever se.** Antes do lançamento na Steam: conferir a origem de cada arquivo usado (o Moodist não lista a fonte
de cada som) ou trocar pelas gravações definitivas; o sistema já aceita até 4 gravações por mapa.


## TD-037 — Nunito SemiBold gerada da fonte variável e alturas de linha numa constante

**Origem.** Pedido de legibilidade do proprietário (08/10/2026, GDD_ADENDO A-149).

**Decisão.** O texto do corpo usa Nunito SemiBold (peso 600), um arquivo estático
(`Resources/Fontes/Nunito-SemiBold.ttf`) gerado da fonte variável oficial (`Nunito[wght].ttf`, versão 3.602, a
mesma dos arquivos Regular e Bold) por `tools/Arte/gerar_nunito_semibold.py`, com fontTools. Se o arquivo faltar,
o jogo volta para a Regular. Os tamanhos pequenos ficam em constantes de `UiSkin` (`SmallSize` 15, `ChipSize` 14,
`BadgeSize` 13) e a altura de uma linha do texto pequeno em `UiSkin.SmallLine` (20 px), usada pelas janelas no
lugar do número solto. Texto direto sobre a arte usa `UiSkin.ShadowLabel` (sombra de 1 px a 50%).

**Por quê.** A interface é desenhada numa tela virtual de 1080 px e reduzida; em janelas de 600–840 px a Regular
ficava fina demais. O Google Fonts só publica a Nunito como fonte variável, e o Unity (fonte dinâmica no IMGUI) não
escolhe peso de fonte variável: precisa de um arquivo por peso. A licença é a mesma (SIL OFL 1.1, sem nome
reservado). Os rótulos do IMGUI cortam o que passa da caixa; com 15 px, a linha da Nunito mede cerca de 20,5 px e
as letras com perna (g, p, ç) eram cortadas nas caixas de 18 px.

**Rever se.** O proprietário quiser o texto mais fino ou mais grosso: trocar o arquivo em `UiSkin` (Regular, Bold
ou outra instância gerada pelo script com outro peso), sem mexer nas janelas.


## TD-038 — Troca de curva de XP do Pescador: XP total vira nível, nunca para baixo

> **Atualizado por TD-053 (12/10/2026, A-172):** a curva está na versão 4 (os níveis 1 a 249 iguais aos da versão 3, o
> resto recalibrado sem a Tripulação). A conversão é a mesma desta decisão. A Tripulação saiu: a pesca é a única fonte
> de XP do Pescador, e o "Rever se" abaixo não vale mais.

**Origem.** M24-T03 (A-153): o Pescador passa do Nv.100 para o Nv.1000 com uma tabela de XP nova.

**Decisão.** `progression.json → fisher.xp_curve_version` numera a curva (1 = Nv.1 ao Nv.100, A-109; 2 = Nv.1 ao
Nv.1000). O save, na versão 12, grava em `fisher_xp_curve_version` a curva em que o nível foi ganho (a migração da
versão 11 grava 1; um jogador novo nasce com a curva atual). Ao carregar o save ou trocar o config, o `GameSession`
chama `FisherLevelRules.MoveToCurrentCurve`: se a curva do save é mais antiga, o `fisher_xp_total` (todo XP já ganho,
inclusive depois do nível máximo) é percorrido na tabela atual. Se o nível alcançado é maior ou igual ao que o jogador
tinha, ele fica com esse nível e a sobra de XP; se é menor, mantém o nível e o XP dentro dele (limitado a 1 abaixo do
próximo). Os marcos de Dólares cruzados na subida (A-111, até `up_to_level`) são pagos. Depois a curva do save passa a
ser a atual, e a conversão não se repete.

**Por quê.** A tabela antiga não precisa ficar no jogo: o XP total já estava no save e é a medida justa do esforço do
jogador. A conversão depende do config (a tabela), por isso fica no `GameSession` e não no `SaveMigrations`, que só
mexe na forma do arquivo. Nunca baixar o nível protege quem subiu com as ferramentas de teste (nível sem XP) e
qualquer curva futura mais lenta.

**Rever se.** Entrar uma fonte de XP do Pescador que não passe por `AddFisherXp` (por exemplo, a Tripulação do M24):
ela também precisa somar em `fisher_xp_total`, senão uma troca de curva futura não a enxerga.

## TD-039 — Tripulação: renda por relógio sem sorteio, cursor no save e preços em fórmula fechada

> **Retirado por TD-053 (12/10/2026, A-172):** a Tripulação saiu do jogo (decisão do proprietário). `config/crew.json`,
> `GameService/Crew` e `PlayerSave.Crew` não existem mais; um save antigo recebe de volta as Moedas que ela custou. Esta
> decisão fica como histórico.

**Origem.** M24-T05 (A-154): pescadores e barcos que rendem Moedas e XP por segundo, com o jogo aberto e fechado.

**Decisão.**
- `config/crew.json` (novo arquivo obrigatório em `GameConfigLoader.RequiredFiles`, validado) guarda os 10
  tripulantes, o desbloqueio (10 do anterior), os marcos, o marco da frota e a regra offline. As regras puras ficam em
  `GameService/Crew/CrewRules.cs`; o serviço em `LocalCrewService` (`ICrewService`: `GetCrew`, `Hire`, `Sync`,
  `MarkSeen`, `TakeOfflineReport`).
- **Sem sorteio.** A renda é determinística: unidades × renda base × 2^(marcos do tripulante) × 2^(marcos da frota),
  em `double` por segundo. Não usa o `Rng`.
- **Cursor no save** (`PlayerSave.Crew`, save versão 13): unidades por id, `LastCreditedAtMs` (até onde a renda já
  foi paga), as frações de Moeda e de XP que ainda não deram 1 (`CoinsCarry`, `XpCarry`) e o total já rendido. `Sync`
  paga de `LastCreditedAtMs` até agora e move o cursor; chamar duas vezes não paga duas vezes. O cliente chama a cada
  1 s; a fração guardada faz a cadência não mudar nada.
- **Aberto ou fechado** se decide pelo tamanho do intervalo: até `online_gap_seconds` (120 s) é jogo aberto (100%);
  acima, é offline (100% até `full_rate_hours`, depois `reduced_rate` até `max_hours`) e gera o resumo do "Bem-vindo
  de volta". Fica separado da pesca offline (TD-019), que tem o próprio teto.
- **Relógio.** O serviço lê só o `IClock` da sessão, que nunca volta (TD-030): relógio voltado não paga nem tira nada.
  Um cursor no futuro (save editado) é trazido para agora sem pagar. O teto offline limita o relógio adiantado.
  Ferramentas de teste: "avançar tempo (online)" paga o período cheio como jogo aberto (`CreditAsOnline`).
- **Gravação.** A renda sozinha grava o save no máximo a cada 30 s (contratar e fechar o jogo gravam na hora). Moedas
  e cursor vão juntos no mesmo arquivo, então uma queda perde no máximo esses segundos de *gravação*, não de renda: no
  próximo início o tempo é pago como offline.
- **Preços em fórmula fechada:** b·r^n·(r^k − 1)/(r − 1) em `double`, arredondada para `long`. O que passa de
  ~9,2 × 10^18 vira `CrewRules.Unaffordable` e nunca é pago (nem com `long.MaxValue` Moedas). "Máx" resolve k por
  logaritmo e confere com a própria fórmula. Moedas e XP somam com teto em `long.MaxValue`.
- **XP** entra por `FisherLevelRules.AddXp`, o mesmo caminho da pesca (que passou a usá-lo): soma em
  `fisher_xp_total` (TD-038), sobe níveis e paga os Dólares dos marcos.
- **Save v13**: a migração da v12 cria a Tripulação vazia com o cursor em 0; o serviço o põe em "agora" ao carregar,
  então o tempo de antes da atualização não é pago.

**Por quê.** É o modelo dos idles do gênero (AdVenture Capitalist, Idle Miner): custo geométrico, marcos que dobram,
renda contínua. Renda por relógio + cursor é o mesmo princípio da pesca (TD-019): o cliente nunca diz "ganhei X".
A fórmula fechada dá ×100 e Máx sem laço de compra e sem estouro.

**Rever se.** O proprietário quiser que os marcos também multipliquem o XP, um limite de unidades, ou a Tripulação
render enquanto o jogo está aberto mas parado por mais de 2 minutos (hoje isso conta como offline, com 100% nas 2
primeiras horas de qualquer jeito). As Melhorias (M24-T06) vão multiplicar a renda: entram em `CrewRules.CoinsPerSecond`.

## TD-040 — Melhorias: ids gerados, nível por id no save e multiplicadores aplicados onde a renda é calculada

> **Retirado por TD-053 (12/10/2026, A-172):** as Melhorias (da Tripulação e gerais) saíram do jogo junto com a
> Tripulação. `config/upgrades.json`, `GameService/Upgrades` e `PlayerSave.Upgrades` não existem mais; o preço de venda
> ao NPC não tem mais multiplicador. Esta decisão fica como histórico.

**Origem.** M24-T06 (A-155): Melhorias compradas com Moedas, as de tripulante (×2, uma vez) e as gerais (com níveis).

**Decisão.**
- `config/upgrades.json` (novo arquivo obrigatório em `GameConfigLoader.RequiredFiles`, validado junto com o
  `crew.json`). As Melhorias de tripulante **não são listadas uma a uma**: o arquivo tem as linhas (`tiers`:
  quantidade que libera, fator de preço, nome) e o complemento do nome de cada tripulante; o jogo gera uma por
  tripulante e por linha, com id `crew_NN_up_K` (`UpgradeRules.CrewUpgradeId`). Um tripulante novo no `crew.json`
  ganha as dele sozinho (o validador pede o complemento do nome). As gerais têm id próprio e um `effect` de uma lista
  fechada (`UpgradeEffects`: `crew_coins`, `fish_sale`, `crew_offline_hours`, `crew_xp`, `fishing_coins`); o
  validador recusa efeito desconhecido e id geral igual a um id gerado.
- **Save v14**: `PlayerSave.Upgrades.Levels`, nível por id (a de tripulante fica 1 quando comprada). Só ids e níveis:
  nomes, preços e efeitos vêm do arquivo, então um ajuste vale para o que já foi comprado. Nível acima do
  `max_level` atual conta só até o máximo; id que saiu do arquivo fica guardado e é ignorado. A migração da v13 cria
  a lista vazia.
- **Regras puras** em `GameService/Upgrades/UpgradeRules.cs` (preço em `double` arredondado para `long`, com
  `Unaffordable` acima de ~9,2 × 10^18, como a Tripulação; efeitos como multiplicadores 1 + Σ nível × valor). O
  serviço `LocalUpgradeService` (`IUpgradeService`: `GetUpgrades`, `Buy`) só confere e grava.
- **Onde os efeitos entram:** `CrewRules.MemberCoinsPerSecond / CoinsPerSecond / XpPerSecond` recebem o
  `UpgradesState` (null = nenhuma; os testes e simuladores antigos continuam valendo); `CrewRules.OfflineSeconds`
  recebe as horas a mais da Caixa Térmica. Na pesca, `CatchRules.SalePrice` ganhou um multiplicador (1 por padrão):
  a Caixa usa Freguesia × Maré Boa (`UpgradeRules.BoxSaleMultiplier`) no cartão, na prévia e na venda; o Aquário usa
  só a Freguesia; o Mercado (`MarketRules`) continua chamando sem multiplicador, com o preço base.
- **Comprar credita a Tripulação antes** (`LocalCrewService.SettleNow`, interno): o que ela rendeu até ali fica na
  taxa antiga, como ao contratar, e as Moedas que ela rendeu já podem pagar a compra.
- A venda da Caixa passou a somar com teto em `long.MaxValue` (antes podia estourar com preços multiplicados).

**Por quê.** Gerar as 50 Melhorias de tripulante a partir de 5 linhas deixa o arquivo pequeno e impossível de ficar
torto (um tripulante sem Melhoria, um preço fora da curva). Aplicar os multiplicadores dentro das regras que já
calculam a renda mantém uma só fonte de verdade: a janela, a barra, o "Bem-vindo de volta" e o servidor futuro veem o
mesmo número. Guardar nível por id é o mesmo princípio da Tripulação (TD-039): o save guarda o que o jogador fez, o
arquivo diz quanto vale.

**Rever se.** O proprietário decidir o OD-056 (Maré Boa e Freguesia se sobrepõem), quiser Melhorias que mexem em
outra coisa (por exemplo XP da pesca, renda offline da pesca) — basta um efeito novo em `UpgradeEffects` e no ponto de
cálculo — ou quiser Melhorias de tripulante diferentes por tripulante (aí as linhas viram uma lista por tripulante).

## TD-041 — Níveis até o Nv.100: curva com dois pontos, preço geométrico e mapa dos níveis antigos

**Origem.** M24-T13 (A-156): varas, barcos e peixes passam a ir até o Nv.100, começando com pequenas porcentagens.

**Decisão.**
- **Curva com dois pontos em vez de tabelas.** As varas não guardam mais 10 valores por eixo e 9 custos: guardam o
  bônus no Nv.1 (`bonuses_at_level_1`, o de antes), o do nível máximo (`bonuses_at_max_level`, 3× o Nv.10 de antes) e
  um expoente comum (`upgrade_rules.bonus_curve_exponent`, 1,2). O bônus no nível N é
  `Nv.1 + (máx − Nv.1) × ((N − 1) ÷ (máx − 1)) ^ expoente` (`Config/LevelCurve.Value`). Os barcos com nível usam a
  mesma forma (`catch_success_bonus` e `catch_success_bonus_at_max_level`, `equipment.json → boat_levels`) e o peixe
  também (`fish_level.stat_bonus_at_max_level_percent`, 0 no Nv.1). Os números são gerados e explicados por
  `tools/Progressao/niveis_100.py`; o mesmo cálculo está em `tools/Progressao/curva_niveis.py` para as simulações.
- **Preço geométrico arredondado.** `upgrade_cost` (vara e barco): `coins_first × coins_growth ^ (N − 1)` Moedas e
  `shells_first × shells_growth ^ (N − 1)` Conchas de N para N + 1 (`LevelCurve.Cost`), arredondado para o inteiro
  abaixo de 1.000 (mínimo 1) e para 3 algarismos acima ("12.300", não "12.347"), com `LevelCurve.Unaffordable` se não
  couber num `long`. O validador recusa crescimento fora de 1–10 e último nível acima de 10^18.
- **O XP do peixe continua tabela** (`fish_level.xp_table`, 99 linhas), gerada pela fórmula registrada em
  `xp_curve_generator`, como a do Pescador: o carregador, o Painel e a migração já trabalham com ela.
- **Migração por mapa no config.** Save v15: `BoatLevels` (nível por id; ausente = Nv.1) e `RodLevelCurveVersion` /
  `FishLevelCurveVersion`. A migração da v14 grava 1 nas duas; o `GameSession` chama
  `Persistence/LevelCurveMigration.MoveToCurrentCurves`, que leva cada vara (Inventário, anúncios, leilões, Itens a
  Retirar) ao nível de `legacy_levels_v1` da vara e cada peixe (Aquário e Mercado) ao de `fish_level.legacy_levels_v1`,
  reaplicando o XP que ele tinha dentro do nível; nunca baixa um nível. `legacy_levels_v1[i]` é o menor nível novo cujo
  bônus é ≥ o do nível antigo i + 1 em **todos** os eixos (calculado pelo script a partir das tabelas antigas, que
  ficam guardadas em `docs/propostas/niveis_100.json → antes`). Jogador novo nasce com as curvas atuais (0 também
  significa "nada a mover").
- **O barco inicial não tem nível** (`has_levels: false`): o bônus dele é 0 e 3 × 0 é 0; um nível que não muda nada
  só confundiria.
- **Números que dependiam do nível 1–10** foram levados ao nível equivalente pelo mesmo mapa: adversários da Arena
  (`top_fish_level` 10 → 41), peixes dos vendedores do Mercado (`max_fish_level` 6 → 26, prêmio 0,15 → 0,0337 por
  nível, para o Nv.41 valer o que o Nv.10 valia), varas dos vendedores (`market_bots.json → supply.max_rod_level`,
  novo, 41) e o aviso de comida valiosa (`min_level` 2 → 8).

**Por quê.** Dois pontos e um expoente escalam a 100 (ou 1.000) níveis sem listas gigantes escritas à mão, mantêm o
Nv.1 de hoje exato e deixam o proprietário mexer em três números por vara no Painel. O mapa dos níveis antigos no
config segue o princípio da TD-038: a conversão depende do balanceamento, então fica no `GameSession`, e o
`SaveMigrations` só muda a forma do arquivo. Converter pelo bônus (e não pelo nível) garante que ninguém fica mais fraco.

**Rever se.** O proprietário quiser níveis com custo também para o peixe (OD-057), uma curva por vara, ou se a força
extra das varas e barcos no Nv.100 adiantar demais o Pescador (a simulação mostra o Nv.1000 em ~50 dias em vez de
~70 com o jogo aberto e a Tripulação, e ~54 em vez de ~75 só pescando; OD-058).

## TD-042 — Barcos por fragmentos e Passe do Pescador: sorteio na mesma tentativa, estado mínimo no save e Dólares antigos marcados na migração

> **Atualizado por TD-051 (12/10/2026, A-168):** não vale mais "um barco por mapa". São 5 barcos; cada um cai em dois
> mapas (`map_id` + `extra_fragment_maps`) e o validador confere "cada mapa dá um barco só". O resto desta decisão continua.

**Origem.** M24-T16 e M24-T17 (A-157 e A-158): os barcos passam a vir de fragmentos que caem pescando (1 barco por
mapa) e entra o Passe do Pescador, com marcos a cada 10 níveis e Dólares que antes vinham sozinhos (A-111).

**Decisão.**
- **Fragmentos no `equipment.json`, por barco.** Cada barco (menos o inicial) tem `map_id`, `fragments_required` e
  `fragment_drop_chance`; `GameConfig.TryGetBoatForMap` acha o barco de um mapa. O validador pede um mapa que exista,
  um barco por mapa e o barco inicial sem mapa. `cost_coins` / `cost_shells` saíram dos barcos e `IGearService.BuyBoat`
  saiu da interface (com o erro `BoatAlreadyOwned`): não existe mais caminho para comprar barco.
- **Sorteio na mesma tentativa, depois da captura.** `Shop/BoatFragmentRules.RollOnCatch` usa o `Rng` do ciclo
  (`Rng.For(seed, sessão, ciclo)`) depois que `CatchRules.Attempt` já usou o dele: a captura é a mesma de antes, o
  ciclo continua idempotente e o mesmo seed dá os mesmos fragmentos. Só peixe puxado sorteia; a pesca offline sorteia
  quando `boat_fragments.drop_on_offline_fishing` é verdadeiro; o peixe achado em Expedição (`AddFoundCatch`) e o dado
  pelas ferramentas de teste não passam por `Attempt` e não sorteiam.
- **Montar é automático** (`BoatFragmentRules.Add`): com `fragments_required` o barco entra em `OwnedBoatIds` (Nv.1,
  ausente em `BoatLevels`), os fragmentos são gastos e ele vira o barco em uso só se o bônus dele no Nv.1 passar o do
  barco em uso no nível atual. O `GameSession` monta ao carregar o que já tem fragmentos suficientes (um config que
  baixou `fragments_required`). Fragmentos depois de montado continuam somando em `BoatFragments` (extras, OD-060), para
  qualquer uso que o proprietário decidir valer também para o que já caiu.
- **Passe em `config/pass.json`** (novo arquivo obrigatório em `GameConfigLoader.RequiredFiles`, validado): `pass_id`,
  `premium_price_dollars` e a lista de marcos (nível, recompensa `free` e `premium`, cada uma com `coins`, `shells`,
  `dollars`, `boat_id` e `fragments`). Gerado por `tools/Progressao/passe_fragmentos.py` a partir de uma simulação de
  referência da economia do M24. Serviço `Pass/LocalPassService` (`IPassService`: `GetPass`, `IsMilestone`, `Claim`,
  `ClaimAll`, `BuyPremium`).
- **Save v16**: `BoatFragments` (por barco) e `Pass` (`PassId`, `Premium`, `PremiumBoughtAtMs`, `FreeClaimed`,
  `PremiumClaimed`, `LegacyDollarsUpToLevel`). Só o que o jogador fez: as recompensas vêm do arquivo, então um ajuste
  vale para os marcos ainda não resgatados.
- **Dólares nunca em dobro.** `fisher.dollars_per_levels` saiu do `progression.json` e `FisherLevelRules` não paga mais
  Dólares ao subir de nível (nem na troca de curva, TD-038). A migração da v15 grava `LegacyDollarsUpToLevel = mín(nível,
  100)`: a regra antiga (10 a cada 10 níveis até o Nv.100) já tinha pago esses marcos, então o resgate grátis deles não
  paga os Dólares (paga o resto). O limite 100 é a regra de todas as versões antigas, por isso fica na migração (que não
  lê o config). Um save ainda na curva antiga é migrado antes da troca de curva: os marcos que ele cruza ao subir nunca
  foram pagos e pagam no Passe.
- **Temporadas: só o `pass_id`.** O config e o save guardam o id do passe; nada muda quando ele muda. É o mínimo para a
  decisão futura (OD-059) não precisar mexer no formato do save.

**Por quê.** O sorteio no mesmo `Rng` do ciclo mantém o princípio da TD-019 (o cliente nunca diz "ganhei X") e não muda
nenhuma captura de quem já joga. Guardar só ids e listas de níveis segue a TD-039/TD-040. Marcar os Dólares antigos na
migração atende ao pedido do proprietário (o total até o Nv.100 continua 100) sem precisar saber quanto cada jogador
recebeu de fato.

**Rever se.** O proprietário decidir os extras (OD-060), as temporadas (OD-059) ou quiser um botão "Montar"; ou se os
fragmentos do Premium deixarem os barcos fáceis demais (OD-062).

## TD-043 — Mais peixes, menos raros: ciclo na metade e um multiplicador de mordida por raridade aplicado sobre a chance final

> **Atualizado por TD-045 (12/10/2026, A-160):** o ciclo passou para 10 s online e 20 s offline (15 s com o VIP) e o
> multiplicador de mordida de 0,35 para 0,233333 (0,35 × 10/15). A forma de aplicar o multiplicador continua a desta decisão.

**Origem.** M24-T22 (A-159): o proprietário pediu o dobro de peixes por hora e ~70% dos raros por hora de antes, sem
mexer no valor nem no XP de cada peixe.

**Decisão.**
- **Quantidade pelo tempo do ciclo**, não pela chance de puxar: `progression.json → fishing.online_cycle_seconds` 30 → 15
  e `offline_cycle_seconds` 60 → 30. Cada tentativa continua igual (mesma chance de puxar, mesmo teto de 95%, mesmo
  `Rng.For(seed, sessão, ciclo)`), o que é o mais limpo: os ciclos, o cursor salvo e a pesca offline já eram contados
  pelo config, e nenhum código mudou para isso. `LocalFishingService.MaxCyclesPerSettle` (10.000) continua acima das
  2.880 tentativas de um dia offline.
- **Raridade por um número por raridade**, `rarity.tiers[].fishing_bite_multiplier` (0,35 no Raro, Épico, Lendário e
  Mítico; 1 quando ausente; ignorado no Comum), aplicado em `CatchRules.FishingWeights` **depois** dos pesos do mapa e
  do bônus de raridade da vara: cada espécie não comum fica com exatamente multiplicador × a chance que tinha, e os
  Comuns dividem o resto na proporção deles (o total dos pesos não muda). Mudar os pesos do `maps.json` foi descartado:
  o efeito dependeria da proporção de raros no mapa e na vara (com a Vara 5 no Nv.100 os raros são 84% das mordidas no
  Mapa 10 e um peso × 0,35 daria ~150% dos raros por hora, não 70%), mexeria nas Expedições e nos vendedores do Mercado,
  e espalharia o ajuste por 10 mapas. Com o número sobre a chance final, o resultado é exato em qualquer mapa e com
  qualquer vara, barco, isca e Melhoria (barco e isca só mexem na chance de puxar, que não muda).
- **Só na pesca.** `CatchRules.Attempt` (online e offline) usa `RollBite`; `Roll`/`RollFound` (Expedições, vendedores do
  Mercado, simulador de mordidas antigo) continuam com `RollSpecies`, o sorteio de antes. O sorteio gasta o mesmo único
  `NextDouble`, então a sequência do `Rng` de cada ciclo não muda de formato.
- **Enciclopédia** usa `CatchRules.BiteShare` (o mesmo `FishingWeights` sobre o pool inteiro do mapa, sem vara). A coluna
  "Chance" do mapa no Painel de Balanceamento continua mostrando o peso puro; o multiplicador ganhou uma coluna na aba
  Raridades.
- **Pool sem Comuns**: nada para preencher o resto, então `FishingWeights` devolve os pesos como estão (não existe no
  config de hoje; evita dividir por zero).
- **Fragmentos com 4 casas.** `tools/Progressao/passe_fragmentos.py gerar` grava `fragment_drop_chance` com 4 casas (com
  3, chances abaixo de 1% erravam até ~10%) e recalcula pelo alvo de dias (A-157), em vez de dividir por 2 à mão: com
  menos raros, mais tentativas viram peixe puxado, e a metade exata deixaria os barcos até ~16% mais rápidos (nos mapas com mais raros).
- **`config/pass.json` igual ao script.** Regerado pelo `gerar`; o arquivo anterior tinha números do Nv.300 em diante que
  o script não produzia.
- **Saídas dos scripts em PT-BR.** `passe_fragmentos.py`, `simular_melhorias.py`, `simular_tripulacao.py` e
  `niveis_100.py` imprimem números com vírgula decimal (antes alguns imprimiam dicionários do Python com ponto).

**Por quê.** O pedido fala em "peixes por hora" e "raros por hora": medir e garantir pela chance final é o que torna as
duas metas verdadeiras ao mesmo tempo em todos os cenários, e um número por raridade deixa o proprietário ajustar uma
raridade sem mexer nas outras.

**Rever se.** O proprietário decidir mexer nos ganhos do fim do jogo (OD-063) ou nos tamanhos especiais por hora
(OD-064); ou se o multiplicador passar de 1 (os Comuns podem chegar a 0; o código aceita, mas o resto deixa de somar 1
na proporção pedida).

## TD-044 · Verificação automática no GitHub (11/10/2026)

**Contexto.** O ambiente onde o código é escrito não tem .NET, então mudanças grandes iam para o repositório sem
compilar. Um erro de compilação (CS1501 na Tripulação) chegou ao proprietário e o Unity não abriu o projeto.

**Decisão.** `.github/workflows/verificar.yml` roda, a cada envio, o mesmo que `ops/scripts/verify.sh`: testes das
regras, compilação do jogo e do Painel contra as bibliotecas de referência do Unity 2021.3 e compilação do Simulador.
Cada etapa roda mesmo que outra falhe. O log e o resultado são gravados na branch `ci-resultados` (arquivos
`verify.log`, `resultado.txt` e `commit.txt`), para serem lidos com `git fetch` sem precisar da API do GitHub.

**Custo.** Gratuito dentro da cota do GitHub Actions (cerca de 1 a 2 minutos por envio).

**Regra.** Uma mudança só é dada como pronta depois que o `resultado.txt` do seu commit disser `success`.


## TD-045 · Ciclo de 10 s, ciclo offline do VIP e o multiplicador proporcional ao ciclo (12/10/2026)

**Origem.** M24-T27 (A-160): pesca a cada 10 s online e 20 s offline, 15 s offline com o VIP, menos raros por peixe e a
Caixa de Pesca para 1 dia de pesca online.

**Decisão.**
- **Ciclo do VIP no config do VIP**: `economy.json → vip.offline_cycle_seconds` (15; 0 ou ausente = o VIP não muda o
  ciclo; validado ≥ 1 quando presente). Fica junto do preço, da duração e do bônus de XP do VIP, e não em
  `progression.json → fishing`, porque é um benefício do VIP.
- **`CatchUpOffline` anda tentativa por tentativa** em vez de dividir o tempo por um ciclo fixo: cada tentativa usa o
  ciclo do VIP quando `VipRules.IsActive` vale no fim dela (o mesmo teste do bônus de XP, que é conferido na hora de
  cada peixe), e o ciclo normal depois. Assim um VIP que acaba no meio da ausência conta 15 s só até o fim, sem
  tentativa parcial, e o XP do VIP e o ciclo do VIP valem para os mesmos peixes. O limite de 24 h e o
  `MaxCyclesPerSettle` (10.000, acima das 5.760 tentativas de um dia com VIP) continuam. A sequência do `Rng`
  (`Rng.For(seed, sessão, ciclo)`) não mudou de formato. `OfflineReport` ganhou `VipCycleSeconds` e `VipAttempts`, e
  `VipView` ganhou `OfflineCycleSeconds` e `NormalOfflineCycleSeconds`, para as telas mostrarem os dois ritmos.
- **Multiplicador proporcional ao ciclo**: `fishing_bite_multiplier` = 0,35 × 10/15 = 0,233333 (6 casas; a diferença
  para 7/30 é de 0,0001% dos raros). Como o multiplicador é aplicado sobre a chance final (TD-043), os raros por hora
  ficam exatamente iguais aos do ciclo de 15 s em qualquer mapa e equipamento.
- **Fragmentos e Passe pelo script**, como no TD-043: `tools/Progressao/passe_fragmentos.py gerar` recalcula a chance
  pelo alvo de dias e regera `config/pass.json` (o arquivo é exatamente a saída do script).
- **`simular_progressao.py --vip`** passou a simular também o ciclo offline do VIP.

**Por quê.** Medir pela chance final mantém a meta "mais peixes, os mesmos raros por hora" verdadeira em todos os
cenários; andar tentativa por tentativa no offline é o jeito mais simples de o VIP terminar no meio sem regras extras.

**Rever se.** O proprietário decidir sobre o bônus de XP do VIP junto com o ciclo mais rápido (OD-065), os ganhos do fim
do jogo (OD-063) ou os tamanhos especiais por hora (OD-064).

## TD-046 · Iscas antigas convertidas pelo valor, com a tabela antiga no config (12/10/2026)

**Origem.** M24-T30 (A-161): as três iscas novas (+5/+10/+15%, 999 tentativas) substituem as de antes (+3/+7/+10%, 180).

**Decisão.**
- **Mesmos ids** (`bait_01` a `bait_03`), com os números novos: o save, as telas, a arte (`Iscas/bait_0N`) e os testes
  continuam apontando para as mesmas chaves. O que muda o significado de um id é a **versão do modelo de iscas**:
  `equipment.json → bait_migration.bait_model_version` (2) e `PlayerSave.BaitModelVersion`.
- **Save versão 17**: `SaveMigrations` marca os saves antigos com `BaitModelVersion = 1` (só forma do arquivo); a
  conversão precisa do config e fica em `Persistence/BaitModelMigration.cs`, chamada pelo `GameSession` ao carregar
  (como `LevelCurveMigration`, TD-038/TD-041). Jogador novo nasce na versão do config (0 = nada a converter).
- **Os números antigos ficam no config** (`bait_migration.legacy_baits`: bônus, tentativas e preço de cada isca de
  antes), não no código: são só para a conta da conversão.
- **Conversão pelo valor em Moedas** em vez de devolver Moedas e Conchas: assim a isca em uso continua em uso (pedido),
  ninguém perde valor (arredonda para cima) e não aparece dinheiro que o jogador não pediu. As Conchas não entram na
  conta porque o preço em Conchas por tentativa subiu na mesma proporção (+17% a +19%) que o de Moedas (+11% a +20%).
  Alvo = a isca de hoje com bônus igual ou o menor acima (a maior, se nenhuma for maior); várias antigas que vão para a
  mesma nova somam.
- **Todo nível custa Conchas**: o validador passou a recusar `upgrade_cost.shells_first` menor que 1 (varas e barcos).

**Por quê.** A conversão por valor é a mais simples que respeita "a isca equipada continua equipada" sem dar nem tirar
nada; guardar a tabela antiga no config segue a regra de nenhum número de balanceamento no código.

**Rever se.** As iscas mudarem de novo (aumentar `bait_model_version` e trocar `legacy_baits` pelas de então) ou o
proprietário preferir devolver o valor em Moedas e Conchas.

## TD-047 · Curva do Pescador calibrada pelo XP acumulado no tempo, com o começo fixo (12/10/2026)

**Origem.** M24-T31 (A-162): "60 dias pro lvl 1000", sem mexer no começo (Nv.10 em ~20 min, Nv.100 em ~0,5 dia).

**Decisão.**
- `tools/Progressao/calibrar_xp.py` passou a medir com `simular_melhorias.py` (pesca, Tripulação, Melhorias, níveis de
  vara e barco, fragmentos, iscas), perfil "sempre aberto", **sem Passe e sem VIP**. O jogador de referência fica
  registrado em `progression.json → fisher.calibration_target`.
- `--manter-ate 100`: os níveis abaixo dele são copiados da tabela atual; o dia em que o jogador chega a ele é medido.
  Do nível mantido em diante, o dia planejado de cada nível sai de uma interpolação monotônica (log do tempo × log do
  nível) pelas metas.
- Em vez de "XP por tentativa × tentativas" (o método do A-153, que só via a pesca), a simulação mede o **XP acumulado
  no tempo**, X(t), com a tabela estendida além do Nv.1000 (para o jogador não parar de ganhar XP), e cada nível pede
  X(dia planejado do seguinte) − X(dia planejado dele). Depois do Nv.90 a renda não depende do nível do Pescador, então
  uma rodada já acerta; o script repete até o erro nas metas ficar abaixo de 0,5%. Suavização e arredondamento iguais
  aos de antes; a suavização usa os níveis mantidos como vizinhos, para não criar degrau na emenda.
- `xp_curve_version` 2 → 3: a conversão do TD-038 não mudou e já cobre 1 → 3 e 2 → 3 (o XP total está no save).

**Por quê.** A Tripulação rende XP por segundo e cresce com o tempo, não com as tentativas de pesca: medir o XP total no
tempo é o que acerta o calendário com a economia de hoje. Manter os níveis do começo garante que as primeiras sessões não
mudam.

**Rever se.** Entrar no perfil de referência o Passe ou o VIP, ou uma fonte de XP que dependa do nível do Pescador
(aí a medida precisa de mais rodadas, que o script já faz). Uma curva nova sempre sobe `xp_curve_version` e regera o
`pass.json` (as Moedas e Conchas dos marcos seguem a renda de quem chega a cada nível).

## TD-048 · Varas e roupas por fragmentos: mapa da vara validado contra a trava, roupas sem nível pago e sorteios depois do barco (12/10/2026)

**Origem.** M24-T32 e M24-T33 (A-163 e A-164): as varas saem da Loja e vêm de fragmentos; entram 5 roupas montadas com
um fragmento só, com bônus por peça e nível por uso.

**Decisão.**
- **Varas no `rods.json`, como os barcos no `equipment.json`.** `acquisition.method` passa a ser `fragments` (ou
  `free_claim_in_shop`, a Vara Inicial); cada vara tem `fragment_map_id`, `fragments_required` e `fragment_drop_chance`,
  e `rods.json → fragment_rules.drop_on_offline_fishing`. `purchase_cost_coins/shells` saíram (o método `coin_purchase`
  é recusado); o valor de referência no Mercado e na revenda virou `acquisition.market_reference_coins` (o preço de antes),
  para o Mercado não mudar. `GameConfig.IsPurchasable` saiu; `IsFragmentRod` / `IsFreeClaimRod` e `TryGetRodForMap`
  entraram. `IShopService.BuyRod` só resgata a Vara Inicial (as outras: `RodNotForSale`).
- **A progressão nunca trava, conferido pelo validador** (`ValidateRodFragments`): o mapa de cada vara pede categoria menor
  que a dela, um mapa dá no máximo uma vara, e, partindo da Vara Inicial, cada vara pode ser montada com uma vara que o
  jogador já consegue ter e todo mapa pede uma categoria alcançável. Assim um ajuste no `maps.json` ou no `rods.json` que
  travaria o jogo nunca chega a ele. O script `fragmentos_equipamento.py` escolhe o último mapa que a vara anterior alcança.
- **Sorteio na mesma tentativa, depois da captura e do barco** (como a TD-042): `RodFragmentRules.RollOnCatch` e
  `ClothingRules.RollOnCatch` usam o `Rng` do ciclo depois do `BoatFragmentRules.RollOnCatch`. A captura e o fragmento de
  barco de quem já joga não mudam; cada sorteio só acontece quando existe o que sortear (mapa com vara, config com roupa).
  `LocalFishingService` passou a ler a vara em uso a cada tentativa (antes uma vez por `Settle`), porque uma vara montada
  no meio da pesca entra em uso na hora; sem vara nova, a sequência é a mesma.
- **Montar a vara é automático** (`RodFragmentRules.Add`): vira um `InventoryItem` no Nv.1 com `PurchasePriceCoins` 0 e
  entra em uso se a categoria for maior. "Ter a vara" conta Inventário, anúncios, leilões e Itens a Retirar (a regra que
  a Loja usava, movida para `RodFragmentRules.Owns`); fragmentos com a vara na mão são extras e remontam a vara se ela for
  vendida. O `GameSession` monta ao carregar o que já tem fragmentos suficientes.
- **Roupas no `equipment.json → clothing`** (sem arquivo novo): chance do fragmento, custos por ordem de montagem
  (`fragments_to_assemble`), curva do bônus (`max_level`, `bonus_curve_exponent`, a mesma `LevelCurve.Value` das varas),
  XP de uso (`use_xp_per_fish`, `level_xp.first/growth`, com `LevelCurve.Cost`) e as peças com `bonus_axis` de uma lista
  fechada (`ClothingAxes`: `catch_success`, `rarity_efficiency`, `size_quality`, `shell_yield`, `fisher_xp`).
- **Onde o bônus entra:** as Luvas somam ao `gearBonus` da tentativa (como barco e isca, com o mesmo teto); Chapéu, Botas
  e Camiseta somam aos bônus da vara em `CatchRules.Attempt` (`CatchRules.WithClothing`, que devolve o mesmo objeto sem
  roupa, então os sorteios de quem não tem roupa são idênticos); a Calça soma +% do XP do peixe em `ApplyCatch`
  (arredondado, só para peixe pescado; Expedição e ferramentas de teste não). `GearView.ClothingBonus` entra no
  `TotalBonus`, então a chance mostrada na Loja, no Perfil e no HUD já conta as Luvas.
- **Nível por uso = peixes puxados**, online e offline, 1 ponto por peixe para cada peça montada (`ClothingRules.GainUse`),
  sem custo. Mais simples que tempo pescando: não precisa de outro relógio e não conta o tempo com a Caixa cheia.
- **Save versão 18**: `RodFragments` (por id de vara) e `Clothing` (`Fragments` e `Pieces` com `Level`, `Xp` e
  `AssembledAtMs`). Só o que o jogador tem; nomes, bônus e XP por nível vêm do config. A migração da v17 cria os dois vazios;
  as varas do Inventário continuam com o nível.
- **Calibração em `tools/Progressao/fragmentos_equipamento.py`**: chance de cada vara = fragmentos ÷ peixes puxados no
  mapa dela até o dia em que o jogador de referência (4 vezes por dia) comprava a vara antes (medidos com a chance quase
  zero, sem montar); Fragmento de Roupa para as 5 peças em ~30 dias (4 vezes por dia); XP de uso para a 1ª peça no Nv.100
  em ~60 dias com o jogo aberto. `simular_progressao.py` ganhou as varas por fragmentos, as roupas e a pesca offline no
  mapa onde o jogo foi fechado (antes o jogador "viajava" no meio da pesca offline, o que adiantava quem abre pouco).
- **Avisos agrupados na tela** (`Game/UI/FragmentNotices`): no máximo um aviso por tipo (barco, vara, roupa, nível de
  cada peça) a cada 8 s; o que chega no meio é somado e mostrado no fim do intervalo. Só apresentação.

**Por quê.** Seguir o modelo dos barcos (TD-042) mantém uma regra só para "item por fragmento" e o princípio de que o
cliente nunca diz "ganhei X". A validação da trava protege a progressão de um ajuste de balanceamento, que é onde o erro
aconteceria. Bônus por eixo somados aos que já existem reaproveitam toda a matemática da pesca e o teto de 95%.

**Rever se.** O proprietário decidir sobre as varas no Mercado (OD-066), o efeito das roupas no Pescador (OD-067), a regra do
nível por uso (OD-068) ou os extras (OD-060); ou quiser escolher quando vestir/tirar uma peça (hoje ficam sempre vestidas).

## TD-049 · Boss Mundial: horário de Brasília fixo, ataques por índice no dia e Dólares atrás de uma chave online (12/10/2026)

**Origem.** M24-T29 (A-165): o proprietário pediu um Boss Mundial todo dia às 19:00, 10 minutos, ataque automático do
Cardume com o jogo aberto, recompensas por dano e Dólares do top 1 a 10.

**Decisão.**
- **Horário de Brasília fixo no config** (`world_boss.json → schedule.utc_offset_minutes` −180), não o fuso do PC: o
  evento é o mesmo para todos os jogadores (é "mundial") e o Brasil não tem horário de verão desde 2019. O dia do evento
  é `floor((utc + offset) ÷ 1 dia)`; o validador recusa uma janela que passe da meia-noite, para um evento pertencer a um
  dia só. As contas ficam em `WorldBoss/WorldBossRules` (puras, sem relógio).
- **Ataque por índice.** O ataque k acontece k × `interval_seconds` depois da abertura (1 a 200). O save guarda o último
  índice processado; `Update` credita do seguinte até o devido agora, no máximo `max_catch_up_attacks` de uma vez. O
  sorteio de cada ataque é `Rng.For(seed ^ sal, dia, k)`: processar de novo não muda nada e nada conta duas vezes.
- **"Só com o jogo aberto"** sem relógio novo: a primeira chamada de `Update` em cada execução do jogo só ancora o índice
  (campo em memória do serviço, zerado no reset do save). Assim o tempo fechado nunca vira ataque, mesmo que o save diga
  que o jogador estava no evento; o resto do anti-trapaça é o `SteadyClock` (TD-030).
- **Dano reaproveitando a Arena.** `BattleEngine` ganhou `RollDamage`, `AverageRoll` e `HitDamage` (a fórmula que estava
  dentro de `Resolve`, que agora chama as duas primeiras — a mesma sequência de sorteios, as batalhas não mudam). O Boss
  usa `HitDamage` com `boss_defense` e multiplica pelos golpes que a Velocidade dá no intervalo (`BattleEngine.Interval`).
- **Faixas relativas** (escolhido por ser o mais simples que vale do Nv.1 ao Nv.1000): `damage_percent` do dano máximo do
  dia, `reference_damage` = o maior "dano dos 10 minutos inteiros com o sorteio médio" entre os Cardumes usados no dia
  (nunca desce, para trocar de Cardume no meio não facilitar a faixa). A recompensa é em **minutos de renda** de uma tabela
  por nível (`reference_income`) gerada do `pass.json` por `tools/Progressao/boss_mundial.py` (conta simples, sem
  simulação longa: o Passe já é calibrado em minutos de renda).
- **Pagamento único**: `Paid` no registro do dia; pago no `Update` que passa das 19:10 (depois do último ataque) ou no
  primeiro da próxima abertura. Um registro só no save (`WorldBossState`, save versão 19), trocado pelo do dia seguinte
  depois de pago; é ele que a tela mostra como "último resultado".
- **Dólares do top 10 atrás de `ranking.online_rewards_enabled`** (falso no repositório): no jogo local o ranking só tem
  o jogador (A-100) e ele seria sempre o 1º. A regra (`WorldBossRules.DollarsFor`) já está pronta e testada com a chave
  ligada; com o servidor, o ranking e o pagamento passam a ser dele.
- **Cardume em Expedição não ataca**, a mesma trava da Arena (`AttackBlocker`), em vez de inventar outra regra.

## TD-050 · A tela de erro cobre a cena e cada quadro da interface (12/10/2026)

**Origem.** M24-T38 (A-166): o proprietário avisou que o jogo "não está abrindo" no Unity 6.3, sem a mensagem de erro. O
CI (TD-044) compila contra o Unity 2021.3 e passa os testes; o problema provável é de execução dentro do Unity.

**Decisão.**
- `GameRoot.Awake` monta a cena e o som dentro de `try/catch` e **sempre** acrescenta o `Hud`, que é quem desenha a tela
  de erro. Antes, uma exceção em `FishingScene.Build` interrompia o `Awake` antes do `Hud`: janela só com o fundo, sem
  explicação.
- `Hud.Start` (criação das janelas) e `Hud.OnGUI` (o desenho de cada quadro) ficam em `try/catch`; a exceção vai para
  `GameRoot.ReportFailure`, que para o serviço (`Game = null`, como o `Guard`) e mostra a tela de erro. `ExitGUIException`
  é relançada (é o fluxo normal do IMGUI). Uma exceção no `OnGUI` se repetiria a cada quadro e deixaria a tela pela
  metade; parar com o motivo é melhor do que um jogo meio desenhado. Se até o `UiSkin` falhar, a lista sai no estilo
  padrão do Unity (`DrawPlainError`).
- `GameRoot.Update` lê `Status` e `Travel` dentro do `Guard` (estavam fora).
- A tela mostra **tipo + mensagem, momento, os três primeiros métodos do jogo na pilha, o erro interno e a pasta do save**
  (`GameRoot.Fail`, textos em `GameTexts.Startup`): o suficiente para achar a falha por um print, sem o Console. Fica só a
  primeira falha (as seguintes vão só para o Console).
- APIs do Unity 6 revisadas no jogo e no Editor (`FindAnyObjectByType`, importadores de textura, áudio e fonte,
  `EditorGUILayout`, `EditorSceneManager`, `BuildPipeline`, `PlayerSettings`): nenhuma é obsoleta como erro no Unity 6;
  nada foi trocado.
- **Teste da abertura** (`StartupWithOldSavesTests`): saves das versões 11 a 18 com dados de jogador real (varas Nv.10 no
  Inventário, no Mercado e num leilão, iscas antigas, peixes Nv.10, barco comprado, Dólares antigos, Expedição e pesca em
  andamento, Tripulação) abertos 3 dias depois com a mesma sequência de chamadas do `GameRoot`, e um save novo. Precisam
  carregar (nunca descartar o save), não lançar exceção e abrir em menos de 2 s.

## TD-051 · Um barco com mais de um mapa de queda e a migração dos barcos removidos (12/10/2026)

**Origem.** M24-T42 (A-168): o proprietário voltou para 5 barcos; os mapas 6 a 10 dão fragmentos dos barcos 1 a 5.

**Decisão.**
- `equipment.json → boats[].extra_fragment_maps`: lista de `{map_id, fragment_drop_chance}` além do `map_id` principal.
  `map_id` + `fragment_drop_chance` ficam como estavam (o Painel de Balanceamento, o simulador C# e o
  `passe_fragmentos.py` continuam editando/gerando o principal); a chance dos mapas extras é a que o mapa tinha e o script
  não a muda. `GameConfig` monta um índice mapa → (barco, chance): `TryGetBoatForMap`, `BoatFragmentChanceOn(mapa)` e
  `FragmentMapsOf(barco)`. O sorteio (`BoatFragmentRules.RollOnCatch`) usa a chance do mapa. O validador continua com
  "cada mapa dá um barco só", agora contando `map_id` e `extra_fragment_maps`.
- **Migração pelo config, como a das iscas (TD de A-161):** os barcos removidos ficam em `equipment.json →
  boat_migration.removed_boats` (id, `fragments_go_to`, `fragments_required`, `upgrade_cost`), com
  `boat_model_version` 2. Save v20 marca os saves antigos com `BoatModelVersion` 1; `Persistence/BoatModelMigration`
  roda na abertura da sessão, **antes** de os níveis de barco serem limitados ao config (um barco desconhecido cairia
  para o Nv.1 e perderia o valor). Reembolso = soma do preço de cada nível do Nv.1 ao nível do barco, pela mesma
  `LevelCurve.Cost` (TD-041); os fragmentos entram por `BoatFragmentRules.Add`, que monta o barco e o põe em uso se
  for melhor (a regra de sempre). Um barco removido em uso → o melhor barco que o jogador tem (por bônus no nível dele).
- Os fragmentos que o barco removido custou para montar também voltam (como fragmentos do barco do mesmo mapa): sem
  isso, o esforço de montar um barco 6 a 10 se perderia; as Moedas e Conchas cobrem só os níveis.


## TD-052 · Revisão de integridade das regras e do save (12/10/2026)

> **Atualizado por TD-053 (12/10/2026, A-172):** sem as Melhorias, o piso dos bots para um peixe é o preço de venda ao
> NPC + 1 Moeda (`UpgradeRules.MaxFishSaleMultiplier` saiu). O objetivo é o mesmo: comprar de um bot e revender ao jogo
> nunca dá lucro. O resto desta decisão continua.

**Origem.** Revisão de integridade das regras, do config e do save de 12/10/2026.

**Decisão.**
- **Piso dos bots no Mercado e no Leilão.** O preço de um anúncio de bot e o lance inicial de um leilão de bot nunca
  ficam abaixo de `MarketRules.BotFloor`: para um peixe, o preço de venda ao NPC com a Freguesia na Feira no **nível
  máximo** (`UpgradeRules.MaxFishSaleMultiplier`, hoje ×2) + 1 Moeda; para uma vara, o valor de revenda + 1. Antes o
  piso ignorava as Melhorias, e comprar um peixe de bot e vendê-lo no Aquário dava lucro. Usar o máximo (e não o nível
  atual do jogador) fecha também o caso de comprar agora e melhorar depois; o custo é que os peixes de nível baixo dos
  bots ficam um pouco mais caros. Anúncios já criados num save antigo seguem até vencer (48 h).
- **Somas que não estouram.** `Core/SafeMath` (`Add` para long, `AddCount` para as cargas de isca em int): uma soma que
  passaria do topo para no topo, em vez de virar negativa (o validador recusaria gravar o save).
- **Save sem `save_version`.** Lido como versão 1 (`DefaultValue(1)` + `DefaultValueHandling.Populate`), então passa
  por todas as migrações em vez de pular todas. Um save novo continua nascendo na versão atual. O validador recusa
  anúncios de Conchas/Dólares com quantidade ≤ 0. As cópias postas de lado levam milissegundos no nome.
- **Sinal de relógio congelado.** O TD-030 continua (o tempo do jogo nunca volta). `GameSession.ClockFrozenSeconds` diz
  quantos segundos o tempo do jogo está à frente do relógio do PC, e `GameSession.ClockFrozen` fica verdadeiro acima de
  5 minutos (`ClockFrozenWarningSeconds`, um limite de aviso, não de balanceamento), para a tela avisar o jogador.
- **Sinal de roupa pronta.** `FishingUpdate.ClothingReady` (um fragmento desta atualização alcançou o custo da próxima
  peça) e `ClothingView.CanAssemble` (os fragmentos pagam a próxima peça): a regra sai da tela e fica no serviço.
- **Semente fixa nos testes.** `LocalGame.Start(..., rngSeed)` e `TestSupport.NewGame(seed:)` aceitam uma semente para um
  jogador novo; os testes do Mercado e do Leilão usam `TestSupport.FixedSeed` e escolhem leilões que cabem no orçamento
  (M24-T26: o teste falhava às vezes quando a semente sorteava leilões de vara acima dos 10 milhões).

## TD-053 · Tripulação e Melhorias retiradas: save v21 com reembolso pelo config e o novo piso dos bots (12/10/2026)

**Origem.** M24-T46 (A-172): o proprietário tirou a Tripulação e as Melhorias (as da Tripulação e as gerais) do jogo e
manteve a meta de ~60 dias até o Nv.1000.

**Decisão.**
- **Código e config.** Saem `GameService/Crew` (`CrewRules`, `LocalCrewService`, `ICrewService`), `GameService/Upgrades`
  (`UpgradeRules`, `LocalUpgradeService`, `IUpgradeService`), `config/crew.json` e `config/upgrades.json` (fora de
  `GameConfigLoader.RequiredFiles`), os modelos e a validação deles, os erros `CrewMember*`/`Upgrade*` e, na tela,
  `CrewWindow`, `UpgradesPanel`, a aba do menu, a parte do "Bem-vindo de volta", a renda/s da placa e
  `Format.PerSecond`/`Rate`. `CatchRules.SalePrice` e `CatchViews.Create` perdem o multiplicador (só o preço do config).
- **Piso dos bots** (`MarketRules.BotFloor`): `NpcValue + 1` para peixe e vara. Sem multiplicador de venda no jogo, esse
  é o máximo que o jogo paga por um peixe, e revender ao NPC o que se comprou de um bot continua sem lucro.
- **Save v21.** As propriedades `crew` e `upgrades` do arquivo continuam **legíveis** (`PlayerSave.RetiredCrew` e
  `RetiredUpgrades`, com `NullValueHandling.Ignore`), mas só as unidades e os níveis: o resto do estado antigo (cursor,
  frações, totais) é ignorado. A migração estrutural v20 → v21 só sobe a versão; as de v12 → v13 e v13 → v14 não criam
  mais nada. Um save v21 nunca tem os dois campos.
- **Reembolso pelo config, na abertura da sessão** (`Persistence/CrewRemovalMigration`, chamada em
  `GameSession.RepairAgainstConfig` antes da troca de curva): como as migrações das iscas e dos barcos (TD-051), os
  preços de antes ficam em `economy.json → crew_removal_refund` (tripulantes: id, `base_cost`, `cost_growth`; linhas
  das Melhorias da Tripulação: `unlock_count`, `cost_factor`; Melhorias gerais: id, `base_cost`, `cost_growth`,
  `max_level`), só para essa conta. Moedas devolvidas = b·(rⁿ − 1)/(r − 1) por tripulante com n unidades (o que a compra
  em fórmula fechada cobrava a partir de 0), + cost_factor × b × r^unlock_count por Melhoria de tripulante comprada (id
  `crew_XX_up_N`), + Σ base × growth^(k−1) de k = 1 ao nível de cada Melhoria geral (no máximo `max_level`). Cada parcela é
  arredondada como o jogo cobrava; tudo soma por `SafeMath.Add` (um save editado com unidades absurdas para em
  `long.MaxValue`, nunca vira negativo). Ids desconhecidos, contagens ≤ 0 e um config sem a seção não dão nada e não
  impedem a abertura (TD-050). Depois os dois campos viram `null` e o save é gravado: abrir de novo não paga de novo.
  Nada de Conchas (nenhum dos dois sistemas custava Conchas). O XP que a Tripulação deu fica (está em `fisher_xp_total`);
  a renda do tempo fechado desde o último cursor não é paga (o sistema não existe mais).
- **Aviso.** `GameSession.CrewRefund` (o `CrewRemovalChange` desta abertura, ou `null`) — o `GameRoot` mostra um aviso
  importante uma vez, com as Moedas devolvidas (`GameTexts.Startup.CrewRemovedRefund`/`CrewRemoved`).
- **Balanceamento.** O jogador de referência das simulações passou para `tools/Progressao/jogador.py` (só a pesca);
  `simular_tripulacao.py` e `simular_melhorias.py` saíram, e `calibrar_xp.py`, `passe_fragmentos.py`,
  `fragmentos_equipamento.py` e `niveis_100.py` usam o novo. A meta foi mantida pela **curva do Pescador versão 4**
  (`calibrar_xp.py --manter-ate 250 --metas 500:14.6d,750:33.3d,1000:60d --gravar`, calibrada sem roupas, OD-067), com
  a conversão de sempre (TD-038). `pass.json` (Moedas e Conchas dos marcos, `passe_fragmentos.py passe`, que não mexe
  nos barcos) e `world_boss.json → reference_income` (`boss_mundial.py`) foram regerados pela renda só da pesca.

**Por quê.** Guardar os preços no config (e não no código) segue a regra de que todo número mora em `/config` e o
precedente das iscas e dos barcos. Ler os campos antigos em vez de apagá-los às cegas é o único jeito de devolver o que
o jogador gastou. Mudar a curva do Pescador (e não o preço dos peixes ou dos níveis) mantém o resto da economia como o
proprietário aprovou; manter os níveis 1 a 249 iguais deixa sem mudança quem está no começo.

**Rever se.** O proprietário quiser uma renda passiva de novo (seria um sistema novo, decisão dele), ou quiser a meta
de ~60 dias também para quem abre o jogo 4 vezes por dia (hoje ~112 dias, eram ~108 com a Tripulação, que rendia com o
jogo fechado).

## TD-054 · Fragmentos em qualquer mapa: um sorteio por item e o validador por chance (13/10/2026)

**Origem.** M24-T47 (A-173): o proprietário decidiu que todos os fragmentos (os 5 barcos, as 5 varas e o Fragmento de
Roupa) caem em qualquer mapa, com 0,1% de chance cada.

**Decisão.**
- **Um sorteio por item, independente.** `BoatFragmentRules.RollOnCatch` e `RodFragmentRules.RollOnCatch` não recebem
  mais o mapa: percorrem `GameConfig.FragmentBoats` e `GameConfig.FragmentRods` (listas prontas na carga, em ordem de
  categoria) e fazem um `NextDouble()` por item com chance maior que 0, do `Rng` do mesmo ciclo, depois da captura. A
  ordem é fixa (barcos 1 a 5, varas 1 a 5, roupa), então a mesma semente dá os mesmos fragmentos; a captura nunca muda,
  porque cada ciclo tem o seu `Rng` (`Rng.For(seed, sessão, ciclo)`) e os sorteios vêm depois dela. Um item com chance 0
  não gasta sorteio. Os dois retornam listas (vários fragmentos no mesmo peixe são possíveis).
- **A chance fica em cada item** (`fragment_drop_chance` de cada barco, de cada vara e de `clothing`), todos 0,001. Uma
  chance global seria mais curta, mas os campos por item já existiam, o Painel já os edita e deixam o proprietário dar
  uma chance diferente a um item sem mudar código.
- **O vínculo mapa → fragmento saiu da regra.** `rods.json → fragment_map_id` e `boats[].extra_fragment_maps[].fragment_drop_chance`
  saíram (do config e dos modelos), com `GameConfig.TryGetRodForMap`, `BoatFragmentChanceOn` e `FragmentMapsOf`. O
  `map_id`/`extra_fragment_maps` dos barcos **ficou**, porque tem outro uso: o Boss Mundial dá fragmentos do barco do mapa
  mais alto liberado (`WorldBossRules.BoatFor` → `TryGetBoatForMap`), e o Passe segue a mesma ordem; `GameConfig.MapsOf`
  substitui `FragmentMapsOf`. As ofertas da Loja perderam `MapId`/`MapName`/`MapNames`; a tela diz "Caem em todos os
  mapas" quando `FragmentsRequired > 0`.
- **Validador.** A regra "a vara que libera o próximo mapa cai num mapa que o jogador já pesca" não faz mais sentido. Agora:
  cada vara por fragmentos com `fragments_required` de 1 a 1.000.000 e chance de 0 a 1, e a categoria que cada mapa pede
  (`minimum_rod_tier`) precisa ser a da Vara Inicial ou a de uma vara com chance **maior que 0** (senão "a progressão
  travaria"). Saíram as mensagens `RodMapUnknown`, `RodMapTooHigh`, `RodMapRepeated` e `RodUnreachable`. Os testes que
  precisam de "nenhuma vara cai" usam `TestSupport.NeverDrops` (1e-12), que nunca sorteia e mantém o config válido.
- **Simulação.** `simular_progressao.Rules` ganhou `boat_drops(map)`/`rod_drops(map)` (hoje: todos os itens em qualquer
  mapa), que `fragmentos_qualquer_mapa.py` troca pela tabela de antes para comparar, e `fish_past_max_level` (a pesca
  continua depois do Nv.1000, só para contar fragmentos). Os geradores (`passe_fragmentos.py gerar`,
  `fragmentos_equipamento.py gerar`) não escrevem mais as chances.

**Por quê.** Sortear cada item separado é o que o proprietário pediu ("0,1% para cada") e é o mais simples de explicar e
de testar. Manter o `map_id` dos barcos evita mudar o Boss e o Passe, que não fizeram parte do pedido.

**Rever se.** O proprietário escolher outra opção do OD-070 (chance maior para as varas ou menos fragmentos), ou quiser
uma chance só para tudo.

*Atualizado em 13/10/2026 (OD-070 resolvido, opção c; M24-T48):* as varas pedem 5 / 8 / 12 / 15 / 20 fragmentos, com a
mesma chance de 0,1%. Nada mudou no código do jogo: um save com mais fragmentos guardados que o número novo é montado na
carga por `RodFragmentRules.AssembleReady` (que já existia para "um config que baixou fragments_required"), gastando só
o número novo; o resto fica como extra (`FewerRodFragmentsTests`). O Passe **não** acompanha a queda: os fragmentos de vara
dele ficam fixos em `passe_fragmentos.py → PASSE_FRAG_VARAS` (3 / 4 / 6 / 7 / 9, os 15% de antes), porque a opção (c)
foi simulada e aprovada com eles; `FRAG_VARA_GRATIS` só vale para uma vara fora da tabela.

## TD-055 · Clãs: um serviço pronto para o servidor e os prêmios do ranking só online (13/10/2026)

**Origem.** M24-T49 (A-174): o proprietário pediu os clãs (criar ou entrar, 10 vagas e vagas compradas, +1% de XP,
Cardume do Clã com 1 peixe por membro, ranking diário com prêmios para o top 3). Respondeu: "pronto só para online" (o
ranking entre clãs e os prêmios ficam desligados no jogo local, sem clãs simulados), só Pescador Nv.100+ e criar e
comprar vagas com Dólares.

**Decisão.**
- **Um serviço como os outros.** `IClanService` / `LocalClanService` com intenções (`Create`, `Join`, `Leave`, `BuySlot`,
  `SetFish`, `ClearFish`) e `Update` chamado pela sincronização do jogo; regras puras em `ClanRules`. A versão remota
  terá a mesma interface sobre HTTP.
- **A lista de clãs mora no save no jogo local** (`ClanState.KnownClans`): é o lugar que o servidor vai ocupar. Hoje ela
  só tem o clã que o jogador criou; os testes põem clãs de "outros jogadores" nela para provar `Join`, o limite de vagas,
  o dono que sai e o ranking. Os membros de fora guardam nome, nível e a Força do peixe como o servidor informaria; o do
  jogador lê o peixe do Aquário na hora (a Força muda com o nível do peixe).
- **Força pela regra existente.** A Força de um peixe é a do Cardume (`CardumeRules.RawStrength` com os pesos do
  `arena.json`, mostrada com `CardumeRules.Display`), sem o bônus de Cardume completo (é um peixe só); a do clã é a soma
  com `SafeMath`. Nenhuma fórmula nova.
- **O peixe não é movido.** O Cardume do Clã guarda só o id do peixe (`ClanMemberRecord.FishId`); ele continua no Aquário
  e no Cardume pessoal. Quando ele deixa o Aquário (venda, alimento, Mercado, Leilão), `ClanRules.DropMissingFish` zera o
  id — chamado nesses pontos, no `Update` e na carga.
- **+1% exato.** O bônus de XP guarda a fração de ponto (`ClanState.XpCarry`) e a soma no próximo peixe, em vez de
  arredondar peixe a peixe como a Calça: arredondando, peixes de menos de 50 XP (os mapas do começo) dariam 0.
- **Ranking por dia de Brasília.** O dia fecha às 00:00 no fuso de `clans.json → ranking.utc_offset_minutes` (−180),
  pelo `IClock` (TD-030). O jogo local fecha o dia na primeira atualização depois da meia-noite e paga no máximo um dia
  (o último); o servidor vai fotografar a Força às 00:00 e pagar cada dia. Um clã sem peixe no Cardume (Força 0) não
  entra no ranking.
- **Prêmios só com `online_rewards_enabled`** (como o top 10 do Boss Mundial, TD-049): no jogo local só existe o clã do
  jogador, que seria sempre o 1º. Com a chave desligada o dia fecha sem ranking e sem pagamento (nada fica guardado para
  pagar depois). Os prêmios: Dólares, Moedas e Conchas em minutos da renda de referência do Boss Mundial
  (`world_boss.json → reference_income`, pelo nível de cada membro), Fragmentos de Roupa e fragmentos do barco do Boss
  (o do mapa mais alto liberado) — o mesmo pacote do Boss e do Passe, sem moeda ou item novo.
- **Save v22** com `ClanState`; um save antigo entra sem clã (TD-050: o jogo sempre abre).

**Por quê.** Separar a regra da tela e guardar a lista de clãs onde o servidor vai ficar deixa a troca para o online
pequena; reaproveitar a Força do Cardume e o pacote de prêmios do Boss evita inventar fórmula ou recompensa.

**Rever se.** O jogo ganhar servidor (ligar `online_rewards_enabled`, mover `KnownClans` para ele e fotografar o
ranking às 00:00), ou o proprietário mudar os números provisórios (OD-071).

## TD-056 · Genes: guardados no peixe, sorteados num fluxo próprio, neutros na migração (13/10/2026)

**Origem.** M24-T52 (A-176): o proprietário pediu genes para cada peixe ser único ("caçar exemplares melhores é o que
prendia no jogo").

**Decisão.**
- **Os genes são dado do peixe; os atributos continuam derivados.** `BoxCatch.Genes` (chave curta `"g"`, a Caixa é
  compacta) e `FishInstance.Genes` guardam os 4 valores (Vida, Ataque, Defesa, Velocidade). Os atributos nunca são
  gravados: `FishRules.Stats` multiplica cada um pelo seu `GeneRules.Multiplier` na mesma conta de espécie, raridade,
  tamanho e nível. Assim uma mudança do config (×0,90 a ×1,10) vale para todos os peixes que já existem.
- **Sorteio num fluxo próprio.** Os genes de uma captura vêm de `Rng.For(semente ^ sal dos genes, id da captura, 1)`
  (`GeneRules.RollForCatch`), não do `Rng` do ciclo. Por isso a espécie, o tamanho, as Conchas e os fragmentos de cada
  ciclo continuam exatamente os mesmos de antes, e o mesmo id sempre dá os mesmos genes (repetir um ciclo não sorteia de
  novo). Os peixes do Mercado e do Leilão simulados usam o fluxo do anúncio com índice 1; os da Arena, o da semente dos
  bots com o índice do peixe: nenhum outro sorteio muda.
- **Neutros na migração (16/16/16/16).** Save v23: a migração v22 → v23 só sobe a versão; ao abrir a sessão,
  `Persistence/GeneMigration.GiveMissingGenes` (precisa do config, como as outras conversões) dá
  `genes.migration_neutral_value` a todo peixe sem genes válidos — Caixa, Aquário (e por ele o Cardume e o Clã), anúncios,
  itens a retirar, leilões e notícias do Mercado. Ela só preenche o que falta, então rodar de novo não muda nada.
  Por que 16 e não um sorteio: sortear daria a quem já jogava peixes 5★ de graça e tiraria força de outros (o Cardume de
  hoje ficaria mais fraco sem o jogador fazer nada); 16 é o valor inteiro mais perto do meio (×1,003) e dá a mesma força
  de antes. Um conjunto quebrado (tamanho errado, fora de 0–31) também vira neutro, nunca erro nem bônus.
- **Qualidade e estrelas só no serviço.** `GeneRules.View` entrega o `GeneView` (genes, total, qualidade 0–1, estrelas,
  genes perfeitos, Exemplar perfeito, se celebra) em `CatchView.Genes` e `FishView.Genes`; a tela só desenha e ordena
  pelo que recebe.
- **Preço NPC intocado.** `CatchRules.SalePrice` não recebe genes: a economia não muda.

**Por quê.** Guardar só os genes (e não os atributos) mantém a regra "nada derivável é gravado" e deixa o balanceamento
em `/config`; o fluxo próprio evita que adicionar um sorteio mude todos os outros; os genes neutros respeitam o "o jogo
sempre abre, sem perda" (TD-050).

**Rever se.** O proprietário aprovar o Temperamento (OD-072) — ele entraria como mais um dado do peixe, com a mesma
migração neutra — ou se o jogo ganhar servidor (aí só o servidor sorteia os genes).

## TD-057 · Missões: progresso lido do save, contadores por eventos e conclusão na gravação (13/10/2026)

**Origem.** M24-T54 (A-177): o proprietário pediu 100 missões (90 de progressão e 10 sociais) com prêmios crescentes.

**Decisão.**
- **Um tipo de objetivo por chave técnica** (`Missions/MissionTypes`, 53 tipos) e os parâmetros em `config/missions.json`;
  o texto é montado por `GameTexts.Missions.Objective` a partir do tipo, do alvo e do nome que está no config. Assim o
  proprietário muda alvo, espécie ou prêmio no config sem mexer em código, e os nomes continuam vindo de `/config`.
- **Ler o estado quando o save já mostra; contar só o que ele não guarda.** `MissionRules` lê nível, Moedas, Enciclopédia
  (`SpeciesRecords.TimesCaught` também dá as capturas por espécie e por raridade), mapas, varas, barcos, roupas, Aquário,
  Cardume (com a Força pela mesma regra da tela do Cardume), Passe, clã e posição na Arena. Os eventos que o save não
  guarda (tamanho e estrelas de cada captura, isca usada, Dólares gastos, duelos, sequência, adversários, anúncios,
  compras, vendas, lances, leilões ganhos, ataques ao Boss, dias e melhor faixa, recordes batidos, visitas de perfil)
  viram contadores em `PlayerSave.Missions.Counters` (um dicionário de chave técnica → número, `SafeMath`), somados por
  `MissionTracker`, chamado só pelos serviços que fazem a ação. Um dicionário evita um campo novo no save para cada tipo.
- **Conclusão na gravação.** `GameSession.Persist` dispara `BeforePersist`; o serviço de missões olha só as missões ainda
  não concluídas e marca as que chegaram ao alvo antes de gravar. Toda mudança das regras grava o save, então nenhum alvo
  alcançado por um instante (as Moedas antes de uma compra) se perde, a conclusão vai na mesma escrita, e a tela não
  precisa perguntar a cada quadro. Concluída é para sempre (`Completed`); pago uma vez (`Claimed`, marcado antes de pagar).
- **Online-only por flag.** `online_only` no config; `LocalMissionService.Online` é falso, então essas missões mostram o
  progresso 0 e nunca concluem no jogo local. No local, jogadores simulados (Arena, Mercado, Leilão) contam como "outro
  jogador"; o Clã não tem outros membros locais, por isso as duas missões de Clã com outras pessoas são online-only.
- **Save v24 sem perda.** A migração v23 → v24 só cria o estado com `SeedPending`; ao abrir, `MissionMigration.SeedFromSave`
  (precisa do config, para as estrelas dos genes e a raridade das espécies) enche cada contador com o que o save antigo
  prova — nunca mais — e roda uma vez. Na primeira olhada, as missões que o estado cumpre ficam prontas sem aviso (só o
  número no atalho), para não encher a tela de avisos.
- **Prêmios por script.** `tools/Progressao/missoes.py gerar` usa o jogador de referência (4 vezes por dia, com o Passe
  grátis) e o nível em que cada missão deve ser feita para dar N minutos da renda da pesca (10 → 60) e o triplo em
  Conchas, com 2 algarismos significativos e nunca menos que a missão de cima; os Dólares ficam numa tabela do script.

**Por quê.** Ler o estado dá conclusão retroativa de graça e não duplica o que o save já sabe; contadores só para eventos
mantêm o save pequeno; concluir na gravação é simples, barato (a gravação já serializa o save inteiro) e não depende da
tela; a flag `online_only` deixa as missões sociais prontas para o servidor sem inventar nada no local.

**Rever se.** O jogo ganhar servidor (aí o servidor soma os contadores, conclui e paga; as missões online-only passam a
contar) ou o proprietário mudar os prêmios (OD-073) ou a lista.

## TD-058 · Montar pelo botão: somar fragmentos e montar são duas ações; mapas sem vara mínima (13/10/2026)

**Origem.** M24-T58 (A-180): o proprietário pediu que barco e vara não montem sozinhos, que cada um peça o anterior e que
os mapas abram só pelo nível.

**Decisão.**
- **`Add` só soma; montar é uma intenção do jogador.** `BoatFragmentRules.Add` e `RodFragmentRules.Add` (pesca, Passe,
  Boss, Clã) não montam mais nada; o ganho leva `Ready` (os fragmentos ficaram completos com este ganho) e
  `NeedsBoatName`/`NeedsRodName` (o anterior que ainda falta), para a tela avisar. Montar é `IGearService.AssembleBoat` e
  `IShopService.AssembleRod`, que conferem `AssembleBlocker` (o mesmo valor vai na oferta da Loja, para o botão) e gastam
  só `fragments_required`. Erros novos: `BoatAlreadyOwned`, `NotEnoughBoatFragments`, `BoatNeedsPrevious`,
  `NotEnoughRodFragments`, `RodNeedsPrevious`, `NotFromFragments`; a frase com o nome ("Precisa do barco X") vem de
  `GameTexts.Gear.NeedsBoat/NeedsRod` com o nome que a oferta traz (`PreviousBoatName`/`PreviousRodName`).
- **"O anterior" é o de categoria logo abaixo** (`Previous`, por `tier` no config), então a ordem acompanha o config sem
  campo novo. O barco anterior conta se é do jogador (`OwnedBoatIds` ou o inicial); a vara anterior conta só **no
  Inventário** (`InInventory`): anunciada, em leilão ou em Itens a Retirar não monta a seguinte. A regra de "já tenho"
  da vara (`Owns`, que inclui o Mercado) continua a mesma para não montar duas.
- **Montar não usa nem equipa.** Nada muda o barco em uso nem a vara equipada sem o jogador pedir (por isso montar não
  chama `Sync` da pesca). O jogador de referência das simulações usa o que monta, como faria um jogador atento.
- **Sem save novo.** Não há campo novo; ao abrir, `GameSession` não monta mais nada (saíram os `AssembleReady`). A única
  montagem automática que ficou é a da `BoatModelMigration` (`AssembleForMigration`), porque ali os barcos eram do
  jogador antes da conversão.
- **Mapas: o campo saiu.** `minimum_rod_tier` saiu do `maps.json` e de `MapConfig` (o loader ignora chaves a mais, então
  um config antigo ainda abre); saíram `RodTooWeakForMap`, `RodNotAllowedOnMap`, `MapView.MinimumRodTier/RodAllowed` e
  `RodItemView.AllowedOnCurrentMap`. O validador agora confere toda vara em todo mapa (`MapRodCatchesNothing`), e o
  `MapView.RaritiesRodMisses` dá à tela o que a vara em uso não pesca ali. Os geradores Python leem
  `m.get('minimum_rod_tier', 0)`; o Passe guarda a ordem antiga das varas numa tabela (`VARA_DO_MAPA`), para continuar
  dando os fragmentos de cada vara nos mesmos marcos.

**Por quê.** Separar "ganhar fragmento" de "montar" deixa a regra num lugar só (o serviço), sem estado novo, e o
servidor futuro faz o mesmo: soma ao pescar e monta quando o jogador pede. Tirar o campo em vez de zerá-lo evita que um
mapa volte a pedir vara por engano.

**Rever se.** O proprietário decidir vender fragmentos ou barcos (OD-074, junto com o OD-060), ou quiser a montagem de
volta automática como opção.

## TD-059 · Pipeline de pixel art: pasta ArtePixel, importação Point, PPU 20, câmera pixel própria e fontes OFL (19/10/2026)

**Origem.** A-182: o proprietário escolheu o Estilo 1 "ARC" (pixel art) para o jogo inteiro. Esta TD fixa como a arte
nova entra no Unity; o trabalho em si é o M25.

**Decisão.**
- **Pasta paralela `Assets/Resources/ArtePixel/`**, com as mesmas subpastas e nomes de `Arte/`. `ArtAssets.Texture`
  procura em `ArtePixel/` primeiro e cai em `Arte/` quando o arquivo não existe (o cache de "não existe" continua por
  caminho). Assim a troca é feita lote por lote, sem um dia em que metade do jogo some, e o código das telas não muda de
  nome de arquivo. A limpeza final (M25) apaga `Arte/` e o fallback.
- **Importação (`ArtImportSettings`, nova regra para `ArtePixel/`):** `FilterMode.Point`, `TextureImporterCompression.Uncompressed`,
  `mipmapEnabled = false`, Clamp, `npotScale = None`, sem `maxTextureSize` reduzido, `alphaIsTransparency`. Sobe
  `GetVersion()` para o Unity reimportar. A arte pixel é pequena (a maior folha recortada tem 384 × 256), então ficar sem
  compressão custa pouco e evita os blocos de compressão em cima dos pixels.
- **PPU 20 na cena:** a câmera continua com 10,8 unidades de altura (`orthographicSize` 5,4), e 216 ÷ 10,8 = 20. Os
  sprites da cena são criados com `pixelsPerUnit = 20` (em vez de "largura desejada em unidades"), então um pixel de
  arte é sempre um pixel da grade. As posições em unidades de `/config` e `Resources/Visual/*.json` continuam valendo;
  só as alturas e larguras de sprite passam a sair do tamanho do arquivo.
- **Câmera pixel própria, sem o pacote 2D Pixel Perfect:** a cena é renderizada numa `RenderTexture` de 216 px de
  altura (largura = 216 × proporção da janela, arredondada para par), `FilterMode.Point`, e desenhada na tela pela maior
  escala inteira; quando a altura da tela não é múltipla de 216, amplia pelo inteiro de cima e reduz com bilinear
  ("nítido-suave"), sem faixas pretas. A posição da câmera e de tudo que anda é arredondada para o pixel da grade. O
  projeto usa o pipeline padrão e quase nenhum pacote; o pacote traria uma dependência que o `verify.sh` não compila
  contra as referências do Unity, e o que ele faz aqui cabe em um componente pequeno.
- **Interface:** o IMGUI continua. Um helper de desenho pixel (`PixelDraw`) troca o `ScaleToFit` pela maior escala
  inteira que cabe (e pela versão `_p` quando nem 1× cabe), arredonda posições para o pixel do quadro e nunca gira
  sprites. Molduras, pílulas, barras e abas da pele ARC são texturas 9-slice geradas uma vez pelo código (Point), não
  arquivos.
- **Fontes OFL embutidas:** `PressStart2P-Regular.ttf` e `VT323-Regular.ttf` em `Resources/Fontes/`, com
  `OFL-PressStart2P.txt` e `OFL-VT323.txt`, como a Fredoka e a Nunito hoje. Importação com `Hinted Raster` para ficarem
  nítidas; os tamanhos mínimos estão na Bíblia (seção 7.1).
- **Recorte das folhas:** `tools/Arte/pixel_folhas.py` (numpy, scipy e Pillow, como os outros scripts de arte) lê o
  manifesto `tools/Arte/pixel_pedidos.json`, gerado junto com `docs/ASSETS_PIXEL.md`. O fator de ampliação é detectado
  pela coerência de fase das bordas de cor para cada período de 2 a 40 px (aceita fator fracionário e eixos
  diferentes, porque o ChatGPT nunca entrega a grade exata); a amostragem pega a cor mais comum do centro de cada
  bloco; uma paleta única por folha mantém a série consistente; o contorno só entra onde falta.
  `tools/Arte/pixel_folhas_teste.py` testa com folhas sintéticas (fator 7,82 com ruído e 6 × 12 esticado).

**Por quê.** Uma pasta paralela com fallback deixa o jogo inteiro funcionando durante a troca e não mexe em nome de
arquivo nenhum. PPU fixo e câmera de grade fixa são o que garante "um pixel de arte = um pixel da grade" em qualquer
resolução. Desenhar a pele da interface pelo código deixa a moldura nítida em qualquer escala e tira dezenas de
arquivos.

**Rever se.** O proprietário quiser mais detalhe na cena (subir a grade para 270 px de altura, 4× no 1080p) ou se a
VT323 não ficar legível em janela pequena (trocar o corpo de texto por outra fonte pixel OFL mais alta).

## TD-060 · Recorte da pixel art por forma, não por grade (20/10/2026)

**Origem.** As 77 folhas reais do ChatGPT mostraram que a TD-059 errava em dois pontos: a detecção do fator de
ampliação dava confiança 0 quase sempre (o ChatGPT não desenha numa grade de pixel de verdade; o "pixel" dele tem
~3,5 px e não casa com o tamanho pedido) e o corte por grade igual cortava desenhos que não ficavam centrados na célula
(o camurupim e o mero, o chapéu do pescador; o pescador avulso saiu esticado).

**Decisão.** `tools/Arte/pixel_folhas.py` passa a:
- achar cada desenho como **componente conectado** sobre o magenta (8 vizinhos); pedaços pequenos (bigodes, respingos,
  brilhos, a isca do peixe-dragão) entram no desenho mais próximo; sobrando desenho, o menor se junta ao vizinho;
  faltando, um bloco de desenhos encostados (retratos, Boss, a paisagem emendada) é dividido onde é mais fino, usando a
  grade do pedido só como pista; a ordem é a de leitura (linhas pelo centro, depois da esquerda para a direita);
- tirar a **escala do tamanho do desenho** contra a caixa-alvo do pedido (campos novos do manifesto: `modo`, `alvo`,
  `escala`, `alvo_altura`, `ancora_x`, `largura`), em vez de tentar adivinhar a grade;
- reduzir cada desenho a uma **paleta própria** (até 32 cores; 48 nos cenários; uma só para todos os quadros de uma
  animação) e amostrar por **moda** dos índices da paleta em cada bloco (o centro vale o dobro), com preferência pelos
  tons escuros quando eles cobrem ≥ 34% do bloco — isso mantém contorno e linhas internas de 1 px;
- tirar o halo rosado (pixels misturados com o magenta) antes de votar a cor e de novo na borda do resultado, remover
  pixels soltos, preencher furos de 1 px e só pôr contorno onde falta;
- **modos:** `objetos`, `quadros` (escala e paleta únicas na folha, pés na mesma linha e na mesma coluna, nada cortado),
  `faixas` (cenário de fundo inteiro em 384 px de largura), `cena` (tela opaca reduzida para 384 × 256) e `paineis`
  (as 4 imagens das expedições, separadas pelas linhas magenta);
- `--pular` deixa de fora desenhos que vieram ruins.

`ArtImportSettings` ganhou a regra de `Resources/ArtePixel/` prevista na TD-059 (Point, sem compressão, sem mipmaps,
Clamp, sem limite de tamanho; `GetVersion` 5).

**Por quê.** O desenho em si é a única coisa estável numa folha do ChatGPT; a grade e o tamanho do pixel não são. Com a
caixa-alvo vinda da Bíblia, todos os peixes saem com o mesmo comprimento e todos os quadros do pescador com a mesma
altura, que é o que importa na tela.

**Rever se.** O ChatGPT passar a entregar grade exata (aí voltar a amostrar na grade dele preserva mais detalhe) ou se
algum desenho fino (linha da vara nos itens) sumir demais: baixar o limiar de cobertura só para aquele pedido.

## TD-062 · Cena pixel: câmera em duas etapas, encaixe na grade na hora de desenhar e composição em dados (20/10/2026)

**Origem.** M25-T04 a T12: ligar a pixel art de `Resources/ArtePixel/` no jogo, com a câmera própria da TD-059.

**Decisão.**
- **Duas câmeras.** A câmera principal (tag, posição, tamanho) continua sendo a do jogo, mas não desenha nada
  (`cullingMask = 0`) enquanto o mapa está em pixel; uma segunda câmera, com a mesma posição arredondada para o pixel
  da grade, desenha o mundo numa `RenderTexture` de 216 px (Point), e o `OnRenderImage` da principal põe essa grade na
  tela (inteiro exato, ou o inteiro de cima reduzido com bilinear). Assim `Camera.main.WorldToScreenPoint` (usado pelo
  HUD) continua certo sem mexer no HUD.
- **Encaixe na grade só para desenhar.** Em vez de mudar cada componente de movimento (deriva, balanço, voos), a
  câmera pixel põe todo sprite sem rotação no pixel inteiro em `OnPreCull` e devolve a posição em `OnPostRender`,
  com listas reaproveitadas (nada alocado por quadro). Sprites com rotação (vara, linha) ficam como estão: a própria
  grade de 216 px já os desenha em pixels.
- **Composição em dados.** Paletas de céu e água, deslocamentos das faixas, nuvens, reflexos, a linha da borda e o
  assento de cada barco, a posição da Caixa e o ponto das mãos em cada quadro do pescador ficam em
  `Resources/Visual/cena_pixel.json`, gravado por `tools/Arte/cena_pixel.py`. O mesmo script monta as prévias com os
  mesmos números, então a prévia fora do Unity e o jogo não divergem.
- **Fallback.** Um mapa sem `bg_far` em `ArtePixel` (ou sem entrada no json) volta ao cenário pintado antigo com a câmera
  normal; um arquivo pixel que falta cai no antigo de mesmo nome. Peças de mapa que ainda não vieram são convertidas da
  arte antiga como provisório (`cena_pixel.py provisorios`, lista em `tools/Arte/pixel_provisorios.txt`) e são
  sobrescritas quando o pedido chegar.

**Por quê.** Duas câmeras e o encaixe na hora de desenhar entregam "um pixel de arte = um pixel da grade" sem reescrever
a cena nem o HUD, e sem o pacote 2D Pixel Perfect. Dados num json só deixam ajustar a cena pelo script e conferir na
prévia antes do Play.

**Rever se.** O custo do encaixe aparecer no perfilador (centenas de sprites): aí marcar só o que se move; ou se o
proprietário quiser a cena em escala inteira com faixas pretas em vez do "nítido-suave".
