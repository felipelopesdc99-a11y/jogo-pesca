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
