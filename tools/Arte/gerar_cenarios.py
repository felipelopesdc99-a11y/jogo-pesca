"""Paints the layered scenery of each map (Art Bible, sections 9, 10, 31–33).

Each map is a set of transparent layers, back to front, that the game stacks with parallax:
sky, far mountains, mid hills, shore trees, water, and the two foreground corners. Clouds are
separate sprites so they can drift. Also paints the boat, the fisherman and the map thumbnails.

The layers are painted in world units (the camera shows 10.8 units of height, horizon at y = 0.2)
so what is painted here lines up with the scene in Unity. Replace any file keeping its name to use
final art. Usage:  python3 tools/Arte/gerar_cenarios.py [PastaDoMapa ...]   (always the same files; numpy + Pillow;
with map folder names, only those maps)
"""
import math
import os

import finais
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), '..', '..', 'client-unity', 'Assets', 'Resources', 'Arte')
HORIZON = 0.2
HALF_W = 13.0  # layers cover x in [-13, 13] (21:9 plus camera sway)


def hexc(s):
    s = s.lstrip('#')
    return np.array([int(s[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    t = np.asarray(t)[..., None] if np.ndim(t) else t
    return a * (1 - t) + b * t


def noise1(rng, n, octaves=5, persistence=0.5):
    """fBm value noise sampled at n points in [0, 1)."""
    out = np.zeros(n)
    amp, freq, total = 1.0, 4, 0.0
    for _ in range(octaves):
        pts = rng.uniform(-1, 1, freq + 3)
        x = np.linspace(0, freq, n, endpoint=False)
        i = np.floor(x).astype(int)
        f = x - i
        f = f * f * (3 - 2 * f)
        out += amp * (pts[i] * (1 - f) + pts[i + 1] * f)
        total += amp
        amp *= persistence
        freq *= 2
    return out / total


def noise2(rng, h, w, scale, octaves=4, persistence=0.5):
    out = np.zeros((h, w))
    amp, total = 1.0, 0.0
    for o in range(octaves):
        gh, gw = int(h / scale * 2 ** o) + 3, int(w / scale * 2 ** o) + 3
        grid = rng.uniform(-1, 1, (gh, gw)).astype(np.float32)
        img = Image.fromarray(((grid + 1) * 127.5).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += amp * (np.asarray(img, dtype=np.float32) / 127.5 - 1)
        total += amp
        amp *= persistence
    return out / total


class Layer:
    """An RGBA canvas mapped to a world rectangle."""

    def __init__(self, x0, x1, y0, y1, ppu):
        self.x0, self.x1, self.y0, self.y1, self.ppu = x0, x1, y0, y1, ppu
        self.w = int(round((x1 - x0) * ppu))
        self.h = int(round((y1 - y0) * ppu))
        self.rgb = np.zeros((self.h, self.w, 3), dtype=np.float32)
        self.a = np.zeros((self.h, self.w), dtype=np.float32)
        ys, xs = np.mgrid[0:self.h, 0:self.w].astype(np.float32)
        self.X = x0 + (xs + 0.5) / ppu
        self.Y = y1 - (ys + 0.5) / ppu

    def col_x(self):
        return self.x0 + (np.arange(self.w) + 0.5) / self.ppu

    def over(self, rgb, alpha):
        alpha = np.clip(alpha, 0, 1)
        out_a = alpha + self.a * (1 - alpha)
        safe = np.maximum(out_a, 1e-5)[..., None]
        self.rgb = (rgb * alpha[..., None] + self.rgb * (self.a * (1 - alpha))[..., None]) / safe
        self.a = out_a

    def ridge(self, heights, rgb, soft=1.0):
        """Fills everything below a height profile (one height per pixel column)."""
        edge = (heights[None, :] - self.Y) * self.ppu / soft
        self.over(rgb if rgb.ndim == 3 else np.broadcast_to(rgb, self.rgb.shape), np.clip(edge + 0.5, 0, 1))

    def image(self):
        return Image.fromarray(np.dstack([np.clip(self.rgb, 0, 1) * 255, np.clip(self.a, 0, 1) * 255]).astype(np.uint8), 'RGBA')

    def save(self, path):
        self.image().save(path, optimize=True)


# ----------------------------------------------------------------------------- elements

def conifer_profile(xs, rng, count, x_from, x_to, base, h_min, h_max, width=0.55):
    """Height of a row of pointed conifers at each x."""
    out = np.full_like(xs, -99.0)
    for _ in range(count):
        cx = rng.uniform(x_from, x_to)
        hh = rng.uniform(h_min, h_max)
        ww = hh * width * rng.uniform(0.8, 1.2)
        # Layered, slightly ragged triangle.
        d = np.abs(xs - cx) / (ww / 2)
        tiers = 0.07 * hh * np.abs(np.sin(d * 7.5 + cx)) * d
        tip = base + hh * (1 - d) - tiers + 0.015 * hh * np.sin((xs - cx) * 90 + cx)
        tip = np.where(d < 1, tip, -99)
        out = np.maximum(out, tip)
    return out


def broadleaf_profile(xs, rng, count, x_from, x_to, base, h_min, h_max):
    out = np.full_like(xs, -99.0)
    for _ in range(count):
        cx = rng.uniform(x_from, x_to)
        r = rng.uniform(h_min, h_max)
        top = base + r * 0.9 + np.sqrt(np.clip(r * r - (xs - cx) ** 2, 0, None)) * 0.9
        top = np.where(np.abs(xs - cx) < r, top, -99)
        out = np.maximum(out, top)
    return out


def rocks(img, layer, items, dark, light, seed):
    """Half-submerged boulders on the waterline, lit from the sun side."""
    rng = np.random.default_rng(seed)
    d = ImageDraw.Draw(img)
    for cx, w, hgt in items:
        px = (cx - layer.x0) * layer.ppu
        py = (layer.y1 - HORIZON) * layer.ppu + 4
        n = 14
        pts = []
        for i in range(n + 1):
            u = i / n
            ang = math.pi * u
            r = 1 + rng.uniform(-0.12, 0.12)
            pts.append((px - math.cos(ang) * w * 50 * r, py - math.sin(ang) * hgt * 100 * r))
        d.polygon(pts, fill=tuple(int(c * 255) for c in dark) + (255,))
        hi = [(x * 0.9 + px * 0.1 + w * 6, y * 0.92 + py * 0.08) for x, y in pts[2:n - 3]]
        d.line(hi, fill=tuple(int(c * 255) for c in light) + (255,), width=max(3, int(hgt * 22)))


def light_rim(layer, profile, sun_x, strength, colour, depth=0.08):
    """Warm light on the sun-facing top edge of a silhouette."""
    xs = layer.col_x()
    slope = np.gradient(profile) * layer.ppu
    facing = np.clip((xs - sun_x) * -0.02 + slope * 0.6 * np.sign(sun_x - xs), 0, 1)
    near_top = np.clip(1 - (profile[None, :] - layer.Y) / depth, 0, 1) * (layer.Y <= profile[None, :] + 0.001)
    k = near_top * (0.35 + facing[None, :]) * strength * layer.a
    layer.rgb = mix(layer.rgb, colour, np.clip(k, 0, 1))


def cloud_sprite(rng, w, h, lit, shade, sun_dir=1.0):
    """A soft sunset-lit cumulus, drawn from overlapping puffs with noise."""
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    dens = np.zeros((h, w), dtype=np.float32)
    for _ in range(14):
        cx = rng.uniform(0.18, 0.82) * w
        cy = rng.uniform(0.45, 0.62) * h
        r = rng.uniform(0.12, 0.24) * w
        d = np.sqrt((xx - cx) ** 2 + ((yy - cy) * 1.25) ** 2) / r
        dens = np.maximum(dens, np.clip(1 - d, 0, 1))
    n = noise2(rng, h, w, w / 6, 4)
    dens = np.clip(dens * 1.6 + n * 0.35 - 0.25, 0, 1)
    dens *= smooth(0.78 * h, 0.66 * h, yy)  # flat base
    # Lighting: brighter towards the sun (upper right), shadowed underneath.
    g = np.clip((h - yy) / h * 0.9 + (xx / w - 0.5) * 0.5 * sun_dir, 0, 1)
    g = g + dens * 0.35
    rgb = mix(np.broadcast_to(shade, (h, w, 3)), np.broadcast_to(lit, (h, w, 3)), smooth(0.35, 0.95, g))
    alpha = smooth(0.02, 0.18, dens) * 0.97
    img = np.dstack([np.clip(rgb, 0, 1) * 255, alpha * 255]).astype(np.uint8)
    return Image.fromarray(img, 'RGBA').filter(ImageFilter.GaussianBlur(0.8))


# ----------------------------------------------------------------------------- Lago Sereno

def lago_sereno(out):
    rng = np.random.default_rng(7)
    sun = (5.3, 1.55)
    P = dict(sky_top='#27467E', sky_mid='#6D7FB0', sky_low='#F2A66A', sky_horizon='#FFD796', sun='#FFF4C8', glow='#FFC063',
             far='#7B7FA6', far_haze='#E0A98E', mid='#4E6376', mid_forest='#3B5256', rim='#F2B470',
             tree='#1E3A2F', tree_shade='#122419', water_top='#F0B37D', water_mid='#3B6B96', water_deep='#132F4E')

    # Sky (x -13..13, y horizon-0.1 .. 5.8)
    sky = Layer(-HALF_W, HALF_W, HORIZON - 0.2, 5.8, 48)
    t = np.clip((sky.Y - HORIZON) / 5.4, 0, 1)
    col = mix(hexc(P['sky_horizon']), hexc(P['sky_low']), smooth(0.0, 0.22, t))
    col = mix(col, hexc(P['sky_mid']), smooth(0.18, 0.55, t))
    col = mix(col, hexc(P['sky_top']), smooth(0.5, 1.0, t))
    d = np.sqrt((sky.X - sun[0]) ** 2 + ((sky.Y - sun[1]) * 1.4) ** 2)
    col = mix(col, hexc(P['glow']), np.exp(-d / 2.2) * 0.75)
    col = mix(col, hexc(P['sun']), np.exp(-d / 0.55) * 0.9)
    sky.over(col, np.ones_like(t))
    # High thin clouds (baked, far).
    n = noise2(rng, sky.h, sky.w, 60, 5)
    streak = smooth(0.15, 0.55, n) * smooth(1.6, 2.6, sky.Y) * (1 - smooth(4.2, 5.4, sky.Y))
    thin = mix(hexc('#F6C39A'), hexc('#B89AC0'), smooth(2.0, 4.5, sky.Y))
    sky.rgb = mix(sky.rgb, thin, streak * 0.22)
    # Sun disc.
    disc = np.clip((0.42 - np.sqrt((sky.X - sun[0]) ** 2 + (sky.Y - sun[1]) ** 2)) * sky.ppu, 0, 1)
    sky.rgb = mix(sky.rgb, hexc('#FFF8DC'), disc)
    sky.save(os.path.join(out, 'map_lago_sereno_bg_sky.png'))

    # Far mountains.
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4, 70)
    xs = far.col_x()
    h1 = HORIZON + 0.9 + 0.55 * noise1(rng, far.w, 6) + 0.35 * np.sin(xs * 0.35 + 1.2)
    h1 = np.maximum(h1, HORIZON + 0.25)
    colf = mix(hexc(P['far']), hexc(P['far_haze']), smooth(HORIZON + 1.4, HORIZON, far.Y) * 0.8)
    far.ridge(h1, colf)
    light_rim(far, h1, sun[0], 0.35, hexc('#F3B98F'), 0.12)
    h2 = HORIZON + 0.45 + 0.35 * noise1(rng, far.w, 6) + 0.2 * np.sin(xs * 0.6)
    colf2 = mix(hexc('#6A6F97'), hexc('#D39C88'), smooth(HORIZON + 0.9, HORIZON, far.Y) * 0.7)
    far.ridge(h2, colf2)
    light_rim(far, h2, sun[0], 0.3, hexc('#F0AE84'), 0.08)
    far.save(os.path.join(out, 'map_lago_sereno_bg_far.png'))

    # Mid hills with a forest texture.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5, 90)
    xs = mid.col_x()
    base = HORIZON + 0.25 + 0.2 * noise1(rng, mid.w, 5) + 0.55 * np.exp(-((xs + 3.5) / 3.0) ** 2) + 0.45 * np.exp(-((xs - 9.5) / 2.6) ** 2)
    # Lower in front of the sun so the far mountains and the light read.
    base = base - 0.25 * np.exp(-((xs - sun[0] + 0.5) / 2.2) ** 2)
    forest = np.maximum(base, conifer_profile(xs, rng, 320, -HALF_W, HALF_W, base - 0.12, 0.16, 0.36, 0.5))
    colm = mix(hexc('#35505A'), hexc('#3F6258'), smooth(HORIZON, HORIZON + 1.0, mid.Y))
    colm = mix(colm, hexc('#8F8C9E'), smooth(HORIZON + 0.4, HORIZON, mid.Y) * 0.3)
    colm = colm * (1 + noise2(rng, mid.h, mid.w, 12, 3)[..., None] * 0.12)
    mid.ridge(forest, colm)
    light_rim(mid, forest, sun[0], 0.55, hexc(P['rim']), 0.06)
    mid.save(os.path.join(out, 'map_lago_sereno_bg_mid.png'))

    # Shore trees: dense on both sides, open in the middle (the lake and the sun breathe).
    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2, 100)
    xs = near.col_x()
    left = conifer_profile(xs, rng, 70, -HALF_W, -4.6, HORIZON - 0.05, 0.8, 2.6, 0.42)
    left = np.maximum(left, broadleaf_profile(xs, rng, 30, -HALF_W, -3.4, HORIZON - 0.1, 0.25, 0.6))
    right = conifer_profile(xs, rng, 40, 7.4, HALF_W, HORIZON - 0.05, 0.7, 2.2, 0.42)
    right = np.maximum(right, broadleaf_profile(xs, rng, 20, 6.8, HALF_W, HORIZON - 0.1, 0.2, 0.5))
    islet = broadleaf_profile(xs, rng, 5, 1.2, 2.4, HORIZON - 0.08, 0.12, 0.22)
    prof = np.maximum(np.maximum(left, right), islet)
    coln = mix(hexc(P['tree_shade']), hexc(P['tree']), smooth(HORIZON, HORIZON + 2.2, near.Y))
    tex = noise2(rng, near.h, near.w, 18, 4)
    coln = coln * (1 + tex[..., None] * 0.18)
    near.ridge(prof, coln, 1.2)
    light_rim(near, prof, sun[0], 0.6, hexc('#E6A560'), 0.07)
    # Shore rocks at the waterline.
    img = near.image()
    rocks(img, near, [(-8.9, 1.4, 0.34), (-7.6, 0.9, 0.2), (-5.2, 1.1, 0.24), (8.3, 1.2, 0.28), (10.5, 1.5, 0.34)], hexc('#3B3942'), hexc('#B98F74'), 3)
    img.save(os.path.join(out, 'map_lago_sereno_bg_near.png'), optimize=True)

    # Water with reflections of the sky, mountains, trees and a golden sun column.
    water = Layer(-HALF_W, HALF_W, -5.8, HORIZON, 64)
    depth = np.clip((HORIZON - water.Y) / 6.0, 0, 1)
    colw = mix(hexc(P['water_top']), hexc('#7B7FA6'), smooth(0.0, 0.06, depth))
    colw = mix(colw, hexc(P['water_mid']), smooth(0.04, 0.35, depth))
    colw = mix(colw, hexc(P['water_deep']), smooth(0.3, 1.0, depth))
    # Mirrored tree line (dark bands near the horizon on both sides).
    xs = water.col_x()
    refl = np.clip((np.interp(water.col_x(), near.col_x(), prof) - HORIZON) * 0.55, 0, None)
    band = (HORIZON - water.Y) < refl[None, :]
    wav = noise2(rng, water.h, water.w, 10, 3)
    # Reflections of the shore are drawn by the game from the layers themselves (they match any art).
    # Sun column: broken horizontal streaks, wider near the viewer.
    spread = 0.35 + depth * 1.6
    col_k = np.exp(-((water.X - sun[0]) / spread) ** 2)
    ripple = np.clip(noise2(rng, water.h, water.w, 7, 3) * 2.4 + 0.2, 0, 1)
    rows = (np.sin(water.Y * 40 + noise2(rng, water.h, water.w, 30, 2) * 6) * 0.5 + 0.5) ** 3
    sunk = col_k * ripple * rows * (1 - smooth(0.0, 0.9, depth) * 0.6)
    colw = mix(colw, hexc('#FFE0A0'), np.clip(sunk * 1.2, 0, 1))
    colw = mix(colw, hexc('#F7C27A'), col_k * 0.25 * (1 - depth))
    # Gentle wave texture all over.
    colw = colw * (1 + wav[..., None] * 0.06)
    water.over(colw, np.ones_like(depth))
    water.save(os.path.join(out, 'map_lago_sereno_water.png'))

    # Foreground corners: reeds, grass and lily pads.
    for side, name in ((-1, 'left'), (1, 'right')):
        fg_rng = np.random.default_rng(20 if side < 0 else 21)
        fg = foreground_corner(fg_rng, side, '#2F5A2A', '#6F8F3A', '#E0A655', lily=True)
        fg.save(os.path.join(out, 'map_lago_sereno_fg_' + name + '.png'), optimize=True)

    # Drifting clouds.
    for i in range(3):
        c = cloud_sprite(np.random.default_rng(40 + i), 520, 260, hexc('#FFD9A8'), hexc('#9C82A6'))
        c.save(os.path.join(out, 'map_lago_sereno_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, 'map_lago_sereno')


def foreground_corner(rng, side, dark, light, tip, lily, cattails=True):
    """A corner of reeds, cattails and grass, 5 x 4 world units, drawn from the bottom."""
    W, H, ppu = 5.0, 4.2, 100
    img = Image.new('RGBA', (int(W * ppu), int(H * ppu)), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    dark_c, light_c, tip_c = hexc(dark), hexc(light), hexc(tip)
    # A couple of boulders in the water, with a soft reflection and a foam line.
    for k in range(2):
        rx = (W * (0.72 if side > 0 else 0.28) + (k - 0.5) * 1.2 * side) * ppu
        ry = (0.9 + k * 0.5) * ppu
        rw = (0.9 - k * 0.25) * ppu
        rh = (0.62 - k * 0.14) * ppu
        pts = [(rx + math.cos(a) * rw * (1 + rng.uniform(-0.1, 0.1)), ry - abs(math.sin(a)) * rh * (1 + rng.uniform(-0.15, 0.15))) for a in np.linspace(math.pi, 0, 16)]
        d.polygon([(x, ry + rh * 0.35) for x, _ in pts[::-1]] + pts, fill=(22, 36, 52, 120))
        d.polygon(pts, fill=(52, 54, 62, 255))
        d.line(pts[3:-4], fill=(170, 140, 118, 255) if lily else (150, 162, 160, 255), width=8)
        d.line([(rx - rw * 0.85, ry + 4), (rx + rw * 0.85, ry + 4)], fill=(230, 238, 240, 80), width=3)
    if lily:
        for _ in range(7):
            cx = rng.uniform(0.6, 4.6) * ppu
            cy = (H - rng.uniform(0.3, 1.3)) * ppu
            r = rng.uniform(0.25, 0.5) * ppu
            col = tuple(int(c * 255) for c in mix(dark_c, light_c, rng.uniform(0.2, 0.6))) + (255,)
            d.pieslice([cx - r, cy - r * 0.35, cx + r, cy + r * 0.35], 20, 340, fill=col)
            d.arc([cx - r, cy - r * 0.35, cx + r, cy + r * 0.35], 200, 330, fill=tuple(int(c * 255) for c in light_c) + (160,), width=3)
    for _ in range(90):
        # Blades lean away from the corner they grow in.
        bx = rng.uniform(0.0, W * (0.9 if side > 0 else 0.9)) * ppu
        dist = (bx / ppu) if side > 0 else (W - bx / ppu)
        tall = rng.uniform(1.2, 3.8) * max(0.35, 1 - (W - dist) / W * 0.2) if dist > W * 0.35 else rng.uniform(0.5, 1.6)
        lean = rng.uniform(0.1, 0.7) * (-side)
        pts = []
        for k in np.linspace(0, 1, 14):
            x = bx + lean * k * k * ppu * 0.8
            y = H * ppu - k * tall * ppu
            pts.append((x, y))
        width = int(rng.uniform(5, 11))
        shade = rng.uniform(0, 1)
        col = tuple(int(c * 255) for c in mix(dark_c, light_c, shade)) + (255,)
        for j in range(len(pts) - 1):
            wj = max(1, int(width * (1 - j / len(pts))))
            d.line([pts[j], pts[j + 1]], fill=col, width=wj)
        if cattails and rng.uniform() < 0.25 and tall > 2.0:
            top = pts[int(len(pts) * 0.72)]
            d.rounded_rectangle([top[0] - 7, top[1] - 34, top[0] + 7, top[1] + 10], radius=7, fill=(92, 58, 32, 255))
        if rng.uniform() < 0.35:
            # Warm light on a few blade tips (the sun is low).
            d.line([pts[-3], pts[-1]], fill=tuple(int(c * 255) for c in tip_c) + (200,), width=max(1, width // 3))
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    # Only keep the corner: fade the inner side out.
    a = np.asarray(img, dtype=np.float32)
    xs = np.linspace(0, 1, img.width)[None, :]
    fade = smooth(1.0, 0.55, xs) if side > 0 else smooth(0.0, 0.45, xs)
    fade = 1 - fade if side < 0 else 1 - fade
    fade = smooth(0.0, 0.25, xs) if side > 0 else smooth(1.0, 0.75, xs)
    a[..., 3] *= fade
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def thumbnail(out, prefix, window=(-6.0, 9.0, -2.4, 3.6), size=(720, 288), target=None):
    """A card picture composed from a map's layers (Mapa and Expedição screens)."""
    base = Image.open(os.path.join(out, prefix + '_bg_sky.png')).convert('RGBA')
    wx0, wx1, wy0, wy1 = window

    def place(img, lx0, lx1, ly0, ly1):
        # Crop the part of a layer inside the thumbnail window and scale it.
        W, H = img.size
        sx = W / (lx1 - lx0)
        sy = H / (ly1 - ly0)
        box = ((wx0 - lx0) * sx, (ly1 - wy1) * sy, (wx1 - lx0) * sx, (ly1 - wy0) * sy)
        return img.crop(tuple(int(round(v)) for v in box)).resize(size, Image.LANCZOS)

    canvas = place(base, -HALF_W, HALF_W, HORIZON - 0.2, 5.8)
    canvas.alpha_composite(place(Image.open(os.path.join(out, prefix + '_water.png')).convert('RGBA'), -HALF_W, HALF_W, -5.8, HORIZON))
    canvas.alpha_composite(place(Image.open(os.path.join(out, prefix + '_bg_far.png')).convert('RGBA'), -HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4))
    canvas.alpha_composite(place(Image.open(os.path.join(out, prefix + '_bg_mid.png')).convert('RGBA'), -HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5))
    canvas.alpha_composite(place(Image.open(os.path.join(out, prefix + '_bg_near.png')).convert('RGBA'), -HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2))
    canvas.save(target or os.path.join(out, prefix + '_thumb.png'), optimize=True)
    return canvas


# ----------------------------------------------------------------------------- Rio Selvagem

def rio_selvagem(out):
    rng = np.random.default_rng(11)
    sun = (-4.5, 3.9)
    sky = Layer(-HALF_W, HALF_W, HORIZON - 0.2, 5.8, 48)
    t = np.clip((sky.Y - HORIZON) / 5.4, 0, 1)
    col = mix(hexc('#E9EFE6'), hexc('#A9CBE0'), smooth(0.0, 0.3, t))
    col = mix(col, hexc('#4F86BE'), smooth(0.3, 1.0, t))
    d = np.sqrt((sky.X - sun[0]) ** 2 + ((sky.Y - sun[1]) * 1.3) ** 2)
    col = mix(col, hexc('#FFF6D8'), np.exp(-d / 1.6) * 0.8)
    sky.over(col, np.ones_like(t))
    disc = np.clip((0.38 - np.sqrt((sky.X - sun[0]) ** 2 + (sky.Y - sun[1]) ** 2)) * sky.ppu, 0, 1)
    sky.rgb = mix(sky.rgb, hexc('#FFFBEA'), disc)
    sky.save(os.path.join(out, 'map_rio_selvagem_bg_sky.png'))

    # Far cliffs (tall, steep, misty).
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 3.6, 70)
    xs = far.col_x()
    cliffs = HORIZON + 1.6 + 1.2 * np.clip(noise1(rng, far.w, 4) * 1.8, -1, 1) + 0.25 * noise1(rng, far.w, 7)
    cliffs = np.maximum(cliffs, HORIZON + 0.6)
    colf = mix(hexc('#5D7F7C'), hexc('#CBD9D4'), smooth(HORIZON + 2.2, HORIZON, far.Y) * 0.85)
    # Vertical rock faces with a green, sunlit top.
    face = np.sin(far.X * 9 + noise2(rng, far.h, far.w, 30, 2) * 3) * 0.5 + 0.5
    colf = colf * (0.92 + 0.12 * face[..., None])
    top_green = np.clip(1 - (cliffs[None, :] - far.Y) / 0.18, 0, 1)
    colf = mix(colf, hexc('#6E9A6A'), top_green * 0.7)
    far.ridge(cliffs, colf)
    # Waterfall down the cliff face, with mist at its foot.
    wx = 3.4
    top_y = np.interp(wx, xs, cliffs) - 0.1
    wf = np.exp(-((far.X - wx) / 0.16) ** 4) * (far.Y < top_y) * (far.Y > HORIZON - 0.05)
    streaks = 0.8 + 0.2 * np.sin(far.X * 120 + noise2(rng, far.h, far.w, 25, 2) * 5)
    far.rgb = mix(far.rgb, hexc('#F1F7F8'), np.clip(wf * streaks, 0, 1))
    mist = np.exp(-((far.X - wx) / 1.3) ** 2) * np.exp(-((far.Y - HORIZON - 0.1) / 0.35) ** 2)
    far.rgb = mix(far.rgb, hexc('#F4F8F8'), mist * 0.75)
    far.a = np.maximum(far.a, mist * 0.75)
    far.save(os.path.join(out, 'map_rio_selvagem_bg_far.png'))

    # Mid jungle hills with a waterfall.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.2, 90)
    xs = mid.col_x()
    base = HORIZON + 0.4 + 0.35 * noise1(rng, mid.w, 5)
    base = base - 0.3 * np.exp(-((xs - 3.4) / 1.4) ** 2)
    canopy = np.maximum(base, broadleaf_profile(xs, rng, 220, -HALF_W, HALF_W, base - 0.1, 0.12, 0.32))
    colm = mix(hexc('#2E5A45'), hexc('#4E7A58'), smooth(HORIZON, HORIZON + 1.2, mid.Y))
    colm = mix(colm, hexc('#A9BDB5'), smooth(HORIZON + 0.5, HORIZON, mid.Y) * 0.4)
    tex = noise2(rng, mid.h, mid.w, 14, 4)
    mid.ridge(canopy, colm * (1 + tex[..., None] * 0.15))
    light_rim(mid, canopy, sun[0], 0.35, hexc('#CFE3A8'), 0.05)
    # Keep the mid canopy low in front of the waterfall.
    mid.save(os.path.join(out, 'map_rio_selvagem_bg_mid.png'))

    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.4, 100)
    xs = near.col_x()
    left = broadleaf_profile(xs, rng, 60, -HALF_W, -3.8, HORIZON - 0.1, 0.4, 1.1)
    left = np.maximum(left, conifer_profile(xs, rng, 12, -HALF_W, -6, HORIZON, 1.5, 2.8, 0.3))
    right = broadleaf_profile(xs, rng, 40, 6.2, HALF_W, HORIZON - 0.1, 0.4, 1.0)
    prof = np.maximum(left, right)
    coln = mix(hexc('#10281C'), hexc('#24503A'), smooth(HORIZON, HORIZON + 2.4, near.Y))
    tex = noise2(rng, near.h, near.w, 16, 4)
    near.ridge(prof, coln * (1 + tex[..., None] * 0.2), 1.2)
    light_rim(near, prof, sun[0], 0.45, hexc('#B9D58E'), 0.06)
    img = near.image()
    rocks(img, near, [(-9.0, 1.7, 0.45), (-6.6, 1.1, 0.3), (4.8, 0.8, 0.2), (7.4, 1.4, 0.4), (10.6, 1.7, 0.5), (-1.8, 0.7, 0.14)], hexc('#353C3E'), hexc('#A7B2A8'), 5)
    img.save(os.path.join(out, 'map_rio_selvagem_bg_near.png'), optimize=True)

    water = Layer(-HALF_W, HALF_W, -5.8, HORIZON, 64)
    depth = np.clip((HORIZON - water.Y) / 6.0, 0, 1)
    colw = mix(hexc('#B7D2D0'), hexc('#3E8286'), smooth(0.0, 0.2, depth))
    colw = mix(colw, hexc('#0F3B42'), smooth(0.2, 1.0, depth))
    refl = np.clip((np.interp(water.col_x(), near.col_x(), prof) - HORIZON) * 0.5, 0, None)
    band = (HORIZON - water.Y) < refl[None, :]
    wav = noise2(rng, water.h, water.w, 9, 3)
    # Current: stretched horizontal foam lines.
    stretched = np.asarray(Image.fromarray(((noise2(rng, water.h, max(8, water.w // 8), 6, 3) + 1) * 127.5).astype(np.uint8)).resize((water.w, water.h), Image.BICUBIC), dtype=np.float32) / 255.0
    foam = smooth(0.62, 0.85, stretched + np.sin(water.Y * 22) * 0.05)
    colw = mix(colw, hexc('#D8ECEA'), foam * 0.22 * (0.4 + depth))
    colw = colw * (1 + wav[..., None] * 0.07)
    water.over(colw, np.ones_like(depth))
    water.save(os.path.join(out, 'map_rio_selvagem_water.png'))

    for side, name in ((-1, 'left'), (1, 'right')):
        fg = foreground_corner(np.random.default_rng(30 if side < 0 else 31), side, '#1F4424', '#4F7A36', '#B7D08A', lily=False, cattails=False)
        fg.save(os.path.join(out, 'map_rio_selvagem_fg_' + name + '.png'), optimize=True)

    for i in range(3):
        c = cloud_sprite(np.random.default_rng(60 + i), 520, 260, hexc('#FFFFFF'), hexc('#B4C4D2'), -1.0)
        c.save(os.path.join(out, 'map_rio_selvagem_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, 'map_rio_selvagem')


# ----------------------------------------------------------------------------- Pantanal Dourado

def palms(img, layer, items, trunk, crown, light):
    """Carandaá palms: thin straight trunks with a fan crown, drawn on a shore layer."""
    d = ImageDraw.Draw(img)
    for cx, hgt in items:
        px = (cx - layer.x0) * layer.ppu
        base = (layer.y1 - HORIZON) * layer.ppu
        top = base - hgt * layer.ppu
        d.line([(px, base), (px + hgt * 3, top)], fill=trunk + (255,), width=max(3, int(layer.ppu * 0.05)))
        tx = px + hgt * 3
        r = layer.ppu * (0.28 + hgt * 0.06)
        for i in range(15):
            a = math.radians(200 + i * (140 / 14))
            ex, ey = tx + math.cos(a) * r, top + math.sin(a) * r * 0.9
            d.line([(tx, top), (ex, ey)], fill=(light if i < 5 else crown) + (255,), width=max(2, int(layer.ppu * 0.03)))
        for i in range(3):
            a = math.radians(40 + i * 25)
            d.line([(tx, top), (tx + math.cos(a) * r * 0.6, top + math.sin(a) * r * 0.9)], fill=(150, 120, 70, 255), width=max(2, int(layer.ppu * 0.02)))


def pantanal_dourado(out):
    rng = np.random.default_rng(13)
    sun = (-5.2, 1.4)
    sky = Layer(-HALF_W, HALF_W, HORIZON - 0.2, 5.8, 48)
    t = np.clip((sky.Y - HORIZON) / 5.4, 0, 1)
    col = mix(hexc('#FFE3A6'), hexc('#F6C58A'), smooth(0.0, 0.16, t))
    col = mix(col, hexc('#A9C3D8'), smooth(0.14, 0.55, t))
    col = mix(col, hexc('#6F95C4'), smooth(0.5, 1.0, t))
    d = np.sqrt((sky.X - sun[0]) ** 2 + ((sky.Y - sun[1]) * 1.4) ** 2)
    col = mix(col, hexc('#FFD27A'), np.exp(-d / 2.6) * 0.7)
    col = mix(col, hexc('#FFF4CC'), np.exp(-d / 0.6) * 0.9)
    sky.over(col, np.ones_like(t))
    n = noise2(rng, sky.h, sky.w, 70, 5)
    streak = smooth(0.2, 0.6, n) * smooth(2.0, 3.0, sky.Y) * (1 - smooth(4.4, 5.6, sky.Y))
    sky.rgb = mix(sky.rgb, hexc('#F7D6C0'), streak * 0.2)
    disc = np.clip((0.40 - np.sqrt((sky.X - sun[0]) ** 2 + (sky.Y - sun[1]) ** 2)) * sky.ppu, 0, 1)
    sky.rgb = mix(sky.rgb, hexc('#FFF9E2'), disc)
    sky.save(os.path.join(out, 'map_pantanal_dourado_bg_sky.png'))

    # Far: a flat, misty line of forest and a low serra on the right third only.
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4, 70)
    xs = far.col_x()
    serra = HORIZON + 0.25 + 0.55 * np.exp(-((xs - 8.0) / 2.6) ** 2) + 0.08 * noise1(rng, far.w, 6)
    far.ridge(serra, mix(hexc('#8FA6B8'), hexc('#E8D2B0'), smooth(HORIZON + 0.8, HORIZON, far.Y) * 0.8))
    line = HORIZON + 0.14 + 0.05 * noise1(rng, far.w, 7)
    far.ridge(line, mix(hexc('#7F9A8E'), hexc('#E6D4A8'), smooth(HORIZON + 0.3, HORIZON, far.Y) * 0.6))
    far.save(os.path.join(out, 'map_pantanal_dourado_bg_far.png'))

    # Mid: capões (round islands of forest) spread over open grass.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5, 90)
    xs = mid.col_x()
    grass = HORIZON + 0.06 + 0.02 * noise1(rng, mid.w, 6)
    capoes = broadleaf_profile(xs, rng, 26, -HALF_W, HALF_W, HORIZON, 0.12, 0.34)
    prof = np.maximum(grass, capoes)
    colm = mix(hexc('#56703E'), hexc('#7C8E48'), smooth(HORIZON, HORIZON + 0.8, mid.Y))
    colm = mix(colm, hexc('#C9B98A'), smooth(HORIZON + 0.3, HORIZON, mid.Y) * 0.35)
    mid.ridge(prof, colm * (1 + noise2(rng, mid.h, mid.w, 12, 3)[..., None] * 0.1))
    light_rim(mid, prof, sun[0], 0.6, hexc('#F2C874'), 0.05)
    mid.save(os.path.join(out, 'map_pantanal_dourado_bg_mid.png'))

    # Shore: low flooded grass fields with carandaá palms and a few broad trees on both sides.
    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2, 100)
    xs = near.col_x()
    left = broadleaf_profile(xs, rng, 14, -HALF_W, -5.4, HORIZON - 0.1, 0.25, 0.6)
    right = broadleaf_profile(xs, rng, 10, 7.0, HALF_W, HORIZON - 0.1, 0.25, 0.55)
    field = HORIZON + 0.12 + 0.05 * np.abs(noise1(rng, near.w, 8))
    field = np.where((xs < -3.8) | (xs > 6.2), field + 0.1, field * 0 - 99)
    prof = np.maximum(np.maximum(left, right), field)
    coln = mix(hexc('#3E5A2C'), hexc('#6E8240'), smooth(HORIZON, HORIZON + 1.6, near.Y))
    near.ridge(prof, coln * (1 + noise2(rng, near.h, near.w, 16, 4)[..., None] * 0.16), 1.2)
    light_rim(near, prof, sun[0], 0.7, hexc('#F0C470'), 0.06)
    img = near.image()
    palms(img, near, [(-11.5, 2.2), (-9.8, 1.7), (-7.2, 2.5), (-5.0, 1.4), (7.8, 1.9), (9.6, 2.6), (11.8, 1.6)], (92, 72, 50), (70, 96, 52), (196, 180, 96))
    img.save(os.path.join(out, 'map_pantanal_dourado_bg_near.png'), optimize=True)

    # Water: wide, shallow and mirror-calm, with a golden column from the low sun.
    water = Layer(-HALF_W, HALF_W, -5.8, HORIZON, 64)
    depth = np.clip((HORIZON - water.Y) / 6.0, 0, 1)
    colw = mix(hexc('#F6D59A'), hexc('#9FB3B8'), smooth(0.0, 0.08, depth))
    colw = mix(colw, hexc('#4E7C84'), smooth(0.06, 0.45, depth))
    colw = mix(colw, hexc('#23464E'), smooth(0.4, 1.0, depth))
    spread = 0.4 + depth * 1.8
    col_k = np.exp(-((water.X - sun[0]) / spread) ** 2)
    ripple = np.clip(noise2(rng, water.h, water.w, 7, 3) * 2.4 + 0.2, 0, 1)
    rows = (np.sin(water.Y * 36 + noise2(rng, water.h, water.w, 30, 2) * 6) * 0.5 + 0.5) ** 3
    colw = mix(colw, hexc('#FFE6A8'), np.clip(col_k * ripple * rows * 1.1 * (1 - depth * 0.5), 0, 1))
    colw = colw * (1 + noise2(rng, water.h, water.w, 10, 3)[..., None] * 0.05)
    water.over(colw, np.ones_like(depth))
    water.save(os.path.join(out, 'map_pantanal_dourado_water.png'))

    for side, name in ((-1, 'left'), (1, 'right')):
        fg = foreground_corner(np.random.default_rng(70 if side < 0 else 71), side, '#3A5A2A', '#8A9A48', '#E8C060', lily=True)
        fg.save(os.path.join(out, 'map_pantanal_dourado_fg_' + name + '.png'), optimize=True)

    for i in range(3):
        c = cloud_sprite(np.random.default_rng(80 + i), 520, 260, hexc('#FFE2B8'), hexc('#A8A8C8'))
        c.save(os.path.join(out, 'map_pantanal_dourado_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, 'map_pantanal_dourado')


# ----------------------------------------------------------------------------- Estuário das Marés

def mangrove_roots(img, layer, x_from, x_to, rng, colour):
    """Arched prop roots of the red mangrove along the waterline."""
    d = ImageDraw.Draw(img)
    base = (layer.y1 - HORIZON) * layer.ppu
    x = x_from
    while x < x_to:
        px = (x - layer.x0) * layer.ppu
        hgt = layer.ppu * rng.uniform(0.25, 0.5)
        wid = layer.ppu * rng.uniform(0.15, 0.35)
        d.arc([px - wid, base - hgt, px + wid, base + hgt], 180, 360, fill=colour + (255,), width=max(2, int(layer.ppu * 0.025)))
        x += rng.uniform(0.12, 0.3)


def estuario_das_mares(out):
    rng = np.random.default_rng(17)
    sun = (1.8, 1.25)
    sky = Layer(-HALF_W, HALF_W, HORIZON - 0.2, 5.8, 48)
    t = np.clip((sky.Y - HORIZON) / 5.4, 0, 1)
    col = mix(hexc('#FFB36A'), hexc('#F08A5C'), smooth(0.0, 0.2, t))
    col = mix(col, hexc('#9A6E9C'), smooth(0.18, 0.6, t))
    col = mix(col, hexc('#3C3F7E'), smooth(0.55, 1.0, t))
    d = np.sqrt((sky.X - sun[0]) ** 2 + ((sky.Y - sun[1]) * 1.5) ** 2)
    col = mix(col, hexc('#FFC06A'), np.exp(-d / 2.4) * 0.8)
    col = mix(col, hexc('#FFF0C0'), np.exp(-d / 0.7) * 0.9)
    sky.over(col, np.ones_like(t))
    n = noise2(rng, sky.h, sky.w, 90, 5)
    streak = smooth(0.1, 0.5, n) * smooth(1.4, 2.2, sky.Y) * (1 - smooth(3.6, 4.8, sky.Y))
    sky.rgb = mix(sky.rgb, hexc('#FF9A7A'), streak * 0.35)
    disc = np.clip((0.55 - np.sqrt((sky.X - sun[0]) ** 2 + (sky.Y - sun[1]) ** 2)) * sky.ppu, 0, 1)
    sky.rgb = mix(sky.rgb, hexc('#FFF4D6'), disc)
    sky.save(os.path.join(out, 'map_estuario_das_mares_bg_sky.png'))

    # Far: low coastal hills with a thin bright line of open sea between them.
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4, 70)
    xs = far.col_x()
    sea = np.full_like(xs, HORIZON + 0.05)
    far.ridge(sea, np.broadcast_to(hexc('#F4C890'), far.rgb.shape))
    gap = 1 - np.exp(-((xs - sun[0]) / 3.0) ** 2)
    hills = HORIZON - 0.05 + (0.35 + 0.5 * np.exp(-((xs + 7.5) / 3.0) ** 2) + 0.35 * np.exp(-((xs - 9.0) / 2.2) ** 2) + 0.06 * noise1(rng, far.w, 6)) * gap
    far.ridge(hills, mix(hexc('#6A5A86'), hexc('#D69A86'), smooth(HORIZON + 0.8, HORIZON, far.Y) * 0.7))
    far.save(os.path.join(out, 'map_estuario_das_mares_bg_far.png'))

    # Mid: a band of dense, rounded mangrove canopy.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5, 90)
    xs = mid.col_x()
    open_ = 1 - np.exp(-((xs - sun[0]) / 1.8) ** 2)
    band = HORIZON - 0.05 + (broadleaf_profile(xs, rng, 160, -HALF_W, HALF_W, HORIZON + 0.02, 0.08, 0.2) - HORIZON + 0.05) * open_
    colm = mix(hexc('#1E3A30'), hexc('#34503A'), smooth(HORIZON, HORIZON + 0.6, mid.Y))
    mid.ridge(band, colm * (1 + noise2(rng, mid.h, mid.w, 12, 3)[..., None] * 0.12))
    light_rim(mid, band, sun[0], 0.7, hexc('#F2A868'), 0.05)
    mid.save(os.path.join(out, 'map_estuario_das_mares_bg_mid.png'))

    # Shore: tall, dense red mangrove on both sides with prop roots over the mud.
    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2, 100)
    xs = near.col_x()
    left = broadleaf_profile(xs, rng, 50, -HALF_W, -4.4, HORIZON + 0.35, 0.4, 1.0)
    right = broadleaf_profile(xs, rng, 36, 6.4, HALF_W, HORIZON + 0.35, 0.4, 0.9)
    prof = np.maximum(left, right)
    coln = mix(hexc('#0E2418'), hexc('#244A30'), smooth(HORIZON + 0.3, HORIZON + 2.4, near.Y))
    near.ridge(prof, coln * (1 + noise2(rng, near.h, near.w, 16, 4)[..., None] * 0.18), 1.2)
    light_rim(near, prof, sun[0], 0.6, hexc('#E89A5A'), 0.06)
    img = near.image()
    mangrove_roots(img, near, -HALF_W, -4.6, rng, (58, 42, 30))
    mangrove_roots(img, near, 6.6, HALF_W, rng, (58, 42, 30))
    mud = ImageDraw.Draw(img)
    for x0, x1 in ((-HALF_W, -4.2), (6.2, HALF_W)):
        y = (near.y1 - HORIZON) * near.ppu
        mud.rectangle([(x0 - near.x0) * near.ppu, y - near.ppu * 0.06, (x1 - near.x0) * near.ppu, y + 2], fill=(46, 36, 30, 255))
    img.save(os.path.join(out, 'map_estuario_das_mares_bg_near.png'), optimize=True)

    # Water: brackish, heavier and darker, with strong sunset reflections and a slow tidal flow.
    water = Layer(-HALF_W, HALF_W, -5.8, HORIZON, 64)
    depth = np.clip((HORIZON - water.Y) / 6.0, 0, 1)
    colw = mix(hexc('#F2A870'), hexc('#5E6A78'), smooth(0.0, 0.06, depth))
    colw = mix(colw, hexc('#1E4A4E'), smooth(0.05, 0.4, depth))
    colw = mix(colw, hexc('#0C2428'), smooth(0.35, 1.0, depth))
    spread = 0.5 + depth * 1.9
    col_k = np.exp(-((water.X - sun[0]) / spread) ** 2)
    ripple = np.clip(noise2(rng, water.h, water.w, 7, 3) * 2.4 + 0.2, 0, 1)
    rows = (np.sin(water.Y * 30 + noise2(rng, water.h, water.w, 30, 2) * 6) * 0.5 + 0.5) ** 3
    colw = mix(colw, hexc('#FFC27A'), np.clip(col_k * ripple * rows * 1.3 * (1 - depth * 0.4), 0, 1))
    stretched = np.asarray(Image.fromarray(((noise2(rng, water.h, max(8, water.w // 8), 6, 3) + 1) * 127.5).astype(np.uint8)).resize((water.w, water.h), Image.BICUBIC), dtype=np.float32) / 255.0
    colw = mix(colw, hexc('#6E9A98'), smooth(0.66, 0.86, stretched) * 0.15 * (0.4 + depth))
    colw = colw * (1 + noise2(rng, water.h, water.w, 9, 3)[..., None] * 0.06)
    water.over(colw, np.ones_like(depth))
    water.save(os.path.join(out, 'map_estuario_das_mares_water.png'))

    for side, name in ((-1, 'left'), (1, 'right')):
        fg = foreground_corner(np.random.default_rng(90 if side < 0 else 91), side, '#1A3A26', '#4E6A3A', '#D8905A', lily=False, cattails=False)
        fg.save(os.path.join(out, 'map_estuario_das_mares_fg_' + name + '.png'), optimize=True)

    for i in range(3):
        c = cloud_sprite(np.random.default_rng(95 + i), 560, 220, hexc('#FFB890'), hexc('#8A6E9A'), 1.0)
        c.save(os.path.join(out, 'map_estuario_das_mares_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, 'map_estuario_das_mares')


# ----------------------------------------------------------------------------- Mapas 5 e 6 (mar)

def day_sea_sky(out, prefix, rng, sun, top, mid, low, glow):
    """A clear tropical day sky with a high sun (Costa de Coral, Arquipélago do Sol)."""
    sky = Layer(-HALF_W, HALF_W, HORIZON - 0.2, 5.8, 48)
    t = np.clip((sky.Y - HORIZON) / 5.4, 0, 1)
    col = mix(hexc(low), hexc(mid), smooth(0.0, 0.3, t))
    col = mix(col, hexc(top), smooth(0.3, 1.0, t))
    d = np.sqrt((sky.X - sun[0]) ** 2 + ((sky.Y - sun[1]) * 1.2) ** 2)
    col = mix(col, hexc(glow), np.exp(-d / 2.0) * 0.6)
    col = mix(col, hexc('#FFFDF0'), np.exp(-d / 0.5) * 0.95)
    sky.over(col, np.ones_like(t))
    n = noise2(rng, sky.h, sky.w, 80, 5)
    streak = smooth(0.25, 0.65, n) * smooth(1.2, 2.2, sky.Y) * (1 - smooth(4.4, 5.6, sky.Y))
    sky.rgb = mix(sky.rgb, hexc('#F4FAFF'), streak * 0.35)
    disc = np.clip((0.38 - np.sqrt((sky.X - sun[0]) ** 2 + (sky.Y - sun[1]) ** 2)) * sky.ppu, 0, 1)
    sky.rgb = mix(sky.rgb, hexc('#FFFFF4'), disc)
    sky.save(os.path.join(out, prefix + '_bg_sky.png'))


def day_sea_water(out, prefix, rng, sun, horizon_col, shallow, deep, darkest, glint, column):
    water = Layer(-HALF_W, HALF_W, -5.8, HORIZON, 64)
    depth = np.clip((HORIZON - water.Y) / 6.0, 0, 1)
    colw = mix(hexc(horizon_col), hexc(shallow), smooth(0.0, 0.08, depth))
    colw = mix(colw, hexc(deep), smooth(0.06, 0.5, depth))
    colw = mix(colw, hexc(darkest), smooth(0.45, 1.0, depth))
    ripple = np.clip(noise2(rng, water.h, water.w, 7, 3) * 2.4 + 0.2, 0, 1)
    rows = (np.sin(water.Y * 34 + noise2(rng, water.h, water.w, 30, 2) * 6) * 0.5 + 0.5) ** 3
    if column:
        spread = 0.5 + depth * 1.8
        col_k = np.exp(-((water.X - sun[0]) / spread) ** 2)
        colw = mix(colw, hexc(glint), np.clip(col_k * ripple * rows * 1.1 * (1 - depth * 0.5), 0, 1))
    # Small bright glints all over (midday sea).
    sparkle = smooth(0.82, 0.95, noise2(rng, water.h, water.w, 4, 2) * 0.5 + 0.5) * rows
    colw = mix(colw, hexc(glint), np.clip(sparkle * 0.35 * (1 - depth * 0.6), 0, 1))
    colw = colw * (1 + noise2(rng, water.h, water.w, 9, 3)[..., None] * 0.06)
    water.over(colw, np.ones_like(depth))
    water.save(os.path.join(out, prefix + '_water.png'))


def costa_de_coral(out):
    rng = np.random.default_rng(19)
    prefix = 'map_costa_de_coral'
    sun = (-5.0, 3.8)
    day_sea_sky(out, prefix, rng, sun, '#2E7FD0', '#6FB6EA', '#D8F0FA', '#FFF2C0')

    # Far: low cliffs of the coast, hazy blue.
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4, 70)
    xs = far.col_x()
    cliffs = HORIZON + 0.12 + 0.25 * np.exp(-((xs + 8.5) / 3.0) ** 2) + 0.3 * np.exp(-((xs - 9.0) / 2.6) ** 2) + 0.04 * noise1(rng, far.w, 6)
    far.ridge(cliffs, mix(hexc('#7FA2B8'), hexc('#CFE4EE'), smooth(HORIZON + 0.5, HORIZON, far.Y) * 0.7))
    far.save(os.path.join(out, prefix + '_bg_far.png'))

    # Mid: a pale beach line with coconut palms.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5, 90)
    xs = mid.col_x()
    sand = HORIZON + 0.04 + 0.02 * noise1(rng, mid.w, 6)
    bush = broadleaf_profile(xs, rng, 30, -HALF_W, HALF_W, HORIZON + 0.02, 0.05, 0.12)
    prof = np.maximum(sand, bush)
    colm = mix(hexc('#3F7A3E'), hexc('#6E9C4A'), smooth(HORIZON, HORIZON + 0.4, mid.Y))
    colm = np.where((mid.Y < HORIZON + 0.06)[..., None], hexc('#EBDDB4'), colm)
    mid.ridge(prof, colm)
    img = mid.image()
    palms(img, mid, [(-10.5, 0.6), (-7.8, 0.75), (-4.0, 0.5), (3.2, 0.55), (7.5, 0.7), (10.8, 0.6)], (110, 88, 60), (60, 110, 60), (150, 190, 90))
    img.save(os.path.join(out, prefix + '_bg_mid.png'), optimize=True)

    # Shore (placeholder for the painted shore groups): rocks with green on top.
    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2, 100)
    xs = near.col_x()
    left = broadleaf_profile(xs, rng, 14, -HALF_W, -5.6, HORIZON - 0.1, 0.25, 0.7)
    right = broadleaf_profile(xs, rng, 10, 7.0, HALF_W, HORIZON - 0.1, 0.25, 0.6)
    prof = np.maximum(left, right)
    near.ridge(prof, mix(hexc('#7A5A3E'), hexc('#5E8A42'), smooth(HORIZON + 0.3, HORIZON + 1.4, near.Y)), 1.2)
    light_rim(near, prof, sun[0], 0.6, hexc('#FFE2A0'), 0.06)
    near.save(os.path.join(out, prefix + '_bg_near.png'))

    # Water: shallow turquoise over the reef, deeper blue towards the viewer.
    day_sea_water(out, prefix, rng, sun, '#BFEFF0', '#3FC8C8', '#1B9AB0', '#0E5C80', '#FFFFFF', False)

    for side, name in ((-1, 'left'), (1, 'right')):
        fg = foreground_corner(np.random.default_rng(100 if side < 0 else 101), side, '#2F6A3A', '#8AB050', '#F0D080', lily=False, cattails=False)
        fg.save(os.path.join(out, prefix + '_fg_' + name + '.png'), optimize=True)

    for i in range(3):
        c = cloud_sprite(np.random.default_rng(105 + i), 560, 260, hexc('#FFFFFF'), hexc('#B8CCE0'), -1.0)
        c.save(os.path.join(out, prefix + '_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, prefix)


def arquipelago_do_sol(out):
    rng = np.random.default_rng(23)
    prefix = 'map_arquipelago_do_sol'
    sun = (5.2, 3.6)
    day_sea_sky(out, prefix, rng, sun, '#1F5FB8', '#4E95DA', '#F4E2B0', '#FFE6A0')

    # Far: rocky islands spaced along the horizon.
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4, 70)
    xs = far.col_x()
    islands = np.full_like(xs, -99.0)
    for cx, w, hgt in ((-10.0, 1.6, 0.8), (-6.0, 1.0, 0.5), (-1.5, 1.4, 0.65), (2.0, 0.7, 0.3), (8.5, 1.8, 0.9), (11.5, 0.9, 0.4)):
        islands = np.maximum(islands, HORIZON + hgt * np.clip(1 - ((xs - cx) / w) ** 2, 0, None) ** 0.7 - 0.02)
    far.ridge(islands + 0.03 * noise1(rng, far.w, 7), mix(hexc('#5E7FA0'), hexc('#C8D8E4'), smooth(HORIZON + 0.6, HORIZON, far.Y) * 0.6))
    far.save(os.path.join(out, prefix + '_bg_far.png'))

    # Mid: nearer green islets.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5, 90)
    xs = mid.col_x()
    islets = np.full_like(xs, -99.0)
    for cx, w, hgt in ((-8.0, 1.2, 0.55), (-3.5, 0.8, 0.35), (4.0, 1.0, 0.45), (10.0, 1.4, 0.6)):
        islets = np.maximum(islets, HORIZON + hgt * np.clip(1 - ((xs - cx) / w) ** 2, 0, None) ** 0.6 - 0.02)
    islets = islets + 0.03 * noise1(rng, mid.w, 7)
    colm = mix(hexc('#6E6A62'), hexc('#3F7A3E'), smooth(HORIZON + 0.1, HORIZON + 0.4, mid.Y))
    mid.ridge(islets, colm * (1 + noise2(rng, mid.h, mid.w, 12, 3)[..., None] * 0.1))
    light_rim(mid, islets, sun[0], 0.6, hexc('#FFD890'), 0.05)
    mid.save(os.path.join(out, prefix + '_bg_mid.png'))

    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2, 100)
    xs = near.col_x()
    left = HORIZON - 0.1 + 2.6 * np.clip((-5.5 - xs) / 7.0, 0, 1) ** 0.6
    right = HORIZON - 0.1 + 2.4 * np.clip((xs - 7.0) / 6.0, 0, 1) ** 0.6
    prof = np.maximum(left, right) + 0.08 * noise1(rng, near.w, 7)
    near.ridge(prof, mix(hexc('#6A6460'), hexc('#4E7A44'), smooth(HORIZON + 1.2, HORIZON + 2.4, near.Y)), 1.2)
    light_rim(near, prof, sun[0], 0.6, hexc('#FFD890'), 0.06)
    near.save(os.path.join(out, prefix + '_bg_near.png'))

    # Water: deep cobalt open sea with a golden sun column.
    day_sea_water(out, prefix, rng, sun, '#D8D8C0', '#2F6FB8', '#14408A', '#0A2456', '#FFE6A8', True)

    for side, name in ((-1, 'left'), (1, 'right')):
        fg = foreground_corner(np.random.default_rng(110 if side < 0 else 111), side, '#3A4A44', '#6E7A60', '#E0C890', lily=False, cattails=False)
        fg.save(os.path.join(out, prefix + '_fg_' + name + '.png'), optimize=True)

    for i in range(3):
        c = cloud_sprite(np.random.default_rng(115 + i), 560, 260, hexc('#FFFBEA'), hexc('#A8BCD8'), 1.0)
        c.save(os.path.join(out, prefix + '_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, prefix)


# ----------------------------------------------------------------------------- Mapas 7 a 10 (mar aberto)

def open_sea(out, prefix, seed, sun, sky, water, night=False, column=True, islets=None):
    """Placeholder for the open-sea maps (Corrente Azul, Banco das Baleias, Talude Noturno, Abismo
    Atlântico): sky (day or night with stars and moon), a low swell line, big swells at the edges and
    dark swell corners. `sky` = (top, mid, horizon, glow); `water` = (horizon, mid, deep, glint)."""
    rng = np.random.default_rng(seed)
    top, midc, low, glow = sky
    s = Layer(-HALF_W, HALF_W, HORIZON - 0.2, 5.8, 48)
    t = np.clip((s.Y - HORIZON) / 5.4, 0, 1)
    col = mix(hexc(low), hexc(midc), smooth(0.0, 0.3, t))
    col = mix(col, hexc(top), smooth(0.3, 1.0, t))
    d = np.sqrt((s.X - sun[0]) ** 2 + ((s.Y - sun[1]) * 1.3) ** 2)
    col = mix(col, hexc(glow), np.exp(-d / (1.4 if night else 2.4)) * (0.45 if night else 0.75))
    s.over(col, np.ones_like(t))
    if night:
        # Stars: more and brighter high up, a faint milky band across.
        n = rng.random((s.h, s.w))
        band = np.exp(-((s.Y - 3.8 - (s.X * 0.12)) / 0.9) ** 2)
        stars = (n > 0.9975 - band * 0.002) * smooth(0.6, 2.0, s.Y)
        s.rgb = mix(s.rgb, hexc('#EAF0FF'), stars * rng.uniform(0.5, 1.0, (s.h, s.w)))
        s.rgb = mix(s.rgb, hexc('#8A9AC8'), band * 0.10 * (0.6 + 0.4 * noise2(rng, s.h, s.w, 20, 3)))
        disc = np.clip((0.30 - np.sqrt((s.X - sun[0]) ** 2 + (s.Y - sun[1]) ** 2)) * s.ppu, 0, 1)
        s.rgb = mix(s.rgb, hexc('#F2F5FF'), disc)
    else:
        n = noise2(rng, s.h, s.w, 80, 5)
        streak = smooth(0.25, 0.65, n) * smooth(1.0, 2.0, s.Y) * (1 - smooth(4.4, 5.6, s.Y))
        s.rgb = mix(s.rgb, hexc('#FFE2B8'), streak * 0.35)
        disc = np.clip((0.42 - np.sqrt((s.X - sun[0]) ** 2 + (s.Y - sun[1]) ** 2)) * s.ppu, 0, 1)
        s.rgb = mix(s.rgb, hexc('#FFF6DC'), disc)
    s.save(os.path.join(out, prefix + '_bg_sky.png'))

    w_h, w_mid, w_deep, glint = water
    # Far: open sea has only a hazy line (or very low islets far away).
    far = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 2.4, 70)
    xs = far.col_x()
    prof = np.full_like(xs, -99.0)
    for cx, wid, hgt in (islets or []):
        prof = np.maximum(prof, HORIZON + hgt * np.clip(1 - ((xs - cx) / wid) ** 2, 0, None) ** 0.7 - 0.01)
    far.ridge(prof, np.broadcast_to(mix(hexc(w_mid), hexc(w_h), 0.55), far.rgb.shape))
    far.save(os.path.join(out, prefix + '_bg_far.png'))

    # Mid: a low line of swells just under the horizon.
    mid = Layer(-HALF_W, HALF_W, HORIZON - 0.1, HORIZON + 1.5, 90)
    xs = mid.col_x()
    swell = HORIZON + 0.03 + 0.03 * np.abs(np.sin(xs * 2.1 + noise1(rng, mid.w, 5) * 3))
    mid.ridge(swell, np.broadcast_to(mix(hexc(w_mid), hexc(w_h), 0.25), mid.rgb.shape))
    mid.save(os.path.join(out, prefix + '_bg_mid.png'))

    # Near: big swells rising at the edges of the view.
    near = Layer(-HALF_W, HALF_W, HORIZON - 0.15, HORIZON + 3.2, 100)
    xs = near.col_x()
    edge = np.maximum(np.clip((-6.5 - xs) / 6.5, 0, 1), np.clip((xs - 7.0) / 6.0, 0, 1))
    prof = HORIZON - 0.12 + 0.9 * edge ** 1.4 + 0.05 * noise1(rng, near.w, 7)
    coln = mix(hexc(w_deep), hexc(w_mid), smooth(HORIZON, HORIZON + 0.9, near.Y))
    near.ridge(prof, coln, 1.2)
    light_rim(near, prof, sun[0], 0.5, hexc(glint), 0.05)
    near.save(os.path.join(out, prefix + '_bg_near.png'))

    wl = Layer(-HALF_W, HALF_W, -5.8, HORIZON, 64)
    depth = np.clip((HORIZON - wl.Y) / 6.0, 0, 1)
    colw = mix(hexc(w_h), hexc(w_mid), smooth(0.0, 0.10, depth))
    colw = mix(colw, hexc(w_deep), smooth(0.3, 1.0, depth))
    ripple = np.clip(noise2(rng, wl.h, wl.w, 7, 3) * 2.4 + 0.2, 0, 1)
    rows = (np.sin(wl.Y * 30 + noise2(rng, wl.h, wl.w, 30, 2) * 6) * 0.5 + 0.5) ** 3
    if column:
        spread = 0.4 + depth * 1.6
        col_k = np.exp(-((wl.X - sun[0]) / spread) ** 2)
        colw = mix(colw, hexc(glint), np.clip(col_k * ripple * rows * (0.6 if night else 1.1) * (1 - depth * 0.5), 0, 1))
    # Long swell bands all over.
    colw = colw * (1 + (np.sin(wl.Y * 9 + noise2(rng, wl.h, wl.w, 40, 2) * 4) * 0.5 + 0.5)[..., None] * 0.06)
    if night:
        # A few faint plankton specks.
        specks = (rng.random((wl.h, wl.w)) > 0.9993) * smooth(0.1, 0.5, depth)
        colw = mix(colw, hexc('#7FE6FF'), specks * 0.6)
    water_layer = colw * (1 + noise2(rng, wl.h, wl.w, 9, 3)[..., None] * 0.05)
    wl.over(water_layer, np.ones_like(depth))
    wl.save(os.path.join(out, prefix + '_water.png'))

    # Corners: dark swells rising from the bottom corners.
    for side, name in ((-1, 'left'), (1, 'right')):
        W, H, ppu = 5.0, 4.2, 100
        c = Layer(0, W, 0, H, ppu)
        xs = c.col_x()
        u = (xs / W) if side < 0 else (1 - xs / W)
        prof = 2.2 * np.clip(1 - u, 0, 1) ** 1.6 + 0.12 * noise1(np.random.default_rng(seed + (1 if side < 0 else 2)), c.w, 6)
        c.ridge(prof, mix(hexc(w_deep), hexc(w_mid), smooth(0.0, 2.0, c.Y)), 1.5)
        light_rim(c, prof, sun[0] * side, 0.15 if night else 0.6, hexc(glint), 0.06)
        c.save(os.path.join(out, prefix + '_fg_' + name + '.png'))

    for i in range(3):
        lit, shade = ('#9AA6C8', '#2A3256') if night else ('#FFE6C0', '#9AA2C0')
        cl = cloud_sprite(np.random.default_rng(seed + 10 + i), 600, 220, hexc(lit), hexc(shade), 1.0 if sun[0] > 0 else -1.0)
        if night:
            a = np.asarray(cl).astype(np.float32)
            a[..., 3] *= 0.55
            cl = Image.fromarray(a.astype(np.uint8), 'RGBA')
        cl.save(os.path.join(out, prefix + '_cloud_%02d.png' % (i + 1)), optimize=True)

    thumbnail(out, prefix)


def corrente_azul(out):
    open_sea(out, 'map_corrente_azul', 47, (4.8, 1.4), ('#2A62B8', '#6E9AD0', '#FFD49A', '#FFC874'),
             ('#F6C890', '#1A4C9A', '#0A2250', '#FFE0A0'))


def banco_das_baleias(out):
    open_sea(out, 'map_banco_das_baleias', 53, (-4.4, 0.9), ('#38609E', '#8AA0C4', '#FFD0A8', '#FFC890'),
             ('#E8C0A0', '#1E5058', '#08262E', '#FFD8B0'), islets=((-8.0, 0.7, 0.18), (2.5, 0.5, 0.12), (9.0, 0.9, 0.2)))


def talude_noturno(out):
    open_sea(out, 'map_talude_noturno', 59, (-4.6, 2.6), ('#070B24', '#1A2250', '#3A4274', '#8A9CD8'),
             ('#323C6E', '#0A1430', '#02060F', '#C8D8FF'), night=True)


def abismo_atlantico(out):
    open_sea(out, 'map_abismo_atlantico', 61, (5.0, 4.0), ('#020412', '#0A1030', '#1A2048', '#6A7AB8'),
             ('#1A2248', '#040A1E', '#01030A', '#B8C8F0'), night=True, column=False)


MAPS = (('LagoSereno', lago_sereno), ('RioSelvagem', rio_selvagem), ('PantanalDourado', pantanal_dourado),
        ('EstuarioDasMares', estuario_das_mares), ('CostaDeCoral', costa_de_coral), ('ArquipelagoDoSol', arquipelago_do_sol),
        ('CorrenteAzul', corrente_azul), ('BancoDasBaleias', banco_das_baleias), ('TaludeNoturno', talude_noturno),
        ('AbismoAtlantico', abismo_atlantico))



def boat_and_fisherman(out):
    """The rowing boat, the seated fisherman (without the arm, which the game animates) and a tackle box."""
    S = 4

    def save(img, name, size):
        img.resize(size, Image.LANCZOS).save(os.path.join(out, name), optimize=True)

    # Boat: 3.6 x 1.0 units, side view with the inside of the hull just visible.
    bw, bh = 720 * S // 2, 200 * S // 2
    img = Image.new('RGBA', (bw, bh), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    top, bottom = bh * 0.30, bh * 0.92
    hull = []
    for u in np.linspace(-1, 1, 60):
        hull.append((bw / 2 + u * bw * 0.49, top + bh * 0.06 * abs(u) ** 3 * -1))
    for u in np.linspace(1, -1, 60):
        hull.append((bw / 2 + u * bw * 0.42 * (1 - 0.25 * abs(u) ** 2), bottom - (bottom - top) * 0.55 * abs(u) ** 2.4))
    d.polygon(hull, fill=(120, 72, 38, 255))
    # Planks, kept inside the hull.
    planks = Image.new('RGBA', (bw, bh), (0, 0, 0, 0))
    pdraw = ImageDraw.Draw(planks)
    for k in range(1, 4):
        y = top + (bottom - top) * k / 4.2
        pdraw.line([(0, y), (bw, y + 4)], fill=(88, 50, 26, 255), width=5 * S // 2)
    planks.putalpha(Image.fromarray(np.minimum(np.asarray(planks.split()[3]), np.asarray(img.split()[3]))))
    img.alpha_composite(planks)
    # Inside of the hull and the rim.
    d.polygon([(bw * 0.03, top), (bw * 0.97, top), (bw * 0.9, top - bh * 0.1), (bw * 0.1, top - bh * 0.1)], fill=(70, 40, 22, 255))
    d.line([(bw * 0.02, top), (bw * 0.98, top)], fill=(214, 160, 96, 255), width=12 * S // 2)
    d.line([(bw * 0.1, top - bh * 0.1), (bw * 0.9, top - bh * 0.1)], fill=(160, 110, 62, 255), width=6 * S // 2)
    # Sun on the hull (right side), shadow at the waterline.
    shade = Image.new('L', (bw, bh), 0)
    ImageDraw.Draw(shade).rectangle([0, bottom - (bottom - top) * 0.35, bw, bh], fill=110)
    mask = img.split()[3]
    dark = Image.new('RGBA', (bw, bh), (30, 16, 10, 0))
    dark.putalpha(Image.fromarray(np.minimum(np.asarray(shade.filter(ImageFilter.GaussianBlur(20 * S // 2))), np.asarray(mask))))
    img.alpha_composite(dark)
    warm = Image.new('RGBA', (bw, bh), (255, 190, 110, 0))
    grad = np.clip((np.arange(bw)[None, :] / bw - 0.45) * 120, 0, 60).astype(np.uint8) * np.ones((bh, 1), dtype=np.uint8)
    warm.putalpha(Image.fromarray(np.minimum(grad, np.asarray(mask))))
    img.alpha_composite(warm)
    save(img, 'barco.png', (720, 200))

    # Fisherman: 0.8 x 1.3 units, seated, seen from the back and side, facing right.
    fw, fh = 320 * S // 2, 520 * S // 2
    img = Image.new('RGBA', (fw, fh), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = fw * 0.46
    # Legs (knees forward, mostly hidden by the rim).
    d.rounded_rectangle([cx - fw * 0.12, fh * 0.80, cx + fw * 0.42, fh * 0.95], radius=fw * 0.07, fill=(46, 52, 66, 255))
    # Torso.
    d.polygon([(cx - fw * 0.26, fh * 0.90), (cx - fw * 0.22, fh * 0.50), (cx - fw * 0.1, fh * 0.40), (cx + fw * 0.16, fh * 0.40),
               (cx + fw * 0.26, fh * 0.52), (cx + fw * 0.24, fh * 0.90)], fill=(58, 104, 152, 255))
    d.rounded_rectangle([cx - fw * 0.27, fh * 0.42, cx + fw * 0.27, fh * 0.92], radius=fw * 0.16, fill=(58, 104, 152, 255))
    # Shirt shading and a warm rim on the sun side.
    d.polygon([(cx - fw * 0.27, fh * 0.55), (cx - fw * 0.05, fh * 0.45), (cx - fw * 0.1, fh * 0.92), (cx - fw * 0.27, fh * 0.92)], fill=(44, 82, 124, 255))
    d.line([(cx + fw * 0.25, fh * 0.52), (cx + fw * 0.25, fh * 0.88)], fill=(236, 176, 120, 200), width=6 * S // 2)
    # Neck, head, ear, hair.
    d.rounded_rectangle([cx - fw * 0.06, fh * 0.34, cx + fw * 0.08, fh * 0.44], radius=fw * 0.04, fill=(196, 132, 96, 255))
    d.ellipse([cx - fw * 0.15, fh * 0.17, cx + fw * 0.17, fh * 0.40], fill=(214, 150, 110, 255))
    d.ellipse([cx - fw * 0.15, fh * 0.20, cx + fw * 0.06, fh * 0.40], fill=(92, 62, 44, 255))
    d.ellipse([cx + fw * 0.02, fh * 0.26, cx + fw * 0.08, fh * 0.33], fill=(190, 124, 90, 255))
    # Straw hat: brim, crown and band.
    d.ellipse([cx - fw * 0.36, fh * 0.155, cx + fw * 0.40, fh * 0.235], fill=(214, 176, 100, 255))
    d.ellipse([cx - fw * 0.34, fh * 0.15, cx + fw * 0.38, fh * 0.215], fill=(236, 204, 128, 255))
    d.rounded_rectangle([cx - fw * 0.17, fh * 0.07, cx + fw * 0.19, fh * 0.19], radius=fw * 0.09, fill=(236, 204, 128, 255))
    d.rectangle([cx - fw * 0.17, fh * 0.155, cx + fw * 0.19, fh * 0.185], fill=(142, 58, 46, 255))
    d.arc([cx - fw * 0.12, fh * 0.075, cx + fw * 0.16, fh * 0.16], 200, 320, fill=(255, 232, 170, 255), width=5 * S // 2)
    save(img, 'pescador.png', (320, 520))

    # Tackle box.
    tb = Image.new('RGBA', (160 * S // 2, 110 * S // 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(tb)
    w, h = tb.size
    d.rounded_rectangle([w * 0.05, h * 0.3, w * 0.95, h * 0.95], radius=w * 0.06, fill=(70, 98, 72, 255))
    d.rounded_rectangle([w * 0.05, h * 0.18, w * 0.95, h * 0.42], radius=w * 0.06, fill=(92, 124, 90, 255))
    d.rounded_rectangle([w * 0.35, h * 0.02, w * 0.65, h * 0.2], radius=w * 0.04, outline=(40, 44, 40, 255), width=6)
    d.rectangle([w * 0.45, h * 0.4, w * 0.55, h * 0.55], fill=(196, 170, 96, 255))
    save(tb, 'caixa_de_pesca.png', (160, 110))


def waves(out):
    """Tileable strip of small wave crests (Resources/Arte/Agua/ondas.png), white on transparent.

    The game lays several rows of it over the water, smaller near the horizon and bigger close to the
    viewer, and slides them sideways so the surface moves. Wraps left to right without a seam.
    """
    W, H, S = 2048, 128, 2
    img = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(9)
    for _ in range(230):
        x = rng.uniform(0, W)
        y = rng.uniform(0.12, 0.88) * H
        length = rng.uniform(40, 150)
        bend = length * rng.uniform(0.01, 0.035)
        width = rng.uniform(2.0, 4.2)
        alpha = rng.uniform(120, 240)
        for dx in (0, -W, W):
            # A ripple: a bright crest with a soft dark trough just under it, thickest in the middle.
            us = np.linspace(0, 1, 18)
            crest = [((x + dx + length * u) * S, (y - math.sin(math.pi * u) * bend) * S) for u in us]
            trough = [(px, py + width * S * 1.6) for px, py in crest]
            for pts, colour, strength in ((trough, (18, 34, 58), 0.55), (crest, (255, 255, 255), 1.0)):
                for i in range(len(pts) - 1):
                    taper = math.sin(math.pi * (i + 0.5) / (len(pts) - 1))
                    d.line([pts[i], pts[i + 1]], fill=colour + (int(alpha * strength * (0.3 + 0.7 * taper)),), width=max(1, int(width * S * taper)))
    img = img.filter(ImageFilter.GaussianBlur(1.0 * S)).resize((W, H), Image.LANCZOS)
    img.save(os.path.join(out, 'ondas.png'), optimize=True)


def rods(out):
    """One picture per rod (Resources/Arte/Varas/rod_<id>.png), 2:1 like the fish, for cards and the Shop."""
    S = 2
    specs = {
        'rod_00_starter': dict(blank=(168, 120, 64), tip=(120, 84, 44), grip=(120, 78, 44), reel=(150, 156, 160), bands=(92, 60, 34), thick=11),
        'rod_01': dict(blank=(88, 96, 112), tip=(220, 70, 60), grip=(196, 150, 96), reel=(54, 120, 196), bands=(214, 176, 90), thick=12),
        'rod_02': dict(blank=(24, 72, 74), tip=(214, 176, 90), grip=(112, 78, 52), reel=(168, 128, 52), bands=(214, 176, 90), thick=13),
    }
    for rod_id, sp in specs.items():
        w, h = 1024 * S, 512 * S
        img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        x0, y0, x1, y1 = w * 0.08, h * 0.86, w * 0.94, h * 0.10
        def at(u):
            # A gentle bend near the tip.
            return x0 + (x1 - x0) * u, y0 + (y1 - y0) * u + h * 0.06 * u ** 3
        # Line from the tip, hanging.
        tx, ty = at(1.0)
        d.line([(tx, ty), (tx + w * 0.01, ty + h * 0.45)], fill=(236, 236, 228, 190), width=2 * S)
        pts = [at(u) for u in np.linspace(0, 1, 60)]
        for i in range(len(pts) - 1):
            u = i / len(pts)
            width = int((sp['thick'] * (1 - u * 0.8)) * S)
            col = sp['blank'] if u < 0.9 else sp['tip']
            d.line([pts[i], pts[i + 1]], fill=col + (255,), width=max(2, width))
        # Guides (rings) along the blank.
        for u in (0.42, 0.56, 0.68, 0.79, 0.88, 0.95):
            gx, gy = at(u)
            d.ellipse([gx - 6 * S, gy + 3 * S, gx + 6 * S, gy + 15 * S], outline=(200, 204, 210, 255), width=2 * S)
        # Grip and butt.
        g0, g1 = at(0.0), at(0.24)
        d.line([g0, g1], fill=sp['grip'] + (255,), width=int(sp['thick'] * 2.4 * S))
        for u in (0.02, 0.24):
            bx, by = at(u)
            d.ellipse([bx - 13 * S, by - 13 * S, bx + 13 * S, by + 13 * S], fill=sp['bands'] + (255,))
        # Reel.
        rx, ry = at(0.3)
        d.rectangle([rx - 6 * S, ry, rx + 6 * S, ry + 26 * S], fill=(70, 74, 80, 255))
        d.ellipse([rx - 40 * S, ry + 18 * S, rx + 40 * S, ry + 98 * S], fill=sp['reel'] + (255,))
        d.ellipse([rx - 26 * S, ry + 32 * S, rx + 26 * S, ry + 84 * S], fill=tuple(int(c * 0.6) for c in sp['reel']) + (255,))
        d.ellipse([rx - 9 * S, ry + 49 * S, rx + 9 * S, ry + 67 * S], fill=(220, 224, 228, 255))
        d.line([(rx + 30 * S, ry + 58 * S), (rx + 62 * S, ry + 70 * S)], fill=(60, 64, 70, 255), width=6 * S)
        d.ellipse([rx + 56 * S, ry + 62 * S, rx + 72 * S, ry + 78 * S], fill=(230, 230, 230, 255))
        img.resize((1024, 512), Image.LANCZOS).save(os.path.join(out, rod_id + '.png'), optimize=True)


def expeditions(maps_dir, out):
    """One picture per expedition (Resources/Arte/Expedicoes/exp_<id>.png), from the Lago Sereno art."""
    lago = os.path.join(maps_dir, 'LagoSereno')
    prefix = 'map_lago_sereno'
    thumbnail(lago, prefix, (-10.0, -2.0, -2.2, 3.0), (600, 390), os.path.join(out, 'exp_30m.png'))
    thumbnail(lago, prefix, (3.0, 11.0, -1.8, 3.4), (600, 390), os.path.join(out, 'exp_1h.png'))
    long_trip = thumbnail(lago, prefix, (-6.5, 6.5, -3.4, 5.0), (600, 390), os.path.join(out, 'exp_6h.png'))
    # Deep water: an underwater view with light rays and distant fish silhouettes.
    w, h = 600, 390
    rng = np.random.default_rng(5)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    col = mix(hexc('#2F7FA6'), hexc('#0B2542'), smooth(0, h, yy))
    rays = np.zeros((h, w), dtype=np.float32)
    for _ in range(7):
        cx = rng.uniform(0, w)
        slope = rng.uniform(-0.35, 0.1)
        width = rng.uniform(14, 40)
        rays += np.exp(-((xx - (cx + yy * slope)) / width) ** 2) * (1 - yy / h) * rng.uniform(0.2, 0.45)
    col = mix(col, hexc('#BFEFFF'), np.clip(rays, 0, 1) * 0.5)
    img = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8), 'RGB').convert('RGBA')
    d = ImageDraw.Draw(img)
    for _ in range(6):
        fx, fy, fl = rng.uniform(60, 540), rng.uniform(150, 340), rng.uniform(40, 90)
        c = (12, 40, 62, 200)
        d.ellipse([fx - fl / 2, fy - fl * 0.18, fx + fl / 2, fy + fl * 0.18], fill=c)
        d.polygon([(fx - fl / 2 + 4, fy), (fx - fl * 0.78, fy - fl * 0.2), (fx - fl * 0.78, fy + fl * 0.2)], fill=c)
    for _ in range(26):
        bx, by, br = rng.uniform(0, w), rng.uniform(0, h), rng.uniform(1.5, 4)
        d.ellipse([bx - br, by - br, bx + br, by + br], outline=(210, 240, 255, 150), width=1)
    img.save(os.path.join(out, 'exp_3h.png'), optimize=True)


def main():
    import sys
    finais.proteger_finais()
    # With map folder names (e.g. "CostaDeCoral ArquipelagoDoSol") only those maps are painted.
    only = set(sys.argv[1:])
    if not only:
        agua = os.path.join(ROOT, 'Agua')
        os.makedirs(agua, exist_ok=True)
        waves(agua)
        varas = os.path.join(ROOT, 'Varas')
        os.makedirs(varas, exist_ok=True)
        rods(varas)
        cena = os.path.join(ROOT, 'Cena')
        os.makedirs(cena, exist_ok=True)
        boat_and_fisherman(cena)
    for folder, fn in MAPS:
        if only and folder not in only:
            continue
        out = os.path.join(ROOT, 'Mapas', folder)
        os.makedirs(out, exist_ok=True)
        fn(out)
        print('  ', folder)
    if not only:
        exp = os.path.join(ROOT, 'Expedicoes')
        os.makedirs(exp, exist_ok=True)
        expeditions(os.path.join(ROOT, 'Mapas'), exp)
    print('Cenários gerados em', os.path.normpath(os.path.join(ROOT, 'Mapas')))


if __name__ == '__main__':
    main()
