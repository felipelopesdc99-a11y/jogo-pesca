"""Vale a pena comprar isca? Custo e retorno das iscas em cada estágio do jogo (M24-T30, A-161).

Uso:  python3 tools/Progressao/simular_iscas.py [--precos 20,100,300] [--saida relatorio.md]

O que ele faz:
  - joga a progressão de simular_progressao.py (perfil quatro_vezes: 2 h online e 22 h offline por dia), SEM isca,
    e anota o equipamento do jogador (mapa, vara e nível, barco e nível) ao chegar em cada nível de referência;
  - em cada um desses momentos, calcula por valor esperado, para cada isca de hoje (equipment.json → baits) e para
    as iscas de antes (equipment.json → bait_migration.legacy_baits), por hora de pesca online (ciclo de
    progression.json → fishing) e por dia (2 h online + 22 h offline):
      peixes puxados, peixes raros ou melhores puxados e Moedas a mais que sem isca;
      o custo da isca (Moedas e Conchas) por hora;
      o retorno = Moedas a mais ÷ Moedas gastas na isca (acima de 1, a isca se paga em Moedas);
  - --precos A,B,C: testa outro custo POR TENTATIVA em Moedas para as iscas de hoje (na ordem da categoria), sem
    mudar /config.

As Conchas da isca não voltam em Moedas: o relatório mostra quanto da renda de Conchas por hora a isca leva.
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import simular_progressao as S  # noqa: E402

# The typical moments of the game (the same as the tests in MoreFishFewerRaresTests.cs): stage, map, rod, rod level,
# boat (None = the starter boat), boat level.
SCENARIOS = (
    ('começo', 'map_01', 'rod_01', 1, None, 1),
    ('começo', 'map_02', 'rod_01', 10, 'boat_01', 1),
    ('meio', 'map_05', 'rod_03', 30, 'boat_05', 10),
    ('meio', 'map_07', 'rod_04', 30, 'boat_05', 10),  # M24-T42: 5 barcos
    ('fim', 'map_10', 'rod_05', 1, None, 1),
    ('fim', 'map_10', 'rod_05', 100, 'boat_05', 100),
)


def by_rarity(r, mp, rod, rod_level, gear):
    """Chance de puxar, por tentativa, um peixe de cada raridade (as mesmas contas de Rules.per_attempt)."""
    b = r.rod_bonus(rod, rod_level)
    pool = []
    for e in mp['fish_pool']:
        s = r.species[e['species_id']]
        if e['catch_weight'] <= 0 or s['rarity'] not in mp['available_rarities'] or s['rarity'] not in rod['can_catch_rarities']:
            continue
        w = e['catch_weight'] * (1.0 if s['rarity'] == 'common' else 1.0 + b['rarity_efficiency'])
        pool.append((s, w))
    pool = r.fishing_weights(pool)
    total = sum(w for _, w in pool)
    out = {}
    for s, w in pool:
        tier = r.tiers[s['rarity']]
        chance = max(r.fishing['catch_success_min'], min(r.fishing['catch_success_max'], tier['catch_success_base'] + b['catch_success'] + gear))
        out[s['rarity']] = out.get(s['rarity'], 0.0) + w / total * chance
    return out


def snapshots(rules):
    """The gear of each scenario: (stage, map, rod, rod level, boat, boat level, boat bonus)."""
    maps = {m['id']: m for m in rules.maps}
    rods = {r['id']: r for r in rules.rods}
    boats = {b['id']: b for b in rules.boats}
    out = []
    for stage, map_id, rod_id, rod_level, boat_id, boat_level in SCENARIOS:
        boat = boats[boat_id] if boat_id else rules.boats[0]
        bonus = rules.boat_bonus(boat, boat_level) if boat_id else 0.0
        out.append((stage, maps[map_id], rods[rod_id], rod_level, boat, boat_level, bonus))
    return out


def br(v, d=0):
    s = ('{:,.%df}' % d).format(v)
    return s.replace(',', '#').replace('.', ',').replace('#', '.')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--precos', help='custo por tentativa em Moedas das iscas de hoje, separado por vírgula')
    ap.add_argument('--saida')
    a = ap.parse_args()

    rules = S.Rules()
    eq = rules.eq_cfg
    baits = sorted(eq['baits'], key=lambda b: b['tier'])
    legacy = sorted((eq.get('bait_migration') or {}).get('legacy_baits', []), key=lambda b: b['catch_success_bonus'])
    online_h = 3600.0 / rules.fishing['online_cycle_seconds']
    offline_h = 3600.0 / rules.fishing['offline_cycle_seconds']
    per_day = 2 * online_h + 22 * offline_h

    def per_try(b):
        return b['cost_coins'] / float(b['charges']), b['cost_shells'] / float(b['charges'])

    rows = [('hoje', b['display_name'], b['catch_success_bonus'], per_try(b)) for b in baits]
    if a.precos:
        prices = [float(x) for x in a.precos.split(',')]
        rows = [(k, n, bonus, (prices[i], cs)) for i, (k, n, bonus, (_, cs)) in enumerate(rows)]
    rows += [('antes', b['display_name'], b['catch_success_bonus'], per_try(b)) for b in legacy]

    lines = []
    w = lines.append
    w('# Iscas: custo e retorno por estágio (M24-T30)')
    w('')
    w('Gerado por `tools/Progressao/simular_iscas.py`. Valor esperado, '
      '%s tentativas por hora online e %s offline.' % (br(online_h), br(offline_h)))
    w('')
    w('Custo por tentativa:')
    w('')
    w('| Isca | Bônus | Moedas/tentativa | Conchas/tentativa |')
    w('|---|---|---|---|')
    for kind, name, bonus, (cc, cs) in rows:
        w('| %s (%s) | +%s%% | %s | %s |' % (name, kind, br(bonus * 100), br(cc, 2), br(cs, 4)))
    w('')
    w('Por hora de pesca online. "Retorno" = Moedas a mais que sem isca ÷ Moedas da isca (acima de 1,00 a isca se paga em Moedas); '
      '"Conchas" = Conchas da isca ÷ Conchas que a pesca dá por hora.')
    w('')
    w('| Estágio | Mapa · vara · barco | Isca | Peixes/h | Raros+/h | Moedas/h | Moedas a mais/h | Custo/h | Retorno | Conchas |')
    w('|---|---|---|---|---|---|---|---|---|---|')
    summary = {}
    for stage, mp, rod, rl, boat, bl, gear in snapshots(rules):
        base = rules.per_attempt(mp, rod, rl, gear)
        base_r = by_rarity(rules, mp, rod, rl, gear)
        base_fish, base_rare = sum(base_r.values()), sum(v for k, v in base_r.items() if k != 'common')
        where = '%s · %s Nv.%d · %s%s' % (mp['id'], rod['id'], rl, boat['id'], (' Nv.%d' % bl) if boat.get('has_levels') else '')
        w('| %s | %s | sem isca | %s | %s | %s | — | — | — | — |' % (
            stage, where, br(base_fish * online_h), br(base_rare * online_h, 1), br(base[1] * online_h)))
        for kind, name, bonus, (cc, cs) in rows:
            res = rules.per_attempt(mp, rod, rl, gear + bonus)
            rr = by_rarity(rules, mp, rod, rl, gear + bonus)
            fish, rare = sum(rr.values()), sum(v for k, v in rr.items() if k != 'common')
            extra = (res[1] - base[1]) * online_h
            cost = cc * online_h
            ratio = extra / cost if cost > 0 else float('inf')
            shells_h = base[2] * online_h
            shell_share = cs * online_h / shells_h if shells_h > 0 else float('inf')
            w('| %s | %s | %s (%s) +%s%% | %s | %s | %s | %s | %s M + %s C | %s | %s |' % (
                stage, where, name, kind, br(bonus * 100), br(fish * online_h), br(rare * online_h, 1), br(res[1] * online_h), br(extra), br(cost), br(cs * online_h, 1),
                br(ratio, 2), ('%s%%' % br(shell_share * 100)) if shell_share != float('inf') else '—'))
            if kind == 'hoje':
                summary.setdefault(stage, {}).setdefault(name, []).append(ratio)
    w('')
    w('Resumo (retorno médio em Moedas das iscas de hoje):')
    w('')
    for stage in ('começo', 'meio', 'fim'):
        if stage in summary:
            w('- %s: %s' % (stage, '; '.join('%s %s' % (n, br(sum(v) / len(v), 2)) for n, v in summary[stage].items())))
    w('')
    if baits:
        pack = baits[0]['charges']
        w('Um pacote de %s tentativas dura %s h de pesca online ou %s h offline; um dia de 2 h online + 22 h offline (%s tentativas) gasta %s pacotes.' % (
            br(pack), br(pack / online_h, 1), br(pack / offline_h, 1), br(per_day), br(per_day / pack, 1)))
    text = '\n'.join(lines)
    print(text)
    if a.saida:
        with open(a.saida, 'w', encoding='utf-8') as f:
            f.write(text + '\n')


if __name__ == '__main__':
    main()
