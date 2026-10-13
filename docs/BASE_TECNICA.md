# Base técnica

Este documento é para quem vai trabalhar no código: um desenvolvedor contratado, o Claude em outra
sessão, ou o próprio proprietário quando quiser entender como o jogo é montado por dentro.

- **O que o jogo deve ser** está no GDD (`docs/GDD_V0_1.md`) e no adendo (`docs/GDD_ADENDO.md`). **Como ele deve parecer** está na Bíblia de Arte (`docs/ART_BIBLE_V0_1.md`).
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
│   │   ├── Vip/           VIP: compra com Dólares e bônus de XP na pesca offline (VipRules)
│   │   ├── Dev/           Ferramentas de teste do proprietário (F2), só no Editor (TD-034)
│   │   ├── GameSession.cs Estado vivo de um jogador (config + save + relógio + armazenamento)
│   │   ├── PlayerService.cs
│   │   └── LocalGame.cs   Ponto de entrada: monta os serviços locais
│   ├── Game/           FishingIdle.Game         Apresentação no Unity
│   │   ├── Bootstrap/     GameBootstrap (Play em qualquer cena), GameRoot (ponte com o serviço), GamePaths
│   │   ├── Scene/         Cena em camadas (FishingScene), pescador (FishermanRig), ambiente, arte de reserva por código (Art)
│   │   ├── Visual/        Tema visual em dados (VisualTheme) e carregamento da arte trocável (ArtAssets, Icons)
│   │   ├── Audio/         Sons: quais arquivos (SoundBank), quando tocam (SoundCue) e quem toca (GameAudio)
│   │   └── UI/            HUD, janelas, avisos e central de notificações, estilos (IMGUI)
│   ├── Editor/         FishingIdle.Editor       Só no Editor: Painel de Desenvolvimento, setup do projeto, passo de build
│   └── Core/ Diagnostics/  FishingIdle.Client   Dormente: cliente HTTP e diagnóstico do servidor online (M12)
├── Resources/
│   ├── Arte/            Arte trocável: Peixes/, Mapas/<Mapa>/, Cena/, Varas/, Expedicoes/, Icones/, Vivos/ (ver docs/ASSETS_PENDENTES.md)
│   ├── Fontes/          Fredoka e Nunito (OFL)
│   ├── Visual/          tema_visual.json (cores, brilho, tempos) e paisagem_viva.json (plantas, animais, ritmo)
│   └── Sons/            Sons (.wav)
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
| `IPlayerService` | `LocalPlayerService` | Resumo do jogador (nível, XP, Moedas, Conchas, Dólares, mapa, vara, Aquário) |
| `IAquariumService` | `LocalAquariumService` | Guardar capturas, ficha do peixe, alimentar, vender do Aquário |
| `ICardumeService` | `LocalCardumeService` | Posições 1–6, bônus 6/6, Força privada |
| `IMapService` | `LocalMapService` | Mapas, requisitos, viagem de 30s (pausa e retoma a pesca) |
| `IShopService` | `LocalShopService` | Loja, aba Varas: desde o M24-T32 nenhuma vara é vendida (vêm de fragmentos, `RodFragmentRules`, TD-048); `BuyRod` só resgata a Vara Inicial do tutorial. A oferta mostra os fragmentos e a chance (a mesma em todos os mapas desde o M24-T47), o nível da sua vara, os bônus agora e o preço do próximo nível (melhorar continua em `IProfileService.UpgradeRod`) |
| `IClothingService` | `LocalClothingService` | Roupas (M24-T33, A-164, TD-048): as 5 peças com nível, XP de uso e bônus; `Assemble` monta a peça escolhida com os Fragmentos de Roupa |
| `IGearService` | `LocalGearService` | Barcos e iscas: iscas compradas, usar, guardar; barcos só usar e melhorar o nível (`UpgradeBoat`, M24-T13) — desde o M24-T16 eles vêm de fragmentos (`BoatFragmentRules`, TD-042) e a oferta mostra fragmentos e chance; equipamento em uso e chance de puxar por raridade |
| `IPassService` | `LocalPassService` | Passe do Pescador (M24-T17, A-158, TD-042): marcos de `pass.json`, resgatar um ou todos, comprar o Premium com Dólares (libera os marcos já passados); `IsMilestone` para o aviso de nível |
| `IWorldBossService` | `LocalWorldBossService` | Boss Mundial (M24-T29, A-165, TD-049): `Update` (chamado pelo jogo aberto a cada sincronização) abre o dia, credita os ataques devidos do Cardume, sobe de faixa e paga uma vez quando a janela fecha; `GetWorldBoss` monta a tela. Regras puras em `WorldBossRules` (dia e janela no fuso do config, dano com `BattleEngine.HitDamage`, faixas, renda de referência, barco do mapa mais alto, Dólares por posição) |
| `IClanService` | `LocalClanService` | Clãs (M24-T49, A-174, TD-055): `Create` (Nv.100+, paga Dólares, nome validado e sem repetir), `Join` (por id, o "convite"; pronto para o servidor), `Leave` (clã vazio fecha; dono que sai passa o clã ao membro mais antigo), `BuySlot` (só o dono, preço crescente, até `max_members`), `SetFish`/`ClearFish` (1 peixe do Aquário no Cardume do Clã, sem tirá-lo do Aquário nem do Cardume), `GetFishCandidates`, `Update` (fecha o dia do ranking às 00:00 de Brasília e, só com `online_rewards_enabled`, paga o top 3 uma vez). Regras puras em `ClanRules` (vagas e preço, nome, `TakeBonusXp` com a fração guardada em `ClanState.XpCarry`, Força do peixe pela regra do Cardume, Força do clã com `SafeMath`, dia no fuso do config, ordem do ranking). O +1% de XP entra em `LocalFishingService.ApplyCatch` (`FishingUpdate.ClanXpGained`); peixe vendido, comido ou anunciado sai do Cardume do Clã (`ClanRules.DropMissingFish`, chamado pelo Aquário, pelo Mercado, pelo Leilão, pelo `Update` e na carga) |
| `IExpeditionService` | `LocalExpeditionService` | Expedições: partida, travas do Cardume, pagamento na volta (online ou ao abrir) |
| `IArenaService` | `LocalArenaService` | Arena: ranking, Energia, Honra, adversários, ataques, ataques recebidos, histórico |
| `IMarketService` | `LocalMarketService` | Mercado: busca com filtros, anunciar, comprar, cancelar, Itens a Retirar, vendedores e compradores simulados |
| `IAuctionService` | `LocalMarketService` | Leilão: criar (1 por vez, 6 h), lances com reserva e taxa, último minuto, encerrar antes |
| `ICurrencyTradeService` | `LocalCurrencyTradeService` | Venda de Conchas e Dólares por Moedas: anunciar (a quantidade sai da carteira) e cancelar; no MVP local não há ofertas nem compradores (`CurrencyTradeView.LocalOnly`) |
| `IDevToolsService` | `LocalDevToolsService` | Ferramentas de teste (A-123): recursos, peixes, nível, Energia e avançar o tempo; recusa tudo se `GameSession.DevToolsEnabled` estiver desligado |
| `IVipService` | `LocalVipService` | VIP (A-110): comprar com Dólares (soma os dias ao fim do atual) e ver até quando vale. O bônus de XP é aplicado pela pesca offline (`VipRules.OfflineBonusXp`), conferido na hora de cada captura |
| `IRankingService` | `LocalRankingService` | Ranking de jogadores reais por Nível, Moedas, Conchas e Peixes pescados; no MVP local só o jogador deste PC (`RankingView.LocalOnly`) |
| `ITutorialService` | `LocalTutorialService` | Tutorial: passo atual, avanço automático, "Entendi", pular |
| `IProfileService` | `LocalProfileService` | Perfil próprio: vara equipada, Inventário, Enciclopédia, Destaques |
| `IPlayerRepository` | `JsonFilePlayerRepository` | Ler/gravar/resetar o save |
| `IClock` | `SystemClock` | A única fonte de "agora" das regras |

Cada sistema novo segue o mesmo desenho: uma interface `I...Service`, uma implementação
`Local...Service` que trabalha sobre a `GameSession`, e visões somente-leitura para a tela.

### Como a pesca online funciona

Cada ciclo é uma **tentativa** (`LocalFishingService.Attempt`): o barco e a isca dão o bônus deles
(`GearRules.SpendAttempt`, que gasta uma carga da isca), `CatchRules.Attempt` sorteia o peixe e se ele
é puxado, e só um peixe puxado vira captura. Um escape entra em `FishingUpdate.Escapes` (só a
raridade e a chance) e em `Stats.Escapes`; quando a isca acaba, `FishingUpdate.BaitRanOut` traz o
nome dela. Veja `docs/SISTEMA_SUCESSO_PESCA.md`.

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
o que foi pescado até `last_seen_at_ms`, credita o intervalo no ritmo offline (`CatchUpOffline`: uma
tentativa a cada `offline_cycle_seconds`, hoje 20 s, ou `vip.offline_cycle_seconds` (15 s) enquanto o VIP está ativo e o ciclo
termina antes do fim do VIP (A-160), no máximo 24h, numa passada só, continuando a mesma sequência de sorteio) e recomeça o
ciclo online a partir de agora. O resumo fica disponível uma vez em `TakeOfflineReport()`.

### Sorteio determinístico

Todo sorteio usa `Rng` (SplitMix64), nunca `UnityEngine.Random`. A semente de cada captura é
`semente do save + índice da sessão + índice do ciclo`. Consequências: o mesmo ciclo sempre dá o
mesmo peixe (idempotência por construção), e os testes podem verificar distribuições com precisão.
Aleatoriedade puramente visual (nuvens, pássaros) usa `UnityEngine.Random` livremente.

### Fórmulas (`CatchRules`)

| O quê | Como | Onde está o número |
|---|---|---|
| Raridade da vara | A eficiência de raridade multiplica o peso de toda espécie não Comum (Raro e Épico) que a vara pode pegar; os Comuns ficam iguais | `rods.json` |
| Chance de Sucesso | Chance-base da raridade + vara (`catch_success`) + barco + isca, entre o piso e o teto; sorteada depois da espécie e antes do tamanho | `progression.json → rarity.tiers, fishing`, `rods.json`, `equipment.json` |
| Espécie | Peso de captura do mapa, só raridades que o mapa **e** a vara permitem; a eficiência de raridade da vara multiplica só o peso das espécies não comuns | `maps.json`, `rods.json` |
| Mordida na pesca (M24-T22, TD-043) | `RollBite` = o sorteio da Espécie e depois `FishingWeights`: cada espécie não Comum fica com `fishing_bite_multiplier` × a chance que tinha e os Comuns dividem o resto; só `Attempt` (pesca online e offline) usa; `Roll`/`RollFound` (Expedições, Mercado) não. `BiteShare` dá o "morde X%" da Enciclopédia | `progression.json → rarity.tiers[].fishing_bite_multiplier` |
| Categoria de tamanho | Peso 20/60/19/1; a qualidade de tamanho da vara multiplica Grande e Excepcional | `progression.json → size` |
| Tamanho exato | Uniforme dentro da faixa de percentil da categoria, sobre a faixa de cm da espécie; gravado em milímetros | `fish_catalog.json → size_cm` |
| XP do Pescador | XP base da espécie × raridade × multiplicador da categoria, arredondado, mínimo 1 | `fish_catalog.json`, `progression.json` |
| Conchas | Só peixe puxado e só varas que geram Conchas; chance base × (1 + bônus da vara) | `economy.json → shells` |
| Preço de venda | Valor base × raridade × (1 + influência × (percentil − 0,5)), mínimo configurável | `economy.json`, `progression.json` |
| Atributos do peixe | Base × raridade × (1 + influência × (percentil − 0,5)) × (1 + bônus do nível na curva, `GameConfig.FishStatBonus`) | `fish_catalog.json`, `progression.json → fish_level` |
| Níveis de vara, barco e peixe (M24-T13, TD-041) | `LevelCurve.Value`: Nv.1 + (máx − Nv.1) × ((N − 1) ÷ (máx − 1)) ^ expoente; preço do nível N → N + 1 = `LevelCurve.Cost` (primeiro × crescimento ^ (N − 1), arredondado a 3 algarismos), em Moedas e Conchas. `GameConfig.RodBonusesAt / RodUpgradeCost / RodUpgradeShellCost / BoatBonusAt / BoatUpgradeCost / BoatUpgradeShellCost / FishStatBonus` | `rods.json → upgrade_rules, bonuses_at_level_1, bonuses_at_max_level, upgrade_cost`; `equipment.json → boat_levels, boats[].upgrade_cost`; `progression.json → fish_level` |
| XP como alimento | XP base × raridade × fator de tamanho + 50% do XP investido | `progression.json → feeding` |
| Bônus do Cardume | +3% nos quatro atributos com 6/6, só enquanto completo | `arena.json → cardume.complete_bonus` |
| Força do Cardume | Σ (Ataque×2 + Defesa×1,5 + Vida÷10 + Velocidade×0,5) × escala | `arena.json → cardume_strength` |
| Revenda de vara | Preço × 40% + melhorias × 25% | `rods.json → npc_resale` |
| Intervalo de ataque | 2 s × (100 ÷ Velocidade) | `arena.json → combat.speed` |
| Dano | Ataque × sorteio(0,97–1,03) × (1 − Defesa ÷ (Defesa + 100)), mínimo 10% do Ataque | `arena.json → combat` |
| Taxa do Mercado | 3% do preço, arredondado, descontada na venda concluída | `economy.json → market_fixed_price` |
| Referência do mercado simulado | Peixe: venda ao NPC × 1,5 × (1 + 3,37% × (nível − 1)); vara: (preço + melhorias) × 0,8 | `market_bots.json → valuation` |
| Lance mínimo do Leilão | 1º lance ≥ lance inicial; depois ≥ arredondar para cima(lance atual × 1,03) | `economy.json → auction` |
| Taxa por lance | 1% do lance, arredondado, cobrada a cada lance e nunca devolvida | `economy.json → auction` |
| Encerramento antecipado | Vendedor recebe o maior lance − 3%; no fim normal, sem taxa | `economy.json → auction` |
| Chance de um comprador simulado | Referência: 25% por verificação; mais barato até o dobro; zero a partir de 4× a referência | `market_bots.json → demand` |

## 4. Balanceamento (/config)

- No Editor, o jogo lê direto da pasta `/config` do repositório. Num build, lê a cópia que o
  `ConfigBuildStep` coloca em `StreamingAssets/config` (essa cópia não é versionada).
- O jogo carrega todos os arquivos de `GameConfigLoader.RequiredFiles`: `fish_catalog`, `maps`,
  `progression`, `rods`, `economy`, `arena`, `expeditions`, `arena_bots` (adversários simulados do
  MVP local), `market_bots` (vendedores e compradores simulados do Mercado), `equipment` (barcos e
  iscas, desde a V0.2), `pass` (o Passe do Pescador, M24-T17), `world_boss` (o Boss Mundial, M24-T29) e `clans` (os
  Clãs, M24-T49, validado por `GameConfigValidator.ValidateClans`). O
  `crew.json` e o `upgrades.json` (Tripulação e Melhorias) saíram no M24-T46 (TD-053); os preços deles ficam só em
  `economy.json → crew_removal_refund`, para o reembolso dos saves antigos. O Painel de Desenvolvimento usa a mesma lista
  (o `pass.json` ainda não tem seção própria no Painel de Balanceamento: edita-se à mão, ou se gera de novo com o
  script, e o jogo valida ao abrir). Os barcos (fragmentos e chance) aparecem na aba Sucesso da pesca.
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
  categoria, hora, marcas (espécie nova, recorde) e, desde a v23, os 4 genes (`"g"`). Não é um peixe completo.
- O Aquário (desde a versão 2 do save) guarda peixes completos (`FishInstance`): id, espécie,
  milímetros, categoria, nível, XP do nível, XP investido, datas, a captura de origem e, desde a v23, os genes. Os
  atributos nunca são gravados; `FishRules.Stats` os calcula: base da espécie × raridade × tamanho × nível × categoria
  de tamanho × o multiplicador do gene de cada atributo (`GeneRules.Multiplier`, ×0,90 no gene 0 a ×1,10 no 31;
  M24-T52, A-176, TD-056). Sem genes (`null`, os números de referência da Enciclopédia) não há fator de gene.
- Genes (`Fishing/GeneRules`): sorteados uma vez, quando a captura entra na Caixa (`LocalFishingService.ApplyCatch`,
  fluxo próprio `GeneRules.RollForCatch(semente, id da captura)`, sem mexer nos outros sorteios do ciclo), copiados para o
  peixe ao guardar no Aquário e levados junto pelo Mercado. Peixes simulados (Mercado, Leilão, Arena) sorteiam com
  fluxos próprios do anúncio ou do bot. `GeneRules.View` faz o `GeneView` (qualidade, estrelas, genes perfeitos) que vai
  em `CatchView.Genes` e `FishView.Genes`; as telas desenham com `UI/GeneUi` e nunca calculam.
- Desde a versão 3, a vara é um item do Inventário (`InventoryItem`) e o slot de Vara guarda o id
  desse item (`EquippedRodItemId`). O Cardume é uma lista de 6 ids de peixes do Aquário (0 = vazio);
  o bônus 6/6 e a Força são calculados na hora (`CardumeRules`) e nunca gravados.
- Histórico de formatos: v1 (Milestone 1), v2 adiciona o Aquário, v3 move a vara para o Inventário
  e adiciona o Cardume, v4 adiciona a viagem (`TravelState`) e separa o preço pago pela vara do que
  foi gasto em melhorias, v5 adiciona a Expedição (`ExpeditionState`, `LastExpedition`), v6 adiciona a Arena
  (`ArenaState`: ranking como lista de ids, Energia, Honra, adversários, histórico), v7 adiciona o
  Mercado (`MarketState`), v8 adiciona os leilões (`MarketState.Auctions`), v9 adiciona o tutorial
  (`TutorialState`; saves antigos entram com ele concluído), v10 adiciona barcos e iscas (`BoatId`,
`OwnedBoatIds`, `BaitCharges`, `ActiveBaitId`) e a contagem de escapes (`Stats.Escapes`); saves
antigos entram com o Barco Inicial e sem isca; v11 adiciona os Dólares (`Dollars`, começa em 0) e os anúncios de Conchas e Dólares
(`MarketState.CurrencyListings`); v12 grava a curva de XP do Pescador em que o nível foi ganho
(`FisherXpCurveVersion`; saves antigos entram com a curva 1); v13 adicionava a Tripulação e v14 as Melhorias (as duas
saíram na v21: as migrações v12 → v13 e v13 → v14 hoje só sobem a versão); v15 adiciona o nível dos barcos (`BoatLevels`: nível por id de barco, ausente = Nv.1) e as curvas de nível
de varas e peixes (`RodLevelCurveVersion`, `FishLevelCurveVersion`; saves antigos entram com 1, a curva Nv.1–Nv.10);
v16 adiciona os fragmentos de barco (`BoatFragments`, por id de barco; começam vazios, os barcos comprados continuam) e
o Passe do Pescador (`PassState`: id do passe, Premium, marcos resgatados grátis e Premium e `LegacyDollarsUpToLevel`,
o nível até onde os Dólares do A-111 já foram pagos: mín(nível, 100) na migração); v17 marca o modelo de iscas
(`BaitModelVersion`, TD-046); v18 adiciona os fragmentos de vara (`RodFragments`, por id de vara) e as roupas
(`ClothingState`: `Fragments` e `Pieces` com nível, XP de uso e quando foi montada); saves antigos entram com os dois
vazios e as varas do Inventário continuam com o nível (TD-048); v19 adiciona o Boss Mundial (`WorldBossState`: o dia
do evento no fuso de Brasília, dano, ataques, último índice de ataque processado, dano máximo do dia, faixa, `Paid` e o
que foi pago; um registro só, trocado no evento do dia seguinte depois de pago; saves antigos entram com ele vazio);
v20 marca o modelo de barcos (`BoatModelVersion`; saves antigos entram com 1, os 10 barcos de antes); v22 adiciona
os Clãs (`ClanState`: `MyClanId`, `KnownClans` — no jogo local só o clã do próprio jogador, o lugar onde o servidor vai
pôr a lista —, `NextClanNumber`, `XpCarry`, `RankingDay` e `LastResult`; saves antigos entram sem clã, e a carga limpa
um `MyClanId` que não aponta para um clã com o jogador, `ClanRules.Repair`); v23 adiciona os genes (M24-T52, A-176,
TD-056: `BoxCatch.Genes`, chave `"g"`, e `FishInstance.Genes`, 4 inteiros de 0 a 31 na ordem Vida, Ataque, Defesa,
Velocidade); a migração v22 → v23 só sobe a versão e, ao abrir a sessão, `Persistence/GeneMigration.GiveMissingGenes`
dá `progression.json → genes.migration_neutral_value` (16) a todo peixe sem genes válidos (Caixa, Aquário e Mercado). A conversão dos
barcos 6 a 10 em fragmentos, Moedas e Conchas (A-168) precisa do config e por isso não fica em `SaveMigrations`: roda
uma vez ao abrir a sessão, em `Persistence/BoatModelMigration.MoveToCurrentModel`, antes de os níveis de barco serem
limitados ao config (TD-051), como a das iscas (`BaitModelMigration`, v17, TD-046); v21 tira a Tripulação e as Melhorias
(M24-T46, TD-053): os campos `crew` e `upgrades` de um save antigo são lidos em `PlayerSave.RetiredCrew` e
`RetiredUpgrades` (só unidades e níveis) e, ao abrir a sessão, `Persistence/CrewRemovalMigration.Refund` devolve as
Moedas que custaram (preços de `economy.json → crew_removal_refund`, somas por `SafeMath`), põe os dois em `null` e o
save é gravado sem eles; `GameSession.CrewRefund` guarda o que foi devolvido para o aviso único da tela.
Cada passo está em `SaveMigrations.Upgrade`.
- Troca de curva de nível de varas e peixes (TD-041): ao carregar, depois da curva do Pescador, o `GameSession` chama
  `LevelCurveMigration.MoveToCurrentCurves`. Um save na curva 1 tem cada vara (Inventário e Mercado) levada ao nível
  de `rods.json → legacy_levels_v1` e cada peixe (Aquário e Mercado) ao de `fish_level.legacy_levels_v1` (com o XP que
  ele tinha dentro do nível aplicado de novo), nunca para baixo; depois a curva do save passa a ser a atual. Um save
  com 0 (jogador novo, criado já na curva atual) não é mexido. Mudou uma curva de um jeito que mexe em quem já joga?
  Gere um mapa novo (`tools/Progressao/niveis_100.py`), aumente `level_curve_version` e trate a versão anterior na
  migração.
- Barcos com nível (M24-T13): `IGearService.UpgradeBoat` cobra Moedas e Conchas do próximo nível (`GameConfig.
  BoatUpgradeCost / BoatUpgradeShellCost`), chama `Sync` da pesca antes (os ciclos já completos ficam no nível antigo)
  e grava `BoatLevels`. O bônus em uso vem de `GearRules.BoatBonus` (nível do save), usado pela pesca
  (`SpendAttempt`), pela tela (`GetGear`) e pelo `CatchSimulator` (parâmetro `boatLevel`).
- Barcos por fragmentos (TD-042; em qualquer mapa desde o M24-T47, TD-054): na pesca, depois de cada peixe puxado,
  `BoatFragmentRules.RollOnCatch` faz um sorteio por barco de `GameConfig.FragmentBoats` (todos menos o inicial, em ordem
  de categoria), com o `Rng` do mesmo ciclo e o `fragment_drop_chance` de cada um; a pesca offline também, se
  `boat_fragments.drop_on_offline_fishing`. `GameConfig.TryGetBoatForMap` ficou só para o Boss Mundial
  (`WorldBossRules.BoatFor`). `BoatFragmentRules.Add` (usado também pelo Passe e pelos testes) soma e
  monta o barco ao chegar em `fragments_required`. O `FishingUpdate.Fragments` leva cada fragmento para a tela e para
  o "Bem-vindo de volta".
- Varas e roupas por fragmentos (TD-048; em qualquer mapa desde o M24-T47, TD-054): na mesma tentativa, depois dos
  fragmentos de barco, `RodFragmentRules.RollOnCatch` faz um sorteio por vara de `GameConfig.FragmentRods` (em ordem de
  categoria) e `ClothingRules.RollOnCatch` o do Fragmento de Roupa; depois `ClothingRules.GainUse` dá 1 ponto de uso por peixe a cada peça montada. `RodFragmentRules.Add`
  monta a vara (Inventário, Nv.1, em uso se a categoria for maior); a pesca lê a vara em uso a cada tentativa. Os bônus
  das roupas vêm de `ClothingRules.Bonuses`: Luvas no `gearBonus`, Chapéu/Botas/Camiseta em `CatchRules.WithClothing`,
  Calça no XP em `ApplyCatch`. `FishingUpdate.RodFragments`, `ClothingFragments`/`ClothingFragmentsHave` e
  `ClothingLevels` levam tudo para a tela. O validador (`ValidateRodFragments`) garante que a categoria que cada mapa
  pede seja a da Vara Inicial ou a de uma vara com chance maior que 0 (a progressão não trava).
- Passe do Pescador (TD-042): `LocalPassService` lê os marcos de `pass.json` e grava só o que foi resgatado. Resgatar
  chama `Sync` da pesca antes (os fragmentos podem trocar o barco ou a vara em uso). Desde o M24-T32/T33 a recompensa
  também tem `rod_id`/`rod_fragments` e `clothing_fragments`. Subir de nível não paga nada sozinho
  (`FisherLevelRules.AddXp` só soma XP e níveis); os Dólares do A-111 estão na faixa grátis.
- Tripulação e Melhorias (TD-039, TD-040): retiradas no M24-T46 (TD-053). A venda ao NPC (`CatchRules.SalePrice`) não
  tem mais multiplicador, e o piso dos vendedores simulados (`MarketRules.BotFloor`) é o preço de venda ao NPC + 1.
- Troca de curva de XP (TD-038): ao carregar, o `GameSession` chama `FisherLevelRules.MoveToCurrentCurve`, que
  converte o XP total (`FisherXpTotal`) no nível da curva atual quando o save é de uma curva mais antiga, sem nunca
  baixar o nível. Mudou a tabela de XP de um jeito que mexe em quem já joga? Aumente `fisher.xp_curve_version` em
  `progression.json`.
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
- **Um tema por mapa** (`SceneTheme.For`): pasta e prefixo das camadas pintadas, posição do sol,
  correnteza. Ao chegar a outro mapa (`GameRoot.MapChanged`), a cena é destruída e montada de novo.
- **Visual "Lago Dourado — Clean Premium"** (V0.2, M14; fonte de verdade: `docs/ART_BIBLE_V0_1.md`):
  - **Tema em dados** (`Visual/VisualTheme`, lê `Resources/Visual/tema_visual.json`): paleta, cores de
    raridade, cores das categorias de tamanho, cor do Excepcional, opacidades, intensidade do brilho,
    tempos de aviso, de celebração, do fade das janelas e do contador de moedas. Nenhuma cor fica
    espalhada nas telas: elas usam `UiSkin` (que lê o tema), `UiSkin.RarityColor(id)` e
    `UiSkin.SizeColor(id)`; uma linha "48,6 cm · Grande · Nv. 3" usa `UiSkin.SizeLine`, que escreve o
    tamanho na cor dele onde quer que ele esteja. A busca pelo nome (`UI/NameSearch`: campo com lupa e ✕, estilo `UiSkin.SearchField`; no Mercado, `MarketWindow.DrawSearch` o põe no alto de cada aba com peixes;
    `NameSearch.Matches` ignora maiúsculas e acentos) filtra só o que é mostrado. Filtros coloridos usam `UiSkin.ColorChip` (ponto na cor; fundo e borda
    na cor quando escolhido).
  - **Arte trocável** (`Visual/ArtAssets`): toda imagem é um arquivo em `Resources/Arte`, carregado
    pelo nome (`ArtAssets.Texture("Peixes/fish_lambari_master")`, `ArtAssets.Sprite(caminho, largura em
    unidades, pivô)`, `ArtAssets.Icon(Icons.X)`). Um arquivo que falta devolve `null` e quem chamou
    volta para a arte de reserva feita por código (`Art.cs`, `FishLooks.cs`), então o jogo nunca
    quebra por falta de arte. `Editor/ArtImportSettings` configura a importação (tamanho real, sem
    reescalar para potência de 2, mipmaps, até 4096 px; fontes com fallback do sistema).
  - **Arte provisória gerada por script** (`tools/Arte/`, Python com numpy e Pillow, sempre igual):
    `gerar_icones.py` (44 ícones brancos, pintados na cor certa pelo jogo), `gerar_peixes.py` (20
    peixes de lado, mesmo "artista") e `gerar_cenarios.py` (camadas dos mapas, nuvens, barco,
    pescador, varas, fotos dos mapas e das Expedições). A lista do que falta como arte final e as
    regras de tamanho estão em `docs/ASSETS_PENDENTES.md`.
  - **Equipamento na cena** (`Scene/EquipmentLook`, A-096): a `FishingScene` lê `GearView` (BoatId,
    RodId, BaitId) a cada quadro e, quando muda, troca o casco (pintura da Loja atrás do pescador e a
    lateral da frente recortada na frente dele), põe a vara pintada ao longo da vara animada
    (`FishermanRig.UseRod`) e pendura a isca na boia (`UseBait`). Posições e tamanhos ficam em
    `Resources/Visual/equipamento_cena.json`; `tools/Arte/equipamento_na_cena.py` recorta as laterais
    (`Arte/Barcos/Cena/boat_XX_frente.png`), mede as varas e grava o arquivo (`--previa` monta uma
    prévia fora do Unity).
  - **Arte dos mapas 3 e 4** (`tools/Arte/processar_mapas_3_4.py`): mesmo processo, com o fundo
    votado na borda inteira (verde, magenta ou azul), limpeza do magenta que vaza em partes finas, nuvens
    lado a lado ou empilhadas, o lado oposto espelhado quando uma margem vem sem fundo chapado, e a faixa
    do meio montada com duas pinturas diferentes alternadas (`alternate`, pedidos Refazer 01 a 04). Os
    animais novos (`LivingAnimals`: araras-azuis, tuiuiú, colhereiros, guarás, trinta-réis, caranguejo,
    boto) reaproveitam os comportamentos existentes com outra arte.
  - **Mapas 5 a 10** (A-104, A-105): `tools/Progressao/aplicar_mapas.py map_05 map_06 ...` copia de
    `docs/propostas/mapas_5_10.json` para `/config` as espécies, os mapas, a vara mínima de cada mapa e a
    raridade nova que eles usam (sem duplicar nada se rodar de novo). A arte de cada par de mapas tem seu
    script (`tools/Arte/processar_mapas_5_6.py`), que reaproveita `processar_mapas_3_4.py` trocando só os
    nomes e tira o mar pintado embaixo das faixas do horizonte (`drop_painted_sea`). `gerar_cenarios.py
    CostaDeCoral ArquipelagoDoSol` pinta o provisório só desses mapas. O cenário de cada mapa novo é um
    `SceneTheme` em `FishingScene.For`; a paisagem viva, uma cena em `paisagem_viva.json`, e os animais
    novos são comportamentos existentes (`Flock`, `Surfacer`) com outra arte, ou rotinas curtas novas
    (`FlyingFish`, `Spout`, `Breach`). `processar_mapas_7_8.py` separa os peixes da grade pela forma
    (`grid_by_pieces`), não pela linha da célula. Mapas noturnos: `SceneTheme.Night` e `SunGlow` (luz fria),
    brilho de plâncton feito no jogo; o provisório é o `open_sea` de `gerar_cenarios.py`.
  - **Tamanho e desempenho** (TD-031, TD-032): `ArtImportSettings.MaxSizeFor` limita cada tipo de
    imagem ao que a tela mostra (as fontes continuam grandes); `tools/Arte/otimizar_png.py` recomprime os
    PNG sem perdas e roda depois de qualquer script de arte; o `GameRoot` baixa para 15 quadros por segundo
    sem foco.
  - **Relógio** (TD-030): as regras recebem o `SteadyClock` do `GameSession`, que nunca volta no tempo.
  - **Arte final do proprietário** (`tools/Arte/processar_pedidos.py`): recorta as imagens do ChatGPT
    (fundo verde, magenta ou preto), separa grades de peixes e ícones, estende o céu e as camadas de
    montanha até as 26 unidades (espelhando faixas sem marcos, para a cachoeira aparecer uma vez só) e
    grava os arquivos do jogo. `tools/Arte/finais.txt` lista esses arquivos, e `finais.proteger_finais()`
    impede os geradores provisórios de sobrescrevê-los.
  - **Margens e reflexos:** as margens (`near_left` / `near_right`) ficam presas às bordas da tela, com
    altura fixa por mapa (`SceneTheme.NearLeftHeight` etc., `ArtAssets.SpriteByHeight`); cada camada de
    terra ganha um reflexo na água, espelhado, achatado, escurecido e cortado em 12 faixas que balançam
    fora de compasso (`Reflect`, `ReflectionSlice`). A superfície se mexe com 10 fileiras de brilhos de
    onda em perspectiva, deslizando (`WaterWaves`, textura `Arte/Agua/ondas.png`).
    O pescador pintado senta no banco entre as duas camadas do casco (`barco_fundo` atrás,
    `barco_frente` na frente, separadas por `processar_pedidos.split_boat`) e segura a vara apoiada no
    joelho (`FishermanRig`: pivô onde ele senta, mãos medidas na imagem).
  - **Cena em camadas** (`FishingScene.BuildPainted`): céu, montanhas, morros, mata da margem, água e
    os dois cantos, cada camada com 26 unidades de largura e base numa altura fixa em relação ao
    horizonte; cantos presos às bordas da tela pela proporção da câmera. Por cima, o movimento: nuvens
    (sprites separados), brilho do sol que respira (`Breathe`), reflexos (`Twinkle`), pássaros, peixes
    saltando, juncos balançando.
  - **Vida na cena** (`Scene/Life.cs`, montada em `FishingScene.BuildLife`): poeira no ar
    (`GoldenMotes`), libélulas (`Dragonflies`), sombras de peixe (`FishShadows`), raios de sol
    (`SunRays`), névoa (`HorizonMist`), brilhos no reflexo (`SunGlints`), anéis do barco (`BoatRipples`)
    e respingos (`Droplet.Splash`, usado pelo `FishermanRig`). Tudo de baixa opacidade e escalado por
    `ambient_life` do tema (0 desliga).
  - **Paisagem viva** (`Scene/LivingScenery.cs`, `Scene/LivingAnimals.cs`, montada em
    `FishingScene.BuildLiving`; dados em `Resources/Visual/paisagem_viva.json`, lido por `LivingConfig`):
    plantas soltas na margem do fundo (com reflexo), nos cantos e boiando (`WindSway`: uma mola que verga
    com as rajadas de `Wind`, que atravessam a tela da esquerda para a direita em intervalos sorteados;
    `Floating` para as vitórias-régias). Os animais são chamados pelo **diretor de cenário**
    (`SceneDirector`): um por vez, descanso sorteado entre um e outro, sorteio por peso sem repetir o
    último, nada novo com janela aberta (`GameRoot.WindowOpen`, marcado pelo `Hud`), celebração ou
    viagem. Cada animal é uma corrotina em `LivingAnimals` que termina quando ele sai; posições por mapa
    ("spots", medidas a partir da borda da tela, como os cantos) no JSON. Libélulas, sombras e saltos de
    peixe pedem vez a `SceneDirector.AllowSmall()`. Os quadros vêm de `tools/Arte/processar_vivos.py`,
    todos do mesmo tamanho e alinhados num ponto fixo (bico, pés ou traseira), para a troca de quadro
    não fazer o corpo pular. Novas ordens de desenho: `OrderSkyLife`, `OrderShorePlants`,
    `OrderShoreLife`, `OrderWaterLife`, `OrderCornerBack`, `OrderCornerFront`.
  - **Card oficial de peixe** (`UI/FishCard`, dados em `FishCardModel`): mesmo layout para toda
    raridade; a raridade é borda, selo e barra; o Excepcional tem selo dourado com brilho que passa
    (`FishCard.ExceptionalSeal`); usado na Caixa, Aquário, Mercado (varas também, com `Art`).
  - **Três níveis de intensidade:** calmo (cena e menus), recompensa (avisos com acento colorido,
    contador de moedas que sobe com "+N") e celebração (`UI/Celebrations`: faixa no alto com raios,
    2–4 s, sem bloquear, uma de cada vez). O `Hud` escolhe o momento; `GameRoot.Celebrations.Show`
    também é usado pelo leilão vencido.
  - **Peças da interface** (`UiSkin`): `FloatingPanel` (painel com sombra), `IconButton`,
    `AccentPill`, `Tag`, `Bar(rect, fração, cor)`, `DrawOutline`, `DrawGlow`, `IconBadge`, `CoinAmount`,
    e as janelas via `WindowFrame.Panel` + `WindowFrame.Header(…, ícone)`.
  - **Janelas no papel ("Diário de bordo", A-170):** `WindowFrame.Panel` desenha a moldura de madeira e a folha e
    liga o **modo papel** do `UiSkin` (`UiSkin.Paper`); o `Hud` desliga ao chamar `WindowFrame.DrawInfoBalloon` depois
    das janelas, e `WindowFrame.Dialog` desliga para os diálogos (que continuam no painel noturno). No modo papel as
    cores (`UiSkin.Text`, `Muted`, `Gold`, `Accent`, `RarityColor`…) respondem com a tinta escura e os estilos
    registrados com `skin.Track(estilo)` trocam texto e fundo na hora; um estilo copiado do skin numa janela
    (`new GUIStyle(skin.X)`) precisa de `skin.Track(...)` para acompanhar. Cena escura dentro da janela (tanque,
    arquipélago, carta náutica, parede da Loja, batalha) vai num `using var night = UiSkin.NightScope();` depois de
    `WindowFrame.Photo(skin, rect)`. Um preenchimento escuro atrás de texto usa `UiSkin.OnSheet(cor)`. Sub-abas:
    `Panel(…, bookmarks: true)` (ou `Draw(…, bookmarks: true)`) e `WindowFrame.Bookmarks(...)`; rodapé com
    `WindowFrame.Footer`; fechar com `WindowFrame.CloseSeal` (o `Header` já desenha).
  - **Dois cuidados com texto:** os rótulos do `UiSkin` quebram linha, então um rótulo da largura
    exata do texto (`CalcSize`) pode jogar o último dígito para uma linha invisível. Dê folga (+4 px)
    ou use `CoinAmountWidth` para moedas. E um texto que cita um número de `/config` (tempos, taxas,
    limites) recebe esse número da view do serviço, nunca escrito na frase.
  - **Tamanhos de texto** (A-149, TD-037): os pequenos ficam em constantes do `UiSkin` (`SmallSize` 15,
    `ChipSize` 14, `BadgeSize` 13). Uma linha do texto pequeno precisa de `UiSkin.SmallLine` (20 px) de
    altura, senão as pernas de g, p e ç são cortadas; o que fica embaixo começa pelo menos 18–20 px depois.
    Texto direto sobre arte, sem painel atrás, usa `UiSkin.ShadowLabel` (sombra de 1 px).
- **Camadas e paralaxe:** câmera ortográfica que oscila devagar; cada camada acompanha a câmera numa
  fração diferente, criando profundidade (2.5D). Ordem de desenho nas constantes de `FishingScene`.
- **Animação sincronizada ao serviço:** `FishermanRig` lê `FishingStatus` (início e fim do ciclo)
  para arremessar, esperar, mostrar a mordida e puxar; o peixe só aparece quando o serviço entrega a
  captura (`GameRoot.CatchesArrived`).
- **Interface em IMGUI** (`Hud`, janelas, `UiSkin`): tela virtual de 1080 px de altura, escalada.
  Todos os estilos ficam em `UiSkin`, o que isola uma futura troca para UI Toolkit. Fontes: Fredoka
  nos títulos e números, Nunito SemiBold no texto e Nunito Bold no negrito (`Resources/Fontes`; a
  SemiBold é gerada por `tools/Arte/gerar_nunito_semibold.py`).
- **Textos:** sempre de `GameTexts`. Nunca escreva uma frase em PT-BR direto num `.cs` fora de `Texts/`.
- **Avisos e central de notificações** (`ToastFeed`): `Push(texto, tipo, ícone, notify)` mostra o
  aviso; com `notify: true` ele também entra na lista do sino (até 50, só enquanto o jogo está
  aberto). Entram: espécie nova, recorde, Excepcional, subir de nível, Mercado/Leilão, Expedição,
  ataques recebidos na Arena.
- **Áudio** (`Audio/`, V0.2): os sons são arquivos `.wav` em `client-unity/Assets/Resources/Sons`,
  carregados pelo nome (`SoundBank`). Trocar um som = trocar o arquivo mantendo o nome (um arquivo
  que falta fica em silêncio). Os arquivos atuais saem de `tools/Audio/gerar_sons.py` (Python com
  numpy e scipy; sempre iguais). Cada aviso diz qual som faz (`ToastFeed.Push(…, sound:)`, tipo
  `SoundCue`; sem isso, só o aviso de subir de nível tem som — A-080); `GameAudio` toca um por quadro, o mais importante, e
  cuida do ambiente: por mapa, uma lista de gravações longas (`AmbiencePlaylist`, arquivos em
  `Resources/Sons/Ambiente`, lista em `Resources/Sons/ambiente.json`) tocadas em ordem sorteada, sem
  repetir a última, com passagem suave entre duas `AudioSource`; enquanto um mapa não tem gravação,
  usa os dois loops antigos (mar e brisa) com fade. `tools/Audio/processar_ambiente.py` converte as
  gravações (Ogg Vorbis, volume igualado, fade nas pontas). `Editor/AudioImportSettings` ajusta a
  importação: ambiente comprimido (Vorbis), efeitos sem compressão.
- **Preferências de apresentação** (`GameSettings`): som, som ambiente e volume, no `PlayerPrefs` do
  PC. Não entram no save porque não mudam nenhuma regra.
- **Modo compacto** (Opções → Modo compacto): janela de 480×270 só com a cena, uma linha de status e a
  última captura. É só apresentação; o serviço pesca igual. No Editor o tamanho da janela não muda.
- **Transição de menus:** a janela que abre faz um fade (0,18 s por padrão, `window_fade_seconds` no tema).
- **Tripulação e Melhorias na tela:** retiradas no M24-T46 (A-172): saíram `UI/CrewWindow`, `UI/UpgradesPanel`, a aba
  do menu, `GameRoot.SyncCrew`/`CrewWelcome`/`CrewCoinsPerSecond` e a parte da Tripulação do `WelcomeBackDialog` (que
  hoje mostra só a pesca). Ao abrir um save antigo, `GameRoot.AnnounceCrewRefund` mostra uma vez as Moedas devolvidas.
- **Passe do Pescador na tela** (M24-T17, A-158): abre pelo atalho redondo "Passe" da lateral direita (A-169), com a
  contagem vermelha de `GameRoot.Pass.ClaimableCount`. A janela `UI/PassWindow`
  pede `Game.Pass.GetPass()` a cada 0,5 s, desenha só as colunas visíveis da trilha (100 marcos) e manda os pedidos por
  `GameRoot.ClaimPass`, `ClaimAllPass` e `BuyPassPremium` (que mostram os avisos e a comemoração de barco montado).
  `GameRoot.PushPassMilestones` avisa os marcos alcançados pela pesca.
- **Fragmentos na tela** (M24-T16, A-157; M24-T32/T33, A-163/A-164): o `Hud` avisa os fragmentos de barco, de vara e de
  roupa e os níveis das roupas por `UI/FragmentNotices` (no máximo um aviso por tipo a cada 8 s, somando o que chega no
  meio) e comemora barco, vara e roupa montados; a `ShopWindow` mostra "12/20" na etiqueta e "Fragmentos 12/20 · Caem em
  todos os mapas" (M24-T47) com a barra na ficha (sem Comprar) nas abas Varas e Barcos, e a aba Roupas (`DrawClothingCard`: nível, barra de
  uso, bônus agora e no Nv.100, "Montar (N fragmentos de roupa)" por `GameRoot.AssembleClothing`); o `WelcomeBackDialog`
  soma os fragmentos e os níveis da pesca offline; a `PassWindow` mostra as linhas de vara e de roupa.
- **Boss Mundial na tela** (M24-T29, A-165): `GameRoot.SyncWorldBoss` chama `Game.WorldBoss.Update()` na sincronização de
  0,25 s (é isso que faz o Cardume atacar com o jogo aberto, mesmo com a janela fechada), guarda `GameRoot.WorldBoss` e
  avisa o começo, a faixa nova e o pagamento. Entra-se pela janela Eventos (abaixo); durante o evento, o atalho redondo
  Eventos do `Hud` vira "Boss Mundial" (dourado, pulsando, com o tempo que falta) e abre direto a janela do Boss. A janela
  `UI/WorldBossWindow` só
  lê `GameRoot.WorldBoss`: Cardume à esquerda (com `ArenaWindow.DrawFish`, agora `internal`), Boss à direita, números
  de dano quando a contagem de ataques sobe, faixas, ranking e Dólares do top 10; fora do horário, o último resultado.
- **Mapas de cada barco** (M24-T42, A-168, TD-051; desde o M24-T47, TD-054, só para o Boss Mundial):
  `equipment.json → boats[].extra_fragment_maps` soma mapas ao `map_id` principal. `GameConfig` monta o índice mapa →
  barco (`TryGetBoatForMap`, `MapsOf`). Os fragmentos caem em qualquer mapa, e a `ShopWindow` mostra "Caem em todos os
  mapas" e a chance de cada item. Os
  barcos removidos e o que eles viram ficam em `equipment.json → boat_migration` (lido pela `BoatModelMigration`).
- **Clãs na tela** (M24-T49, A-174): `GameRoot.SyncClan` chama `Game.Clans.Update()` na sincronização de 0,25 s (fecha
  o dia do ranking e avisa um pagamento ou um peixe que saiu), `GameRoot.Clan` é atualizado com o jogador, e as
  intenções são `CreateClan`, `LeaveClan`, `BuyClanSlot`, `SetClanFish`, `ClearClanFish` e `ClanFishCandidates`. A
  `UI/ClanWindow` é uma sub-aba do Perfil (o `Hud` mostra "Perfil | Clã" embaixo da aba Perfil quando uma das duas está
  aberta) e só lê `GameRoot.Clan`: o requisito do Nv.100, "Criar um clã", o clã (resumo, Cardume do Clã, membros) e o
  bloco do ranking diário com os prêmios do top 3; tem dois diálogos próprios (escolher o peixe e "Sair do clã?").
- **Menu Eventos** (M24-T39, A-167): `UI/EventsWindow` mostra os eventos em cartões (hoje só o Boss Mundial), lendo
  `GameRoot.WorldBoss`; "Abrir" abre a `WorldBossWindow`, e o "‹ Eventos" dela volta. Um evento novo acrescenta o tipo
  em `EventsWindow.EventKind` e um desenhador de cartão; nenhuma regra mora na janela.
- **Menu de cima** (M24-T43, A-169): o `Hud` desenha a faixa escura (`DrawStrip`) com o cartão do jogador à esquerda
  (clique abre o Perfil), as **4 abas** no centro (`GroupFishing`, `GroupAdventure`, `GroupTrade`, `GroupProfile`;
  rótulos em `GameTexts.Navigation`; a aba Tripulação saiu no M24-T46), o sino e as opções à direita (`TopRightExtent`). As sub-abas da aba
  ativa (Expedição | Arena | Eventos e Mercado | Loja) saem em `DrawSubTabs`. Sem janela aberta, a placa pendurada
  (`DrawIncomePlaque`) mostra "MOEDAS" e o total contando (até o M24-T46 mostrava também a renda da Tripulação). Os atalhos redondos (Mapa, Aquário, Passe, Eventos,
  Ranking) ficam em coluna na direita (`DrawShortcuts`, constantes `Shortcut*`); com uma janela aberta ficam cobertos e
  não respondem. As peças de desenho (faixa, abas coloridas, placa, moldura redonda, contagens) estão em
  `UI/TopMenuArt`; os ícones pendentes (`ico_aba_*`, `ico_atalho_*`, `ui_atalho_moldura`) são usados quando o arquivo
  existir (`ASSET_PENDENTE`). O tutorial aponta para as abas pelas mesmas posições.
- **Janelas "Diário de bordo"** (M24-T44, A-170): moldura, folha, etiqueta, lacre e marcadores em `WindowFrame`; as cores e
  estilos no papel em `UI/UiSkinPaper` (parte do `UiSkin`). Como usar: veja "Janelas no papel" acima.
- **Tela de erro** (M24-T38, A-166, TD-050): `GameRoot.Awake` monta a cena e o som em `try/catch` e sempre acrescenta o
  `Hud`; `Hud.Start` e `Hud.OnGUI` também ficam em `try/catch` e mandam a exceção para `GameRoot.ReportFailure`, que
  para o serviço e mostra `DrawStartupError` (tipo e mensagem, momento, três pontos do código, pasta do save; textos em
  `GameTexts.Startup`). Se até o `UiSkin` falhar, `DrawPlainError` usa o estilo padrão do Unity. `ExitGUIException` é
  relançada (fluxo normal do IMGUI).

## 7. Painel de Desenvolvimento (Editor)

Menu **Fishing Idle → Painel de Desenvolvimento** (`DevPanelWindow`).

- **Visão geral / Roadmap:** lêem `docs/roadmap.json` e `version.json` (`RoadmapData.cs`). Não
  escrevem nada. O status mostrado é o que está no arquivo — nunca "atividade em segundo plano".
- **Balanceamento** (`BalanceEditor`): edita os JSON como árvore, preservando ordem, notas e
  formatação (um teste garante que salvar sem editar não muda nenhum byte). Valida antes de gravar.
  Com o jogo rodando, "Salvar e aplicar" recarrega o config na hora (`GameRoot.ReloadConfig`).
  Seções: Pesca, Espécies, Mapas e chances, Distribuição de tamanho, Raridades (multiplicadores de
  venda, atributos e XP, e `size_weight_multipliers` com a chance final de cada tamanho por
  raridade; um tamanho não listado vale 1 e só é gravado se for mudado), Sucesso da pesca (chance-base
  por raridade, piso e teto, barcos, iscas e um simulador: mapa + vara + nível + barco + isca + N
  tentativas → capturas, escapes, taxa real, por raridade, XP/h, Moedas/h, Conchas/h e custo da
  isca; roda com os números da tela, mesmo não salvos), XP, Varas (com a coluna "Puxar"), Economia e
  Outros arquivos.
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

Sem .NET na máquina, o GitHub faz o mesmo a cada envio (`.github/workflows/verificar.yml`, TD-044) e grava o
resultado na branch `ci-resultados`: `git fetch origin ci-resultados && git show origin/ci-resultados:resultado.txt`.

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
Mercado, preço final dos leilões e o Sucesso da Captura (seção 7: o jogo de hoje com e sem a
chance, e combinações de equipamento com `CatchSimulator`). No topo, "Pontos de atenção" lista o que chama atenção nos números
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
