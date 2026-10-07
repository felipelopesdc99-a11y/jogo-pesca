# Simulação da progressão até o Nível 100

Gerado por `tools/Progressao/simular_progressao.py` em 07/10/2026, com o balanceamento atual de `/config`
(tabela de XP recalibrada por `tools/Progressao/calibrar_xp.py`, A-109). O simulador joga cada tentativa de
pesca pelo valor esperado das regras do jogo (espécie, chance de puxar, tamanho, XP, venda e Conchas), com
pesca online a cada 30 s e offline a cada 60 s (limite de 24 h), e compra varas, barcos, melhorias e iscas
como um jogador cuidadoso que vende tudo ao NPC. Não conta Mercado, Leilão nem Expedições.

## Dias até cada nível

| Jeito de jogar | Nv.10 | Nv.20 | Nv.30 | Nv.40 | Nv.50 | Nv.60 | Nv.70 | Nv.80 | Nv.90 | Nv.100 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Deixa o jogo aberto o dia todo (só online) | 2,2 h | 7,5 h | 16,2 h | 1,1 d | 1,5 d | 2,0 d | 2,6 d | 3,2 d | 4,1 d | 5,1 d |
| Abre 4 vezes por dia, 30 min de cada vez (2 h online; o resto offline) | 12,0 h | 22,0 h | 1,8 d | 2,5 d | 3,3 d | 4,3 d | 5,3 d | 6,5 d | 8,3 d | 10,3 d |
| Abre 2 vezes por dia, 1 h de cada vez (2 h online; o resto offline) | 20,0 h | 1,3 d | 1,8 d | 2,8 d | 3,8 d | 4,8 d | 5,8 d | 6,8 d | 8,8 d | 10,8 d |
| Abre 1 vez por dia, 30 min (o resto offline) | 1,8 d | 2,8 d | 2,8 d | 3,8 d | 4,8 d | 5,8 d | 6,8 d | 8,8 d | 10,8 d | 12,8 d |

## Quando cada equipamento é comprado

| Jeito de jogar | Vara 1 | Vara 2 | Vara 3 | Vara 4 | Vara 5 |
|---|---:|---:|---:|---:|---:|
| Deixa o jogo aberto o dia todo (só online) | 2,2 h | 16,2 h | 1,5 d | 2,6 d | 4,2 d |
| Abre 4 vezes por dia, 30 min de cada vez (2 h online; o resto offline) | 12,0 h | 1,8 d | 3,3 d | 5,3 d | 8,5 d |
| Abre 2 vezes por dia, 1 h de cada vez (2 h online; o resto offline) | 20,0 h | 1,8 d | 3,8 d | 5,8 d | 8,8 d |
| Abre 1 vez por dia, 30 min (o resto offline) | 1,8 d | 2,8 d | 4,8 d | 6,8 d | 10,8 d |

## Variações testadas (quem abre 4 vezes por dia)

- **Sem isca:** chega ao Nv.100 no mesmo dia; a isca acelera pouco.
- **Vendendo só metade ou um quarto dos peixes** (guardando raros para o Aquário): o Nv.100 atrasa no máximo meio dia; o dinheiro não trava a subida de nível.
- **Deixando o jogo aberto o dia todo:** cerca de metade do tempo, porque a pesca online rende o dobro da offline.

O tempo vem quase todo do XP: com as Moedas da venda, a vara de cada mapa sempre está paga antes ou logo depois de o nível liberar o mapa.

