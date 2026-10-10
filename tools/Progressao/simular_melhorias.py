"""Simula a Tripulação com as Melhorias compradas (M24-T06, A-155), junto com a pesca, com os números de /config.

Uso:  python3 tools/Progressao/simular_melhorias.py [--dias 80] [--perfis sempre_aberto,quatro_vezes,uma_vez] [--gravar]

O que ele faz (além do que tools/Progressao/simular_tripulacao.py já faz):
  - Melhorias de tripulante (config/upgrades.json → crew_upgrades): uma por tripulante e por linha de tiers, liberada
    pela quantidade dele, preço cost_factor × base_cost × cost_growth^unlock_count, ×2 na renda daquele tripulante;
  - Melhorias gerais com níveis: Rádio do Porto (+% na renda da Tripulação), Freguesia na Feira (+% na venda de
    peixe), Maré Boa (+% nas Moedas da venda da Caixa), Sonar de Cardume (+% no XP da Tripulação) e Caixa Térmica
    (horas a mais no teto offline da Tripulação), exatamente como o serviço do jogo (UpgradeRules);
  - compra como um jogador atento, com uma política simples: entre tripulantes e Melhorias que aumentam a renda
    de Moedas, sempre a de melhor retorno por Moeda (renda a mais ÷ preço); o Sonar e a Caixa Térmica, que não
    aumentam a renda, quando o próximo nível custa até UTILITY_SECONDS de renda (Tripulação + pesca);
  - compara com a Tripulação sem Melhorias (os mesmos números de crew.json) e mede os tempos por perfil.

Com --gravar, escreve o resultado em docs/propostas/melhorias.json.
"""
import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import simular_progressao as S  # noqa: E402
import simular_tripulacao as T  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, 'docs', 'propostas', 'melhorias.json')
LONG_MAX = T.LONG_MAX

# Sonar and Caixa Térmica do not raise the Moedas per second: a careful player buys the next level when it costs
# at most this many seconds of income.
UTILITY_SECONDS = 600.0

CHECK_LEVELS = (10, 100, 500, 1000)


def load_json(name):
    with open(os.path.join(S.CONFIG, name), encoding='utf-8') as f:
        return json.load(f)


class Upgrades:
    """Same rules as client-unity/Assets/Scripts/GameService/Upgrades/UpgradeRules.cs."""

    def __init__(self, cfg, crew_cfg):
        self.cfg = cfg
        cu = cfg['crew_upgrades']
        self.mult = cu['multiplier']
        self.tiers = cu['tiers']
        self.members = crew_cfg['members']
        self.general = cfg['general_upgrades']
        self.bought = [[False] * len(self.tiers) for _ in self.members]   # crew upgrades
        self.level = {g['id']: 0 for g in self.general}

    # ------------------------------------------------------------------ prices
    def crew_cost(self, i, k):
        m, t = self.members[i], self.tiers[k]
        c = t['cost_factor'] * m['base_cost'] * m['cost_growth'] ** t['unlock_count']
        return c if c < LONG_MAX else float('inf')

    def general_cost(self, g):
        lvl = self.level[g['id']]
        if lvl >= g['max_level']:
            return float('inf')
        c = g['base_cost'] * g['cost_growth'] ** lvl
        return c if c < LONG_MAX else float('inf')

    # ------------------------------------------------------------------ effects
    def member_mult(self, i):
        return self.mult ** sum(self.bought[i])

    def effect(self, key, extra=None):
        total = 0.0
        for g in self.general:
            if g['effect'] == key:
                lvl = self.level[g['id']] + (1 if extra is g else 0)
                total += lvl * g['value_per_level']
        return total

    def crew_coins(self, extra=None):
        return 1.0 + self.effect('crew_coins', extra)

    def crew_xp(self):
        return 1.0 + self.effect('crew_xp')

    def fishing(self, extra=None):
        return (1.0 + self.effect('fish_sale', extra)) * (1.0 + self.effect('fishing_coins', extra))

    def offline_hours(self):
        return self.effect('crew_offline_hours')


class Crew(T.Crew):
    """The Crew with the upgrades' multipliers (member ×2s and the Rádio do Porto)."""

    def __init__(self, cfg, up):
        super().__init__(cfg)
        self.up = up
        self.base_max_s = self.max_s

    def coins_rate(self, n=None, member_mult=None, radio=None):
        n = n or self.n
        fleet = self.fleet_mult ** self.fleet_level(n)
        mm = member_mult or [self.up.member_mult(i) for i in range(len(self.members))]
        r = self.up.crew_coins() if radio is None else radio
        return sum(m['coins_per_second'] * n[i] * self.mult ** self.reached(n[i], self.counts) * fleet * mm[i]
                   for i, m in enumerate(self.members)) * r

    def xp_rate(self, n=None):
        return super().xp_rate(n) * self.up.crew_xp()

    def offline_seconds(self, away_s):
        self.max_s = self.base_max_s + self.up.offline_hours() * 3600.0
        return super().offline_seconds(away_s)


class Game(T.Game):
    def __init__(self, crew_cfg, up_cfg, crew_xp=True, use_upgrades=True):
        super().__init__(crew_cfg, crew_xp)
        self.up = Upgrades(up_cfg, crew_cfg)
        self.use_upgrades = use_upgrades
        self.crew = Crew(crew_cfg, self.up)
        self.upgrades_bought = 0
        self.first_upgrade = None
        self.coins_spent_upgrades = 0.0
        fish = self.p.fish

        def fish_with_upgrades(attempts, t_days, xp_mult=1.0):
            before = self.p.coins
            left = fish(attempts, t_days, xp_mult)
            self.p.coins = before + (self.p.coins - before) * self.up.fishing()
            return left

        self.p.fish = fish_with_upgrades

    def fishing_coins_per_second(self):
        return super().fishing_coins_per_second() * self.up.fishing()

    def upgrade_options(self):
        """(cost, rate gain, apply) of every upgrade a player can buy now that raises the Moedas per second."""
        c, up = self.crew, self.up
        out = []
        base = c.coins_rate()
        for i in range(len(c.members)):
            for k, t in enumerate(up.tiers):
                if up.bought[i][k] or c.n[i] < t['unlock_count']:
                    continue
                gain = c.member_rate(i) * up.member_mult(i) * up.crew_coins() * (up.mult - 1.0)
                out.append((up.crew_cost(i, k), gain, ('crew', i, k)))
        fishing = super().fishing_coins_per_second()
        for g in up.general:
            cost = up.general_cost(g)
            if cost == float('inf'):
                continue
            if g['effect'] == 'crew_coins':
                gain = c.coins_rate(radio=up.crew_coins(g)) - base
            elif g['effect'] in ('fish_sale', 'fishing_coins'):
                gain = fishing * (up.fishing(g) - up.fishing())
            else:
                continue
            out.append((cost, gain, ('general', g)))
        return [o for o in out if o[0] < float('inf') and o[1] > 0]

    def apply(self, what, cost):
        self.p.coins -= cost
        self.coins_spent_upgrades += cost
        self.upgrades_bought += 1
        if self.first_upgrade is None:
            self.first_upgrade = self.t - self.start
        if what[0] == 'crew':
            self.up.bought[what[1]][what[2]] = True
        else:
            self.up.level[what[1]['id']] += 1

    def buy_utilities(self):
        income = self.crew.coins_rate() + self.fishing_coins_per_second()
        for g in self.up.general:
            if g['effect'] not in ('crew_xp', 'crew_offline_hours'):
                continue
            while True:
                cost = self.up.general_cost(g)
                if cost == float('inf') or cost > self.p.coins or cost > income * UTILITY_SECONDS:
                    break
                self.apply(('general', g), cost)

    def buy(self):
        if not self.use_upgrades:
            return super().buy()
        c = self.crew
        self.buy_utilities()
        for _ in range(500):
            crew_opts = [(o[0], o[1], ('units', o[2])) for o in c.options()]
            opts = crew_opts + self.upgrade_options()
            if not opts:
                return
            best = max(opts, key=lambda o: o[1] / o[0])
            if best[0] <= self.p.coins:
                if best[2][0] == 'units':
                    self.p.coins -= best[0]
                    self.units_bought += sum(best[2][1]) - sum(c.n)
                    c.n = best[2][1]
                else:
                    self.apply(best[2], best[0])
            elif best[2][0] == 'units':
                # Saving for a bundle: buy its cheapest next unit meanwhile (as simular_tripulacao.py).
                steps = [(c.cost(i, c.n[i], 1), i) for i in range(len(c.n)) if best[2][1][i] > c.n[i] and c.unlocked(i)]
                cost, i = min(steps) if steps else (float('inf'), -1)
                if cost > self.p.coins:
                    return
                self.p.coins -= cost
                self.units_bought += 1
                c.n[i] += 1
            else:
                return   # saving for the upgrade
            if self.first_buy is None:
                self.first_buy = self.t - self.start
            if self.second_member is None and c.n[1] > 0:
                self.second_member = self.t - self.start


def play(crew_cfg, up_cfg, profile, days, crew_xp=True, use_upgrades=True):
    g = Game(crew_cfg, up_cfg, crew_xp, use_upgrades)
    sessions = T.PROFILES[profile][1]
    end = days * 86400.0
    if sessions is None:
        while g.t < end:
            g.online(T.step_for(g.t) * 20, T.step_for(g.t))
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


def days_to(g, threshold):
    return round(g.rate_at[threshold] / 86400.0, 2) if threshold in g.rate_at else None


def level_days(g, level):
    d = g.p.reached.get(level)
    return round(d, 2) if d is not None else None


def summary(g, base):
    s = {
        'primeira_compra': T.fmt_time(g.first_buy),
        'segundo_tripulante': T.fmt_time(g.second_member),
        'primeira_melhoria': T.fmt_time(g.first_upgrade),
        'tripulacao_passa_a_pesca': T.fmt_time(g.passes_fishing),
        'renda_maxima_moedas_s': round(g.max_rate, 1),
        'dias_ate_renda': {(T.short(t) + '/s'): days_to(g, t) for t in T.RATE_THRESHOLDS},
        'dias_ate_renda_sem_melhorias': {(T.short(t) + '/s'): days_to(base, t) for t in T.RATE_THRESHOLDS},
        'dias_ate_nivel': {('Nv.%d' % l): level_days(g, l) for l in CHECK_LEVELS},
        'dias_ate_nivel_sem_melhorias': {('Nv.%d' % l): level_days(base, l) for l in CHECK_LEVELS},
        'melhorias_compradas': g.upgrades_bought,
        'melhorias_de_tripulante': sum(sum(b) for b in g.up.bought),
        'niveis_gerais': dict(g.up.level),
        'por_dia': {str(d).replace('.', ','): {
            'renda_tripulacao': T.short(x['renda_moedas_s']) + '/s',
            'renda_pesca': T.short(x['pesca_moedas_s']) + '/s',
            'xp_tripulacao': ('%.1f' % x['xp_s']).replace('.', ',') + '/s',
            'nivel': x['nivel'],
            'unidades': x['unidades'],
        } for d, x in sorted(g.snap.items())},
    }
    return s


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dias', type=float, default=80, help='dias simulados de quem deixa o jogo aberto (padrão 80)')
    ap.add_argument('--dias-perfis', type=float, default=170, help='dias simulados dos outros perfis (padrão 170)')
    ap.add_argument('--perfis', default='sempre_aberto,quatro_vezes,uma_vez')
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    crew_cfg = load_json('crew.json')
    up_cfg = load_json('upgrades.json')
    result = {'gerado_por': 'tools/Progressao/simular_melhorias.py', 'config': ['config/crew.json', 'config/upgrades.json'],
              'dias': a.dias, 'dias_perfis': a.dias_perfis, 'segundos_de_renda_para_sonar_e_caixa': UTILITY_SECONDS,
              'nota': 'Política de compra: entre tripulantes e Melhorias que aumentam a renda, sempre a de maior renda a mais '
                      'por Moeda; Sonar de Cardume e Caixa Térmica quando o próximo nível custa até 10 minutos de renda. '
                      '"sem_melhorias" é a mesma Tripulação (os mesmos números de crew.json) sem comprar nenhuma Melhoria. '
                      'Primeira compra, segundo tripulante, primeira Melhoria e "passa a pesca" contam da primeira vez que o '
                      'jogo abre; os dias, da meia-noite do primeiro dia.',
              'perfis': {}}
    for name in a.perfis.split(','):
        desc = T.PROFILES[name][0]
        days = a.dias if T.PROFILES[name][1] is None else a.dias_perfis
        g = play(crew_cfg, up_cfg, name, days)
        base = play(crew_cfg, up_cfg, name, days, use_upgrades=False)
        s = summary(g, base)
        s['descricao'] = desc
        result['perfis'][name] = s
        print('== %s (%s)' % (name, desc))
        print('  primeira compra %s · 2º tripulante %s · 1ª Melhoria %s · passa a pesca %s · renda máxima %s/s'
              % (s['primeira_compra'], s['segundo_tripulante'], s['primeira_melhoria'], s['tripulacao_passa_a_pesca'], T.short(g.max_rate)))
        print('  dias até a renda (com Melhorias):', s['dias_ate_renda'])
        print('  dias até a renda (sem Melhorias):', s['dias_ate_renda_sem_melhorias'])
        for d, row in s['por_dia'].items():
            print('  dia %5s: Tripulação %-12s pesca %-10s XP %-8s Nv.%d' % (d, row['renda_tripulacao'], row['renda_pesca'], row['xp_tripulacao'], row['nivel']))
        print('  dias até o nível (com Melhorias):', s['dias_ate_nivel'])
        print('  dias até o nível (sem Melhorias):', s['dias_ate_nivel_sem_melhorias'])
        print('  Melhorias: %d (de tripulante %d), níveis gerais %s' % (s['melhorias_compradas'], s['melhorias_de_tripulante'], s['niveis_gerais']))
    if a.gravar:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
            f.write('\n')
        print('Gravado em', os.path.relpath(OUT, ROOT))


if __name__ == '__main__':
    main()
