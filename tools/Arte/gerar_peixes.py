"""Paints the placeholder fish art (Art Bible, sections 7, 8 and 41).

One side view per species, facing right, transparent background, no text and no baked glow,
all drawn by the same "hand" (same light, outline, eye and fin style) so the set looks consistent.
They are replaceable masters: to use final art, replace fish_<species>_master.png keeping the name
(the Bible's final size is 2048x1024; these placeholders are 1024x512 to keep the repository small).

Usage:  python3 tools/Arte/gerar_peixes.py [species ...]
Always produces the same files (fixed seeds). Requires numpy and Pillow.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'client-unity', 'Assets', 'Resources', 'Arte', 'Peixes')
W, H = 2048, 1024  # painted at 2x, saved at half size
FINAL = (1024, 512)


def hexc(s):
    s = s.lstrip('#')
    return np.array([int(s[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


class Spec(dict):
    def __getattr__(self, k):
        return self[k]


def spec(**kw):
    base = dict(
        length=0.62,         # body length as a fraction of the canvas width (without tail)
        height=0.34,         # max body height as a fraction of the canvas height
        peak=0.55,           # where the body is tallest (0 tail .. 1 snout)
        belly=0.52,          # share of the height below the centre line
        snout=0.35,          # 0 pointed .. 1 blunt
        tail='forked',       # forked, rounded, truncate, lunate
        tail_size=1.0,
        dorsal=(0.40, 0.70, 0.45),  # start, end (0 tail .. 1 snout), height relative to body height
        dorsal_spiny=False,
        anal=(0.12, 0.30, 0.28),
        adipose=False,
        pectoral=0.28, pelvic=0.22,
        barbels=0,           # pairs of barbels
        mouth=0.10, jaw_under=0.0, teeth=False,
        eye=0.050,
        back='#6A7A86', belly_col='#E6ECEE', fin='#9AA6AE', mark='#3A454C', fin_alpha=0.88,
        pattern=[],          # list of (kind, params)
        scales=0.25, lateral=0.35, seed=1,
        eye_col='#D8C27A',
    )
    base.update(kw)
    return Spec(base)


SPECIES = {
    # ---- Mapa 1 — Lago Sereno
    'lambari': spec(length=0.60, height=0.27, peak=0.52, snout=0.30, back='#7F95A2', belly_col='#EEF3F4', fin='#E6B84A',
                    mark='#2E3A40', pattern=[('spot', (0.78, 0.40, 0.040)), ('spot', (0.06, 0.50, 0.035)), ('stripe', (0.47, 0.035, 0.45, '#AFC3CC'))],
                    dorsal=(0.44, 0.60, 0.5), adipose=True, scales=0.35, seed=11),
    'tilapia': spec(length=0.60, height=0.44, peak=0.55, snout=0.45, tail='truncate', back='#56666B', belly_col='#C9D3D0', fin='#7F6E78',
                    mark='#34403F', dorsal=(0.18, 0.80, 0.30), dorsal_spiny=True, anal=(0.12, 0.34, 0.30),
                    pattern=[('bars', (7, 0.12, 0.85, 0.45))], scales=0.35, seed=12),
    'carpa': spec(length=0.62, height=0.40, peak=0.56, snout=0.55, tail='forked', back='#8E6C2E', belly_col='#EBD69A', fin='#A87434',
                  mark='#5C4420', dorsal=(0.22, 0.66, 0.32), barbels=1, scales=0.9, seed=13, eye_col='#E0B64E'),
    'piau': spec(length=0.62, height=0.30, peak=0.55, snout=0.35, back='#A0937A', belly_col='#F0E9D6', fin='#C9905E',
                 mark='#3E352A', pattern=[('spot', (0.62, 0.48, 0.045)), ('spot', (0.40, 0.48, 0.040)), ('spot', (0.12, 0.50, 0.040))],
                 adipose=True, scales=0.45, seed=14),
    'cascudo': spec(length=0.64, height=0.30, peak=0.62, belly=0.40, snout=0.85, tail='truncate', back='#4B4032', belly_col='#8E7C60',
                    fin='#4A3E30', mark='#2A221A', dorsal=(0.50, 0.78, 1.1), pectoral=0.50, anal=(0.20, 0.32, 0.25),
                    pattern=[('dots', (0.020, 110))], barbels=1, jaw_under=0.6, mouth=0.05, scales=0.0, eye=0.035, seed=15, eye_col='#8C7A50'),
    'curimbata': spec(length=0.62, height=0.34, peak=0.55, snout=0.45, back='#76868A', belly_col='#E3E7DF', fin='#A7A28A',
                      mark='#4E585A', pattern=[('finstripes', ())], scales=0.8, seed=16),
    'traira': spec(length=0.66, height=0.26, peak=0.50, snout=0.25, tail='rounded', back='#4A472F', belly_col='#B5AE88', fin='#584F35',
                   mark='#24221A', dorsal=(0.40, 0.66, 0.40), anal=(0.10, 0.26, 0.30), mouth=0.18,
                   pattern=[('blotches', (0.060, 22))], scales=0.3, teeth=True, seed=17, eye_col='#C8A24A'),
    'pacu': spec(length=0.56, height=0.56, peak=0.52, belly=0.55, snout=0.60, back='#50565B', belly_col='#D9B98E', fin='#C06A3E',
                 mark='#34373B', dorsal=(0.40, 0.62, 0.34), anal=(0.08, 0.36, 0.22), adipose=True, scales=0.2, seed=18),
    'matrinxa': spec(length=0.62, height=0.32, peak=0.55, snout=0.40, back='#667B8A', belly_col='#E4E9EB', fin='#C7534A',
                     mark='#2F3A42', pattern=[('spot', (0.80, 0.42, 0.045)), ('tailbar', ('#2A3036',))], adipose=True, scales=0.5, seed=19),
    'tambaqui': spec(length=0.58, height=0.52, peak=0.52, belly=0.56, snout=0.62, back='#3C4549', belly_col='#D5B574', fin='#262D30',
                     mark='#22292C', dorsal=(0.40, 0.62, 0.34), anal=(0.08, 0.34, 0.24), adipose=True, scales=0.2, seed=20,
                     pattern=[('belly_dark', ())]),
    # ---- Mapa 2 — Rio Selvagem
    'tucunare': spec(length=0.64, height=0.34, peak=0.55, snout=0.30, tail='truncate', back='#6F8032', belly_col='#EDDC86', fin='#C2572E',
                     mark='#26301A', dorsal=(0.20, 0.72, 0.40), dorsal_spiny=True, mouth=0.16,
                     pattern=[('bars', (3, 0.16, 0.80, 0.70)), ('ocellus', ())], scales=0.4, seed=21, eye_col='#D0442C'),
    'piranha': spec(length=0.56, height=0.50, peak=0.56, belly=0.58, snout=0.55, tail='truncate', back='#6C787D', belly_col='#D8612F',
                    fin='#B8452A', mark='#3C4448', dorsal=(0.40, 0.58, 0.40), anal=(0.08, 0.36, 0.26), adipose=True,
                    pattern=[('dots', (0.012, 90)), ('redbelly', ())], jaw_under=-0.5, teeth=True, mouth=0.13, scales=0.2, seed=22, eye_col='#C8352A'),
    'dourado': spec(length=0.66, height=0.32, peak=0.52, snout=0.35, back='#C28A22', belly_col='#F7DC6E', fin='#E5A33A',
                    mark='#7A5412', pattern=[('dotlines', (7,)), ('tailbar', ('#6A4A12',))], adipose=True, scales=0.5, mouth=0.14, seed=23),
    'pintado': spec(length=0.70, height=0.22, peak=0.60, belly=0.45, snout=0.20, back='#616466', belly_col='#ECEAE2', fin='#7A7C7E',
                    mark='#1C1F21', dorsal=(0.55, 0.68, 0.55), adipose=True, anal=(0.12, 0.26, 0.28), barbels=3, mouth=0.10,
                    pattern=[('dots', (0.018, 70, 0.62))], scales=0.0, eye=0.030, seed=24),
    'cachara': spec(length=0.70, height=0.22, peak=0.60, belly=0.45, snout=0.20, back='#66634E', belly_col='#E4E0CE', fin='#6E6A55',
                    mark='#23221A', dorsal=(0.55, 0.68, 0.55), adipose=True, anal=(0.12, 0.26, 0.28), barbels=3, mouth=0.10,
                    pattern=[('bars', (13, 0.05, 0.62, 0.62))], scales=0.0, eye=0.030, seed=25),
    'jau': spec(length=0.64, height=0.36, peak=0.65, belly=0.50, snout=0.70, tail='forked', back='#4F4838', belly_col='#C8BC95', fin='#4A4334',
                mark='#2A251C', dorsal=(0.52, 0.68, 0.55), adipose=True, anal=(0.12, 0.28, 0.24), barbels=3, mouth=0.18,
                pattern=[('blotches', (0.070, 16))], scales=0.0, eye=0.030, seed=26),
    'peixe_cachorra': spec(length=0.68, height=0.24, peak=0.45, snout=0.10, back='#8A99A0', belly_col='#F0F4F4', fin='#A0AAB0',
                           mark='#4E5A60', dorsal=(0.26, 0.38, 0.40), anal=(0.08, 0.40, 0.25), mouth=0.24, jaw_under=-0.3, teeth=True,
                           pattern=[('spot', (0.70, 0.40, 0.040))], scales=0.3, seed=27),
    'piracanjuba': spec(length=0.62, height=0.32, peak=0.55, snout=0.40, back='#728589', belly_col='#E7EDEA', fin='#CB7C4A',
                        mark='#43545A', adipose=True, scales=0.6, pattern=[('tailbar', ('#34424A',))], seed=28),
    'pirarucu': spec(length=0.72, height=0.24, peak=0.55, snout=0.35, tail='rounded', back='#3E4A43', belly_col='#C8B99A', fin='#8E3A30',
                     mark='#2A332E', dorsal=(0.06, 0.22, 0.45), anal=(0.04, 0.20, 0.40), pectoral=0.22, mouth=0.14,
                     pattern=[('redscales', ())], scales=1.0, seed=29),
    'aruana': spec(length=0.74, height=0.22, peak=0.55, belly=0.46, snout=0.20, tail='rounded', tail_size=0.7, back='#B2AE95', belly_col='#F4F0DE',
                   fin='#8FA0A6', mark='#6E6A56', dorsal=(0.02, 0.28, 0.40), anal=(0.0, 0.34, 0.45), barbels=1, jaw_under=-0.7, mouth=0.18,
                   scales=1.0, eye=0.055, seed=30),
}


def paint(sp):
    rng = np.random.default_rng(sp.seed)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    L = sp.length * W
    x_tail = W * 0.5 - L * 0.5 + W * 0.07      # where the body meets the tail
    x_snout = x_tail + L
    cy = H * 0.5
    Hb = sp.height * H

    t = (xx - x_tail) / L  # 0 at tail base, 1 at snout
    tt = np.clip(t, 0, 1)
    # Envelope: thin peduncle, fullest at "peak", rounded or pointed head.
    ped = 0.12 + 0.05 * sp.snout
    q = 0.30 + 0.32 * (1 - sp.snout)

    def envelope(tv):
        tv = np.clip(np.asarray(tv, dtype=np.float64), 0, 1)
        p = sp.peak
        rear = ped + (1 - ped) * np.sin(np.pi / 2 * np.clip(tv / p, 0, 1)) ** 1.5
        u = np.clip((tv - p) / (1 - p), 0, 1)
        front = (1 - u ** 2.4) ** q
        return np.where(tv < p, rear, front)

    env = envelope(tt).astype(np.float32)
    top = cy - env * Hb * (1 - sp.belly)
    bot = cy + env * Hb * sp.belly
    # Back slightly arched, belly a touch fuller behind the head.
    top -= Hb * 0.04 * np.sin(np.pi * tt)
    body = ((yy >= top) & (yy <= bot) & (t >= -0.001) & (t <= 1.0)).astype(np.float32)

    fins = Image.new('L', (W, H), 0)
    fin_rays = Image.new('L', (W, H), 0)
    fd = ImageDraw.Draw(fins)
    rd = ImageDraw.Draw(fin_rays)

    def at(tv):
        tv = min(max(tv, 0.0), 1.0)
        x = x_tail + tv * L
        e = float(envelope(tv))
        tp = cy - e * Hb * (1 - sp.belly) - Hb * 0.04 * math.sin(math.pi * tv)
        bt = cy + e * Hb * sp.belly
        return x, tp, bt

    def rounded(pts, iterations=3):
        # Chaikin corner cutting: soft, organic fin edges from a few control points.
        for _ in range(iterations):
            out = []
            for i in range(len(pts)):
                p0, p1 = pts[i], pts[(i + 1) % len(pts)]
                out.append((p0[0] * 0.75 + p1[0] * 0.25, p0[1] * 0.75 + p1[1] * 0.25))
                out.append((p0[0] * 0.25 + p1[0] * 0.75, p0[1] * 0.25 + p1[1] * 0.75))
            pts = out
        return pts

    def fin_poly(pts, rays=True, iterations=3):
        fd.polygon(rounded(pts, iterations), fill=255)
        if rays and len(pts) >= 3:
            base = pts[0]
            for i in range(1, len(pts) - 1):
                rd.line([base, pts[i]], fill=255, width=4)

    # Tail
    xt, tpt, btt = at(0.0)
    th = Hb * 0.62 * sp.tail_size
    tl = L * 0.26 * sp.tail_size
    if sp.tail == 'forked':
        pts = [(xt + 6, cy - Hb * 0.07), (xt - tl, cy - th), (xt - tl * 0.62, cy - th * 0.35), (xt - tl * 0.55, cy),
               (xt - tl * 0.62, cy + th * 0.35), (xt - tl, cy + th), (xt + 6, cy + Hb * 0.07)]
    elif sp.tail == 'rounded':
        pts = [(xt + 6, cy - Hb * 0.10)] + [(xt - tl * 0.85 * math.cos(u), cy - th * 0.75 * math.sin(u)) for u in np.linspace(1.35, -1.35, 21)] + [(xt + 6, cy + Hb * 0.10)]
    else:  # truncate
        pts = [(xt + 6, cy - Hb * 0.09), (xt - tl * 0.85, cy - th * 0.78), (xt - tl * 0.95, cy - th * 0.3), (xt - tl * 0.92, cy),
               (xt - tl * 0.95, cy + th * 0.3), (xt - tl * 0.85, cy + th * 0.78), (xt + 6, cy + Hb * 0.09)]
    fd.polygon(rounded(pts, 2), fill=255)
    for p in pts[1:-1]:
        rd.line([(xt + 8, cy), p], fill=255, width=4)

    # Dorsal
    d0, d1, dh = sp.dorsal
    xa, ta, _ = at(d0)
    xb, tb, _ = at(d1)
    hh = Hb * dh
    if sp.dorsal_spiny:
        n = 9
        pts = [(xb, tb + 6)]
        for i in range(n + 1):
            u = i / n
            x = xb + (xa - xb) * u
            ytop = at(d1 + (d0 - d1) * u)[1] - hh * (0.75 + 0.25 * math.sin(math.pi * u)) * (0.85 if i % 2 else 1.0)
            pts.append((x, ytop))
        pts.append((xa - L * 0.02, ta + 4))
        fin_poly(pts, iterations=1)
    else:
        pts = [(xb, tb + 6), (xb - (xb - xa) * 0.35, tb - hh), (xa - L * 0.03, ta - hh * 0.35), (xa - L * 0.02, ta + 4)]
        fin_poly(pts)
    if sp.adipose:
        x0, t0, _ = at(0.12)
        fd.ellipse([x0 - L * 0.03, t0 - Hb * 0.10, x0 + L * 0.03, t0 + Hb * 0.04], fill=255)
    # Anal
    a0, a1, ah = sp.anal
    xa, _, ba = at(a0)
    xb, _, bb = at(a1)
    fin_poly([(xb, bb - 6), (xb - (xb - xa) * 0.3, bb + Hb * ah), (xa - L * 0.02, ba + Hb * ah * 0.5), (xa, ba - 4)])
    # Pelvic
    xp, _, bp = at(0.52)
    fin_poly([(xp + L * 0.03, bp - 6), (xp - L * 0.02, bp + Hb * sp.pelvic), (xp - L * 0.06, bp + Hb * sp.pelvic * 0.8), (xp - L * 0.03, bp - 4)])

    fin_mask = np.asarray(fins.filter(ImageFilter.GaussianBlur(1.2)), dtype=np.float32) / 255.0
    ray_mask = np.asarray(fin_rays.filter(ImageFilter.GaussianBlur(1.0)), dtype=np.float32) / 255.0 * fin_mask

    # ---- body colour: countershading + soft light from the top-left
    v = np.clip((yy - top) / np.maximum(bot - top, 1), 0, 1)
    back, bel = hexc(sp.back), hexc(sp.belly_col)
    k = smooth(0.18, 0.80, v)[..., None]
    col = back * (1 - k) + bel * k

    # Patterns in body coordinates (t along, v across).
    mark = hexc(sp.mark)
    for kind, prm in sp.pattern:
        if kind == 'stripe':
            vc, wv, strength, c = prm
            m = np.exp(-((v - vc) / wv) ** 2) * smooth(0.05, 0.2, t) * (1 - smooth(0.82, 0.92, t))
            col = col * (1 - m[..., None] * strength) + hexc(c) * (m[..., None] * strength)
        elif kind == 'spot':
            ts, vs, r = prm
            d = np.sqrt(((t - ts) * L) ** 2 + ((v - vs) * (bot - top)) ** 2) / (r * H)
            m = np.clip(1.4 - d * 1.4, 0, 1) ** 0.8
            col = col * (1 - m[..., None] * 0.85) + mark * (m[..., None] * 0.85)
        elif kind == 'bars':
            n, wd, strength, vmax = prm
            phase = (t * n) % 1.0
            m = (np.exp(-((phase - 0.5) / wd) ** 2) * smooth(0.10, 0.2, t) * (1 - smooth(0.80, 0.9, t)) * (1 - smooth(vmax - 0.1, vmax + 0.05, v)))
            col = col * (1 - m[..., None] * strength) + mark * (m[..., None] * strength)
        elif kind == 'dots':
            r = prm[0]
            n = prm[1]
            vmax = prm[2] if len(prm) > 2 else 0.9
            mm = np.zeros_like(t)
            for _ in range(n):
                ts, vs = rng.uniform(0.02, 0.95), rng.uniform(0.05, vmax)
                rr = r * rng.uniform(0.7, 1.3) * H
                d = np.sqrt(((t - ts) * L) ** 2 + ((v - vs) * (bot - top)) ** 2)
                mm = np.maximum(mm, np.clip(1.2 - d / rr, 0, 1))
            col = col * (1 - mm[..., None] * 0.9) + mark * (mm[..., None] * 0.9)
        elif kind == 'blotches':
            r, n = prm
            mm = np.zeros_like(t)
            for _ in range(n):
                ts, vs = rng.uniform(0.0, 0.92), rng.uniform(0.0, 0.7)
                rr = r * rng.uniform(0.6, 1.4) * H
                d = np.sqrt(((t - ts) * L * 0.8) ** 2 + ((v - vs) * (bot - top) * 1.3) ** 2)
                mm = np.maximum(mm, smooth(1.0, 0.4, d / rr))
            col = col * (1 - mm[..., None] * 0.6) + mark * (mm[..., None] * 0.6)
        elif kind == 'dotlines':
            n = prm[0]
            row = (v * n) % 1.0
            dots = ((t * 60) % 1.0)
            m = np.exp(-((row - 0.5) / 0.16) ** 2) * np.exp(-((dots - 0.5) / 0.28) ** 2) * (v < 0.72) * smooth(0.06, 0.15, t) * (1 - smooth(0.82, 0.9, t))
            col = col * (1 - m[..., None] * 0.7) + mark * (m[..., None] * 0.7)
        elif kind == 'ocellus':
            pass  # drawn on the tail below
        elif kind == 'redbelly':
            m = smooth(0.55, 0.85, v) * smooth(0.25, 0.45, t) * (1 - smooth(0.85, 0.95, t))
            col = col * (1 - m[..., None] * 0.9) + hexc('#D9542A') * (m[..., None] * 0.9)
        elif kind == 'belly_dark':
            m = smooth(0.75, 0.98, v)
            col = col * (1 - m[..., None] * 0.6) + hexc('#2A2F31') * (m[..., None] * 0.6)
        elif kind == 'redscales':
            m = smooth(0.25, 0.05, t) * 0 + (1 - smooth(0.25, 0.6, t)) * smooth(0.1, 0.5, v)
            sc = ((t * 34) % 1.0 < 0.22) | (((v * 10) + (np.floor(t * 34) % 2) * 0.5) % 1.0 < 0.18)
            col = col * (1 - (m * sc)[..., None] * 0.4) + hexc('#C2412E') * ((m * sc)[..., None] * 0.4)
            col = col * (1 - (m * 0.55)[..., None]) + hexc('#A63E2E') * (m * 0.55)[..., None]
        elif kind in ('finstripes', 'tailbar'):
            pass

    # Scale texture: faint scallops.
    if sp.scales > 0:
        su = (t * 40 * (1.2 - 0.4 * sp.length))
        row = np.floor(v * 14)
        ph = (su + row * 0.5) % 1.0
        vv = (v * 14) % 1.0
        arc = np.abs(np.sqrt((ph - 0.5) ** 2 + (vv * 0.9) ** 2) - 0.55) < 0.07
        sm = arc * smooth(0.06, 0.16, t) * (1 - smooth(0.74, 0.8, t)) * sp.scales * 0.16
        col = col * (1 - sm[..., None]) + (col * 0.55) * sm[..., None]

    # Lateral line.
    lat = np.exp(-((v - (0.42 - 0.06 * np.sin(np.pi * tt))) / 0.012) ** 2) * smooth(0.04, 0.12, t) * (1 - smooth(0.74, 0.78, t)) * sp.lateral
    col = col * (1 - lat[..., None] * 0.5) + (col * 0.5 + 0.25) * (lat[..., None] * 0.5)

    # Shading from the body silhouette: light from the upper left, soft rim.
    blur = np.asarray(Image.fromarray((body * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(Hb * 0.10)), dtype=np.float32) / 255.0
    gy, gx = np.gradient(blur)
    light = -(gy * 0.9 + gx * 0.35) * Hb * 0.12
    depth = np.clip(blur * 2 - 0.6, 0, 1)
    shade = 0.74 + 0.34 * np.clip(light, -1, 1) + 0.16 * depth
    shade = shade * (1 - 0.18 * smooth(0.65, 1.0, v))
    col = col * shade[..., None]
    # Top highlight band (satin sheen).
    sheen = np.exp(-((v - 0.24) / 0.10) ** 2) * smooth(0.08, 0.25, t) * (1 - smooth(0.8, 0.9, t)) * 0.18
    col = col * (1 - sheen[..., None]) + np.array([1, 1, 1]) * sheen[..., None]

    # ---- fins colour
    fcol = hexc(sp.fin)
    fin_rgb = np.ones((H, W, 3)) * fcol
    fin_rgb = fin_rgb * (1 - ray_mask[..., None] * 0.4)
    near_body = np.asarray(Image.fromarray((body * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(Hb * 0.12)), dtype=np.float32) / 255.0
    fin_rgb = fin_rgb * (1 - 0.3 * near_body[..., None])
    # Tail bar / ocellus / fin stripes.
    for kind, prm in sp.pattern:
        if kind == 'tailbar':
            m = smooth(x_tail - tl * 0.3, x_tail - tl * 0.55, xx) * (np.abs(yy - cy) < th * 0.55)
            fin_rgb = fin_rgb * (1 - m[..., None] * 0.8) + hexc(prm[0]) * (m[..., None] * 0.8)
        elif kind == 'ocellus':
            ox, oy = x_tail - tl * 0.22, cy - th * 0.05
            d = np.sqrt((xx - ox) ** 2 + (yy - oy) ** 2) / (Hb * 0.13)
            ring = (d < 1.0).astype(np.float32)
            inner = (d < 0.62).astype(np.float32)
            fin_rgb = fin_rgb * (1 - ring[..., None]) + hexc('#E8B23A') * ring[..., None]
            fin_rgb = fin_rgb * (1 - inner[..., None]) + hexc('#1C2012') * inner[..., None]
        elif kind == 'finstripes':
            m = (((xx + yy * 0.6) / (Hb * 0.09)) % 1.0 < 0.3) * 0.5
            fin_rgb = fin_rgb * (1 - m[..., None]) + mark * m[..., None]
    fin_alpha = fin_mask * sp.fin_alpha * (1 - body)
    # Fins fade a little towards their edges.
    fin_edge = np.asarray(fins.filter(ImageFilter.GaussianBlur(10)), dtype=np.float32) / 255.0
    fin_alpha *= 0.65 + 0.35 * fin_edge

    # ---- compose
    rgb = fin_rgb * (fin_alpha[..., None]) + col * body[..., None]
    alpha = np.clip(fin_alpha + body, 0, 1)
    rgb = np.where(alpha[..., None] > 0, rgb / np.maximum(alpha[..., None], 1e-4), 0)

    img = Image.fromarray(np.dstack([np.clip(rgb, 0, 1) * 255, alpha * 255]).astype(np.uint8), 'RGBA')
    d = ImageDraw.Draw(img)

    # Outline: darker rim around the body.
    edge = Image.fromarray((body * 255).astype(np.uint8)).filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.0))
    outline_col = tuple(int(c * 255 * 0.5) for c in back) + (0,)
    ol = Image.new('RGBA', (W, H), outline_col)
    ol.putalpha(edge.point(lambda p: int(p * 0.55)))
    img.alpha_composite(ol)

    # Gill cover.
    xg, tg, bg = at(0.78)
    gh = bg - tg
    d.arc([xg - gh * 0.55, tg + gh * 0.12, xg + gh * 0.25, bg - gh * 0.10], 290, 70, fill=tuple(int(c * 255 * 0.55) for c in back) + (150,), width=6)
    # Pectoral fin.
    xpf, tpf, bpf = at(0.72)
    pc = (xpf - L * 0.01, tpf + (bpf - tpf) * 0.62)
    pf = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    pd = ImageDraw.Draw(pf)
    plen = L * sp.pectoral * 0.55
    pd.polygon(rounded([pc, (pc[0] - plen, pc[1] + plen * 0.22), (pc[0] - plen * 0.75, pc[1] + plen * 0.42), (pc[0] + 6, pc[1] + 16)], 3),
               fill=tuple(int(c * 255) for c in fcol * 0.9) + (int(255 * 0.72),))
    img.alpha_composite(pf.filter(ImageFilter.GaussianBlur(1.2)))

    # Eye.
    xe, te, be = at(0.905 - 0.03 * sp.snout)
    ey = te + (be - te) * (0.36 + 0.05 * sp.jaw_under)
    er = sp.eye * H
    d.ellipse([xe - er * 1.25, ey - er * 1.25, xe + er * 1.25, ey + er * 1.25], fill=tuple(int(c * 255 * 0.45) for c in back) + (160,))
    d.ellipse([xe - er, ey - er, xe + er, ey + er], fill=tuple(int(c * 255) for c in hexc(sp.eye_col)) + (255,))
    d.ellipse([xe - er * 0.62, ey - er * 0.62, xe + er * 0.62, ey + er * 0.62], fill=(16, 18, 22, 255))
    d.ellipse([xe - er * 0.05, ey - er * 0.55, xe + er * 0.32, ey - er * 0.18], fill=(255, 255, 255, 230))

    # Mouth.
    xm, tm, bm = at(0.985)
    my = tm + (bm - tm) * (0.55 + 0.2 * sp.jaw_under)
    ml = sp.mouth * L * 0.5
    d.line([(xm, my), (xm - ml, my + ml * 0.18)], fill=(30, 26, 24, 200), width=6)
    if sp.teeth:
        for i in range(4):
            x = xm - ml * (0.15 + 0.2 * i)
            d.polygon([(x, my), (x - 7, my), (x - 3.5, my + 11)], fill=(245, 242, 232, 255))

    # Barbels.
    for i in range(sp.barbels):
        blen = L * (0.10 + 0.07 * i)
        sx, sy = xm - ml * 0.3, my + 4 + i * 6
        pts = [(sx - blen * u, sy + blen * 0.5 * u * u + math.sin(u * 3 + i) * 10) for u in np.linspace(0, 1, 12)]
        d.line(pts, fill=tuple(int(c * 255 * 0.55) for c in back) + (230,), width=7 - i, joint='curve')

    img = img.resize(FINAL, Image.LANCZOS)
    # Trim to the drawn area with a small margin, but keep the 2:1 frame (the pipeline expects it).
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    names = sys.argv[1:] or list(SPECIES)
    for name in names:
        paint(SPECIES[name]).save(os.path.join(OUT, 'fish_' + name + '_master.png'), optimize=True)
        print('  ', name)
    print('Peixes gerados em', os.path.normpath(OUT))


if __name__ == '__main__':
    main()
