"""Varas, barcos e peixes até o Nv.100 (M24-T13, A-156, TD-041): gera os números em /config e simula o ritmo.

Uso:
  python3 tools/Progressao/niveis_100.py gerar      # grava os números novos em config/ (rods, equipment, progression,
                                                    # arena_bots, market_bots)
  python3 tools/Progressao/niveis_100.py simular [--dias 80] [--gravar]
                                                    # simula o ritmo com a economia do M24 (Tripulação + Melhorias) e,
                                                    # com --gravar, escreve docs/propostas/niveis_100.json

Regras (as mesmas do jogo, curva_niveis.py e GameService/Config/LevelCurve.cs):
  - bônus no nível N = bônus do Nv.1 (o de hoje) + (bônus do Nv.100 − bônus do Nv.1) × ((N − 1) ÷ 99) ^ EXPOENTE;
    o Nv.100 vale MULT_NO_MAXIMO × o bônus do Nv.10 de antes (varas) ou × o bônus de hoje (barcos);
  - custo de N para N + 1 = primeiro × crescimento ^ (N − 1), em Moedas e em Conchas;
  - peixes: bônus de atributo 0 no Nv.1 e MULT_NO_MAXIMO × 36% (o Nv.10 de antes) no Nv.100, na mesma curva;
    XP para o próximo nível = máx(XP_PEIXE_MINIMO, base × N ^ expoente), arredondado para múltiplo de 5;
  - migração (save v15): cada nível antigo (1 a 10) vira o menor nível novo cujo bônus é maior ou igual ao antigo em
    todos os eixos; nunca perde força. Gravado em legacy_levels_v1 nas varas e nos peixes.

Os números de antes (as tabelas de 10 níveis) ficam em docs/propostas/niveis_100.json → "antes", para o script poder
rodar de novo depois de /config já ter o formato novo.
"""
import argparse
import collections
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import curva_niveis as C  # noqa: E402
import simular_progressao as S  # noqa: E402

ROOT = S.ROOT
CONFIG = S.CONFIG
OUT = os.path.join(ROOT, 'docs', 'propostas', 'niveis_100.json')

# ---------------------------------------------------------------- knobs (ajuste aqui e rode "gerar" de novo)
MAX_LEVEL = 100
EXPOENTE = 1.2            # > 1: cada nível soma um pouco mais que o anterior (começa com passos pequenos)
MULT_NO_MAXIMO = 3.0      # "crescimento mediano": o Nv.100 vale ~3× o máximo de hoje

# Varas: o 1º nível custa uma fração do 1º nível de hoje; cada nível custa "crescimento" × o anterior.
VARA_MOEDAS_PRIMEIRO = 1.0 / 3.0
VARA_MOEDAS_CRESCIMENTO = 1.14
VARA_CONCHAS_PRIMEIRO = 1.0 / 4.0
VARA_CONCHAS_CRESCIMENTO = 1.03

# Barcos: o 1º nível custa uma fração do preço do barco.
BARCO_MOEDAS_PRIMEIRO = 0.05
BARCO_MOEDAS_CRESCIMENTO = 1.14
BARCO_CONCHAS_PRIMEIRO = 0.05
BARCO_CONCHAS_CRESCIMENTO = 1.03

# Peixes
PEIXE_BONUS_ANTES_POR_NIVEL = 4.0     # % por nível de antes (Nv.10 = +36%)
XP_PEIXE_BASE = 0.11
XP_PEIXE_EXPOENTE = 2.0
XP_PEIXE_MINIMO = 5

# Simulação: um nível de vara ou barco é comprado quando custa até estes segundos de renda (Tripulação + pesca), como
# o Sonar e a Caixa Térmica em simular_melhorias.py, e quando há Moedas e Conchas.
SEGUNDOS_DE_RENDA = 600.0


def load(name):
    with open(os.path.join(CONFIG, name), encoding='utf-8') as f:
        return json.load(f, object_pairs_hook=collections.OrderedDict)


def save(name, data):
    with open(os.path.join(CONFIG, name), 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')


def read_before():
    """As tabelas de 10 níveis de antes: do config (formato antigo) ou de docs/propostas/niveis_100.json."""
    rods = load('rods.json')
    if any(r.get('bonuses_per_level') for r in rods['rods']):
        eq = load('equipment.json')
        prog = load('progression.json')
        return {
            'varas': {r['id']: {'bonuses_per_level': {k: r['bonuses_per_level'][k] for k in C.AXES},
                                'upgrade_costs': [[u['cost_coins'], u['cost_shells']] for u in r['upgrade_costs']]}
                      for r in rods['rods'] if r.get('bonuses_per_level')},
            'barcos': {b['id']: b['catch_success_bonus'] for b in eq['boats']},
            'peixes': {'max_level': prog['fish_level']['max_level'],
                       'stat_bonus_per_level_percent': prog['fish_level']['stat_bonus_per_level_percent'],
                       'xp_table': [x['xp_to_next_level'] for x in prog['fish_level']['xp_table']]},
            'arena_top_fish_level': load('arena_bots.json')['strength_by_rank']['top_fish_level'],
            'peixes_aviso_min_level': prog['feeding']['valuable_feed_rules']['min_level'],
            'mercado_max_fish_level': load('market_bots.json')['supply']['max_fish_level'],
            'mercado_fish_level_premium_per_level': load('market_bots.json')['valuation']['fish_level_premium_per_level'],
        }
    with open(OUT, encoding='utf-8') as f:
        return json.load(f)['antes']


def round5(x):
    return int(5 * math.floor(x / 5.0 + 0.5))


def fish_xp_table():
    return [max(XP_PEIXE_MINIMO, round5(XP_PEIXE_BASE * lvl ** XP_PEIXE_EXPOENTE)) for lvl in range(1, MAX_LEVEL)]


def first_level_at_least(values_at, targets):
    """Menor nível novo cujo bônus é ≥ o alvo em todos os eixos."""
    for lvl in range(1, MAX_LEVEL + 1):
        v = values_at(lvl)
        if all(v[k] >= targets[k] - 1e-9 for k in targets):
            return lvl
    return MAX_LEVEL


def gerar(before):
    rods = load('rods.json')
    ur = rods['upgrade_rules']
    ur['internal_levels']['max'] = MAX_LEVEL
    ur['bonus_curve_exponent'] = EXPOENTE
    ur['level_curve_version'] = 2
    ur['level_curve_note'] = (
        'Desde 10/10/2026 (M24-T13, A-156) as varas vão até o Nv.100. Cada vara guarda o bônus do Nv.1 '
        '(bonuses_at_level_1, o de antes) e o do Nv.100 (bonuses_at_max_level, ~3× o Nv.10 de antes); o bônus no nível N '
        'é Nv.1 + (Nv.100 − Nv.1) × ((N − 1) ÷ 99) ^ bonus_curve_exponent. Melhorar de N para N + 1 custa '
        'coins_first × coins_growth ^ (N − 1) Moedas e shells_first × shells_growth ^ (N − 1) Conchas, arredondados '
        '(abaixo de 1.000 para o inteiro; acima, para 3 algarismos). legacy_levels_v1 diz para qual nível novo vai uma '
        'vara que estava no nível 1 a 10 da curva antiga (o menor com bônus maior ou igual; level_curve_version 1). '
        'Números gerados por tools/Progressao/niveis_100.py.')
    ur['purchase_model'] = 'Pagar Moedas e Conchas para comprar o próximo nível diretamente.'
    for rod in rods['rods']:
        if not rod.get('has_internal_levels'):
            continue
        old = before['varas'][rod['id']]
        bpl = old['bonuses_per_level']
        at1 = collections.OrderedDict((k, bpl[k][0]) for k in C.AXES)
        atmax = collections.OrderedDict((k, round(bpl[k][-1] * MULT_NO_MAXIMO, 4)) for k in C.AXES)
        first_coins, first_shells = old['upgrade_costs'][0]
        cost = collections.OrderedDict([
            ('coins_first', C.nice_round(first_coins * VARA_MOEDAS_PRIMEIRO)),
            ('coins_growth', VARA_MOEDAS_CRESCIMENTO),
            ('shells_first', max(1, int(math.floor(first_shells * VARA_CONCHAS_PRIMEIRO + 0.5)))),
            ('shells_growth', VARA_CONCHAS_CRESCIMENTO),
        ])
        new = collections.OrderedDict()
        for k, v in rod.items():
            if k in ('bonuses_per_level', 'upgrade_costs', 'bonuses_at_level_1', 'bonuses_at_max_level', 'upgrade_cost', 'legacy_levels_v1'):
                continue
            new[k] = v
            if k == 'generates_shells':
                new['bonuses_at_level_1'] = at1
                new['bonuses_at_max_level'] = atmax
                new['upgrade_cost'] = cost
        rod.clear()
        rod.update(new)
        legacy = [first_level_at_least(lambda lvl, r=rod: C.rod_bonus(rods, r, lvl), {k: bpl[k][i] for k in C.AXES}) for i in range(len(bpl['catch_success']))]
        rod['legacy_levels_v1'] = legacy
    save('rods.json', rods)

    eq = load('equipment.json')
    starter = min(eq['boats'], key=lambda b: b['tier'])
    levels = collections.OrderedDict([
        ('max_level', MAX_LEVEL),
        ('bonus_curve_exponent', EXPOENTE),
        ('note', 'Desde 10/10/2026 (M24-T13, A-156) cada barco comprado tem nível, do 1 ao max_level. catch_success_bonus '
                 'é o bônus no Nv.1 (o de antes) e catch_success_bonus_at_max_level o do nível máximo (~3×); o bônus no '
                 'nível N é Nv.1 + (máx − Nv.1) × ((N − 1) ÷ (max_level − 1)) ^ bonus_curve_exponent. Melhorar de N para '
                 'N + 1 custa coins_first × coins_growth ^ (N − 1) Moedas e shells_first × shells_growth ^ (N − 1) '
                 'Conchas, arredondados como as varas. O barco inicial não tem nível (has_levels false): o bônus dele é 0. '
                 'Números gerados por tools/Progressao/niveis_100.py.'),
    ])
    new_eq = collections.OrderedDict()
    for k, v in eq.items():
        if k == 'boat_levels':
            continue
        new_eq[k] = v
        if k == 'description':
            new_eq['boat_levels'] = levels
    eq.clear()
    eq.update(new_eq)
    for boat in eq['boats']:
        base = before['barcos'][boat['id']]
        nb = collections.OrderedDict()
        for k, v in boat.items():
            if k in ('has_levels', 'catch_success_bonus_at_max_level', 'upgrade_cost'):
                continue
            nb[k] = v
            if k == 'catch_success_bonus':
                nb['catch_success_bonus'] = base
                if boat['id'] == starter['id']:
                    nb['has_levels'] = False
                else:
                    nb['has_levels'] = True
                    nb['catch_success_bonus_at_max_level'] = round(base * MULT_NO_MAXIMO, 4)
        if boat['id'] != starter['id']:
            nb['upgrade_cost'] = collections.OrderedDict([
                ('coins_first', C.nice_round(boat['cost_coins'] * BARCO_MOEDAS_PRIMEIRO)),
                ('coins_growth', BARCO_MOEDAS_CRESCIMENTO),
                ('shells_first', max(1, int(math.floor(boat['cost_shells'] * BARCO_CONCHAS_PRIMEIRO + 0.5)))),
                ('shells_growth', BARCO_CONCHAS_CRESCIMENTO),
            ])
        boat.clear()
        boat.update(nb)
    save('equipment.json', eq)

    prog = load('progression.json')
    fl = prog['fish_level']
    old_fish = before['peixes']
    at_max_pct = round(old_fish['stat_bonus_per_level_percent'] * (old_fish['max_level'] - 1) * MULT_NO_MAXIMO, 4)
    table = fish_xp_table()
    nf = collections.OrderedDict()
    nf['max_level'] = MAX_LEVEL
    nf['level_curve_version'] = 2
    nf['stat_bonus_at_max_level_percent'] = at_max_pct
    nf['stat_bonus_curve_exponent'] = EXPOENTE
    nf['stat_bonus_note'] = ('Desde 10/10/2026 (M24-T13, A-156): cada atributo derivado é multiplicado por (1 + bônus), '
                             'com bônus = %s%% × ((nível − 1) ÷ 99) ^ %s: 0 no Nv.1, pequenos passos no começo e +%s%% no '
                             'Nv.100 (3× os +36%% do Nv.10 de antes).' % (str(at_max_pct).replace('.', ','), str(EXPOENTE).replace('.', ','), str(at_max_pct).replace('.', ',')))
    nf['player_distributed_stat_points'] = fl.get('player_distributed_stat_points', False)
    nf['xp_curve_generator'] = collections.OrderedDict([
        ('formula', 'máximo(minimo, base × nível^expoente), arredondado para o múltiplo de 5 mais próximo'),
        ('base', XP_PEIXE_BASE),
        ('exponent', XP_PEIXE_EXPOENTE),
        ('minimum', XP_PEIXE_MINIMO),
    ])
    nf['total_xp_level_1_to_max'] = sum(table)
    fish_cfg = {'max_level': MAX_LEVEL, 'stat_bonus_at_max_level_percent': at_max_pct, 'stat_bonus_curve_exponent': EXPOENTE}
    legacy = []
    for old_level in range(1, old_fish['max_level'] + 1):
        target = old_fish['stat_bonus_per_level_percent'] / 100.0 * (old_level - 1)
        legacy.append(next(lvl for lvl in range(1, MAX_LEVEL + 1) if C.fish_stat_bonus(fish_cfg, lvl) >= target - 1e-9))
    nf['legacy_levels_v1'] = legacy
    nf['legacy_note'] = ('Para qual nível novo vai um peixe que estava no nível 1 a 10 da curva antiga (level_curve_version 1, '
                         '+4% por nível): o menor com bônus maior ou igual. O XP que ele tinha dentro do nível é somado de novo.')
    nf['xp_table'] = [collections.OrderedDict([('level', i + 1), ('xp_to_next_level', x)]) for i, x in enumerate(table)]
    prog['fish_level'] = nf
    # "Valuable food" warns from the level that matches the old Nv.2 (a Nv.2 fish is now 5 XP of food).
    prog['feeding']['valuable_feed_rules']['min_level'] = legacy[before['peixes_aviso_min_level'] - 1]
    save('progression.json', prog)

    arena = load('arena_bots.json')
    arena['strength_by_rank']['top_fish_level'] = legacy[before['arena_top_fish_level'] - 1]
    save('arena_bots.json', arena)

    market = load('market_bots.json')
    market['supply']['max_fish_level'] = legacy[before['mercado_max_fish_level'] - 1]
    rods_now = load('rods.json')
    market['supply']['max_rod_level'] = max(r['legacy_levels_v1'][-1] for r in rods_now['rods'] if r.get('legacy_levels_v1'))
    premium = before['mercado_fish_level_premium_per_level'] * (old_fish['max_level'] - 1) / float(legacy[-1] - 1)
    market['valuation']['fish_level_premium_per_level'] = round(premium, 4)
    save('market_bots.json', market)
    print('Gravado em config/: rods.json, equipment.json, progression.json, arena_bots.json, market_bots.json')


# ---------------------------------------------------------------- simulation

def make_game(days, max_tier=None):
    import simular_melhorias as M
    import simular_tripulacao as T

    class Game(M.Game):
        def __init__(self, crew_cfg, up_cfg):
            super().__init__(crew_cfg, up_cfg)
            p = self.p
            p.upgrade_ok = lambda coins: coins <= self.income() * SEGUNDOS_DE_RENDA
            self.item_levels = {}       # (kind, id, level) -> seconds
            self.bought_at = {}         # item id -> seconds
            self.shells_gained = 0.0
            self.shells_at = {}         # day -> shells gained so far
            fish = p.fish

            def counting_fish(attempts, t_days, xp_mult=1.0):
                before = p.shells
                left = fish(attempts, t_days, xp_mult)
                self.shells_gained += max(0.0, p.shells - before)
                return left

            p.fish = counting_fish
            if max_tier is not None:
                # A player who stays with one rod (a "medium" one) to see how fast it levels.
                next_rod = p.next_rod
                p.next_rod = lambda: (lambda r: r if r is not None and r['tier'] <= max_tier else None)(next_rod())

        def income(self):
            return self.crew.coins_rate() + self.fishing_coins_per_second()

        def observe(self):
            super().observe()
            p = self.p
            for rid, lvl in p.rods.items():
                self.bought_at.setdefault(rid, self.t)
                for target in (10, 30, 50, 100):
                    if lvl >= target:
                        self.item_levels.setdefault(('vara', rid, target), self.t)
            for bid, lvl in p.boat_levels.items():
                self.bought_at.setdefault(bid, self.t)
                for target in (10, 30, 50, 100):
                    if lvl >= target:
                        self.item_levels.setdefault(('barco', bid, target), self.t)
            d = int(self.t // 86400)
            self.shells_at.setdefault(d, self.shells_gained)

    crew_cfg = M.load_json('crew.json')
    up_cfg = M.load_json('upgrades.json')
    g = Game(crew_cfg, up_cfg)
    end = days * 86400.0
    while g.t < end:
        g.online(T.step_for(g.t) * 20, T.step_for(g.t))
    return g


def fmt_days(seconds):
    if seconds is None:
        return '—'
    d = seconds / 86400.0
    if d < 1 / 24.0:
        return ('%.0f min' % (d * 1440))
    if d < 1:
        return ('%.1f h' % (d * 24)).replace('.', ',')
    return ('%.1f dias' % d).replace('.', ',')


def curve_examples():
    rods = load('rods.json')
    eq = load('equipment.json')
    prog = load('progression.json')
    out = collections.OrderedDict()
    show = (1, 2, 10, 30, 50, 99, 100)
    for rod in rods['rods']:
        if not rod.get('has_internal_levels'):
            continue
        rows = collections.OrderedDict()
        for lvl in show:
            b = C.rod_bonus(rods, rod, lvl)
            c = C.rod_cost(rods, rod, lvl)
            spent = [0, 0]
            for l in range(1, lvl):
                cc = C.rod_cost(rods, rod, l)
                spent[0] += cc[0]
                spent[1] += cc[1]
            rows['Nv.%d' % lvl] = {
                'bonus': {k: round(b[k], 4) for k in C.AXES},
                'proximo_nivel': {'moedas': c[0], 'conchas': c[1]} if c else None,
                'gasto_desde_nv1': {'moedas': spent[0], 'conchas': spent[1]},
            }
        out[rod['id']] = {'nome': rod['display_name'], 'niveis': rows, 'migracao_nv1_a_10': rod['legacy_levels_v1']}
    boats = collections.OrderedDict()
    for boat in eq['boats']:
        if not boat.get('has_levels'):
            continue
        rows = collections.OrderedDict()
        for lvl in show:
            c = C.boat_cost(eq, boat, lvl)
            spent = [0, 0]
            for l in range(1, lvl):
                cc = C.boat_cost(eq, boat, l)
                spent[0] += cc[0]
                spent[1] += cc[1]
            rows['Nv.%d' % lvl] = {'bonus': round(C.boat_bonus(eq, boat, lvl), 4),
                                   'proximo_nivel': {'moedas': c[0], 'conchas': c[1]} if c else None,
                                   'gasto_desde_nv1': {'moedas': spent[0], 'conchas': spent[1]}}
        boats[boat['id']] = {'nome': boat['display_name'], 'niveis': rows}
    fl = prog['fish_level']
    table = [x['xp_to_next_level'] for x in fl['xp_table']]
    fish = collections.OrderedDict()
    for lvl in (1, 2, 5, 10, 20, 30, 40, 50, 75, 100):
        fish['Nv.%d' % lvl] = {'bonus_atributos': round(C.fish_stat_bonus(fl, lvl), 4),
                               'xp_proximo_nivel': table[lvl - 1] if lvl < fl['max_level'] else 0,
                               'xp_desde_nv1': sum(table[:lvl - 1]),
                               'comuns_de_39_xp': int(math.ceil(sum(table[:lvl - 1]) / 39.0))}
    return out, boats, fish, fl['legacy_levels_v1']


def focused(days):
    """Each rod alone: a player who stops buying rods at that one (boats as usual)."""
    out = collections.OrderedDict()
    rods = load('rods.json')
    for rod in rods['rods']:
        if not rod.get('has_internal_levels'):
            continue
        g = make_game(days, rod['tier'])
        t0 = g.bought_at.get(rod['id'])
        row = collections.OrderedDict([('comprada_em', fmt_days(t0))])
        for target in (10, 30, 50, 100):
            t = g.item_levels.get(('vara', rod['id'], target))
            row['Nv.%d' % target] = '%s (%s depois da compra)' % (fmt_days(t), fmt_days(t - t0)) if t is not None else '—'
        out[rod['id']] = row
        print('  só até %-8s %s' % (rod['id'], ' · '.join('%s: %s' % kv for kv in row.items())))
    return out


def simular(days, gravar):
    g = make_game(days)
    rods, boats, fish, fish_legacy = curve_examples()
    p = g.p
    items = collections.OrderedDict()
    for (kind, iid, target), t in sorted(g.item_levels.items(), key=lambda x: x[1]):
        key = '%s %s' % (kind, iid)
        row = items.setdefault(key, collections.OrderedDict([('comprada_em', fmt_days(g.bought_at.get(iid)))]))
        row['Nv.%d' % target] = '%s (%s depois da compra)' % (fmt_days(t), fmt_days(t - g.bought_at.get(iid, 0.0)))
    shells = collections.OrderedDict(('dia %d' % d, int(v)) for d, v in sorted(g.shells_at.items()) if d in (1, 2, 3, 7, 14, 30, 45, 60, 75))
    print('== Ritmo (jogo aberto o dia todo, Tripulação + Melhorias do M24, um nível quando custa até %d s de renda)' % SEGUNDOS_DE_RENDA)
    for k, row in items.items():
        print('  %-24s %s' % (k, ' · '.join('%s: %s' % kv for kv in row.items())))
    print('  Conchas ganhas (acumulado):', dict(shells))
    print('  Final: vara %s Nv.%d, barco %s Nv.%d, Nv. do Pescador %d' % (p.rod['id'], p.rods[p.rod['id']], p.boat['id'], p.boat_levels.get(p.boat['id'], 1), p.level))
    fisher = collections.OrderedDict(('Nv.%d' % l, round(p.reached[l], 2) if l in p.reached else None) for l in (100, 500, 1000))
    print('  Dias até o nível do Pescador:', dict(fisher))
    for rid, info in rods.items():
        print('  %s %s' % (rid, ' | '.join('%s: +%.1f%% sucesso, raridade +%.0f%%, próximo %s/%s' % (
            lvl, r['bonus']['catch_success'] * 100, r['bonus']['rarity_efficiency'] * 100,
            r['proximo_nivel']['moedas'] if r['proximo_nivel'] else '—', r['proximo_nivel']['conchas'] if r['proximo_nivel'] else '—')
            for lvl, r in info['niveis'].items() if lvl in ('Nv.1', 'Nv.10', 'Nv.30', 'Nv.50', 'Nv.100'))))
        print('     migração 1..10 →', info['migracao_nv1_a_10'])
    print('  peixes: migração 1..10 →', fish_legacy)
    for lvl, r in fish.items():
        print('    %s: +%.1f%%, XP desde Nv.1 %d (%d Comuns)' % (lvl, r['bonus_atributos'] * 100, r['xp_desde_nv1'], r['comuns_de_39_xp']))
    print('== Cada vara sozinha (o jogador não compra vara melhor que ela)')
    alone = focused(days)
    if gravar:
        result = collections.OrderedDict([
            ('gerado_por', 'tools/Progressao/niveis_100.py'),
            ('nota', 'Varas, barcos e peixes até o Nv.100 (M24-T13, A-156). Ritmo simulado com o perfil "jogo aberto o dia '
                     'todo" e a economia do M24 (Tripulação e Melhorias, simular_melhorias.py): o jogador compra o próximo '
                     'nível da vara ou do barco em uso (o mais barato primeiro) quando tem as Moedas e as Conchas e o nível '
                     'custa até %d segundos de renda (Tripulação + pesca); guarda Conchas para a próxima vara. As Conchas '
                     'só vêm da pesca.' % SEGUNDOS_DE_RENDA),
            ('parametros', collections.OrderedDict([
                ('nivel_maximo', MAX_LEVEL), ('expoente_da_curva', EXPOENTE), ('multiplicador_no_maximo', MULT_NO_MAXIMO),
                ('vara_moedas_primeiro_fracao_do_antigo', VARA_MOEDAS_PRIMEIRO), ('vara_moedas_crescimento', VARA_MOEDAS_CRESCIMENTO),
                ('vara_conchas_primeiro_fracao_do_antigo', VARA_CONCHAS_PRIMEIRO), ('vara_conchas_crescimento', VARA_CONCHAS_CRESCIMENTO),
                ('barco_moedas_primeiro_fracao_do_preco', BARCO_MOEDAS_PRIMEIRO), ('barco_moedas_crescimento', BARCO_MOEDAS_CRESCIMENTO),
                ('barco_conchas_primeiro_fracao_do_preco', BARCO_CONCHAS_PRIMEIRO), ('barco_conchas_crescimento', BARCO_CONCHAS_CRESCIMENTO),
                ('xp_peixe', {'base': XP_PEIXE_BASE, 'expoente': XP_PEIXE_EXPOENTE, 'minimo': XP_PEIXE_MINIMO}),
                ('segundos_de_renda_por_nivel', SEGUNDOS_DE_RENDA),
            ])),
            ('ritmo_jogo_aberto', items),
            ('cada_vara_sozinha', alone),
            ('conchas_ganhas_acumulado', shells),
            ('dias_ate_nivel_do_pescador', fisher),
            ('varas', rods),
            ('barcos', boats),
            ('peixes', fish),
            ('peixes_migracao_nv1_a_10', fish_legacy),
            ('antes', read_before()),
        ])
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
            f.write('\n')
        print('Gravado em', os.path.relpath(OUT, ROOT))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('acao', choices=('gerar', 'simular'))
    ap.add_argument('--dias', type=float, default=80)
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    if a.acao == 'gerar':
        before = read_before()
        if not os.path.exists(OUT):
            # Keep the old tables so the script can run again on the new format.
            os.makedirs(os.path.dirname(OUT), exist_ok=True)
            with open(OUT, 'w', encoding='utf-8') as f:
                json.dump({'antes': before}, f, ensure_ascii=False, indent=2)
                f.write('\n')
        gerar(before)
    else:
        simular(a.dias, a.gravar)


if __name__ == '__main__':
    main()
