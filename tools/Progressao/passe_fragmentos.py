"""Barcos por fragmentos (M24-T16, A-157) e Passe do Pescador (M24-T17, A-158): gera os números e simula o ritmo.

Uso:
  python3 tools/Progressao/passe_fragmentos.py gerar
      # grava em config/equipment.json a chance de fragmento de cada barco (fragment_drop_chance) e a quantidade
      # (fragments_required), e gera config/pass.json (100 marcos, do Nv.10 ao Nv.1000)
  python3 tools/Progressao/passe_fragmentos.py simular [--dias 170] [--gravar]
      # simula o ritmo (os 3 jeitos de jogar, com o passe grátis e com o premium) e, com --gravar, escreve
      # docs/propostas/passe_fragmentos.json

Regras (as mesmas do jogo, GameService/Shop/BoatFragmentRules.cs e GameService/Pass/LocalPassService.cs):
  - cada peixe puxado no mapa de um barco tem fragment_drop_chance de dar 1 fragmento dele, online e offline
    (equipment.json → boat_fragments.drop_on_offline_fishing); Expedições não dão;
  - com fragments_required fragmentos o barco é montado (Nv.1) e passa a ser usado se for melhor;
  - o passe tem um marco a cada 10 níveis do Pescador; cada marco tem a recompensa grátis e a premium. O premium é
    comprado com Dólares e libera também as recompensas premium dos marcos já passados.

Como os números são gerados (ajuste as constantes abaixo e rode "gerar" de novo):
  - chance de fragmento: o jogador que abre 4 vezes por dia (30 min online + o resto offline, até 24 h), pescando no
    mapa do barco com a vara e o barco que tinha ao chegar lá (uma simulação de referência), monta o barco em
    DIAS_ALVO_PRIMEIRO dias no Mapa 1 e DIAS_ALVO_ULTIMO no Mapa 10 (crescendo em progressão geométrica);
  - Moedas do passe: MINUTOS_MOEDAS minutos da renda (Tripulação + pesca) que o jogador de referência (4 vezes por
    dia, com Tripulação e Melhorias) tem ao chegar àquele nível; Conchas: MINUTOS_CONCHAS minutos de Conchas da pesca
    online; o premium dá PREMIUM_MULT vezes isso;
  - Dólares: os 10 a cada 10 níveis até o Nv.100 do A-111 passam para a faixa grátis (mesmo total); depois, 10 a cada
    100 níveis (grátis) e 10 a cada 50 níveis (premium);
  - fragmentos: no Nv.10·X (X = 1 a 10) do barco do Mapa X; depois do Nv.100 os barcos se revezam (Nv.110 = Mapa 1,
    Nv.120 = Mapa 2, … Nv.200 = Mapa 10, Nv.210 = Mapa 1 …). Grátis: FRAG_GRATIS_ATE_100 dos fragmentos que o barco
    pede até o Nv.100 e FRAG_GRATIS_DEPOIS depois; o premium, o dobro (arredondado para cima);
  - fragmentos de vara (M24-T32, A-163): no marco do nível que libera o primeiro mapa que pede cada vara (Nv.10 = Vara 1,
    Nv.30 = Vara 2, Nv.50 = Vara 3, Nv.70 = Vara 4, Nv.90 = Vara 5), FRAG_VARA_GRATIS dos fragmentos que a vara pede; o
    premium, o dobro (arredondado para cima). Só esses 5 marcos: depois do Nv.90 todas as varas já liberaram o mapa delas;
  - Fragmentos de Roupa (M24-T33, A-164): em todo marco, ROUPA_ATE_100 (grátis, até o Nv.100) e ROUPA_DEPOIS (grátis,
    depois); o premium, o dobro.
"""
import argparse
import collections
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import curva_niveis as C  # noqa: E402
import simular_melhorias as M  # noqa: E402
import simular_progressao as S  # noqa: E402
import simular_tripulacao as T  # noqa: E402

ROOT = S.ROOT
CONFIG = S.CONFIG
OUT = os.path.join(ROOT, 'docs', 'propostas', 'passe_fragmentos.json')

# ---------------------------------------------------------------- knobs
FRAGMENTOS = [20, 25, 35, 45, 55, 70, 90, 120, 155, 200]   # fragments_required, barco do Mapa 1 ao Mapa 10
DIAS_ALVO_PRIMEIRO = 2.0       # 4 vezes por dia, pescando no Mapa 1
DIAS_ALVO_ULTIMO = 10.0        # 4 vezes por dia, pescando no Mapa 10
PERFIL_REFERENCIA = 'quatro_vezes'

MARCO_A_CADA = 10
NIVEL_MAXIMO = 1000
PRECO_PREMIUM_DOLARES = 100    # o mesmo do VIP (economy.json → vip.price_dollars)
MINUTOS_MOEDAS = 10.0
MINUTOS_CONCHAS = 30.0
PREMIUM_MULT = 2.0
DOLARES_ATE_100 = 10           # grátis, a cada 10 níveis até o Nv.100 (o A-111 de antes)
DOLARES_GRATIS_DEPOIS = (100, 10)    # grátis depois do Nv.100: a cada 100 níveis, 10 Dólares
DOLARES_PREMIUM = (50, 10)           # premium: a cada 50 níveis, 10 Dólares
FRAG_GRATIS_ATE_100 = 0.15
FRAG_GRATIS_DEPOIS = 0.10
FRAG_VARA_GRATIS = 0.15
ROUPA_ATE_100 = 2
ROUPA_DEPOIS = 1

PROFILES = T.PROFILES


def load(name):
    with open(os.path.join(CONFIG, name), encoding='utf-8') as f:
        return json.load(f, object_pairs_hook=collections.OrderedDict)


def save(name, data):
    with open(os.path.join(CONFIG, name), 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')


def attempts_per_day(rules, profile):
    """Fishing attempts in a day: online every online_cycle_seconds, offline every offline_cycle_seconds up to the cap."""
    fishing = rules.fishing
    online_per_min = 60.0 / fishing['online_cycle_seconds']
    offline_per_min = 60.0 / fishing['offline_cycle_seconds']
    sessions = PROFILES[profile][1]
    if sessions is None:
        return 1440 * online_per_min
    online = sum(m for _, m in sessions)
    # Gaps between sessions are offline (each one capped).
    cap = fishing['offline_accumulation_cap_hours'] * 60
    starts = [h * 60 for h, _ in sessions]
    ends = [h * 60 + m for h, m in sessions]
    gaps = [min(cap, (starts[(i + 1) % len(starts)] + (1440 if i + 1 == len(starts) else 0)) - ends[i]) for i in range(len(starts))]
    return online * online_per_min + sum(gaps) * offline_per_min


# ---------------------------------------------------------------- the reference player and the simulations

class Game(M.Game):
    """The M24 economy (fishing + Crew + Upgrades) with boats by fragments, plus the pass when one is given."""

    def __init__(self, crew_cfg, up_cfg, pass_cfg=None, premium=False):
        super().__init__(crew_cfg, up_cfg)
        self.pass_cfg = pass_cfg
        self.buy_premium = premium
        self.premium = False
        self.claimed_free = set()
        self.claimed_premium = set()
        self.dollars = 0.0
        self.dollars_at = {}         # level -> Dólares from the pass until then
        self.premium_at = None       # seconds
        self.levels_seen = 0
        self.at_level = {}           # level (multiple of 10) -> what the player has then (reference run)
        self.boats_at = {}           # boat id -> seconds
        self.boats_by_pass = {}      # boat id -> fragments the pass gave
        self.boats_by_fishing = {}   # boat id -> fragments fishing gave
        add = self.p.add_fragments
        p = self.p

        def counting_add(boat, amount, source='fishing'):
            book = self.boats_by_pass if source == 'pass' else self.boats_by_fishing
            book[boat['id']] = book.get(boat['id'], 0.0) + amount
            add(boat, amount)

        p.add_fragments = counting_add

    def fishing_shells_per_second(self):
        p = self.p
        _, _, shells, _ = p.stats()
        return shells / self.rules.fishing['online_cycle_seconds']

    def observe(self):
        super().observe()
        p = self.p
        while self.levels_seen + MARCO_A_CADA <= p.level:
            self.levels_seen += MARCO_A_CADA
            lvl = self.levels_seen
            self.at_level[lvl] = {
                'renda_moedas_s': self.crew.coins_rate() + self.fishing_coins_per_second(),
                'conchas_s': self.fishing_shells_per_second(),
                'vara': p.rod['id'], 'nivel_vara': p.rods[p.rod['id']], 'bonus_barco': p.boat_bonus() + p.gloves(), 'roupas': p.extra(),
                'dia': self.t / 86400.0,
            }
        for bid in p.owned_boats:
            self.boats_at.setdefault(bid, self.t)
        if self.pass_cfg:
            self.claim()

    def give(self, reward):
        p = self.p
        p.coins += reward.get('coins', 0)
        p.shells += reward.get('shells', 0)
        self.dollars += reward.get('dollars', 0)
        if reward.get('boat_id') and reward.get('fragments'):
            boat = next(b for b in self.rules.boats if b['id'] == reward['boat_id'])
            p.add_fragments(boat, reward['fragments'], 'pass')
        if reward.get('rod_id') and reward.get('rod_fragments'):
            rod = next(r for r in self.rules.rods if r['id'] == reward['rod_id'])
            p.add_rod_fragments(rod, reward['rod_fragments'])
        if reward.get('clothing_fragments') and self.rules.clothing:
            p.add_clothing_fragments(reward['clothing_fragments'])

    def claim(self):
        p = self.p
        for m in self.pass_cfg['milestones']:
            lvl = m['level']
            if lvl > p.level:
                break
            if lvl not in self.claimed_free:
                self.claimed_free.add(lvl)
                self.give(m['free'])
                self.dollars_at[lvl] = self.dollars
        if self.buy_premium and not self.premium and self.dollars >= self.pass_cfg['premium_price_dollars']:
            self.dollars -= self.pass_cfg['premium_price_dollars']
            self.premium = True
            self.premium_at = self.t
        if self.premium:
            for m in self.pass_cfg['milestones']:
                if m['level'] > p.level:
                    break
                if m['level'] not in self.claimed_premium:
                    self.claimed_premium.add(m['level'])
                    self.give(m['premium'])


def play(profile, days, pass_cfg=None, premium=False):
    """The same loop as simular_melhorias.play, with this Game."""
    g = Game(M.load_json('crew.json'), M.load_json('upgrades.json'), pass_cfg, premium)
    sessions = PROFILES[profile][1]
    end = days * 86400.0
    if sessions is None:
        while g.t < end and g.p.level < g.rules.max_level:
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


def map_of_level(level, n_maps):
    """Which map's boat a milestone gives fragments of: Nv.10·X → Map X up to Nv.100; then they take turns."""
    k = level // MARCO_A_CADA
    return (k - 1) % n_maps + 1


def catch_rates(rules, reference):
    """Pulled-out fish per attempt on each map, with the rod and boat the reference player had on arriving."""
    out = collections.OrderedDict()
    for mp in rules.maps:
        lvl = max(MARCO_A_CADA, mp['unlock_fisher_level'])
        snap = reference.at_level.get(lvl) or reference.at_level[max(reference.at_level)]
        rod = next(r for r in rules.rods if r['id'] == snap['vara'])
        if rod['tier'] < mp['minimum_rod_tier']:
            rod = next(r for r in rules.rods if r['tier'] >= mp['minimum_rod_tier'])
        _, _, _, caught = rules.per_attempt(mp, rod, snap['nivel_vara'] if rod['id'] == snap['vara'] else 1, snap['bonus_barco'], snap.get('roupas'))
        out[mp['id']] = {'por_tentativa': caught, 'vara': rod['id'], 'nivel_vara': snap['nivel_vara'], 'bonus_barco': round(snap['bonus_barco'], 4)}
    return out


def smooth(values):
    """Income never goes down from one milestone to the next (a session-based run is jumpy)."""
    best = 0.0
    out = []
    for v in values:
        best = max(best, v)
        out.append(best)
    return out


def rods_by_level(rules):
    """The milestone of each rod (M24-T32): the Fisher level that unlocks the first map asking for its tier."""
    out = {}
    for rod in rules.rods:
        if rod['tier'] == 0:
            continue
        first = min((m['unlock_fisher_level'] for m in rules.maps if m['minimum_rod_tier'] == rod['tier']), default=None)
        if first:
            out[max(MARCO_A_CADA, int(math.ceil(first / float(MARCO_A_CADA))) * MARCO_A_CADA)] = rod
    return out


def build_pass(reference, boats_by_map, rod_at=None):
    levels = list(range(MARCO_A_CADA, NIVEL_MAXIMO + 1, MARCO_A_CADA))
    known = sorted(reference.at_level)
    incomes = smooth([reference.at_level[min(known, key=lambda k: abs(k - l))]['renda_moedas_s'] for l in levels])
    shells_s = smooth([reference.at_level[min(known, key=lambda k: abs(k - l))]['conchas_s'] for l in levels])
    milestones = []
    n_maps = len(boats_by_map)
    for i, lvl in enumerate(levels):
        coins = C.nice_round(max(100.0, incomes[i] * MINUTOS_MOEDAS * 60))
        shells = max(1, int(round(shells_s[i] * MINUTOS_CONCHAS * 60)))
        boat = boats_by_map[map_of_level(lvl, n_maps)]
        share = FRAG_GRATIS_ATE_100 if lvl <= 100 else FRAG_GRATIS_DEPOIS
        frags = max(1, int(math.ceil(boat['fragments_required'] * share)))
        free_dollars = DOLARES_ATE_100 if lvl <= 100 else (DOLARES_GRATIS_DEPOIS[1] if lvl % DOLARES_GRATIS_DEPOIS[0] == 0 else 0)
        prem_dollars = DOLARES_PREMIUM[1] if lvl % DOLARES_PREMIUM[0] == 0 else 0
        free = collections.OrderedDict([('coins', coins), ('shells', shells), ('dollars', free_dollars),
                                        ('boat_id', boat['id']), ('fragments', frags)])
        premium = collections.OrderedDict([('coins', C.nice_round(coins * PREMIUM_MULT)), ('shells', int(math.ceil(shells * PREMIUM_MULT))),
                                           ('dollars', prem_dollars), ('boat_id', boat['id']), ('fragments', int(math.ceil(frags * PREMIUM_MULT)))])
        rod = (rod_at or {}).get(lvl)
        if rod:
            rod_frags = max(1, int(math.ceil(rod['fragments_required'] * FRAG_VARA_GRATIS)))
            free['rod_id'] = rod['id']
            free['rod_fragments'] = rod_frags
            premium['rod_id'] = rod['id']
            premium['rod_fragments'] = int(math.ceil(rod_frags * PREMIUM_MULT))
        clothes = ROUPA_ATE_100 if lvl <= 100 else ROUPA_DEPOIS
        free['clothing_fragments'] = clothes
        premium['clothing_fragments'] = int(math.ceil(clothes * PREMIUM_MULT))
        milestones.append(collections.OrderedDict([('level', lvl), ('free', free), ('premium', premium)]))
    return milestones


def gerar(days):
    rules = S.Rules()
    eq = load('equipment.json')
    boats = [b for b in eq['boats'] if b.get('map_id')]
    for b in boats:
        b['fragments_required'] = FRAGMENTOS[b['tier'] - 1]

    print('Simulação de referência (%s, %d dias, sem passe)…' % (PERFIL_REFERENCIA, days))
    reference = play(PERFIL_REFERENCIA, days)
    rates = catch_rates(rules, reference)
    per_day = attempts_per_day(rules, PERFIL_REFERENCIA)
    n = len(boats)
    for b in boats:
        i = b['tier'] - 1
        target = DIAS_ALVO_PRIMEIRO * (DIAS_ALVO_ULTIMO / DIAS_ALVO_PRIMEIRO) ** (i / float(max(1, n - 1)))
        rate = rates[b['map_id']]['por_tentativa']
        chance = b['fragments_required'] / (target * per_day * rate)
        # 4 casas: com o ciclo de pesca na metade (M24-T22) as chances ficaram abaixo de 1% e 3 casas erravam ~10%.
        b['fragment_drop_chance'] = round(max(0.0001, chance), 4)
        print('  %s (%s): %d fragmentos, %s por peixe (alvo %s dias, %s das tentativas viram peixe puxado)'
              % (b['id'], b['map_id'], b['fragments_required'], pct(b['fragment_drop_chance'], 2), dec(target, 1), pct(rate, 0)))
    save('equipment.json', eq)

    boats_by_map = {int(b['map_id'][-2:]): b for b in boats}
    pass_cfg = collections.OrderedDict([
        ('config_file', 'pass.json'),
        ('config_schema_version', 1),
        ('balance_status', 'provisional'),
        ('description', 'Passe do Pescador (M24-T17, A-158): um passe permanente, um marco a cada 10 níveis do Pescador '
                        '(Nv.10 ao Nv.1000), cada um com a recompensa grátis e a premium. O premium é comprado uma vez com '
                        'Dólares (premium_price_dollars) e libera também as recompensas premium dos marcos já passados. '
                        'Os 10 Dólares a cada 10 níveis até o Nv.100 (A-111) estão na faixa grátis. Números gerados por '
                        'tools/Progressao/passe_fragmentos.py gerar; são sugestões para ajustar jogando.'),
        ('pass_id', 'pescador'),
        ('pass_id_note', 'Identifica este passe no save. Temporadas (passes que trocam) ficaram para depois (OD-059): '
                         'hoje existe um passe só, e um pass_id diferente não zera nada.'),
        ('premium_price_dollars', PRECO_PREMIUM_DOLARES),
        ('rewards_note', 'coins = Moedas, shells = Conchas, dollars = Dólares, fragments = fragmentos do barco boat_id '
                         '(equipment.json), rod_fragments = fragmentos da vara rod_id (rods.json, M24-T32), '
                         'clothing_fragments = Fragmentos de Roupa (equipment.json → clothing, M24-T33). Moedas: ~%d min da '
                         'renda de quem chega ao nível abrindo o jogo 4 vezes por dia; Conchas: ~%d min de Conchas da pesca; '
                         'premium: ~%g× e Dólares a cada %d níveis.'
                         % (MINUTOS_MOEDAS, MINUTOS_CONCHAS, PREMIUM_MULT, DOLARES_PREMIUM[0])),
        ('milestones', build_pass(reference, boats_by_map, rods_by_level(rules))),
    ])
    save('pass.json', pass_cfg)
    print('config/equipment.json e config/pass.json gravados.')


def dec(value, places):
    """A number with a decimal comma (PT-BR, CLAUDE.md): 1.5 → "1,5"."""
    return ('%.*f' % (places, value)).replace('.', ',')


def pct(ratio, places):
    """A ratio as a PT-BR percentage: 0.0065 → "0,65%"."""
    return dec(ratio * 100.0, places) + '%'


def fmt_days(seconds):
    if seconds is None:
        return '—'
    d = seconds / 86400.0
    if d < 1 / 24.0:
        return ('%.0f min' % (d * 1440))
    if d < 1:
        return ('%.1f h' % (d * 24)).replace('.', ',')
    return ('%.1f dias' % d).replace('.', ',')


def simular(days, gravar):
    rules = S.Rules()
    eq = load('equipment.json')
    pass_cfg = load('pass.json')
    boats = [b for b in eq['boats'] if b.get('map_id')]

    reference = play(PERFIL_REFERENCIA, days)
    rates = catch_rates(rules, reference)
    out = collections.OrderedDict()
    out['gerado_por'] = 'tools/Progressao/passe_fragmentos.py'
    out['nota'] = ('Barcos por fragmentos (M24-T16, A-157) e Passe do Pescador (M24-T17, A-158). "no_mapa": dias para montar o '
                   'barco pescando só no mapa dele (online e offline), com a vara e o barco que o jogador de referência '
                   '(4 vezes por dia) tinha ao chegar lá. "progressao": o jogo inteiro (pesca, Tripulação e Melhorias, '
                   'sempre no melhor mapa liberado, sem voltar para pegar fragmentos), com o passe grátis resgatado na '
                   'hora e, no "premium", o premium comprado assim que os Dólares dão. Dias contados da meia-noite do '
                   'primeiro dia.')
    out['parametros'] = collections.OrderedDict([
        ('dias_alvo_mapa_1', DIAS_ALVO_PRIMEIRO), ('dias_alvo_mapa_10', DIAS_ALVO_ULTIMO), ('perfil_referencia', PERFIL_REFERENCIA),
        ('minutos_de_renda_moedas', MINUTOS_MOEDAS), ('minutos_conchas', MINUTOS_CONCHAS), ('premium_mult', PREMIUM_MULT),
        ('preco_premium_dolares', pass_cfg['premium_price_dollars']),
    ])
    out['tentativas_por_dia'] = {p: round(attempts_per_day(rules, p)) for p in PROFILES}

    no_mapa = collections.OrderedDict()
    print('Dias para montar cada barco pescando no mapa dele:')
    for b in boats:
        rate = rates[b['map_id']]['por_tentativa']
        row = collections.OrderedDict([('nome', b['display_name']), ('mapa', b['map_id']), ('fragmentos', b['fragments_required']),
                                       ('chance_por_peixe', b['fragment_drop_chance']), ('peixes_puxados_por_tentativa', round(rate, 3)),
                                       ('bonus_nv1', b['catch_success_bonus']), ('bonus_nv100', b.get('catch_success_bonus_at_max_level'))])
        for p in PROFILES:
            per_day = attempts_per_day(rules, p) * rate * b['fragment_drop_chance']
            row['dias_' + p] = round(b['fragments_required'] / per_day, 2) if per_day > 0 else None
        no_mapa[b['id']] = row
        print('  %-18s %-7s %3d frag. %6s  aberto %5s d · 4×/dia %5s d · 1×/dia %5s d'
              % (b['display_name'], b['map_id'], b['fragments_required'], pct(b['fragment_drop_chance'], 2),
                 dec(row['dias_sempre_aberto'], 1), dec(row['dias_quatro_vezes'], 1), dec(row['dias_uma_vez'], 1)))
    out['no_mapa'] = no_mapa

    resumo = collections.OrderedDict()
    for lvl in (10, 50, 100, 250, 500, 1000):
        m = next(x for x in pass_cfg['milestones'] if x['level'] == lvl)
        resumo['Nv.%d' % lvl] = collections.OrderedDict([('gratis', m['free']), ('premium', m['premium'])])
    out['passe_resumo'] = resumo
    tot = lambda key, track, upto=NIVEL_MAXIMO: sum(m[track].get(key, 0) for m in pass_cfg['milestones'] if m['level'] <= upto)  # noqa: E731
    out['passe_totais'] = collections.OrderedDict([
        ('dolares_gratis_ate_nv100', tot('dollars', 'free', 100)), ('dolares_gratis_total', tot('dollars', 'free')),
        ('dolares_premium_total', tot('dollars', 'premium')),
        ('moedas_gratis_total', tot('coins', 'free')), ('conchas_gratis_total', tot('shells', 'free')),
        ('moedas_premium_total', tot('coins', 'premium')), ('conchas_premium_total', tot('shells', 'premium')),
        ('fragmentos_de_vara_gratis_total', tot('rod_fragments', 'free')), ('fragmentos_de_vara_premium_total', tot('rod_fragments', 'premium')),
        ('fragmentos_de_roupa_gratis_total', tot('clothing_fragments', 'free')), ('fragmentos_de_roupa_premium_total', tot('clothing_fragments', 'premium')),
    ])

    prog = collections.OrderedDict()
    for profile in PROFILES:
        for premium in (False, True):
            key = profile + ('_premium' if premium else '_gratis')
            print('Simulando %s…' % key)
            g = play(profile, days, pass_cfg, premium)
            boats_row = collections.OrderedDict()
            for b in boats:
                t = g.boats_at.get(b['id'])
                boats_row[b['id']] = collections.OrderedDict([
                    ('montado_em', fmt_days(t)), ('dias', round(t / 86400.0, 2) if t is not None else None),
                    ('fragmentos_da_pesca', round(g.boats_by_fishing.get(b['id'], 0.0), 1)),
                    ('fragmentos_do_passe', round(g.boats_by_pass.get(b['id'], 0.0), 1)),
                ])
            reached = lambda l: g.p.reached.get(l)  # noqa: E731
            prog[key] = collections.OrderedDict([
                ('descricao', PROFILES[profile][0] + (' · compra o premium' if premium else ' · só o passe grátis')),
                ('barcos', boats_row),
                ('dias_ate_nivel', {('Nv.%d' % l): (round(reached(l), 2) if reached(l) is not None else None) for l in (10, 100, 500, 1000)}),
                ('premium_comprado_em', fmt_days(g.premium_at)),
                ('dolares_do_passe_recebidos', round(g.dollars + (pass_cfg['premium_price_dollars'] if g.premium else 0))),
                ('marcos_resgatados', len(g.claimed_free)),
            ])
            print('  barcos:', ', '.join('%s %s' % (bid, r['montado_em']) for bid, r in boats_row.items()))
            print('  níveis:', ', '.join('%s %s' % (k, fmt_days(v * 86400.0) if v is not None else '—') for k, v in prog[key]['dias_ate_nivel'].items()),
                  '· premium', prog[key]['premium_comprado_em'])
    out['progressao'] = prog
    if gravar:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(out, f, ensure_ascii=False, indent=2)
            f.write('\n')
        print('Gravado em', os.path.relpath(OUT, ROOT))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('acao', choices=('gerar', 'simular'))
    ap.add_argument('--dias', type=float, default=170)
    ap.add_argument('--gravar', action='store_true')
    a = ap.parse_args()
    if a.acao == 'gerar':
        gerar(a.dias)
    else:
        simular(a.dias, a.gravar)


if __name__ == '__main__':
    main()
