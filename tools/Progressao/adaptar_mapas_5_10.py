"""Adapta a proposta docs/PROGRESSAO_MAPAS_5_A_10.md às regras atuais do jogo (05/10/2026).

Lê as tabelas de espécies e de varas do documento do proprietário e grava
docs/propostas/mapas_5_10.json com os números adaptados (nada em /config muda).
Regras de adaptação (explicadas em docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md):
  - venda e XP de alimento: um fator por métrica e raridade, calculado para o Mapa 5 render as
    metas da seção 9 sobre o Mapa 4 real; o mesmo fator vale nos Mapas 6 a 10, então a escada
    entre mapas e a proporção entre espécies do documento ficam iguais; Lendário e Mítico usam o
    fator do Épico;
  - XP do Pescador: a curva de XP do jogo não muda (seção 9); o XP de cada mapa é calibrado para a
    faixa de 10 níveis dele levar o mesmo tempo online que a faixa 30–40 leva hoje no Mapa 4,
    mantendo a proporção entre as espécies do documento; trava: em cada raridade, o XP médio de um
    mapa nunca fica abaixo de 1,05× o do mapa anterior (peixe de mapa maior sempre vale mais XP),
    então as últimas faixas podem ficar um pouco mais curtas;
  - atributos: só sobem quando o documento deixaria o Mapa 5 mais fraco que o Mapa 4 (+15%);
    Velocidade não muda;
  - chance de puxar das varas: um terço menor (A-095, o documento usou a escala antiga);
  - Conchas em toda compra e melhoria de vara (A-099);
  - preço das varas em Moedas: multiplicado pelo quanto a renda por peixe subiu no mapa onde o
    jogador junta dinheiro para ela, para o tempo de compra continuar o que o documento pensou.
Uso: python3 tools/Progressao/adaptar_mapas_5_10.py
"""
import json, os, re, statistics as st

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DOC = os.path.join(ROOT, 'docs', 'PROGRESSAO_MAPAS_5_A_10.md')
OUT = os.path.join(ROOT, 'docs', 'propostas', 'mapas_5_10.json')

XP_STEP_MIN = 1.00                                          # XP médio por raridade: mínimo × mapa anterior
TARGET = {'sale': 1.50, 'fisher_xp': 1.30, 'feed_xp': 1.30}   # Mapa 5 sobre o Mapa 4 (seção 9)
STAT_FLOOR = 1.15                                           # Mapa 5 nunca mais fraco que Mapa 4 × 1,15
SUCCESS_CUT = 2.0 / 3.0                                     # A-095
NEW_RARITIES = [
    {'id': 'legendary', 'display_name': 'Lendário', 'catch_success_base': 0.14, 'stat_multiplier': 1.55, 'fisher_xp_multiplier': 9.0,
     'feed_xp_multiplier': 6.0, 'sale_value_multiplier': 8.0, 'size_weight_multipliers': {'large': 0.6, 'exceptional': 0.35, 'perfect': 0.35}, 'color': '#E5484D'},
    {'id': 'mythic', 'display_name': 'Mítico', 'catch_success_base': 0.08, 'stat_multiplier': 1.85, 'fisher_xp_multiplier': 15.0,
     'feed_xp_multiplier': 10.0, 'sale_value_multiplier': 14.0, 'size_weight_multipliers': {'large': 0.5, 'exceptional': 0.25, 'perfect': 0.25}, 'color': '#EC4899'},
]
# Equipamento esperado em cada mapa: (vara, nível da vara, bônus do barco da faixa).
GEAR = {'map_04': ('rod_02', 5, 0.06), 'map_05': ('rod_02', 10, 0.06), 'map_06': ('rod_03', 3, 0.08), 'map_07': ('rod_03', 8, 0.08),
        'map_08': ('rod_04', 3, 0.08), 'map_09': ('rod_04', 8, 0.10), 'map_10': ('rod_05', 5, 0.10)}
RARITY = {'Comum': 'common', 'Raro': 'rare', 'Épico': 'epic', 'Lendário': 'legendary', 'Mítico': 'mythic'}

def num(s):
    return float(s.replace('.', '').replace(',', '.'))

def parse_doc():
    text = open(DOC, encoding='utf-8').read()
    maps, cur = {}, None
    for line in text.splitlines():
        m = re.match(r'## Mapa (\d+) — (.+)', line)
        if m and int(m.group(1)) >= 5:
            cur = 'map_%02d' % int(m.group(1)); maps.setdefault(cur, [])
            continue
        if line.startswith('# 6.'):
            cur = None
        if cur and line.startswith('| `'):
            c = [x.strip() for x in line.strip('|').split('|')]
            lo, hi = c[3].split('–')
            maps[cur].append({
                'id': c[0].strip('`'), 'display_name': c[1], 'rarity': RARITY[c[2].strip('*')],
                'size_cm': {'min': int(lo), 'max': int(hi)},
                'base_stats': {'hp': int(num(c[4])), 'attack': int(num(c[5])), 'defense': int(num(c[6])), 'speed': int(num(c[7]))},
                'sale': num(c[8]), 'feed_xp': num(c[9]), 'fisher_xp': num(c[10]), 'catch_weight': int(num(c[11])),
            })
    return maps

def main():
    cat = json.load(open(os.path.join(ROOT, 'config', 'fish_catalog.json'), encoding='utf-8'))['species']
    prog = json.load(open(os.path.join(ROOT, 'config', 'progression.json'), encoding='utf-8'))
    sale_mult = {t['id']: t['sale_value_multiplier'] for t in prog['rarity']['tiers']}
    sale_mult.update({'legendary': 8.0, 'mythic': 14.0})
    doc = parse_doc()
    assert sorted(doc) == ['map_%02d' % i for i in range(5, 11)] and all(len(v) == 10 for v in doc.values())
    taken = {s['id'] for s in cat}
    clash = [f['id'] for v in doc.values() for f in v if f['id'] in taken]
    assert not clash, clash

    m4 = [s for s in cat if s['primary_map_id'] == 'map_04']
    real = lambda r, k: st.mean(s[k] for s in m4 if s['rarity'] == r)
    keys = {'sale': 'base_sale_value_coins', 'fisher_xp': 'base_fisher_xp', 'feed_xp': 'base_feed_xp'}
    factor = {}
    for r in ('common', 'rare', 'epic'):
        d5 = [f for f in doc['map_05'] if f['rarity'] == r]
        for k in keys:
            factor[(k, r)] = TARGET[k] * real(r, keys[k]) / st.mean(f[k] for f in d5)
        for s in ('hp', 'attack', 'defense'):
            want = STAT_FLOOR * st.mean(x['base_stats'][s] for x in m4 if x['rarity'] == r)
            have = st.mean(f['base_stats'][s] for f in d5 if True)
            factor[(s, r)] = max(1.0, want / have)
    for r in ('legendary', 'mythic'):
        for k in list(keys) + ['hp', 'attack', 'defense']:
            factor[(k, r)] = factor[(k, 'epic')]

    species, ev_doc, ev_new = [], {}, {}
    for mid, fish in doc.items():
        tot = sum(f['catch_weight'] for f in fish)
        ev_doc[mid] = sum(f['catch_weight'] / tot * f['sale'] * sale_mult[f['rarity']] for f in fish)
        e = 0.0
        for f in fish:
            r = f['rarity']
            sale = int(round(f['sale'] * factor[('sale', r)], -1))
            e += f['catch_weight'] / tot * sale * sale_mult[r]
            species.append({
                'id': f['id'], 'display_name': f['display_name'], 'primary_map_id': mid, 'rarity': r,
                'size_cm': f['size_cm'],
                'base_stats': {s: (int(round(f['base_stats'][s] * factor[(s, r)])) if s != 'speed' else f['base_stats'][s]) for s in f['base_stats']},
                'base_sale_value_coins': sale,
                'base_feed_xp': int(round(f['feed_xp'] * factor[('feed_xp', r)])),
                'base_fisher_xp': int(round(f['fisher_xp'] * factor[('fisher_xp', r)])),
                'catch_weight': f['catch_weight'],
            })
        ev_new[mid] = e
    income = {m: ev_new[m] / ev_doc[m] for m in ev_doc}

    def coins(x, k):
        return int(round(x * k, -4 if x * k >= 100000 else -3))
    up_shell_shape = [10, 12, 15, 18, 22, 26, 30, 35, 40]          # Maré Dourada (A-099)
    rods_doc = {
        'rod_03': {'name': 'Corrente Mestra', 'unlock': 50, 'buy': 450000, 'buy_shells': None, 'farm': 'map_05', 'use': ['map_06', 'map_07'],
                   'maps': ['map_06', 'map_07'], 'rarities': ['common', 'rare', 'epic', 'legendary'],
                   'success': [12, 13, 14, 15, 16, 18, 19, 21, 22, 24],
                   'rarity_efficiency': [0.52, 0.55, 0.58, 0.61, 0.64, 0.67, 0.70, 0.73, 0.76, 0.80],
                   'size_quality': [0.44, 0.46, 0.48, 0.50, 0.52, 0.54, 0.56, 0.58, 0.60, 0.62],
                   'shell_yield': [1.15, 1.22, 1.29, 1.36, 1.43, 1.50, 1.57, 1.64, 1.70, 1.75],
                   'upgrades': [60000, 90000, 135000, 200000, 300000, 450000, 675000, 1000000, 1500000]},
        'rod_04': {'name': 'Atlântico Nobre', 'unlock': 70, 'buy': 1800000, 'buy_shells': 180, 'farm': 'map_07', 'use': ['map_08', 'map_09'],
                   'maps': ['map_08', 'map_09'], 'rarities': ['common', 'rare', 'epic', 'legendary'],
                   'success': [18, 19, 21, 22, 24, 25, 27, 29, 30, 32],
                   'rarity_efficiency': [0.82, 0.85, 0.88, 0.92, 0.95, 0.98, 1.02, 1.05, 1.08, 1.12],
                   'size_quality': [0.64, 0.66, 0.68, 0.70, 0.72, 0.74, 0.76, 0.78, 0.80, 0.82],
                   'shell_yield': [1.80, 1.88, 1.96, 2.04, 2.12, 2.20, 2.28, 2.36, 2.43, 2.50],
                   'upgrades': [250000, 375000, 560000, 840000, 1250000, 1900000, 2850000, 4250000, 6400000]},
        'rod_05': {'name': 'Soberana Abissal', 'unlock': 90, 'buy': 8000000, 'buy_shells': 650, 'farm': 'map_09', 'use': ['map_10'],
                   'maps': ['map_10'], 'rarities': ['common', 'rare', 'epic', 'legendary', 'mythic'],
                   'success': [25, 27, 29, 31, 33, 34, 36, 37, 39, 40],
                   'rarity_efficiency': [1.15, 1.19, 1.23, 1.27, 1.31, 1.35, 1.39, 1.43, 1.47, 1.50],
                   'size_quality': [0.85, 0.87, 0.89, 0.92, 0.94, 0.96, 0.98, 1.00, 1.03, 1.05],
                   'shell_yield': [2.60, 2.70, 2.80, 2.90, 3.00, 3.10, 3.20, 3.30, 3.40, 3.50],
                   'upgrades': [1000000, 1500000, 2250000, 3400000, 5100000, 7600000, 11400000, 17000000, 25000000]},
    }
    rods = []
    for rid, r in rods_doc.items():
        buy_shells = r['buy_shells'] if r['buy_shells'] else int(round((40 * 180) ** 0.5 / 5.0)) * 5   # entre Maré Dourada (40) e Atlântico Nobre (180)
        k_buy = income[r['farm']]
        k_up = st.mean(income[m] for m in r['use'])
        rods.append({
            'id': rid, 'display_name': r['name'], 'unlock_fisher_level': r['unlock'], 'compatible_maps': r['maps'],
            'can_catch_rarities': r['rarities'],
            'purchase_cost_coins': coins(r['buy'], k_buy), 'purchase_cost_shells': buy_shells,
            'bonuses_per_level': {
                'rarity_efficiency': r['rarity_efficiency'], 'size_quality': r['size_quality'], 'shell_yield': r['shell_yield'],
                'catch_success': [round(x / 100.0 * SUCCESS_CUT, 3) for x in r['success']],
            },
            'upgrade_costs': [{'to_level': i + 2, 'cost_coins': coins(c, k_up), 'cost_shells': int(round(sh * buy_shells / 40.0))}
                              for i, (c, sh) in enumerate(zip(r['upgrades'], up_shell_shape))],
            'doc_values': {'purchase_cost_coins': r['buy'], 'purchase_cost_shells': r['buy_shells'], 'catch_success_pp': r['success'], 'upgrade_costs_coins': r['upgrades']},
        })

    # ---- XP do Pescador calibrado por faixa (estimativa simples: 120 tentativas por hora online,
    # equipamento esperado em cada faixa, sem isca e sem efeito de tamanho).
    rods_cfg = json.load(open(os.path.join(ROOT, 'config', 'rods.json'), encoding='utf-8'))['rods']
    bonus = {r['id']: r.get('bonuses_per_level') for r in rods_cfg}
    for r in rods:
        bonus[r['id']] = r['bonuses_per_level']
    tiers = {t['id']: t for t in prog['rarity']['tiers']}
    tiers.update({t['id']: t for t in NEW_RARITIES})
    maps_cfg = json.load(open(os.path.join(ROOT, 'config', 'maps.json'), encoding='utf-8'))['maps']
    by_id = {x['id']: x for x in cat}
    by_id.update({x['id']: x for x in species})
    pools = {m['id']: [(f['species_id'], f['catch_weight']) for f in m['fish_pool']] for m in maps_cfg}
    for mid in doc:
        pools[mid] = [(x['id'], x['catch_weight']) for x in species if x['primary_map_id'] == mid]

    def per_attempt(mid):
        rod, lv, boat = GEAR[mid]
        b = bonus[rod]
        re_, cs = b['rarity_efficiency'][lv - 1], b['catch_success'][lv - 1]
        w = [(sid, wt * (1 + re_) if by_id[sid]['rarity'] != 'common' else wt) for sid, wt in pools[mid]]
        tot = sum(x for _, x in w)
        coins = xp = 0.0
        for sid, wt in w:
            r = by_id[sid]['rarity']
            p = min(0.95, max(0.05, tiers[r]['catch_success_base'] + cs + boat))
            coins += wt / tot * p * by_id[sid]['base_sale_value_coins'] * tiers[r]['sale_value_multiplier']
            xp += wt / tot * p * by_id[sid]['base_fisher_xp'] * tiers[r]['fisher_xp_multiplier']
        return coins, xp

    xp_table = {e['level']: e['xp_to_next_level'] for e in prog['fisher']['xp_table']}
    band = lambda a: sum(xp_table[l] for l in range(a, min(a + 10, 100)))
    target_hours = band(30) / per_attempt('map_04')[1] / 120.0
    chain = {}
    def avg_xp(mid, r):
        v = [by_id[sid]['base_fisher_xp'] for sid, _ in pools[mid] if by_id[sid]['rarity'] == r]
        return sum(v) / len(v) if v else None

    prev_mid = 'map_04'
    for i, mid in enumerate(sorted(doc)):
        start = 40 + 10 * i
        k = band(start) / (target_hours * 120.0) / per_attempt(mid)[1]
        for r in ('common', 'rare', 'epic', 'legendary'):
            a, b_ = avg_xp(mid, r), avg_xp(prev_mid, r)
            if a and b_:
                k = max(k, XP_STEP_MIN * b_ / a)
        prev_mid = mid
        for x in species:
            if x['primary_map_id'] == mid:
                x['base_fisher_xp'] = max(1, int(round(x['base_fisher_xp'] * k)))
    prev = per_attempt('map_04')
    for i, mid in enumerate(sorted(doc)):
        c, x = per_attempt(mid)
        chain[mid] = {'niveis': '%d-%d' % (40 + 10 * i, 50 + 10 * i), 'moedas_por_tentativa': round(c), 'xp_por_tentativa': round(x, 1),
                      'moedas_x_anterior': round(c / prev[0], 2), 'xp_x_anterior': round(x / prev[1], 2),
                      'horas_online_na_faixa': round(band(40 + 10 * i) / x / 120.0, 1)}
        prev = (c, x)

    out = {
        'note': 'Proposta adaptada (não está em /config). Gerado por tools/Progressao/adaptar_mapas_5_10.py a partir de docs/PROGRESSAO_MAPAS_5_A_10.md.',
        'factors': {'%s/%s' % k: round(v, 3) for k, v in sorted(factor.items())},
        'gear_assumed': {m: {'vara': g[0], 'nivel_vara': g[1], 'barco_bonus': g[2]} for m, g in GEAR.items()},
        'target_hours_per_band': round(target_hours, 2),
        'chain': chain,
        'income_per_catch_vs_doc': {m: round(v, 3) for m, v in income.items()},
        'expected_sale_per_catch': {m: round(ev_new[m]) for m in ev_new},
        'rarities_new': NEW_RARITIES,
        'species': species,
        'rods': rods,
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, indent=2); f.write('\n')
    print('Gravado:', os.path.relpath(OUT, ROOT))
    return out

if __name__ == '__main__':
    o = main()
    print(json.dumps(o['factors'], ensure_ascii=False))
    print('renda por peixe (×doc):', o['income_per_catch_vs_doc'])
    print('horas por faixa (meta):', o['target_hours_per_band'])
    for m, v in o['chain'].items():
        print(m, v)
    print('venda esperada por peixe:', o['expected_sale_per_catch'])
    for r in o['rods']:
        print(r['id'], r['purchase_cost_coins'], r['purchase_cost_shells'], r['bonuses_per_level']['catch_success'][0], r['bonuses_per_level']['catch_success'][-1], [u['cost_coins'] for u in r['upgrade_costs']][-1], [u['cost_shells'] for u in r['upgrade_costs']])
