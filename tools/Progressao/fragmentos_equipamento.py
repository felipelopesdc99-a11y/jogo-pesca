"""Varas e roupas por fragmentos (M24-T32, A-163; M24-T33, A-164): gera os números e simula o ritmo.

Uso:
  python3 tools/Progressao/fragmentos_equipamento.py gerar
      # grava em config/rods.json a chance de fragmento de cada vara (fragment_drop_chance), o mapa onde ela cai
      # (fragment_map_id) e quantos fragmentos ela pede (fragments_required); e em config/equipment.json → clothing a
      # chance do Fragmento de Roupa, o custo de montar cada peça e a curva de XP de uso
  python3 tools/Progressao/fragmentos_equipamento.py simular [--dias 60] [--gravar]
      # compara o ritmo de hoje com o de antes (varas compradas na Loja, sem roupas) nos 3 jeitos de jogar e, com
      # --gravar, escreve docs/propostas/fragmentos_equipamento.json

Regras (as mesmas do jogo, GameService/Shop/RodFragmentRules.cs e ClothingRules.cs; o simulador é
simular_progressao.py, pelo jogador de referência de jogador.py, como passe_fragmentos.py; até o A-172 com a
Tripulação e as Melhorias, que saíram do jogo):
  - cada vara (menos a inicial) cai no ÚLTIMO mapa que a vara anterior alcança (o de maior nível entre os que pedem no
    máximo a categoria anterior): a vara que libera o próximo mapa sempre cai num mapa que o jogador já pesca;
  - chance da vara: o jogador de referência (4 vezes por dia, PERFIL_REFERENCIA) monta cada vara no mesmo dia em que a
    comprava na Loja antes (PRECOS_ANTIGOS, os preços de antes de M24-T32), pescando no melhor mapa liberado;
  - Fragmento de Roupa: um tipo só, em todos os mapas; a chance faz o jogador de referência montar as 5 peças em
    DIAS_ROUPAS dias, sem o Passe (o Passe também dá Fragmentos de Roupa);
  - XP de uso: a 1ª peça de quem deixa o jogo aberto chega ao Nv.100 em DIAS_NV100_ROUPA dias (crescimento geométrico
    a partir de XP_PRIMEIRO_NIVEL por nível).
"""
import argparse
import collections
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import curva_niveis as C  # noqa: E402
import passe_fragmentos as P  # noqa: E402
import jogador as J  # noqa: E402
import simular_progressao as S  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, 'docs', 'propostas', 'fragmentos_equipamento.json')

# ---------------------------------------------------------------- knobs
PERFIL_REFERENCIA = 'quatro_vezes'
# The Shop prices before M24-T32 (Moedas, Conchas): only to measure the old pace and to calibrate.
PRECOS_ANTIGOS = collections.OrderedDict([
    ('rod_01', (2500, 5)), ('rod_02', (90000, 40)), ('rod_03', (730000, 85)), ('rod_04', (3280000, 180)), ('rod_05', (15610000, 650)),
])
FRAGMENTOS_VARAS = {'rod_01': 15, 'rod_02': 25, 'rod_03': 35, 'rod_04': 45, 'rod_05': 60}
CUSTO_ROUPAS = [15, 25, 40, 60, 80]       # fragmentos da 1ª, 2ª … 5ª peça montada (qualquer peça)
DIAS_ROUPAS = 30.0                        # 4 vezes por dia, sem o Passe: as 5 peças
DIAS_NV100_ROUPA = 60.0                   # jogo aberto: a 1ª peça no Nv.100
XP_PRIMEIRO_NIVEL = 10                    # XP de uso do Nv.1 para o Nv.2
DIAS_CALIBRAR = 12.0
DIAS_SIMULAR = 60.0
CHECK_DAYS = (1, 3, 7, 14, 30, 45, 60)
TINY = 1e-9


def load(name):
    with open(os.path.join(S.CONFIG, name), encoding='utf-8') as f:
        return json.load(f, object_pairs_hook=collections.OrderedDict)


def save(name, data):
    with open(os.path.join(S.CONFIG, name), 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')


def dec(value, places):
    return ('%.*f' % (places, value)).replace('.', ',')


def pct(ratio, places):
    return dec(ratio * 100.0, places) + '%'


def fmt(days):
    return P.fmt_days(days * 86400.0) if days is not None else '—'


def drop_maps(maps, rods):
    """The map of each rod: the last map (highest unlock level) whose minimum_rod_tier is below the rod's tier."""
    ordered = sorted(maps, key=lambda m: m['unlock_fisher_level'])
    out = collections.OrderedDict()
    for rod in sorted(rods, key=lambda r: r['tier']):
        if rod['tier'] == 0:
            continue
        reachable = [m for m in ordered if m['minimum_rod_tier'] < rod['tier']]
        out[rod['id']] = reachable[-1]['id']
    return out


class Game(P.Game):
    """The reference economy plus a record of when the player first fished on each map."""

    def __init__(self, *a, **k):
        super().__init__(*a, **k)
        self.first_on_map = {}
        self.trace = []           # (day, rod fragments so far, clothing fragments ever received)
        self.clothes_on_day = collections.OrderedDict()   # check day -> {piece: level}

    def observe(self):
        super().observe()
        day = self.t / 86400.0
        p = self.p
        self.first_on_map.setdefault(p.map()['id'], day)
        spent = sum(p.r.clothing_cost(i) for i in range(len(p.clothes))) if p.r.clothing else 0.0
        self.trace.append((day, dict(p.rod_fragments), p.clothing_fragments + spent))
        for d in CHECK_DAYS:
            if d not in self.clothes_on_day and day >= d:
                self.clothes_on_day[d] = collections.OrderedDict((pid, st[0]) for pid, st in p.clothes.items())


def play(profile, days, legacy=False, clothing=True, rod_chances=None, clothing_chance=None, level_growth=None, pass_cfg=None):
    """passe_fragmentos.play with the rules adjusted in memory (nothing is written)."""
    g = Game(pass_cfg, False)
    r = g.rules
    if legacy:
        r.legacy_rod_prices = dict(PRECOS_ANTIGOS)
    if not clothing:
        r.clothing = None
    elif r.clothing is not None:
        r.clothing = json.loads(json.dumps(r.clothing))
        if clothing_chance is not None:
            r.clothing['fragment_drop_chance'] = clothing_chance
        if level_growth is not None:
            r.clothing['level_xp']['growth'] = level_growth
    for rid, chance in (rod_chances or {}).items():
        next(x for x in r.rods if x['id'] == rid)['fragment_drop_chance'] = chance
    sessions = J.PROFILES[profile][1]
    end = days * 86400.0
    if sessions is None:
        while g.t < end and g.p.level < r.max_level:
            g.online(J.step_for(g.t) * 20, J.step_for(g.t))
        return g
    last_end = None
    day = 0
    while g.t < end:
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


def at_day(trace, day, pick):
    """The traced value at a day (linear between two observations)."""
    prev = (0.0, 0.0)
    for t, rods, clothes in trace:
        v = pick(rods, clothes)
        if t >= day:
            if t == prev[0]:
                return v
            return prev[1] + (v - prev[1]) * (day - prev[0]) / (t - prev[0])
        prev = (t, v)
    return prev[1]


# ---------------------------------------------------------------- gerar

def gerar():
    rods_cfg = load('rods.json')
    eq = load('equipment.json')
    maps = load('maps.json')['maps']
    where = drop_maps(maps, rods_cfg['rods'])

    print('Antes (varas compradas na Loja, sem roupas), %s…' % PERFIL_REFERENCIA)
    old = play(PERFIL_REFERENCIA, DIAS_CALIBRAR, legacy=True, clothing=False).p.bought
    chances = {rid: TINY for rid in where}
    for rid in where:
        # Rod `rid` never assembles (tiny chance): its fragments ÷ chance = fish pulled out on its map so far.
        g = play(PERFIL_REFERENCIA, DIAS_CALIBRAR, rod_chances=chances)
        caught = at_day(g.trace, old[rid], lambda rods, _: rods.get(rid, 0.0) / TINY)
        chances[rid] = round(FRAGMENTOS_VARAS[rid] / max(1.0, caught), 4)
        print('  %s: cai em %s, %d fragmentos, %s por peixe (comprada no dia %s antes; %s peixes puxados ali até lá)'
              % (rid, where[rid], FRAGMENTOS_VARAS[rid], pct(chances[rid], 2), dec(old[rid], 2), dec(caught, 0)))

    for rod in rods_cfg['rods']:
        if rod['id'] in where:
            rod['fragment_map_id'] = where[rod['id']]
            rod['fragments_required'] = FRAGMENTOS_VARAS[rod['id']]
            rod['fragment_drop_chance'] = chances[rod['id']]
    save('rods.json', rods_cfg)

    # Clothing: the chance that assembles the 5 pieces in DIAS_ROUPAS days (4 vezes por dia, no pass).
    total = float(sum(CUSTO_ROUPAS))
    chance = eq['clothing']['fragment_drop_chance']
    for _ in range(4):
        g = play(PERFIL_REFERENCIA, DIAS_ROUPAS + 1, clothing_chance=chance)
        frags = at_day(g.trace, DIAS_ROUPAS, lambda _, c: c)
        chance = chance * total / max(TINY, frags)
    chance = round(chance, 4)
    print('Fragmento de Roupa: %s por peixe (as 5 peças em ~%s dias, 4 vezes por dia)' % (pct(chance, 2), dec(DIAS_ROUPAS, 0)))

    # Use XP: the first piece of the always-open player reaches Nv.100 in DIAS_NV100_ROUPA days.
    growth = eq['clothing']['level_xp']['growth']
    for _ in range(3):
        g = play('sempre_aberto', DIAS_NV100_ROUPA, clothing_chance=chance, level_growth=growth)
        first = min(g.p.clothes_at.values()) if g.p.clothes_at else 0.0
        # Fish pulled out from the first piece to the target day = the use XP it needs (use_xp_per_fish each); the
        # clothing fragments ever received ÷ the chance count the fish.
        received = lambda d: at_day(g.trace, d, lambda _, c: c)  # noqa: E731
        fish = (received(DIAS_NV100_ROUPA) - received(first)) / chance * eq['clothing'].get('use_xp_per_fish', 1)
        lo, hi = 1.0001, 1.5
        for _ in range(60):
            mid = (lo + hi) / 2
            need = sum(C.cost(XP_PRIMEIRO_NIVEL, mid, l) for l in range(1, eq['clothing']['max_level']))
            lo, hi = (mid, hi) if need < fish else (lo, mid)
        growth = round(lo, 4)
    print('XP de uso: %d no Nv.1, × %s por nível (1ª peça no Nv.100 em ~%s dias com o jogo aberto)' % (XP_PRIMEIRO_NIVEL, dec(growth, 4), dec(DIAS_NV100_ROUPA, 0)))

    c = eq['clothing']
    c['fragment_drop_chance'] = chance
    c['fragments_to_assemble'] = list(CUSTO_ROUPAS)
    c['level_xp'] = collections.OrderedDict([('first', XP_PRIMEIRO_NIVEL), ('growth', growth)])
    save('equipment.json', eq)
    print('config/rods.json e config/equipment.json gravados.')


# ---------------------------------------------------------------- simular

def simular(days, gravar):
    rods_cfg = load('rods.json')
    eq = load('equipment.json')
    maps = sorted(load('maps.json')['maps'], key=lambda m: m['unlock_fisher_level'])
    rods = [r for r in rods_cfg['rods'] if r.get('fragment_map_id')]
    map_name = {m['id']: m['display_name'] for m in maps}
    rules = S.Rules()

    out = collections.OrderedDict()
    out['gerado_por'] = 'tools/Progressao/fragmentos_equipamento.py'
    out['nota'] = ('Varas por fragmentos (M24-T32, A-163) e roupas (M24-T33, A-164). "antes": as varas compradas na Loja '
                   '(os preços de antes) e sem roupas; "agora": as varas por fragmentos e as roupas. Simulação por valor '
                   'esperado só com a pesca (sem Tripulação nem Melhorias desde o A-172), sem o Passe e sem VIP, sempre no melhor mapa liberado. '
                   'Dias contados da meia-noite do primeiro dia.')
    out['varas'] = collections.OrderedDict()
    for r in rods:
        out['varas'][r['id']] = collections.OrderedDict([
            ('nome', r['display_name']), ('categoria', r['tier']), ('mapa', r['fragment_map_id']), ('nome_do_mapa', map_name[r['fragment_map_id']]),
            ('fragmentos', r['fragments_required']), ('chance_por_peixe', r['fragment_drop_chance']),
            ('preco_antes', PRECOS_ANTIGOS[r['id']]),
        ])
    c = eq['clothing']
    out['roupas'] = collections.OrderedDict([
        ('chance_por_peixe', c['fragment_drop_chance']), ('custo_por_ordem', c['fragments_to_assemble']),
        ('xp_de_uso', c['level_xp']), ('ordem_simulada', list(S.CLOTHING_ORDER)),
        ('pecas', [collections.OrderedDict([('id', p['id']), ('nome', p['display_name']), ('eixo', p['bonus_axis']),
                                            ('nv1', p['bonus_at_level_1']), ('nv100', p['bonus_at_max_level'])]) for p in c['pieces']]),
    ])
    nivel_pecas = collections.OrderedDict()
    xp_need = [0] + [rules.clothing_xp(l) for l in range(1, c['max_level'])]
    out['xp_de_uso_total_ate_nv100'] = sum(xp_need)

    out['perfis'] = collections.OrderedDict()
    for profile in J.PROFILES:
        print('Simulando %s (antes e agora)…' % profile)
        before = play(profile, days, legacy=True, clothing=False)
        now = play(profile, days)
        row = collections.OrderedDict()
        row['descricao'] = J.PROFILES[profile][0]
        row['varas'] = collections.OrderedDict()
        for r in rods:
            row['varas'][r['id']] = collections.OrderedDict([
                ('antes_dia', round(before.p.bought.get(r['id'], -1), 3)), ('agora_dia', round(now.p.bought.get(r['id'], -1), 3)),
            ])
        row['primeiro_dia_em_cada_mapa'] = collections.OrderedDict(
            (m['id'], collections.OrderedDict([('antes', round(before.first_on_map.get(m['id'], -1), 3)), ('agora', round(now.first_on_map.get(m['id'], -1), 3))]))
            for m in maps)
        row['roupas_montadas_dia'] = collections.OrderedDict((pid, round(d, 2)) for pid, d in now.p.clothes_at.items())
        row['niveis_das_roupas_no_fim'] = collections.OrderedDict((pid, st[0]) for pid, st in now.p.clothes.items())
        nivel_pecas[profile] = collections.OrderedDict((str(d), lv) for d, lv in now.clothes_on_day.items())
        row['dias_ate_nivel'] = collections.OrderedDict()
        for lvl in (10, 100, 500, 1000):
            row['dias_ate_nivel']['Nv.%d' % lvl] = collections.OrderedDict([
                ('antes', round(before.p.reached[lvl], 2) if lvl in before.p.reached else None),
                ('agora', round(now.p.reached[lvl], 2) if lvl in now.p.reached else None)])
        row['moedas_gastas_em_varas_antes'] = sum(PRECOS_ANTIGOS[r['id']][0] for r in rods if r['id'] in before.p.bought)
        out['perfis'][profile] = row
        print('  varas:', ', '.join('%s %s → %s' % (rid, fmt(v['antes_dia']), fmt(v['agora_dia'])) for rid, v in row['varas'].items()))
        print('  roupas montadas:', ', '.join('%s %s' % (pid, fmt(d)) for pid, d in row['roupas_montadas_dia'].items()),
              '· níveis no fim:', row['niveis_das_roupas_no_fim'])
        print('  níveis:', ', '.join('%s %s → %s' % (k, fmt(v['antes']), fmt(v['agora'])) for k, v in row['dias_ate_nivel'].items()))

    out['niveis_das_roupas_por_dia'] = nivel_pecas
    for profile, rows in nivel_pecas.items():
        print('Níveis das roupas (%s):' % profile)
        for d, lv in rows.items():
            print('  dia %s: %s' % (d, ', '.join('%s Nv.%d' % (k, v) for k, v in lv.items())))

    if gravar:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(out, f, ensure_ascii=False, indent=2)
            f.write('\n')
        print('Gravado em', os.path.relpath(OUT, ROOT))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('acao', choices=('gerar', 'simular'))
    ap.add_argument('--dias', type=float, default=DIAS_SIMULAR)
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    if a.acao == 'gerar':
        gerar()
    else:
        simular(a.dias, a.gravar)


if __name__ == '__main__':
    main()
