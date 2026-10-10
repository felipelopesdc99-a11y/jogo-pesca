"""Calibra a tabela de XP do Pescador (Nv.1 ao nível máximo, hoje Nv.1000) para metas de tempo.

Uso:  python3 tools/Progressao/calibrar_xp.py [--max 1000] [--metas 10:1h,100:1d,500:17.5d,1000:75d] [--gravar]

Como funciona (M24-T03, A-153):
  1. As metas dizem em quanto tempo quem deixa o jogo aberto o dia todo chega a cada nível (padrão: Nv.10 em
     ~1 h, Nv.100 em ~1 dia, Nv.500 em ~17,5 dias e Nv.1000 em ~75 dias). Entre as metas, o tempo segue uma curva
     suave (logaritmo do tempo contra o logaritmo do nível, interpolação monotônica). Antes do Nv.10, o formato
     da curva original (18 × nível^1,5) é mantido, só mais rápido.
  2. Simula o jogador (simular_progressao.py) e mede quanto XP ele ganha por tentativa em cada nível (o mapa, a
     vara, o barco e a isca que ele tem naquele momento). O XP de cada nível = tempo planejado para aquele nível
     × tentativas por dia × XP por tentativa medido. Repete até o tempo simulado bater com o planejado.
  3. Suaviza a tabela (a troca de mapa não vira um degrau) e arredonda: de 5 em 5 até 1.000 e com 3 algarismos
     significativos acima disso. A tabela nunca pede menos XP que o nível anterior.
  4. Com --gravar, escreve a tabela, o nível máximo e os totais em config/progression.json.

O XP dos peixes não muda: continua subindo de mapa para mapa. O que muda é quanto cada nível pede.
"""
import argparse
import collections
import json
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import simular_progressao as S  # noqa: E402

PROFILE = 'sempre_aberto'
DEFAULT_TARGETS = '10:1h,100:1d,500:17.5d,1000:75d'
REPORT_LEVELS = (1, 10, 50, 100, 250, 500, 750, 1000)


def parse_targets(text):
    out = []
    for part in text.split(','):
        level, when = part.split(':')
        when = when.strip().lower().replace(',', '.')
        days = float(when[:-1]) / 24.0 if when.endswith('h') else float(when.rstrip('d'))
        out.append((int(level), days))
    return sorted(out)


def pchip(xs, ys):
    """Monotone cubic interpolation (Fritsch-Carlson) through the points."""
    n = len(xs)
    h = [xs[i + 1] - xs[i] for i in range(n - 1)]
    d = [(ys[i + 1] - ys[i]) / h[i] for i in range(n - 1)]
    m = [d[0]] + [0.0] * (n - 2) + [d[-1]]
    for i in range(1, n - 1):
        if d[i - 1] * d[i] > 0:
            w1, w2 = 2 * h[i] + h[i - 1], h[i] + 2 * h[i - 1]
            m[i] = (w1 + w2) / (w1 / d[i - 1] + w2 / d[i])

    def f(x):
        i = max(0, min(n - 2, int(np.searchsorted(xs, x) - 1)))
        t = (x - xs[i]) / h[i]
        h00, h10, h01, h11 = 2 * t ** 3 - 3 * t ** 2 + 1, t ** 3 - 2 * t ** 2 + t, -2 * t ** 3 + 3 * t ** 2, t ** 3 - t ** 2
        return h00 * ys[i] + h10 * h[i] * m[i] + h01 * ys[i + 1] + h11 * h[i] * m[i + 1]
    return f


def planned_days(targets, max_level):
    """Planned day each level is reached, index = level (1..max)."""
    first_level, first_days = targets[0]
    curve = pchip([math.log(l) for l, _ in targets], [math.log(d) for _, d in targets])
    original = [0.0] + [18.0 * l ** 1.5 for l in range(1, first_level)]
    cum = np.cumsum([0.0] + original[1:])   # cum[k] = XP from Nv.1 to Nv.(k+1)
    days = [0.0] * (max_level + 1)
    for level in range(2, first_level):
        days[level] = first_days * cum[level - 1] / cum[first_level - 1]
    for level in range(first_level, max_level + 1):
        days[level] = math.exp(curve(math.log(level)))
    return days


def run(table, profile=PROFILE):
    r = S.Rules()
    r.max_level = len(table)
    r.xp_to_next = table
    return S.play(r, profile, max_days=400)


def rounded(x):
    if x < 1000:
        return max(5, int(round(x / 5.0)) * 5)
    digits = int(math.floor(math.log10(x))) - 2
    step = 10 ** digits
    return int(round(x / step)) * step


def smooth(values, first_level, window):
    """Moving average of the logarithm, from first_level on (the earlier levels keep their shape)."""
    logs = np.log(np.maximum(values, 1.0))
    out = values[:]
    for level in range(first_level, len(values)):
        lo, hi = max(first_level, level - window), min(len(values) - 1, level + window)
        out[level] = math.exp(float(np.mean(logs[lo:hi + 1])))
    return out


def finish(raw, first_level, window):
    table = [0] + [rounded(x) for x in smooth(raw, first_level, window)[1:]]
    for level in range(2, len(table)):
        table[level] = max(table[level], table[level - 1])
    return table


def calibrate(targets, max_level, window=12, rounds=40):
    days = planned_days(targets, max_level)
    per_day = 1440 * 60.0 / S.Rules().fishing['online_cycle_seconds']
    raw = [0.0] + [max(5.0, 18.0 * l ** 1.5) for l in range(1, max_level)]
    table = finish(raw, targets[0][0], window)
    best = None
    for _ in range(rounds):
        p = run(table)
        last = None
        xpa = [0.0] * max_level
        for level in range(1, max_level):
            if p.att.get(level):
                last = p.xp_at[level] / p.att[level]
            xpa[level] = last if last is not None else 1.0
        wanted = [0.0] + [max(1.0, (days[l + 1] - days[l]) * per_day * xpa[l]) for l in range(1, max_level)]
        # Half a step at a time in the logarithm, so the buying (which depends on the time) settles.
        raw = [0.0] + [math.sqrt(max(1.0, raw[l]) * wanted[l]) for l in range(1, max_level)]
        table = finish(raw, targets[0][0], window)
        got = run(table)
        err = max(abs(math.log((got.reached.get(l) or 999.0) / d)) for l, d in targets)
        if best is None or err < best[0]:
            best = (err, table)
        if err < 0.03:
            break
    return best[1]


def report(table):
    print('Tempo até cada nível com a tabela nova:')
    levels = [l for l in S.MILESTONES if l <= len(table)]
    print('  %-14s' % 'perfil', ' '.join('%8s' % ('Nv.%d' % l) for l in levels))
    for prof in S.PROFILES:
        p = run(table, prof)
        print('  %-14s' % prof, ' '.join('%8s' % S.fmt(p.reached.get(l)) for l in levels))
    print('XP para o próximo nível:')
    for l in REPORT_LEVELS:
        if l < len(table):
            print('  Nv.%-5d %s' % (l, '{:,}'.format(table[l]).replace(',', '.')))
    print('XP total do Nv.1 ao Nv.%d: %s' % (len(table), '{:,}'.format(sum(table[1:])).replace(',', '.')))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--max', type=int, default=1000, help='nível máximo do Pescador')
    ap.add_argument('--metas', default=DEFAULT_TARGETS, help='nível:tempo para quem deixa o jogo aberto (h = horas, d = dias)')
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    targets = parse_targets(a.metas)
    table = calibrate(targets, a.max)
    report(table)
    if a.gravar:
        path = os.path.join(S.CONFIG, 'progression.json')
        with open(path, encoding='utf-8') as f:
            prog = json.load(f, object_pairs_hook=collections.OrderedDict)
        fisher = prog['fisher']
        fisher['max_level'] = a.max
        fisher['xp_table'] = [collections.OrderedDict(level=l, xp_to_next_level=table[l]) for l in range(1, a.max)]
        for key in [k for k in fisher if k.startswith('total_xp_level_1_to_')]:
            del fisher[key]
        for top in (10, 100, 500, 1000):
            if top <= a.max:
                fisher['total_xp_level_1_to_%d' % top] = sum(table[1:top])
        # Same layout as the Dev Panel writes (DevPanelRoundTripTests): one key per line.
        with open(path, 'w', encoding='utf-8') as f:
            f.write(json.dumps(prog, ensure_ascii=False, indent=2) + '\n')
        print('Gravado em config/progression.json.')


if __name__ == '__main__':
    main()
