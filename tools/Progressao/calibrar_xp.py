"""Calibra a tabela de XP do Pescador (até o nível máximo, hoje Nv.1000) para metas de tempo.

Uso:  python3 tools/Progressao/calibrar_xp.py [--manter-ate 100] [--metas 250:3.67d,500:14.6d,750:33.3d,1000:60d] [--gravar]

Como funciona (M24-T31, A-162; refeito no A-172, sem a Tripulação e sem as Melhorias, que saíram do jogo):
  1. Mede com a economia atual: o jogador de referência é o de tools/Progressao/jogador.py no perfil "sempre aberto"
     (o jogo aberto o dia todo, só online), com a pesca no ciclo de progression.json → fishing (10 s online), os níveis
     de vara e de barco, os barcos e as varas por fragmentos e as iscas de config/equipment.json. Sem o Passe do
     Pescador, sem VIP e **sem roupas** (decisão do proprietário na OD-067: as roupas, sobretudo a Calça, adiantam o
     Nv.1000 de ~60 para ~55 dias, e isso é pensado). Com --com-roupas, calibra com as roupas.
  2. O começo fica como está: os níveis abaixo de --manter-ate (padrão 100) não mudam, e o tempo até esse nível é o
     que a simulação mede com eles (hoje Nv.10 em ~20 min e Nv.100 em ~0,5 dia).
  3. Depois dele, o tempo planejado até cada nível segue uma curva suave (logaritmo do tempo contra o logaritmo do
     nível, interpolação monotônica) que passa pelo tempo medido no nível mantido e pelas metas.
  4. Depois do Nv.90 todos os mapas estão liberados, e a renda (Moedas e XP por segundo) depende do tempo de jogo,
     não do nível do Pescador. Então a simulação mede o XP acumulado até cada momento, X(t), e o XP de cada nível
     L = X(dia planejado do Nv.L+1) − X(dia planejado do Nv.L). Para medir além do fim da tabela, a simulação usa a
     tabela com níveis a mais (o jogador não para de ganhar XP no nível máximo). Repete com a tabela nova até o tempo
     bater com as metas.
  5. Suaviza (média móvel do logaritmo, sem degrau na emenda) e arredonda: de 5 em 5 até 1.000 e com 3 algarismos
     significativos acima disso. A tabela nunca pede menos XP que o nível anterior.
  6. Com --gravar, escreve a tabela e os totais em config/progression.json.

O XP dos peixes não muda: o que muda é quanto cada nível pede. Trocar a curva pede subir
fisher.xp_curve_version, para os saves serem convertidos (TD-038).
"""
import argparse
import bisect
import collections
import json
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import jogador as J  # noqa: E402
import simular_progressao as S  # noqa: E402

PROFILE = 'sempre_aberto'
DEFAULT_KEEP = 100
DEFAULT_TARGETS = '250:3.67d,500:14.6d,750:33.3d,1000:60d'
REPORT_LEVELS = (1, 10, 50, 100, 101, 250, 500, 750, 999)
TIME_LEVELS = (10, 50, 100, 250, 500, 750, 1000)
EXTRA_LEVELS = 3000   # levels added past the end while measuring, so the XP keeps being counted


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


def current_table():
    r = S.Rules()
    return list(r.xp_to_next)   # index = level, [0] unused; len = max_level


# Whether the reference player wears clothing (A-164). The calibration is without it (OD-067); main() can change it.
WITH_CLOTHING = False


def run(table, profile=PROFILE, days=None, extend=False, stop_at_max=True, clothing=None):
    """The reference player (fishing only, no pass, no VIP; clothing per WITH_CLOTHING) with this XP table."""
    t = list(table) + ([table[-1] * 1000] * EXTRA_LEVELS if extend else [])
    rules = S.Rules()
    if not (WITH_CLOTHING if clothing is None else clothing):
        rules.clothing = None
    rules.max_level = len(t)
    rules.xp_to_next = t
    g = J.Game(rules)
    return J.play(g, profile, days or 200.0, stop_at_level=len(table) if stop_at_max else None)


def xp_by_time(table, days):
    """(days, cumulative XP) at every level-up of a run with the table extended past its end."""
    g = run(table, days=days, extend=True, stop_at_max=False)
    t = list(table) + [table[-1] * 1000] * EXTRA_LEVELS
    cum = [0.0] * (len(t) + 1)
    for level in range(2, len(t) + 1):
        cum[level] = cum[level - 1] + t[level - 1]
    pts = sorted((d, cum[level]) for level, d in g.p.reached.items() if level <= len(t))
    return [p[0] for p in pts], [p[1] for p in pts], g


def interp(xs, ys, x):
    i = bisect.bisect_left(xs, x)
    if i <= 0:
        return ys[0]
    if i >= len(xs):
        return ys[-1]
    x0, x1, y0, y1 = xs[i - 1], xs[i], ys[i - 1], ys[i]
    return y0 + (y1 - y0) * (x - x0) / (x1 - x0) if x1 > x0 else y1


def rounded(x):
    if x < 1000:
        return max(5, int(round(x / 5.0)) * 5)
    digits = int(math.floor(math.log10(x))) - 2
    step = 10 ** digits
    return int(round(x / step)) * step


def smooth(values, first_level, window):
    """Moving average of the logarithm from first_level on, the window anchored on the kept levels before it."""
    logs = np.log(np.maximum(values, 1.0))
    out = values[:]
    for level in range(first_level, len(values)):
        lo, hi = max(1, level - window), min(len(values) - 1, level + window)
        out[level] = math.exp(float(np.mean(logs[lo:hi + 1])))
    return out


def planned_days(anchor, targets, max_level):
    """Planned day each level is reached, from the kept level (anchor = (level, day)) on."""
    pts = [anchor] + [t for t in targets if t[0] > anchor[0]]
    curve = pchip([math.log(l) for l, _ in pts], [math.log(d) for _, d in pts])
    return {level: math.exp(curve(math.log(level))) for level in range(anchor[0], max_level + 1)}


def calibrate(keep, targets, window=12, rounds=6):
    base = current_table()
    max_level = max(len(base), targets[-1][0])
    base = base + [base[-1]] * (max_level - len(base))
    probe = run(base[:keep], stop_at_max=True)
    anchor = (keep, probe.p.reached[keep])
    plan = planned_days(anchor, targets, max_level)
    horizon = targets[-1][1] * 1.25
    table = base[:]
    best = None
    for _ in range(rounds):
        xs, ys, _ = xp_by_time(table, horizon)
        raw = [float(x) for x in table]
        for level in range(keep, max_level):
            raw[level] = max(1.0, interp(xs, ys, plan[level + 1]) - interp(xs, ys, plan[level]))
        sm = smooth(raw, keep, window)
        new = table[:keep] + [rounded(x) for x in sm[keep:]]
        for level in range(keep, max_level):
            new[level] = max(new[level], new[level - 1])
        got = run(new)
        err = max(abs(math.log((got.p.reached.get(l) or 999.0) / d)) for l, d in targets)
        if best is None or err < best[0]:
            best = (err, new, got)
        table = new
        if err < 0.005:
            break
    return best[1], anchor


def report(table):
    for clothing in (False, True):
        print('Tempo até cada nível (só a pesca, %s roupas, sem Passe e sem VIP):' % ('com' if clothing else 'sem'))
        levels = [l for l in TIME_LEVELS if l <= len(table)]
        print('  %-14s' % 'perfil', ' '.join('%8s' % ('Nv.%d' % l) for l in levels))
        for prof in J.PROFILES:
            g = run(table, prof, days=400, clothing=clothing)
            print('  %-14s' % prof, ' '.join('%8s' % S.fmt(g.p.reached.get(l)) for l in levels))
    print('XP para o próximo nível:')
    for l in REPORT_LEVELS:
        if l < len(table):
            print('  Nv.%-5d %s' % (l, '{:,}'.format(table[l]).replace(',', '.')))
    print('XP total do Nv.1 ao Nv.%d: %s' % (len(table), '{:,}'.format(sum(table[1:])).replace(',', '.')))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--manter-ate', type=int, default=DEFAULT_KEEP, help='os níveis abaixo deste não mudam')
    ap.add_argument('--metas', default=DEFAULT_TARGETS, help='nível:tempo para quem deixa o jogo aberto (h = horas, d = dias)')
    ap.add_argument('--com-roupas', action='store_true', help='calibra com as roupas (o padrão é sem, OD-067)')
    ap.add_argument('--so-relatorio', action='store_true', help='não calibra: só mostra os tempos da tabela de hoje')
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    global WITH_CLOTHING
    WITH_CLOTHING = a.com_roupas
    if a.so_relatorio:
        report(current_table())
        return
    targets = parse_targets(a.metas)
    table, anchor = calibrate(a.manter_ate, targets)
    print('Mantidos os níveis 1 a %d; Nv.%d medido em %s dia.' % (a.manter_ate - 1, anchor[0], ('%.2f' % anchor[1]).replace('.', ',')))
    report(table)
    if a.gravar:
        max_level = len(table)
        path = os.path.join(S.CONFIG, 'progression.json')
        with open(path, encoding='utf-8') as f:
            prog = json.load(f, object_pairs_hook=collections.OrderedDict)
        fisher = prog['fisher']
        fisher['max_level'] = max_level
        fisher['xp_table'] = [collections.OrderedDict(level=l, xp_to_next_level=table[l]) for l in range(1, max_level)]
        for key in [k for k in fisher if k.startswith('total_xp_level_1_to_')]:
            del fisher[key]
        for top in (10, 100, 500, 1000):
            if top <= max_level:
                fisher['total_xp_level_1_to_%d' % top] = sum(table[1:top])
        # Same layout as the Dev Panel writes (DevPanelRoundTripTests): one key per line.
        with open(path, 'w', encoding='utf-8') as f:
            f.write(json.dumps(prog, ensure_ascii=False, indent=2) + '\n')
        print('Gravado em config/progression.json.')


if __name__ == '__main__':
    main()
