"""Curvas de nível de varas, barcos e peixes até o Nv.100 (M24-T13, A-156, TD-041).

As mesmas contas de client-unity/Assets/Scripts/GameService/Config/LevelCurve.cs:

  bônus(N)  = bônus_nv1 + (bônus_nv_max − bônus_nv1) × ((N − 1) ÷ (máx − 1)) ^ expoente
  custo(N)  = primeiro × crescimento ^ (N − 1)      (de N para N + 1), arredondado:
              abaixo de 1.000, para o inteiro mais próximo (no mínimo 1);
              a partir de 1.000, para 3 algarismos significativos (12.345 → 12.300).

Usado por simular_progressao.py (varas e barcos com nível) e por niveis_100.py (gera os números e simula o ritmo).
"""
import math

LONG_MAX = 9.2e18


def bonus_at(at_level_1, at_max, level, max_level, exponent):
    """O bônus total no nível (1 = o de hoje, máx = o do topo)."""
    if max_level <= 1:
        return at_level_1
    level = max(1, min(max_level, level))
    t = (level - 1) / float(max_level - 1)
    return at_level_1 + (at_max - at_level_1) * t ** exponent


def nice_round(raw):
    """Arredonda um preço como o jogo (LevelCurve.RoundPrice)."""
    if raw >= LONG_MAX:
        return float('inf')
    if raw < 1000:
        return max(1, int(math.floor(raw + 0.5)))
    digits = int(math.floor(math.log10(raw)))
    unit = 10 ** (digits - 2)
    return int(math.floor(raw / unit + 0.5)) * unit


def cost(first, growth, level):
    """Preço para ir do nível `level` ao seguinte (0 quando o primeiro é 0)."""
    if first <= 0:
        return 0
    return nice_round(first * growth ** (level - 1))


AXES = ('rarity_efficiency', 'size_quality', 'shell_yield', 'catch_success')


def rod_max_level(rods_cfg, rod):
    if not rod.get('has_internal_levels'):
        return 1
    return rods_cfg['upgrade_rules']['internal_levels']['max']


def rod_bonus(rods_cfg, rod, level):
    """Os quatro bônus da vara no nível (vara sem nível: os bônus fixos)."""
    if not rod.get('has_internal_levels'):
        b = rod.get('bonuses') or {}
        return {k: b.get(k, 0.0) for k in AXES}
    exp = rods_cfg['upgrade_rules']['bonus_curve_exponent']
    mx = rod_max_level(rods_cfg, rod)
    a, z = rod['bonuses_at_level_1'], rod['bonuses_at_max_level']
    return {k: bonus_at(a[k], z[k], level, mx, exp) for k in AXES}


def rod_cost(rods_cfg, rod, level):
    """(Moedas, Conchas) para ir do nível ao seguinte; None no nível máximo ou sem nível."""
    if not rod.get('has_internal_levels') or level >= rod_max_level(rods_cfg, rod):
        return None
    c = rod['upgrade_cost']
    return cost(c['coins_first'], c['coins_growth'], level), cost(c['shells_first'], c['shells_growth'], level)


def boat_max_level(eq_cfg, boat):
    return eq_cfg['boat_levels']['max_level'] if boat.get('has_levels') else 1


def boat_bonus(eq_cfg, boat, level):
    if not boat.get('has_levels'):
        return boat['catch_success_bonus']
    return bonus_at(boat['catch_success_bonus'], boat['catch_success_bonus_at_max_level'], level,
                    boat_max_level(eq_cfg, boat), eq_cfg['boat_levels']['bonus_curve_exponent'])


def boat_cost(eq_cfg, boat, level):
    if not boat.get('has_levels') or level >= boat_max_level(eq_cfg, boat):
        return None
    c = boat['upgrade_cost']
    return cost(c['coins_first'], c['coins_growth'], level), cost(c['shells_first'], c['shells_growth'], level)


def fish_stat_bonus(fish_cfg, level):
    """Bônus de atributo do peixe no nível (0,0 no Nv.1; stat_bonus_at_max_level_percent ÷ 100 no máximo)."""
    return bonus_at(0.0, fish_cfg['stat_bonus_at_max_level_percent'] / 100.0, level, fish_cfg['max_level'],
                    fish_cfg['stat_bonus_curve_exponent'])
