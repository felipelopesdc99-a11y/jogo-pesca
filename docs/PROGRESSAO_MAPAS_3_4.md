# Fishing Idle — Progressão e Conteúdo dos Mapas 3 e 4

> **Situação no repositório (30/09/2026):** documento do proprietário, guardado como está. Implementado
> no Milestone 16 (`docs/roadmap.json`), com um ajuste: venda, XP e XP como alimento das espécies novas
> foram multiplicados por 1,75 (mapa 3) e 1,55 (mapa 4) por causa da Chance de Sucesso da Captura, que
> chegou depois deste documento. Detalhes em `docs/GDD_ADENDO.md` (A-093).

**Documento de expansão para implementação no jogo existente**  
**Escopo:** somente Mapas 3 e 4, seus peixes, raridade Épico, progressão econômica/XP e Vara 2.  
**Status de balanceamento:** PROVISÓRIO — todos os valores devem permanecer configuráveis e ser ajustáveis depois de playtest.  
**Importante:** este documento NÃO manda recriar sistemas já existentes.

---

# 1. Objetivo desta expansão

O jogo já existe e já possui os sistemas centrais funcionando. Esta expansão deve apenas estender a progressão atual para cobrir aproximadamente os níveis de Pescador **20 a 40**.

A intenção é que, ao trocar de mapa, o jogador perceba imediatamente:

- novas espécies;
- maior ganho de XP de Pescador;
- maior ganho de Moedas;
- peixes progressivamente mais fortes para Aquário/Cardume/Arena;
- mais presença de Raros;
- introdução da raridade **Épico**;
- uma nova Vara no nível 30;
- uma identidade visual clara para cada novo mapa.

Não alterar a filosofia geral do jogo. Não criar sistemas extras para preencher conteúdo.

---

# 2. Fontes de verdade e regras herdadas

Antes de implementar, Claude deve inspecionar o projeto atual e reutilizar as estruturas existentes.

Continuam valendo as regras dos documentos e configs atuais, salvo quando este documento explicitamente amplia algo:

- `docs/GDD_V0_1.md`
- `docs/GDD_ADENDO.md`
- `docs/DECISOES.md`
- `docs/ART_BIBLE_V0_1.md`
- `docs/BASE_TECNICA.md`
- `config/progression.json`
- `config/fish_catalog.json`
- `config/maps.json`
- `config/rods.json`
- `config/economy.json`

## Não recriar

Não recriar nem substituir:

- pesca online/offline;
- Fishing Box;
- Aquário;
- Enciclopédia;
- Cardume;
- Arena;
- Mercado;
- Leilão;
- Expedições;
- sistema de tamanho;
- sistema de nível dos peixes;
- saves existentes;
- Vara Inicial ou Vara 1 existentes.

A regra é: **adicionar conteúdo usando os sistemas existentes**.

---

# 3. Progressão estrutural já decidida

| Mapa | Nível de desbloqueio | Vara mínima |
|---|---:|---:|
| 1 — Lago Sereno | 1 | Vara Inicial |
| 2 — Rio Selvagem | 10 | Vara 1 |
| **3 — Pantanal Dourado** | **20** | **Vara 1** |
| **4 — Estuário das Marés** | **30** | **Vara 2** |
| 5 — futuro | 40 | Vara 2 |

A viagem continua exatamente como já existe:

- manual;
- 30 s;
- pesca pausada durante a viagem;
- sem combustível;
- sem taxa;
- sem viagem automática ao subir de nível.

## Comportamento de desbloqueio após atualização

- jogador já em Nv.20+ deve enxergar o Mapa 3 desbloqueado imediatamente;
- jogador já em Nv.30+ satisfaz o requisito de nível do Mapa 4;
- porém o Mapa 4 continua exigindo **Vara 2 equipada** para pescar;
- não teleportar automaticamente o jogador para nenhum mapa novo.

---

# 4. Nova raridade — Épico

A partir desta expansão, a hierarquia ativa passa a ser:

**Comum → Raro → Épico**

`exceptional` continua sendo uma categoria de **tamanho**, não raridade.

Não adicionar Incomum, Lendário ou Mítico nesta expansão.

## 4.1 Configuração inicial do Épico

Adicionar a `config/progression.json`:

| Campo | Épico |
|---|---:|
| `id` | `epic` |
| `display_name` | `Épico` |
| `stat_multiplier` | **1.30** |
| `fisher_xp_multiplier` | **5.00** |
| `feed_xp_multiplier` | **3.50** |
| `sale_value_multiplier` | **4.00** |

Motivo do `stat_multiplier` ser controlado: Épico deve ser claramente melhor que Raro sem criar um salto exagerado de poder no PvP já nos níveis 20–40.

## 4.2 Visual

Usar um acento roxo para Épico. Valor inicial recomendado:

`#A855F7`

Não confundir com as cores de tamanho. Um peixe pode ser simultaneamente:

**Épico + Excepcional**.

Nesse caso, raridade e tamanho precisam continuar visualmente separados.

---

# 5. Mudança necessária na Vara 1

O Mapa 3 agora possui um peixe Épico. Portanto, a Vara 1 precisa ser capaz de capturar Épico.

Alterar a elegibilidade da Vara 1 de:

```text
common, rare
```

para:

```text
common, rare, epic
```

Isso **não adiciona Épicos ao Rio Selvagem**, porque o Mapa 2 continuará sem espécies Épicas no pool.

A Vara Inicial continua apenas `common`.

Esta é uma decisão nova da expansão e substitui a limitação antiga da V0.1 apenas no que diz respeito à Vara 1 poder capturar Épico quando o mapa possuir Épico disponível.

---

# 6. Meta de progressão econômica e de XP

A curva de XP do Pescador já existente **não deve ser substituída**.

Na configuração atual:

- Nv.20 → Nv.30 exige **21.930 XP**;
- Nv.30 → Nv.40 exige **36.570 XP**.

O aumento de velocidade deve vir dos peixes novos entregarem mais XP, não de uma nova curva de níveis.

## Referência atual — Mapa 2

Com o balanceamento atual do projeto, o Rio Selvagem fica aproximadamente em:

- **33,73 XP por captura** em média, já considerando o multiplicador médio de tamanho;
- **226,3 Moedas por captura** em média antes de decisões do jogador de guardar/alimentar/vender no Mercado.

## Alvo do Mapa 3

- XP médio: aproximadamente **47,9 por captura**;
- venda média: aproximadamente **464 Moedas por captura**;
- ganho de XP vs. Mapa 2: aproximadamente **+42%**;
- ganho de Moedas vs. Mapa 2: aproximadamente **+105%**.

Resultado esperado em pesca online contínua:

- ~120 capturas/h;
- ~5.749 XP/h;
- ~55.682 Moedas/h se tudo fosse vendido ao NPC;
- Nv.20 → 30 em aproximadamente **3,8 h online**.

## Alvo do Mapa 4

- XP médio: aproximadamente **75,7 por captura**;
- venda média: aproximadamente **1.008 Moedas por captura**;
- ganho de XP vs. Mapa 3: aproximadamente **+58%**;
- ganho de Moedas vs. Mapa 3: aproximadamente **+117%**.

Resultado esperado em pesca online contínua:

- ~120 capturas/h;
- ~9.089 XP/h;
- ~120.936 Moedas/h se tudo fosse vendido ao NPC;
- Nv.30 → 40 em aproximadamente **4,0 h online**.

### Observação importante

Esses números representam **alvos de simulação**, não garantias de recompensa real do jogador. O jogador pode guardar peixes, alimentar outros peixes, negociar no Mercado etc.

Em pesca offline, a quantidade de ciclos continua sendo metade da online (1 captura/min), seguindo a regra atual.

---

# 7. Mapa 3 — Pantanal Dourado

## 7.1 Identidade

**ID:** `map_03`  
**Nome:** `Pantanal Dourado`  
**Unlock:** Pescador Nv.20  
**Vara mínima:** Tier 1 / Vara 1  
**Raridades disponíveis:** `common`, `rare`, `epic`

## Papel na progressão

O Mapa 3 deve ser a primeira sensação clara de que o jogador deixou a fase inicial do jogo.

Ele introduz:

- 10 espécies novas;
- presença real de peixes Raros;
- o primeiro Épico;
- maior valor econômico;
- maior XP;
- peixes capazes de começar a substituir os melhores peixes dos Mapas 1–2 no Cardume.

A Vara 1 continua útil do Nv.20 ao 29.

---

# 8. Direção visual resumida — Pantanal Dourado

A cena deve ser coerente com o estilo Lago Dourado já implementado, sem virar um novo estilo artístico.

Características:

- grande área alagada e aberta;
- água calma com reflexos dourados;
- ilhas baixas de vegetação;
- gramíneas e plantas aquáticas;
- árvores espaçadas nas margens;
- céu amplo;
- luz dourada de manhã cedo ou fim de tarde;
- neblina muito leve no horizonte;
- sensação quente, viva e relaxante.

Vida ambiente possível usando o sistema já existente:

- tuiuiú/garça;
- capivara;
- jacaré;
- arara;
- martim-pescador;
- sombras de peixe;
- libélulas.

Nada disso possui efeito de gameplay.

---

# 9. Peixes do Mapa 3

Distribuição:

- **6 Comuns**
- **3 Raros**
- **1 Épico**

Pool total: **1000**.

## 9.1 Dados de espécie

Os valores abaixo devem entrar no catálogo como valores-base. Raridade, tamanho e nível continuam sendo aplicados pelo sistema determinístico já existente.

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `mandi_amarelo` | Mandi-amarelo | Comum | 20–45 | 150 | 28 | 13 | 102 | 230 | 90 | 34 | 220 |
| `pacu_peva` | Pacu-peva | Comum | 20–50 | 190 | 25 | 23 | 96 | 280 | 105 | 36 | 190 |
| `jundia` | Jundiá | Comum | 25–100 | 205 | 29 | 23 | 94 | 350 | 120 | 40 | 170 |
| `jurupensem` | Jurupensém | Comum | 25–70 | 185 | 33 | 18 | 100 | 420 | 135 | 43 | 150 |
| `mucum` | Muçum | Comum | 30–100 | 215 | 31 | 24 | 91 | 480 | 150 | 46 | 120 |
| `piavucu` | Piavuçu | Comum | 25–70 | 225 | 34 | 25 | 97 | 560 | 165 | 50 | 102 |
| `piraputanga` | Piraputanga | **Raro** | 30–60 | 195 | 39 | 20 | 108 | 900 | 220 | 60 | 15 |
| `jurupoca` | Jurupoca | **Raro** | 30–80 | 255 | 36 | 29 | 95 | 1.100 | 250 | 65 | 15 |
| `armado` | Armado | **Raro** | 30–100 | 290 | 33 | 36 | 88 | 1.300 | 280 | 70 | 15 |
| `barbado` | Barbado | **Épico** | 50–130 | 315 | 45 | 33 | 98 | 2.000 | 350 | 90 | 3 |

## 9.2 Chances-base do Mapa 3

Antes dos bônus da vara:

- Comum: **95,2%**
- Raro: **4,5%**
- Épico: **0,3%**

Em pesca online de 30 s:

- algum Raro: média de ~**11 min**;
- cada Raro específico: média de ~**33 min**;
- Barbado Épico: média de ~**2 h 47 min**.

Com a Vara 1 evoluída, esses tempos melhoram modestamente por `rarity_efficiency`.

Não criar pity timer nesta expansão.

## 9.3 Recompensas de referência

Aplicando apenas multiplicador de raridade, antes da influência de tamanho:

| Espécie | Venda de referência | Fisher XP de referência |
|---|---:|---:|
| Piraputanga | 1.800 | 150 |
| Jurupoca | 2.200 | 162,5 |
| Armado | 2.600 | 175 |
| Barbado | **8.000** | **450** |

O Barbado deve ser um evento perceptível sem tornar as capturas normais irrelevantes.

---

# 10. Mapa 4 — Estuário das Marés

## 10.1 Identidade

**ID:** `map_04`  
**Nome:** `Estuário das Marés`  
**Unlock:** Pescador Nv.30  
**Vara mínima:** Tier 2 / Vara 2  
**Raridades disponíveis:** `common`, `rare`, `epic`

## Papel na progressão

O Mapa 4 é o primeiro salto claro de tier de equipamento.

Ele introduz:

- Vara 2;
- 10 espécies novas;
- 3 Raros;
- 2 Épicos;
- ambiente de água salobra/costeira;
- nova faixa de valor econômico;
- nova faixa de poder para Cardume/Arena;
- preparação temática para futuros mapas marinhos.

---

# 11. Direção visual resumida — Estuário das Marés

Características:

- canal de maré largo;
- raízes de mangue visíveis nas margens;
- bancos de areia/lama molhada;
- água verde-azulada;
- abertura para o mar no fundo;
- ilhotas baixas de mangue;
- céu mais aberto que o Pantanal;
- luz quente de fim de tarde;
- névoa salgada muito leve no horizonte.

Vida ambiente possível:

- garças e aves costeiras;
- martim-pescador;
- caranguejo em raiz de mangue;
- sombras de peixe;
- aves fazendo rasantes ocasionais.

Sem impacto de gameplay.

---

# 12. Peixes do Mapa 4

Distribuição:

- **5 Comuns**
- **3 Raros**
- **2 Épicos**

Pool total: **1000**.

## 12.1 Dados de espécie

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `parati` | Parati | Comum | 15–40 | 175 | 34 | 16 | 107 | 550 | 180 | 52 | 250 |
| `tainha` | Tainha | Comum | 25–80 | 215 | 31 | 23 | 102 | 650 | 200 | 56 | 220 |
| `carapeba` | Carapeba | Comum | 15–50 | 245 | 30 | 31 | 95 | 750 | 220 | 60 | 190 |
| `corvina` | Corvina | Comum | 30–120 | 255 | 40 | 25 | 100 | 900 | 250 | 66 | 150 |
| `bagre_marinho` | Bagre-marinho | Comum | 30–120 | 290 | 38 | 33 | 92 | 1.050 | 280 | 72 | 124 |
| `robalo_peva` | Robalo-peva | **Raro** | 25–80 | 265 | 48 | 24 | 110 | 1.700 | 380 | 90 | 20 |
| `pescada_amarela` | Pescada-amarela | **Raro** | 30–130 | 280 | 46 | 27 | 105 | 1.900 | 420 | 96 | 20 |
| `xareu` | Xaréu | **Raro** | 40–120 | 285 | 51 | 26 | 109 | 2.200 | 450 | 102 | 20 |
| `camurupim` | Camurupim | **Épico** | 80–250 | 370 | 59 | 34 | 110 | 3.500 | 650 | 135 | 3 |
| `mero` | Mero | **Épico** | 100–250 | 460 | 51 | 52 | 86 | 3.800 | 700 | 140 | 3 |

## 12.2 Chances-base do Mapa 4

Antes dos bônus da Vara 2:

- Comum: **93,4%**
- Raro: **6,0%**
- Épico: **0,6%**

Em pesca online de 30 s:

- algum Raro: média de ~**8 min 20 s**;
- cada Raro específico: média de ~**25 min**;
- algum Épico: média de ~**1 h 23 min**;
- cada Épico específico: média de ~**2 h 47 min**.

A Vara 2 melhora modestamente essas probabilidades por `rarity_efficiency`.

Não criar pity timer.

## 12.3 Recompensas de referência

Aplicando apenas multiplicador de raridade, antes de tamanho:

| Espécie | Venda de referência | Fisher XP de referência |
|---|---:|---:|
| Robalo-peva | 3.400 | 225 |
| Pescada-amarela | 3.800 | 240 |
| Xaréu | 4.400 | 255 |
| Camurupim | **14.000** | **675** |
| Mero | **15.200** | **700** |

---

# 13. Vara 2

## 13.1 Definição

**ID:** `rod_02`  
**Nome provisório:** `Vara 2`  
**Tier:** 2  
**Desbloqueio:** Pescador Nv.30  
**Custo de compra:** **90.000 Moedas**  
**Níveis internos:** 1–10  
**Raridades permitidas:** `common`, `rare`, `epic`  
**Gera Conchas:** sim  
**Negociável:** sim

O custo de 90.000 foi escolhido para que um jogador que realmente pescou no Mapa 3 normalmente tenha condições de comprá-la ao chegar ao Nv.30, sem torná-la automática ou irrelevante.

## 13.2 Bônus por nível

Os valores são bônus totais do nível, seguindo o modelo atual da Vara 1.

### `rarity_efficiency`

```json
[0.24, 0.26, 0.28, 0.31, 0.34, 0.37, 0.40, 0.43, 0.47, 0.50]
```

### `size_quality`

```json
[0.23, 0.25, 0.27, 0.29, 0.31, 0.33, 0.35, 0.37, 0.40, 0.42]
```

### `shell_yield`

```json
[0.55, 0.60, 0.66, 0.72, 0.78, 0.84, 0.91, 0.98, 1.04, 1.10]
```

A Vara 2 Nv.1 deve começar levemente acima da Vara 1 Nv.10, para a mudança de tier nunca parecer um downgrade.

## 13.3 Custos de melhoria

| Para nível | Custo |
|---:|---:|
| 2 | 10.000 |
| 3 | 15.000 |
| 4 | 23.000 |
| 5 | 35.000 |
| 6 | 52.000 |
| 7 | 78.000 |
| 8 | 117.000 |
| 9 | 175.000 |
| 10 | 260.000 |

Custo total de upgrades 1→10: **765.000 Moedas**, além da compra inicial.

Isso é intencional: a Vara 2 atende Mapas 4 e 5, portanto não precisa ser maximizada inteiramente durante o Mapa 4.

Mantém as regras atuais:

- sem XP da vara;
- sem falha;
- sem materiais;
- sem Conchas como custo;
- revenda ao NPC usando a lógica atual;
- negociação no Mercado preservando nível e bônus.

---

# 14. Ajuste de `rarity_efficiency`

A fórmula atual foi escrita pensando apenas em Raro. Agora deve ser generalizada sem criar uma mecânica nova.

Regra simples:

1. pegar o pool do mapa;
2. remover qualquer raridade que a vara não possa capturar;
3. multiplicar o peso de cada espécie **não Comum elegível** por `(1 + rarity_efficiency)`;
4. manter pesos Comuns inalterados;
5. renormalizar o pool.

Assim, o bônus melhora Raro e Épico, mas nunca cria uma raridade ausente do mapa.

Não usar uma soma direta de porcentagem na chance final.

---

# 15. Progressão esperada do jogador

## Nv.20–29 — Pantanal Dourado

Objetivos naturais:

- descobrir as 10 espécies;
- conseguir os primeiros Raros com frequência perceptível;
- tentar capturar Barbado Épico;
- substituir gradualmente peixes antigos no Cardume;
- acumular Moedas para Vara 2;
- continuar evoluindo Vara 1 se desejar.

Ao chegar ao Nv.30, um jogador que vendeu boa parte de suas capturas terá gerado, em ordem de grandeza, ~**212 mil Moedas brutas** durante a progressão 20→30. Isso é suficiente para justificar a compra de 90 mil da Vara 2 sem deixar a compra gratuita em termos econômicos.

## Nv.30–39 — Estuário das Marés

Objetivos naturais:

- comprar/equipar Vara 2;
- descobrir as 10 espécies;
- capturar os primeiros dois Épicos do mapa;
- melhorar Vara 2 parcialmente;
- subir o teto de poder do Cardume;
- aumentar renda e XP;
- chegar ao Nv.40 pronto para um futuro Mapa 5.

Durante 30→40, um jogador que vendesse tudo ao NPC geraria em ordem de grandeza ~**487 mil Moedas brutas**.

Esse valor não deve ser lido como saldo líquido, porque haverá peixes guardados, alimentação, mercado e outros gastos.

---

# 16. Integração mínima com os sistemas existentes

## Fishing Box

Adicionar `epic` aos filtros de raridade.

Peixes Épicos sempre devem ser considerados valiosos em ações em lote.

Se a proteção continuar usando lista explícita em `economy.json`, mudar de:

```json
["rare"]
```

para:

```json
["rare", "epic"]
```

Se o código já possui conceito de `rarity_at_or_above`, preferir a regra por hierarquia.

## Aquário

Nenhuma mudança de capacidade. Continua **100**.

Cards Épicos precisam exibir raridade corretamente sem alterar a lógica do tamanho Excepcional.

## Enciclopédia

Adicionar as 20 espécies novas.

Total após a expansão: **40 espécies**.

Histórico atual permanece intacto.

## Feed

A regra atual de aviso para `rare` ou superior deve incluir Épico automaticamente. Se não incluir, corrigir a comparação de ordem de raridade em vez de criar exceção específica por peixe.

## Cardume / Arena

Peixes novos usam os quatro atributos atuais:

- Vida;
- Ataque;
- Defesa;
- Velocidade.

Não adicionar skills, classes, crítico, esquiva, precisão, IV ou atributos novos.

Os atributos continuam derivados de:

`espécie + raridade + tamanho + nível`

## Mercado / Leilão

Épico deve aparecer corretamente em:

- filtros;
- cards;
- detalhes;
- ordenação, quando houver;
- proteção de item valioso.

Não alterar taxas nem regras do Mercado/Leilão.

## Pesca offline

Novos mapas e pools precisam funcionar na mesma resolução offline atual, respeitando:

- mapa ativo;
- vara equipada;
- raridades permitidas;
- pesos configurados;
- 1 captura/min;
- limite de 24 h.

---

# 17. Compatibilidade de save

Esta expansão deve ser **100% aditiva**.

Regras obrigatórias:

- não resetar jogador;
- não apagar inventário;
- não apagar peixes;
- não alterar IDs dos 20 peixes existentes;
- não alterar IDs de mapas/varas existentes;
- não converter raridade de peixe antigo;
- não conceder Épico retroativamente;
- não criar cópia nova da Vara 1 para jogadores que já possuem uma;
- a mudança de elegibilidade da Vara 1 deve valer para a instância já existente do jogador;
- novos mapas devem ser liberados com base no nível atual do save.

Se a enumeração/serialização de raridade for fechada, migrar de forma compatível para aceitar `epic` sem invalidar saves antigos.

---

# 18. Arquivos/configs que provavelmente precisam ser alterados

Claude deve confirmar a estrutura real antes de editar, mas o conteúdo deve chegar pelo menos a:

### `config/progression.json`

- adicionar `epic`;
- não substituir a curva de XP atual.

### `config/fish_catalog.json`

- adicionar 20 espécies novas;
- não modificar os 20 IDs existentes.

### `config/maps.json`

- adicionar `map_03` e `map_04`;
- incluir unlock level;
- tier mínimo de vara;
- raridades disponíveis;
- pools e pesos.

### `config/rods.json`

- permitir Épico na Vara 1;
- adicionar `rod_02`;
- adicionar seus bônus/custos;
- generalizar a descrição/regra de `rarity_efficiency` para raridades não Comuns elegíveis.

### `config/economy.json`

- incluir Épico na proteção de venda em lote, se a implementação ainda depender de lista explícita.

### UI/cliente

Somente ajustes necessários para:

- novo tier de raridade;
- dois novos mapas;
- Vara 2;
- novas espécies;
- filtros e cards existentes reconhecerem Épico.

Não redesenhar telas sem necessidade.

---

# 19. Assets necessários

A implementação pode usar placeholders temporários se as artes finais ainda não existirem, mas deve deixar as referências organizadas.

Necessários:

- cenário/camadas do Pantanal Dourado;
- cenário/camadas do Estuário das Marés;
- 10 artes de peixe do Mapa 3;
- 10 artes de peixe do Mapa 4;
- arte da Vara 2;
- thumbnails dos dois mapas;
- qualquer asset novo de paisagem viva que for realmente usado.

As artes devem seguir o Art Bible atual. Não mudar direção artística.

---

# 20. Testes obrigatórios de balanceamento

Antes de considerar a expansão pronta, executar simulação grande o suficiente para validar distribuição.

## Mapa 3 — alvo

Sem bônus de vara:

- Comum ~95,2%;
- Raro ~4,5%;
- Épico ~0,3%;
- XP médio ~47,9/captura;
- venda média ~464/captura.

Aceitar pequena variação estatística, mas não diferença estrutural.

## Mapa 4 — alvo

Sem bônus de vara:

- Comum ~93,4%;
- Raro ~6,0%;
- Épico ~0,6%;
- XP médio ~75,7/captura;
- venda média ~1.008/captura.

## Simular também

- Vara 1 Nv.1 e Nv.10 no Mapa 3;
- Vara 2 Nv.1 e Nv.10 no Mapa 4;
- distribuição de tamanhos;
- renda/hora;
- XP/hora;
- quantidade média de Raros/Épicos por hora;
- pesca offline;
- venda em lote com Épico;
- alimentação usando Épico;
- Mercado/Leilão com Épico;
- Cardume/Arena com peixes novos.

---

# 21. Testes funcionais obrigatórios

1. Nv.19 não acessa Mapa 3.
2. Nv.20 acessa Mapa 3.
3. Vara Inicial não pesca no Mapa 3.
4. Vara 1 pesca no Mapa 3.
5. Vara 1 consegue Barbado Épico no Mapa 3.
6. Vara 1 não começa a gerar Épicos no Mapa 2 porque o pool do Mapa 2 não possui Épico.
7. Nv.29 não acessa Mapa 4.
8. Nv.30 sem Vara 2 não pesca no Mapa 4.
9. Nv.30 com Vara 2 equipada pesca no Mapa 4.
10. Vara 2 captura Comum, Raro e Épico no Mapa 4.
11. Camurupim e Mero podem ser salvos, alimentados, vendidos e negociados normalmente.
12. Épico aparece corretamente nos filtros.
13. Épico aciona confirmação de ação valiosa.
14. Enciclopédia passa de 20 para 40 espécies sem perder descobertas existentes.
15. Save antigo abre normalmente após a atualização.
16. Jogador já Nv.20+ recebe acesso ao Mapa 3 sem reset.
17. Jogador já Nv.30+ recebe requisito de nível do Mapa 4 sem reset.
18. Viagem continua manual em 30 s.
19. Pesca offline respeita mapa e vara novos.
20. Nenhum sistema fora do escopo foi recriado ou alterado desnecessariamente.

---

# 22. Ordem de implementação sugerida para Claude

1. Ler projeto atual e identificar contratos/configs existentes.
2. Adicionar suporte a `epic` na hierarquia de raridade.
3. Garantir compatibilidade de save/serialização.
4. Adicionar 20 espécies ao catálogo.
5. Adicionar Mapas 3 e 4 aos dados.
6. Atualizar Vara 1 para permitir Épico.
7. Implementar Vara 2.
8. Generalizar `rarity_efficiency` para raridades não Comuns elegíveis.
9. Atualizar filtros/proteções/UI para Épico.
10. Integrar mapas às cenas e usar placeholders onde arte final ainda estiver pendente.
11. Rodar testes e simuladores.
12. Ajustar apenas se os resultados saírem significativamente dos alvos deste documento.
13. Atualizar `CHANGELOG`, documentação de decisões e roadmap do projeto.

Não renumerar nem reescrever milestones antigos. Criar um novo milestone de expansão após o milestone atual, usando o próximo ID livre.

---

# 23. Critério de conclusão

A expansão só é considerada pronta quando:

- Mapas 3 e 4 funcionam no fluxo normal do jogo;
- 20 espécies novas existem e persistem corretamente;
- Épico funciona em todos os sistemas que lidam com raridade;
- Vara 2 pode ser comprada, evoluída, equipada e negociada;
- XP e economia ficam próximos dos alvos definidos;
- saves antigos permanecem íntegros;
- nenhuma mecânica central fora do escopo foi reescrita;
- a progressão chega de forma limpa ao Nv.40 e deixa o projeto pronto para o futuro Mapa 5.

---

# 24. Resumo executivo

## Mapa 3 — Pantanal Dourado

- Nv.20
- Vara 1
- 6 Comuns / 3 Raros / 1 Épico
- ~47,9 XP/captura
- ~464 Moedas/captura
- primeiro Épico: Barbado
- alvo 20→30: ~3,8 h online

## Mapa 4 — Estuário das Marés

- Nv.30
- Vara 2
- 5 Comuns / 3 Raros / 2 Épicos
- ~75,7 XP/captura
- ~1.008 Moedas/captura
- Épicos: Camurupim e Mero
- alvo 30→40: ~4,0 h online

## Vara 2

- desbloqueio Nv.30
- compra: 90.000 Moedas
- níveis 1–10
- atende Mapas 4–5
- mantém os três bônus existentes: raridade, tamanho e Conchas

## Filosofia

**Mais mapa = mais XP, mais Moedas, peixes mais desejáveis e mais poder, sem mudar o loop central do Fishing Idle.**
