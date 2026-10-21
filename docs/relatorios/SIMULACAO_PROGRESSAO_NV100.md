# Simulação da progressão até o Nível 100

Gerado por `tools/Progressao/simular_progressao.py` em 07/10/2026, com o balanceamento atual de `/config`
(tabela de XP recalibrada por `tools/Progressao/calibrar_xp.py --dias 10 --perfil sempre_aberto`, A-109). O
simulador joga cada tentativa de pesca pelo valor esperado das regras do jogo (espécie, chance de puxar, tamanho,
XP, venda e Conchas), com pesca online a cada 30 s e offline a cada 60 s (limite de 24 h), e compra varas,
barcos, melhorias e iscas como um jogador cuidadoso que vende tudo ao NPC. Não conta Mercado, Leilão nem Expedições.

**Meta (decisão do proprietário, 07/10/2026):** quem deixa o jogo aberto o dia todo chega ao Nv.100 em ~10 dias;
quem joga menos leva mais.

## Dias até cada nível

| Jeito de jogar | Nv.10 | Nv.20 | Nv.30 | Nv.40 | Nv.50 | Nv.60 | Nv.70 | Nv.80 | Nv.90 | Nv.100 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Deixa o jogo aberto o dia todo (só online) | 2,2 h | 12,2 h | 1,2 d | 2,0 d | 2,9 d | 3,8 d | 4,9 d | 6,2 d | 7,9 d | 10,0 d |
| Abre 4 vezes por dia, 30 min de cada vez (2 h online; o resto offline) | 12,0 h | 1,3 d | 2,8 d | 4,3 d | 5,9 d | 7,8 d | 9,8 d | 12,3 d | 15,3 d | 19,3 d |
| Abre 2 vezes por dia, 1 h de cada vez (2 h online; o resto offline) | 20,0 h | 1,8 d | 2,9 d | 4,4 d | 6,3 d | 8,3 d | 10,3 d | 12,8 d | 16,3 d | 20,3 d |
| Abre 1 vez por dia, 30 min (o resto offline) | 1,8 d | 2,8 d | 4,8 d | 6,8 d | 7,9 d | 9,8 d | 12,8 d | 15,8 d | 18,8 d | 23,8 d |

## Quando cada equipamento é comprado

| Jeito de jogar | Vara 1 | Vara 2 | Vara 3 | Vara 4 | Vara 5 |
|---|---:|---:|---:|---:|---:|
| Deixa o jogo aberto o dia todo (só online) | 2,2 h | 1,2 d | 2,9 d | 4,9 d | 7,9 d |
| Abre 4 vezes por dia, 30 min de cada vez (2 h online; o resto offline) | 12,0 h | 2,8 d | 5,9 d | 9,8 d | 15,3 d |
| Abre 2 vezes por dia, 1 h de cada vez (2 h online; o resto offline) | 20,0 h | 2,9 d | 6,3 d | 10,3 d | 16,3 d |
| Abre 1 vez por dia, 30 min (o resto offline) | 1,8 d | 4,8 d | 7,9 d | 12,8 d | 18,8 d |

## Variações testadas

| Variação | Aberto o dia todo | 4 vezes por dia | 1 vez por dia |
|---|---:|---:|---:|
| Padrão (vende tudo, usa isca) | 10,0 d | 19,3 d | 23,8 d |
| Sem isca | 11,0 d | 20,9 d | 24,8 d |
| Vendendo só metade dos peixes | 10,1 d | 19,5 d | 23,8 d |
| Vendendo só um quarto dos peixes | 10,6 d | 20,5 d | 24,8 d |

O tempo vem quase todo do XP: com as Moedas da venda, a vara de cada mapa fica paga logo que o nível libera
o mapa. A pesca offline rende metade da online (um ciclo a cada 60 s contra 30 s), por isso quem joga poucas
vezes por dia leva cerca do dobro do tempo.

## Com o VIP (+50% de XP na pesca offline, A-110)

Rodado com `python3 tools/Progressao/simular_progressao.py --vip`, supondo o VIP ativo o tempo todo.

| Jeito de jogar | Nv.100 sem VIP | Nv.100 com VIP |
|---|---:|---:|
| Deixa o jogo aberto o dia todo | 10,0 d | 10,0 d |
| Abre 4 vezes por dia | 19,3 d | 14,9 d |
| Abre 2 vezes por dia | 20,3 d | 15,3 d |
| Abre 1 vez por dia | 23,8 d | 17,8 d |

O VIP só mexe na pesca offline, então quem deixa o jogo aberto não ganha nada com ele, e ninguém passa da
meta de 10 dias. As Moedas acompanham: com o VIP, a última vara continua comprada antes do Nv.100.
