"""Turns the owner's final art (the ChatGPT requests in the "Pedidos de arte" document) into game files.

Usage:  python3 tools/Arte/processar_pedidos.py <folder with Pedido_XX_*.png>

What it does, per request (numbers as in the document):
  1, 17        map photos for the Map screen (thumb)
  2, 18        sky: the painting placed so its sun sits where the scene expects it, widened to the
               26 units of the layer by continuing its edge colours
  3, 4, 19, 20 far mountains / hills: background keyed out, trimmed, and continued sideways with
               mirrored copies of a strip without landmarks (the waterfall appears once)
  5, 6, 21, 22 the two shore groups, keyed out and trimmed (the game pins them to the screen edges)
  7, 8, 22A/B  foreground corners
  9, 25        three clouds split from one picture
  10-12, 23-24 fish (grids cut cell by cell, magenta keyed out)
  13-16        boat, fisherman, portrait, tackle box
  26, 27       icons (white on black) and the coloured coin and shell
  28           the two rods
  29-32        expedition pictures

Every file written is listed in tools/Arte/finais.txt, so the placeholder generators never overwrite it.
The source pictures are not stored in the repository (they are large); keep them with the owner.
"""
import glob
import math
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(__file__)
ART = os.path.join(HERE, '..', '..', 'client-unity', 'Assets', 'Resources', 'Arte')
FINALS = os.path.join(HERE, 'finais.txt')
HORIZON = 0.2
HALF_W = 13.0
GREEN, MAGENTA = (0, 255, 0), (255, 0, 255)
written = []


# ----------------------------------------------------------------------------- helpers

def src(folder, number):
    hits = sorted(glob.glob(os.path.join(folder, 'Pedido_%s_*.png' % number)))
    if not hits:
        raise SystemExit('Faltou o pedido %s na pasta.' % number)
    return Image.open(hits[0])


def save(img, rel):
    path = os.path.join(ART, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)
    written.append(rel)
    print('  ', rel, '%dx%d' % img.size)


def key_out(img, key, lo=0.20, hi=0.42):
    """Removes a flat background colour: soft edge, colour spill taken off the rim, 1 px shaved."""
    rgb = np.asarray(img.convert('RGB'), dtype=np.float32) / 255.0
    k = np.array(key, dtype=np.float32) / 255.0
    dist = np.sqrt(((rgb - k) ** 2).sum(axis=2))
    alpha = np.clip((dist - lo) / (hi - lo), 0, 1)
    a3 = np.maximum(alpha[..., None], 1e-3)
    clean = np.clip((rgb - k * (1 - a3)) / a3, 0, 1)
    rgb = np.where(alpha[..., None] < 0.999, clean, rgb)
    # Leftover spill: pull a pure key tint towards grey on the edge pixels.
    if key == GREEN:
        g_excess = np.clip(rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2]) - 0.15, 0, 1)
        rgb[..., 1] -= g_excess * (1 - alpha) * 0.9
    elif key == MAGENTA:
        m_excess = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - 0.25, 0, 1)
        rgb[..., 0] -= m_excess * (1 - alpha) * 0.9
        rgb[..., 2] -= m_excess * (1 - alpha) * 0.9
    out = Image.fromarray((np.dstack([np.clip(rgb, 0, 1), alpha]) * 255).astype(np.uint8), 'RGBA')
    out.putalpha(out.split()[3].filter(ImageFilter.MinFilter(3)))
    return out


def main_piece(img):
    """Keeps the drawing that belongs to this cell: the largest shape and the pieces near it that
    do not touch the cell border (bits of a neighbour's fin crossing into the cell are dropped)."""
    from scipy import ndimage
    rgba = np.asarray(img).copy()
    solid = rgba[..., 3] > 24
    labels, count = ndimage.label(solid, structure=np.ones((3, 3)))
    if count <= 1:
        return img
    sizes = ndimage.sum(solid, labels, range(1, count + 1))
    biggest = int(np.argmax(sizes)) + 1
    keep = np.zeros(count + 1, dtype=bool)
    keep[biggest] = True
    h, w = solid.shape
    for i, box in enumerate(ndimage.find_objects(labels), start=1):
        if i == biggest or box is None:
            continue
        touches = box[0].start == 0 or box[1].start == 0 or box[0].stop == h or box[1].stop == w
        if not touches and sizes[i - 1] > 0.002 * sizes[biggest - 1]:
            keep[i] = True
    mask = keep[labels]
    # Keep the soft edge around what stays.
    mask = ndimage.binary_dilation(mask, iterations=2)
    rgba[..., 3] = np.where(mask, rgba[..., 3], 0)
    return Image.fromarray(rgba, 'RGBA')


def trim(img, threshold=12):
    a = np.asarray(img)[..., 3]
    ys, xs = np.where(a > threshold)
    return img.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def fit(img, size, margin):
    piece = trim(img)
    tw, th = size
    s = min(tw * (1 - margin) / piece.width, th * (1 - margin) / piece.height)
    piece = piece.resize((max(1, round(piece.width * s)), max(1, round(piece.height * s))), Image.LANCZOS)
    canvas = Image.new('RGBA', size, (0, 0, 0, 0))
    canvas.alpha_composite(piece, ((tw - piece.width) // 2, (th - piece.height) // 2))
    return canvas


def cells(img, cols, rows):
    w, h = img.size
    for r in range(rows):
        for c in range(cols):
            yield img.crop((int(c * w / cols), int(r * h / rows), int((c + 1) * w / cols), int((r + 1) * h / rows)))


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def extend_sideways(piece, anchor_frac, anchor_x, left_strip, right_strip, ppu):
    """Continues a trimmed layer piece to cover x in [-13, 13] with mirrored copies of edge strips.

    `anchor_frac` of the piece's width lands on world x `anchor_x`. `left_strip` / `right_strip` are
    (from, to) fractions of the piece used for the continuation (choose parts without landmarks).
    Mirroring keeps every seam continuous.
    """
    total = int(round(2 * HALF_W * ppu))
    canvas = Image.new('RGBA', (total, piece.height), (0, 0, 0, 0))
    x0 = int(round((anchor_x + HALF_W) * ppu - anchor_frac * piece.width))
    canvas.alpha_composite(piece, (max(0, x0), 0), (max(0, -x0), 0))

    def strip(frac):
        return piece.crop((int(frac[0] * piece.width), 0, int(frac[1] * piece.width), piece.height))

    # Leftwards: the strip is taken at the piece's left edge, so mirror it first.
    ls = strip(left_strip)
    x, flip = x0, True
    while x > 0:
        tile = ls.transpose(Image.FLIP_LEFT_RIGHT) if flip else ls
        x -= tile.width
        canvas.alpha_composite(tile, (max(0, x), 0), (max(0, -x), 0))
        flip = not flip
    rs = strip(right_strip)
    x, flip = x0 + piece.width, True
    while x < total:
        tile = rs.transpose(Image.FLIP_LEFT_RIGHT) if flip else rs
        if x >= 0:
            canvas.alpha_composite(tile, (x, 0))
        x += tile.width
        flip = not flip
    return canvas


def layer_piece(img, key, height_units, ppu):
    """Keys out, trims and scales a layer painting to a height in world units."""
    piece = trim(key_out(img, key))
    h = int(round(height_units * ppu))
    return piece.resize((max(1, round(piece.width * h / piece.height)), h), Image.LANCZOS)


# ----------------------------------------------------------------------------- scenery

def sky(img, sun_frac, sun_world, height_units, rel, ppu=96):
    """Places the sky painting so its sun is at `sun_world`, then widens it to 26 x 6 units."""
    W, H = int(2 * HALF_W * ppu), int(6.0 * ppu)
    iw = int(round(img.width * height_units * ppu / img.height))
    ih = int(round(height_units * ppu))
    pic = np.asarray(img.convert('RGB').resize((iw, ih), Image.LANCZOS), dtype=np.float32)
    left = int(round((sun_world[0] + HALF_W) * ppu - sun_frac[0] * iw))
    bottom = sun_world[1] - (1 - sun_frac[1]) * height_units  # world y of the picture's bottom edge
    top_row = int(round((6.0 - (bottom + height_units - (HORIZON - 0.2))) * ppu))  # canvas row of the picture's top
    rows = np.clip(np.arange(H) - top_row, 0, ih - 1)
    band = 2.4 * ppu
    # Colour of the sky continued sideways: per-row median of a wide edge strip, smoothed
    # vertically, so thin clouds at the edge are not smeared into streaks.
    edge = max(8, iw // 5)
    kernel = np.ones(int(0.35 * ppu)) / int(0.35 * ppu)

    def profile(block):
        col = np.median(block, axis=1)
        return np.stack([np.convolve(np.pad(col[:, c], len(kernel), mode='edge'), kernel, mode='same')[len(kernel):-len(kernel)] for c in range(3)], axis=1)

    left_col = profile(pic[:, :edge])
    right_col = profile(pic[:, -edge:])
    out = np.zeros((H, W, 3), dtype=np.float32)
    for x in range(W):
        sx = x - left
        if 0 <= sx < iw:
            inside = min(sx, iw - 1 - sx)
            w = float(smooth(0, band, np.array(inside)))
            fill = left_col if sx < iw / 2 else right_col
            out[:, x] = pic[rows, sx] * w + fill[rows] * (1 - w)
        else:
            out[:, x] = (left_col if sx < 0 else right_col)[rows]
    save(Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), 'RGB').convert('RGBA'), rel)


def clouds(img, prefix):
    """Splits the three clouds where the picture is emptiest between them (they may touch)."""
    rgba = img.convert('RGBA')
    a = np.asarray(rgba)[..., 3].astype(np.float32)
    profile = a.sum(axis=0)
    w = rgba.width
    cuts = [0]
    for lo, hi in ((0.22, 0.45), (0.55, 0.78)):
        i0, i1 = int(lo * w), int(hi * w)
        cuts.append(i0 + int(np.argmin(profile[i0:i1])))
    cuts.append(w)
    for i in range(3):
        part = rgba.crop((cuts[i], 0, cuts[i + 1], rgba.height))
        save(fit(main_piece(part), (640, 320), 0.04), prefix + '_cloud_%02d.png' % (i + 1))


def thumb(img, rel):
    w, h = img.size
    ch = int(w / 2.5)
    y0 = int(h * 0.22)
    save(img.convert('RGBA').crop((0, y0, w, min(h, y0 + ch))).resize((1080, 432), Image.LANCZOS), rel)


def lago(folder):
    base = 'Mapas/LagoSereno/map_lago_sereno_'
    thumb(src(folder, '01'), base + 'thumb.png')
    # The sun of the sky painting is at 78% across, 83% down; the scene's sun is at (5.3, 1.95).
    sky(src(folder, '02'), (0.78, 0.83), (5.3, 1.95), 7.5, base + 'bg_sky.png')
    far = layer_piece(src(folder, '03'), GREEN, 1.75, 120)
    save(extend_sideways(far, 0.55, 3.0, (0.0, 0.5), (0.5, 1.0), 120), base + 'bg_far.png')
    mid = layer_piece(src(folder, '04'), GREEN, 1.05, 130)
    save(extend_sideways(mid, 0.5, 0.0, (0.0, 0.5), (0.5, 1.0), 130), base + 'bg_mid.png')
    save(trim(key_out(src(folder, '05'), GREEN)), base + 'near_left.png')
    save(trim(key_out(src(folder, '06'), GREEN)), base + 'near_right.png')
    save(trim(key_out(src(folder, '07'), GREEN)), base + 'fg_left.png')
    save(trim(key_out(src(folder, '08'), GREEN)), base + 'fg_right.png')
    clouds(src(folder, '09'), base[:-1])


def rio(folder):
    base = 'Mapas/RioSelvagem/map_rio_selvagem_'
    thumb(src(folder, '17'), base + 'thumb.png')
    # Sun at 26% across, 28% down; the scene's sun is at (-4.5, 3.9).
    sky(src(folder, '18'), (0.26, 0.28), (-4.5, 3.9), 7.5, base + 'bg_sky.png')
    # Cliffs: the waterfall is at about 67% across; it must appear once, at x = 3.4.
    far = layer_piece(src(folder, '19'), GREEN, 3.2, 100)
    save(extend_sideways(far, 0.67, 3.4, (0.0, 0.45), (0.82, 1.0), 100), base + 'bg_far.png')
    mid = layer_piece(src(folder, '20'), GREEN, 1.5, 110)
    save(extend_sideways(mid, 0.55, 3.4, (0.0, 0.35), (0.75, 1.0), 110), base + 'bg_mid.png')
    save(trim(key_out(src(folder, '21'), GREEN)), base + 'near_left.png')
    save(trim(key_out(src(folder, '22'), GREEN)), base + 'near_right.png')
    save(trim(key_out(src(folder, '22A'), GREEN)), base + 'fg_left.png')
    save(trim(key_out(src(folder, '22B'), GREEN)), base + 'fg_right.png')
    clouds(src(folder, '25'), base[:-1])


# ----------------------------------------------------------------------------- fish, props, icons

LAGO_GRID_1 = ['lambari', 'tilapia', 'piau', 'cascudo', 'curimbata', 'traira']
LAGO_GRID_2 = ['pacu', 'matrinxa', 'carpa', 'tambaqui']
RIO_GRID_1 = ['piranha', 'piracanjuba', 'peixe_cachorra', 'tucunare', 'cachara', 'dourado']
RIO_GRID_2 = ['pintado', 'jau', 'pirarucu', 'aruana']
ICONS = ['pesca', 'mapa', 'aquario', 'arena', 'expedicao', 'mercado', 'loja', 'perfil', 'avisos', 'opcoes',
         'fechar', 'iniciar', 'caixa', 'vender', 'estrela', 'cadeado']


def fish(folder):
    def grid(number, cols, rows, names):
        for cell, name in zip(cells(key_out(src(folder, number), MAGENTA), cols, rows), names):
            if name != 'lambari':  # the approved single lambari (request 10) is sharper
                save(fit(main_piece(cell), (1024, 512), 0.06), 'Peixes/fish_%s_master.png' % name)

    save(fit(main_piece(key_out(src(folder, '10'), MAGENTA)), (1024, 512), 0.06), 'Peixes/fish_lambari_master.png')
    grid('11', 2, 3, LAGO_GRID_1)
    grid('12', 2, 2, LAGO_GRID_2)
    grid('23', 2, 3, RIO_GRID_1)
    grid('24', 2, 2, RIO_GRID_2)


def props(folder):
    save(trim(key_out(src(folder, '13'), GREEN)), 'Cena/barco.png')
    fisher = trim(key_out(src(folder, '14'), GREEN))
    save(fisher, 'Cena/pescador.png')
    save(fit(key_out(src(folder, '15'), GREEN), (512, 512), 0.0), 'Cena/retrato.png')
    save(fit(key_out(src(folder, '16'), MAGENTA), (320, 220), 0.04), 'Cena/caixa_de_pesca.png')


def icons(folder):
    sheet = src(folder, '26').convert('L')
    for cell, name in zip(cells(sheet, 4, 4), ICONS):
        lum = np.asarray(cell, dtype=np.float32)
        alpha = np.clip((lum - 40) / 175, 0, 1)
        white = np.full(lum.shape, 255, dtype=np.float32)
        img = Image.fromarray(np.dstack([white, white, white, alpha * 255]).astype(np.uint8), 'RGBA')
        out = fit(img, (96, 96), 0.14)
        save(out, 'Icones/ico_%s.png' % name)
        if name == 'aquario':
            save(out, 'Icones/ico_peixe.png')
    coin, shell = cells(key_out(src(folder, '27'), MAGENTA), 2, 1)
    save(fit(main_piece(coin), (128, 128), 0.04), 'Icones/ico_moeda.png')
    save(fit(main_piece(shell), (128, 128), 0.04), 'Icones/ico_concha.png')


def rods(folder):
    starter, rod1 = cells(key_out(src(folder, '28'), MAGENTA), 1, 2)
    save(fit(main_piece(starter), (1024, 512), 0.04), 'Varas/rod_00_starter.png')
    save(fit(main_piece(rod1), (1024, 512), 0.04), 'Varas/rod_01.png')


def expeditions(folder):
    for number, exp_id in (('29', 'exp_30m'), ('30', 'exp_1h'), ('31', 'exp_3h'), ('32', 'exp_6h')):
        save(src(folder, number).convert('RGBA').resize((900, 600), Image.LANCZOS), 'Expedicoes/%s.png' % exp_id)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    folder = sys.argv[1]
    for step in (lago, rio, fish, props, icons, rods, expeditions):
        step(folder)
    old = set()
    if os.path.exists(FINALS):
        old = {l.strip() for l in open(FINALS, encoding='utf-8') if l.strip() and not l.startswith('#')}
    with open(FINALS, 'w', encoding='utf-8') as f:
        f.write('# Arquivos de arte finais (feitos pelo proprietário). Os geradores de arte provisória não os sobrescrevem.\n')
        for rel in sorted(old | set(written)):
            f.write(rel + '\n')
    print('Arte final gravada:', len(written), 'arquivos.')


if __name__ == '__main__':
    main()
