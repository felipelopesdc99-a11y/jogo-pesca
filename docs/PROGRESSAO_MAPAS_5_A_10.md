# FISHING IDLE — PROGRESSÃO COMPLETA DOS MAPAS 5 A 10
## Documento mestre de expansão para implementação no jogo existente

**Escopo:** níveis de Pescador 40–100; Mapas 5–10; 60 novas espécies; novas raridades; Varas 3–5; direção visual; progressão econômica/XP; integração com equipamentos e sistemas existentes.

**Status:** proposta completa de conteúdo e balanceamento inicial. Os valores são concretos para implementação e simulação, mas devem permanecer configuráveis. Ajustes pontuais posteriores não devem exigir reescrever sistemas.

---

# 0. O que é herdado do GDD e o que é novo

## Herdado / não alterar

- nível máximo de Pescador: **100**;
- 10 mapas no total;
- desbloqueios: Mapa 5 Nv.40, Mapa 6 Nv.50, Mapa 7 Nv.60, Mapa 8 Nv.70, Mapa 9 Nv.80, Mapa 10 Nv.90;
- Vara 2 atende Mapas 4–5; Vara 3 atende 6–7; Vara 4 atende 8–9; Vara 5 atende 10;
- Varas compráveis desbloqueiam em Nv.10/30/50/70/90;
- cada Vara comprável possui níveis internos 1–10;
- bônus de Vara continuam ligados a raridade, qualidade de tamanho e geração de Conchas;
- viagem entre mapas continua manual;
- tamanho Excepcional continua sendo **tamanho**, nunca raridade;
- stats dos peixes continuam determinísticos;
- Aquário continua com limite fixo de 100;
- não criar skills, classes, crítico, esquiva, genética/IV, equipamentos de peixe ou outro sistema paralelo.

## Já consolidado nos Mapas 3–4

- Mapa 3: **Pantanal Dourado**, Nv.20, Vara 1/Ponta Selvagem;
- Mapa 4: **Estuário das Marés**, Nv.30, Vara 2/Maré Dourada;
- Épico já existe como raridade ativa;
- a progressão deve aumentar XP, Moedas e poder dos peixes por conteúdo, sem substituir a curva de níveis.

## Novas decisões deste documento

- nomes, temas e conteúdo dos Mapas 5–10;
- 60 espécies novas, totalizando **100 espécies** no jogo ao completar o Mapa 10;
- raridade **Lendário** estreia no Mapa 6;
- raridade **Mítico** estreia exclusivamente no Mapa 10;
- `Incomum` continua reservado/inativo;
- nomes e identidade visual das Varas 3–5;
- preços e bônus iniciais das Varas 3–5;
- distribuição de pesos, valores, XP e stats-base para as 60 espécies;
- nenhum prestige/rebirth ao Nv.100 nesta expansão.

---

# 1. Visão geral da progressão 1–100

| Mapa | Nível | Nome | Vara mínima | Nova raridade-chave | Papel |
|---|---:|---|---|---|---|
| 1 | 1 | Lago Sereno | Caniço Manso | — | começo relaxante |
| 2 | 10 | Rio Selvagem | Ponta Selvagem | Raro | primeira aventura |
| 3 | 20 | Pantanal Dourado | Ponta Selvagem | Épico | primeiro salto real |
| 4 | 30 | Estuário das Marés | Maré Dourada | Épico | transição para o mar |
| 5 | 40 | **Costa de Coral** | **Maré Dourada** | — | Primeiro mapa totalmente marinho. Mantém a Vara 2 útil e transforma a chegada ao oceano em um novo capítulo visual. |
| 6 | 50 | **Arquipélago do Sol** | **Corrente Mestra** | Lendário | Primeiro salto oceânico grande. Introduz Vara 3 e Lendário. Peixes começam a parecer verdadeiros troféus. |
| 7 | 60 | **Corrente Azul** | **Corrente Mestra** | — | Mapa de domínio da Vara 3. Foco em peixes pelágicos rápidos e grandes. Sensação de oceano aberto. |
| 8 | 70 | **Banco das Baleias** | **Atlântico Nobre** | — | Novo tier de equipamento. Oceano profundo sobre bancos e montes submarinos. Lendários tornam-se parte real da caça. |
| 9 | 80 | **Talude Noturno** | **Atlântico Nobre** | — | Preparação para o fim do jogo. Pesca em águas muito profundas na borda da plataforma continental, com espécies estranhas e valiosas. |
| 10 | 90 | **Abismo Atlântico** | **Soberana Abissal** | Mítico | Mapa final da progressão 1–100. Introduz Mítico e a Vara 5. Deve parecer o destino máximo do pescador. |

A progressão final forma uma viagem visual contínua:

> lago → rio → planície alagada → estuário → recife costeiro → arquipélago → mar aberto → banco oceânico → talude profundo → abismo atlântico

---

# 2. Hierarquia de raridades

Manter a ordem:

`Comum < Raro < Épico < Lendário < Mítico`

`Incomum` permanece reservado e **não deve ser ativado**.

## Novos multiplicadores

| Raridade | stat_multiplier | Fisher XP | Feed XP | Venda | Cor UI sugerida |
|---|---:|---:|---:|---:|---|
| Épico (já existente) | 1,30 | ×5,00 | ×3,50 | ×4,00 | roxo `#A855F7` |
| **Lendário** | **1,55** | **×9,00** | **×6,00** | **×8,00** | laranja `#F97316` |
| **Mítico** | **1,85** | **×15,00** | **×10,00** | **×14,00** | rosa-framboesa `#EC4899` |

Não usar dourado como cor principal de Lendário, porque dourado já comunica **tamanho Excepcional**. Um peixe pode ser `Lendário + Excepcional` ou `Mítico + Excepcional` sem conflito visual.

## Proteção de itens

Venda em lote/alimentação deve considerar `Raro ou superior` como valioso, preferencialmente por comparação hierárquica e não por lista hardcoded.

---

# 3. Chance de puxar o peixe

O sistema novo de sucesso/escape continua sendo aplicado depois que o peixe é sorteado.

Se os valores atuais da build para Comum/Raro/Épico forem diferentes, **não sobrescrever**. Apenas adicionar Lendário e Mítico proporcionalmente.

| Raridade | chance-base inicial sugerida |
|---|---:|
| Comum | 50% |
| Raro | 38% |
| Épico | 24% |
| **Lendário** | **14%** |
| **Mítico** | **8%** |

Fórmula conceitual:

`chance_final = base_raridade + bônus_vara + bônus_barco + bônus_isca`

Aplicar piso/teto da configuração atual. Escapar não remove o ciclo: mostra feedback e segue o fluxo normal.

---

# 4. Mapas 5–10 — identidade e progressão

## Mapa 5 — Costa de Coral

**ID:** `map_05`  
**Desbloqueio:** Nv.40  
**Vara mínima:** Maré Dourada (`rod_02`)  
**Raridades:** Comum, Raro, Épico  

**Papel:** Primeiro mapa totalmente marinho. Mantém a Vara 2 útil e transforma a chegada ao oceano em um novo capítulo visual.

**Direção visual:** Costa tropical brasileira rasa, água turquesa muito limpa, recifes/rochas de coral visíveis sob a superfície, faixas de areia clara, pequenas falésias vegetadas ao fundo, céu aberto de manhã clara. Sem resort, prédios ou cidade.

**Luz:** manhã clara e quente, sol alto à esquerda.  
**Vida ambiente:** gaivotas/fragatas ao longe, tartaruga-marinha surgindo raramente, pequenos peixes saltando, espuma suave sobre pedras.  
**Paleta:** turquesa, azul-claro, areia quente, verde costeiro e coral queimado.

**Distribuição-base do pool:** Comum 91.0%, Raro 7.5%, Épico 1.5%.

## Mapa 6 — Arquipélago do Sol

**ID:** `map_06`  
**Desbloqueio:** Nv.50  
**Vara mínima:** Corrente Mestra (`rod_03`)  
**Raridades:** Comum, Raro, Épico, Lendário  

**Papel:** Primeiro salto oceânico grande. Introduz Vara 3 e Lendário. Peixes começam a parecer verdadeiros troféus.

**Direção visual:** Arquipélago tropical brasileiro, ilhas rochosas altas com vegetação, canais de água azul-cobalto, mar mais profundo e ondulação visível. Horizonte amplo e sensação de estar longe da costa.

**Luz:** fim de manhã / começo da tarde, dourado limpo.  
**Vida ambiente:** atobás, fragatas, golfinhos ocasionais e espuma nas pedras.  
**Paleta:** cobalto, azul-profundo, verde-ilha, cinza de rocha e dourado solar.

**Distribuição-base do pool:** Comum 86.5%, Raro 10.0%, Épico 3.0%, Lendário 0.5%.

## Mapa 7 — Corrente Azul

**ID:** `map_07`  
**Desbloqueio:** Nv.60  
**Vara mínima:** Corrente Mestra (`rod_03`)  
**Raridades:** Comum, Raro, Épico, Lendário  

**Papel:** Mapa de domínio da Vara 3. Foco em peixes pelágicos rápidos e grandes. Sensação de oceano aberto.

**Direção visual:** Mar aberto brasileiro sem terra visível, grandes ondulações longas, água azul intensa, horizonte enorme, nuvens altas e vento constante. Deve transmitir liberdade e distância sem parecer perigoso.

**Luz:** fim de tarde dourado, reflexos longos na água.  
**Vida ambiente:** peixes-voadores, aves oceânicas, golfinhos distantes, cardumes riscando a superfície.  
**Paleta:** ultramarino, azul-marinho, branco frio, dourado e azul-celeste.

**Distribuição-base do pool:** Comum 80.0%, Raro 13.5%, Épico 5.5%, Lendário 1.0%.

## Mapa 8 — Banco das Baleias

**ID:** `map_08`  
**Desbloqueio:** Nv.70  
**Vara mínima:** Atlântico Nobre (`rod_04`)  
**Raridades:** Comum, Raro, Épico, Lendário  

**Papel:** Novo tier de equipamento. Oceano profundo sobre bancos e montes submarinos. Lendários tornam-se parte real da caça.

**Direção visual:** Banco oceânico inspirado na costa brasileira, água azul-esverdeada mais escura, ilhotas rochosas muito distantes, mar profundo com zonas claras sobre bancos submarinos. Presença de baleias é ambiental, não mecânica.

**Luz:** amanhecer frio com dourado suave no horizonte.  
**Vida ambiente:** baleia-jubarte soprando ou saltando raramente, trinta-réis, atobás e grandes manchas de cardume.  
**Paleta:** azul-aço, verde-petróleo, cinza de baleia, pérola e pêssego frio.

**Distribuição-base do pool:** Comum 72.0%, Raro 17.0%, Épico 9.0%, Lendário 2.0%.

## Mapa 9 — Talude Noturno

**ID:** `map_09`  
**Desbloqueio:** Nv.80  
**Vara mínima:** Atlântico Nobre (`rod_04`)  
**Raridades:** Comum, Raro, Épico, Lendário  

**Papel:** Preparação para o fim do jogo. Pesca em águas muito profundas na borda da plataforma continental, com espécies estranhas e valiosas.

**Direção visual:** Oceano profundo ao anoitecer/noite, sem costa visível. Água azul-marinho quase preta, lua baixa ou céu estrelado, brilho mínimo de plâncton e reflexos frios. Misterioso, mas nunca terror.

**Luz:** crepúsculo azul / noite limpa.  
**Vida ambiente:** petreis, brilho discreto de plâncton, bolhas profundas ocasionais e silhuetas grandes sob a superfície.  
**Paleta:** azul-noite, índigo, ciano controlado, violeta acinzentado e prata lunar.

**Distribuição-base do pool:** Comum 62.0%, Raro 22.0%, Épico 12.5%, Lendário 3.5%.

## Mapa 10 — Abismo Atlântico

**ID:** `map_10`  
**Desbloqueio:** Nv.90  
**Vara mínima:** Soberana Abissal (`rod_05`)  
**Raridades:** Comum, Raro, Épico, Lendário, Mítico  

**Papel:** Mapa final da progressão 1–100. Introduz Mítico e a Vara 5. Deve parecer o destino máximo do pescador.

**Direção visual:** Mar oceânico extremamente profundo sob céu quase sem luz, água negro-azulada, linha de horizonte mínima, estrelas fortes e fosforescência muito discreta na água. A cena deve ser majestosa e silenciosa, não assustadora nem fantasiosa.

**Luz:** noite profunda / pré-amanhecer, luz fria com poucos reflexos.  
**Vida ambiente:** plâncton luminoso muito sutil, raríssimos vultos profundos, ondas longas e lentas, céu estrelado.  
**Paleta:** azul-abissal, preto azulado, ciano frio, violeta escuro e prata.

**Distribuição-base do pool:** Comum 52.0%, Raro 25.0%, Épico 16.0%, Lendário 6.8%, Mítico 0.2%.

---

# 5. Catálogo completo dos Mapas 5–10

Os tamanhos abaixo são faixas de gameplay estilizadas; não devem ser tratados como tabela científica. Todos os números permanecem editáveis.

## Mapa 5 — Costa de Coral

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `sargo` | Sargo | **Comum** | 20–55 | 325 | 43 | 34 | 100 | 1.150 | 250 | 70 | 220 |
| `salema` | Salema | **Comum** | 15–45 | 280 | 46 | 26 | 112 | 1.200 | 260 | 75 | 200 |
| `peixe_porco` | Peixe-porco | **Comum** | 20–60 | 400 | 40 | 41 | 88 | 1.300 | 280 | 80 | 180 |
| `pampo` | Pampo | **Comum** | 20–70 | 295 | 48 | 25 | 112 | 1.400 | 300 | 85 | 165 |
| `sargentinho` | Sargentinho | **Comum** | 10–25 | 305 | 50 | 24 | 112 | 1.450 | 310 | 90 | 145 |
| `ariaco` | Ariacó | **Raro** | 25–70 | 325 | 43 | 34 | 100 | 2.000 | 370 | 92 | 30 |
| `cioba` | Cioba | **Raro** | 30–100 | 315 | 54 | 28 | 105 | 2.150 | 390 | 99 | 25 |
| `badejo` | Badejo | **Raro** | 40–120 | 400 | 40 | 41 | 88 | 2.300 | 420 | 105 | 20 |
| `dentao` | Dentão | **Épico** | 50–140 | 330 | 56 | 26 | 105 | 4.050 | 660 | 154 | 8 |
| `caranha` | Caranha | **Épico** | 50–150 | 455 | 51 | 35 | 84 | 4.250 | 690 | 162 | 7 |

**Pool:** 1000.

## Mapa 6 — Arquipélago do Sol

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `olhete` | Olhete | **Comum** | 30–100 | 325 | 52 | 32 | 113 | 1.850 | 320 | 88 | 210 |
| `peixe_galo` | Peixe-galo | **Comum** | 30–100 | 330 | 53 | 31 | 113 | 1.950 | 340 | 94 | 190 |
| `beijupira` | Beijupirá | **Comum** | 60–180 | 400 | 52 | 38 | 101 | 2.100 | 360 | 100 | 175 |
| `bonito_cachorro` | Bonito-cachorro | **Comum** | 40–100 | 350 | 56 | 30 | 113 | 2.250 | 380 | 106 | 155 |
| `serra` | Serra | **Comum** | 40–140 | 400 | 67 | 30 | 106 | 2.350 | 400 | 112 | 135 |
| `cavala_verdadeira` | Cavala-verdadeira | **Raro** | 60–170 | 325 | 52 | 32 | 113 | 3.150 | 480 | 119 | 55 |
| `olho_de_boi` | Olho-de-boi | **Raro** | 60–170 | 370 | 62 | 33 | 106 | 3.400 | 510 | 127 | 45 |
| `dourado_do_mar` | Dourado-do-mar | **Épico** | 60–200 | 340 | 55 | 30 | 113 | 5.600 | 780 | 185 | 16 |
| `atum_amarelo` | Atum-amarelo | **Épico** | 80–240 | 525 | 58 | 43 | 85 | 5.950 | 830 | 196 | 14 |
| `veleiro` | Veleiro | **Lendário** | 150–300 | 355 | 57 | 29 | 113 | 10.100 | 1.230 | 291 | 5 |

**Pool:** 1000.

## Mapa 7 — Corrente Azul

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `bicuda` | Bicuda | **Comum** | 50–160 | 425 | 70 | 39 | 107 | 2.800 | 400 | 110 | 230 |
| `bonito_pintado` | Bonito-pintado | **Comum** | 40–100 | 390 | 61 | 36 | 114 | 3.000 | 430 | 118 | 210 |
| `agulha_branca` | Agulha-branca | **Comum** | 60–160 | 400 | 63 | 35 | 114 | 3.200 | 460 | 125 | 190 |
| `cavalinha` | Cavalinha | **Comum** | 20–45 | 410 | 65 | 34 | 114 | 3.400 | 490 | 132 | 170 |
| `atum_patudo` | Atum-patudo | **Raro** | 80–250 | 630 | 68 | 48 | 86 | 5.800 | 760 | 190 | 50 |
| `albacora_branca` | Albacora-branca | **Raro** | 70–140 | 380 | 60 | 37 | 114 | 4.600 | 600 | 150 | 45 |
| `peixe_lua` | Peixe-lua | **Raro** | 100–300 | 540 | 53 | 58 | 90 | 4.900 | 640 | 160 | 40 |
| `espadarte` | Espadarte | **Épico** | 150–450 | 445 | 73 | 37 | 107 | 8.200 | 980 | 230 | 30 |
| `marlim_branco` | Marlim-branco | **Épico** | 180–350 | 410 | 65 | 34 | 114 | 8.700 | 1.040 | 244 | 25 |
| `marlim_azul` | Marlim-azul | **Lendário** | 200–500 | 630 | 68 | 48 | 86 | 14.550 | 1.570 | 358 | 10 |

**Pool:** 1000.

## Mapa 8 — Banco das Baleias

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `pargo_rosa` | Pargo-rosa | **Comum** | 30–90 | 530 | 65 | 52 | 100 | 4.200 | 510 | 136 | 200 |
| `namorado` | Namorado | **Comum** | 40–120 | 545 | 66 | 51 | 100 | 4.500 | 550 | 146 | 190 |
| `batata` | Batata | **Comum** | 30–100 | 660 | 61 | 64 | 88 | 4.800 | 580 | 155 | 175 |
| `congro_rosa` | Congro-rosa | **Comum** | 60–180 | 545 | 85 | 41 | 105 | 5.100 | 610 | 164 | 155 |
| `cherne` | Cherne | **Raro** | 60–200 | 755 | 77 | 55 | 84 | 8.500 | 960 | 235 | 90 |
| `cacao_anjo` | Cação-anjo | **Raro** | 80–180 | 630 | 58 | 67 | 88 | 6.700 | 760 | 185 | 80 |
| `tubarao_lixa` | Tubarão-lixa | **Épico** | 150–320 | 700 | 72 | 59 | 84 | 11.300 | 1.180 | 273 | 48 |
| `arraia_chita` | Arraia-chita | **Épico** | 100–250 | 475 | 71 | 40 | 112 | 12.000 | 1.250 | 290 | 42 |
| `tubarao_martelo` | Tubarão-martelo | **Lendário** | 180–450 | 545 | 85 | 41 | 105 | 20.150 | 1.910 | 424 | 11 |
| `raia_manta` | Raia-manta | **Lendário** | 250–700 | 755 | 77 | 55 | 84 | 21.300 | 2.020 | 448 | 9 |

**Pool:** 1000.

## Mapa 9 — Talude Noturno

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `escolar` | Escolar | **Comum** | 70–220 | 605 | 90 | 51 | 103 | 6.150 | 630 | 167 | 230 |
| `peixe_espada` | Peixe-espada | **Comum** | 80–250 | 555 | 80 | 48 | 110 | 6.600 | 680 | 179 | 210 |
| `abrotea_fundo` | Abrótea-de-fundo | **Comum** | 40–120 | 670 | 78 | 58 | 98 | 7.000 | 720 | 190 | 180 |
| `congro_negro` | Congro-negro | **Raro** | 100–250 | 650 | 98 | 48 | 103 | 11.650 | 1.110 | 276 | 80 |
| `peixe_lanceta` | Peixe-lanceta | **Raro** | 100–220 | 600 | 86 | 44 | 110 | 12.300 | 1.180 | 291 | 75 |
| `quimera` | Quimera | **Raro** | 60–150 | 685 | 80 | 67 | 90 | 9.700 | 920 | 229 | 65 |
| `tubarao_lanterna` | Tubarão-lanterna | **Épico** | 40–90 | 705 | 82 | 65 | 90 | 16.000 | 1.460 | 338 | 65 |
| `peixe_opah` | Peixe-opah | **Épico** | 80–200 | 790 | 70 | 74 | 86 | 17.000 | 1.550 | 360 | 60 |
| `tubarao_duende` | Tubarão-duende | **Lendário** | 180–400 | 650 | 98 | 48 | 103 | 29.700 | 2.440 | 530 | 18 |
| `tubarao_seis_guelras` | Tubarão-seis-guelras | **Lendário** | 250–550 | 900 | 88 | 63 | 82 | 31.350 | 2.580 | 560 | 17 |

**Pool:** 1000.

## Mapa 10 — Abismo Atlântico

| ID | Espécie | Raridade | Tamanho cm | HP | ATQ | DEF | VEL | Venda base | Feed XP base | Fisher XP base | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `peixe_machado` | Peixe-machado | **Comum** | 8–18 | 820 | 92 | 76 | 88 | 8.800 | 790 | 202 | 190 |
| `peixe_dragao` | Peixe-dragão | **Comum** | 15–40 | 740 | 107 | 57 | 101 | 9.400 | 850 | 216 | 175 |
| `peixe_vibora` | Peixe-víbora | **Comum** | 20–35 | 760 | 110 | 55 | 101 | 10.000 | 900 | 230 | 155 |
| `enguia_pelicano` | Enguia-pelicano | **Raro** | 60–180 | 885 | 100 | 71 | 88 | 16.950 | 1.430 | 339 | 130 |
| `peixe_ogro` | Peixe-ogro | **Raro** | 10–25 | 800 | 115 | 53 | 101 | 17.900 | 1.510 | 358 | 120 |
| `peixe_pescador_abissal` | Peixe-pescador-abissal | **Épico** | 20–100 | 820 | 92 | 76 | 88 | 22.000 | 1.760 | 396 | 85 |
| `quimera_azul` | Quimera-azul | **Épico** | 70–180 | 840 | 95 | 74 | 88 | 23.500 | 1.880 | 423 | 75 |
| `tubarao_cobra` | Tubarão-cobra | **Lendário** | 150–220 | 760 | 110 | 55 | 101 | 42.000 | 3.000 | 650 | 36 |
| `peixe_fita_gigante` | Peixe-fita-gigante | **Lendário** | 250–900 | 695 | 97 | 51 | 108 | 44.500 | 3.180 | 689 | 32 |
| `tubarao_boca_grande` | Tubarão-boca-grande | **Mítico** | 300–550 | 1075 | 102 | 72 | 81 | 100.800 | 5.380 | 1064 | 2 |

**Pool:** 1000.

---

# 6. Varas 3–5

A família final de nomes fica:

> **Caniço Manso → Ponta Selvagem → Maré Dourada → Corrente Mestra → Atlântico Nobre → Soberana Abissal**

| ID | Nome | Unlock | Tier | Compatibilidade | Raridades permitidas | Compra |
|---|---|---:|---:|---|---|---:|
| `rod_03` | **Corrente Mestra** | Nv.50 | 3 | Mapas 6–7 | Comum, Raro, Épico, Lendário | 450.000 Moedas |
| `rod_04` | **Atlântico Nobre** | Nv.70 | 4 | Mapas 8–9 | Comum, Raro, Épico, Lendário | 1.800.000 Moedas + 180 Conchas |
| `rod_05` | **Soberana Abissal** | Nv.90 | 5 | Mapa 10 | Comum, Raro, Épico, Lendário, Mítico | 8.000.000 Moedas + 650 Conchas |

## Corrente Mestra

**Visual:** Fibra de carbono azul-cobalto quase preta, anéis em titânio, detalhes turquesa, cabo EVA/cortiça escura e molinete robusto grafite-azul.

### Bônus por nível

| Nível | rarity_efficiency | size_quality | shell_yield | success_bonus |
|---:|---:|---:|---:|---:|
| 1 | 0.52 | 0.44 | 1.15 | +12 p.p. |
| 2 | 0.55 | 0.46 | 1.22 | +13 p.p. |
| 3 | 0.58 | 0.48 | 1.29 | +14 p.p. |
| 4 | 0.61 | 0.50 | 1.36 | +15 p.p. |
| 5 | 0.64 | 0.52 | 1.43 | +16 p.p. |
| 6 | 0.67 | 0.54 | 1.50 | +18 p.p. |
| 7 | 0.70 | 0.56 | 1.57 | +19 p.p. |
| 8 | 0.73 | 0.58 | 1.64 | +21 p.p. |
| 9 | 0.76 | 0.60 | 1.70 | +22 p.p. |
| 10 | 0.80 | 0.62 | 1.75 | +24 p.p. |

### Custos de melhoria

| Upgrade | Custo |
|---:|---:|
| → Nv.2 | 60.000 Moedas |
| → Nv.3 | 90.000 Moedas |
| → Nv.4 | 135.000 Moedas |
| → Nv.5 | 200.000 Moedas |
| → Nv.6 | 300.000 Moedas |
| → Nv.7 | 450.000 Moedas |
| → Nv.8 | 675.000 Moedas |
| → Nv.9 | 1.000.000 Moedas |
| → Nv.10 | 1.500.000 Moedas |
| **Total 1→10** | **4.410.000 Moedas** |

## Atlântico Nobre

**Visual:** Carbono azul-noite, acabamento metálico prateado, detalhes discretos em dourado frio, cabo preto premium e molinete grande de alto torque.

### Bônus por nível

| Nível | rarity_efficiency | size_quality | shell_yield | success_bonus |
|---:|---:|---:|---:|---:|
| 1 | 0.82 | 0.64 | 1.80 | +18 p.p. |
| 2 | 0.85 | 0.66 | 1.88 | +19 p.p. |
| 3 | 0.88 | 0.68 | 1.96 | +21 p.p. |
| 4 | 0.92 | 0.70 | 2.04 | +22 p.p. |
| 5 | 0.95 | 0.72 | 2.12 | +24 p.p. |
| 6 | 0.98 | 0.74 | 2.20 | +25 p.p. |
| 7 | 1.02 | 0.76 | 2.28 | +27 p.p. |
| 8 | 1.05 | 0.78 | 2.36 | +29 p.p. |
| 9 | 1.08 | 0.80 | 2.43 | +30 p.p. |
| 10 | 1.12 | 0.82 | 2.50 | +32 p.p. |

### Custos de melhoria

| Upgrade | Custo |
|---:|---:|
| → Nv.2 | 250.000 Moedas |
| → Nv.3 | 375.000 Moedas |
| → Nv.4 | 560.000 Moedas |
| → Nv.5 | 840.000 Moedas |
| → Nv.6 | 1.250.000 Moedas |
| → Nv.7 | 1.900.000 Moedas |
| → Nv.8 | 2.850.000 Moedas |
| → Nv.9 | 4.250.000 Moedas |
| → Nv.10 | 6.400.000 Moedas |
| **Total 1→10** | **18.675.000 Moedas** |

## Soberana Abissal

**Visual:** Carbono negro-azulado, componentes de titânio escuro, detalhes ciano e dourado muito controlados, grande molinete de pesca profunda; aparência máxima sem fantasia.

### Bônus por nível

| Nível | rarity_efficiency | size_quality | shell_yield | success_bonus |
|---:|---:|---:|---:|---:|
| 1 | 1.15 | 0.85 | 2.60 | +25 p.p. |
| 2 | 1.19 | 0.87 | 2.70 | +27 p.p. |
| 3 | 1.23 | 0.89 | 2.80 | +29 p.p. |
| 4 | 1.27 | 0.92 | 2.90 | +31 p.p. |
| 5 | 1.31 | 0.94 | 3.00 | +33 p.p. |
| 6 | 1.35 | 0.96 | 3.10 | +34 p.p. |
| 7 | 1.39 | 0.98 | 3.20 | +36 p.p. |
| 8 | 1.43 | 1.00 | 3.30 | +37 p.p. |
| 9 | 1.47 | 1.03 | 3.40 | +39 p.p. |
| 10 | 1.50 | 1.05 | 3.50 | +40 p.p. |

### Custos de melhoria

| Upgrade | Custo |
|---:|---:|
| → Nv.2 | 1.000.000 Moedas |
| → Nv.3 | 1.500.000 Moedas |
| → Nv.4 | 2.250.000 Moedas |
| → Nv.5 | 3.400.000 Moedas |
| → Nv.6 | 5.100.000 Moedas |
| → Nv.7 | 7.600.000 Moedas |
| → Nv.8 | 11.400.000 Moedas |
| → Nv.9 | 17.000.000 Moedas |
| → Nv.10 | 25.000.000 Moedas |
| **Total 1→10** | **74.250.000 Moedas** |

### Regra econômica das Varas

- a Vara nova deve começar levemente acima da anterior no Nv.10;
- o preço de compra deve exigir planejamento, mas não grindar o jogador por dezenas de horas antes de poder usar o mapa recém-liberado;
- se a economia real da build divergir, ajustar **custos**, não reescrever toda a curva de venda/XP;
- todos os valores ficam em `config/rods.json` / Painel de Desenvolvimento.

---

# 7. Barcos e iscas ao longo do restante da progressão

Não criar novas famílias automaticamente. O conjunto já desenhado é suficiente até o Nv.100.

## Barcos

| Nível | Barco | Papel |
|---:|---|---|
| 1 | Água Mansa | inicial |
| 10 | Remo Valente | começo da progressão |
| 20 | Rastro Azul | Pantanal |
| 30 | Proa Selvagem | Estuário/Costa de Coral |
| 50 | Costa Nobre | Arquipélago/Corrente Azul/Banco das Baleias |
| 80 | Horizonte Dourado | Talude Noturno/Abismo Atlântico |

## Iscas

- **Terra Viva:** econômica e comum;
- **Maré Viva:** uso intermediário;
- **Ouro de Maré:** melhor chance de sucesso e maior custo.

Não adicionar mais iscas só para preencher tiers. Se a economia pedir um novo consumível no futuro, isso deve ser uma decisão separada.

---

# 8. Progressão por faixa de nível

## Nv.40–49 — Costa de Coral

- continuar usando Maré Dourada;
- completar primeiras espécies totalmente marinhas;
- Épicos deixam de ser novidade única, mas continuam valiosos;
- preparar Moedas/Conchas para Corrente Mestra.

## Nv.50–59 — Arquipélago do Sol

- comprar/equipar Corrente Mestra;
- desbloquear o primeiro Lendário: **Veleiro**;
- Costa Nobre passa a ser o barco-alvo;
- peixes já devem substituir claramente exemplares dos Mapas 3–4 no Cardume.

## Nv.60–69 — Corrente Azul

- dominar Corrente Mestra;
- foco em grandes pelágicos;
- **Marlim-azul** é o principal troféu desta faixa;
- acumular para Atlântico Nobre.

## Nv.70–79 — Banco das Baleias

- comprar/equipar Atlântico Nobre;
- dois Lendários no mesmo mapa pela primeira vez;
- sensação de equipamento realmente profissional.

## Nv.80–89 — Talude Noturno

- Horizonte Dourado torna-se o melhor barco disponível;
- espécies profundas e estranhas mudam fortemente a identidade visual;
- preparar grande reserva para Soberana Abissal.

## Nv.90–100 — Abismo Atlântico

- comprar/equipar Soberana Abissal;
- Mítico é liberado exclusivamente aqui;
- **Tubarão-boca-grande** funciona como captura máxima da progressão atual;
- chegar ao Nv.100 encerra a progressão de nível atual.

**Não implementar prestige, reset, rebirth ou novos níveis além de 100 neste documento.**

---

# 9. Metas de XP e economia

A curva de XP do Pescador existente continua sendo a fonte de verdade. Não substituir a curva.

A progressão deve ser acelerada pelos peixes de mapas maiores entregarem mais XP e valor.

Objetivo de tuning inicial:

| Faixa | XP/captura bem-sucedida | Moedas/h brutas | Observação |
|---|---|---|---|
| 40–50 | ~1,25–1,40× o Mapa 4 | ~1,4–1,7× Mapa 4 | economizar para Vara 3 |
| 50–60 | ~1,30× Mapa 5 | ~1,5× Mapa 5 | primeira faixa Lendária |
| 60–70 | ~1,25× Mapa 6 | ~1,4× Mapa 6 | Vara 3 sendo maximizada |
| 70–80 | ~1,30× Mapa 7 | ~1,5× Mapa 7 | financiar Vara 4 |
| 80–90 | ~1,25× Mapa 8 | ~1,4× Mapa 8 | financiar Vara 5 |
| 90–100 | ~1,25× Mapa 9 | ~1,4× Mapa 9 | endgame atual |

Essas metas devem ser verificadas com o sistema real de sucesso/escape. **Moedas/h e XP/h devem contar apenas peixes efetivamente puxados**, não apenas sorteados.

---

# 10. Identidade visual e assets por mapa

Cada mapa precisa parecer um capítulo novo, sem abandonar o mesmo artista/estilo Clean Premium.

Para cada Mapa 5–10 prever:

- pintura mestre de referência;
- céu;
- horizonte/fundo distante;
- camada média específica do bioma;
- margem/primeiro plano esquerdo;
- margem/primeiro plano direito;
- nuvens/efeitos separados quando necessário;
- fauna ambiente em sprites;
- 10 peixes de perfil;
- foto/card do mapa;
- variações suficientes para o diretor de paisagem viva não ficar repetitivo.

Regras:

- nada fotorrealista;
- nada de render 3D genérico;
- formas limpas, sombreado suave, pouca textura;
- brilho e partículas são aplicados pelo jogo, não pintados no peixe;
- cenário primeiro, UI depois;
- mapas escuros 9–10 precisam continuar confortáveis por horas e não virar terror.

---

# 11. UI das novas raridades

## Lendário

- selo `LENDÁRIO`;
- acento/borda laranja;
- celebração: **“Captura lendária!”**;
- usar intensidade visual maior que Épico, mas ainda limpa.

## Mítico

- selo `MÍTICO`;
- acento/borda rosa-framboesa;
- celebração: **“Captura mítica!”**;
- evento visual mais raro da pesca normal.

**Som:** não adicionar som específico de raridade por conta própria. Manter a regra atual de sons especiais, salvo decisão posterior do proprietário.

---

# 12. Integração com sistemas existentes

Os 60 peixes novos devem funcionar automaticamente em:

- Caixa de Pesca;
- Aquário;
- Enciclopédia;
- Cardume;
- Arena;
- Mercado;
- Leilão;
- Alimentação;
- Expedições;
- pesca offline;
- filtros e proteção de itens valiosos.

Não criar lógica paralela para Lendário/Mítico. Expandir a hierarquia existente.

---

# 13. Compatibilidade de save

- nenhuma expansão pode resetar o jogador;
- IDs dos 40 peixes existentes permanecem intactos;
- IDs dos Mapas 1–4 e Varas 0–2 permanecem intactos;
- jogador acima do nível de um novo mapa recebe o requisito de nível automaticamente, sem viagem automática;
- novas raridades precisam ser desserializadas sem invalidar saves antigos;
- Enciclopédia apenas anexa as novas entradas;
- defaults seguros para campos de novos equipamentos.

---

# 14. Configurações a estender

Claude deve confirmar a estrutura real da build antes de editar, mas a expansão deve atingir pelo menos:

- `config/maps.json` — Mapas 5–10, pools, pesos, níveis e tiers;
- `config/fish_catalog.json` — 60 espécies;
- `config/progression.json` — Legendary/Mythic;
- `config/rods.json` — Varas 3–5;
- `config/economy.json` — proteção por hierarquia;
- visual theme — cores de Legendary/Mythic;
- landscape director — perfis ambientais 5–10;
- textos PT-BR;
- filtros de UI;
- simulador/balanceamento.

Evitar qualquer `switch` fixo com somente `common/rare/epic`. Preferir ordem/hierarquia vinda de config.

---

# 15. Simulações obrigatórias

Antes de considerar cada mapa pronto, simular no mínimo **100.000 tentativas** por cenário:

- Vara mínima Nv.1;
- Vara mínima Nv.5;
- Vara mínima Nv.10;
- barco esperado para a faixa;
- sem isca;
- Maré Viva;
- Ouro de Maré.

Registrar:

- tentativas;
- peixes sorteados por raridade;
- peixes efetivamente capturados por raridade;
- taxa de fuga;
- tamanho Grande/Excepcional;
- XP/h;
- Moedas/h;
- Conchas/h;
- tempo estimado para compra/upgrade da próxima Vara;
- distribuição de força dos peixes para Cardume/Arena.

---

# 16. Ordem recomendada de implementação

1. Adicionar `legendary` e `mythic` na hierarquia/config, sem ativá-los em mapas antigos.
2. Adicionar as 60 espécies ao catálogo.
3. Adicionar Mapas 5–10 aos dados, inicialmente com placeholders visuais.
4. Adicionar Varas 3–5.
5. Generalizar filtros/proteções/cards/celebrações para qualquer raridade configurada.
6. Integrar novos pools à pesca online e offline.
7. Integrar Arena/Expedições/Mercado apenas onde houver lista explícita de espécies/raridades.
8. Rodar migração com save atual.
9. Rodar simuladores por mapa.
10. Ajustar economia/custos se necessário.
11. Integrar arte final mapa a mapa.
12. Atualizar GDD_ADENDO, DECISOES, CHANGELOG e roadmap.

---

# 17. Critério de conclusão do jogo 1–100

O bloco de progressão é considerado completo quando:

- existem 10 mapas jogáveis;
- existem 100 espécies no catálogo;
- níveis 1–100 podem ser percorridos sem buracos de conteúdo;
- Varas 0–5 têm função clara e não viram downgrade ao trocar de tier;
- Lendário funciona a partir do Mapa 6;
- Mítico existe somente no Mapa 10;
- todos os peixes funcionam em coleção, Cardume, Arena e comércio;
- economia permite comprar equipamento por progressão normal;
- save atual migra sem perda;
- cada mapa é visualmente reconhecível em segundos;
- Nv.100 é atingível e representa o fim da progressão atual, sem reset obrigatório.

---

# 18. Regra final para Claude

Este documento **não pede para recriar o jogo**.

O jogo já existe. A tarefa é estender os mesmos sistemas data-driven até o Nv.100.

Quando houver conflito entre este documento e a build atual:

1. preservar save e contratos existentes;
2. preservar decisões estruturais já implementadas;
3. adaptar os números novos à economia real;
4. registrar qualquer mudança estrutural como decisão explícita, nunca silenciosa.

O objetivo da progressão é fazer cada 10 níveis parecer um novo capítulo, sem transformar Fishing Idle em um jogo cheio de sistemas paralelos.