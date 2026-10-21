# Relatório de simulação do balanceamento

Gerado por `./ops/scripts/simular.sh` (ferramenta em `tools/Simulador`). Ele joga as **regras reais**
do jogo com um relógio simulado e mede os números atuais de `/config`. Não muda nenhum valor: serve
para decidir o balanceamento com dados. Rode de novo depois de editar o balanceamento.

- Versão do balanceamento: `1e3e5a5520`
- Jogadores simulados por medição: 5 (a tabela mostra a média)

## Pontos de atenção

Observações automáticas sobre os números atuais. São só fatos medidos: decidir se algo muda é do
proprietário (o balanceamento ainda não foi feito de propósito).

- Nível 10 (libera o segundo mapa) chega com 2,1 h de pesca online (4,2 h se fosse só offline).
- Nível 20 chega com 4,6 h de pesca online.
- Do Nível 20 ao 30 (Pantanal Dourado): 3,9 h online (meta de docs/PROGRESSAO_MAPAS_3_4.md: ~3,8 h, com 100% de captura).
- Do Nível 30 ao 40 (Estuário das Marés): 4,5 h online (meta: ~4,0 h, com 100% de captura).
- Peixes acima de comum: 0,99% das mordidas em Rio Selvagem · Ponta Selvagem Nv.1; com a Chance de Sucesso, um puxado a cada ~255 tentativas (~2,1 h online, sem barco nem isca).
- Peixes acima de comum: 1,22% das mordidas em Rio Selvagem · Maré Dourada Nv.1; com a Chance de Sucesso, um puxado a cada ~189 tentativas (~1,6 h online, sem barco nem isca).
- Peixes acima de comum: 4,83% das mordidas em Pantanal Dourado · Ponta Selvagem Nv.1; com a Chance de Sucesso, um puxado a cada ~52 tentativas (~0,4 h online, sem barco nem isca).
- Peixes acima de comum: 5,92% das mordidas em Pantanal Dourado · Maré Dourada Nv.1; com a Chance de Sucesso, um puxado a cada ~39 tentativas (~0,3 h online, sem barco nem isca).
- Peixes acima de comum: 8,08% das mordidas em Estuário das Marés · Maré Dourada Nv.1; com a Chance de Sucesso, um puxado a cada ~28 tentativas (~0,2 h online, sem barco nem isca).
- Ao chegar ao segundo mapa, as Moedas por hora sobem 8,3× (3.298 → 27.321).
- As batalhas duram em média 8 a 22 s, bem abaixo da meta de ~60 s.
- A Expedição que mais rende por hora, na Força recomendada, dá 333 Moedas/h — 10% do que a pesca rende no primeiro mapa (ela roda junto com a pesca).
- Sucesso da Captura: 46 peixes escapam por hora. Se toda mordida virasse captura, o Nível 10 chegaria com 1,0 h em vez de 2,1 h.

## 1. Progressão do Pescador (pesca online, sem parar)

Estratégia simulada: pesca o tempo todo, vende tudo a cada 10 minutos, compra a próxima vara assim
que pode, depois os barcos, e viaja para o próximo mapa assim que ele libera. Não usa isca. Offline, cada captura leva 1 minuto em vez de 30 segundos.

| Nível | Horas de pesca online | Moedas ganhas até ali |
|---:|---:|---:|
| 2 | 0,0 h | 0 |
| 3 | 0,1 h | 0 |
| 4 | 0,2 h | 149 |
| 5 | 0,4 h | 774 |
| 6 | 0,6 h | 1.359 |
| 7 | 0,8 h | 2.273 |
| 8 | 1,2 h | 3.441 |
| 9 | 1,6 h | 4.851 |
| 10 | 2,1 h | 6.807 |
| 11 | 2,3 h | 7.257 |
| 12 | 2,5 h | 11.929 |
| 13 | 2,6 h | 18.546 |
| 14 | 2,8 h | 23.516 |
| 15 | 3,1 h | 31.743 |
| 16 | 3,3 h | 37.120 |
| 17 | 3,6 h | 44.397 |
| 18 | 3,9 h | 51.277 |
| 19 | 4,3 h | 61.167 |
| 20 | 4,6 h | 68.486 |
| 30 | 8,5 h | 270.410 |
| 40 | 13,0 h | 695.412 |
| 50 | 19,1 h | 1.380.773 |
| 60 | 27,5 h | 2.342.490 |
| 70 | 38,1 h | 3.610.107 |
| 80 | 51,5 h | 5.138.818 |
| 90 | 67,5 h | 6.975.476 |
| 100 | 85,8 h | 9.186.792 |

Até o Nível 20 aparecem todos os níveis; depois, de 10 em 10. A simulação para no nível máximo ou com 150 h.

- Capturas por hora online: 74
- Ponta Selvagem com 2,2 h de pesca
- Maré Dourada com 9,8 h de pesca

| Mapa | Chegada | XP por hora | Moedas por hora (vendendo tudo) | Conchas por hora |
|---|---:|---:|---:|---:|
| Lago Sereno | 0,0 h | 958 | 3.298 | 6,8 |
| Rio Selvagem | 2,2 h | 4.176 | 27.321 | 6,3 |
| Pantanal Dourado | 4,7 h | 5.327 | 51.095 | 7,5 |
| Estuário das Marés | 9,8 h | 8.744 | 116.398 | 12,2 |

- Remo Valente com 2,5 h de pesca
- Rastro Azul com 4,7 h de pesca
- Proa Selvagem com 11,4 h de pesca
- Costa Nobre com 22,7 h de pesca
- Horizonte Dourado com 63,3 h de pesca

## 2. Frequência de raridade e de tamanho

200.000 sorteios reais (`CatchRules.Roll`) por combinação de mapa e vara: o que morde, antes da Chance de Sucesso (seção 7).

| Mapa · vara | Comum | Raro | Épico | Pequeno | Adulto | Grande | Excepcional | Perfeição | Conchas por 100 capturas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Caniço Manso | 100,00% | 0,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,01% | 0,01% | 10,5 |
| Lago Sereno · Ponta Selvagem Nv.1 | 100,00% | 0,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,01% | 0,01% | 10,5 |
| Lago Sereno · Ponta Selvagem Nv.10 | 100,00% | 0,00% | 0,00% | 19,16% | 57,53% | 22,14% | 1,17% | 0,01% | 16,1 |
| Lago Sereno · Maré Dourada Nv.1 | 100,00% | 0,00% | 0,00% | 19,07% | 57,31% | 22,43% | 1,19% | 0,01% | 16,4 |
| Lago Sereno · Maré Dourada Nv.10 | 100,00% | 0,00% | 0,00% | 18,40% | 55,29% | 24,99% | 1,31% | 0,01% | 22,2 |
| Rio Selvagem · Ponta Selvagem Nv.1 | 99,01% | 0,99% | 0,00% | 19,98% | 59,96% | 19,04% | 1,01% | 0,01% | 10,5 |
| Rio Selvagem · Ponta Selvagem Nv.10 | 98,80% | 1,20% | 0,00% | 19,16% | 57,56% | 22,10% | 1,16% | 0,01% | 16,1 |
| Rio Selvagem · Maré Dourada Nv.1 | 98,78% | 1,22% | 0,00% | 19,08% | 57,34% | 22,40% | 1,18% | 0,01% | 16,4 |
| Rio Selvagem · Maré Dourada Nv.10 | 98,52% | 1,48% | 0,00% | 18,41% | 55,33% | 24,95% | 1,30% | 0,01% | 22,2 |
| Pantanal Dourado · Ponta Selvagem Nv.1 | 95,17% | 4,53% | 0,30% | 20,01% | 60,06% | 18,93% | 0,99% | 0,01% | 10,5 |
| Pantanal Dourado · Ponta Selvagem Nv.10 | 94,17% | 5,47% | 0,36% | 19,20% | 57,67% | 21,97% | 1,15% | 0,01% | 16,1 |
| Pantanal Dourado · Maré Dourada Nv.1 | 94,08% | 5,56% | 0,37% | 19,11% | 57,46% | 22,26% | 1,17% | 0,01% | 16,4 |
| Pantanal Dourado · Maré Dourada Nv.10 | 92,92% | 6,65% | 0,44% | 18,46% | 55,47% | 24,77% | 1,29% | 0,01% | 22,2 |
| Estuário das Marés · Maré Dourada Nv.1 | 91,92% | 7,34% | 0,74% | 19,13% | 57,51% | 22,19% | 1,16% | 0,01% | 16,4 |
| Estuário das Marés · Maré Dourada Nv.10 | 90,37% | 8,76% | 0,87% | 18,48% | 55,54% | 24,69% | 1,28% | 0,01% | 22,2 |

## 3. Economia

- Moedas por hora vendendo tudo, primeiro mapa: 3.298
- Moedas por hora vendendo tudo, segundo mapa (com a vara comprada, Nv.1): 27.321
- Conchas por hora no segundo mapa: 6,3

| Vara | Preço | Todas as melhorias | Horas de pesca para pagar a vara | Horas para pagar as melhorias |
|---|---:|---:|---:|---:|
| Ponta Selvagem | 2.500 | 138.050 | 0,8 h | 5,1 h |
| Maré Dourada | 90.000 | 765.000 | 27,3 h | 28,0 h |

Preço de venda ao NPC por espécie (tamanho mínimo, médio e máximo da espécie):

| Espécie | Raridade | Mínimo | Médio | Máximo |
|---|---|---:|---:|---:|
| Lambari | Comum | 6 | 16 | 26 |
| Tilápia | Comum | 14 | 36 | 58 |
| Piau | Comum | 18 | 44 | 70 |
| Cascudo | Comum | 21 | 52 | 83 |
| Curimbatá | Comum | 24 | 60 | 96 |
| Traíra | Comum | 30 | 76 | 122 |
| Pacu | Comum | 36 | 90 | 144 |
| Matrinxã | Comum | 48 | 120 | 192 |
| Carpa | Comum | 60 | 150 | 240 |
| Piranha | Comum | 72 | 180 | 288 |
| Tambaqui | Comum | 96 | 240 | 384 |
| Piracanjuba | Comum | 96 | 240 | 384 |
| Peixe-cachorra | Comum | 120 | 300 | 480 |
| Tucunaré | Comum | 152 | 380 | 608 |
| Mandi-amarelo | Comum | 161 | 402 | 643 |
| Cachara | Comum | 184 | 460 | 736 |
| Pacu-peva | Comum | 196 | 490 | 784 |
| Dourado | Comum | 240 | 600 | 960 |
| Jundiá | Comum | 245 | 612 | 979 |
| Jurupensém | Comum | 294 | 735 | 1.176 |
| Pintado | Comum | 304 | 760 | 1.216 |
| Muçum | Comum | 336 | 840 | 1.344 |
| Parati | Comum | 341 | 852 | 1.363 |
| Piavuçu | Comum | 392 | 980 | 1.568 |
| Tainha | Comum | 403 | 1.008 | 1.613 |
| Jaú | Comum | 416 | 1.040 | 1.664 |
| Carapeba | Comum | 465 | 1.162 | 1.859 |
| Corvina | Comum | 558 | 1.395 | 2.232 |
| Pirarucu | Comum | 560 | 1.400 | 2.240 |
| Bagre-marinho | Comum | 651 | 1.628 | 2.605 |
| Piraputanga | Raro | 1.260 | 3.150 | 5.040 |
| Jurupoca | Raro | 1.540 | 3.850 | 6.160 |
| Armado | Raro | 1.820 | 4.550 | 7.280 |
| Robalo-peva | Raro | 2.108 | 5.270 | 8.432 |
| Pescada-amarela | Raro | 2.356 | 5.890 | 9.424 |
| Xaréu | Raro | 2.728 | 6.820 | 10.912 |
| Aruanã | Raro | 2.880 | 7.200 | 11.520 |
| Barbado | Épico | 5.600 | 14.000 | 22.400 |
| Camurupim | Épico | 8.680 | 21.700 | 34.720 |
| Mero | Épico | 9.424 | 23.560 | 37.696 |

## 4. Combate (adversários simulados da Arena)

Cada linha: 500 batalhas do adversário de uma posição contra um ~10% acima dele (como um ataque
normal). Meta do GDD: a maioria das lutas em até ~60 s.

| Posição do atacante | Alvo | Força do atacante | Força do alvo | Duração média | 90% das lutas até | Acima da meta | Atacante vence |
|---:|---:|---:|---:|---:|---:|---:|---:|
| #2 | #1 | 1.254 | 1.307 | 15,8 s | 16,3 s | 0% | 0% |
| #5 | #4 | 1.332 | 1.293 | 15,3 s | 16,0 s | 0% | 100% |
| #10 | #9 | 1.452 | 1.291 | 11,2 s | 11,2 s | 0% | 100% |
| #25 | #22 | 1.373 | 1.219 | 13,9 s | 14,0 s | 0% | 100% |
| #50 | #45 | 952 | 1.052 | 12,5 s | 12,5 s | 0% | 0% |
| #100 | #90 | 524 | 731 | 8,5 s | 8,5 s | 0% | 0% |
| #150 | #135 | 311 | 338 | 14,2 s | 14,2 s | 0% | 0% |
| #200 | #180 | 202 | 199 | 22,4 s | 22,4 s | 0% | 99% |

## 5. Expedições

Força de referência: Cardumes dos adversários simulados em algumas posições da Arena.

| Expedição | Duração | Força recomendada | Moedas na recomendada | Moedas por hora | Moedas c/ Cardume do #200 | Moedas c/ Cardume do #150 | Moedas c/ Cardume do #100 | Moedas c/ Cardume do #50 | Moedas c/ Cardume do #1 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Saída Rápida | 30 minutos | 250 | 120 | 240 | 101 | 130 | 166 | 180 | 180 |
| Volta na Margem | 1 h 00 min | 450 | 260 | 260 | 137 | 193 | 275 | 362 | 390 |
| Águas Profundas | 3 h 00 min | 800 | 900 | 300 | 299 | 423 | 642 | 960 | 1.100 |
| Viagem Longa | 6 h 00 min | 1.400 | 2.000 | 333 | 500 | 600 | 911 | 1.469 | 1.893 |

Para comparar: pescar rende 3.298 Moedas por hora no primeiro mapa (e a Expedição roda junto com a pesca).

## 6. Mercado e Leilão (jogadores simulados)

Tempo médio até um comprador simulado levar um anúncio, pelo preço em relação à referência (taxa de 3% na venda):

| Preço | Chance por verificação | Tempo médio até vender |
|---:|---:|---:|
| 0,5× | 38% | 53 minutos e 20 s |
| 1,0× | 25% | 1 h 20 min |
| 1,5× | 21% | 1 h 36 min |
| 2,0× | 17% | 2 h 00 min |
| 3,0× | 8% | 4 h 00 min |
| 3,9× | 1% | 40 h 00 min |

Leilões do jogador (30 leilões reais de 6 h, lance inicial = preço de venda ao NPC):

- Terminaram sem lance: 0 de 30
- Preço final médio: 1,31× a referência (mín. 0,94×, máx. 1,40×)

## 7. Sucesso da Captura: quanto ela muda o jogo

A primeira coluna é o jogo de hoje se toda mordida virasse captura, com os mesmos números de `/config`;
a segunda é o jogo de verdade, com a Chance de Sucesso (docs/SISTEMA_SUCESSO_PESCA.md). Mesma estratégia
da seção 1. O intervalo da pesca não muda. A comparação com a versão de antes do rebalanceamento está
no CHANGELOG (0.2.0-m14.19).

| Medida | Se toda mordida virasse captura | Com a Chance de Sucesso |
|---|---:|---:|
| Nível 5 | 0,2 h | 0,4 h |
| Nível 10 | 1,0 h | 2,1 h |
| Nível 15 | 1,6 h | 3,1 h |
| Nível 20 | 2,3 h | 4,6 h |
| Nível 30 | 4,4 h | 8,5 h |
| Nível 40 | 7,0 h | 13,0 h |
| Capturas por hora | 120 | 74 |
| Escapes por hora | 0 | 46 |
| Moedas por hora, primeiro mapa | 6.491 | 3.298 |
| Moedas por hora, segundo mapa | 56.428 | 27.321 |
| Conchas por hora, segundo mapa | 11,0 | 6,3 |
| Primeiro peixe Raro | 2,0 h | 4,1 h |
| Vara 1 comprada | 1,1 h | 2,2 h |

Combinações de equipamento, 10.000 tentativas cada (`CatchSimulator`, a mesma regra do jogo). A isca fica
sempre ligada; o custo dela por hora já está descontado em "Moedas/h líquidas".

| Mapa · vara · barco · isca | Taxa real | Comum puxados (chance) | Raro puxados (chance) | Épico puxados (chance) | Capturas/h | Escapes/h | XP/h | Moedas/h | Moedas/h líquidas | Conchas/h |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Caniço Manso · Água Mansa · sem isca | 49,4% | 4.944 (50%) | — | — | 59 | 61 | 963 | 3.311 | 3.311 | 6,6 |
| Lago Sereno · Caniço Manso · Remo Valente · Terra Viva | 54,3% | 5.434 (55%) | — | — | 65 | 55 | 1.066 | 3.680 | 680 | 5,1 |
| Lago Sereno · Ponta Selvagem Nv.1 · Água Mansa · sem isca | 51,0% | 5.104 (52%) | — | — | 61 | 59 | 997 | 3.428 | 3.428 | 6,9 |
| Lago Sereno · Ponta Selvagem Nv.10 · Água Mansa · sem isca | 57,6% | 5.764 (58%) | — | — | 69 | 51 | 1.129 | 3.953 | 3.953 | 11,0 |
| Lago Sereno · Ponta Selvagem Nv.10 · Remo Valente · Terra Viva | 62,9% | 6.289 (63%) | — | — | 75 | 45 | 1.239 | 4.345 | 1.345 | 9,9 |
| Lago Sereno · Ponta Selvagem Nv.10 · Horizonte Dourado · Ouro de Maré | 77,8% | 7.783 (78%) | — | — | 93 | 27 | 1.522 | 5.319 | -24.680 | 4,7 |
| Lago Sereno · Maré Dourada Nv.1 · Água Mansa · sem isca | 54,9% | 5.493 (56%) | — | — | 66 | 54 | 1.079 | 3.781 | 3.781 | 10,4 |
| Lago Sereno · Maré Dourada Nv.10 · Água Mansa · sem isca | 63,4% | 6.335 (64%) | — | — | 76 | 44 | 1.250 | 4.463 | 4.463 | 16,0 |
| Lago Sereno · Maré Dourada Nv.10 · Remo Valente · Terra Viva | 68,4% | 6.842 (69%) | — | — | 82 | 38 | 1.353 | 4.847 | 1.847 | 15,1 |
| Lago Sereno · Maré Dourada Nv.10 · Horizonte Dourado · Ouro de Maré | 83,4% | 8.337 (84%) | — | — | 100 | 20 | 1.632 | 5.823 | -24.176 | 11,9 |
| Rio Selvagem · Ponta Selvagem Nv.1 · Água Mansa · sem isca | 51,0% | 5.064 (52%) | 37 (40%) | — | 61 | 59 | 4.207 | 28.337 | 28.337 | 7,0 |
| Rio Selvagem · Ponta Selvagem Nv.10 · Água Mansa · sem isca | 57,5% | 5.708 (58%) | 46 (46%) | — | 69 | 51 | 4.798 | 32.913 | 32.913 | 11,0 |
| Rio Selvagem · Ponta Selvagem Nv.10 · Remo Valente · Terra Viva | 62,8% | 6.230 (63%) | 51 (51%) | — | 75 | 45 | 5.264 | 36.393 | 33.393 | 9,9 |
| Rio Selvagem · Ponta Selvagem Nv.10 · Horizonte Dourado · Ouro de Maré | 77,7% | 7.704 (78%) | 61 (66%) | — | 93 | 27 | 6.449 | 44.120 | 14.120 | 4,7 |
| Rio Selvagem · Maré Dourada Nv.1 · Água Mansa · sem isca | 54,8% | 5.432 (56%) | 49 (44%) | — | 66 | 54 | 4.617 | 31.825 | 31.825 | 10,5 |
| Rio Selvagem · Maré Dourada Nv.10 · Água Mansa · sem isca | 63,3% | 6.260 (64%) | 67 (52%) | — | 76 | 44 | 5.442 | 38.600 | 38.600 | 15,9 |
| Rio Selvagem · Maré Dourada Nv.10 · Remo Valente · Terra Viva | 68,4% | 6.760 (69%) | 76 (57%) | — | 82 | 38 | 5.908 | 41.977 | 38.977 | 15,1 |
| Rio Selvagem · Maré Dourada Nv.10 · Horizonte Dourado · Ouro de Maré | 83,2% | 8.236 (84%) | 85 (72%) | — | 100 | 20 | 7.089 | 50.155 | 20.155 | 11,9 |
| Pantanal Dourado · Ponta Selvagem Nv.1 · Água Mansa · sem isca | 50,5% | 4.880 (52%) | 157 (40%) | 8 (26%) | 61 | 59 | 4.828 | 45.025 | 45.025 | 6,8 |
| Pantanal Dourado · Ponta Selvagem Nv.10 · Água Mansa · sem isca | 56,8% | 5.438 (58%) | 230 (46%) | 11 (32%) | 68 | 52 | 5.595 | 53.937 | 53.937 | 10,9 |
| Pantanal Dourado · Ponta Selvagem Nv.10 · Remo Valente · Terra Viva | 62,2% | 5.954 (63%) | 249 (51%) | 15 (37%) | 75 | 45 | 6.155 | 59.535 | 56.535 | 9,9 |
| Pantanal Dourado · Ponta Selvagem Nv.10 · Horizonte Dourado · Ouro de Maré | 76,9% | 7.341 (78%) | 330 (66%) | 15 (52%) | 92 | 28 | 7.618 | 73.671 | 43.671 | 4,7 |
| Pantanal Dourado · Maré Dourada Nv.1 · Água Mansa · sem isca | 54,0% | 5.174 (56%) | 217 (44%) | 12 (30%) | 65 | 55 | 5.333 | 51.711 | 51.711 | 10,2 |
| Pantanal Dourado · Maré Dourada Nv.10 · Água Mansa · sem isca | 62,5% | 5.916 (64%) | 318 (52%) | 16 (38%) | 75 | 45 | 6.380 | 63.561 | 63.561 | 15,8 |
| Pantanal Dourado · Maré Dourada Nv.10 · Remo Valente · Terra Viva | 67,6% | 6.399 (69%) | 339 (57%) | 17 (43%) | 81 | 39 | 6.885 | 68.737 | 65.737 | 14,8 |
| Pantanal Dourado · Maré Dourada Nv.10 · Horizonte Dourado · Ouro de Maré | 82,4% | 7.771 (84%) | 446 (72%) | 18 (58%) | 99 | 21 | 8.438 | 84.504 | 54.504 | 11,7 |
| Estuário das Marés · Maré Dourada Nv.1 · Água Mansa · sem isca | 53,7% | 5.062 (56%) | 295 (44%) | 17 (30%) | 64 | 56 | 7.309 | 96.602 | 96.602 | 10,1 |
| Estuário das Marés · Maré Dourada Nv.10 · Água Mansa · sem isca | 62,0% | 5.721 (64%) | 447 (52%) | 36 (38%) | 74 | 46 | 9.008 | 123.733 | 123.733 | 15,9 |
| Estuário das Marés · Maré Dourada Nv.10 · Remo Valente · Terra Viva | 67,1% | 6.184 (69%) | 486 (57%) | 37 (43%) | 80 | 40 | 9.726 | 133.773 | 130.773 | 14,7 |
| Estuário das Marés · Maré Dourada Nv.10 · Horizonte Dourado · Ouro de Maré | 82,2% | 7.525 (84%) | 654 (72%) | 36 (58%) | 99 | 21 | 11.980 | 164.072 | 134.072 | 11,7 |

Preço dos barcos em horas de pesca no segundo mapa (Moedas e Conchas por hora da seção 1):

| Barco | Bônus | Custo | Nível | Horas de pesca | Conchas |
|---|---:|---:|---:|---:|---:|
| Remo Valente | +2% | 12.000 Moedas + 5 Conchas | 10 | 0,4 h | 0,8 h |
| Rastro Azul | +4% | 45.000 Moedas + 15 Conchas | 20 | 1,6 h | 2,4 h |
| Proa Selvagem | +6% | 160.000 Moedas + 25 Conchas | 30 | 5,9 h | 4,0 h |
| Costa Nobre | +8% | 600.000 Moedas + 120 Conchas | 50 | 22,0 h | 19,0 h |
| Horizonte Dourado | +10% | 2.500.000 Moedas + 500 Conchas | 80 | 91,5 h | 79,1 h |

