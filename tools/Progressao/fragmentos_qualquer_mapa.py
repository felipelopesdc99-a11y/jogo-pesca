"""Fragmentos em qualquer mapa (M24-T47, A-173): quantos dias para fechar todos os itens e o efeito na progressão.

Uso:
  python3 tools/Progressao/fragmentos_qualquer_mapa.py simular [--dias 400] [--gravar]
      # simula os 3 jeitos de jogar (tools/Progressao/jogador.py) com a regra de antes e com a de hoje e, com --gravar,
      # escreve docs/propostas/fragmentos_qualquer_mapa.json

A regra de hoje (decisão do proprietário em 13/10/2026): em qualquer mapa, cada peixe puxado (online e offline) tem
fragment_drop_chance (hoje 0,1%) de dar 1 fragmento de CADA barco, de CADA vara e do Fragmento de Roupa, cada um num
sorteio independente. Os números ficam em config/equipment.json (barcos e clothing) e config/rods.json (varas).

A regra de antes (ANTES, abaixo, os números de 12/10/2026): cada barco caía só no mapa dele (map_id, 0,43% a 0,52%) e num
dos mapas 6 a 10 (extra_fragment_maps, 0,53% a 0,67%); cada vara só no fragment_map_id dela (1,63% a 7%); o Fragmento de
Roupa em qualquer mapa (0,17%).

O que a simulação conta:
  - a pesca (valor esperado, sem sorte), sempre no melhor mapa liberado, como simular_progressao.py, e continua pescando
    depois do Nv.1000 (só os fragmentos e as Moedas contam ali);
  - o Passe do Pescador grátis (config/pass.json: fragmentos de barco, de vara e de roupa), resgatado na hora; sem o
    Premium;
  - NÃO conta o Boss Mundial (os fragmentos de barco e de roupa das faixas dele): ele depende do dano de cada dia e não
    está no jogador de referência. O Boss só adianta os números abaixo;
  - roupas montadas na ordem de simular_progressao.CLOTHING_ORDER, assim que há fragmentos.
"""
import argparse
import collections
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import jogador as J  # noqa: E402
import passe_fragmentos as P  # noqa: E402
import simular_progressao as S  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, 'docs', 'propostas', 'fragmentos_qualquer_mapa.json')
DIAS = 400.0
NIVEIS = (10, 100, 500, 1000)

# The rule before M24-T47 (config of 12/10/2026): {map id: (item id, chance per fish pulled out)}.
ANTES_BARCOS = {
    'map_01': ('boat_01', 0.0043), 'map_02': ('boat_02', 0.0043), 'map_03': ('boat_03', 0.0048), 'map_04': ('boat_04', 0.0051),
    'map_05': ('boat_05', 0.0052), 'map_06': ('boat_01', 0.0053), 'map_07': ('boat_02', 0.0058), 'map_08': ('boat_03', 0.0061),
    'map_09': ('boat_04', 0.0066), 'map_10': ('boat_05', 0.0067),
}
ANTES_VARAS = {
    'map_01': ('rod_01', 0.037), 'map_03': ('rod_02', 0.0405), 'map_05': ('rod_03', 0.07), 'map_07': ('rod_04', 0.036),
    'map_09': ('rod_05', 0.0163),
}
ANTES_ROUPA = 0.0017


class Game(P.Game):
    """The reference player with the free pass, recording the first day on each map."""

    def __init__(self, pass_cfg):
        super().__init__(pass_cfg, False)
        self.first_on_map = {}
        self.rules.fish_past_max_level = True

    def observe(self):
        super().observe()
        self.first_on_map.setdefault(self.p.map()['id'], self.t / 86400.0)


def regra_antes(rules):
    """Puts the rule before M24-T47 in `rules` (in memory only)."""
    boats = {b['id']: b for b in rules.boats}
    rods = {r['id']: r for r in rules.rods}
    rules.boat_drops = lambda map_id: [(boats[ANTES_BARCOS[map_id][0]], ANTES_BARCOS[map_id][1])] if map_id in ANTES_BARCOS else []
    rules.rod_drops = lambda map_id: [(rods[ANTES_VARAS[map_id][0]], ANTES_VARAS[map_id][1])] if map_id in ANTES_VARAS else []
    if rules.clothing is not None:
        rules.clothing = json.loads(json.dumps(rules.clothing))
        rules.clothing['fragment_drop_chance'] = ANTES_ROUPA


def done(g):
    """Every boat, rod and clothing piece assembled."""
    r, p = g.rules, g.p
    return (all(b['id'] in p.owned_boats for b in r.fragment_boats) and all(x['id'] in p.rods for x in r.fragment_rods)
            and (r.clothing is None or len(p.clothes) >= len(S.CLOTHING_ORDER)))


def play(profile, days, antes, pass_cfg):
    """jogador.play, but it goes on past the Nv.1000 until every item is assembled (or `days`)."""
    g = Game(pass_cfg)
    if antes:
        regra_antes(g.rules)
    sessions = J.PROFILES[profile][1]
    end = days * 86400.0
    finished = lambda: done(g) and g.p.level >= g.rules.max_level  # noqa: E731
    if sessions is None:
        while g.t < end and not finished():
            g.online(J.step_for(g.t) * 20, J.step_for(g.t))
        return g
    last_end = None
    day = 0
    while g.t < end and not finished():
        for start_h, minutes in sessions:
            start = day * 86400.0 + start_h * 3600.0
            if last_end is None:
                g.t = start
            else:
                g.offline(start - last_end)
            g.online(minutes * 60.0, 5.0 if day == 0 else 30.0)
            last_end = g.t
        day += 1
    return g


def d2(v):
    return round(v, 2) if v is not None else None


def fmt(v):
    return P.fmt_days(v * 86400.0) if v is not None else '—'


def liberado(g, m):
    """The day a map opens: the Fisher level it asks for and a rod of its minimum_rod_tier (None = not yet)."""
    p = g.p
    level_day = p.reached.get(m['unlock_fisher_level'])
    rods = [0.0 if x['tier'] == 0 else p.bought.get(x['id']) for x in g.rules.rods if x['tier'] >= m['minimum_rod_tier']]
    rods = [d for d in rods if d is not None]
    if level_day is None or not rods:
        return None
    return max(level_day, min(rods))


def resultado(g):
    r, p = g.rules, g.p
    out = collections.OrderedDict()
    out['barcos'] = collections.OrderedDict((b['id'], d2(p.bought.get(b['id']))) for b in r.fragment_boats)
    out['varas'] = collections.OrderedDict((x['id'], d2(p.bought.get(x['id']))) for x in r.fragment_rods)
    out['roupas'] = collections.OrderedDict((pid, d2(p.clothes_at.get(pid))) for pid in S.CLOTHING_ORDER)
    days = list(out['barcos'].values()) + list(out['varas'].values()) + list(out['roupas'].values())
    out['todos_os_itens'] = None if any(v is None for v in days) else max(days)
    out['mapas_liberados'] = collections.OrderedDict((m['id'], d2(liberado(g, m))) for m in r.maps)
    out['primeiro_dia_pescando_no_mapa'] = collections.OrderedDict((m['id'], d2(g.first_on_map.get(m['id']))) for m in r.maps)
    out['niveis'] = collections.OrderedDict(('Nv.%d' % l, d2(p.reached.get(l))) for l in NIVEIS)
    out['fragmentos_de_barco_do_passe'] = collections.OrderedDict((k, round(v, 1)) for k, v in g.boats_by_pass.items())
    return out


def simular(days, gravar):
    pass_cfg = P.load('pass.json')
    rules = S.Rules()
    out = collections.OrderedDict()
    out['gerado_por'] = 'tools/Progressao/fragmentos_qualquer_mapa.py simular'
    out['nota'] = ('Fragmentos em qualquer mapa (M24-T47, A-173). Dias contados da meia-noite do primeiro dia, por valor '
                   'esperado (sem sorte). "antes": cada barco e cada vara só no mapa deles (a regra até 12/10/2026); '
                   '"agora": todos em qualquer mapa, 0,1%% cada. Conta a pesca (online e offline, continuando depois do '
                   'Nv.1000) e o Passe grátis; não conta o Boss Mundial nem o Premium. null = não chegou em %d dias.' % days)
    out['chances_agora'] = collections.OrderedDict(
        [(b['id'], b.get('fragment_drop_chance')) for b in rules.fragment_boats]
        + [(x['id'], x.get('fragment_drop_chance')) for x in rules.fragment_rods]
        + [('clothing', (rules.clothing or {}).get('fragment_drop_chance'))])
    out['fragmentos_para_montar'] = collections.OrderedDict(
        [(b['id'], b['fragments_required']) for b in rules.fragment_boats]
        + [(x['id'], x['fragments_required']) for x in rules.fragment_rods]
        + [('roupas', (rules.clothing or {}).get('fragments_to_assemble'))])
    out['perfis'] = collections.OrderedDict()
    for profile in J.PROFILES:
        row = collections.OrderedDict([('descricao', J.PROFILES[profile][0])])
        for key, antes in (('antes', True), ('agora', False)):
            print('Simulando %s (%s)…' % (profile, key))
            res = resultado(play(profile, days, antes, pass_cfg))
            row[key] = res
            print('  barcos:', ', '.join('%s %s' % (k, fmt(v)) for k, v in res['barcos'].items()))
            print('  varas: ', ', '.join('%s %s' % (k, fmt(v)) for k, v in res['varas'].items()))
            print('  roupas:', ', '.join('%s %s' % (k, fmt(v)) for k, v in res['roupas'].items()))
            print('  tudo:  ', fmt(res['todos_os_itens']))
            print('  mapas liberados:', ', '.join('%s %s' % (k[-2:], fmt(v)) for k, v in res['mapas_liberados'].items()))
            print('  níveis:', ', '.join('%s %s' % (k, fmt(v)) for k, v in res['niveis'].items()))
        out['perfis'][profile] = row
    if gravar:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(out, f, ensure_ascii=False, indent=2)
            f.write('\n')
        print('Gravado em', os.path.relpath(OUT, ROOT))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('acao', choices=('simular',))
    ap.add_argument('--dias', type=float, default=DIAS)
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    simular(a.dias, a.gravar)


if __name__ == '__main__':
    main()
