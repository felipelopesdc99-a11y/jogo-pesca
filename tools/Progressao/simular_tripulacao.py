"""Simula a Tripulação automática (M24-T05, A-154) junto com a pesca, com os números de /config.

Uso:  python3 tools/Progressao/simular_tripulacao.py [--dias 75] [--gravar]

O que ele faz:
  - joga a pesca como tools/Progressao/simular_progressao.py (valor esperado, sem sorte): pesca online a cada 30 s,
    offline a cada 60 s até 24 h, compra varas, barcos, melhorias e iscas, vende tudo ao NPC;
  - a Tripulação (config/crew.json) rende Moedas e XP de Pescador por segundo, com o jogo aberto e fechado
    (offline: 100% nas primeiras horas, depois a taxa reduzida, até o teto), exatamente como o serviço do jogo:
    custo b·r^k·(r^n−1)/(r−1), cada marco de quantidade ×2 para aquele tripulante, marco da frota ×2 para todos;
  - a carteira é uma só: Moedas da pesca e da Tripulação pagam os equipamentos e os tripulantes;
  - compra tripulantes como um jogador atento: sempre a compra que mais aumenta a renda por Moeda gasta (1 unidade,
    até o próximo marco, até liberar o próximo tripulante ou até o próximo marco da frota), só com o jogo aberto;
  - roda três jeitos de jogar (sempre aberto, 4 vezes por dia, 1 vez por dia) e mede: a primeira compra, o segundo
    tripulante, quando a renda da Tripulação passa a da pesca, a renda em cada dia e o nível do Pescador com e sem
    o XP da Tripulação.

Com --gravar, escreve o resultado em docs/propostas/tripulacao.json.
"""
import argparse
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import simular_progressao as S  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, 'docs', 'propostas', 'tripulacao.json')

PROFILES = {
    'sempre_aberto': ('Deixa o jogo aberto o dia todo', None),
    'quatro_vezes': ('Abre 4 vezes por dia, 30 min de cada vez', [(8, 30), (12, 30), (18, 30), (22, 30)]),
    'uma_vez': ('Abre 1 vez por dia, 30 min', [(20, 30)]),
}
CHECK_DAYS = (1, 3, 7, 17.5, 30, 45, 60, 75, 100, 144, 168)
RATE_THRESHOLDS = (1e3, 1e6, 1e9, 1e12)
LONG_MAX = 9223372036854775807


class Crew:
    """Same rules as client-unity/Assets/Scripts/GameService/Crew/CrewRules.cs."""

    def __init__(self, cfg):
        self.cfg = cfg
        self.members = cfg['members']
        self.n = [0] * len(self.members)
        self.counts = cfg['milestones']['counts']
        self.mult = cfg['milestones']['multiplier']
        self.fleet_counts = cfg['fleet_milestones']['counts']
        self.fleet_mult = cfg['fleet_milestones']['multiplier']
        self.unlock = cfg['unlock_previous_count']
        off = cfg['offline']
        self.full_s = off['full_rate_hours'] * 3600.0
        self.reduced = off['reduced_rate']
        self.max_s = off['max_hours'] * 3600.0

    def cost(self, i, n, k):
        m = self.members[i]
        r = m['cost_growth']
        c = m['base_cost'] * r ** n * (r ** k - 1) / (r - 1)
        return c if c < LONG_MAX else float('inf')

    def unlocked(self, i, n=None):
        n = n or self.n
        return i == 0 or n[i - 1] >= self.unlock

    def reached(self, units, counts):
        return sum(1 for c in counts if units >= c)

    def fleet_level(self, n=None):
        n = n or self.n
        return self.reached(min(n), self.fleet_counts)

    def coins_rate(self, n=None):
        n = n or self.n
        fleet = self.fleet_mult ** self.fleet_level(n)
        return sum(m['coins_per_second'] * n[i] * self.mult ** self.reached(n[i], self.counts) * fleet
                   for i, m in enumerate(self.members))

    def member_rate(self, i):
        return self.members[i]['coins_per_second'] * self.n[i] * self.mult ** self.reached(self.n[i], self.counts) \
            * self.fleet_mult ** self.fleet_level()

    def xp_rate(self, n=None):
        n = n or self.n
        return sum(m['xp_per_second'] * n[i] for i, m in enumerate(self.members))

    def offline_seconds(self, away_s):
        full = min(away_s, self.full_s)
        reduced = max(0.0, min(away_s, self.max_s) - self.full_s)
        return full + reduced * self.reduced

    def options(self):
        """(cost, rate gain, new counts) of the purchases a careful player considers."""
        base = self.coins_rate()
        out = []
        for i in range(len(self.members)):
            if not self.unlocked(i):
                continue
            n = self.n[i]
            targets = {n + 1}
            nxt = [c for c in self.counts if c > n]
            if nxt:
                targets.add(nxt[0])
            if i + 1 < len(self.members) and n < self.unlock and not self.unlocked(i + 1):
                targets.add(self.unlock)
            for t in targets:
                new = self.n[:]
                new[i] = t
                cost = self.cost(i, n, t - n)
                gain = self.coins_rate(new) - base
                if i + 1 < len(self.members) and t >= self.unlock > n:
                    # Unlocking the next one: count its first unit too.
                    cost += self.members[i + 1]['base_cost']
                    new[i + 1] = max(new[i + 1], 1)
                    gain = self.coins_rate(new) - base
                out.append((cost, gain, new))
        if all(self.unlocked(i) for i in range(len(self.members))):
            nxt = [c for c in self.fleet_counts if c > min(self.n)]
            if nxt:
                new = [max(x, nxt[0]) for x in self.n]
                cost = sum(self.cost(i, self.n[i], new[i] - self.n[i]) for i in range(len(self.n)))
                out.append((cost, self.coins_rate(new) - base, new))
        return [o for o in out if o[0] < float('inf') and o[1] > 0]


class Game:
    def __init__(self, crew_cfg, crew_xp=True):
        self.rules = S.Rules()
        self.p = S.Player(self.rules)
        self.crew = Crew(crew_cfg)
        self.crew_xp = crew_xp
        self.t = 0.0                 # seconds
        self.first_buy = None
        self.second_member = None
        self.passes_fishing = None
        self.units_bought = 0
        self.max_rate = 0.0
        self.snap = {}
        self.carry = 0.0
        self.start = None            # first moment the game is open (the first session of a profile)
        self.rate_at = {}            # threshold (coins/s) -> first time the Crew earns that much

    def add_xp(self, xp):
        p, r = self.p, self.rules
        p.xp += xp
        while p.level < r.max_level and p.xp >= r.xp_to_next[p.level]:
            p.xp -= r.xp_to_next[p.level]
            p.level += 1
            p.reached[p.level] = self.t / 86400.0

    def fishing_coins_per_second(self):
        p = self.p
        _, coins, _, _ = self.rules.per_attempt(p.map(), p.rod, p.rods[p.rod['id']], p.gear())
        return coins / self.rules.fishing['online_cycle_seconds']

    def buy(self):
        c = self.crew
        for _ in range(500):
            opts = c.options()
            if not opts:
                return
            best = max(opts, key=lambda o: o[1] / o[0])
            pick = best if best[0] <= self.p.coins else None
            if pick is None:
                # Saving for a bundle (a milestone, the fleet milestone): buy its cheapest next unit meanwhile,
                # as a player does, instead of waiting with the coins in the pocket.
                steps = [(c.cost(i, c.n[i], 1), i) for i in range(len(c.n)) if best[2][i] > c.n[i] and c.unlocked(i)]
                cost, i = min(steps) if steps else (float('inf'), -1)
                if cost > self.p.coins:
                    return
                new = c.n[:]
                new[i] += 1
                pick = (cost, 0.0, new)
            self.p.coins -= pick[0]
            self.units_bought += sum(pick[2]) - sum(c.n)
            c.n = pick[2]
            if self.first_buy is None:
                self.first_buy = self.t - self.start
            if self.second_member is None and c.n[1] > 0:
                self.second_member = self.t - self.start

    def online(self, seconds, step):
        r = self.rules
        if self.start is None:
            self.start = self.t
        per_attempt = r.fishing['online_cycle_seconds']
        done = 0.0
        while done < seconds:
            dt = min(step, seconds - done)
            self.carry += dt / per_attempt
            attempts = int(self.carry)
            self.carry -= attempts
            self.t += dt
            done += dt
            if attempts:
                self.p.fish(attempts, self.t / 86400.0)
            self.credit(dt)
            self.p.shop()
            self.buy()
            self.observe()

    def offline(self, seconds):
        r = self.rules
        attempts = int(min(seconds, r.fishing['offline_accumulation_cap_hours'] * 3600) / r.fishing['offline_cycle_seconds'])
        self.t += seconds
        self.p.fish(attempts, self.t / 86400.0)
        effective = self.crew.offline_seconds(seconds)
        self.p.coins += self.crew.coins_rate() * effective
        if self.crew_xp:
            self.add_xp(self.crew.xp_rate() * effective)
        self.observe()

    def credit(self, dt):
        self.p.coins += self.crew.coins_rate() * dt
        if self.crew_xp:
            self.add_xp(self.crew.xp_rate() * dt)

    def observe(self):
        rate = self.crew.coins_rate()
        self.max_rate = max(self.max_rate, rate)
        if self.passes_fishing is None and rate > self.fishing_coins_per_second() and rate > 0:
            self.passes_fishing = self.t - self.start
        for threshold in RATE_THRESHOLDS:
            if threshold not in self.rate_at and rate >= threshold:
                self.rate_at[threshold] = self.t
        day = self.t / 86400.0
        for d in CHECK_DAYS:
            if d not in self.snap and day >= d:
                self.snap[d] = {'renda_moedas_s': rate, 'xp_s': self.crew.xp_rate(), 'nivel': self.p.level,
                                'pesca_moedas_s': self.fishing_coins_per_second(), 'unidades': self.crew.n[:]}


def step_for(t):
    return 5.0 if t < 3600 else 30.0 if t < 86400 else 120.0


def play(crew_cfg, profile, days, crew_xp=True):
    g = Game(crew_cfg, crew_xp)
    sessions = PROFILES[profile][1]
    end = days * 86400.0
    if sessions is None:
        while g.t < end:
            g.online(step_for(g.t) * 20, step_for(g.t))
        return g
    last_end = None
    for day in range(int(math.ceil(days))):
        for start_h, minutes in sessions:
            start = day * 86400.0 + start_h * 3600.0
            if last_end is None:
                g.t = start
            else:
                g.offline(start - last_end)
            g.online(minutes * 60.0, 5.0 if day == 0 else 30.0)
            last_end = g.t
            if g.t >= end:
                return g
    return g


def fmt_time(seconds):
    if seconds is None:
        return '—'
    if seconds < 3600:
        return ('%.1f min' % (seconds / 60)).replace('.', ',')
    if seconds < 86400:
        return ('%.1f h' % (seconds / 3600)).replace('.', ',')
    return ('%.1f d' % (seconds / 86400)).replace('.', ',')


def short(v):
    for limit, unit in ((1e12, ' tri'), (1e9, ' bi'), (1e6, ' mi'), (1e3, ' mil')):
        if v >= limit:
            return ('%.1f' % (v / limit)).replace('.', ',') + unit
    return ('%.1f' % v).replace('.', ',')


def summary(g, base):
    lv = lambda p, l: p.reached.get(l)  # noqa: E731
    out = {
        'primeira_compra': fmt_time(g.first_buy),
        'segundo_tripulante': fmt_time(g.second_member),
        'tripulacao_passa_a_pesca': fmt_time(g.passes_fishing),
        'renda_maxima_moedas_s': round(g.max_rate, 1),
        'dias_ate_renda': {(short(t) + '/s'): (round(g.rate_at[t] / 86400.0, 2) if t in g.rate_at else None) for t in RATE_THRESHOLDS},
        'por_dia': {str(d).replace('.', ','): {
            'renda_tripulacao': short(s['renda_moedas_s']) + '/s',
            'renda_pesca': short(s['pesca_moedas_s']) + '/s',
            'xp_tripulacao': ('%.1f' % s['xp_s']).replace('.', ',') + '/s',
            'nivel': s['nivel'],
            'unidades': s['unidades'],
        } for d, s in sorted(g.snap.items())},
        'dias_ate_nivel_com_tripulacao': {('Nv.%d' % l): (round(lv(g.p, l), 2) if lv(g.p, l) is not None else None) for l in (10, 100, 500, 1000)},
        'dias_ate_nivel_sem_xp_da_tripulacao': {('Nv.%d' % l): (round(lv(base.p, l), 2) if lv(base.p, l) is not None else None) for l in (10, 100, 500, 1000)},
    }
    return out


def load_crew():
    with open(os.path.join(S.CONFIG, 'crew.json'), encoding='utf-8') as f:
        return json.load(f)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dias', type=float, default=75, help='dias simulados de quem deixa o jogo aberto (padrão 75, o Nv.1000)')
    ap.add_argument('--dias-perfis', type=float, default=170, help='dias simulados dos outros perfis (padrão 170)')
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    cfg = load_crew()
    result = {'gerado_por': 'tools/Progressao/simular_tripulacao.py', 'config': 'config/crew.json', 'dias': a.dias,
              'dias_perfis': a.dias_perfis,
              'nota': 'Tempos de primeira compra, segundo tripulante e "passa a pesca" contados da primeira vez que o jogo '
                      'abre (8 h no perfil de 4 vezes por dia, 20 h no de 1 vez). Os dias por dia e os dias até a renda e '
                      'o nível contam da meia-noite do primeiro dia.',
              'perfis': {}}
    for name, (desc, _) in PROFILES.items():
        days = a.dias if PROFILES[name][1] is None else a.dias_perfis
        g = play(cfg, name, days)
        base = play(cfg, name, days, crew_xp=False)
        s = summary(g, base)
        s['descricao'] = desc
        result['perfis'][name] = s
        print('== %s (%s)' % (name, desc))
        print('  primeira compra %s · 2º tripulante %s · passa a pesca %s · renda máxima %s/s'
              % (s['primeira_compra'], s['segundo_tripulante'], s['tripulacao_passa_a_pesca'], short(g.max_rate)))
        print('  dias até a renda da Tripulação chegar a:', s['dias_ate_renda'])
        for d, row in s['por_dia'].items():
            print('  dia %5s: Tripulação %-12s pesca %-10s XP %-8s Nv.%d' % (d, row['renda_tripulacao'], row['renda_pesca'], row['xp_tripulacao'], row['nivel']))
        print('  dias até o nível com o XP da Tripulação:', s['dias_ate_nivel_com_tripulacao'])
        print('  dias até o nível sem o XP da Tripulação:', s['dias_ate_nivel_sem_xp_da_tripulacao'])
    if a.gravar:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
            f.write('\n')
        print('Gravado em', os.path.relpath(OUT, ROOT))


if __name__ == '__main__':
    main()
