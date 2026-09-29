# Base técnica

Este documento é para quem vai trabalhar no código: um desenvolvedor contratado, o Claude em outra
sessão, ou o próprio proprietário quando quiser entender como o jogo é montado por dentro.

- **O que o jogo deve ser** está no GDD (`docs/GDD_V0_1.md`) e no adendo (`docs/GDD_ADENDO.md`).
- **Como o jogo é construído** está aqui.
- **Por que cada escolha foi feita** está em `docs/DECISOES.md`.

Sempre que uma mudança alterar algo descrito aqui (uma pasta nova, um fluxo novo, uma regra de
arquitetura), este documento é atualizado **na mesma mudança**.

---

## 1. Visão geral

A V0.1 é, por enquanto, um **MVP local**: o jogo inteiro roda no PC, dentro do Unity, sem servidor.
Mesmo assim, o código mantém a separação que um jogo online exige:

```
┌──────────────────────────────── Unity (client-unity) ────────────────────────────────┐
│                                                                                      │
│   Game (apresentação)                      GameService (regras = "servidor local")   │
│   ───────────────────                      ────────────────────────────────────────  │
│   Cena, animação, interface   ──intenção──▶ IFishingService / IPlayerService         │
│   (FishingScene, Hud...)      ◀─resultado── LocalFishingService, CatchRules          │
│                                              GameSession ── IPlayerRepository ──▶ save│
│                                              GameConfig  ◀── /config (JSON)          │
│                                                                                      │
│   Editor (Painel de Desenvolvimento)  ──lê──▶ docs/roadmap.json, version.json,       │
│                                        ──edita──▶ /config, save                       │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

**Cliente = intenção + apresentação. Serviço de jogo = verdade + validação + sorteio + persistência.**
A parte visual nunca decide uma captura, um preço, um XP ou um resultado. Ela pede ("iniciar pesca",
"vender estes peixes") e mostra o que o serviço respondeu.

## 2. Pastas

```
client-unity/Assets/
├── Scripts/
│   ├── Texts/          FishingIdle.Texts        Todos os textos PT-BR (GameTexts) e formatação brasileira (Format)
│   ├── GameService/    FishingIdle.GameService  Regras do jogo. NÃO pode usar UnityEngine (noEngineReferences)
│   │   ├── Config/        Modelos de /config, carregamento, validação, impressão digital da versão
│   │   ├── Core/          Relógio (IClock), sorteio determinístico (Rng), ServiceResult/ServiceError
│   │   ├── Persistence/   Save (PlayerSave), repositório JSON com backup, validação e migrações
│   │   ├── Fishing/       Serviço de pesca, fórmulas de captura (CatchRules) e de peixe (FishRules), visões
│   │   ├── Aquarium/      Serviço do Aquário: guardar, alimentar, vender, ordenar
│   │   ├── Profile/       Cardume (posições, bônus, Força), Perfil, Inventário, Enciclopédia, varas
│   │   ├── Maps/          Mapas e viagem
│   │   ├── Expeditions/   Expedições e a fórmula de aproveitamento (ExpeditionRules)
│   │   ├── Arena/         Motor de combate (BattleEngine), adversários simulados (ArenaBots), Arena
│   │   ├── Market/        Mercado e Leilão: anúncios, compra, custódia (Itens a Retirar), lances, jogadores simulados
│   │   ├── Tutorial/      Tutorial curto: passos, avanço automático pelo que o jogador fez, pular
│   │   ├── Shop/          Loja e regras de preço/revenda de vara (RodRules)
│   │   ├── GameSession.cs Estado vivo de um jogador (config + save + relógio + armazenamento)
│   │   ├── PlayerService.cs
│   │   └── LocalGame.cs   Ponto de entrada: monta os serviços locais
│   ├── Game/           FishingIdle.Game         Apresentação no Unity
│   │   ├── Bootstrap/     GameBootstrap (Play em qualquer cena), GameRoot (ponte com o serviço), GamePaths
│   │   ├── Scene/         Arte provisória por código (Art), cena (FishingScene), pescador (FishermanRig), ambiente
│   │   ├── Audio/         Áudio provisório gerado por código (SoundBank) e quem toca (GameAudio)
│   │   └── UI/            HUD, janelas, avisos e central de notificações, estilos (IMGUI)
│   ├── Editor/         FishingIdle.Editor       Só no Editor: Painel de Desenvolvimento, setup do projeto, passo de build
│   └── Core/ Diagnostics/  FishingIdle.Client   Dormente: cliente HTTP e diagnóstico do servidor online (M12)
├── Scenes/Principal.unity   Criada automaticamente na primeira abertura
└── link.xml                 Protege o GameService da remoção de código no IL2CPP

config/     Balanceamento (JSON). Fonte única.
tools/      Checagens que rodam sem o Unity (ver seção 9)
docs/       Documentação
```

### Regra das dependências

```
Texts  ◀──  GameService  ◀──  Game  ◀──  Editor
```

Nunca ao contrário. O `GameService` não conhece o Unity, então ele compila e é testado fora do
Unity, e pode ser levado para um servidor .NET sem mudanças.

## 3. O serviço de jogo (GameService)

### Ponto de entrada

`LocalGame.Start(pastaConfig, pastaSave, relógio, log)` carrega e valida o balanceamento, abre (ou
cria) o save e devolve os serviços. Se o balanceamento for inválido, devolve a lista de erros em
PT-BR e o jogo mostra essa lista na tela, em vez de rodar com valores quebrados.

### Serviços

| Interface | Implementação atual | Faz |
|---|---|---|
| `IFishingService` | `LocalFishingService` | Iniciar/parar pesca, sincronizar ciclos, Caixa de Pesca, prévia de venda, venda |
| `IPlayerService` | `LocalPlayerService` | Resumo do jogador (nível, XP, Moedas, mapa, vara, Aquário) |
| `IAquariumService` | `LocalAquariumService` | Guardar capturas, ficha do peixe, alimentar, vender do Aquário |
| `ICardumeService` | `LocalCardumeService` | Posições 1–6, bônus 6/6, Força privada |
| `IMapService` | `LocalMapService` | Mapas, requisitos, viagem de 30s (pausa e retoma a pesca) |
| `IShopService` | `LocalShopService` | Loja: varas à venda e compra |
| `IExpeditionService` | `LocalExpeditionService` | Expedições: partida, travas do Cardume, pagamento na volta (online ou ao abrir) |
| `IArenaService` | `LocalArenaService` | Arena: ranking, Energia, Honra, adversários, ataques, ataques recebidos, histórico |
| `IMarketService` | `LocalMarketService` | Mercado: busca com filtros, anunciar, comprar, cancelar, Itens a Retirar, vendedores e compradores simulados |
| `IAuctionService` | `LocalMarketService` | Leilão: criar (1 por vez, 6 h), lances com reserva e taxa, último minuto, encerrar antes |
| `ITutorialService` | `LocalTutorialService` | Tutorial: passo atual, avanço automático, "Entendi", pular |
| `IProfileService` | `LocalProfileService` | Perfil próprio: vara equipada, Inventário, Enciclopédia, Destaques |
| `IPlayerRepository` | `JsonFilePlayerRepository` | Ler/gravar/resetar o save |
| `IClock` | `SystemClock` | A única fonte de "agora" das regras |

Cada sistema novo segue o mesmo desenho: uma interface `I...Service`, uma implementação
`Local...Service` que trabalha sobre a `GameSession`, e visões somente-leitura para a tela.

### Como a pesca online funciona

O serviço **não** roda um cronômetro por jogador. Ele guarda no save um cursor:

- `started_at_ms`: quando a sequência de ciclos começou;
- `cycle_ms`: duração do ciclo, congelada no início (editar o config não reescreve o passado);
- `cycles_processed`: quantos ciclos já viraram captura;
- `last_seen_at_ms`: a última vez que o jogo foi visto rodando.

A cada `Sync()` (a interface chama 4 vezes por segundo), o serviço calcula
`ciclos completos = (agora − início) ÷ ciclo` e processa só os que faltam. Chamar `Sync()` mil vezes
não gera peixe a mais; chamar cedo não gera nada.

**Tempo com o jogo fechado não é pesca online; é pesca offline.** Se o jogo ficou mais de
`max(3 ciclos, 2 minutos)` sem ser visto (fechado, PC dormindo, relógio adiantado), o serviço entrega
o que foi pescado até `last_seen_at_ms`, credita o intervalo no ritmo offline (`CatchUpOffline`: 60s
por captura, no máximo 24h, numa passada só, continuando a mesma sequência de sorteio) e recomeça o
ciclo online a partir de agora. O resumo fica disponível uma vez em `TakeOfflineReport()`.

### Sorteio determinístico

Todo sorteio usa `Rng` (SplitMix64), nunca `UnityEngine.Random`. A semente de cada captura é
`semente do save + índice da sessão + índice do ciclo`. Consequências: o mesmo ciclo sempre dá o
mesmo peixe (idempotência por construção), e os testes podem verificar distribuições com precisão.
Aleatoriedade puramente visual (nuvens, pássaros) usa `UnityEngine.Random` livremente.

### Fórmulas (`CatchRules`)

| O quê | Como | Onde está o número |
|---|---|---|
| Espécie | Peso de captura do mapa, só raridades que o mapa **e** a vara permitem; a eficiência de raridade da vara multiplica só o peso das espécies não comuns | `maps.json`, `rods.json` |
| Categoria de tamanho | Peso 20/60/19/1; a qualidade de tamanho da vara multiplica Grande e Excepcional | `progression.json → size` |
| Tamanho exato | Uniforme dentro da faixa de percentil da categoria, sobre a faixa de cm da espécie; gravado em milímetros | `fish_catalog.json → size_cm` |
| XP do Pescador | XP base da espécie × raridade × multiplicador da categoria, arredondado, mínimo 1 | `fish_catalog.json`, `progression.json` |
| Conchas | Só varas que geram Conchas; chance base × (1 + bônus da vara) | `economy.json → shells` |
| Preço de venda | Valor base × raridade × (1 + influência × (percentil − 0,5)), mínimo configurável | `economy.json`, `progression.json` |
| Atributos do peixe | Base × raridade × (1 + influência × (percentil − 0,5)) × (1 + bônus por nível × (nível − 1)) | `fish_catalog.json`, `progression.json` |
| XP como alimento | XP base × raridade × fator de tamanho + 50% do XP investido | `progression.json → feeding` |
| Bônus do Cardume | +3% nos quatro atributos com 6/6, só enquanto completo | `arena.json → cardume.complete_bonus` |
| Força do Cardume | Σ (Ataque×2 + Defesa×1,5 + Vida÷10 + Velocidade×0,5) × escala | `arena.json → cardume_strength` |
| Revenda de vara | Preço × 40% + melhorias × 25% | `rods.json → npc_resale` |
| Intervalo de ataque | 2 s × (100 ÷ Velocidade) | `arena.json → combat.speed` |
| Dano | Ataque × sorteio(0,97–1,03) × (1 − Defesa ÷ (Defesa + 100)), mínimo 10% do Ataque | `arena.json → combat` |
| Taxa do Mercado | 3% do preço, arredondado, descontada na venda concluída | `economy.json → market_fixed_price` |
| Referência do mercado simulado | Peixe: venda ao NPC × 1,5 × (1 + 15% × (nível − 1)); vara: (preço + melhorias) × 0,8 | `market_bots.json → valuation` |
| Lance mínimo do Leilão | 1º lance ≥ lance inicial; depois ≥ arredondar para cima(lance atual × 1,03) | `economy.json → auction` |
| Taxa por lance | 1% do lance, arredondado, cobrada a cada lance e nunca devolvida | `economy.json → auction` |
| Encerramento antecipado | Vendedor recebe o maior lance − 3%; no fim normal, sem taxa | `economy.json → auction` |
| Chance de um comprador simulado | Referência: 25% por verificação; mais barato até o dobro; zero a partir de 4× a referência | `market_bots.json → demand` |

## 4. Balanceamento (/config)

- No Editor, o jogo lê direto da pasta `/config` do repositório. Num build, lê a cópia que o
  `ConfigBuildStep` coloca em `StreamingAssets/config` (essa cópia não é versionada).
- O jogo carrega todos os arquivos de `GameConfigLoader.RequiredFiles`: `fish_catalog`, `maps`,
  `progression`, `rods`, `economy`, `arena`, `expeditions`, `arena_bots` (adversários simulados do
  MVP local) e `market_bots` (vendedores e compradores simulados do Mercado). O Painel de Desenvolvimento usa a mesma lista.
- `GameConfigLoader.LoadFromTexts` é usado tanto pelo jogo quanto pelo Painel de Desenvolvimento:
  o painel só grava se a mesma validação que o jogo usa passar.
- `GameConfig.Version` é uma impressão digital curta do conteúdo. Mesmos arquivos, mesma versão.
- Para adicionar um valor novo: acrescente a chave no JSON, a propriedade no modelo
  (`ConfigModels.cs`, em PascalCase — a conversão para snake_case é automática), a checagem em
  `GameConfigValidator` e, se o proprietário deve poder editar, a linha no `BalanceEditor`.

## 5. Save

| Arquivo | Para quê |
|---|---|
| `player_save.json` | O save atual |
| `player_save.backup.json` | O save anterior (cópia feita a cada gravação) |
| `player_save.corrupt-AAAAMMDD-HHMMSS.json` | Save danificado, guardado de lado para inspeção |
| `player_save.reset-AAAAMMDD-HHMMSS.json` | Cópia feita antes de um reset |

A pasta fica em `Application.persistentDataPath/save` (no Windows,
`%USERPROFILE%\AppData\LocalLow\FishingIdle\Fishing Idle\save`). O Painel de Desenvolvimento tem um
botão para abri-la.

Regras:

- Grava num arquivo temporário e só depois substitui o save.
- O save é gravado depois de toda mudança (captura, venda, iniciar/parar) e ao fechar o jogo.
- `save_version` identifica o formato. Mudou o formato? Aumente `PlayerSave.CurrentVersion` e
  acrescente um passo em `SaveMigrations.Upgrade`.
- Um save de versão **mais nova** que o jogo nunca é sobrescrito: o jogo roda com um save
  temporário e avisa.
- Valores derivados do config (preço, XP, nomes) não são gravados; só o que é do jogador.
- A Caixa de Pesca é compacta (seção 11 do GDD): cada captura guarda id, espécie, milímetros,
  categoria, hora e marcas (espécie nova, recorde). Não é um peixe completo.
- O Aquário (desde a versão 2 do save) guarda peixes completos (`FishInstance`): id, espécie,
  milímetros, categoria, nível, XP do nível, XP investido, datas e a captura de origem. Os atributos
  nunca são gravados; `FishRules.Stats` os calcula.
- Desde a versão 3, a vara é um item do Inventário (`InventoryItem`) e o slot de Vara guarda o id
  desse item (`EquippedRodItemId`). O Cardume é uma lista de 6 ids de peixes do Aquário (0 = vazio);
  o bônus 6/6 e a Força são calculados na hora (`CardumeRules`) e nunca gravados.
- Histórico de formatos: v1 (Milestone 1), v2 adiciona o Aquário, v3 move a vara para o Inventário
  e adiciona o Cardume, v4 adiciona a viagem (`TravelState`) e separa o preço pago pela vara do que
  foi gasto em melhorias, v5 adiciona a Expedição (`ExpeditionState`, `LastExpedition`), v6 adiciona a Arena
  (`ArenaState`: ranking como lista de ids, Energia, Honra, adversários, histórico), v7 adiciona o
  Mercado (`MarketState`), v8 adiciona os leilões (`MarketState.Auctions`), v9 adiciona o tutorial
  (`TutorialState`; saves antigos entram com ele concluído). Cada passo está em `SaveMigrations.Upgrade`.
- Tutorial (desde a versão 9): o jogador novo começa **sem vara** (`EquippedRodItemId = 0`) e pega a
  Vara Inicial de graça na Loja (`free_claim_in_shop` em `rods.json`). O save só aceita "sem vara"
  enquanto o tutorial não terminou; pular o tutorial entrega a Vara Inicial. Cada passo termina sozinho
  quando o save mostra que o jogador fez aquilo (pegou a vara, pescou, vendeu, guardou, montou o
  Cardume); os passos só de leitura (boas-vindas, Caixa, Expedição) terminam com `Acknowledge`.
- Mercado (desde a versão 7): um item anunciado **sai** do Aquário/Inventário e passa a morar dentro
  do anúncio (`MarketListing.Goods`, com todos os dados do peixe ou da vara). Comprado, cancelado ou
  vencido, ele vai para `Withdrawals` (Itens a Retirar) e só volta ao Aquário/Inventário quando o
  jogador retira. Assim o mesmo peixe nunca está em dois lugares, e o limite de 100 do Aquário vale
  na retirada. Peixes e varas que vêm dos vendedores simulados ganham id novo só ao serem retirados.
- Mercado simulado: `LocalMarketService` avança em "ticks" do relógio (`SupplyTick`, `DemandTick` =
  hora ÷ intervalo), inclusive pelo tempo em que o jogo ficou fechado (com teto). Cada tick usa o
  `Rng` do jogador com uma semente própria, então o resultado não depende de quantas vezes a tela foi
  aberta. As vendas e vencimentos viram `MarketEvent` e aparecem como aviso uma única vez.
- Leilão (desde a versão 8): `Auction` guarda o item, o lance inicial, o maior lance e quem o deu
  (`"player"` ou o nome de um jogador simulado). Ao dar um lance, o valor sai das Moedas (fica
  "reservado") junto com a taxa de 1%; se alguém passar, o valor volta na hora e a taxa não. Quem já
  tem o maior lance não pode dar outro, então repetir o pedido nunca cobra duas vezes. O mesmo
  `LocalMarketService` (classe parcial, arquivo `LocalMarketService.Auctions.cs`) implementa
  `IAuctionService`; os lances simulados rodam em ticks de 10 minutos, também nos leilões do jogador.

## 6. Apresentação (Game)

- **Sem cena montada à mão.** Ao apertar Play, `GameBootstrap` cria o `GameRoot`, que inicia o
  serviço e monta a cena por código (`FishingScene`). Funciona em qualquer cena.
- **Um tema por mapa** (`SceneTheme.For`): cores, relevo, correnteza, pedras e cachoeira. Ao chegar a
  outro mapa (`GameRoot.MapChanged`), a cena é destruída e montada de novo com o tema dele.
- **Arte provisória por código** (`Art.cs`, `FishLooks.cs`): gradientes, silhuetas, barco, peixes com
  cores por espécie. Trocar pela arte final = trocar o que esses métodos devolvem.
- **Camadas e paralaxe:** câmera ortográfica que oscila devagar; cada camada acompanha a câmera numa
  fração diferente, criando profundidade (2.5D). Ordem de desenho nas constantes de `FishingScene`.
- **Animação sincronizada ao serviço:** `FishermanRig` lê `FishingStatus` (início e fim do ciclo)
  para arremessar, esperar, mostrar a mordida e puxar; o peixe só aparece quando o serviço entrega a
  captura (`GameRoot.CatchesArrived`).
- **Interface em IMGUI** (`Hud`, `FishingBoxWindow`, `UiSkin`): tela virtual de 1080 px de altura,
  escalada. Todos os estilos ficam em `UiSkin`, o que isola uma futura troca para UI Toolkit.
- **Textos:** sempre de `GameTexts`. Nunca escreva uma frase em PT-BR direto num `.cs` fora de `Texts/`.
- **Avisos e central de notificações** (`ToastFeed`): `Push(texto, tipo, ícone, notify)` mostra o
  aviso; com `notify: true` ele também entra na lista do sino (até 50, só enquanto o jogo está
  aberto). Entram: espécie nova, recorde, Excepcional, subir de nível, Mercado/Leilão, Expedição,
  ataques recebidos na Arena.
- **Áudio provisório** (`Audio/`): `SoundBank` sintetiza sons curtos por tipo de aviso e um loop de
  água; `GameAudio` toca um som a cada aviso (`ToastFeed.Pushed`), sem empilhar. Trocar pelo áudio
  final = trocar o que `SoundBank` devolve.
- **Preferências de apresentação** (`GameSettings`): som, som ambiente e volume, no `PlayerPrefs` do
  PC. Não entram no save porque não mudam nenhuma regra.
- **Modo compacto** (Opções → Modo compacto): janela de 480×270 só com a cena, uma linha de status e a
  última captura. É só apresentação; o serviço pesca igual. No Editor o tamanho da janela não muda.
- **Transição de menus:** a janela que abre faz um fade de 0,18 s (`Hud`).

## 7. Painel de Desenvolvimento (Editor)

Menu **Fishing Idle → Painel de Desenvolvimento** (`DevPanelWindow`).

- **Visão geral / Roadmap:** lêem `docs/roadmap.json` e `version.json` (`RoadmapData.cs`). Não
  escrevem nada. O status mostrado é o que está no arquivo — nunca "atividade em segundo plano".
- **Balanceamento** (`BalanceEditor`): edita os JSON como árvore, preservando ordem, notas e
  formatação (um teste garante que salvar sem editar não muda nenhum byte). Valida antes de gravar.
  Com o jogo rodando, "Salvar e aplicar" recarrega o config na hora (`GameRoot.ReloadConfig`).
- **Save:** leitura sem efeitos colaterais, e reset com confirmação.
- **Primeira abertura** (`ProjectSetup`): cria e abre `Assets/Scenes/Principal.unity`, registra no
  Build Settings e, só se o projeto ainda estiver com os valores padrão do Unity, define nome do
  produto/empresa, janela redimensionável e "rodar em segundo plano".

## 8. Textos e idioma

`client-unity/Assets/Scripts/Texts/GameTexts.cs` é o único lugar com texto para pessoas (jogo,
painel, mensagens de validação). `Format.cs` formata números (`1.234`, `35,2 cm`, `10,3%`), datas
(`28/09/2026 21:55`) e contagens regressivas. Chaves técnicas gravadas em dados (`common`,
`exceptional`, `DONE`, `rod_01`) ficam em inglês e são traduzidas na hora de exibir.

## 9. Testes e checagens (sem precisar do Unity)

```bash
./ops/scripts/verify.sh              # o jogo (padrão)
./ops/scripts/verify.sh --completo   # também o servidor e o painel web arquivados
```

| Projeto | O que garante |
|---|---|
| `tools/GameService.Build` | O `GameService` compila como no Unity: .NET Standard 2.1 e C# 9 |
| `tools/GameService.Tests` | Testes das regras: ciclo, idempotência, sorteio e distribuições, vara, XP, venda, save, backup, reset, formatação, integridade do roadmap, ida e volta do balanceamento |
| `tools/UnityCheck.Game` | O código de apresentação compila contra as bibliotecas de referência do Unity |
| `tools/UnityCheck.Editor` | O Painel de Desenvolvimento compila contra a biblioteca de referência do Editor |
| `tools/Simulador` | Só compila no `verify.sh`; roda com `./ops/scripts/simular.sh` (abaixo) |

Os testes cobrem também abuso local (Milestone 11, `AbuseTests`): relógio voltando ou pulando anos,
pedidos repetidos, o mesmo peixe em dois lugares, Cardume em Expedição no Mercado, preços e lances
negativos, save editado à mão, e um save com todos os sistemas em uso que reabre igual.

### Simulador de balanceamento

```bash
./ops/scripts/simular.sh   # gera docs/relatorios/SIMULACAO_BALANCEAMENTO.md
```

Joga as **regras reais** (`LocalGame`) com um relógio manual e um save em memória, com sementes fixas
(o relatório sai igual para o mesmo `/config`). Mede: progressão por nível e Moedas (jogador que pesca
sem parar, vende tudo, compra a vara e troca de mapa assim que pode), frequência de raridade/tamanho
e Conchas por mapa e vara (200 mil sorteios), preços de venda por espécie, retorno das varas, duração
e resultado das batalhas entre adversários simulados, Moedas das Expedições, tempo de venda no
Mercado e preço final dos leilões. No topo, "Pontos de atenção" lista o que chama atenção nos números
— sem mudar nada. Para medir algo novo, acrescente uma seção em `tools/Simulador/Program.cs`.
As checagens do Unity pegam erros de digitação, tipos e membros inexistentes. Elas **não**
substituem abrir o projeto no Editor: comportamento visual só se confirma apertando Play.

**Cuidado com APIs novas:** as bibliotecas de referência são do Unity 2021.3. O código evita de
propósito APIs que só existem no Unity 6; se uma for realmente necessária, a checagem vai acusar e
a exceção precisa ser registrada em `DECISOES.md`.

## 10. Como adicionar um sistema (receita)

Exemplo: o Aquário (Milestone 2).

1. **Dados:** valores novos em `/config` + modelo em `ConfigModels.cs` + validação.
2. **Save:** campos novos em `PlayerSave` (e migração, se o formato mudar).
3. **Regras:** `IAquariumService` + `LocalAquariumService` sobre a `GameSession`, devolvendo visões
   somente-leitura e `ServiceResult` com `ServiceError` para recusas.
4. **Testes** em `tools/GameService.Tests` antes da interface.
5. **Apresentação:** janela/tela em `Game/UI`, pedindo ações ao serviço via `GameRoot`.
6. **Textos** em `GameTexts`.
7. **Painel:** novos valores editáveis no `BalanceEditor`, se fizer sentido.
8. **Documentação:** roadmap (`DONE` com nota), `CHANGELOG.md`, e este documento se a arquitetura
   mudou; `GDD_ADENDO.md` se algum detalhe de design foi decidido.

## 11. Do local para o online

Quando o jogo for para a internet (Milestone 12):

- Cada `Local...Service` ganha um `Remote...Service` com a mesma interface, que chama a API em
  `server/` (o cliente HTTP dormente em `Assets/Scripts/Core` é o ponto de partida).
- O `GameService` pode ser referenciado pelo servidor .NET, porque não depende do Unity: as mesmas
  regras passam a rodar do lado do servidor, que vira a autoridade.
- `LocalGame` é o único lugar que decide "rodar localmente"; trocar por uma versão remota não mexe
  na cena nem na interface.
- O save local deixa de ser a verdade: o servidor passa a guardar o estado (PostgreSQL).
