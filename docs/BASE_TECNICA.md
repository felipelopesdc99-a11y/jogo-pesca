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
│   │   ├── Profile/       Cardume (posições, bônus, Força), Perfil, Inventário, Enciclopédia
│   │   ├── GameSession.cs Estado vivo de um jogador (config + save + relógio + armazenamento)
│   │   ├── PlayerService.cs
│   │   └── LocalGame.cs   Ponto de entrada: monta os serviços locais
│   ├── Game/           FishingIdle.Game         Apresentação no Unity
│   │   ├── Bootstrap/     GameBootstrap (Play em qualquer cena), GameRoot (ponte com o serviço), GamePaths
│   │   ├── Scene/         Arte provisória por código (Art), cena (FishingScene), pescador (FishermanRig), ambiente
│   │   └── UI/            HUD, Caixa de Pesca, avisos, estilos (IMGUI)
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

**Tempo com o jogo fechado não é pesca online.** Se o jogo ficou mais de `max(3 ciclos, 2 minutos)`
sem ser visto (fechado, PC dormindo, relógio adiantado), o serviço entrega só o que foi pescado
até `last_seen_at_ms` e recomeça o ciclo a partir de agora. A pesca offline (60s por captura, até
24h) é o Milestone 5 e vai usar exatamente esse intervalo.

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

## 4. Balanceamento (/config)

- No Editor, o jogo lê direto da pasta `/config` do repositório. Num build, lê a cópia que o
  `ConfigBuildStep` coloca em `StreamingAssets/config` (essa cópia não é versionada).
- O jogo carrega `fish_catalog`, `maps`, `progression`, `rods`, `economy` e `arena` (a parte do
  Cardume). `expeditions.json` entra no Milestone 6.
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
  e adiciona o Cardume. Cada passo está em `SaveMigrations.Upgrade`.

## 6. Apresentação (Game)

- **Sem cena montada à mão.** Ao apertar Play, `GameBootstrap` cria o `GameRoot`, que inicia o
  serviço e monta a cena por código (`FishingScene`). Funciona em qualquer cena.
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
