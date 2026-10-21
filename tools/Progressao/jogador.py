"""O jogador de referência das simulações de ritmo (M24, depois da saída da Tripulação e das Melhorias, A-172).

Não é um script para rodar: é a peça comum de calibrar_xp.py, passe_fragmentos.py, fragmentos_equipamento.py e
niveis_100.py.

O que ele faz:
  - joga a pesca como tools/Progressao/simular_progressao.py (valor esperado, sem sorte): pesca online e offline no
    ciclo de progression.json → fishing, offline até 24 h, níveis de vara e de barco, iscas, barcos, varas e roupas por
    fragmentos, vende tudo ao NPC, sempre no melhor mapa liberado;
  - avança o tempo em passos pequenos (5 s na primeira hora, 30 s no primeiro dia, 2 min depois), para medir o nível
    a qualquer momento, e roda os três jeitos de jogar de PROFILES;
  - a única renda de Moedas é a pesca (desde 12/10/2026 não há Tripulação nem Melhorias, A-172).

Antes do A-172 este papel era de simular_tripulacao.py e simular_melhorias.py (removidos com a Tripulação).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import simular_progressao as S  # noqa: E402

ROOT = S.ROOT
CONFIG = S.CONFIG
LONG_MAX = 9223372036854775807

PROFILES = {
    'sempre_aberto': ('Deixa o jogo aberto o dia todo', None),
    'quatro_vezes': ('Abre 4 vezes por dia, 30 min de cada vez', [(8, 30), (12, 30), (18, 30), (22, 30)]),
    'uma_vez': ('Abre 1 vez por dia, 30 min', [(20, 30)]),
}


def step_for(t):
    """Seconds per simulation step at time t (finer at the start, where the levels come fast)."""
    return 5.0 if t < 3600 else 30.0 if t < 86400 else 120.0


class Game:
    """The reference player: fishing only (online and offline), with the shopping of simular_progressao.Player."""

    def __init__(self, rules=None):
        self.rules = rules or S.Rules()
        self.p = S.Player(self.rules)
        self.t = 0.0                 # seconds
        self.carry = 0.0
        self.start = None            # first moment the game is open (the first session of a profile)

    def add_xp(self, xp):
        p, r = self.p, self.rules
        p.xp += xp
        while p.level < r.max_level and p.xp >= r.xp_to_next[p.level]:
            p.xp -= r.xp_to_next[p.level]
            p.level += 1
            p.reached[p.level] = self.t / 86400.0

    def fishing_coins_per_second(self):
        _, coins, _, _ = self.p.stats()
        return coins / self.rules.fishing['online_cycle_seconds']

    def income(self):
        """Moedas per second with the game open: the fishing (the only steady income since A-172)."""
        return self.fishing_coins_per_second()

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
            self.p.shop()
            self.observe()

    def offline(self, seconds):
        r = self.rules
        attempts = int(min(seconds, r.fishing['offline_accumulation_cap_hours'] * 3600) / r.fishing['offline_cycle_seconds'])
        self.t += seconds
        self.p.offline = True
        self.p.fish(attempts, self.t / 86400.0)
        self.p.offline = False
        self.observe()

    def observe(self):
        """Called after every step; subclasses record what they measure."""


def play(game, profile, days, stop_at_level=None):
    """Plays `game` for `days` with the sessions of `profile`; stops early at `stop_at_level` when given."""
    g = game
    sessions = PROFILES[profile][1]
    end = days * 86400.0
    done = lambda: stop_at_level is not None and g.p.level >= stop_at_level  # noqa: E731
    if sessions is None:
        while g.t < end and not done():
            g.online(step_for(g.t) * 20, step_for(g.t))
        return g
    last_end = None
    day = 0
    while g.t < end and not done():
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
