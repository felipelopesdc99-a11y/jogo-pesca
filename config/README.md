# /config — balanceamento orientado a dados

Todo valor em que o jogo se apoia mora aqui, nunca dentro do código do jogo (regras 2 e 43 do GDD).

| Arquivo | Responsável por |
|---|---|
| `fish_catalog.json` | Dados próprios da espécie: raridade, faixa de tamanho, atributos base, valor de venda, XP de alimento, XP do Pescador |
| `maps.json` | Mapas, pools de peixes por mapa com pesos de captura, nível de desbloqueio, vara mínima, regras de viagem |
| `progression.json` | Curva de XP do Pescador, curva de XP do peixe (até o Nv.100) e bônus de atributo por nível do peixe, distribuição de tamanho, regras de modificador de raridade/tamanho/nível, regras de alimentação, intervalos de pesca |
| `rods.json` | Definições das varas, bônus no Nv.1 e no Nv.100 (com a curva entre eles), custo de cada nível (Moedas e Conchas), regras de revenda; varas por fragmentos (o mapa onde cada vara cai, quantos fragmentos montam e a chance por peixe puxado; o valor de referência no Mercado) |
| `economy.json` | Moedas, fórmula de venda ao NPC, obtenção de Conchas, peixes que pedem confirmação na venda da Caixa, valores de Mercado e Leilão, limite do Aquário, VIP (preço em Dólares, dias, bônus de XP offline e o ciclo da pesca offline com VIP, `offline_cycle_seconds`) |
| `arena.json` | Energia, seleção de oponentes, Honra, constantes de combate, formação, regras de Cardume, métrica de Força, Loja da Arena |
| `expeditions.json` | Durações, Força Recomendada, curvas de eficiência, recompensas |
| `arena_bots.json` | Só no jogo local: como os 200 adversários simulados da Arena são montados e com que frequência atacam você |
| `market_bots.json` | Só no jogo local: vendedores e compradores simulados do Mercado (quantos anúncios, preços de referência, chance de vender) |
| `equipment.json` | Barcos, iscas e roupas: bônus na Chance de Sucesso da Captura, preço e tentativas de cada isca; barcos por fragmentos (o mapa de cada barco, quantos fragmentos montam e a chance por peixe puxado; se a pesca offline também dá), níveis dos barcos (bônus no Nv.1 e no Nv.100, custo de cada nível); roupas (`clothing`: chance do Fragmento de Roupa, custo de cada peça na ordem de montagem, bônus de cada peça no Nv.1 e no Nv.100 e o XP de uso de cada nível) |
| `crew.json` | A Tripulação automática (M24): os 10 tripulantes (nome, preço da 1ª unidade, quanto cada unidade a mais encarece, Moedas e XP por segundo), quantos do anterior liberam o próximo, marcos de quantidade e da frota, e a renda com o jogo fechado (100% nas primeiras horas, depois a taxa reduzida até o teto). Resultado da simulação em `docs/propostas/tripulacao.json` (`tools/Progressao/simular_tripulacao.py`); a renda por unidade foi recalibrada com as Melhorias (A-155) |
| `pass.json` | O Passe do Pescador (M24-T17): o preço do Premium em Dólares e os 100 marcos (Nv.10 ao Nv.1000), cada um com a recompensa grátis e a Premium (Moedas, Conchas, Dólares, fragmentos de um barco, de uma vara e de roupa). Gerado por `tools/Progressao/passe_fragmentos.py gerar` (resultado da simulação em `docs/propostas/passe_fragmentos.json`) |
| `world_boss.json` | O Boss Mundial (M24-T29, A-165): nome (sugestão, OD-069), horário (19:00 de Brasília, UTC−3 fixo, por 10 minutos), intervalo dos ataques automáticos do Cardume (3 s), quantos ataques atrasados contam depois de um travamento, a defesa do Boss, as 6 faixas de dano (porcentagem do dano máximo do dia e a recompensa de cada uma: minutos de renda em Moedas e Conchas, Fragmentos de Roupa e fragmentos de barco), os Dólares do top 1 a 10 e a chave `online_rewards_enabled` (desligada no jogo local), e a renda de referência por nível, gerada por `tools/Progressao/boss_mundial.py` a partir do `pass.json` |
| `upgrades.json` | As Melhorias compradas com Moedas (M24): as de tripulante (`crew_upgrades`: com quantas unidades cada uma libera, o fator de preço sobre a unidade daquele número, o nome e o complemento do nome de cada tripulante; ×2 na renda dele, uma vez só) e as gerais com níveis (`general_upgrades`: efeito, quanto cada nível soma, quantos níveis, preço do nível 1 e quanto cada nível encarece). Resultado da simulação com a Tripulação em `docs/propostas/melhorias.json` (`tools/Progressao/simular_melhorias.py`) |

## Como editar

O jeito mais fácil é pelo **Painel de Desenvolvimento dentro do Unity** (menu Fishing Idle → Painel
de Desenvolvimento → Balanceamento). Ele valida tudo antes de gravar, só altera os valores que você
mudou e, com o jogo rodando, aplica na hora. Editar os arquivos à mão também funciona: o jogo valida
ao apertar Play e, se algo estiver errado, mostra a lista de problemas na tela.

Onde fica cada coisa no painel:

| Quero mudar… | Seção do painel | Arquivo |
|---|---|---|
| Preço de venda, tamanho, XP e atributos de um peixe | Espécies | `fish_catalog.json` |
| Chance de cada peixe em cada mapa | Mapas e chances | `maps.json` |
| Chance de Pequeno, Adulto, Grande e Excepcional | Distribuição de tamanho | `progression.json` |
| Quanto o Raro vale a mais (venda, atributos, XP) e a chance de tamanho por raridade | Raridades | `progression.json` |
| Quanto cada raridade morde na pesca (o 0,233333 do A-160; era 0,35 no A-159) | Raridades (coluna "Na pesca") | `progression.json` |
| Chance de puxar o peixe (por raridade, mínimo e máximo), barcos e iscas, e o simulador | Sucesso da pesca | `progression.json`, `equipment.json` |
| Bônus de puxar de cada vara no Nv.1 e no Nv.100 | Varas (coluna "Puxar") | `rods.json` |
| Tempo de pesca | Pesca | `progression.json` |
| Níveis do Pescador e do peixe | XP do Pescador | `progression.json` |
| Varas (bônus, curva, nível máximo e custo de cada nível; fragmentos e chance por peixe) | Varas | `rods.json` |
| Roupas (chance do Fragmento de Roupa, XP de uso, bônus de cada peça no Nv.1 e no Nv.100) | Sucesso da pesca → Roupas (o custo de cada peça: edite o arquivo) | `equipment.json` |
| Barcos (bônus no Nv.1 e no máximo, fragmentos e chance por peixe, custo de cada nível) | Sucesso da pesca → Barcos | `equipment.json` |
| Preço mínimo, multiplicador geral do preço de venda dos peixes, Conchas, Aquário, Mercado | Economia | `economy.json` |
| Preço, duração, bônus de XP e ciclo offline do VIP | Economia → `vip` | `economy.json` |
| Arena, Cardume, Expedições | Outros arquivos | `arena.json`, `expeditions.json`… |
| Tripulação (preços, renda, marcos, offline) | Ainda sem seção no painel: edite o arquivo; o jogo valida ao apertar Play | `crew.json` |
| Melhorias (preços, efeitos, níveis, nomes) | Ainda sem seção no painel: edite o arquivo; o jogo valida ao apertar Play | `upgrades.json` |
| Boss Mundial (horário, intervalo, faixas, Dólares do top 10) | Ainda sem seção no painel: edite o arquivo; o jogo valida ao apertar Play. Mudou o `pass.json`? Rode `tools/Progressao/boss_mundial.py` | `world_boss.json` |
| Passe do Pescador (recompensas, preço do Premium) | Ainda sem seção no painel: edite o arquivo ou gere de novo com o script; o jogo valida ao apertar Play | `pass.json` |

## Regras

- **Os pesos de captura ficam em `maps.json`, não em `fish_catalog.json`.** Um pool é propriedade de
  um mapa. O `fish_catalog.json` guarda somente o que é verdade sobre uma espécie independentemente
  de onde ela seja pescada, então uma espécie que apareça em dois mapas nunca é duplicada.
- **`balance_status: "PROVISÓRIO"`** em todo arquivo significa que os números são um balanceamento
  inicial com hierarquia clara, a ser ajustado por simulação e pelo painel (seção 46 do GDD). Para ver
  os números atuais medidos, rode `./ops/scripts/simular.sh` (relatório em
  `docs/relatorios/SIMULACAO_BALANCEAMENTO.md`). A
  *estrutura* não é provisória.
- **Os atributos base valem para nível 1, tamanho no percentil 0,50, antes dos modificadores de
  raridade, tamanho e nível.** O servidor aplica esses modificadores usando o `progression.json`;
  ele nunca grava atributos derivados de forma redundante.
- **Tamanho Perfeição:** acima do Excepcional, com `draw_weight` 0,01 (1 em cada 100 dos dois) e
  `stat_multiplier` 1,05 (+5% em todos os atributos). Qualquer categoria de tamanho pode ter um
  `stat_multiplier` (1 quando não tem) e `special: true` (selo, celebração e proteção na venda).
- **Tamanho por raridade:** cada raridade pode ter `size_weight_multipliers` (em
  `progression.json → rarity.tiers`), que multiplica o `draw_weight` de cada categoria de tamanho
  para os peixes dessa raridade. O que não estiver listado fica 1. Hoje o Raro tem Grande × 0,85 e
  Excepcional × 0,7 (GDD_ADENDO A-083). Campo opcional: arquivos sem ele continuam valendo.
- **Mordida na pesca por raridade (M24-T22, A-159, TD-043):** `rarity.tiers[].fishing_bite_multiplier` (0,233333 no
  Raro, Épico, Lendário e Mítico desde o M24-T27, A-160; era 0,35) multiplica a chance de um peixe daquela raridade
  morder **na pesca** (online e offline), depois dos pesos do mapa e do bônus de raridade da vara; os Comuns completam o
  resto. Com o ciclo de 10 s (`fishing.online_cycle_seconds` 10 e `offline_cycle_seconds` 20), cada raridade continua
  com os mesmos peixes por hora do ciclo de 15 s (0,35 × 10/15) e ~70% dos do ciclo de 30 s. Para manter os raros por
  hora ao mudar o ciclo, mude este número na mesma proporção. Expedições e os vendedores do Mercado usam os pesos do mapa
  sem ele. Ausente = 1; não vale para o Comum.
- **Ciclo de pesca (M24-T27, A-160):** `progression.json → fishing.online_cycle_seconds` (10) e
  `offline_cycle_seconds` (20); com o VIP ativo, a pesca offline usa `economy.json → vip.offline_cycle_seconds` (15; 0 =
  o VIP não muda o ciclo). A Caixa de Pesca (`economy.json → fishing_box.capacity`, 8.640) guarda 1 dia de pesca online;
  as iscas (`equipment.json → baits[].charges`, 999 desde o M24-T30) duram 2,8 h online.
- Todo arquivo carrega `config_schema_version`. Aumente-o quando o *formato* do arquivo mudar, não
  quando um número mudar.

## Idioma

Os textos legíveis por humanos — descrições, notas, e os nomes exibidos de espécies, varas,
expedições, raridades e categorias de tamanho — estão em PT-BR, porque o jogador vai lê-los.

Os **identificadores** (`lambari`, `rod_01`, `map_02`, `common`, `exceptional`) permanecem como
chaves técnicas em texto simples, sem acento. Eles aparecem em URLs, em colunas de banco e em
código; mudá-los ao traduzir quebraria dados já gravados. Veja `docs/DECISOES.md`, TD-014.

## Carregamento

A partir do Milestone 1, o servidor carrega estes arquivos em um registro `GameConfigVersion`, para
que qualquer resultado possa ser rastreado até a versão de balanceamento que o produziu. Até lá, o
servidor os serve somente para leitura em `GET /api/dev/config`.

## Calibragem provisória registrada

- *Histórico, substituído pelo A-153 e pelo A-162 (números de hoje logo abaixo, na curva do Pescador):* o XP do
  Pescador do nível 1 ao 10 somava **2000 XP**, mirando cerca de 240 tentativas no Mapa 1
  (aproximadamente 2 horas de pesca online). Hoje: Nv.1 → 10 = **990 XP**; até o Nv.100 = **309.220 XP**; até o
  Nv.1000 = **229.596.770 XP** (curva versão 3). Desde a V0.2 só metade delas vira peixe no começo
  (Chance de Sucesso da Captura), e por isso o XP, o valor e o XP como alimento das espécies foram
  dobrados (30/09/2026, `docs/GDD_ADENDO.md` A-092).
- Mapas 3 e 4 (V0.2, `docs/PROGRESSAO_MAPAS_3_4.md`): venda, XP e XP como alimento das espécies
  multiplicados por 1,75 (Pantanal Dourado) e 1,55 (Estuário das Marés) em relação ao documento, para o
  Nível 20→30 e o 30→40 ficarem perto de 3,8 h e 4,0 h online com a Chance de Sucesso (A-093).
- Chance de Sucesso da Captura: Comum 50%, Raro 38%, Épico 24%, entre 5% e 95%.
  *Histórico (30/09/2026), substituído pelo A-156, A-157, A-161, A-163 e A-168:* barcos de +3% a +15%, iscas de +5% a
  +15% por 100 tentativas, Vara 1 de +2% a +12%. Hoje: 5 barcos de **+2% a +10%** no Nv.1 (3× no Nv.100), três iscas de
  **+5%, +10% e +15%** com **999 tentativas** por compra, e varas com níveis até o Nv.100 (Vara 1: +1,5% no Nv.1, +24%
  no Nv.100).
- *Histórico, substituído pelo A-109, A-153 e A-162:* o XP do Pescador do nível 1 ao 100 somava **711.000 XP**,
  contra uma meta de cerca de 90 dias. Hoje são **309.220 XP** até o Nv.100 (~0,5 dia com o jogo aberto) e
  **229.596.770 XP** até o Nv.1000 (~60 dias).
- O XP do peixe do nível 1 ao 10 somava **3.885 XP** (A-132). Desde 10/10/2026 (M24-T13, A-156) o peixe vai até o
  **Nv.100**: do Nv.1 ao 100 são **36.160 XP** (~930 Comuns de 39 XP); o Nv.41, que tem a força do Nv.10 de antes
  (+36%), pede **2.465 XP** (~63 Comuns).
- **Mais peixes, menos raros (11/10/2026, M24-T22, A-159):** ciclo de pesca na metade (online 30 → 15 s, offline 60 →
  30 s, limite offline de 24 h igual), `fishing_bite_multiplier` 0,35 nas raridades acima de Comum, Caixa de Pesca de
  1.500 para 3.000 peixes, iscas de 60 para 120 tentativas pelo mesmo preço, chance de fragmento ~metade e o Passe
  regerado. O Nv.10 do Pescador chega em ~30 min online (era ~1 h); o Nv.1000 com a Tripulação e as Melhorias, em
  ~57 / ~102 / ~119 dias (jogo aberto / 4 vezes / 1 vez por dia; eram ~50 / ~92 / ~106), porque no fim do jogo os raros
  valiam mais que o dobro de Comuns (OD-063).
- **Pesca a cada 10 s (12/10/2026, M24-T27, A-160):** online 15 → 10 s, offline 30 → 20 s, 15 s offline com o VIP
  (`vip.offline_cycle_seconds`); `fishing_bite_multiplier` 0,35 → 0,233333 (raros por hora iguais, chance por peixe
  2/3); Caixa de Pesca 3.000 → 8.640 (1 dia de pesca online); iscas 120 → 180 tentativas pelo mesmo preço; chance de
  fragmento ÷1,5 (0,43% a 0,67%) e o Passe regerado. Nv.10 em ~20 min online; Nv.1000 com a Tripulação e as Melhorias em
  ~50 / ~91 / ~106 dias (eram ~57 / ~102 / ~119).
- **Três iscas (12/10/2026, M24-T30, A-161, TD-046):** `equipment.json → baits` tem exatamente Terra Viva (+5%),
  Maré Viva (+10%) e Ouro de Maré (+15%), 999 tentativas por compra, 23.000 / 100.000 / 200.000 Moedas e 13 / 33 / 55
  Conchas: por tentativa, ~10% a 20% mais caro que as iscas de antes pelo mesmo bônus. `bait_migration` guarda a versão
  do modelo de iscas (`bait_model_version`, 2) e as iscas de antes (`legacy_baits`), usadas só para converter um save
  antigo pelo valor em Moedas. Retorno de cada isca por estágio: `python3 tools/Progressao/simular_iscas.py`
  (`--precos A,B,C` testa outro custo por tentativa sem mexer no config).
- **Curva do Pescador: Nv.1000 em ~60 dias (12/10/2026, M24-T31, A-162, TD-047):** `progression.json → fisher`
  na curva versão 3 (`xp_curve_version`): os níveis 1 a 99 iguais aos de antes e o resto regerado por
  `python3 tools/Progressao/calibrar_xp.py --gravar` (`--manter-ate 100`, `--metas 250:3.67d,500:14.6d,750:33.3d,1000:60d`),
  medido com a Tripulação e as Melhorias, jogo aberto, sem Passe e sem VIP. Nv.1000 em ~60 / ~108 / ~124 dias (jogo
  aberto / 4 vezes / 1 vez por dia; eram ~50 / ~91 / ~105); do Nv.1 ao Nv.1000, 229.596.770 XP. Trocar a curva de novo
  pede subir `xp_curve_version` (os saves são convertidos pelo XP total, sem baixar nível) e regerar o `pass.json`.
- **Todo nível custa Moedas e Conchas (M24-T30):** `upgrade_cost.shells_first` das varas e dos barcos precisa ser pelo
  menos 1; o jogo recusa um config com 0.
- A Vara 1 **não é mais vendida** (M24-T32, A-163): monta com **15 fragmentos** que caem no Lago Sereno (3,7% por peixe
  puxado) e no Passe; os 2.500 Moedas de antes ficaram só como valor de referência no Mercado
  (`acquisition.market_reference_coins`). Desde o M24-T13 ela vai até o **Nv.100**: o 1º nível custa **500**
  Moedas e 1 Concha (antes 1.500 e 2), do Nv.1 ao 30 são ~156 mil Moedas e 44 Conchas, e do 1 ao 100 ~1,5 bi Moedas
  e 588 Conchas.

## Níveis até o Nv.100 (varas, barcos e peixes)

Desde 10/10/2026 (M24-T13, A-156, TD-041). Os números são gerados por `tools/Progressao/niveis_100.py gerar` (os
"botões" ficam no começo do script) e o ritmo é simulado por `tools/Progressao/niveis_100.py simular --gravar`
(resultado em `docs/propostas/niveis_100.json`).

- **Bônus.** Cada vara com nível tem `bonuses_at_level_1` (o bônus de antes no Nv.1) e `bonuses_at_max_level` (3× o
  bônus do Nv.10 de antes). Cada barco com nível tem `catch_success_bonus` (Nv.1, o de antes) e
  `catch_success_bonus_at_max_level` (3×). Entre os dois, o bônus no nível N é
  `Nv.1 + (máx − Nv.1) × ((N − 1) ÷ (nível máximo − 1)) ^ expoente`, com o expoente em
  `rods.json → upgrade_rules.bonus_curve_exponent` e `equipment.json → boat_levels.bonus_curve_exponent` (1,2: cada
  nível soma um pouco mais que o anterior, começando com passos pequenos). O peixe usa a mesma curva com
  `progression.json → fish_level.stat_bonus_at_max_level_percent` (108%) e `stat_bonus_curve_exponent`.
- **Custo.** `upgrade_cost` em cada vara e barco: o nível N → N + 1 custa `coins_first × coins_growth ^ (N − 1)`
  Moedas e `shells_first × shells_growth ^ (N − 1)` Conchas, arredondados (abaixo de 1.000 para o inteiro, no mínimo
  1; a partir de 1.000, para 3 algarismos: 12.345 vira 12.300).
- **Nível máximo.** `rods.json → upgrade_rules.internal_levels.max`, `equipment.json → boat_levels.max_level` e
  `progression.json → fish_level.max_level` (os três em 100). O barco inicial não tem nível (`has_levels: false`).
- **Teto da chance.** A Chance de Sucesso da Captura continua entre `fishing.catch_success_min` e
  `catch_success_max` (5% e 95%), mesmo com a vara e o barco no Nv.100 e a melhor isca.
- **Migração.** `legacy_levels_v1` (em cada vara e em `fish_level`) diz para qual nível novo vai quem estava no nível
  1 a 10 da curva antiga (`level_curve_version` 1): o menor nível cujo bônus é maior ou igual ao de antes em todos os
  eixos. Se a curva mudar de novo, gere um mapa novo e aumente `level_curve_version`.
- **Outros números ajustados para a mesma força de antes:** adversários da Arena (`arena_bots.json →
  top_fish_level` 10 → 41), peixes à venda no Mercado (`market_bots.json → max_fish_level` 6 → 26, prêmio por nível
  0,15 → 0,0337), varas à venda no Mercado (`max_rod_level` 41, novo) e o aviso de "peixe valioso" ao alimentar
  (`progression.json → feeding.valuable_feed_rules.min_level` 2 → 8).

## Barcos por fragmentos e Passe do Pescador

Desde 11/10/2026 (M24-T16 e M24-T17, A-157 e A-158, TD-042). Números gerados por
`tools/Progressao/passe_fragmentos.py gerar` (os "botões" ficam no começo do script); o ritmo é simulado por
`tools/Progressao/passe_fragmentos.py simular --gravar` (resultado em `docs/propostas/passe_fragmentos.json`).

- **Barcos não são vendidos.** São **5 barcos** (mais o inicial) desde o M24-T42 (A-168, TD-051); cada um cai em **dois
  mapas**: `map_id` (um dos Mapas 1 a 5) com `fragment_drop_chance`, e `extra_fragment_maps` (lista de `{map_id,
  fragment_drop_chance}`, hoje um dos Mapas 6 a 10). `fragments_required` diz quantos montam o barco (20 no Remo Valente
  até 55 no Horizonte Dourado). Chance de um peixe puxado dar 1 fragmento: **0,43% a 0,52%** nos Mapas 1 a 5 e **0,53% a
  0,67%** nos Mapas 6 a 10 (ciclo de 10 s, M24-T27; eram 0,64% a 1,03% com o de 15 s e 1,3% a 2,4% com o de 30 s). O
  validador confere que cada mapa dá um barco só, contando os dois campos. O script grava com 4 casas e mexe só no
  `map_id` principal; a chance dos mapas extras é a que o mapa já tinha. O barco inicial não tem `map_id`.
  `boat_fragments.drop_on_offline_fishing` diz se a pesca offline também dá fragmentos (sim).
- **`boat_migration`** (`boat_model_version` 2 e `removed_boats`: id, `fragments_go_to`, `fragments_required`,
  `upgrade_cost` de cada barco 6 a 10 que saiu) é usado só para converter, uma vez, um save que ainda tinha os 10 barcos
  (save v20, `BoatModelMigration`).
- **Como a chance foi calibrada.** Com os 10 barcos de antes, para quem abre o jogo 4 vezes por dia e pesca no mapa do
  barco, montar levava ~2 dias no Mapa 1 e ~10 no Mapa 10 (`DIAS_ALVO_PRIMEIRO` e `DIAS_ALVO_ULTIMO` no script).
- **Passe.** `premium_price_dollars` (100) e `milestones`: `level` e as recompensas `free` e `premium`, cada uma com
  `coins`, `shells`, `dollars`, `boat_id` e `fragments` (0 = não dá). Os níveis precisam estar em ordem crescente, até
  o nível máximo do Pescador; `boat_id` precisa existir e não pode ser o barco inicial. Os Dólares de nível do A-111
  (`fisher.dollars_per_levels`, que saiu do `progression.json`) estão em `free.dollars` do Nv.10 ao Nv.100.
- `pass_id` identifica o passe no save; temporadas ainda não existem (OD-059).
- Desde 12/10/2026 (M24-T32/T33) a recompensa também pode ter `rod_id` e `rod_fragments` (fragmentos de uma vara, que
  precisa existir e não ser a Vara Inicial) e `clothing_fragments` (Fragmentos de Roupa).

## Varas e roupas por fragmentos

Desde 12/10/2026 (M24-T32 e M24-T33, A-163 e A-164, TD-048). Números gerados por
`tools/Progressao/fragmentos_equipamento.py gerar` (os "botões" ficam no começo do script); o ritmo é simulado por
`tools/Progressao/fragmentos_equipamento.py simular --gravar` (resultado em `docs/propostas/fragmentos_equipamento.json`).
Depois de mudar varas ou roupas, rode também `passe_fragmentos.py gerar` (o Passe dá fragmentos delas).

- **Varas não são vendidas.** Cada vara menos a inicial tem `acquisition.method` `"fragments"`, `fragment_map_id` (o mapa
  cujos peixes dão os fragmentos dela; um mapa dá no máximo uma vara), `fragments_required` (15 a 60) e
  `fragment_drop_chance` (1,63% a 7% por peixe puxado). `acquisition.market_reference_coins` é o valor da vara no Mercado e
  na revenda (o preço que ela tinha na Loja). `fragment_rules.drop_on_offline_fishing` diz se a pesca offline também dá
  fragmentos (sim). A Vara Inicial é `"free_claim_in_shop"` e não tem mapa.
- **A progressão não pode travar.** O mapa de uma vara precisa pedir uma categoria **menor** que a dela
  (`minimum_rod_tier` no `maps.json`), e todo mapa precisa pedir uma categoria que alguma vara alcançável tenha. O jogo
  recusa o config que travaria, com a mensagem dizendo qual vara ou mapa. O script escolhe sempre o último mapa que a vara
  anterior alcança (Vara 1 no Mapa 1, 2 no 3, 3 no 5, 4 no 7, 5 no 9).
- **Como a chance foi calibrada.** Quem abre o jogo 4 vezes por dia monta cada vara no mesmo dia em que comprava antes
  (`PRECOS_ANTIGOS` no script guarda os preços de antes, só para essa conta).
- **Roupas** (`equipment.json → clothing`): `fragment_drop_chance` (0,17%: um tipo só de fragmento, em todos os mapas),
  `drop_on_offline_fishing`, `fragments_to_assemble` (custo da 1ª, 2ª … peça montada, qualquer peça: 15, 25, 40, 60, 80),
  `max_level` (100) e `bonus_curve_exponent` (a curva das varas), `use_xp_per_fish` (pontos de uso por peixe puxado) e
  `level_xp` (`first` × `growth` ^ (N − 1) pontos do nível N para o N + 1, arredondado como os preços), e `pieces` (id,
  nome, descrição, `bonus_axis` — `catch_success`, `rarity_efficiency`, `size_quality`, `shell_yield` ou `fisher_xp` — e o
  bônus no Nv.1 e no Nv.100). O jogo recusa um eixo desconhecido, uma chance fora de 0 a 1 e bônus que diminuem.
