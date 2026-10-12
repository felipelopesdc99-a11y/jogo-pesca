# Iscas: custo e retorno por estágio (M24-T30)

Gerado por `tools/Progressao/simular_iscas.py`. Valor esperado, 360 tentativas por hora online e 180 offline.

Custo por tentativa:

| Isca | Bônus | Moedas/tentativa | Conchas/tentativa |
|---|---|---|---|
| Terra Viva (hoje) | +5% | 23,02 | 0,0130 |
| Maré Viva (hoje) | +10% | 100,10 | 0,0330 |
| Ouro de Maré (hoje) | +15% | 200,20 | 0,0551 |
| Terra Viva (antes) | +3% | 8,33 | 0,0056 |
| Maré Viva (antes) | +7% | 33,33 | 0,0167 |
| Ouro de Maré (antes) | +10% | 83,33 | 0,0278 |

Por hora de pesca online. "Retorno" = Moedas a mais que sem isca ÷ Moedas da isca (acima de 1,00 a isca se paga em Moedas); "Conchas" = Conchas da isca ÷ Conchas que a pesca dá por hora.

| Estágio | Mapa · vara · barco | Isca | Peixes/h | Raros+/h | Moedas/h | Moedas a mais/h | Custo/h | Retorno | Conchas |
|---|---|---|---|---|---|---|---|---|---|
| começo | map_01 · rod_01 Nv.1 · boat_00 | sem isca | 185 | 0,0 | 10.571 | — | — | — | — |
| começo | map_01 · rod_01 Nv.1 · boat_00 | Terra Viva (hoje) +5% | 203 | 0,0 | 11.598 | 1.026 | 8.288 M + 4,7 C | 0,12 | 24% |
| começo | map_01 · rod_01 Nv.1 · boat_00 | Maré Viva (hoje) +10% | 221 | 0,0 | 12.624 | 2.053 | 36.036 M + 11,9 C | 0,06 | 61% |
| começo | map_01 · rod_01 Nv.1 · boat_00 | Ouro de Maré (hoje) +15% | 239 | 0,0 | 13.650 | 3.079 | 72.072 M + 19,8 C | 0,04 | 102% |
| começo | map_01 · rod_01 Nv.1 · boat_00 | Terra Viva (antes) +3% | 196 | 0,0 | 11.187 | 616 | 3.000 M + 2,0 C | 0,21 | 10% |
| começo | map_01 · rod_01 Nv.1 · boat_00 | Maré Viva (antes) +7% | 211 | 0,0 | 12.008 | 1.437 | 12.000 M + 6,0 C | 0,12 | 31% |
| começo | map_01 · rod_01 Nv.1 · boat_00 | Ouro de Maré (antes) +10% | 221 | 0,0 | 12.624 | 2.053 | 30.000 M + 10,0 C | 0,07 | 51% |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | sem isca | 197 | 0,4 | 85.262 | — | — | — | — |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | Terra Viva (hoje) +5% | 215 | 0,4 | 93.113 | 7.852 | 8.288 M + 4,7 C | 0,95 | 21% |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | Maré Viva (hoje) +10% | 233 | 0,5 | 100.965 | 15.703 | 36.036 M + 11,9 C | 0,44 | 53% |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | Ouro de Maré (hoje) +15% | 251 | 0,5 | 108.817 | 23.555 | 72.072 M + 19,8 C | 0,33 | 88% |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | Terra Viva (antes) +3% | 208 | 0,4 | 89.973 | 4.711 | 3.000 M + 2,0 C | 1,57 | 9% |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | Maré Viva (antes) +7% | 222 | 0,4 | 96.254 | 10.992 | 12.000 M + 6,0 C | 0,92 | 27% |
| começo | map_02 · rod_01 Nv.10 · boat_01 Nv.1 | Ouro de Maré (antes) +10% | 233 | 0,5 | 100.965 | 15.703 | 30.000 M + 10,0 C | 0,52 | 44% |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | sem isca | 280 | 8,7 | 626.207 | — | — | — | — |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | Terra Viva (hoje) +5% | 298 | 9,4 | 668.304 | 42.096 | 8.288 M + 4,7 C | 5,08 | 5% |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | Maré Viva (hoje) +10% | 316 | 10,0 | 710.400 | 84.192 | 36.036 M + 11,9 C | 2,34 | 13% |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | Ouro de Maré (hoje) +15% | 334 | 10,7 | 752.496 | 126.289 | 72.072 M + 19,8 C | 1,75 | 22% |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | Terra Viva (antes) +3% | 291 | 9,1 | 651.465 | 25.258 | 3.000 M + 2,0 C | 8,42 | 2% |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | Maré Viva (antes) +7% | 305 | 9,6 | 685.142 | 58.935 | 12.000 M + 6,0 C | 4,91 | 7% |
| meio | map_05 · rod_03 Nv.30 · boat_05 Nv.10 | Ouro de Maré (antes) +10% | 316 | 10,0 | 710.400 | 84.192 | 30.000 M + 10,0 C | 2,81 | 11% |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | sem isca | 317 | 22,8 | 2.338.264 | — | — | — | — |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | Terra Viva (hoje) +5% | 335 | 24,4 | 2.488.870 | 150.606 | 8.288 M + 4,7 C | 18,17 | 3% |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | Maré Viva (hoje) +10% | 338 | 26,0 | 2.571.266 | 233.003 | 36.036 M + 11,9 C | 6,47 | 9% |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | Ouro de Maré (hoje) +15% | 340 | 27,6 | 2.645.550 | 307.286 | 72.072 M + 19,8 C | 4,26 | 15% |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | Terra Viva (antes) +3% | 328 | 23,8 | 2.428.627 | 90.364 | 3.000 M + 2,0 C | 30,12 | 1% |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | Maré Viva (antes) +7% | 337 | 25,0 | 2.526.696 | 188.433 | 12.000 M + 6,0 C | 15,70 | 4% |
| meio | map_07 · rod_04 Nv.30 · boat_07 Nv.10 | Ouro de Maré (antes) +10% | 338 | 26,0 | 2.571.266 | 233.003 | 30.000 M + 10,0 C | 7,77 | 7% |
| fim | map_10 · rod_05 Nv.1 · boat_00 | sem isca | 229 | 26,0 | 7.381.664 | — | — | — | — |
| fim | map_10 · rod_05 Nv.1 · boat_00 | Terra Viva (hoje) +5% | 247 | 28,8 | 8.196.906 | 815.241 | 8.288 M + 4,7 C | 98,36 | 5% |
| fim | map_10 · rod_05 Nv.1 · boat_00 | Maré Viva (hoje) +10% | 265 | 31,6 | 9.012.147 | 1.630.482 | 36.036 M + 11,9 C | 45,25 | 14% |
| fim | map_10 · rod_05 Nv.1 · boat_00 | Ouro de Maré (hoje) +15% | 283 | 34,4 | 9.827.388 | 2.445.724 | 72.072 M + 19,8 C | 33,93 | 23% |
| fim | map_10 · rod_05 Nv.1 · boat_00 | Terra Viva (antes) +3% | 240 | 27,7 | 7.870.809 | 489.145 | 3.000 M + 2,0 C | 163,05 | 2% |
| fim | map_10 · rod_05 Nv.1 · boat_00 | Maré Viva (antes) +7% | 254 | 29,9 | 8.523.002 | 1.141.338 | 12.000 M + 6,0 C | 95,11 | 7% |
| fim | map_10 · rod_05 Nv.1 · boat_00 | Ouro de Maré (antes) +10% | 265 | 31,6 | 9.012.147 | 1.630.482 | 30.000 M + 10,0 C | 54,35 | 12% |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | sem isca | 342 | 66,7 | 20.096.599 | — | — | — | — |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | Terra Viva (hoje) +5% | 342 | 66,7 | 20.096.599 | 0 | 8.288 M + 4,7 C | 0,00 | 1% |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | Maré Viva (hoje) +10% | 342 | 66,7 | 20.096.599 | 0 | 36.036 M + 11,9 C | 0,00 | 3% |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | Ouro de Maré (hoje) +15% | 342 | 66,7 | 20.096.599 | 0 | 72.072 M + 19,8 C | 0,00 | 5% |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | Terra Viva (antes) +3% | 342 | 66,7 | 20.096.599 | 0 | 3.000 M + 2,0 C | 0,00 | 0% |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | Maré Viva (antes) +7% | 342 | 66,7 | 20.096.599 | 0 | 12.000 M + 6,0 C | 0,00 | 1% |
| fim | map_10 · rod_05 Nv.100 · boat_10 Nv.100 | Ouro de Maré (antes) +10% | 342 | 66,7 | 20.096.599 | 0 | 30.000 M + 10,0 C | 0,00 | 2% |

Resumo (retorno médio em Moedas das iscas de hoje):

- começo: Terra Viva 0,54; Maré Viva 0,25; Ouro de Maré 0,18
- meio: Terra Viva 11,62; Maré Viva 4,40; Ouro de Maré 3,01
- fim: Terra Viva 49,18; Maré Viva 22,62; Ouro de Maré 16,97

Um pacote de 999 tentativas dura 2,8 h de pesca online ou 5,5 h offline; um dia de 2 h online + 22 h offline (4.680 tentativas) gasta 4,7 pacotes.
