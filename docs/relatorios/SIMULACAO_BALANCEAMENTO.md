# Relatório de simulação do balanceamento

Gerado por `./ops/scripts/simular.sh` (ferramenta em `tools/Simulador`). Ele joga as **regras reais**
do jogo com um relógio simulado e mede os números atuais de `/config`. Não muda nenhum valor: serve
para decidir o balanceamento com dados. Rode de novo depois de editar o balanceamento.

- Versão do balanceamento: `131a093466`
- Jogadores simulados por medição: 5 (a tabela mostra a média)

## Pontos de atenção

Observações automáticas sobre os números atuais. São só fatos medidos: decidir se algo muda é do
proprietário (o balanceamento ainda não foi feito de propósito).

- Nível 10 (libera o segundo mapa) chega com 2,1 h de pesca online (4,2 h se fosse só offline).
- Nível 20 chega com 4,6 h de pesca online.
- Do Nível 20 ao 30 (Pantanal Dourado): 3,8 h online (meta de docs/PROGRESSAO_MAPAS_3_4.md: ~3,8 h, com 100% de captura).
- Do Nível 30 ao 40 (Estuário das Marés): 4,2 h online (meta: ~4,0 h, com 100% de captura).
- Peixes acima de comum: 0,99% das mordidas em Rio Selvagem · Vara 1 Nv.1; com a Chance de Sucesso, um puxado a cada ~255 tentativas (~2,1 h online, sem barco nem isca).
- Peixes acima de comum: 1,22% das mordidas em Rio Selvagem · Vara 2 Nv.1; com a Chance de Sucesso, um puxado a cada ~189 tentativas (~1,6 h online, sem barco nem isca).
- Peixes acima de comum: 4,83% das mordidas em Pantanal Dourado · Vara 1 Nv.1; com a Chance de Sucesso, um puxado a cada ~52 tentativas (~0,4 h online, sem barco nem isca).
- Peixes acima de comum: 5,92% das mordidas em Pantanal Dourado · Vara 2 Nv.1; com a Chance de Sucesso, um puxado a cada ~39 tentativas (~0,3 h online, sem barco nem isca).
- Peixes acima de comum: 8,08% das mordidas em Estuário das Marés · Vara 2 Nv.1; com a Chance de Sucesso, um puxado a cada ~28 tentativas (~0,2 h online, sem barco nem isca).
- Ao chegar ao segundo mapa, as Moedas por hora sobem 8,4× (1.649 → 13.783).
- As batalhas duram em média 8 a 22 s, bem abaixo da meta de ~60 s.
- A Expedição que mais rende por hora, na Força recomendada, dá 333 Moedas/h — 20% do que a pesca rende no primeiro mapa (ela roda junto com a pesca).
- Sucesso da Captura: 45 peixes escapam por hora. Se toda mordida virasse captura, o Nível 10 chegaria com 1,0 h em vez de 2,1 h.

## 1. Progressão do Pescador (pesca online, sem parar)

Estratégia simulada: pesca o tempo todo, vende tudo a cada 10 minutos, compra a próxima vara assim
que pode, depois os barcos, e viaja para o próximo mapa assim que ele libera. Não usa isca. Offline, cada captura leva 1 minuto em vez de 30 segundos.

| Nível | Horas de pesca online | Moedas ganhas até ali |
|---:|---:|---:|
| 2 | 0,0 h | 0 |
| 3 | 0,1 h | 0 |
| 4 | 0,2 h | 75 |
| 5 | 0,4 h | 388 |
| 6 | 0,6 h | 680 |
| 7 | 0,8 h | 1.137 |
| 8 | 1,2 h | 1.719 |
| 9 | 1,6 h | 2.425 |
| 10 | 2,1 h | 3.404 |
| 11 | 2,3 h | 3.630 |
| 12 | 2,5 h | 5.965 |
| 13 | 2,6 h | 9.297 |
| 14 | 2,8 h | 11.782 |
| 15 | 3,1 h | 15.895 |
| 16 | 3,3 h | 18.584 |
| 17 | 3,6 h | 21.692 |
| 18 | 3,9 h | 25.813 |
| 19 | 4,3 h | 30.519 |
| 20 | 4,6 h | 34.628 |
| 30 | 8,4 h | 136.943 |
| 40 | 12,6 h | 352.981 |
| 50 | 18,6 h | 697.483 |
| 60 | 26,9 h | 1.177.762 |
| 70 | 37,5 h | 1.808.131 |
| 80 | 50,4 h | 2.574.248 |
| 90 | 65,9 h | 3.498.119 |
| 100 | 84,3 h | 4.602.165 |

Até o Nível 20 aparecem todos os níveis; depois, de 10 em 10. A simulação para no nível máximo ou com 150 h.

- Capturas por hora online: 75
- Vara 1 com 2,2 h de pesca
- Vara 2 com 9,2 h de pesca

| Mapa | Chegada | XP por hora | Moedas por hora (vendendo tudo) | Conchas por hora |
|---|---:|---:|---:|---:|
| Lago Sereno | 0,0 h | 958 | 1.649 | 0,0 |
| Rio Selvagem | 2,2 h | 4.236 | 13.783 | 9,6 |
| Pantanal Dourado | 4,6 h | 5.469 | 26.434 | 10,3 |
| Estuário das Marés | 9,2 h | 8.879 | 59.130 | 17,8 |

- Barco 1 com 2,4 h de pesca
- Barco 2 com 3,5 h de pesca
- Barco 3 com 6,0 h de pesca
- Barco 4 com 14,2 h de pesca
- Barco 5 com 34,3 h de pesca

## 2. Frequência de raridade e de tamanho

200.000 sorteios reais (`CatchRules.Roll`) por combinação de mapa e vara: o que morde, antes da Chance de Sucesso (seção 7).

| Mapa · vara | Comum | Raro | Épico | Pequeno | Adulto | Grande | Excepcional | Perfeição | Conchas por 100 capturas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Vara Inicial | 100,00% | 0,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,01% | 0,01% | 0,0 |
| Lago Sereno · Vara 1 Nv.1 | 100,00% | 0,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,01% | 0,01% | 15,1 |
| Lago Sereno · Vara 1 Nv.10 | 100,00% | 0,00% | 0,00% | 19,16% | 57,53% | 22,14% | 1,17% | 0,01% | 22,9 |
| Lago Sereno · Vara 2 Nv.1 | 100,00% | 0,00% | 0,00% | 19,07% | 57,31% | 22,43% | 1,19% | 0,01% | 23,4 |
| Lago Sereno · Vara 2 Nv.10 | 100,00% | 0,00% | 0,00% | 18,40% | 55,29% | 24,99% | 1,31% | 0,01% | 31,5 |
| Rio Selvagem · Vara 1 Nv.1 | 99,01% | 0,99% | 0,00% | 19,98% | 59,96% | 19,04% | 1,01% | 0,01% | 15,1 |
| Rio Selvagem · Vara 1 Nv.10 | 98,80% | 1,20% | 0,00% | 19,16% | 57,56% | 22,10% | 1,16% | 0,01% | 22,9 |
| Rio Selvagem · Vara 2 Nv.1 | 98,78% | 1,22% | 0,00% | 19,08% | 57,34% | 22,40% | 1,18% | 0,01% | 23,4 |
| Rio Selvagem · Vara 2 Nv.10 | 98,52% | 1,48% | 0,00% | 18,41% | 55,33% | 24,95% | 1,30% | 0,01% | 31,5 |
| Pantanal Dourado · Vara 1 Nv.1 | 95,17% | 4,53% | 0,30% | 20,01% | 60,06% | 18,93% | 0,99% | 0,01% | 15,1 |
| Pantanal Dourado · Vara 1 Nv.10 | 94,17% | 5,47% | 0,36% | 19,20% | 57,67% | 21,97% | 1,15% | 0,01% | 22,9 |
| Pantanal Dourado · Vara 2 Nv.1 | 94,08% | 5,56% | 0,37% | 19,11% | 57,46% | 22,26% | 1,17% | 0,01% | 23,4 |
| Pantanal Dourado · Vara 2 Nv.10 | 92,92% | 6,65% | 0,44% | 18,46% | 55,47% | 24,77% | 1,29% | 0,01% | 31,5 |
| Estuário das Marés · Vara 2 Nv.1 | 91,92% | 7,34% | 0,74% | 19,13% | 57,51% | 22,19% | 1,16% | 0,01% | 23,4 |
| Estuário das Marés · Vara 2 Nv.10 | 90,37% | 8,76% | 0,87% | 18,48% | 55,54% | 24,69% | 1,28% | 0,01% | 31,5 |

## 3. Economia

- Moedas por hora vendendo tudo, primeiro mapa: 1.649
- Moedas por hora vendendo tudo, segundo mapa (com a vara comprada, Nv.1): 13.783
- Conchas por hora no segundo mapa: 9,6

| Vara | Preço | Todas as melhorias | Horas de pesca para pagar a vara | Horas para pagar as melhorias |
|---|---:|---:|---:|---:|
| Vara 1 | 2.500 | 138.050 | 1,5 h | 10,0 h |
| Vara 2 | 90.000 | 765.000 | 54,5 h | 55,5 h |

Preço de venda ao NPC por espécie (tamanho mínimo, médio e máximo da espécie):

| Espécie | Raridade | Mínimo | Médio | Máximo |
|---|---|---:|---:|---:|
| Lambari | Comum | 3 | 8 | 13 |
| Tilápia | Comum | 7 | 18 | 29 |
| Piau | Comum | 9 | 22 | 35 |
| Cascudo | Comum | 10 | 26 | 42 |
| Curimbatá | Comum | 12 | 30 | 48 |
| Traíra | Comum | 15 | 38 | 61 |
| Pacu | Comum | 18 | 45 | 72 |
| Matrinxã | Comum | 24 | 60 | 96 |
| Carpa | Comum | 30 | 75 | 120 |
| Piranha | Comum | 36 | 90 | 144 |
| Tambaqui | Comum | 48 | 120 | 192 |
| Piracanjuba | Comum | 48 | 120 | 192 |
| Peixe-cachorra | Comum | 60 | 150 | 240 |
| Tucunaré | Comum | 76 | 190 | 304 |
| Mandi-amarelo | Comum | 80 | 201 | 322 |
| Cachara | Comum | 92 | 230 | 368 |
| Pacu-peva | Comum | 98 | 245 | 392 |
| Dourado | Comum | 120 | 300 | 480 |
| Jundiá | Comum | 122 | 306 | 490 |
| Jurupensém | Comum | 147 | 368 | 588 |
| Pintado | Comum | 152 | 380 | 608 |
| Muçum | Comum | 168 | 420 | 672 |
| Parati | Comum | 170 | 426 | 682 |
| Piavuçu | Comum | 196 | 490 | 784 |
| Tainha | Comum | 202 | 504 | 806 |
| Jaú | Comum | 208 | 520 | 832 |
| Carapeba | Comum | 232 | 581 | 930 |
| Corvina | Comum | 279 | 698 | 1.116 |
| Pirarucu | Comum | 280 | 700 | 1.120 |
| Bagre-marinho | Comum | 326 | 814 | 1.302 |
| Piraputanga | Raro | 630 | 1.575 | 2.520 |
| Jurupoca | Raro | 770 | 1.925 | 3.080 |
| Armado | Raro | 910 | 2.275 | 3.640 |
| Robalo-peva | Raro | 1.054 | 2.635 | 4.216 |
| Pescada-amarela | Raro | 1.178 | 2.945 | 4.712 |
| Xaréu | Raro | 1.364 | 3.410 | 5.456 |
| Aruanã | Raro | 1.440 | 3.600 | 5.760 |
| Barbado | Épico | 2.800 | 7.000 | 11.200 |
| Camurupim | Épico | 4.340 | 10.850 | 17.360 |
| Mero | Épico | 4.712 | 11.780 | 18.848 |

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

Para comparar: pescar rende 1.649 Moedas por hora no primeiro mapa (e a Expedição roda junto com a pesca).

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
- Preço final médio: 1,34× a referência (mín. 1,00×, máx. 1,40×)

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
| Nível 30 | 4,4 h | 8,4 h |
| Nível 40 | 6,7 h | 12,6 h |
| Capturas por hora | 120 | 75 |
| Escapes por hora | 0 | 45 |
| Moedas por hora, primeiro mapa | 3.246 | 1.649 |
| Moedas por hora, segundo mapa | 28.215 | 13.783 |
| Conchas por hora, segundo mapa | 14,6 | 9,6 |
| Primeiro peixe Raro | 2,0 h | 4,1 h |
| Vara 1 comprada | 1,1 h | 2,2 h |

Combinações de equipamento, 10.000 tentativas cada (`CatchSimulator`, a mesma regra do jogo). A isca fica
sempre ligada; o custo dela por hora já está descontado em "Moedas/h líquidas".

| Mapa · vara · barco · isca | Taxa real | Comum puxados (chance) | Raro puxados (chance) | Épico puxados (chance) | Capturas/h | Escapes/h | XP/h | Moedas/h | Moedas/h líquidas | Conchas/h |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Vara Inicial · Barco Inicial · sem isca | 49,4% | 4.944 (50%) | — | — | 59 | 61 | 963 | 1.655 | 1.655 | 0,0 |
| Lago Sereno · Vara Inicial · Barco 1 · Isca Simples | 54,3% | 5.434 (55%) | — | — | 65 | 55 | 1.066 | 1.840 | 640 | 0,0 |
| Lago Sereno · Vara 1 Nv.1 · Barco Inicial · sem isca | 51,0% | 5.104 (52%) | — | — | 61 | 59 | 997 | 1.714 | 1.714 | 9,2 |
| Lago Sereno · Vara 1 Nv.10 · Barco Inicial · sem isca | 57,6% | 5.764 (58%) | — | — | 69 | 51 | 1.129 | 1.977 | 1.977 | 15,3 |
| Lago Sereno · Vara 1 Nv.10 · Barco 1 · Isca Simples | 62,9% | 6.289 (63%) | — | — | 75 | 45 | 1.239 | 2.172 | 972 | 16,4 |
| Lago Sereno · Vara 1 Nv.10 · Barco 5 · Isca Premium | 77,8% | 7.783 (78%) | — | — | 93 | 27 | 1.522 | 2.659 | -2.140 | 6,6 |
| Lago Sereno · Vara 2 Nv.1 · Barco Inicial · sem isca | 54,9% | 5.493 (56%) | — | — | 66 | 54 | 1.079 | 1.890 | 1.890 | 14,7 |
| Lago Sereno · Vara 2 Nv.10 · Barco Inicial · sem isca | 63,4% | 6.335 (64%) | — | — | 76 | 44 | 1.250 | 2.231 | 2.231 | 23,4 |
| Lago Sereno · Vara 2 Nv.10 · Barco 1 · Isca Simples | 68,4% | 6.842 (69%) | — | — | 82 | 38 | 1.353 | 2.423 | 1.223 | 25,3 |
| Lago Sereno · Vara 2 Nv.10 · Barco 5 · Isca Premium | 83,4% | 8.337 (84%) | — | — | 100 | 20 | 1.632 | 2.911 | -1.888 | 17,3 |
| Rio Selvagem · Vara 1 Nv.1 · Barco Inicial · sem isca | 51,0% | 5.064 (52%) | 37 (40%) | — | 61 | 59 | 4.207 | 14.169 | 14.169 | 9,2 |
| Rio Selvagem · Vara 1 Nv.10 · Barco Inicial · sem isca | 57,5% | 5.708 (58%) | 46 (46%) | — | 69 | 51 | 4.798 | 16.457 | 16.457 | 15,4 |
| Rio Selvagem · Vara 1 Nv.10 · Barco 1 · Isca Simples | 62,8% | 6.230 (63%) | 51 (51%) | — | 75 | 45 | 5.264 | 18.197 | 16.997 | 16,4 |
| Rio Selvagem · Vara 1 Nv.10 · Barco 5 · Isca Premium | 77,7% | 7.704 (78%) | 61 (66%) | — | 93 | 27 | 6.449 | 22.060 | 17.260 | 6,6 |
| Rio Selvagem · Vara 2 Nv.1 · Barco Inicial · sem isca | 54,8% | 5.432 (56%) | 49 (44%) | — | 66 | 54 | 4.617 | 15.913 | 15.913 | 14,7 |
| Rio Selvagem · Vara 2 Nv.10 · Barco Inicial · sem isca | 63,3% | 6.260 (64%) | 67 (52%) | — | 76 | 44 | 5.442 | 19.300 | 19.300 | 23,3 |
| Rio Selvagem · Vara 2 Nv.10 · Barco 1 · Isca Simples | 68,4% | 6.760 (69%) | 76 (57%) | — | 82 | 38 | 5.908 | 20.989 | 19.789 | 25,3 |
| Rio Selvagem · Vara 2 Nv.10 · Barco 5 · Isca Premium | 83,2% | 8.236 (84%) | 85 (72%) | — | 100 | 20 | 7.089 | 25.077 | 20.277 | 17,3 |
| Pantanal Dourado · Vara 1 Nv.1 · Barco Inicial · sem isca | 50,5% | 4.880 (52%) | 157 (40%) | 8 (26%) | 61 | 59 | 4.828 | 22.513 | 22.513 | 9,1 |
| Pantanal Dourado · Vara 1 Nv.10 · Barco Inicial · sem isca | 56,8% | 5.438 (58%) | 230 (46%) | 11 (32%) | 68 | 52 | 5.595 | 26.969 | 26.969 | 15,3 |
| Pantanal Dourado · Vara 1 Nv.10 · Barco 1 · Isca Simples | 62,2% | 5.954 (63%) | 249 (51%) | 15 (37%) | 75 | 45 | 6.155 | 29.768 | 28.568 | 16,4 |
| Pantanal Dourado · Vara 1 Nv.10 · Barco 5 · Isca Premium | 76,9% | 7.341 (78%) | 330 (66%) | 15 (52%) | 92 | 28 | 7.618 | 36.836 | 32.036 | 6,4 |
| Pantanal Dourado · Vara 2 Nv.1 · Barco Inicial · sem isca | 54,0% | 5.174 (56%) | 217 (44%) | 12 (30%) | 65 | 55 | 5.333 | 25.856 | 25.856 | 14,5 |
| Pantanal Dourado · Vara 2 Nv.10 · Barco Inicial · sem isca | 62,5% | 5.916 (64%) | 318 (52%) | 16 (38%) | 75 | 45 | 6.380 | 31.781 | 31.781 | 23,1 |
| Pantanal Dourado · Vara 2 Nv.10 · Barco 1 · Isca Simples | 67,6% | 6.399 (69%) | 339 (57%) | 17 (43%) | 81 | 39 | 6.885 | 34.369 | 33.169 | 25,0 |
| Pantanal Dourado · Vara 2 Nv.10 · Barco 5 · Isca Premium | 82,4% | 7.771 (84%) | 446 (72%) | 18 (58%) | 99 | 21 | 8.438 | 42.253 | 37.453 | 17,2 |
| Estuário das Marés · Vara 2 Nv.1 · Barco Inicial · sem isca | 53,7% | 5.062 (56%) | 295 (44%) | 17 (30%) | 64 | 56 | 7.309 | 48.301 | 48.301 | 14,3 |
| Estuário das Marés · Vara 2 Nv.10 · Barco Inicial · sem isca | 62,0% | 5.721 (64%) | 447 (52%) | 36 (38%) | 74 | 46 | 9.008 | 61.867 | 61.867 | 23,1 |
| Estuário das Marés · Vara 2 Nv.10 · Barco 1 · Isca Simples | 67,1% | 6.184 (69%) | 486 (57%) | 37 (43%) | 80 | 40 | 9.726 | 66.887 | 65.687 | 24,8 |
| Estuário das Marés · Vara 2 Nv.10 · Barco 5 · Isca Premium | 82,2% | 7.525 (84%) | 654 (72%) | 36 (58%) | 99 | 21 | 11.980 | 82.036 | 77.236 | 17,3 |

Preço dos barcos em horas de pesca no segundo mapa (Moedas e Conchas por hora da seção 1):

| Barco | Bônus | Custo | Nível | Horas de pesca | Conchas |
|---|---:|---:|---:|---:|---:|
| Barco 1 | +2% | 3.000 Moedas + 0 Conchas | 5 | 0,2 h | — |
| Barco 2 | +4% | 15.000 Moedas + 0 Conchas | 12 | 1,1 h | — |
| Barco 3 | +6% | 45.000 Moedas + 30 Conchas | 20 | 3,3 h | 3,1 h |
| Barco 4 | +8% | 130.000 Moedas + 120 Conchas | 30 | 9,4 h | 12,5 h |
| Barco 5 | +10% | 350.000 Moedas + 350 Conchas | 40 | 25,4 h | 36,4 h |

