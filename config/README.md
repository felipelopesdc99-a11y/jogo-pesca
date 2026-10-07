# /config — balanceamento orientado a dados

Todo valor em que o jogo se apoia mora aqui, nunca dentro do código do jogo (regras 2 e 43 do GDD).

| Arquivo | Responsável por |
|---|---|
| `fish_catalog.json` | Dados próprios da espécie: raridade, faixa de tamanho, atributos base, valor de venda, XP de alimento, XP do Pescador |
| `maps.json` | Mapas, pools de peixes por mapa com pesos de captura, nível de desbloqueio, vara mínima, regras de viagem |
| `progression.json` | Curva de XP do Pescador, curva de XP do peixe, distribuição de tamanho, regras de modificador de raridade/tamanho/nível, regras de alimentação, intervalos de pesca |
| `rods.json` | Definições das varas, bônus por nível interno, custos de compra e de melhoria (Moedas e Conchas), regras de revenda |
| `economy.json` | Moedas, fórmula de venda ao NPC, obtenção de Conchas, peixes que pedem confirmação na venda da Caixa, valores de Mercado e Leilão, limite do Aquário, VIP (preço em Dólares, dias e bônus de XP offline) |
| `arena.json` | Energia, seleção de oponentes, Honra, constantes de combate, formação, regras de Cardume, métrica de Força, Loja da Arena |
| `expeditions.json` | Durações, Força Recomendada, curvas de eficiência, recompensas |
| `arena_bots.json` | Só no jogo local: como os 200 adversários simulados da Arena são montados e com que frequência atacam você |
| `market_bots.json` | Só no jogo local: vendedores e compradores simulados do Mercado (quantos anúncios, preços de referência, chance de vender) |
| `equipment.json` | Barcos e iscas: bônus na Chance de Sucesso da Captura, custo em Moedas e Conchas, nível, tentativas de cada isca |

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
| Chance de puxar o peixe (por raridade, mínimo e máximo), barcos e iscas, e o simulador | Sucesso da pesca | `progression.json`, `equipment.json` |
| Bônus de puxar de cada vara por nível | Varas (coluna "Puxar") | `rods.json` |
| Tempo de pesca | Pesca | `progression.json` |
| Níveis do Pescador e do peixe | XP do Pescador | `progression.json` |
| Varas (bônus e custo) | Varas | `rods.json` |
| Preço mínimo, multiplicador geral do preço de venda dos peixes, Conchas, Aquário, Mercado | Economia | `economy.json` |
| Preço, duração e bônus do VIP | Economia → `vip` | `economy.json` |
| Arena, Cardume, Expedições | Outros arquivos | `arena.json`, `expeditions.json`… |

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

- O XP do Pescador do nível 1 ao 10 soma **2000 XP**, mirando cerca de 240 tentativas no Mapa 1
  (aproximadamente 2 horas de pesca online). Desde a V0.2 só metade delas vira peixe no começo
  (Chance de Sucesso da Captura), e por isso o XP, o valor e o XP como alimento das espécies foram
  dobrados (30/09/2026, `docs/GDD_ADENDO.md` A-092).
- Mapas 3 e 4 (V0.2, `docs/PROGRESSAO_MAPAS_3_4.md`): venda, XP e XP como alimento das espécies
  multiplicados por 1,75 (Pantanal Dourado) e 1,55 (Estuário das Marés) em relação ao documento, para o
  Nível 20→30 e o 30→40 ficarem perto de 3,8 h e 4,0 h online com a Chance de Sucesso (A-093).
- Chance de Sucesso da Captura: Comum 50%, Raro 38%, Épico 24%, entre 5% e 95%.
  Barcos de +3% a +15%, iscas de +5% a +15% por 100 tentativas, Vara 1 de +2% a +12%.
- O XP do Pescador do nível 1 ao 100 soma **711.000 XP**, contra uma meta de cerca de 90 dias. É o
  valor com maior chance de precisar de recalibragem quando houver dados reais de jogo.
- O XP do peixe do nível 1 ao 10 soma **995 XP**.
- A Vara 1 custa **2.500** Moedas para comprar e **138.050** Moedas para ir do nível interno 1 ao 10.
