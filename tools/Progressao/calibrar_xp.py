"""Calibra a tabela de XP do Pescador para o jogador chegar ao Nv.100 em cerca de N dias.

Uso:  python3 tools/Progressao/calibrar_xp.py [--dias 10] [--gravar]

Como funciona:
  1. Parte da curva original (18 × nível^1,5, de 5 em 5) e não mexe nos níveis 1 a 9 (Nv.10 em ~2 h online).
  2. Ajusta um multiplicador por faixa de 10 níveis até cada faixa durar o planejado no ritmo de quem abre
     o jogo 4 vezes por dia (2 h online + o resto offline), simulado por simular_progressao.py.
  3. Troca esses multiplicadores por uma curva suave (parábola no logaritmo) e acerta a escala para o
     total dar N dias. A tabela nunca pede menos XP que o nível anterior.
  4. Com --gravar, escreve a tabela em config/progression.json.

O XP dos peixes não muda: continua subindo de mapa para mapa. O que muda é quanto cada nível pede.
"""
import argparse
import json
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import simular_progressao as S  # noqa: E402

# Duração de cada faixa de 10 níveis, em dias, no ritmo contínuo de 4 visitas por dia (cresce até o fim).
BANDS = [0.5, 0.6, 0.7, 0.8, 0.9, 1.1, 1.3, 1.6, 2.0]


def original_curve(max_level):
    return [0] + [int(round(18.0 * l ** 1.5 / 5.0)) * 5 for l in range(1, max_level)]


def build(base, factor_of_level):
    out = base[:]
    for l in range(10, len(base)):
        out[l] = max(out[l - 1], int(round(base[l] * factor_of_level(l) / 5.0)) * 5)
    return out


def run(table, profile):
    r = S.Rules()
    r.xp_to_next = table
    return S.play(r, profile)


def calibrate(days):
    rules = S.Rules()
    base = original_curve(rules.max_level)
    first = S.bands(run(base, 'continuo'))[0]
    scale = (days - first) / sum(BANDS)
    target = [first]
    for b in BANDS:
        target.append(target[-1] + b * scale)
    g = [1.0] * 10
    centers = [5 + 10 * b for b in range(10)]

    def by_band(l):
        for b in range(9):
            if centers[b] <= l <= centers[b + 1]:
                t = (l - centers[b]) / 10.0
                return math.exp(math.log(g[b]) * (1 - t) + math.log(g[b + 1]) * t)
        return g[9]

    best = None
    for _ in range(200):
        got = S.bands(run(build(base, by_band), 'continuo'))
        dur = [(got[i] or 99) - ((got[i - 1] or 0) if i else 0) for i in range(10)]
        want = [target[i] - (target[i - 1] if i else 0) for i in range(10)]
        err = max(abs(dur[i] - want[i]) / want[i] for i in range(1, 10))
        if best is None or err < best[0]:
            best = (err, g[:])
        if err < 0.02:
            break
        for b in range(1, 10):
            g[b] *= (want[b] / dur[b]) ** 0.5
    g = best[1]
    levels = np.arange(10, rules.max_level)
    coef = np.polyfit(levels, np.log([by_band(l) for l in levels]), 2)
    lo, hi = -1.0, 1.0
    for _ in range(40):
        mid = (lo + hi) / 2
        total = S.bands(run(build(base, lambda l: math.exp(np.polyval(coef, l) + mid)), 'continuo'))[-1] or 999
        lo, hi = (mid, hi) if total < target[-1] else (lo, mid)
    shift = (lo + hi) / 2
    return build(base, lambda l: math.exp(np.polyval(coef, l) + shift))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dias', type=float, default=10.0)
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    # The continuous pace reaches the end a little before the 4-sessions pace (no wait for the first session).
    table = calibrate(a.dias - 0.33)
    print('Dias até cada nível com a tabela nova:')
    for prof in S.PROFILES:
        p = run(table, prof)
        print('  %-14s' % prof, ' '.join('%7s' % S.fmt(x) for x in S.bands(p)))
    print('XP total do Nv.1 ao Nv.100: %d' % sum(table[1:]))
    if a.gravar:
        path = os.path.join(S.CONFIG, 'progression.json')
        import collections
        with open(path, encoding='utf-8') as f:
            prog = json.load(f, object_pairs_hook=collections.OrderedDict)
        for entry in prog['fisher']['xp_table']:
            entry['xp_to_next_level'] = table[entry['level']]
        prog['fisher']['total_xp_level_1_to_100'] = sum(table[1:])
        with open(path, 'w', encoding='utf-8') as f:
            f.write(json.dumps(prog, ensure_ascii=False, indent=2) + '\n')
        print('Gravado em config/progression.json.')


if __name__ == '__main__':
    main()
