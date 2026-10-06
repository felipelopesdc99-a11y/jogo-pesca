"""Coloca no jogo (/config) os mapas da proposta adaptada docs/propostas/mapas_5_10.json.

Uso:  python3 tools/Progressao/aplicar_mapas.py map_05 map_06

Para cada mapa pedido grava, sem mexer no que já existe:
  - as 10 espécies em config/fish_catalog.json;
  - o mapa em config/maps.json (pool, nível, vara mínima, raridades e visual);
  - a vara mínima dele em config/rods.json, se ainda não existir (Varas 3 a 5);
  - a raridade Lendário (Mapa 6 em diante) ou Mítico (só Mapa 10) em config/progression.json,
    e a mesma raridade na proteção da venda em lote (config/economy.json).
Rodar de novo com os mesmos mapas não duplica nada. Os números vêm de tools/Progressao/adaptar_mapas_5_10.py.
"""
import collections
import json
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CONFIG = os.path.join(ROOT, 'config')
PROPOSTA = os.path.join(ROOT, 'docs', 'propostas', 'mapas_5_10.json')

# Visual de cada mapa (docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md, seção 4 do original).
MAPAS = {
    'map_05': {'display_name': 'Costa de Coral', 'level': 40, 'rod_tier': 2,
               'visual_theme': {'summary': 'Costa tropical brasileira rasa, água turquesa muito limpa, recifes de coral sob a superfície, praia de areia clara com coqueiros e falésias baixas ao fundo. Primeiro mapa de mar.',
                                'water': 'mar raso e turquesa sobre recife', 'light': 'manhã clara e quente, sol alto à esquerda', 'mood': 'chegada ao mar'}},
    'map_06': {'display_name': 'Arquipélago do Sol', 'level': 50, 'rod_tier': 3,
               'visual_theme': {'summary': 'Arquipélago tropical brasileiro em mar aberto: ilhas rochosas altas com vegetação, canais azul-cobalto e ondulação visível. Primeiro mar fundo.',
                                'water': 'mar fundo azul-cobalto', 'light': 'começo da tarde, dourado limpo', 'mood': 'longe da costa, peixes troféu'}},
    'map_07': {'display_name': 'Corrente Azul', 'level': 60, 'rod_tier': 3,
               'visual_theme': {'summary': 'Mar aberto sem terra à vista, ondulações longas, água azul intensa e horizonte enorme.',
                                'water': 'mar aberto azul intenso', 'light': 'fim de tarde dourado', 'mood': 'liberdade e distância'}},
    'map_08': {'display_name': 'Banco das Baleias', 'level': 70, 'rod_tier': 4,
               'visual_theme': {'summary': 'Banco oceânico com ilhotas rochosas muito distantes, água azul-esverdeada escura e baleias ao longe.',
                                'water': 'oceano profundo sobre bancos', 'light': 'amanhecer frio', 'mood': 'equipamento profissional'}},
    'map_09': {'display_name': 'Talude Noturno', 'level': 80, 'rod_tier': 4,
               'visual_theme': {'summary': 'Oceano profundo à noite, sem costa, água quase preta e brilho discreto de plâncton.',
                                'water': 'mar profundo noturno', 'light': 'crepúsculo azul e noite limpa', 'mood': 'mistério sem medo'}},
    'map_10': {'display_name': 'Abismo Atlântico', 'level': 90, 'rod_tier': 5,
               'visual_theme': {'summary': 'Mar oceânico extremamente profundo sob céu estrelado, água negro-azulada e fosforescência muito discreta.',
                                'water': 'abismo negro-azulado', 'light': 'noite profunda', 'mood': 'destino final'}},
}
ROD_TIER = {'rod_03': 3, 'rod_04': 4, 'rod_05': 5}
ROD_TEXT = {
    'rod_03': 'Uma vara de mar aberto, feita para peixes rápidos e grandes longe da costa.',
    'rod_04': 'Uma vara de oceano profundo, forte e precisa para os maiores peixes do Atlântico.',
    'rod_05': 'A vara definitiva, feita para o abismo e para os peixes que quase ninguém vê.',
}
RARITY_NOTE = {
    'legendary': 'Lendário (docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md): a partir do Mapa 6. Chance-base de puxar 14%. Tamanho: Grande × 0,6, Excepcional e Perfeição × 0,35, seguindo Raro e Épico (A-083).',
    'mythic': 'Mítico (docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md): só no Mapa 10. Chance-base de puxar 8%. Tamanho: Grande × 0,5, Excepcional e Perfeição × 0,25.',
}


def load(name):
    path = os.path.join(CONFIG, name)
    with open(path, encoding='utf-8') as f:
        return json.load(f, object_pairs_hook=collections.OrderedDict)


def save(name, data):
    with open(os.path.join(CONFIG, name), 'w', encoding='utf-8') as f:
        f.write(json.dumps(data, ensure_ascii=False, indent=2) + '\n')


def od(**kw):
    return collections.OrderedDict(kw)


def main(wanted):
    proposta = json.load(open(PROPOSTA, encoding='utf-8'))
    unknown = [m for m in wanted if m not in MAPAS]
    if not wanted or unknown:
        print(__doc__)
        sys.exit('Mapas desconhecidos: ' + ', '.join(unknown) if unknown else 1)

    progression, catalog, maps, rods, economy = (load(n) for n in ('progression.json', 'fish_catalog.json', 'maps.json', 'rods.json', 'economy.json'))
    species = [s for s in proposta['species'] if s['primary_map_id'] in wanted]

    # ---- raridades novas que esses mapas usam
    tiers = progression['rarity']['tiers']
    have = {t['id'] for t in tiers}
    needed = {s['rarity'] for s in species}
    for new in proposta['rarities_new']:
        if new['id'] in needed and new['id'] not in have:
            tiers.append(od(id=new['id'], display_name=new['display_name'], catch_success_base=new['catch_success_base'],
                            stat_multiplier=new['stat_multiplier'], fisher_xp_multiplier=new['fisher_xp_multiplier'],
                            feed_xp_multiplier=new['feed_xp_multiplier'], sale_value_multiplier=new['sale_value_multiplier'],
                            size_weight_multipliers=od(**new['size_weight_multipliers'])))
            progression['rarity'][new['id'] + '_note'] = RARITY_NOTE[new['id']]
            protected = economy['fishing_box']['bulk_sale_protection']['rarities']
            if new['id'] not in protected:
                protected.append(new['id'])
            print('   raridade', new['display_name'])

    # ---- espécies
    known = {s['id'] for s in catalog['species']}
    for s in species:
        if s['id'] in known:
            continue
        catalog['species'].append(od(id=s['id'], display_name=s['display_name'], primary_map_id=s['primary_map_id'], rarity=s['rarity'],
                                     size_cm=od(min=s['size_cm']['min'], max=s['size_cm']['max']),
                                     base_stats=od(hp=s['base_stats']['hp'], attack=s['base_stats']['attack'], defense=s['base_stats']['defense'], speed=s['base_stats']['speed']),
                                     base_sale_value_coins=s['base_sale_value_coins'], base_feed_xp=s['base_feed_xp'], base_fisher_xp=s['base_fisher_xp']))
    catalog['maps_5_10_note'] = 'Mapas 5 em diante: espécies de docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md (números adaptados por tools/Progressao/adaptar_mapas_5_10.py).'

    # ---- mapas
    have_maps = {m['id'] for m in maps['maps']}
    for mid in sorted(wanted):
        if mid in have_maps:
            continue
        meta = MAPAS[mid]
        fish = [s for s in species if s['primary_map_id'] == mid]
        rarities = [r for r in ('common', 'rare', 'epic', 'legendary', 'mythic') if any(s['rarity'] == r for s in fish)]
        maps['maps'].append(od(id=mid, display_name=meta['display_name'], playable_in_v0_1=False, unlock_fisher_level=meta['level'],
                               minimum_rod_tier=meta['rod_tier'], visual_theme=od(**meta['visual_theme']), available_rarities=rarities,
                               fish_pool_weight_total=sum(s['catch_weight'] for s in fish),
                               fish_pool=[od(species_id=s['id'], catch_weight=s['catch_weight']) for s in fish]))
        print('   mapa', meta['display_name'])
    maps['unlock_ladder_reference']['note'] = 'Escada de progressão da seção 17 do GDD. Os Mapas 5 a 10 seguem docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md e entram no jogo aos poucos.'

    # ---- varas mínimas dos mapas novos
    have_rods = {r['id'] for r in rods['rods']}
    tiers_needed = {MAPAS[m]['rod_tier'] for m in wanted}
    for prop in proposta['rods']:
        rid = prop['id']
        if ROD_TIER[rid] not in tiers_needed or rid in have_rods:
            continue
        b = prop['bonuses_per_level']
        rods['rods'].append(od(
            id=rid, display_name=prop['display_name'], description=ROD_TEXT[rid], tier=ROD_TIER[rid], implemented_in_v0_1=False,
            acquisition=od(method='coin_purchase', purchase_cost_coins=prop['purchase_cost_coins'], purchase_cost_shells=prop['purchase_cost_shells'],
                           unlock_fisher_level=prop['unlock_fisher_level']),
            has_internal_levels=True, can_catch_rarities=prop['can_catch_rarities'], generates_shells=True,
            bonuses_per_level=od(note='Os valores são o bônus total naquele nível interno, não um acréscimo por nível. Chance de puxar um terço menor que no documento do proprietário (A-095); demais bônus como no documento.',
                                 rarity_efficiency=b['rarity_efficiency'], size_quality=b['size_quality'], shell_yield=b['shell_yield'], catch_success=b['catch_success']),
            upgrade_costs=[od(to_level=u['to_level'], cost_coins=u['cost_coins'], cost_shells=u['cost_shells']) for u in prop['upgrade_costs']],
            npc_resale=od(base_value_ratio=0.4, upgrade_investment_ratio=0.25,
                          note='A revenda ao NPC devolve 40% do custo de compra mais 25% do total de Moedas gasto nas melhorias de nível interno.'),
            tradable_on_market=True, market_note='Uma vara negociada mantém o tier, o nível interno e os bônus.'))
        print('   vara', prop['display_name'])
    rods['tier_ladder_reference']['note'] = 'Escada de progressão da seção 19 do GDD. As Varas 3 a 5 seguem docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md e entram junto com os mapas delas.'

    for name, data in (('progression.json', progression), ('fish_catalog.json', catalog), ('maps.json', maps), ('rods.json', rods), ('economy.json', economy)):
        save(name, data)
    print('Pronto: %d espécies no catálogo, %d mapas, %d varas.' % (len(catalog['species']), len(maps['maps']), len(rods['rods'])))


if __name__ == '__main__':
    main(sys.argv[1:])
