# Relatório de simulação do balanceamento

Gerado por `./ops/scripts/simular.sh` (ferramenta em `tools/Simulador`). Ele joga as **regras reais**
do jogo com um relógio simulado e mede os números atuais de `/config`. Não muda nenhum valor: serve
para decidir o balanceamento com dados. Rode de novo depois de editar o balanceamento.

- Versão do balanceamento: `54fa2523be`
- Jogadores simulados por medição: 5 (a tabela mostra a média)

## Pontos de atenção

Observações automáticas sobre os números atuais. São só fatos medidos: decidir se algo muda é do
proprietário (o balanceamento ainda não foi feito de propósito).

- Nível 10 (libera o segundo mapa) chega com 2,1 h de pesca online (4,2 h se fosse só offline).
- Nível 20 chega com 4,4 h de pesca online.
- Peixes acima de comum: 0,99% das mordidas em Rio Selvagem · Vara 1 Nv.1; com a Chance de Sucesso, um puxado a cada ~252 tentativas (~2,1 h online, sem barco nem isca).
- Ao chegar ao segundo mapa, as Moedas por hora sobem 11,4× (3.298 → 37.481).
- As batalhas duram em média 8 a 22 s, bem abaixo da meta de ~60 s.
- A Expedição que mais rende por hora, na Força recomendada, dá 333 Moedas/h — 10% do que a pesca rende no primeiro mapa (ela roda junto com a pesca).
- Sucesso da Captura: 42 peixes escapam por hora. Se toda mordida virasse captura, o Nível 10 chegaria com 1,0 h em vez de 2,1 h.

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
| 12 | 2,5 h | 11.273 |
| 13 | 2,6 h | 18.487 |
| 14 | 2,8 h | 24.104 |
| 15 | 3,0 h | 31.084 |
| 16 | 3,2 h | 34.964 |
| 17 | 3,4 h | 44.086 |
| 18 | 3,7 h | 52.191 |
| 19 | 4,0 h | 60.907 |
| 20 | 4,4 h | 70.285 |
| 30 | 8,8 h | 215.357 |
| 40 | 15,7 h | 462.802 |
| 50 | 25,8 h | 823.009 |
| 60 | 39,1 h | 1.319.507 |
| 70 | 56,2 h | 1.946.287 |
| 80 | 76,6 h | 2.743.739 |
| 90 | 101,0 h | 3.698.894 |
| 100 | 130,5 h | 4.815.050 |

Até o Nível 20 aparecem todos os níveis; depois, de 10 em 10. A simulação para no nível máximo ou com 150 h.

- Capturas por hora online: 78
- Vara comprada (em média) com 2,2 h de pesca
- Viagem ao segundo mapa com 2,2 h de pesca
- Barco 1 com 2,2 h de pesca
- Barco 2 com 2,7 h de pesca
- Barco 3 com 5,5 h de pesca
- Barco 4 com 17,0 h de pesca
- Barco 5 com 47,6 h de pesca

## 2. Frequência de raridade e de tamanho

200.000 sorteios reais (`CatchRules.Roll`) por combinação de mapa e vara: o que morde, antes da Chance de Sucesso (seção 7).

| Mapa · vara | Comum | Raro | Pequeno | Adulto | Grande | Excepcional | Perfeição | Conchas por 100 capturas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Vara Inicial | 100,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,01% | 0,01% | 0,0 |
| Lago Sereno · Vara 1 Nv.1 | 100,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,01% | 0,01% | 15,1 |
| Lago Sereno · Vara 1 Nv.10 | 100,00% | 0,00% | 19,16% | 57,53% | 22,14% | 1,17% | 0,01% | 22,9 |
| Rio Selvagem · Vara 1 Nv.1 | 99,01% | 0,99% | 19,98% | 59,96% | 19,04% | 1,01% | 0,01% | 15,1 |
| Rio Selvagem · Vara 1 Nv.10 | 98,80% | 1,20% | 19,16% | 57,56% | 22,10% | 1,16% | 0,01% | 22,9 |

## 3. Economia

- Moedas por hora vendendo tudo, primeiro mapa: 3.298
- Moedas por hora vendendo tudo, segundo mapa (com a vara comprada, Nv.1): 37.481
- Conchas por hora no segundo mapa: 7,6

| Vara | Preço | Todas as melhorias | Horas de pesca para pagar a vara | Horas para pagar as melhorias |
|---|---:|---:|---:|---:|
| Vara 1 | 2.500 | 138.050 | 0,8 h | 3,7 h |

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
| Cachara | Comum | 184 | 460 | 736 |
| Dourado | Comum | 240 | 600 | 960 |
| Pintado | Comum | 304 | 760 | 1.216 |
| Jaú | Comum | 416 | 1.040 | 1.664 |
| Pirarucu | Comum | 560 | 1.400 | 2.240 |
| Aruanã | Raro | 2.880 | 7.200 | 11.520 |

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
| Nível 15 | 1,6 h | 3,0 h |
| Nível 20 | 2,3 h | 4,4 h |
| Nível 30 | 5,0 h | 8,8 h |
| Capturas por hora | 120 | 78 |
| Escapes por hora | 0 | 42 |
| Moedas por hora, primeiro mapa | 6.491 | 3.298 |
| Moedas por hora, segundo mapa | 58.528 | 37.481 |
| Conchas por hora, segundo mapa | 11,9 | 7,6 |
| Primeiro peixe Raro | 1,9 h | 3,9 h |
| Vara 1 comprada | 1,1 h | 2,2 h |

Combinações de equipamento, 10.000 tentativas cada (`CatchSimulator`, a mesma regra do jogo). A isca fica
sempre ligada; o custo dela por hora já está descontado em "Moedas/h líquidas".

| Mapa · vara · barco · isca | Taxa real | Comuns (chance) | Raros (chance) | Capturas/h | Escapes/h | XP/h | Moedas/h | Moedas/h líquidas | Conchas/h |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Vara Inicial · Barco Inicial · sem isca | 49,4% | 4.944 (50%) | — | 59 | 61 | 963 | 3.311 | 3.311 | 0,0 |
| Lago Sereno · Vara Inicial · Barco 1 · Isca Simples | 57,6% | 5.764 (58%) | — | 69 | 51 | 1.128 | 3.897 | 3.657 | 0,0 |
| Lago Sereno · Vara 1 Nv.1 · Barco Inicial · sem isca | 51,6% | 5.159 (52%) | — | 62 | 58 | 1.009 | 3.476 | 3.476 | 9,2 |
| Lago Sereno · Vara 1 Nv.10 · Barco Inicial · sem isca | 62,0% | 6.200 (62%) | — | 74 | 46 | 1.222 | 4.282 | 4.282 | 16,3 |
| Lago Sereno · Vara 1 Nv.10 · Barco 1 · Isca Simples | 70,0% | 6.998 (70%) | — | 84 | 36 | 1.376 | 4.828 | 4.588 | 18,5 |
| Lago Sereno · Vara 1 Nv.10 · Barco 5 · Isca Premium | 92,3% | 9.227 (92%) | — | 111 | 9 | 1.820 | 6.398 | 5.438 | 18,3 |
| Rio Selvagem · Vara 1 Nv.1 · Barco Inicial · sem isca | 51,6% | 5.119 (52%) | 37 (40%) | 62 | 58 | 4.255 | 28.695 | 28.695 | 9,3 |
| Rio Selvagem · Vara 1 Nv.10 · Barco Inicial · sem isca | 62,0% | 6.145 (62%) | 50 (50%) | 74 | 46 | 5.193 | 35.728 | 35.728 | 16,3 |
| Rio Selvagem · Vara 1 Nv.10 · Barco 1 · Isca Simples | 69,9% | 6.935 (70%) | 56 (58%) | 84 | 36 | 5.837 | 40.193 | 39.953 | 18,5 |
| Rio Selvagem · Vara 1 Nv.10 · Barco 5 · Isca Premium | 92,0% | 9.122 (92%) | 80 (80%) | 110 | 10 | 7.745 | 53.440 | 52.480 | 18,1 |

Preço dos barcos em horas de pesca no segundo mapa (Moedas e Conchas por hora da seção 1):

| Barco | Bônus | Custo | Nível | Horas de pesca | Conchas |
|---|---:|---:|---:|---:|---:|
| Barco 1 | +3% | 3.000 Moedas + 0 Conchas | 5 | 0,1 h | — |
| Barco 2 | +6% | 15.000 Moedas + 0 Conchas | 12 | 0,4 h | — |
| Barco 3 | +9% | 45.000 Moedas + 30 Conchas | 20 | 1,2 h | 4,0 h |
| Barco 4 | +12% | 130.000 Moedas + 120 Conchas | 30 | 3,5 h | 15,9 h |
| Barco 5 | +15% | 350.000 Moedas + 350 Conchas | 40 | 9,3 h | 46,3 h |

