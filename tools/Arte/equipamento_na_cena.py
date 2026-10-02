"""Puts the equipped boat, rod and bait in the fishing scene (addendum A-096).

Usage:  python3 tools/Arte/equipamento_na_cena.py [--iscas <imagem.png>] [--previa <saida.png>]

What it does:
  - For each Shop boat (Resources/Arte/Barcos/boat_01..05) cuts a "front" layer: the near side of the
    hull, below the near gunwale. The scene draws the whole boat, then the fisherman, then this layer,
    so he sits inside the boat. The gunwale lines are drawn by hand below (pixels of the 1024 x 512 art).
  - Measures each rod picture (Resources/Arte/Varas): the butt and the tip, so the scene can lay the
    painted rod along the animated one.
  - Writes Resources/Visual/equipamento_cena.json with all of it (plus the placeholder bait colours).
  - With --iscas, cuts the bait picture from the owner (three baits side by side, Simples, Melhorada
    and Premium, on a flat green, magenta or blue background) into Resources/Arte/Iscas/bait_0X.png.
  - With --previa, draws every boat with the fisherman and each rod, to check the fit outside Unity.

The starter boat (boat_00) keeps the scene's own two-layer hull (Cena/barco_fundo + barco_frente).
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ARTE = os.path.join(ROOT, 'client-unity', 'Assets', 'Resources', 'Arte')
VISUAL = os.path.join(ROOT, 'client-unity', 'Assets', 'Resources', 'Visual')
FINAIS = os.path.join(os.path.dirname(__file__), 'finais.txt')

# Near gunwale of each boat (x, y in the 1024 x 512 picture), stern to bow. Below the line = front layer.
GUNWALES = {
    'boat_01': [(25, 188), (100, 216), (250, 246), (400, 262), (550, 266), (700, 259), (850, 236), (950, 202), (1000, 168)],
    'boat_02': [(100, 258), (400, 263), (650, 256), (800, 242), (900, 214), (1000, 176)],
    'boat_03': [(80, 262), (300, 259), (500, 251), (700, 239), (850, 216), (1000, 181)],
    'boat_04': [(70, 296), (300, 293), (500, 286), (700, 273), (850, 263), (1000, 251)],
    'boat_05': [(140, 323), (370, 323), (410, 346), (600, 341), (800, 323), (910, 301)],
}

# Size in the scene (width of the 1024 px picture, in scene units) and where the fisherman sits
# (x in the picture; he sits just below the gunwale there). Bigger boats look bigger.
BOATS = {
    'boat_01': {'width': 3.9, 'seat_x': 330},
    'boat_02': {'width': 4.1, 'seat_x': 330},
    'boat_03': {'width': 4.3, 'seat_x': 420},
    'boat_04': {'width': 4.6, 'seat_x': 300},
    'boat_05': {'width': 5.3, 'seat_x': 300},
}
SEAT_DROP_PX = 34       # his seat (the pivot of pescador.png) this far below the gunwale
WATERLINE = 0.22        # waterline: this fraction of the hull height above its keel

# Where the hands hold each rod, as a fraction from the butt to the tip.
ROD_GRIP = {'rod_00_starter': 0.2, 'rod_01': 0.22, 'rod_02': 0.24}

# Bait on the hook: Resources/Arte/Iscas/<id>.png when it exists; until then this colour (ASSET_PENDENTE).
BAITS = {'bait_01': '#9A6A4A', 'bait_02': '#E0803C', 'bait_03': '#E9BE45'}

# Scene constants mirrored from FishermanRig / FishingScene (for the preview only).
ROD_LENGTH = 2.7
FISHERMAN_HEIGHT = 1.7
FISHERMAN_PIVOT = (0.27, 0.12)
FISHERMAN_HANDS = (0.97, 0.23)


def line_y(points, x):
    if x <= points[0][0]:
        return points[0][1]
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        if x <= x1:
            return y0 + (y1 - y0) * (x - x0) / float(x1 - x0)
    return points[-1][1]


def front_layer(boat_id):
    im = Image.open(os.path.join(ARTE, 'Barcos', boat_id + '.png')).convert('RGBA')
    a = np.array(im)
    h, w = a.shape[:2]
    line = np.array([line_y(GUNWALES[boat_id], x) for x in range(w)])
    ys = np.arange(h)[:, None]
    # 2 px soft edge, so the cut never shows a hard step.
    keep = np.clip((ys - line[None, :] + 1.0) / 2.0, 0.0, 1.0)
    a[..., 3] = (a[..., 3] * keep).astype(np.uint8)
    return Image.fromarray(a)


def keel_and_rim(boat_id):
    a = np.array(Image.open(os.path.join(ARTE, 'Barcos', boat_id + '.png')).convert('RGBA'))[..., 3]
    rows = np.where(a.max(axis=1) > 128)[0]
    keel = int(rows.max())
    seat_x = BOATS[boat_id]['seat_x']
    rim = line_y(GUNWALES[boat_id], seat_x)
    return keel, rim


def boat_entry(boat_id):
    keel, rim = keel_and_rim(boat_id)
    waterline_px = keel - WATERLINE * (keel - min(p[1] for p in GUNWALES[boat_id]))
    pivot_y = 1.0 - waterline_px / 512.0
    width = BOATS[boat_id]['width']
    units = width / 1024.0
    seat_x = BOATS[boat_id]['seat_x']
    return {
        'id': boat_id,
        'art': 'Barcos/' + boat_id,
        'front': 'Barcos/Cena/' + boat_id + '_frente',
        'width': width,
        'pivot_y': round(pivot_y, 4),
        'seat_x': round((seat_x - 512) * units, 3),
        'seat_y': round((waterline_px - (rim + SEAT_DROP_PX)) * units, 3),
    }


def rod_entry(rod_id):
    a = np.array(Image.open(os.path.join(ARTE, 'Varas', rod_id + '.png')).convert('RGBA'))[..., 3]
    ys, xs = np.where(a > 128)
    left = xs <= xs.min() + 12
    right = xs >= xs.max() - 6
    butt = (float(xs[left].mean()), float(ys[left].mean()))
    tip = (float(xs[right].mean()), float(ys[right].mean()))
    h, w = a.shape
    return {
        'id': rod_id,
        'art': 'Varas/' + rod_id,
        'butt_u': round(butt[0] / w, 4), 'butt_v': round(1 - butt[1] / h, 4),
        'tip_u': round(tip[0] / w, 4), 'tip_v': round(1 - tip[1] / h, 4),
        'grip': ROD_GRIP.get(rod_id, 0.2),
    }


def bait_entry(bait_id, color):
    # The picture (Resources/Arte/Iscas/<id>.png) is used when it exists; the colour stays as the fallback.
    entry = {'id': bait_id, 'color': color, 'width': 0.22}
    if os.path.exists(os.path.join(ARTE, 'Iscas', bait_id + '.png')):
        entry['art'] = 'Iscas/' + bait_id
    return entry


def write_all():
    os.makedirs(os.path.join(ARTE, 'Barcos', 'Cena'), exist_ok=True)
    written = []
    for boat_id in GUNWALES:
        path = os.path.join(ARTE, 'Barcos', 'Cena', boat_id + '_frente.png')
        front_layer(boat_id).save(path, optimize=True)
        written.append(os.path.relpath(path, ARTE).replace(os.sep, '/'))
    data = {
        'note': 'Gerado por tools/Arte/equipamento_na_cena.py. Barco, vara e isca equipados na cena (GDD_ADENDO A-096).',
        'boats': [boat_entry(b) for b in GUNWALES],
        'rods': [rod_entry(r) for r in sorted(ROD_GRIP)],
        'baits': [bait_entry(k, v) for k, v in BAITS.items()],
    }
    path = os.path.join(VISUAL, 'equipamento_cena.json')
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')
    with open(FINAIS, encoding='utf-8') as f:
        known = set(l.strip() for l in f if l.strip())
    with open(FINAIS, 'a', encoding='utf-8') as f:
        for p in written:
            if p not in known:
                f.write(p + '\n')
    print('Arquivos gravados:', len(written) + 1)
    return data


# ---------------------------------------------------------------- preview

S = 120  # pixels per scene unit


def paste(canvas, img, units_wide, pivot, at, origin, rotate=0.0):
    w = int(round(units_wide * S))
    h = int(round(units_wide * S * img.height / img.width))
    im = img.resize((w, h), Image.LANCZOS)
    px, py = pivot[0] * w, (1 - pivot[1]) * h
    if rotate:
        big = Image.new('RGBA', (w * 3, h * 3 + w * 2), (0, 0, 0, 0))
        cx, cy = big.width // 2, big.height // 2
        big.alpha_composite(im, (int(cx - px), int(cy - py)))
        im = big.rotate(rotate, resample=Image.BICUBIC, center=(cx, cy))
        px, py = cx, cy
    x = origin[0] + at[0] * S - px
    y = origin[1] - at[1] * S - py
    canvas.alpha_composite(im, (int(x), int(y)))


def scene_tile(boat, rod, bait):
    tile = Image.new('RGBA', (900, 560))
    d = ImageDraw.Draw(tile)
    for y in range(560):
        c = (150, 200, 225) if y < 300 else (40, 110, 130)
        d.line([(0, y), (900, y)], fill=c + (255,))
    origin = (360, 330)  # the boat's position
    fisher = Image.open(os.path.join(ARTE, 'Cena', 'pescador.png')).convert('RGBA')
    fw = FISHERMAN_HEIGHT * fisher.width / fisher.height
    if boat is None:
        back = Image.open(os.path.join(ARTE, 'Cena', 'barco_fundo.png')).convert('RGBA')
        front = Image.open(os.path.join(ARTE, 'Cena', 'barco_frente.png')).convert('RGBA')
        paste(tile, back, 3.6, (0.5, 0.3), (0, 0), origin)
        seat = (-0.35, 0.2)
        paste(tile, fisher, fw, FISHERMAN_PIVOT, seat, origin)
        draw_rod(tile, rod, seat, fw, origin, bait)
        paste(tile, front, 3.6, (0.5, 0.3), (0, 0), origin)
    else:
        whole = Image.open(os.path.join(ARTE, 'Barcos', boat['id'] + '.png')).convert('RGBA')
        front = Image.open(os.path.join(ARTE, 'Barcos', 'Cena', boat['id'] + '_frente.png')).convert('RGBA')
        paste(tile, whole, boat['width'], (0.5, boat['pivot_y']), (0, 0), origin)
        seat = (boat['seat_x'], boat['seat_y'])
        paste(tile, fisher, fw, FISHERMAN_PIVOT, seat, origin)
        draw_rod(tile, rod, seat, fw, origin, bait)
        paste(tile, front, boat['width'], (0.5, boat['pivot_y']), (0, 0), origin)
    return tile


def draw_rod(tile, rod, seat, fw, origin, bait):
    hands = (seat[0] + (FISHERMAN_HANDS[0] - FISHERMAN_PIVOT[0]) * fw, seat[1] + (FISHERMAN_HANDS[1] - FISHERMAN_PIVOT[1]) * FISHERMAN_HEIGHT)
    angle = 58.0
    img = Image.open(os.path.join(ARTE, rod['art'] + '.png')).convert('RGBA')
    du = (rod['tip_u'] - rod['butt_u']) * img.width
    dv = (rod['tip_v'] - rod['butt_v']) * img.height
    length_px = math.hypot(du, dv)
    image_angle = math.degrees(math.atan2(dv, du))
    total = ROD_LENGTH / (1 - rod['grip'])
    units = total * img.width / length_px
    rad = math.radians(angle)
    butt = (hands[0] - math.cos(rad) * rod['grip'] * total, hands[1] - math.sin(rad) * rod['grip'] * total)
    paste(tile, img, units, (rod['butt_u'], rod['butt_v']), butt, origin, rotate=angle - image_angle)
    tip = (hands[0] + math.cos(rad) * ROD_LENGTH, hands[1] + math.sin(rad) * ROD_LENGTH)
    d = ImageDraw.Draw(tile)
    tx, ty = origin[0] + tip[0] * S, origin[1] - tip[1] * S
    bx, by = tx, ty + 0.75 * S
    d.line([(tx, ty), (bx, by)], fill=(235, 235, 225, 200), width=2)
    r = 0.085 * S
    d.ellipse([bx - r, by - r, bx + r, by + r], fill=(224, 56, 46, 255))
    if bait:
        hx, hy = bx, by + 0.3 * S
        d.line([(bx, by + r), (hx, hy)], fill=(235, 235, 225, 200), width=1)
        col = tuple(int(bait[i:i + 2], 16) for i in (1, 3, 5))
        d.rounded_rectangle([hx - 0.08 * S, hy - 0.03 * S, hx + 0.08 * S, hy + 0.05 * S], radius=4, fill=col + (255,))


def preview(data, out):
    rods = {r['id']: r for r in data['rods']}
    rows = [(None, 'rod_00_starter', None, 'Barco Inicial · Vara Inicial · sem isca')]
    names = ['Barco 1', 'Barco 2', 'Barco 3', 'Barco 4', 'Barco 5']
    rod_for = ['rod_00_starter', 'rod_01', 'rod_01', 'rod_02', 'rod_02']
    bait_for = ['bait_01', None, 'bait_02', 'bait_03', 'bait_03']
    rod_names = {'rod_00_starter': 'Vara Inicial', 'rod_01': 'Vara 1', 'rod_02': 'Vara 2'}
    bait_names = {None: 'sem isca', 'bait_01': 'Isca Simples', 'bait_02': 'Isca Melhorada', 'bait_03': 'Isca Premium'}
    for i, b in enumerate(data['boats']):
        rows.append((b, rod_for[i], bait_for[i], names[i] + ' · ' + rod_names[rod_for[i]] + ' · ' + bait_names[bait_for[i]]))
    sheet = Image.new('RGBA', (1800, 60 + 3 * 600), (18, 26, 38, 255))
    d = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype(os.path.join(ROOT, 'client-unity', 'Assets', 'Resources', 'Fontes', 'Nunito-Bold.ttf'), 26)
        small = ImageFont.truetype(os.path.join(ROOT, 'client-unity', 'Assets', 'Resources', 'Fontes', 'Nunito-Regular.ttf'), 22)
    except OSError:
        font = small = ImageFont.load_default()
    d.text((20, 14), 'Prévia montada fora do Unity (não é print do jogo) — barco, vara e isca equipados', font=font, fill=(240, 240, 240))
    for i, (boat, rod_id, bait_id, label) in enumerate(rows):
        tile = scene_tile(boat, rods[rod_id], BAITS.get(bait_id))
        x, y = (i % 2) * 900, 60 + (i // 2) * 600
        sheet.alpha_composite(tile, (x, y))
        d.text((x + 16, y + 520), label, font=small, fill=(255, 255, 255))
    sheet.convert('RGB').save(out)
    print('Prévia:', out)


def cut_baits(path):
    import processar_mapas_3_4 as M
    import processar_pedidos as P
    M.grid(Image.open(path).convert('RGBA'), 3, 1, ['bait_01', 'bait_02', 'bait_03'], 'Iscas/%s.png', (256, 256), 0.06)
    with open(FINAIS, encoding='utf-8') as f:
        known = set(l.strip() for l in f if l.strip())
    with open(FINAIS, 'a', encoding='utf-8') as f:
        for rel in P.written:
            if rel not in known:
                f.write(rel + '\n')


if __name__ == '__main__':
    if '--iscas' in sys.argv:
        cut_baits(sys.argv[sys.argv.index('--iscas') + 1])
    data = write_all()
    if '--previa' in sys.argv:
        preview(data, sys.argv[sys.argv.index('--previa') + 1])
