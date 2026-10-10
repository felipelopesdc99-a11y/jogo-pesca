"""Simula a progressão do Pescador do Nv.1 ao nível máximo (hoje Nv.1000) com as regras e os números de /config.

Uso:  python3 tools/Progressao/simular_progressao.py [--escala-xp arquivo.json] [--saida relatorio.md]

O que ele faz:
  - joga, por valor esperado (sem sorte), cada tentativa de pesca como o serviço do jogo faz: espécie pelo
    peso do mapa (raridade da vara), chance de puxar (raridade + vara + barco + isca, entre 5% e 95%),
    tamanho (qualidade da vara e raridade), XP do Pescador, venda ao NPC e Conchas;
  - pesca online a cada 30 s e offline a cada 60 s, com o limite de 24 h, e gasta uma carga de isca por
    tentativa, online ou offline;
  - compra como um jogador cuidadoso: a próxima vara assim que tem o dinheiro (itens não têm nível mínimo desde
    M24-T09; guarda dinheiro para ela quando o mapa que a pede está a 8 níveis), depois barcos, níveis da vara e
    do barco (até o Nv.100, curva_niveis.py; o mais barato primeiro) e iscas (só quando a isca se paga);
  - vende tudo o que pesca ao NPC (é o máximo de Moedas sem Mercado) e sempre pesca no melhor mapa liberado;
  - repete para quatro jeitos de jogar (perfis) e escreve os dias até cada faixa de nível.

--escala-xp: um JSON {"map_05": 0.8, ...} que multiplica o XP do Pescador de todas as espécies daquele
mapa, para testar um ajuste antes de gravar em /config.
"""
import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import curva_niveis as C  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CONFIG = os.path.join(ROOT, 'config')


def load(name):
    with open(os.path.join(CONFIG, name), encoding='utf-8') as f:
        return json.load(f)


class Rules:
    def __init__(self, xp_scale=None):
        prog = load('progression.json')
        self.fishing = prog['fishing']
        self.tiers = {t['id']: t for t in prog['rarity']['tiers']}
        self.sizes = prog['size']['categories']
        self.influence = prog['size']['sale_value_influence']['influence']
        table = {e['level']: e['xp_to_next_level'] for e in prog['fisher']['xp_table']}
        self.max_level = prog['fisher']['max_level']
        self.xp_to_next = [0] + [table.get(l, 0) for l in range(1, self.max_level)]
        self.species = {s['id']: s for s in load('fish_catalog.json')['species']}
        self.maps = sorted(load('maps.json')['maps'], key=lambda m: m['unlock_fisher_level'])
        self.rods_cfg = load('rods.json')
        self.rods = sorted(self.rods_cfg['rods'], key=lambda r: r['tier'])
        # Fisher level of the first map that asks for each rod tier (items themselves have no level, M24-T09).
        self.rod_needed_at = {r['id']: min([m['unlock_fisher_level'] for m in self.maps if m['minimum_rod_tier'] >= r['tier']] or [10 ** 9])
                              for r in self.rods}
        eq = load('equipment.json')
        self.eq_cfg = eq
        self.boats = sorted(eq['boats'], key=lambda b: b['tier'])
        self.baits = eq['baits']
        econ = load('economy.json')
        sh = econ['shells']
        self.shell_chance = sh['base_drop_chance_per_catch']
        self.shell_amount = (sh['amount_per_drop']['min'] + sh['amount_per_drop']['max']) / 2.0
        self.price_mult = econ['npc_fish_sale']['price_multiplier']
        self.xp_scale = xp_scale or {}
        self._cache = {}

    def rod_bonus(self, rod, level):
        # Rods level up to Nv.100 on a curve (M24-T13, curva_niveis.py).
        return C.rod_bonus(self.rods_cfg, rod, level)

    def rod_max_level(self, rod):
        return C.rod_max_level(self.rods_cfg, rod)

    def boat_bonus(self, boat, level):
        return C.boat_bonus(self.eq_cfg, boat, level)

    def per_attempt(self, mp, rod, rod_level, gear):
        """Expected XP, coins and Shells of one attempt, and the chance it is pulled out."""
        key = (mp['id'], rod['id'], rod_level, round(gear, 4))
        if key in self._cache:
            return self._cache[key]
        b = self.rod_bonus(rod, rod_level)
        pool = []
        for e in mp['fish_pool']:
            s = self.species[e['species_id']]
            if e['catch_weight'] <= 0 or s['rarity'] not in mp['available_rarities'] or s['rarity'] not in rod['can_catch_rarities']:
                continue
            w = e['catch_weight'] * (1.0 if s['rarity'] == 'common' else 1.0 + b['rarity_efficiency'])
            pool.append((s, w))
        total = sum(w for _, w in pool)
        xp = coins = shells = caught = 0.0
        scale = self.xp_scale.get(mp['id'], 1.0)
        for s, w in pool:
            p = w / total
            tier = self.tiers[s['rarity']]
            chance = max(self.fishing['catch_success_min'], min(self.fishing['catch_success_max'], tier['catch_success_base'] + b['catch_success'] + gear))
            mults = tier.get('size_weight_multipliers', {})
            cw = [(c, (c['draw_weight'] * (1 + b['size_quality']) if c['id'] in ('large', 'exceptional', 'perfect') else c['draw_weight']) * mults.get(c['id'], 1.0)) for c in self.sizes]
            ct = sum(w2 for _, w2 in cw)
            xp_mult = sum(c['fisher_xp_multiplier'] * w2 for c, w2 in cw) / ct
            pct = sum((c['percentile_min'] + c['percentile_max']) / 2 * w2 for c, w2 in cw) / ct
            fish_xp = max(1.0, s['base_fisher_xp'] * scale * tier['fisher_xp_multiplier'] * xp_mult)
            price = max(1.0, s['base_sale_value_coins'] * tier['sale_value_multiplier'] * (1 + self.influence * (pct - 0.5)) * self.price_mult)
            shell = self.shell_chance * (1 + b['shell_yield']) * self.shell_amount if rod.get('generates_shells') else 0.0
            caught += p * chance
            xp += p * chance * fish_xp
            coins += p * chance * price
            shells += p * chance * min(1.0, shell / self.shell_amount) * self.shell_amount
        out = (xp, coins, shells, caught)
        self._cache[key] = out
        return out


class Player:
    def __init__(self, rules):
        self.r = rules
        self.level, self.xp = 1, 0.0
        self.coins, self.shells = 0.0, 0.0
        self.rods = {rules.rods[0]['id']: 1}
        self.rod = rules.rods[0]
        self.boat = rules.boats[0]
        self.boat_levels = {}    # boat id -> level (boats level up to Nv.100 since M24-T13)
        # Whether a level of a rod or boat that costs this many Moedas is worth it now (a simulator can limit it).
        self.upgrade_ok = lambda coins: True
        self.bait, self.bait_charges = None, 0
        self.reached = {1: 0.0}
        self.usage = {}          # (band of 10 levels, map id) -> attempts
        self.att = {}            # Fisher level -> fishing attempts made at it
        self.xp_at = {}          # Fisher level -> XP earned at it
        self.bought = {}         # item id -> day bought

    # ------------------------------------------------------------- where and with what
    def map(self):
        best = self.r.maps[0]
        for m in self.r.maps:
            if m['unlock_fisher_level'] <= self.level and m['minimum_rod_tier'] <= self.rod['tier']:
                best = m
        return best

    def gear(self):
        return self.boat_bonus() + (self.bait['catch_success_bonus'] if self.bait and self.bait_charges > 0 else 0.0)

    def boat_bonus(self):
        return self.r.boat_bonus(self.boat, self.boat_levels.get(self.boat['id'], 1))

    # ------------------------------------------------------------- fishing
    def fish(self, attempts, t_days, xp_mult=1.0):
        self.now = t_days
        while attempts > 0 and self.level < self.r.max_level:
            mp = self.map()
            lvl = self.rods[self.rod['id']]
            # bait runs out in the middle: split the batch there
            n = attempts if not (self.bait and self.bait_charges > 0) else min(attempts, self.bait_charges)
            xp, coins, shells, _ = self.r.per_attempt(mp, self.rod, lvl, self.gear())
            xp *= xp_mult
            # level ups inside the batch: stop at the next level
            need = self.r.xp_to_next[self.level] - self.xp
            steps = n if xp <= 0 else min(n, max(1, int(-(-need // xp))))
            key = ((self.level - 1) // 10, mp['id'])
            self.usage[key] = self.usage.get(key, 0) + steps
            self.att[self.level] = self.att.get(self.level, 0) + steps
            self.xp_at[self.level] = self.xp_at.get(self.level, 0.0) + xp * steps
            self.xp += xp * steps
            self.coins += coins * steps
            self.shells += shells * steps
            if self.bait and self.bait_charges > 0:
                self.bait_charges -= steps
            attempts -= steps
            while self.level < self.r.max_level and self.xp >= self.r.xp_to_next[self.level]:
                self.xp -= self.r.xp_to_next[self.level]
                self.level += 1
                self.reached[self.level] = t_days
        return attempts

    # ------------------------------------------------------------- shopping
    def next_rod(self):
        for rod in self.r.rods:
            if rod['tier'] > self.rod['tier']:
                return rod
        return None

    def shop(self):
        r = self.r
        reserve_c = reserve_s = 0.0
        nxt = self.next_rod()
        if nxt:
            acq = nxt['acquisition']
            if self.coins >= acq['purchase_cost_coins'] and self.shells >= acq.get('purchase_cost_shells', 0):
                self.coins -= acq['purchase_cost_coins']
                self.shells -= acq.get('purchase_cost_shells', 0)
                self.rods[nxt['id']] = 1
                self.rod = nxt
                self.bought[nxt['id']] = getattr(self, 'now', 0.0)
                nxt = self.next_rod()
            if nxt and r.rod_needed_at[nxt['id']] <= self.level + 8:
                reserve_c = nxt['acquisition']['purchase_cost_coins']
                reserve_s = nxt['acquisition'].get('purchase_cost_shells', 0)
        # boats
        for boat in r.boats:
            if boat['tier'] > self.boat['tier'] \
                    and self.coins - reserve_c >= boat['cost_coins'] and self.shells - reserve_s >= boat['cost_shells']:
                self.coins -= boat['cost_coins']
                self.shells -= boat['cost_shells']
                self.boat = boat
                self.boat_levels.setdefault(boat['id'], 1)
                self.bought[boat['id']] = getattr(self, 'now', 0.0)
        # rod and boat levels (curva_niveis.py): the cheaper of the two next levels first
        while True:
            options = []
            u = C.rod_cost(r.rods_cfg, self.rod, self.rods[self.rod['id']])
            if u:
                options.append((u, 'rod'))
            u = C.boat_cost(r.eq_cfg, self.boat, self.boat_levels.get(self.boat['id'], 1))
            if u:
                options.append((u, 'boat'))
            if not options:
                break
            (coins, shells), what = min(options)
            if self.coins - reserve_c < coins or self.shells - reserve_s < shells or not self.upgrade_ok(coins):
                break
            self.coins -= coins
            self.shells -= shells
            if what == 'rod':
                self.rods[self.rod['id']] += 1
            else:
                self.boat_levels[self.boat['id']] = self.boat_levels.get(self.boat['id'], 1) + 1
        # bait: the best one that pays for itself in coins
        if self.bait_charges <= 0:
            self.bait = None
            mp = self.map()
            lvl = self.rods[self.rod['id']]
            base = r.per_attempt(mp, self.rod, lvl, self.boat_bonus())
            best = None
            for bait in sorted(r.baits, key=lambda b: -b['catch_success_bonus']):
                with_bait = r.per_attempt(mp, self.rod, lvl, self.boat_bonus() + bait['catch_success_bonus'])
                gain = with_bait[1] - base[1]
                cost = bait['cost_coins'] / bait['charges']
                if gain >= cost and self.coins - reserve_c >= bait['cost_coins'] and self.shells - reserve_s >= bait['cost_shells']:
                    best = bait
                    break
            if best:
                self.coins -= best['cost_coins']
                self.shells -= best['cost_shells']
                self.bait, self.bait_charges = best, best['charges'] * 5   # a few packs at once
                self.coins -= best['cost_coins'] * 4
                self.shells -= best['cost_shells'] * 4
                if self.coins < 0 or self.shells < 0:   # could not afford five packs: one is enough
                    self.coins += best['cost_coins'] * 4
                    self.shells += best['cost_shells'] * 4
                    self.bait_charges = best['charges']


PROFILES = {
    # name: (description, list of (start hour, minutes online) per day)
    'sempre_aberto': ('Deixa o jogo aberto o dia todo (só online)', None),
    'quatro_vezes': ('Abre 4 vezes por dia, 30 min de cada vez (2 h online; o resto offline)', [(8, 30), (12, 30), (18, 30), (22, 30)]),
    'duas_vezes': ('Abre 2 vezes por dia, 1 h de cada vez (2 h online; o resto offline)', [(8, 60), (20, 60)]),
    'uma_vez': ('Abre 1 vez por dia, 30 min (o resto offline)', [(20, 30)]),
}
# The same pace as 'quatro_vezes' (2 h online + 22 h offline a day) spread evenly over the day, without
# the jumps of a session: used to calibrate, because the level is then measured at any minute.
SMOOTH = 'quatro_vezes'


def play(rules, profile, max_days=400):
    p = Player(rules)
    online_per_min = 60.0 / rules.fishing['online_cycle_seconds']
    offline_per_min = 60.0 / rules.fishing['offline_cycle_seconds']
    cap_min = rules.fishing['offline_accumulation_cap_hours'] * 60
    if profile == 'continuo':
        per_day = 2 * 60 * online_per_min + 22 * 60 * offline_per_min
        t = 0.0
        while p.level < rules.max_level and t < max_days * 1440:
            p.shop()
            p.fish(int(round(per_day * 15 / 1440)), (t + 15) / 1440)
            t += 15
        return p
    sessions = PROFILES[profile][1]
    if sessions is None:
        t = 0.0
        while p.level < rules.max_level and t < max_days * 1440:
            p.shop()
            p.fish(int(15 * online_per_min), (t + 15) / 1440)
            t += 15
        return p
    last_end = None
    for day in range(max_days):
        for start_h, minutes in sessions:
            start = day * 1440 + start_h * 60
            if last_end is not None:
                away = min(start - last_end, cap_min)
                p.fish(int(away * offline_per_min), start / 1440, getattr(rules, 'offline_xp_mult', 1.0))
            p.shop()
            for k in range(0, minutes, 10):
                p.fish(int(min(10, minutes - k) * online_per_min), (start + k + 10) / 1440)
                p.shop()
            last_end = start + minutes
            if p.level >= rules.max_level:
                return p
    return p


MILESTONES = (10, 50, 100, 250, 500, 750, 1000)


def bands(p, levels=None):
    """Days until each milestone level (None if not reached)."""
    return [p.reached.get(lvl) for lvl in (levels or [l for l in MILESTONES if l <= p.r.max_level])]


def fmt(d):
    if d is None:
        return '—'
    if d < 1 / 24.0:
        return ('%.0f min' % (d * 1440)).replace('.', ',')
    if d < 1:
        return ('%.1f h' % (d * 24)).replace('.', ',')
    return ('%.1f d' % d).replace('.', ',')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--escala-xp')
    ap.add_argument('--saida')
    ap.add_argument('--vip', action='store_true', help='simula o VIP: XP offline multiplicado por 1 + economy.json vip.offline_fisher_xp_bonus (A-110)')
    a = ap.parse_args()
    scale = json.load(open(a.escala_xp)) if a.escala_xp else None
    rules = Rules(scale)
    if a.vip:
        econ = json.load(open(os.path.join(CONFIG, 'economy.json'), encoding='utf-8'))
        rules.offline_xp_mult = 1.0 + econ['vip']['offline_fisher_xp_bonus']
    rows = []
    for name, (desc, _) in PROFILES.items():
        p = play(rules, name)
        rows.append((name, desc, bands(p), p))
    print('Dias até cada nível (a partir do início do jogo):')
    print('%-14s ' % 'perfil' + ' '.join('%7s' % ('Nv.%d' % l) for l in MILESTONES if l <= rules.max_level))
    for name, desc, b, p in rows:
        print('%-14s ' % name + ' '.join('%7s' % fmt(x) for x in b) + '   vara %s nv %d, barco %s' % (p.rod['id'], p.rods[p.rod['id']], p.boat['id']))
    if a.saida:
        with open(a.saida, 'w', encoding='utf-8') as f:
            json.dump({name: {'descricao': desc, 'dias': b} for name, desc, b, _ in rows}, f, ensure_ascii=False, indent=2)


if __name__ == '__main__':
    main()
