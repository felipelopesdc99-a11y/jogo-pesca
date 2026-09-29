"""Generates the game's icon family (Art Bible, section 12) as white glyphs on transparency.

One stroke weight, round caps, the same 100x100 grid for every icon, so they read as one set.
The game tints them at runtime (white = the text colour, turquoise = action, gold = reward).
The coin and the shell are the only coloured icons.

Usage:  python3 tools/Arte/gerar_icones.py        (writes client-unity/Assets/Resources/Arte/Icones)
Always produces the same files. To use a drawn icon instead, replace the PNG keeping its name.
"""
import math
import os

import finais
from PIL import Image, ImageDraw, ImageFilter

SIZE = 96
SS = 4  # supersampling
C = SIZE * SS
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'client-unity', 'Assets', 'Resources', 'Arte', 'Icones')
W = 8.5  # stroke width on the 100 grid
WHITE = (255, 255, 255, 255)


def P(x, y):
    return (x * C / 100.0, y * C / 100.0)


class Pen:
    def __init__(self):
        self.img = Image.new('RGBA', (C, C), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def line(self, pts, w=W, col=WHITE):
        pts = [P(*p) for p in pts]
        r = w * C / 200.0
        self.d.line(pts, fill=col, width=int(round(w * C / 100.0)), joint='curve')
        for p in pts:
            self.d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=col)

    def arc(self, cx, cy, r, a0, a1, w=W, col=WHITE, steps=48):
        pts = [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps + 1)]
        self.line(pts, w, col)

    def poly(self, pts, col=WHITE):
        self.d.polygon([P(*p) for p in pts], fill=col)

    def circle(self, cx, cy, r, col=WHITE):
        a, b = P(cx - r, cy - r), P(cx + r, cy + r)
        self.d.ellipse([a, b], fill=col)

    def ring(self, cx, cy, r, w=W, col=WHITE):
        self.arc(cx, cy, r, 0, 360, w, col, 96)

    def rrect(self, x0, y0, x1, y1, rad, col=WHITE, outline=False, w=W):
        a, b = P(x0, y0), P(x1, y1)
        if outline:
            self.d.rounded_rectangle([a, b], radius=rad * C / 100.0, outline=col, width=int(round(w * C / 100.0)))
        else:
            self.d.rounded_rectangle([a, b], radius=rad * C / 100.0, fill=col)

    def cut_circle(self, cx, cy, r):
        self.circle(cx, cy, r, (0, 0, 0, 0))

    def save(self, name):
        img = self.img.resize((SIZE, SIZE), Image.LANCZOS)
        img.save(os.path.join(OUT, 'ico_' + name + '.png'), optimize=True)


def fish_shape(p, x0=6, y0=50, length=72, height=46, col=WHITE):
    # Body (ellipse) + tail triangle; eye cut out.
    p.d.ellipse([P(x0 + 16, y0 - height / 2), P(x0 + 16 + length, y0 + height / 2)], fill=col)
    p.poly([(x0 + 20, y0), (x0, y0 - height * 0.48), (x0 + 4, y0), (x0, y0 + height * 0.48)], col)
    p.cut_circle(x0 + 16 + length * 0.76, y0 - height * 0.1, 5)


def icons():
    def fishing(p):  # hook
        p.ring(58, 14, 7, 7)
        p.line([(58, 21), (58, 62)], 10)
        p.arc(41, 62, 17, 0, 180, 10)
        p.line([(24, 62), (24, 46)], 10)
        p.poly([(16, 50), (24, 34), (32, 50)])

    def map_(p):  # folded map
        p.poly([(10, 22), (36, 12), (64, 22), (90, 12), (90, 78), (64, 88), (36, 78), (10, 88)])
        cut = (0, 0, 0, 0)
        p.line([(36, 18), (36, 80)], 5, cut)
        p.line([(64, 22), (64, 84)], 5, cut)

    def aquarium(p):
        fish_shape(p)

    def arena(p):  # trophy
        p.d.pieslice([P(26, 4), P(74, 60)], 0, 180, fill=WHITE)
        p.rrect(26, 12, 74, 34, 2)
        p.arc(24, 30, 12, 90, 270)
        p.arc(76, 30, 12, -90, 90)
        p.rrect(44, 56, 56, 74, 2)
        p.rrect(28, 74, 72, 88, 4)

    def expedition(p):  # compass
        p.ring(50, 50, 38)
        p.poly([(50, 20), (60, 50), (50, 80), (40, 50)])
        p.cut_circle(50, 50, 5)

    def market(p):  # stall with awning
        p.poly([(10, 36), (18, 14), (82, 14), (90, 36)])
        cut = (0, 0, 0, 0)
        for x in (30, 50, 70):
            p.line([(x, 14), (x - (x - 50) * 0.25, 36)], 4, cut)
        p.rrect(16, 42, 84, 88, 4, outline=True)
        p.rrect(40, 58, 60, 88, 3)

    def shop(p):  # shopping bag
        p.rrect(16, 32, 84, 90, 8)
        p.arc(50, 34, 16, 180, 360, 7)
        p.cut_circle(34, 46, 4)
        p.cut_circle(66, 46, 4)

    def profile(p):
        p.circle(50, 32, 18)
        p.d.pieslice([P(16, 58), P(84, 126)], 180, 360, fill=WHITE)

    def bell(p):
        p.d.pieslice([P(22, 14), P(78, 70)], 180, 360, fill=WHITE)
        p.poly([(22, 42), (78, 42), (84, 72), (16, 72)])
        p.rrect(12, 68, 88, 78, 5)
        p.circle(50, 86, 8)
        p.circle(50, 12, 5)

    def gear(p):
        for i in range(8):
            a = math.radians(i * 45)
            cx, cy = 50 + 34 * math.cos(a), 50 + 34 * math.sin(a)
            p.circle(cx, cy, 9)
        p.circle(50, 50, 32)
        p.cut_circle(50, 50, 12)

    def close(p):
        p.line([(24, 24), (76, 76)], 10)
        p.line([(76, 24), (24, 76)], 10)

    def play(p):
        p.poly([(28, 16), (82, 50), (28, 84)])

    def stop(p):
        p.rrect(22, 22, 78, 78, 10)

    def pause(p):
        p.rrect(24, 18, 42, 82, 5)
        p.rrect(58, 18, 76, 82, 5)

    def star(p):
        pts = []
        for i in range(10):
            r = 44 if i % 2 == 0 else 19
            a = math.radians(-90 + i * 36)
            pts.append((50 + r * math.cos(a), 53 + r * math.sin(a)))
        p.poly(pts)

    def rod(p):
        p.line([(14, 88), (84, 12)], 7)
        p.line([(84, 12), (84, 60)], 3)
        p.circle(84, 64, 6)
        p.circle(30, 70, 10)
        p.cut_circle(30, 70, 4)

    def fish(p):
        fish_shape(p)

    def book(p):
        p.poly([(8, 22), (46, 16), (46, 86), (8, 90)])
        p.poly([(54, 16), (92, 22), (92, 90), (54, 86)])

    def pin(p):
        p.circle(50, 38, 28)
        p.poly([(28, 52), (72, 52), (50, 92)])
        p.cut_circle(50, 38, 11)

    def clock(p):
        p.ring(50, 50, 38)
        p.line([(50, 50), (50, 26)])
        p.line([(50, 50), (66, 60)])

    def lock(p):
        p.arc(50, 42, 18, 180, 360, 9)
        p.line([(32, 42), (32, 48)], 9)
        p.line([(68, 42), (68, 48)], 9)
        p.rrect(18, 44, 82, 90, 9)
        p.cut_circle(50, 64, 6)

    def check(p):
        p.line([(18, 52), (40, 74), (84, 28)], 11)

    def bolt(p):
        p.poly([(58, 6), (18, 56), (46, 56), (40, 94), (82, 40), (54, 40)])

    def medal(p):
        p.poly([(28, 6), (44, 6), (56, 36), (40, 40)])
        p.poly([(72, 6), (56, 6), (44, 36), (60, 40)])
        p.circle(50, 64, 28)
        p.cut_circle(50, 64, 17)
        p.circle(50, 64, 11)

    def chevron(p):
        p.line([(36, 18), (68, 50), (36, 82)], 11)

    def arrow(p):
        p.line([(14, 50), (84, 50)], 10)
        p.line([(58, 24), (84, 50), (58, 76)], 10)

    def plus(p):
        p.line([(50, 18), (50, 82)], 11)
        p.line([(18, 50), (82, 50)], 11)

    def box(p):  # tackle box / chest
        p.rrect(10, 36, 90, 88, 8)
        p.rrect(16, 16, 84, 34, 7)
        p.line([(38, 16), (38, 8), (62, 8), (62, 16)], 6)
        p.rrect(42, 48, 58, 64, 3, (0, 0, 0, 0))

    def tag(p):  # sale tag
        p.poly([(10, 50), (46, 14), (88, 14), (88, 56), (52, 92)])
        p.cut_circle(70, 32, 7)

    def swords(p):
        p.line([(18, 18), (78, 78)], 8)
        p.line([(82, 18), (22, 78)], 8)
        p.line([(62, 84), (84, 62)], 8)
        p.line([(16, 62), (38, 84)], 8)

    def refresh(p):
        p.arc(50, 52, 32, -40, 225)
        a = math.radians(225)
        ex, ey = 50 + 32 * math.cos(a), 52 + 32 * math.sin(a)
        dx, dy = -math.sin(a), math.cos(a)
        p.poly([(ex + dx * 16, ey + dy * 16), (ex - dy * 13, ey + dx * 13), (ex + dy * 13, ey - dx * 13)])

    def ranking(p):
        p.rrect(10, 52, 34, 90, 4)
        p.rrect(38, 18, 62, 90, 4)
        p.rrect(66, 38, 90, 90, 4)

    def info(p):
        p.circle(50, 50, 42)
        cut = (0, 0, 0, 0)
        p.circle(50, 28, 7, cut)
        p.rrect(43, 42, 57, 78, 5, cut)

    def warning(p):
        p.d.polygon([P(50, 8), P(94, 88), P(6, 88)], fill=WHITE)
        cut = (0, 0, 0, 0)
        p.rrect(44, 34, 56, 64, 5, cut)
        p.circle(50, 75, 6, cut)

    def hourglass(p):
        p.rrect(18, 6, 82, 16, 4)
        p.rrect(18, 84, 82, 94, 4)
        p.poly([(24, 16), (76, 16), (54, 50), (76, 84), (24, 84), (46, 50)])

    def level(p):  # up arrow in a circle
        p.circle(50, 50, 42)
        cut = (0, 0, 0, 0)
        p.line([(50, 74), (50, 30)], 10, cut)
        p.line([(32, 46), (50, 28), (68, 46)], 10, cut)

    def cart(p):
        p.line([(8, 16), (22, 16), (32, 64), (80, 64), (88, 30), (26, 30)], 8)
        p.circle(36, 82, 8)
        p.circle(74, 82, 8)

    def sort(p):
        p.line([(28, 18), (28, 82)], 8)
        p.line([(14, 66), (28, 82), (42, 66)], 8)
        p.line([(72, 82), (72, 18)], 8)
        p.line([(58, 34), (72, 18), (86, 34)], 8)

    def feed(p):  # food pellets / drop
        p.poly([(50, 6), (78, 52), (22, 52)])
        p.circle(50, 60, 28)

    def home(p):
        p.poly([(50, 10), (92, 48), (80, 48), (80, 90), (20, 90), (20, 48), (8, 48)])
        p.rrect(40, 60, 60, 90, 3, (0, 0, 0, 0))

    def wave(p):
        for y in (32, 54, 76):
            pts = [(8 + i * 2, y + 7 * math.sin(i * 0.33)) for i in range(43)]
            p.line(pts, 7)

    def compact(p):  # minimise window
        p.rrect(10, 18, 90, 82, 8, outline=True, w=7)
        p.rrect(50, 50, 82, 74, 4)

    return {k.rstrip('_'): v for k, v in locals().items() if callable(v)}


def coloured_coin():
    img = Image.new('RGBA', (C, C), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([P(6, 6), P(94, 94)], fill=(196, 128, 22, 255))
    d.ellipse([P(8, 6), P(92, 88)], fill=(246, 185, 59, 255))
    d.ellipse([P(20, 18), P(80, 76)], fill=(226, 156, 32, 255))
    d.ellipse([P(24, 20), P(76, 72)], fill=(255, 212, 102, 255))
    # Soft shine.
    shine = Image.new('RGBA', (C, C), (0, 0, 0, 0))
    ImageDraw.Draw(shine).ellipse([P(28, 22), P(52, 40)], fill=(255, 250, 220, 170))
    img.alpha_composite(shine.filter(ImageFilter.GaussianBlur(C / 60)))
    img.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, 'ico_moeda.png'), optimize=True)


def coloured_shell():
    img = Image.new('RGBA', (C, C), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.pieslice([P(8, 10), P(92, 94)], 180, 360, fill=(240, 196, 176, 255))
    d.polygon([P(8, 52), P(92, 52), P(62, 86), P(38, 86)], fill=(240, 196, 176, 255))
    d.rounded_rectangle([P(36, 80), P(64, 94)], radius=C * 0.04, fill=(214, 150, 128, 255))
    for i in range(-3, 4):
        a = math.radians(-90 + i * 24)
        d.line([P(50, 86), P(50 + 40 * math.cos(a), 52 + 40 * math.sin(a))], fill=(206, 136, 116, 255), width=int(C * 0.035))
    img.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, 'ico_concha.png'), optimize=True)


NAMES = {
    'fishing': 'pesca', 'map': 'mapa', 'aquarium': 'aquario', 'arena': 'arena', 'expedition': 'expedicao',
    'market': 'mercado', 'shop': 'loja', 'profile': 'perfil', 'bell': 'avisos', 'gear': 'opcoes',
    'close': 'fechar', 'play': 'iniciar', 'stop': 'parar', 'pause': 'pausa', 'star': 'estrela', 'rod': 'vara',
    'fish': 'peixe', 'book': 'especies', 'pin': 'local', 'clock': 'relogio', 'lock': 'cadeado',
    'check': 'confirmar', 'bolt': 'energia', 'medal': 'honra', 'chevron': 'seta', 'arrow': 'seta_longa',
    'plus': 'adicionar', 'box': 'caixa', 'tag': 'vender', 'swords': 'atacar', 'refresh': 'trocar',
    'ranking': 'ranking', 'info': 'info', 'warning': 'aviso', 'hourglass': 'ampulheta', 'level': 'nivel',
    'cart': 'comprar', 'sort': 'ordenar', 'feed': 'alimentar', 'home': 'inicio', 'wave': 'ondas',
    'compact': 'compacto',
}


def main():
    finais.proteger_finais()
    os.makedirs(OUT, exist_ok=True)
    for key, draw in icons().items():
        pen = Pen()
        draw(pen)
        pen.save(NAMES[key])
    coloured_coin()
    coloured_shell()
    print('Ícones gerados em', os.path.normpath(OUT))


if __name__ == '__main__':
    main()
