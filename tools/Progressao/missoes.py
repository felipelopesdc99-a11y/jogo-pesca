"""Missões (M24-T54, A-177): gera config/missions.json com as 100 missões e os prêmios, e a lista legível.

Uso:
  python3 tools/Progressao/missoes.py gerar [--dias 170]
      # roda o jogador de referência (4 vezes por dia, com o Passe grátis; tools/Progressao/jogador.py e
      # passe_fragmentos.py) e reescreve config/missions.json:
      # as 100 missões de MISSOES abaixo, na ordem de docs/propostas/missoes_100.txt, com os prêmios calculados
  python3 tools/Progressao/missoes.py lista [arquivo]
      # escreve a lista das 100 missões com o prêmio de cada uma (PT-BR, números no formato brasileiro), lida de
      # config/missions.json; sem arquivo, imprime na tela

Como os prêmios são calculados (ajuste as constantes e rode "gerar" de novo; ou edite config/missions.json à mão):
  - cada missão tem um nível de referência (NIVEL abaixo): o nível do Pescador em que o jogador de referência deve
    concluí-la. Ao longo da lista o nível nunca desce (uma missão mais para baixo nunca paga menos que uma de cima);
  - Moedas: N minutos da renda da pesca que o jogador de referência tem ao chegar àquele nível, com N indo de
    MINUTOS_INICIO (missão 1) a MINUTOS_FIM (missão 100) em linha reta; Conchas: CONCHAS_MULT × N minutos de Conchas
    da pesca (o mesmo jeito do Passe do Pescador, que dá ~10 min de Moedas e ~30 min de Conchas por marco);
  - arredondamento "bonito": 2 algarismos significativos (1.700, 130.000, 19.000.000);
  - Dólares: só nas missões difíceis (DOLARES abaixo); o total fica entre o que o Passe grátis dá até o Nv.100
    (100 Dólares) e o dobro disso.
"""
import argparse
import collections
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import passe_fragmentos as P  # noqa: E402
import simular_progressao as S  # noqa: E402

ROOT = S.ROOT
CONFIG = S.CONFIG
ARQUIVO = 'missions.json'

MINUTOS_INICIO = 10.0
MINUTOS_FIM = 60.0
CONCHAS_MULT = 3.0
PERFIL_REFERENCIA = 'quatro_vezes'

# As 100 missões, na ordem da lista aprovada (docs/propostas/missoes_100.txt): (tipo, parâmetros, nível de referência).
# Os tipos são as chaves técnicas de GameService/Missions/MissionTypes.cs; "target" 0 = "todas" (onde o tipo aceita).
MISSOES = [
    ('catch_count', {'target': 10}, 2),
    ('box_sell', {'target': 10}, 2),
    ('aquarium_keep', {'target': 1}, 3),
    ('fisher_level', {'target': 5}, 5),
    ('catch_species', {'target': 5, 'species_id': 'lambari'}, 5),
    ('catch_count', {'target': 100}, 8),
    ('fish_feed', {'target': 1}, 8),
    ('fish_level', {'target': 5}, 10),
    ('cardume_size', {'target': 3}, 10),
    ('catch_size_category', {'target': 1, 'size_category': 'large'}, 12),
    ('coins_held', {'target': 10000}, 15),
    ('bait_used', {'target': 1, 'bait_id': 'bait_01'}, 15),
    ('fisher_level', {'target': 25}, 25),
    ('encyclopedia_species', {'target': 10}, 25),
    ('catch_gene_stars', {'target': 1, 'stars': 4}, 25),
    ('rod_assembled', {'target': 1, 'rod_id': 'rod_01'}, 25),
    ('map_unlocked', {'target': 1, 'map_id': 'map_02'}, 25),
    ('arena_duels', {'target': 1}, 25),
    ('catch_count', {'target': 1000}, 30),
    ('catch_rarity', {'target': 1, 'rarity': 'rare'}, 30),
    ('expedition_started', {'target': 1}, 30),
    ('profile_visit', {'target': 1}, 30),
    ('arena_wins', {'target': 5}, 35),
    ('market_listing', {'target': 1}, 35),
    ('market_buy', {'target': 1}, 35),
    ('boat_assembled', {'target': 1, 'boat_id': 'boat_01'}, 40),
    ('clothing_assembled', {'target': 1}, 40),
    ('pass_rewards_claimed', {'target': 10}, 50),
    ('fisher_level', {'target': 50}, 50),
    ('map_all_species', {'map_id': 'map_01'}, 50),
    ('shells_held', {'target': 100}, 50),
    ('map_unlocked', {'target': 1, 'map_id': 'map_03'}, 50),
    ('clan_join', {'target': 1}, 100),
    ('fisher_level', {'target': 100}, 100),
    ('catch_rarity', {'target': 1, 'rarity': 'epic'}, 100),
    ('catch_rarity', {'target': 50, 'rarity': 'rare'}, 110),
    ('bait_used', {'target': 1, 'bait_id': 'bait_03'}, 110),
    ('catch_count', {'target': 10000}, 120),
    ('fish_level', {'target': 25}, 120),
    ('catch_size_category', {'target': 1, 'size_category': 'exceptional'}, 130),
    ('catch_gene_stars', {'target': 1, 'stars': 5}, 140),
    ('catch_perfect_gene', {'target': 1}, 140),
    ('boss_attack', {'target': 1}, 150),
    ('clan_fish', {'target': 1}, 150),
    ('boss_band', {'target': 3}, 160),
    ('arena_wins', {'target': 50}, 170),
    ('arena_win_streak', {'target': 3}, 170),
    ('expeditions_completed', {'target': 10}, 180),
    ('market_sell', {'target': 1, 'rarity': 'rare'}, 180),
    ('auction_bid', {'target': 1}, 180),
    ('rod_assembled', {'target': 1, 'rod_id': 'rod_02'}, 190),
    ('boat_assembled', {'target': 1, 'boat_id': 'boat_02'}, 190),
    ('arena_distinct_opponents', {'target': 10}, 200),
    ('clothing_assembled', {'target': 5}, 200),
    ('clothing_max_level', {'target': 1}, 220),
    ('map_unlocked', {'target': 1, 'map_id': 'map_05'}, 220),
    ('encyclopedia_species', {'target': 40}, 230),
    ('coins_held', {'target': 1000000}, 230),
    ('fisher_level', {'target': 250}, 250),
    ('cardume_all_rarity_min', {'rarity': 'rare'}, 250),
    ('arena_win_stronger', {'target': 1}, 260),
    ('cardume_strength', {'target': 100000}, 270),
    ('arena_rank_top', {'target': 100}, 280),
    ('pass_rewards_claimed', {'target': 50}, 300),
    ('dollars_spent', {'target': 100}, 300),
    ('catch_rarity', {'target': 1, 'rarity': 'legendary'}, 320),
    ('map_unlocked', {'target': 1, 'map_id': 'map_06'}, 320),
    ('catch_count', {'target': 100000}, 350),
    ('market_buy', {'target': 1, 'goods_kind': 'fish'}, 350),
    ('fish_level', {'target': 100}, 380),
    ('catch_size_category', {'target': 1, 'size_category': 'perfect'}, 400),
    ('catch_gene_stars', {'target': 10, 'stars': 5}, 400),
    ('cardume_gene_stars', {'target': 3, 'stars': 5}, 420),
    ('boss_days', {'target': 7}, 420),
    ('boss_band', {'target': 0}, 450),
    ('arena_wins', {'target': 500}, 480),
    ('arena_rank_top', {'target': 10}, 500),
    ('market_sell', {'target': 1}, 500),
    ('expeditions_completed', {'target': 100}, 520),
    ('market_sell', {'target': 50}, 520),
    ('auction_won', {'target': 10}, 540),
    ('rod_assembled', {'target': 1, 'rod_id': 'rod_03'}, 550),
    ('rod_assembled', {'target': 1, 'rod_id': 'rod_04'}, 580),
    ('rod_assembled', {'target': 1, 'rod_id': 'rod_05'}, 620),
    ('auction_won', {'target': 1, 'contested': True}, 620),
    ('boats_all', {}, 650),
    ('clothing_max_level', {'target': 0}, 680),
    ('map_unlocked', {'target': 1, 'map_id': 'map_10'}, 700),
    ('fisher_level', {'target': 500}, 700),
    ('coins_held', {'target': 1000000000}, 720),
    ('encyclopedia_species', {'target': 0}, 750),
    ('clan_same_day_boss', {'target': 1, 'online_only': True}, 760),
    ('rarity_all_species', {'rarity': 'legendary'}, 780),
    ('catch_species', {'target': 1, 'species_id': 'tubarao_boca_grande'}, 820),
    ('size_records', {'target': 20}, 850),
    ('cardume_all_level', {'level': 100}, 880),
    ('catch_count', {'target': 1000000}, 920),
    ('catch_perfect_specimen', {'target': 1}, 960),
    ('clan_rank_top', {'target': 3, 'online_only': True}, 980),
    ('fisher_level', {'target': 1000}, 1000),
]

# Dólares nas missões difíceis (número da missão na lista → Dólares).
DOLARES = {
    35: 5,    # primeiro Épico
    40: 5,    # tamanho Excepcional
    41: 5,    # 5 estrelas
    42: 5,    # gene perfeito
    66: 10,   # primeiro Lendário
    71: 10,   # tamanho Perfeição
    75: 5,    # faixa máxima do Boss Mundial
    77: 10,   # top 10 da Arena
    86: 5,    # os 5 barcos
    87: 5,    # todas as roupas no máximo
    89: 10,   # Nv.500
    91: 15,   # todas as espécies
    93: 10,   # todas as Lendárias
    94: 10,   # o Mítico
    96: 10,   # Cardume completo no Nv.100
    97: 10,   # 1 milhão de peixes
    98: 20,   # Exemplar perfeito
    100: 20,  # Nv.1000
}


def load(name):
    with open(os.path.join(CONFIG, name), encoding='utf-8') as f:
        return json.load(f, object_pairs_hook=collections.OrderedDict)


def save(name, data):
    with open(os.path.join(CONFIG, name), 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')


def bonito(x):
    """2 algarismos significativos: 1.710 → 1.700; 125.866 → 130.000; abaixo de 100, o inteiro mais perto (mínimo 1)."""
    if x < 100:
        return max(1, int(math.floor(x + 0.5)))
    digits = int(math.floor(math.log10(x)))
    unit = 10 ** (digits - 1)
    return int(math.floor(x / unit + 0.5)) * unit


def renda_no_nivel(reference, level):
    """Moedas/s, Conchas/s e dia do jogador de referência ao chegar a `level` (entre os marcos de 10 em 10, em linha reta)."""
    known = sorted(reference.at_level)
    coins = P.smooth([reference.at_level[k]['renda_moedas_s'] for k in known])
    shells = P.smooth([reference.at_level[k]['conchas_s'] for k in known])
    days = [reference.at_level[k]['dia'] for k in known]
    if level <= known[0]:
        return coins[0], shells[0], days[0] * level / float(known[0])
    for i in range(1, len(known)):
        if level <= known[i]:
            a, b = known[i - 1], known[i]
            f = (level - a) / float(b - a)
            return (coins[i - 1] + (coins[i] - coins[i - 1]) * f, shells[i - 1] + (shells[i] - shells[i - 1]) * f,
                    days[i - 1] + (days[i] - days[i - 1]) * f)
    return coins[-1], shells[-1], days[-1]


def gerar(dias):
    assert len(MISSOES) == 100, len(MISSOES)
    print('Jogador de referência (%s, com o Passe grátis, até %d dias)…' % (PERFIL_REFERENCIA, dias))
    reference = P.play(PERFIL_REFERENCIA, dias, pass_cfg=load('pass.json'))
    missions = []
    nivel = 0
    moedas_antes = conchas_antes = 0
    for i, (tipo, params, ref) in enumerate(MISSOES):
        numero = i + 1
        nivel = max(nivel, ref)
        minutos = MINUTOS_INICIO + (MINUTOS_FIM - MINUTOS_INICIO) * i / float(len(MISSOES) - 1)
        moedas_s, conchas_s, _ = renda_no_nivel(reference, nivel)
        moedas = max(moedas_antes, bonito(moedas_s * minutos * 60))
        conchas = max(conchas_antes, bonito(conchas_s * minutos * CONCHAS_MULT * 60))
        moedas_antes, conchas_antes = moedas, conchas
        m = collections.OrderedDict()
        m['id'] = 'mission_%03d' % numero
        m['type'] = tipo
        m['target'] = params.get('target', 0)
        for chave in ('species_id', 'rarity', 'size_category', 'map_id', 'rod_id', 'boat_id', 'bait_id', 'goods_kind', 'stars', 'level', 'contested', 'online_only'):
            if chave in params:
                m[chave] = params[chave]
        m['reference_level'] = nivel
        m['reward'] = collections.OrderedDict([('coins', moedas), ('shells', conchas), ('dollars', DOLARES.get(numero, 0))])
        missions.append(m)

    total_dolares = sum(DOLARES.values())
    data = collections.OrderedDict()
    data['config_file'] = 'missions'
    data['config_schema_version'] = 1
    data['balance_status'] = 'PROVISÓRIO'
    data['description'] = (
        'Missões (M24-T54, A-177, pedido do proprietário em 13/10/2026): 100 missões que acompanham o jogo inteiro, na ordem '
        'natural do jogo (docs/propostas/missoes_100.txt), todas visíveis desde o começo. Cada uma tem um tipo (chave técnica), '
        'o alvo e os parâmetros do tipo; o texto que o jogador lê é montado pelo jogo (GameTexts.Missions) a partir deles. '
        'Prêmios provisórios (OD-073): gerados por tools/Progressao/missoes.py gerar; pode editar à mão.')
    data['types_note'] = (
        'Tipos: catch_count (peixes pescados), catch_species (species_id), catch_rarity (rarity), catch_size_category '
        '(size_category), catch_gene_stars (stars: peixes com pelo menos tantas estrelas), catch_perfect_gene, '
        'catch_perfect_specimen, fisher_level, coins_held, shells_held, dollars_spent, box_sell (peixes vendidos da Caixa), '
        'aquarium_keep, fish_feed, fish_level (nível de um peixe do Aquário), cardume_size, cardume_strength, '
        'cardume_all_rarity_min (rarity; o Cardume completo), cardume_gene_stars (stars), cardume_all_level (level), '
        'map_unlocked (map_id), encyclopedia_species (target 0 = todas), map_all_species (map_id), rarity_all_species (rarity), '
        'size_records (espécies com o recorde de tamanho batido), rod_assembled (rod_id), boat_assembled (boat_id), boats_all, '
        'clothing_assembled e clothing_max_level (target 0 = todas), bait_used (bait_id), pass_rewards_claimed, '
        'expedition_started, expeditions_completed, arena_duels, arena_wins, arena_win_streak, arena_distinct_opponents, '
        'arena_win_stronger (vencer quem está acima de você no Ranking), arena_rank_top (target = a posição), market_listing, '
        'market_buy (goods_kind "fish" = só peixes), market_sell (peixes; rarity opcional), auction_bid, auction_won '
        '(contested = alguém mais deu lance), boss_attack, boss_band (target 0 = a faixa máxima), boss_days, clan_join, '
        'clan_fish, clan_same_day_boss, clan_rank_top, profile_visit. reference_level: o nível do Pescador em que o jogador '
        'de referência deve concluir a missão (só o script usa).')
    data['rewards_note'] = (
        'Prêmios: Moedas = N minutos da renda da pesca do jogador de referência (4 vezes por dia) ao chegar ao '
        'reference_level, com N de %d (missão 1) a %d (missão 100); Conchas = %d × N minutos das Conchas da pesca; '
        '2 algarismos significativos; nenhuma missão paga menos que a de cima. Dólares só nas %d missões difíceis, '
        '%d no total (o Passe grátis dá 100 até o Nv.100).' % (MINUTOS_INICIO, MINUTOS_FIM, CONCHAS_MULT, len(DOLARES), total_dolares))
    data['online_only_note'] = (
        'online_only: a missão precisa de outros jogadores de verdade e só conclui com o jogo online; no jogo local ela '
        'aparece com "Liga quando o jogo estiver online". No jogo local, os adversários da Arena e os vendedores, compradores '
        'e licitantes do Mercado e do Leilão são jogadores simulados e contam como "outro jogador" (A-177); os do Clã não '
        'existem localmente: o Boss no mesmo dia que outro membro e o top 3 de Clãs ficam online_only.')
    data['missions'] = missions
    save(ARQUIVO, data)
    print('config/missions.json: %d missões. Total: %s Moedas, %s Conchas, %d Dólares.'
          % (len(missions), fmt(sum(m['reward']['coins'] for m in missions)), fmt(sum(m['reward']['shells'] for m in missions)), total_dolares))


def fmt(v):
    return '{:,}'.format(int(v)).replace(',', '.')


# ---------------------------------------------------------------- a lista legível (a mesma regra de GameTexts.Missions)

def nome(cfg, tipo, m):
    if tipo == 'catch_species':
        sp = cfg['species'][m['species_id']]
        n = sp['display_name']
        if sp['rarity'] != 'common':
            n += ' (' + cfg['rarities'][sp['rarity']] + ')'
        return n
    if m.get('rarity'):
        return cfg['rarities'][m['rarity']]
    if m.get('size_category'):
        return cfg['sizes'][m['size_category']]
    for chave, tabela in (('map_id', 'maps'), ('rod_id', 'rods'), ('boat_id', 'boats'), ('bait_id', 'baits')):
        if m.get(chave):
            return cfg[tabela][m[chave]]
    return None


def primeira(n):
    for sep in '- ':
        if sep in n:
            n = n.split(sep)[0]
    return n


def artigo(n):
    return 'a' if primeira(n).endswith('a') else 'o'


def plural(n):
    w = primeira(n)
    rest = n[len(w):]
    if w.endswith('ão'):
        p = w[:-2] + 'ões'
    elif w.endswith('m'):
        p = w[:-1] + 'ns'
    elif w.endswith('l'):
        p = w[:-1] + 'is'
    elif w.endswith('r') or w.endswith('z'):
        p = w + 'es'
    elif w.endswith('s'):
        p = w
    else:
        p = w + 's'
    return p + rest


def quantia(v, substantivo):
    if v >= 10 ** 9 and v % 10 ** 9 == 0:
        b = v // 10 ** 9
        return fmt(b) + (' bilhão de ' if b == 1 else ' bilhões de ') + substantivo.replace('de ', '', 1)
    if v >= 10 ** 6 and v % 10 ** 6 == 0:
        mi = v // 10 ** 6
        return fmt(mi) + (' milhão de ' if mi == 1 else ' milhões de ') + substantivo.replace('de ', '', 1)
    return fmt(v) + ' ' + substantivo


def alvo(cfg, m):
    t = m['type']
    if t == 'map_all_species':
        return len(cfg['pools'][m['map_id']])
    if t == 'rarity_all_species':
        return sum(1 for s in cfg['species'].values() if s['rarity'] == m['rarity'])
    if t == 'boats_all':
        return cfg['n_boats']
    return m.get('target', 0)


def texto(cfg, m):
    t = m['type']
    a = alvo(cfg, m)
    um = a == 1
    n = fmt(a)
    nm = nome(cfg, t, m)
    todas = a == 0
    s = m.get('stars', 0)
    g = cfg['gene_max']
    T = {
        'catch_count': lambda: 'Pesque 1 peixe' if um else 'Pesque ' + quantia(a, 'peixes'),
        'catch_species': lambda: ('Pesque ' + artigo(nm) + ' ' + nm) if um else 'Pesque ' + n + ' ' + plural(nm),
        'catch_rarity': lambda: ('Pesque o primeiro peixe ' + nm) if um else 'Pesque ' + n + ' peixes ' + plural(nm),
        'catch_size_category': lambda: ('Pesque um peixe de tamanho ' + nm) if um else 'Pesque ' + n + ' peixes de tamanho ' + nm,
        'catch_gene_stars': lambda: ('Pesque um peixe %d estrelas' % s) if um else 'Pesque %s peixes %d estrelas' % (n, s),
        'catch_perfect_gene': lambda: 'Pesque um peixe com um gene perfeito (%d)' % g,
        'catch_perfect_specimen': lambda: 'Pesque um Exemplar perfeito (os 4 genes em %d)' % g,
        'fisher_level': lambda: 'Chegue ao Nv.%d de Pescador' % a,
        'coins_held': lambda: 'Junte ' + quantia(a, 'Moedas'),
        'shells_held': lambda: 'Junte ' + quantia(a, 'Conchas'),
        'dollars_spent': lambda: 'Gaste ' + quantia(a, 'Dólares'),
        'box_sell': lambda: 'Venda ' + n + ' peixes da Caixa de Pesca',
        'aquarium_keep': lambda: 'Guarde 1 peixe no Aquário' if um else 'Guarde ' + n + ' peixes no Aquário',
        'fish_feed': lambda: 'Alimente um peixe do Aquário pela primeira vez',
        'fish_level': lambda: 'Suba um peixe do Aquário ao Nv.%d' % a,
        'cardume_size': lambda: 'Monte seu Cardume com ' + n + ' peixes',
        'cardume_strength': lambda: 'Alcance ' + quantia(a, 'de força no Cardume'),
        'cardume_all_rarity_min': lambda: 'Monte um Cardume completo só com peixes ' + plural(nm) + ' ou melhores',
        'cardume_gene_stars': lambda: 'Tenha %s peixes %d estrelas no Cardume ao mesmo tempo' % (n, s),
        'cardume_all_level': lambda: 'Tenha um Cardume completo com todos os peixes no Nv.%d' % m['level'],
        'map_unlocked': lambda: 'Libere ' + artigo(nm) + ' ' + nm,
        'encyclopedia_species': lambda: 'Registre todas as espécies na Enciclopédia' if todas else 'Registre ' + n + ' espécies na Enciclopédia',
        'map_all_species': lambda: 'Pesque todas as ' + n + ' espécies ' + ('da' if artigo(nm) == 'a' else 'do') + ' ' + nm,
        'rarity_all_species': lambda: 'Pesque todas as espécies ' + (nm[:-1] + 'as' if nm.endswith('o') else plural(nm)),
        'size_records': lambda: 'Bata o recorde de tamanho de ' + n + ' espécies diferentes',
        'rod_assembled': lambda: 'Monte a vara ' + nm,
        'boat_assembled': lambda: 'Monte o barco ' + nm,
        'boats_all': lambda: 'Monte todos os ' + n + ' barcos',
        'clothing_assembled': lambda: 'Monte todas as peças de roupa' if todas else 'Monte a primeira peça de roupa' if um else 'Monte as ' + n + ' peças de roupa',
        'clothing_max_level': lambda: 'Suba todas as roupas ao nível máximo' if todas else 'Suba uma peça de roupa ao nível máximo' if um else 'Suba ' + n + ' peças de roupa ao nível máximo',
        'bait_used': lambda: 'Use uma isca ' + nm,
        'pass_rewards_claimed': lambda: 'Pegue ' + n + ' recompensas do Passe do Pescador',
        'expedition_started': lambda: 'Mande o Cardume na primeira Expedição',
        'expeditions_completed': lambda: 'Complete ' + n + ' Expedições',
        'arena_duels': lambda: 'Faça seu primeiro duelo na Arena',
        'arena_wins': lambda: 'Ganhe ' + n + ' duelos na Arena',
        'arena_win_streak': lambda: 'Ganhe ' + n + ' duelos seguidos na Arena',
        'arena_distinct_opponents': lambda: 'Ataque ' + n + ' jogadores diferentes na Arena',
        'arena_win_stronger': lambda: 'Vença um jogador mais forte que você na Arena',
        'arena_rank_top': lambda: 'Chegue ao top ' + n + ' do Ranking da Arena',
        'market_listing': lambda: 'Coloque o primeiro anúncio no Mercado',
        'market_buy': lambda: 'Compre um peixe anunciado por outro jogador no Mercado' if m.get('goods_kind') == 'fish' else 'Compre um item no Mercado',
        'market_sell': lambda: ('Venda 1 peixe ' + nm + ' no Mercado') if nm else ('Venda um peixe para outro jogador no Mercado' if um else 'Venda ' + n + ' peixes no Mercado'),
        'auction_bid': lambda: 'Dê um lance num Leilão',
        'auction_won': lambda: 'Ganhe um Leilão contra outros jogadores' if m.get('contested') else ('Ganhe um Leilão' if um else 'Ganhe ' + n + ' Leilões'),
        'boss_attack': lambda: 'Ataque o Boss Mundial pela primeira vez',
        'boss_band': lambda: 'Chegue à faixa máxima de dano do Boss Mundial' if todas else 'Chegue à faixa ' + n + ' de dano do Boss Mundial',
        'boss_days': lambda: 'Ataque o Boss Mundial em ' + n + ' dias diferentes',
        'clan_join': lambda: 'Entre em um Clã ou crie o seu',
        'clan_fish': lambda: 'Coloque seu melhor peixe no Cardume do Clã',
        'clan_same_day_boss': lambda: 'Ataque o Boss Mundial no mesmo dia que outro membro do seu Clã',
        'clan_rank_top': lambda: 'Fique no top ' + n + ' de Clãs no ranking diário',
        'profile_visit': lambda: 'Visite o perfil de outro jogador',
    }
    return T[t]()


def config_names():
    fish = load('fish_catalog.json')
    prog = load('progression.json')
    maps = load('maps.json')
    rods = load('rods.json')
    eq = load('equipment.json')
    return {
        'species': {s['id']: s for s in fish['species']},
        'rarities': {r['id']: r['display_name'] for r in prog['rarity']['tiers']},
        'sizes': {c['id']: c['display_name'] for c in prog['size']['categories']},
        'maps': {m['id']: m['display_name'] for m in maps['maps']},
        'pools': {m['id']: sorted({p['species_id'] for p in m['fish_pool']}) for m in maps['maps']},
        'rods': {r['id']: r['display_name'] for r in rods['rods']},
        'boats': {b['id']: b['display_name'] for b in eq['boats']},
        'baits': {b['id']: b['display_name'] for b in eq['baits']},
        'n_boats': sum(1 for b in eq['boats'] if b.get('tier', 0) > 0),
        'gene_max': prog['genes']['max_value'],
    }


def lista(arquivo):
    cfg = config_names()
    data = load(ARQUIVO)
    linhas = ['MISSÕES — Fishing Idle: as 100 missões e o prêmio de cada uma (provisório, OD-073)',
              'Moedas / Conchas / Dólares. "online" = só conclui com o jogo online.', '']
    tm = tc = td = 0
    for i, m in enumerate(data['missions']):
        r = m['reward']
        tm += r['coins']
        tc += r['shells']
        td += r['dollars']
        premio = fmt(r['coins']) + ' Moedas / ' + fmt(r['shells']) + ' Conchas'
        if r['dollars']:
            premio += ' / ' + fmt(r['dollars']) + ' Dólares'
        extra = '  [online]' if m.get('online_only') else ''
        linhas.append('%3d. %s%s\n     Prêmio: %s' % (i + 1, texto(cfg, m), extra, premio))
    linhas += ['', 'Total: %s Moedas, %s Conchas e %s Dólares.' % (fmt(tm), fmt(tc), fmt(td)),
               'Missões com Dólares: %d.' % sum(1 for m in data['missions'] if m['reward']['dollars'])]
    out = '\n'.join(linhas) + '\n'
    if arquivo:
        with open(arquivo, 'w', encoding='utf-8') as f:
            f.write(out)
        print('Lista escrita em ' + arquivo)
    else:
        print(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('acao', choices=('gerar', 'lista'))
    ap.add_argument('arquivo', nargs='?')
    ap.add_argument('--dias', type=float, default=170)
    a = ap.parse_args()
    if a.acao == 'gerar':
        gerar(a.dias)
    else:
        lista(a.arquivo)


if __name__ == '__main__':
    main()
