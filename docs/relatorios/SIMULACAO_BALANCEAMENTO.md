# Relatório de simulação do balanceamento

Gerado por `./ops/scripts/simular.sh` (ferramenta em `tools/Simulador`). Ele joga as **regras reais**
do jogo com um relógio simulado e mede os números atuais de `/config`. Não muda nenhum valor: serve
para decidir o balanceamento com dados. Rode de novo depois de editar o balanceamento.

- Versão do balanceamento: `07a87985e2`
- Jogadores simulados por medição: 5 (a tabela mostra a média)

## Pontos de atenção

Observações automáticas sobre os números atuais. São só fatos medidos: decidir se algo muda é do
proprietário (o balanceamento ainda não foi feito de propósito).

- Nível 10 (libera o segundo mapa) chega com 2,1 h de pesca online (4,1 h se fosse só offline).
- Nível 20 chega com 4,6 h de pesca online.
- Peixes acima de comum: 0,50% das capturas em Rio Selvagem · Vara 1 Nv.1 (um a cada ~200 capturas, ~1,7 h online).
- Ao chegar ao segundo mapa, as Moedas por hora sobem 8,1× (3.345 → 27.196).
- As batalhas duram em média 8 a 22 s, bem abaixo da meta de ~60 s.
- A Expedição que mais rende por hora, na Força recomendada, dá 333 Moedas/h — 10% do que a pesca rende no primeiro mapa (ela roda junto com a pesca).

## 1. Progressão do Pescador (pesca online, sem parar)

Estratégia simulada: pesca o tempo todo, vende tudo a cada 10 minutos, compra a próxima vara assim
que pode e viaja para o próximo mapa assim que ele libera. Offline, cada captura leva 1 minuto em vez de 30 segundos.

| Nível | Horas de pesca online | Moedas ganhas até ali |
|---:|---:|---:|
| 2 | 0,0 h | 0 |
| 3 | 0,1 h | 0 |
| 4 | 0,2 h | 281 |
| 5 | 0,3 h | 890 |
| 6 | 0,5 h | 1.571 |
| 7 | 0,8 h | 2.302 |
| 8 | 1,2 h | 3.563 |
| 9 | 1,6 h | 4.922 |
| 10 | 2,1 h | 6.531 |
| 11 | 2,3 h | 7.137 |
| 12 | 2,4 h | 12.710 |
| 13 | 2,6 h | 17.428 |
| 14 | 2,8 h | 22.825 |
| 15 | 3,1 h | 30.052 |
| 16 | 3,3 h | 37.990 |
| 17 | 3,6 h | 45.779 |
| 18 | 3,9 h | 54.012 |
| 19 | 4,2 h | 63.826 |
| 20 | 4,6 h | 73.283 |
| 30 | 10,1 h | 219.557 |
| 40 | 19,1 h | 458.715 |
| 50 | 32,2 h | 820.790 |
| 60 | 50,2 h | 1.303.693 |
| 70 | 73,0 h | 1.933.257 |
| 80 | 101,4 h | 2.716.177 |
| 90 | 136,0 h | 3.652.995 |

Até o Nível 20 aparecem todos os níveis; depois, de 10 em 10. A simulação para no nível máximo ou com 150 h.

- Capturas por hora online: 120
- Vara comprada (em média) com 2,1 h de pesca
- Viagem ao segundo mapa com 2,1 h de pesca

## 2. Frequência de raridade e de tamanho

200.000 sorteios reais (`CatchRules.Roll`) por combinação de mapa e vara.

| Mapa · vara | Comum | Raro | Pequeno | Adulto | Grande | Excepcional | Conchas por 100 capturas |
|---|---:|---:|---:|---:|---:|---:|---:|
| Lago Sereno · Vara Inicial | 100,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,02% | 0,0 |
| Lago Sereno · Vara 1 Nv.1 | 100,00% | 0,00% | 19,98% | 59,94% | 19,06% | 1,02% | 7,5 |
| Lago Sereno · Vara 1 Nv.10 | 100,00% | 0,00% | 19,16% | 57,53% | 22,14% | 1,18% | 11,5 |
| Rio Selvagem · Vara 1 Nv.1 | 99,50% | 0,50% | 19,98% | 59,94% | 19,06% | 1,02% | 7,5 |
| Rio Selvagem · Vara 1 Nv.10 | 99,39% | 0,61% | 19,16% | 57,53% | 22,14% | 1,18% | 11,5 |

## 3. Economia

- Moedas por hora vendendo tudo, primeiro mapa: 3.345
- Moedas por hora vendendo tudo, segundo mapa (com a vara comprada, Nv.1): 27.196
- Conchas por hora no segundo mapa: 8,9

| Vara | Preço | Todas as melhorias | Horas de pesca para pagar a vara | Horas para pagar as melhorias |
|---|---:|---:|---:|---:|
| Vara 1 | 2.500 | 138.050 | 0,7 h | 5,1 h |

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
| Cachara | Comum | 92 | 230 | 368 |
| Dourado | Comum | 120 | 300 | 480 |
| Pintado | Comum | 152 | 380 | 608 |
| Jaú | Comum | 208 | 520 | 832 |
| Pirarucu | Comum | 280 | 700 | 1.120 |
| Aruanã | Raro | 1.440 | 3.600 | 5.760 |

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

Para comparar: pescar rende 3.345 Moedas por hora no primeiro mapa (e a Expedição roda junto com a pesca).

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

