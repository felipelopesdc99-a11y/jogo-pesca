# Fishing Idle — Progressão dos Mapas 5 a 10, adaptada ao jogo atual

**Base:** `docs/PROGRESSAO_MAPAS_5_A_10.md`, enviado pelo proprietário em 05/10/2026. **Adaptado em:** 05/10/2026, a pedido dele:
"mantenha as progressões de mapas e os peixes; adapte o que for preciso às regras de agora". **Situação:** proposta aprovada no conteúdo, ainda **não** está no jogo (nada em `/config` mudou).

Os números deste documento saem de `tools/Progressao/adaptar_mapas_5_10.py` e ficam em `docs/propostas/mapas_5_10.json`, pronto para virar `/config` na implementação (roadmap M21).

## O que fica exatamente como o proprietário mandou

- Os 6 mapas, os nomes, os níveis (40, 50, 60, 70, 80, 90), a vara mínima de cada um, a direção visual, a luz, a fauna e as paletas (seção 4 do original).
- As 60 espécies, os IDs, os nomes, as raridades, os tamanhos e os pesos de captura de cada mapa. A ordem de valor entre as espécies de um mesmo mapa também não muda.
- Lendário a partir do Mapa 6 e Mítico só no Mapa 10, com os multiplicadores do documento (atributos 1,55 e 1,85; XP do Pescador ×9 e ×15; alimento ×6 e ×10; venda ×8 e ×14) e chance-base de puxar 14% e 8%.
- As Varas 3 a 5 (Corrente Mestra, Atlântico Nobre, Soberana Abissal), os níveis de liberação, os mapas que cada uma atende e os bônus de raridade, tamanho e Conchas por nível.
- Barcos e iscas: nada novo (os níveis do documento já são os do jogo). Nenhum prestígio ou reinício no Nv.100. A curva de XP do Pescador não muda.

## O que foi adaptado às regras de agora, e por quê

| Ponto | No documento original | Adaptado | Regra atual que pede isso |
|---|---|---|---|
| Valor de venda dos peixes | Mapa 5 vale menos que o Mapa 4 atual (ex.: Raro 2.000–2.300 contra 2.635–3.410 no Estuário) | Cada raridade recebe um fator que faz o Mapa 5 render 1,5× o Mapa 4 real (Comum ×1,40, Raro ×2,09, Épico/Lendário/Mítico ×2,04). O mesmo fator vale nos Mapas 6 a 10, então a escada entre os mapas do documento continua igual | O documento foi escrito com números antigos e pede para adaptar à economia real (seção 18) |
| XP do Pescador por peixe | Mapa 5 dá menos XP que o Mapa 4; do Mapa 7 em diante cada faixa de 10 níveis ficaria cada vez mais curta (o 90–100 em menos de 1 h online) | O XP de cada mapa foi calibrado para a faixa dele levar o mesmo tempo online que a faixa 30–40 leva hoje (cerca de 3,74 h), com uma trava: em cada raridade, o XP de um mapa nunca fica abaixo do mapa anterior. Proporção entre as espécies mantida. Por causa da trava, as faixas do 60 ao 100 ficam um pouco mais rápidas (decisão OD-047) | A curva de XP do jogo é a fonte de verdade (seção 9) e o ritmo atual dos Mapas 3 e 4 (A-095) |
| XP de alimento | Abaixo do Mapa 4 | Mesmo método da venda, mirando 1,3× o Mapa 4 (Comum ×1,63, Raro ×2,13, Épico/Lendário/Mítico ×2,02) | Economia real |
| Atributos | Alguns Raros e Épicos do Mapa 5 mais fracos que os do Mapa 4 | Só sobem quando o Mapa 5 ficaria abaixo do Mapa 4 +15%: Ataque do Raro ×1,22; Vida ×1,22, Ataque ×1,18 e Defesa ×1,62 do Épico (o mesmo vale para Lendário e Mítico). Velocidade não muda | Peixe de mapa novo deve substituir o antigo no Cardume (seção 8 do original) |
| Chance de puxar das varas | +12 a +40 p.p. | Um terço menor: +8 a +26,7 p.p. A vara nova começa abaixo do Nv.10 da anterior em chance, mas acima em raridade, tamanho e Conchas, igual a Maré Dourada faz hoje com a Ponta Selvagem (+5,5 contra +8) | A-095: todos os bônus de chance caíram um terço; o documento usou a escala antiga |
| Conchas nas varas | Corrente Mestra e as melhorias das Varas 3 a 5 só em Moedas | Toda compra e melhoria pede Conchas: Corrente Mestra 85 (entre os 40 da Maré Dourada e os 180 da Atlântico Nobre); melhorias no mesmo formato da Maré Dourada, proporcional à compra | A-099: todo equipamento pede Conchas |
| Preço das varas em Moedas | 450 mil / 1,8 mi / 8 mi | Multiplicado pelo quanto a renda por peixe subiu no mapa onde o jogador junta o dinheiro, para o tempo de compra continuar o que o documento pensou (cerca de 3 h online cada) | Seção 6: "ajustar custos, não reescrever a curva" |
| Cor do Lendário | Laranja `#F97316` | Vermelho-rubi `#E5484D` | O laranja colide com o tamanho Grande (`#FF9F6B`) e o dourado com Excepcional; o tema hoje usa dourado provisório para Lendário |
| Cor do Mítico | Rosa-framboesa `#EC4899` | Igual | — (o tema hoje tem um coral provisório, que sai) |
| Tamanhos de Lendário e Mítico | Não fala | Um pouco menos Grande, Excepcional e Perfeição, continuando a regra do Raro e do Épico (Lendário: Grande ×0,6, Excepcional e Perfeição ×0,35; Mítico: ×0,5 e ×0,25) | Regra de tamanho por raridade (Raro ×0,85/0,7; Épico ×0,7/0,45) |
| Frase de chegada e capítulo | Não fala | Capítulos 5 a 10, cada um com uma frase de chegada (tabela abaixo) | A-085: cada mapa é um capítulo com título ao chegar |
| Proteção de peixes valiosos | "Raro ou superior" | A venda em lote passa a listar Lendário e Mítico também; a alimentação já usa "Raro ou acima" | economy.json hoje lista só Raro e Épico |

Ficam para o balanceamento depois da implementação (M21-T07 e T08), sem mudar nada agora: recompensas das Expedições, adversários da Arena e anúncios simulados do Mercado, que hoje são pensados até o Mapa 4.

## Ritmo esperado

Estimativa simples, sem isca e sem efeito de tamanho, contando só peixes puxados, 120 tentativas por hora online e o equipamento esperado em cada faixa. A simulação completa da seção 15 do original (M21-T07) confirma ou corrige.

| Faixa | Mapa | Moedas por tentativa | × anterior | XP por tentativa | × anterior | Horas online na faixa |
|---|---|---:|---:|---:|---:|---:|
| Nv.40–50 | Costa de Coral | 1.951 | 1,94 | 114,30 | 1,54 | 3,70 |
| Nv.50–60 | Arquipélago do Sol | 3.901 | 2,00 | 161,60 | 1,41 | 3,70 |
| Nv.60–70 | Corrente Azul | 8.706 | 2,23 | 235,50 | 1,46 | 3,30 |
| Nv.70–80 | Banco das Baleias | 17.215 | 1,98 | 315,70 | 1,34 | 3,10 |
| Nv.80–90 | Talude Noturno | 37.700 | 2,19 | 477,20 | 1,51 | 2,40 |
| Nv.90–100 | Abismo Atlântico | 79.686 | 2,11 | 685,20 | 1,44 | 2,00 |

As Moedas sobem cerca de 2× por mapa, o mesmo salto que o jogo já tem do Mapa 3 para o Mapa 4 (×2,1). O XP por tentativa sobe de 1,34× a 1,54× por mapa. Até o Nv.60 cada faixa leva o mesmo tempo da faixa 30–40 de hoje; depois fica mais rápida, até cerca de 2 h no 90–100. O motivo é o próprio desenho dos mapas finais: muito mais Raros, Épicos e Lendários no pool, e Lendário e Mítico dão ×9 e ×15 de XP. Para manter todas as faixas com o mesmo tempo, o XP dos peixes Comuns teria de cair nos últimos mapas (um Comum do Abismo valeria menos que um do Estuário). Por isso a escolha ficou com o proprietário (OD-047).

## Raridades novas

| Raridade | Chance-base de puxar | Atributos | XP do Pescador | Alimento | Venda | Cor |
|---|---:|---:|---:|---:|---:|---|
| Lendário | 14% | ×1,55 | ×9,00 | ×6,00 | ×8,00 | `#E5484D` |
| Mítico | 8% | ×1,85 | ×15,00 | ×10,00 | ×14,00 | `#EC4899` |

## Mapas, capítulos e frases de chegada

| Capítulo | Mapa | Nível | Vara mínima | Frase ao chegar |
|---:|---|---:|---|---|
| 5 | Costa de Coral | 40 | Maré Dourada | Finalmente cheguei ao mar aberto da costa. |
| 6 | Arquipélago do Sol | 50 | Corrente Mestra | Longe da costa, os peixes viram troféus. |
| 7 | Corrente Azul | 60 | Corrente Mestra | Só o horizonte e o mar azul em volta. |
| 8 | Banco das Baleias | 70 | Atlântico Nobre | Aqui o oceano mostra o seu tamanho. |
| 9 | Talude Noturno | 80 | Atlântico Nobre | A noite revela o que vive no fundo. |
| 10 | Abismo Atlântico | 90 | Soberana Abissal | O destino final de todo pescador. |

## Varas 3 a 5

| Vara | Libera | Compra | Chance de puxar Nv.1 → Nv.10 | Melhorias Nv.2 a 10 (total) | No original |
|---|---:|---|---|---|---|
| Corrente Mestra | Nv.50 | 730.000 Moedas + 85 Conchas | +8,00 → +16,00 p.p. | 7.820.000 Moedas + 442 Conchas | 450.000 Moedas; +12 → +24 p.p.; melhorias 4.410.000 Moedas |
| Atlântico Nobre | Nv.70 | 3.280.000 Moedas + 180 Conchas | +12,00 → +21,30 p.p. | 35.890.000 Moedas + 937 Conchas | 1.800.000 Moedas + 180 Conchas; +18 → +32 p.p.; melhorias 18.675.000 Moedas |
| Soberana Abissal | Nv.90 | 15.610.000 Moedas + 650 Conchas | +16,70 → +26,70 p.p. | 148.050.000 Moedas + 3380 Conchas | 8.000.000 Moedas + 650 Conchas; +25 → +40 p.p.; melhorias 74.250.000 Moedas |

Custo de cada melhoria (Moedas / Conchas):

| Vara | → Nv.2 | → Nv.3 | → Nv.4 | → Nv.5 | → Nv.6 | → Nv.7 | → Nv.8 | → Nv.9 | → Nv.10 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Corrente Mestra | 110.000 / 21 | 160.000 / 26 | 240.000 / 32 | 350.000 / 38 | 530.000 / 47 | 800.000 / 55 | 1.200.000 / 64 | 1.770.000 / 74 | 2.660.000 / 85 |
| Atlântico Nobre | 480.000 / 45 | 720.000 / 54 | 1.080.000 / 68 | 1.610.000 / 81 | 2.400.000 / 99 | 3.650.000 / 117 | 5.480.000 / 135 | 8.170.000 / 158 | 12.300.000 / 180 |
| Soberana Abissal | 1.990.000 / 162 | 2.990.000 / 195 | 4.490.000 / 244 | 6.780.000 / 292 | 10.170.000 / 358 | 15.150.000 / 422 | 22.730.000 / 488 | 33.900.000 / 569 | 49.850.000 / 650 |

Conchas: com a chance de 7%% por peixe puxado (A-099), juntar as Conchas da compra leva cerca de 5 h online para a Corrente Mestra, 8 h para a Atlântico Nobre e 24 h para a Soberana Abissal (os 650 do original foram mantidos). É uma estimativa; a ideia da A-099 é que os jogadores também negociem Conchas.

## As 60 espécies com os números adaptados

Tamanho, peso de captura, raridade e Velocidade são os do original. Venda, XP e atributos já adaptados.

### Costa de Coral (Nv.40)

| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `sargo` | Sargo | Comum | 20–55 | 325 | 43 | 34 | 100 | 1.600 | 407 | 106 | 220 |
| `salema` | Salema | Comum | 15–45 | 280 | 46 | 26 | 112 | 1.670 | 423 | 114 | 200 |
| `peixe_porco` | Peixe-porco | Comum | 20–60 | 400 | 40 | 41 | 88 | 1.810 | 456 | 122 | 180 |
| `pampo` | Pampo | Comum | 20–70 | 295 | 48 | 25 | 112 | 1.950 | 488 | 128 | 165 |
| `sargentinho` | Sargentinho | Comum | 10–25 | 305 | 50 | 24 | 112 | 2.020 | 504 | 136 | 145 |
| `ariaco` | Ariacó | Raro | 25–70 | 325 | 52 | 34 | 100 | 4.180 | 790 | 178 | 30 |
| `cioba` | Cioba | Raro | 30–100 | 315 | 66 | 28 | 105 | 4.500 | 833 | 190 | 25 |
| `badejo` | Badejo | Raro | 40–120 | 400 | 49 | 41 | 88 | 4.810 | 897 | 202 | 20 |
| `dentao` | Dentão | Épico | 50–140 | 401 | 66 | 42 | 105 | 8.280 | 1.330 | 265 | 8 |
| `caranha` | Caranha | Épico | 50–150 | 553 | 60 | 57 | 84 | 8.690 | 1.391 | 279 | 7 |

### Arquipélago do Sol (Nv.50)

| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `olhete` | Olhete | Comum | 30–100 | 325 | 52 | 32 | 113 | 2.580 | 521 | 128 | 210 |
| `peixe_galo` | Peixe-galo | Comum | 30–100 | 330 | 53 | 31 | 113 | 2.720 | 553 | 136 | 190 |
| `beijupira` | Beijupirá | Comum | 60–180 | 400 | 52 | 38 | 101 | 2.930 | 586 | 145 | 175 |
| `bonito_cachorro` | Bonito-cachorro | Comum | 40–100 | 350 | 56 | 30 | 113 | 3.140 | 618 | 154 | 155 |
| `serra` | Serra | Comum | 40–140 | 400 | 67 | 30 | 106 | 3.280 | 651 | 163 | 135 |
| `cavala_verdadeira` | Cavala-verdadeira | Raro | 60–170 | 325 | 63 | 32 | 113 | 6.590 | 1.025 | 220 | 55 |
| `olho_de_boi` | Olho-de-boi | Raro | 60–170 | 370 | 75 | 33 | 106 | 7.110 | 1.089 | 234 | 45 |
| `dourado_do_mar` | Dourado-do-mar | Épico | 60–200 | 413 | 65 | 49 | 113 | 11.450 | 1.572 | 305 | 16 |
| `atum_amarelo` | Atum-amarelo | Épico | 80–240 | 638 | 69 | 70 | 85 | 12.170 | 1.673 | 323 | 14 |
| `veleiro` | Veleiro | **Lendário** | 150–300 | 432 | 67 | 47 | 113 | 20.650 | 2.479 | 480 | 5 |

### Corrente Azul (Nv.60)

| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `bicuda` | Bicuda | Comum | 50–160 | 425 | 70 | 39 | 107 | 3.910 | 651 | 132 | 230 |
| `bonito_pintado` | Bonito-pintado | Comum | 40–100 | 390 | 61 | 36 | 114 | 4.180 | 700 | 141 | 210 |
| `agulha_branca` | Agulha-branca | Comum | 60–160 | 400 | 63 | 35 | 114 | 4.460 | 748 | 150 | 190 |
| `cavalinha` | Cavalinha | Comum | 20–45 | 410 | 65 | 34 | 114 | 4.740 | 797 | 158 | 170 |
| `atum_patudo` | Atum-patudo | Raro | 80–250 | 630 | 83 | 48 | 86 | 12.130 | 1.623 | 289 | 50 |
| `albacora_branca` | Albacora-branca | Raro | 70–140 | 380 | 73 | 37 | 114 | 9.620 | 1.281 | 228 | 45 |
| `peixe_lua` | Peixe-lua | Raro | 100–300 | 540 | 65 | 58 | 90 | 10.240 | 1.366 | 243 | 40 |
| `espadarte` | Espadarte | Épico | 150–450 | 541 | 86 | 60 | 107 | 16.770 | 1.975 | 312 | 30 |
| `marlim_branco` | Marlim-branco | Épico | 180–350 | 499 | 77 | 55 | 114 | 17.790 | 2.096 | 332 | 25 |
| `marlim_azul` | Marlim-azul | **Lendário** | 200–500 | 766 | 80 | 78 | 86 | 29.750 | 3.164 | 486 | 10 |

### Banco das Baleias (Nv.70)

| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `pargo_rosa` | Pargo-rosa | Comum | 30–90 | 530 | 65 | 52 | 100 | 5.860 | 830 | 137 | 200 |
| `namorado` | Namorado | Comum | 40–120 | 545 | 66 | 51 | 100 | 6.280 | 895 | 147 | 190 |
| `batata` | Batata | Comum | 30–100 | 660 | 61 | 64 | 88 | 6.700 | 944 | 156 | 175 |
| `congro_rosa` | Congro-rosa | Comum | 60–180 | 545 | 85 | 41 | 105 | 7.110 | 992 | 165 | 155 |
| `cherne` | Cherne | Raro | 60–200 | 755 | 94 | 55 | 84 | 17.770 | 2.050 | 301 | 90 |
| `cacao_anjo` | Cação-anjo | Raro | 80–180 | 630 | 71 | 67 | 88 | 14.010 | 1.623 | 237 | 80 |
| `tubarao_lixa` | Tubarão-lixa | Épico | 150–320 | 851 | 85 | 96 | 84 | 23.110 | 2.378 | 312 | 48 |
| `arraia_chita` | Arraia-chita | Épico | 100–250 | 578 | 84 | 65 | 112 | 24.540 | 2.519 | 332 | 42 |
| `tubarao_martelo` | Tubarão-martelo | **Lendário** | 180–450 | 663 | 100 | 66 | 105 | 41.200 | 3.850 | 485 | 11 |
| `raia_manta` | Raia-manta | **Lendário** | 250–700 | 918 | 91 | 89 | 84 | 43.560 | 4.071 | 513 | 9 |

### Talude Noturno (Nv.80)

| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `escolar` | Escolar | Comum | 70–220 | 605 | 90 | 51 | 103 | 8.580 | 1.025 | 142 | 230 |
| `peixe_espada` | Peixe-espada | Comum | 80–250 | 555 | 80 | 48 | 110 | 9.210 | 1.106 | 151 | 210 |
| `abrotea_fundo` | Abrótea-de-fundo | Comum | 40–120 | 670 | 78 | 58 | 98 | 9.760 | 1.171 | 161 | 180 |
| `congro_negro` | Congro-negro | Raro | 100–250 | 650 | 119 | 48 | 103 | 24.360 | 2.370 | 297 | 80 |
| `peixe_lanceta` | Peixe-lanceta | Raro | 100–220 | 600 | 105 | 44 | 110 | 25.720 | 2.519 | 313 | 75 |
| `quimera` | Quimera | Raro | 60–150 | 685 | 97 | 67 | 90 | 20.280 | 1.964 | 247 | 65 |
| `tubarao_lanterna` | Tubarão-lanterna | Épico | 40–90 | 857 | 97 | 105 | 90 | 32.720 | 2.943 | 325 | 65 |
| `peixe_opah` | Peixe-opah | Épico | 80–200 | 961 | 83 | 120 | 86 | 34.760 | 3.124 | 346 | 60 |
| `tubarao_duende` | Tubarão-duende | **Lendário** | 180–400 | 790 | 116 | 78 | 103 | 60.730 | 4.918 | 510 | 18 |
| `tubarao_seis_guelras` | Tubarão-seis-guelras | **Lendário** | 250–550 | 1094 | 104 | 102 | 82 | 64.110 | 5.200 | 538 | 17 |

### Abismo Atlântico (Nv.90)

| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `peixe_machado` | Peixe-machado | Comum | 8–18 | 820 | 92 | 76 | 88 | 12.280 | 1.285 | 146 | 190 |
| `peixe_dragao` | Peixe-dragão | Comum | 15–40 | 740 | 107 | 57 | 101 | 13.110 | 1.383 | 156 | 175 |
| `peixe_vibora` | Peixe-víbora | Comum | 20–35 | 760 | 110 | 55 | 101 | 13.950 | 1.464 | 166 | 155 |
| `enguia_pelicano` | Enguia-pelicano | Raro | 60–180 | 885 | 122 | 71 | 88 | 35.440 | 3.053 | 311 | 130 |
| `peixe_ogro` | Peixe-ogro | Raro | 10–25 | 800 | 140 | 53 | 101 | 37.420 | 3.224 | 329 | 120 |
| `peixe_pescador_abissal` | Peixe-pescador-abissal | Épico | 20–100 | 997 | 109 | 123 | 88 | 44.990 | 3.547 | 325 | 85 |
| `quimera_azul` | Quimera-azul | Épico | 70–180 | 1021 | 112 | 120 | 88 | 48.050 | 3.789 | 346 | 75 |
| `tubarao_cobra` | Tubarão-cobra | **Lendário** | 150–220 | 924 | 130 | 89 | 101 | 85.880 | 6.046 | 533 | 36 |
| `peixe_fita_gigante` | Peixe-fita-gigante | **Lendário** | 250–900 | 845 | 115 | 83 | 108 | 91.000 | 6.409 | 564 | 32 |
| `tubarao_boca_grande` | Tubarão-boca-grande | **Mítico** | 300–550 | 1307 | 121 | 117 | 81 | 206.120 | 10.843 | 872 | 2 |

## O que vem depois

A ordem de implementação é a da seção 16 do original (roadmap M21): raridades novas, espécies, mapas com cenário provisório, varas, proteções e filtros por hierarquia, integração e migração de save, simulação por mapa, ajuste fino, pedidos de arte e arte final mapa a mapa.
