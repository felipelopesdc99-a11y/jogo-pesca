"""Cena de pesca em pixel art (Estilo ARC, M25-T05 a T08 e T12): dados da cena, cortes, conversões e prévias.

Uso:  python3 tools/Arte/cena_pixel.py dados          grava Resources/Visual/cena_pixel.json e a frente dos barcos
      python3 tools/Arte/cena_pixel.py simbolos       desenha os 12 símbolos simples (brancos, tingíveis) em ArtePixel/Icones
      python3 tools/Arte/cena_pixel.py provisorios    converte da arte antiga as laterais e cantos que faltam (provisório)
      python3 tools/Arte/cena_pixel.py vivos          converte a paisagem viva (fontes_antigas/Vivos) para ArtePixel/Vivos
      python3 tools/Arte/cena_pixel.py miniaturas     gera as miniaturas dos 10 mapas das camadas em pixel
      python3 tools/Arte/cena_pixel.py previa <pasta> [mapa ...]   monta a cena como o jogo desenha (PNG) e o GIF do pescador
      python3 tools/Arte/cena_pixel.py tudo           dados + simbolos + provisorios + vivos + miniaturas

A montagem das prévias e das miniaturas segue a mesma composição do código (FishingScene.BuildPixel, PixelSky,
FishermanRig): grade de 216 px de altura, 20 px por unidade, horizonte na linha 104, camadas presas ao horizonte e
às bordas da tela, tudo em pixel inteiro. Os números ficam no cena_pixel.json que o jogo lê, então a prévia e o jogo
usam os mesmos dados. Visual só fica verificado quando o proprietário apertar Play.

Os comandos provisorios e vivos leem a arte antiga guardada em tools/Arte/fontes_antigas (fora de Resources, para
não ir no build do jogo; M25-T17).
"""
import json
import math
import os
import random
import re
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_folhas as PF  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ASSETS = os.path.join(ROOT, 'client-unity', 'Assets')
# The old painted art the provisional conversions start from, kept out of Resources so it never ships (M25-T17).
ARTE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fontes_antigas')
PIXEL = os.path.join(ASSETS, 'Resources', 'ArtePixel')
VISUAL = os.path.join(ASSETS, 'Resources', 'Visual')
ANIMALS_CS = os.path.join(ASSETS, 'Scripts', 'Game', 'Scene', 'LivingAnimals.cs')
JSON_OUT = os.path.join(VISUAL, 'cena_pixel.json')

# ---------------------------------------------------------------- the grid (ART_BIBLE_PIXEL 3.1)

GRID_H = 216
PPU = 20
HORIZON_Y = 0.2                     # world units (FishingScene.Horizon)
HORIZON_ROW = int(round((GRID_H / 2) - HORIZON_Y * PPU))   # 104 rows of sky above the water

# ---------------------------------------------------------------- maps (ART_BIBLE_PIXEL 6.2)
# sky: top → horizon; water: surface → bottom. Offsets in grid pixels, positive = up.
#   far_dy/mid_dy/near_dy: bottom of each band against the horizon
#   side_dy: bottom of the side banks (near_left/right) against the horizon (negative = in the water)
#   reflect_bands: False when the bands are open sea themselves (nothing to mirror; the painted moon of map 9 is in its band)
#   cloud: tint of the 6 common clouds; clouds: how many; stars: 1 px stars (maps 9 and 10); moon: [x, y, radius] world units

MAPS = [
    dict(id='map_01', folder='LagoSereno', prefix='map_lago_sereno',
         sky=['#FCFCE9', '#F9FAE4', '#F5F7DF', '#F0F4DB'],
         water=['#DCEDE4', '#CFE5DD', '#C0DBD4', '#AECDC8', '#99BDBA', '#83A9A8'],
         cloud='#FFFFFF', cloud_alpha=0.95, clouds=4, glint='#FFFFFF', reflect='#9FC3BE', reflect_alpha=0.55,
         far_dy=14, mid_dy=2, near_dy=0, side_dy=-10),
    dict(id='map_02', folder='RioSelvagem', prefix='map_rio_selvagem',
         sky=['#EEF6F4', '#E2F0EE', '#D3E8E6', '#C4DEDD'],
         water=['#BFE0DC', '#9FCFCC', '#7DBAB8', '#5EA3A3', '#468A8C', '#34707A'],
         cloud='#F4FBF8', cloud_alpha=0.9, clouds=4, glint='#F4FBF8', reflect='#6FA7A6', reflect_alpha=0.55,
         far_dy=14, mid_dy=2, near_dy=0, side_dy=-12),
    dict(id='map_03', folder='PantanalDourado', prefix='map_pantanal_dourado',
         sky=['#FFF1CF', '#FDE4B0', '#F9D494', '#F3C37F'],
         water=['#E9DCB0', '#D6CFA4', '#BFC19A', '#A6B18F', '#8C9E84', '#738A78'],
         cloud='#FFF6E0', cloud_alpha=0.9, clouds=4, glint='#FFF1CF', reflect='#B9B38E', reflect_alpha=0.55,
         far_dy=18, mid_dy=4, near_dy=0, side_dy=-12),
    dict(id='map_04', folder='EstuarioDasMares', prefix='map_estuario_das_mares',
         sky=['#FFD8A8', '#F7BB88', '#E99D78', '#D4826F'],
         water=['#8AA59A', '#6F8F88', '#587A76', '#446663', '#335250', '#243F3F'],
         cloud='#FCE0C4', cloud_alpha=0.85, clouds=3, glint='#F2B67A', reflect='#6F8F88', reflect_alpha=0.6,
         far_dy=20, mid_dy=4, near_dy=0, side_dy=-12),
    dict(id='map_05', folder='CostaDeCoral', prefix='map_costa_de_coral',
         sky=['#E9F7FB', '#D7F0F7', '#C1E7F2', '#A9DCEC'],
         water=['#A6ECE6', '#7FDCD8', '#58C8C8', '#3FB0B8', '#2F95A6', '#237A90'],
         cloud='#FFFFFF', cloud_alpha=0.95, clouds=4, glint='#FFFFFF', reflect='#7FDCD8', reflect_alpha=0.5,
         far_dy=18, mid_dy=4, near_dy=0, side_dy=-12),
    dict(id='map_06', folder='ArquipelagoDoSol', prefix='map_arquipelago_do_sol',
         sky=['#DFF0FB', '#C8E5F8', '#B0D8F3', '#97C9EC'],
         water=['#6AA6E0', '#4D8BD0', '#3A72BD', '#2C5CA6', '#22498E', '#193875'],
         cloud='#FFFFFF', cloud_alpha=0.95, clouds=4, glint='#F6D27A', reflect='#4D8BD0', reflect_alpha=0.55,
         far_dy=16, mid_dy=4, near_dy=0, side_dy=-12),
    dict(id='map_07', folder='CorrenteAzul', prefix='map_corrente_azul',
         sky=['#8FBDE6', '#A9CBE8', '#D3DFE6', '#FBE1B0'],
         water=['#4F86D1', '#3D70BF', '#2F5BA8', '#24498F', '#1B3976', '#142B5E'],
         cloud='#FBE1B0', cloud_alpha=0.85, clouds=4, glint='#EEF4FB', reflect='#3D70BF', reflect_alpha=0.55,
         far_dy=10, mid_dy=2, near_dy=-4, side_dy=-14, reflect_bands=False),
    dict(id='map_08', folder='BancoDasBaleias', prefix='map_banco_das_baleias',
         sky=['#B9CDD8', '#CDD9DD', '#E2DFD8', '#F3D9C8'],
         water=['#6F9A97', '#5B8784', '#497370', '#3A605E', '#2D4E4D', '#213D3D'],
         cloud='#E9EEF0', cloud_alpha=0.85, clouds=4, glint='#E9EEF0', reflect='#5B8784', reflect_alpha=0.55,
         far_dy=10, mid_dy=2, near_dy=-2, side_dy=-14),
    dict(id='map_09', folder='TaludeNoturno', prefix='map_talude_noturno',
         sky=['#0D1430', '#141C3E', '#1D2750', '#2A3463'],
         water=['#1C2B4F', '#172443', '#121D38', '#0E172E', '#0A1224', '#070D1B'],
         cloud='#3A4677', cloud_alpha=0.7, clouds=3, glint='#5FD6E0', reflect='#2A3463', reflect_alpha=0.6,
         far_dy=8, mid_dy=2, near_dy=-2, side_dy=-14, stars=46, moon_color='#E8ECF2', reflect_bands=False),
    dict(id='map_10', folder='AbismoAtlantico', prefix='map_abismo_atlantico',
         sky=['#05070F', '#080B18', '#0B1022', '#10172E'],
         water=['#0B1530', '#091228', '#070E20', '#050A18', '#040712', '#02040B'],
         cloud='#1B2340', cloud_alpha=0.6, clouds=2, glint='#3FB7C9', reflect='#10172E', reflect_alpha=0.6,
         far_dy=8, mid_dy=2, near_dy=-2, side_dy=-16, stars=80, moon=[5.0, 4.1, 4], moon_color='#DFE6F5', reflect_bands=False),
]

# Sky and water bands: relative heights (water grows towards the viewer, for depth).
SKY_WEIGHTS = [30, 26, 24, 24]
WATER_WEIGHTS = [1.0, 1.4, 1.8, 2.3, 2.8, 3.4]

# ---------------------------------------------------------------- boats, fisherman, box, bobber

# Near gunwale of each pixel boat (x, y from the top-left, pixels of the file), stern to bow. Below the line = the
# near side of the hull, drawn over the fisherman so he sits inside. seat: where his feet go (column 40 of his frames);
# waterline: row of the file on the water line (the boat's pivot).
BOATS = {
    'boat_00': dict(gunwale=[(0, 16), (15, 19), (30, 21), (60, 21), (90, 20), (105, 17), (118, 10), (124, 6)], seat=(60, 27)),
    'boat_01': dict(gunwale=[(0, 20), (15, 25), (35, 28), (60, 28), (85, 25), (105, 20), (118, 12), (124, 8)], seat=(60, 34)),
    'boat_02': dict(gunwale=[(0, 18), (20, 18), (60, 17), (90, 15), (110, 12), (120, 6), (124, 4)], seat=(60, 23)),
    'boat_03': dict(gunwale=[(0, 28), (20, 28), (60, 27), (90, 24), (110, 18), (120, 10), (124, 6)], seat=(60, 33)),
    'boat_04': dict(gunwale=[(0, 26), (20, 25), (60, 24), (90, 22), (110, 19), (124, 16)], seat=(54, 30)),
    'boat_05': dict(gunwale=[(0, 35), (20, 34), (60, 33), (90, 30), (107, 27)], seat=(48, 39)),
}
STARTER_BOAT = 'boat_00'
# Caixa de Pesca on the starter boat: bottom-centre at this pixel of boat_00 (from the top-left).
BOX_ON_BOAT = (34, 25)

# The fisherman's frames (A-183): 64 × 64, feet centred on column 40, on the last row but one. hand: where his hands
# close on the rod (pixels from the top-left), measured on each frame; the rod and line are drawn from there.
FRAME = 64
FEET = (40, 63)  # the corner under the feet: column 40, bottom edge of row 62
FRAMES = [
    ('pescador', (41, 41)),
    ('parado_1', (41, 41)), ('parado_2', (41, 43)), ('parado_3', (40, 44)), ('parado_4', (41, 45)),
    ('arremesso_1', (23, 39)), ('arremesso_2', (16, 29)), ('arremesso_3', (45, 41)), ('arremesso_4', (43, 43)),
    ('recolhe_1', (41, 45)), ('recolhe_2', (41, 46)), ('recolhe_3', (41, 46)), ('recolhe_4', (40, 47)),
    ('fisgada_1', (37, 29)), ('fisgada_2', (41, 40)),
    ('mostra_1', (44, 34)),
    ('escapou_1', (39, 46)),
]
# Rod drawn by the code: length in grid pixels and the angles of each phase (degrees, as FishermanRig).
ROD_PX = 60
BOBBER_WATERLINE = 11  # row of Cena/boia (12 × 16) on the water line

# World positions (FishingScene / FishermanRig).
BOAT_AT = (-1.2, -2.1)
WATER_TARGET = (3.3, -2.5)

# ---------------------------------------------------------------- helpers


def hexrgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def band_rows(total, weights):
    s = float(sum(weights))
    rows = [int(round(total * w / s)) for w in weights]
    rows[-1] += total - sum(rows)
    return rows


def load(path):
    return Image.open(path).convert('RGBA')


def pixel_path(rel):
    return os.path.join(PIXEL, rel + '.png')


def arte_path(rel):
    return os.path.join(ARTE, rel + '.png')


def exists_pixel(rel):
    return os.path.exists(pixel_path(rel))


def line_y(points, x):
    if x <= points[0][0]:
        return points[0][1]
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        if x <= x1:
            return y0 + (y1 - y0) * (x - x0) / float(x1 - x0)
    return points[-1][1]


def save_png(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)


# ---------------------------------------------------------------- data + boat fronts


def boat_front(boat_id):
    im = load(pixel_path('Barcos/' + boat_id))
    a = np.array(im)
    h, w = a.shape[:2]
    line = np.array([line_y(BOATS[boat_id]['gunwale'], x + 0.5) for x in range(w)])
    keep = np.arange(h)[:, None] >= np.round(line)[None, :]
    a[..., 3] = np.where(keep, a[..., 3], 0)
    return Image.fromarray(a)


def boat_entry(boat_id):
    a = np.array(load(pixel_path('Barcos/' + boat_id)))[..., 3]
    rows = np.where(a.max(axis=1) > 0)[0]
    cols = np.where(a.max(axis=0) > 0)[0]
    keel = int(rows.max())
    rail = min(p[1] for p in BOATS[boat_id]['gunwale'][1:-1])
    waterline = keel - int(round(0.28 * (keel - rail)))
    centre = int(round((cols.min() + cols.max() + 1) / 2))
    return {
        'id': boat_id,
        'art': 'Barcos/' + boat_id,
        'front': 'Barcos/Cena/' + boat_id + '_frente',
        'pivot_x': centre,
        'pivot_y': waterline,
        'seat_x': BOATS[boat_id]['seat'][0],
        'seat_y': BOATS[boat_id]['seat'][1],
    }


def rod_colors(rod_id):
    """The shaft and the grip of a rod, from its pixel picture: the most common light colour and the darkest one."""
    p = pixel_path('Varas/' + rod_id)
    if not os.path.exists(p):
        return None
    a = np.array(load(p)).reshape(-1, 4)
    a = a[a[:, 3] > 0][:, :3]
    luma = a @ np.array([0.3, 0.59, 0.11])
    body = a[(luma > 60) & (luma < 200)]
    vals, cnt = np.unique(body, axis=0, return_counts=True)
    shaft = vals[np.argmax(cnt)] if len(vals) else np.array([90, 60, 40])
    dark = a[(luma > 25) & (luma < 80)]
    vals2, cnt2 = np.unique(dark, axis=0, return_counts=True)
    grip = vals2[np.argmax(cnt2)] if len(vals2) else shaft // 2
    return {'id': rod_id, 'shaft': '#%02X%02X%02X' % tuple(shaft), 'grip': '#%02X%02X%02X' % tuple(grip)}


def data():
    os.makedirs(os.path.join(PIXEL, 'Barcos', 'Cena'), exist_ok=True)
    for b in BOATS:
        if exists_pixel('Barcos/' + b):
            save_png(boat_front(b), pixel_path('Barcos/Cena/' + b + '_frente'))
    maps = []
    for m in MAPS:
        e = {k: m[k] for k in ('id', 'folder', 'prefix', 'sky', 'water', 'cloud', 'cloud_alpha', 'clouds', 'glint',
                               'reflect', 'reflect_alpha', 'far_dy', 'mid_dy', 'near_dy', 'side_dy')}
        e['stars'] = m.get('stars', 0)
        e['reflect_bands'] = m.get('reflect_bands', True)
        moon = m.get('moon')
        e['moon'] = moon is not None
        e['moon_x'], e['moon_y'], e['moon_r'] = (moon if moon else [0, 0, 0])
        e['moon_color'] = m.get('moon_color', '#FFFFFF')
        e['sky_rows'] = band_rows(HORIZON_ROW, SKY_WEIGHTS)
        e['water_rows'] = band_rows(GRID_H - HORIZON_ROW, WATER_WEIGHTS)
        maps.append(e)
    rods = [r for r in (rod_colors(i) for i in ['rod_00_starter', 'rod_01', 'rod_02', 'rod_03', 'rod_04', 'rod_05']) if r]
    out = {
        'note': 'Gerado por tools/Arte/cena_pixel.py. Cena de pesca em pixel art (ART_BIBLE_PIXEL, M25). Pixels contados '
                'do canto de cima à esquerda de cada arquivo; deslocamentos de faixa em pixels da grade (positivo = para cima).',
        'grid_height': GRID_H,
        'horizon_row': HORIZON_ROW,
        'maps': maps,
        'fisherman': {
            'frame': FRAME, 'feet_x': FEET[0], 'feet_y': FEET[1], 'rod_px': ROD_PX,
            'frames': [{'name': n, 'hand_x': h[0], 'hand_y': h[1]} for n, h in FRAMES],
        },
        'boats': [boat_entry(b) for b in BOATS if exists_pixel('Barcos/' + b)],
        'starter_boat': STARTER_BOAT,
        'box_x': BOX_ON_BOAT[0], 'box_y': BOX_ON_BOAT[1],
        'bobber_waterline': BOBBER_WATERLINE,
        'rods': rods,
    }
    with open(JSON_OUT, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
        f.write('\n')
    print('Gravado', os.path.relpath(JSON_OUT, ROOT), 'e a frente de', len(out['boats']), 'barcos')
    return out


# ---------------------------------------------------------------- the 12 simple symbols (white, tinted by the code)

SYMBOLS = {
    'adicionar': ["....##....", "....##....", "....##....", "....##....", "##########", "##########", "....##....", "....##....", "....##....", "....##...."],
    'fechar': ["##......##", "###....###", ".###..###.", "..######..", "...####...", "...####...", "..######..", ".###..###.", "###....###", "##......##"],
    'confirmar': [".........#", "........##", ".......###", "......###.", "#....###..", "##..###...", "######....", ".####.....", "..##......", ".........."],
    'seta': ["..##......", "..###.....", "...###....", "....###...", ".....###..", ".....###..", "....###...", "...###....", "..###.....", "..##......"],
    'seta_longa': [".....##...", ".....###..", "......###.", "##########", "##########", "......###.", ".....###..", ".....##...", "..........", ".........."],
    'iniciar': ["##........", "####......", "######....", "########..", "##########", "##########", "########..", "######....", "####......", "##........"],
    'pausa': ["###....###", "###....###", "###....###", "###....###", "###....###", "###....###", "###....###", "###....###", "###....###", "###....###"],
    'parar': ["##########", "##########", "##########", "##########", "##########", "##########", "##########", "##########", "##########", "##########"],
    'ordenar': ["....##....", "...####...", "..######..", "....##....", "....##....", "....##....", "....##....", "..######..", "...####...", "....##...."],
    'compacto': ["##########", "##########", "..........", "##########", "##########", "..........", "##########", "##########", "..........", ".........."],
    'info': ["....##....", "....##....", "..........", "...###....", "....##....", "....##....", "....##....", "....##....", "...####...", "...####..."],
    'aviso': ["....##....", "....##....", "....##....", "....##....", "....##....", "....##....", "....##....", "..........", "....##....", "....##...."],
}


def symbols():
    for name, rows in SYMBOLS.items():
        img = Image.new('RGBA', (12, 12), (0, 0, 0, 0))
        for y, row in enumerate(rows):
            for x, c in enumerate(row):
                if c == '#':
                    img.putpixel((x + 1, y + 1), (255, 255, 255, 255))
        save_png(img, pixel_path('Icones/ico_' + name))
    print('Símbolos gravados:', len(SYMBOLS))


# ---------------------------------------------------------------- conversion of old art (provisional and the living landscape)


def convert(img, size, colors=32, outline=False, alpha_cut=0.5):
    """Old painted art → pixel art: area reduction to the target size, hard alpha, reduced palette, no lone pixels."""
    img = img.convert('RGBA')
    a = np.array(img).astype(np.float32) / 255.0
    # premultiply so the transparent background does not bleed into the edge colours
    a[..., :3] *= a[..., 3:4]
    pm = Image.fromarray((a * 255).astype(np.uint8), 'RGBA').resize(size, Image.BOX)
    b = np.array(pm).astype(np.float32)
    alpha = b[..., 3] / 255.0
    op = alpha >= alpha_cut
    rgb = np.where(alpha[..., None] > 0.001, b[..., :3] / np.maximum(alpha[..., None], 0.001), 0)
    rgb = np.clip(rgb, 0, 255).astype(np.uint8)
    # lone pixels
    from scipy import ndimage
    n = ndimage.convolve(op.astype(int), np.ones((3, 3), int), mode='constant') - op
    op &= ~(op & (n <= 1))
    rgb = PF.limit_colors(rgb, op, colors)
    if outline and op.any():
        rgb, op = PF.add_outline(rgb, op)
    return PF.to_image(rgb, op)


def provisional_pieces():
    """Map pieces still missing in pixel art: converted from the old art, marked as provisional in ASSETS_PIXEL."""
    wanted = {
        'LagoSereno': ['near_left', 'near_right'],
        'BancoDasBaleias': ['near_left', 'near_right', 'fg_left', 'fg_right'],
        'TaludeNoturno': ['near_left', 'near_right', 'fg_left', 'fg_right'],
    }
    prefix = {m['folder']: m['prefix'] for m in MAPS}
    done = []
    for folder, parts in wanted.items():
        for part in parts:
            rel = 'Mapas/%s/%s_%s' % (folder, prefix[folder], part)
            if exists_pixel(rel) and not is_provisional(rel):
                continue  # the real piece arrived: never overwrite it
            src = arte_path(rel)
            if not os.path.exists(src):
                continue
            img = load(src)
            # same size as the pixel pieces of the other maps: sides ~96 px high, corners ~60 px high
            target_h = 80 if part.startswith('near') else 60
            w = max(1, int(round(img.width * target_h / img.height)))
            w = min(w, 132 if part.startswith('near') else 100)
            h = int(round(img.height * w / img.width))
            out = convert(img, (w, h), colors=48, outline=False)
            save_png(out, pixel_path(rel))
            done.append(rel)
    _mark_provisional(done)
    print('Provisórios convertidos:', len(done))
    return done


PROVISIONAL_LIST = os.path.join(os.path.dirname(__file__), 'pixel_provisorios.txt')


def is_provisional(rel):
    if not os.path.exists(PROVISIONAL_LIST):
        return False
    with open(PROVISIONAL_LIST, encoding='utf-8') as f:
        return rel in set(l.strip() for l in f)


def _mark_provisional(rels):
    known = set()
    if os.path.exists(PROVISIONAL_LIST):
        with open(PROVISIONAL_LIST, encoding='utf-8') as f:
            known = set(l.strip() for l in f if l.strip() and not l.startswith('#'))
    known |= set(rels)
    with open(PROVISIONAL_LIST, 'w', encoding='utf-8') as f:
        f.write('# Peças de ArtePixel convertidas da arte antiga como provisório (cena_pixel.py). Saem quando o pedido chegar.\n')
        for r in sorted(known):
            f.write(r + '\n')


def animal_sizes():
    """Size each animal is shown at (world units, 'w' wide or 'h' tall), read from LivingAnimals.cs: the largest use wins."""
    src = open(ANIMALS_CS, encoding='utf-8').read()
    sizes = {}

    def put(name, kind, value):
        if name not in sizes or value > sizes[name][1]:
            sizes[name] = (kind, value)

    num = r'([\d.]+)f'
    for m in re.finditer(r'Flock\("(\w+)", \d+, ' + num, src):
        put(m.group(1), 'w', float(m.group(2)))
    for m in re.finditer(r'(?<!\w)Frames\("(\w+)", \d+, ' + num, src):
        put(m.group(1), 'w', float(m.group(2)))
    for m in re.finditer(r'FramesByHeight\("(\w+)", \d+, ' + num, src):
        put(m.group(1), 'h', float(m.group(2)))
    for m in re.finditer(r'(?<!\w)One\("(\w+)", ' + num, src):
        put(m.group(1), 'w', float(m.group(2)))
    for m in re.finditer(r'SpriteTall\("(\w+)", ' + num, src):
        put(m.group(1), 'h', float(m.group(2)))
    for m in re.finditer(r'Appear\("(\w+)", ' + num, src):
        put(m.group(1), 'h', float(m.group(2)))
    for m in re.finditer(r'HeronWading\("(\w+)", "(\w+)", ' + num + ', ' + num, src):
        put(m.group(1), 'h', float(m.group(3)))
        put(m.group(2), 'w', float(m.group(4)))
    for kind in ('Surfacer', 'Spout', 'Breach'):
        for m in re.finditer(kind + r'\("(\w+)", ' + num, src):
            put(m.group(1), 'w', float(m.group(2)))
    for m in re.finditer(r'Shadow\((?:Has\("\w+"\) \? )?"(\w+)"(?: : "(\w+)")?, ' + num, src):
        put(m.group(1), 'w', float(m.group(3)))
        if m.group(2):
            put(m.group(2), 'w', float(m.group(3)))
    put('pato', 'w', 0.55)
    put('martim_pouso', 'w', 0.55)
    put('martim_pouso_galho', 'w', 0.55)
    return sizes


def plant_heights():
    d = json.load(open(os.path.join(VISUAL, 'paisagem_viva.json'), encoding='utf-8'))
    heights, layers = {}, {}
    for s in d['scenes']:
        for p in s.get('plants', []):
            heights[p['art']] = max(heights.get(p['art'], 0), p['height'])
            layers.setdefault(p['art'], set()).add(p['layer'])
    return heights, layers


def vivos():
    sizes = animal_sizes()
    heights, layers = plant_heights()
    src_dir = os.path.join(ARTE, 'Vivos')
    count, missing = 0, []
    for sub in ('Animais', 'Plantas'):
        for fn in sorted(os.listdir(os.path.join(src_dir, sub))):
            if not fn.endswith('.png'):
                continue
            name = fn[:-4]
            img = load(os.path.join(src_dir, sub, fn))
            if sub == 'Plantas':
                h_units = heights.get(name)
                if h_units is None:
                    h_units = 1.0
                    missing.append(name)
                th = max(4, int(round(h_units * PPU)))
                tw = max(2, int(round(img.width * th / img.height)))
                shore_only = layers.get(name, {'shore'}) == {'shore'}
                out = convert(img, (tw, th), colors=32, outline=not shore_only)
            else:
                base = re.sub(r'_\d+$', '', name)
                kind, units = sizes.get(base, (None, None))
                if kind is None:
                    kind, units = 'w', 0.6
                    missing.append(name)
                if kind == 'w':
                    tw = max(4, int(round(units * PPU)))
                    th = max(2, int(round(img.height * tw / img.width)))
                else:
                    th = max(4, int(round(units * PPU)))
                    tw = max(2, int(round(img.width * th / img.height)))
                out = convert(img, (tw, th), colors=32, outline=True, alpha_cut=0.45)
            save_png(out, os.path.join(PIXEL, 'Vivos', sub, fn))
            count += 1
    print('Paisagem viva convertida:', count, 'arquivos; sem tamanho conhecido (usado o padrão):', sorted(set(missing)))


# ---------------------------------------------------------------- the composer (mirrors FishingScene.BuildPixel)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new('RGBA', (w, h), (0, 0, 0, 255))

    def paste(self, sprite, left, top, tint=None, alpha=1.0, flip_x=False, flip_y=False):
        if sprite is None:
            return
        s = sprite
        if flip_x:
            s = s.transpose(Image.FLIP_LEFT_RIGHT)
        if flip_y:
            s = s.transpose(Image.FLIP_TOP_BOTTOM)
        if tint is not None or alpha < 1.0:
            a = np.array(s).astype(np.float32)
            if tint is not None:
                a[..., :3] *= np.array(tint, np.float32) / 255.0
            a[..., 3] *= alpha
            s = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')
        layer = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        layer.paste(s, (int(left), int(top)))
        self.img.alpha_composite(layer)

    def dot(self, x, y, rgb, alpha=1.0):
        if 0 <= x < self.w and 0 <= y < self.h:
            base = self.img.getpixel((x, y))
            c = tuple(int(round(base[i] * (1 - alpha) + rgb[i] * alpha)) for i in range(3))
            self.img.putpixel((x, y), c + (255,))

    def line(self, x0, y0, x1, y1, rgb, alpha=1.0):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.dot(x0, y0, rgb, alpha)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy


def sky_water_image(m, width):
    """The sky and water drawn by the code (PixelSky.cs): bands with one checker row at each change."""
    img = Image.new('RGBA', (width, GRID_H), (0, 0, 0, 255))
    px = img.load()
    rows = []
    # one checker row at each change of the sky; the water changes clean (the glints already break it up)
    for colors, counts, dither in ((m['sky'], band_rows(HORIZON_ROW, SKY_WEIGHTS), True), (m['water'], band_rows(GRID_H - HORIZON_ROW, WATER_WEIGHTS), False)):
        for i, (c, n) in enumerate(zip(colors, counts)):
            for k in range(n):
                nxt = colors[i + 1] if (dither and i + 1 < len(colors)) else None
                rows.append((hexrgb(c), hexrgb(nxt) if (nxt and k == n - 1) else None))
    for y, (c, nxt) in enumerate(rows):
        for x in range(width):
            px[x, y] = (nxt if (nxt and (x + y) % 2 == 0) else c) + (255,)
    return img


def stars_and_moon(cv, m, cam_x):
    rnd = random.Random(m['id'])
    for _ in range(m.get('stars', 0)):
        x = rnd.randrange(cv.w)
        y = rnd.randrange(int(HORIZON_ROW * 0.75))
        cv.dot(x, y, hexrgb(m.get('moon_color', '#FFFFFF')), rnd.choice([0.5, 0.75, 1.0]))
    moon = m.get('moon')
    if moon:
        cx = cv.w // 2 + int(round((moon[0] - cam_x) * PPU))
        cy = GRID_H // 2 - int(round(moon[1] * PPU))
        r = moon[2]
        col = hexrgb(m['moon_color'])
        for y in range(-r, r + 1):
            for x in range(-r, r + 1):
                if x * x + y * y <= r * r + r * 0.6:
                    cv.dot(cx + x, cy + y, col)


def clouds(cv, m):
    rnd = random.Random(m['id'] + 'nuvens')
    n = m['clouds']
    for i in range(n):
        idx = (i * 2 + (1 if m['id'] > 'map_05' else 0)) % 6 + 1
        p = pixel_path('Mapas/Comum/nuvem_%02d' % idx)
        if not os.path.exists(p):
            continue
        c = load(p)
        x = int(cv.w * (i + 0.5) / n + rnd.randint(-30, 30) - c.width / 2)
        y = rnd.randint(8, 52)
        cv.paste(c, x, y, tint=hexrgb(m['cloud']), alpha=m['cloud_alpha'])


def glints(cv, m, t=0):
    rnd = random.Random(m['id'] + 'brilhos' + str(t))
    col = hexrgb(m['glint'])
    for _ in range(40):
        y = rnd.randint(HORIZON_ROW + 3, GRID_H - 4)
        depth = (y - HORIZON_ROW) / float(GRID_H - HORIZON_ROW)
        length = 1 + int(round(depth * 5))
        x = rnd.randrange(cv.w)
        alpha = 0.35 + 0.35 * depth
        for k in range(length):
            cv.dot(x + k, y, col, alpha)


def band(m, part):
    p = pixel_path('Mapas/%s/%s_%s' % (m['folder'], m['prefix'], part))
    return load(p) if os.path.exists(p) else None


def compose_scene(m, width=384, frame='parado_1', rod_deg=27, bobber=None, fish=None, glint_t=0, living=True):
    cfg = json.load(open(JSON_OUT, encoding='utf-8'))
    cv = Canvas(width, GRID_H)
    cv.img.alpha_composite(sky_water_image(m, width))
    stars_and_moon(cv, m, 0)
    clouds(cv, m)
    cx = width // 2
    hz = HORIZON_ROW

    def place_band(img, dy, reflect):
        if img is None:
            return
        bottom = hz - dy
        top = bottom - img.height
        left = cx - img.width // 2
        for k in (-1, 0, 1):
            # mirrored copies on the sides for wide windows
            cv.paste(img, left + k * img.width, top, flip_x=(k != 0))
        if reflect:
            r = hexrgb(m['reflect'])
            for k in (-1, 0, 1):
                cv.paste(img, left + k * img.width, bottom, tint=r, alpha=m['reflect_alpha'], flip_x=(k != 0), flip_y=True)

    rb = m.get('reflect_bands', True)
    place_band(band(m, 'bg_far'), m['far_dy'], rb)
    place_band(band(m, 'bg_mid'), m['mid_dy'], rb)
    place_band(band(m, 'bg_near'), m['near_dy'], rb)
    # side banks pinned to the edges
    for part, left_side in (('near_left', True), ('near_right', False)):
        img = band(m, part)
        if img is None:
            continue
        bottom = hz - m['side_dy']
        x = 0 if left_side else width - img.width
        cv.paste(img, x, bottom - img.height)
        if m.get('reflect_bands', True):
            cv.paste(img, x, bottom, tint=hexrgb(m['reflect']), alpha=m['reflect_alpha'], flip_y=True)
    if living:
        living_plants(cv, m, ('shore', 'water'))
    glints(cv, m, glint_t)

    # boat, box, fisherman, front of the hull, rod, line, bobber
    boat = next(b for b in cfg['boats'] if b['id'] == cfg['starter_boat'])
    hull = load(pixel_path(boat['art']))
    front = load(pixel_path(boat['front']))
    bx = cx + int(round(BOAT_AT[0] * PPU))
    by = GRID_H // 2 - int(round(BOAT_AT[1] * PPU))
    left, top = bx - boat['pivot_x'], by - boat['pivot_y']
    cv.paste(hull, left, top)
    box = pixel_path('Cena/caixa_de_pesca')
    if os.path.exists(box):
        b = load(box)
        cv.paste(b, left + cfg['box_x'] - b.width // 2, top + cfg['box_y'] - b.height)
    fr = load(pixel_path('Cena/pescador_' + frame if frame != 'pescador' else 'Cena/pescador'))
    fx, fy = left + boat['seat_x'] - FEET[0], top + boat['seat_y'] - FEET[1]
    cv.paste(fr, fx, fy)
    cv.paste(front, left, top)
    hand = dict((n, h) for n, h in FRAMES)[frame]
    hx, hy = fx + hand[0], fy + hand[1]
    rods = {r['id']: r for r in cfg['rods']}
    rod = rods.get('rod_00_starter', {'shaft': '#6B4A2B', 'grip': '#3A2A1C'})
    ang = math.radians(rod_deg)
    tx, ty = hx + math.cos(ang) * ROD_PX, hy - math.sin(ang) * ROD_PX
    gx, gy = hx - math.cos(ang) * 6, hy + math.sin(ang) * 6
    cv.line(gx, gy, hx, hy, hexrgb(rod['grip']))
    cv.line(hx, hy, tx, ty, hexrgb(rod['shaft']))
    if bobber is None:
        bobber = (cx + int(round(WATER_TARGET[0] * PPU)), GRID_H // 2 - int(round(WATER_TARGET[1] * PPU)))
    # line with a little sag
    bxw, byw = bobber
    prev = (tx, ty)
    for i in range(1, 19):
        t = i / 18.0
        mx, my = (tx + bxw) / 2, (ty + byw) / 2 + 9
        p = ((1 - t) ** 2 * tx + 2 * (1 - t) * t * mx + t * t * bxw, (1 - t) ** 2 * ty + 2 * (1 - t) * t * my + t * t * byw)
        cv.line(prev[0], prev[1], p[0], p[1], (232, 232, 224), 0.75)
        prev = p
    if fish is None:
        boia = load(pixel_path('Cena/boia'))
        cv.paste(boia, bxw - boia.width // 2, byw - cfg['bobber_waterline'])
    else:
        f = load(pixel_path('Peixes/fish_%s_master_p' % fish))
        cv.paste(f, bxw - f.width // 2, byw - f.height // 2)
    if living:
        living_plants(cv, m, ('corner_back',))
    # foreground corners
    for part, left_side in (('fg_left', True), ('fg_right', False)):
        img = band(m, part)
        if img is None:
            continue
        cv.paste(img, 0 if left_side else width - img.width, GRID_H - img.height)
    if living:
        living_plants(cv, m, ('corner_front',))
    return cv.img


def living_plants(cv, m, layers):
    d = json.load(open(os.path.join(VISUAL, 'paisagem_viva.json'), encoding='utf-8'))
    scene = next((s for s in d['scenes'] if s['scene'] == m['folder']), None)
    if not scene:
        return
    half = cv.w / 2.0 / PPU
    for p in scene.get('plants', []):
        if p['layer'] not in layers:
            continue
        path = os.path.join(PIXEL, 'Vivos', 'Plantas', p['art'] + '.png')
        if not os.path.exists(path):
            continue
        img = load(path)
        x = {'left': -half + p['x'], 'right': half - p['x']}.get(p['side'], p['x'])
        col = cv.w // 2 + int(round(x * PPU))
        row = GRID_H // 2 - int(round(p['y'] * PPU))
        floating = p['layer'] == 'water'
        top = row - (img.height // 2 if floating else img.height)
        tint = (230, 230, 242) if p['layer'] == 'shore' else None
        cv.paste(img, col - img.width // 2, top, tint=tint)


def thumbnails():
    for m in MAPS:
        if band(m, 'bg_far') is None:
            continue
        img = compose_scene(m, living=False)
        # 2.5:1 like the old thumbnails, centred a little above the horizon
        w, h = 240, 96
        left = (img.width - w) // 2
        top = HORIZON_ROW - 52
        save_png(img.crop((left, top, left + w, top + h)).convert('RGBA'), pixel_path('Mapas/%s/%s_thumb' % (m['folder'], m['prefix'])))
    print('Miniaturas gravadas')


def preview(out_dir, ids):
    os.makedirs(out_dir, exist_ok=True)
    chosen = [m for m in MAPS if m['id'] in ids] if ids else MAPS
    for m in chosen:
        img = compose_scene(m)
        big = img.resize((img.width * 5, img.height * 5), Image.NEAREST)
        big.convert('RGB').save(os.path.join(out_dir, 'cena_%s_%s_1920x1080.png' % (m['id'], m['folder'])))
        img.convert('RGB').save(os.path.join(out_dir, 'cena_%s_%s_grade_384x216.png' % (m['id'], m['folder'])))
    gif(out_dir)


def gif(out_dir):
    """The fisherman changing animation with the phase (FishermanRig in pixel mode), cropped around the boat, 4×."""
    m = MAPS[0]
    cx = 192
    target = (cx + int(round(WATER_TARGET[0] * PPU)), 108 - int(round(WATER_TARGET[1] * PPU)))
    seq = []

    def add(frame, rod, n, ms, bobber=None, fish=None, label=''):
        for i in range(n):
            seq.append((frame(i) if callable(frame) else frame, rod(i) if callable(rod) else rod, ms,
                        bobber(i) if callable(bobber) else bobber, fish))

    # Parado (4 quadros, 4 por segundo)
    add(lambda i: 'parado_%d' % (i % 4 + 1), 58, 8, 250, bobber=(target[0] - 40, target[1] - 26))
    # Arremesso (4 quadros, 10 por segundo, uma vez) — vara para trás e para a frente
    add(lambda i: 'arremesso_%d' % (i + 1), lambda i: [90, 105, 50, 30][i], 4, 100, bobber=lambda i: (target[0] - 40 + i * 10, target[1] - 26 - [0, 8, 14, 6][i]))
    # Esperando (parado em loop)
    add(lambda i: 'parado_%d' % (i % 4 + 1), 27, 8, 250, bobber=lambda i: (target[0], target[1] + (i % 2)))
    # Fisgada (2 quadros, 10 por segundo)
    add(lambda i: 'fisgada_%d' % (i % 2 + 1), lambda i: 25 - (i % 2) * 3, 6, 100, bobber=lambda i: (target[0], target[1] + 2 + (i % 2)))
    # Recolhendo (4 quadros, 8 por segundo)
    add(lambda i: 'recolhe_%d' % (i % 4 + 1), lambda i: 26 + i * 4, 8, 125, bobber=lambda i: (target[0] - i * 6, target[1] - i))
    # Mostrando o peixe
    add('mostra_1', 70, 8, 250, bobber=(cx - 4, 124), fish='tucunare')
    # Esperando de novo, depois o peixe escapa
    add(lambda i: 'parado_%d' % (i % 4 + 1), 27, 4, 250, bobber=(target[0], target[1]))
    add('fisgada_2', lambda i: 20 + (i % 2) * 6, 4, 100, bobber=lambda i: (target[0], target[1] + 3))
    add('escapou_1', 62, 8, 250, bobber=(target[0] - 16, target[1] - 10))

    frames, durations = [], []
    for k, (frame, rod, ms, bobber, fish) in enumerate(seq):
        img = compose_scene(m, frame=frame, rod_deg=rod, bobber=bobber, fish=fish, glint_t=k // 3)
        crop = img.crop((96, 60, 320, 196))
        frames.append(crop.resize((crop.width * 4, crop.height * 4), Image.NEAREST).convert('RGB'))
        durations.append(ms)
    frames[0].save(os.path.join(out_dir, 'pescador_animacoes.gif'), save_all=True, append_images=frames[1:],
                   duration=durations, loop=0, optimize=False)
    print('GIF gravado:', len(frames), 'quadros')


# ----------------------------------------------------------------


def main(argv):
    if not argv:
        print(__doc__)
        return 1
    cmd = argv[0]
    if cmd in ('dados', 'tudo'):
        data()
    if cmd in ('simbolos', 'tudo'):
        symbols()
    if cmd in ('provisorios', 'tudo'):
        provisional_pieces()
    if cmd in ('vivos', 'tudo'):
        vivos()
    if cmd in ('miniaturas', 'tudo'):
        thumbnails()
    if cmd == 'previa':
        preview(argv[1], argv[2:])
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
