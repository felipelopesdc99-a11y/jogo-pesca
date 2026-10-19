"""Cuts the owner's pixel-art sheets (made with ChatGPT, Estilo ARC) into clean game files.

Uso (mensagens em PT-BR, porque o proprietário roda no terminal):

  python3 tools/Arte/pixel_folhas.py FOLHA.png --pedido L4-01
      usa o pedido do manifesto tools/Arte/pixel_pedidos.json (grade, tamanho da célula, nomes,
      pasta, contorno, âncora) e grava em client-unity/Assets/Resources/ArtePixel/<pasta>/

  python3 tools/Arte/pixel_folhas.py FOLHA.png --grade 3x2 --celula 128x128 \\
          --nomes a,b,c,d,e,f --saida /tmp/saida [--cores 32] [--contorno auto|sim|nao]
          [--ancora centro|base] [--tamanho-fixo] [--margem 2] [--fator 8] [--metade]

What it does, in order (docs/ART_BIBLE_PIXEL.md, section 12; TD-059):
  1. background out: real alpha, flat magenta #FF00FF, or a flat/checkerboard border colour
     flood-filled from the edges;
  2. detects the up-scaling factor (ChatGPT never delivers the exact size asked): edges of colour
     change are histogrammed modulo k and the largest k that still lines up with them wins; the
     size asked in the request is the fallback;
  3. splits the sheet into the grid cells (equal split), finds the drawing in each cell;
  4. samples one colour per grid pixel (the most common colour in the centre of each block),
     snapping to the sheet's own phase, so drifted ChatGPT grids still land on whole pixels;
  5. one shared palette for the whole sheet (frames and series stay consistent), binary alpha,
     lone pixels removed;
  6. 1 px outline pulled towards the colour it touches, only where the drawing lacks one;
  7. trims to the drawing + margin (or keeps the cell size, aligned on an anchor, for animation
     frames and icons), and writes PNGs with the names of the request, in order.

Nothing here touches game rules: it only produces picture files.
"""
import argparse
import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
MANIFEST = os.path.join(os.path.dirname(__file__), 'pixel_pedidos.json')
OUT_ROOT = os.path.join(ROOT, 'client-unity', 'Assets', 'Resources', 'ArtePixel')


# ---------------------------------------------------------------- background

def _is_magenta(rgb):
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    return (r > 170) & (b > 170) & (g < 110) & (np.abs(r - b) < 90)


def remove_background(img):
    """RGBA uint8 array → (rgb, opaque mask)."""
    a = np.asarray(img.convert('RGBA'))
    rgb, alpha = a[..., :3], a[..., 3]
    if (alpha < 128).mean() > 0.02:
        return rgb, (alpha >= 128) & ~_is_magenta(rgb)
    mag = _is_magenta(rgb)
    border = np.concatenate([mag[0], mag[-1], mag[:, 0], mag[:, -1]])
    if border.mean() > 0.5:
        return rgb, ~mag
    # Flat or checkerboard "fake transparency": flood from the edges through the border colours.
    edge = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]]).reshape(-1, 3).astype(int)
    keys, counts = np.unique((edge // 8), axis=0, return_counts=True)
    top = keys[np.argsort(-counts)[:2]] * 8 + 4
    close = np.zeros(rgb.shape[:2], bool)
    for c in top:
        close |= (np.abs(rgb.astype(int) - c).sum(axis=2) < 40)
    lab, _ = ndimage.label(close)
    touching = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(touching))
    return rgb, ~bg


# ---------------------------------------------------------------- scale

def _edges(rgb, mask):
    d = rgb.astype(int)
    hx = (np.abs(np.diff(d, axis=1)).sum(axis=2) > 48) & mask[:, 1:] & mask[:, :-1]
    vy = (np.abs(np.diff(d, axis=0)).sum(axis=2) > 48) & mask[1:] & mask[:-1]
    xs = np.nonzero(hx)[1] + 1
    ys = np.nonzero(vy)[0] + 1
    return xs, ys


def _coherence(pos, periods):
    """Phase coherence of edge positions for each candidate period (1 = all edges on one grid)."""
    if len(pos) < 40:
        return np.zeros(len(periods)), np.zeros(len(periods))
    if len(pos) > 12000:
        pos = np.random.default_rng(1).choice(pos, 12000, replace=False)
    ang = 2 * np.pi * pos[None, :] / periods[:, None]
    z = np.exp(1j * ang).mean(axis=1)
    return np.abs(z), np.angle(z)


PERIODS = np.arange(2.0, 40.0, 0.02)


def _pick(score, expected):
    best = score.max() if len(score) else 0
    if best < 0.3:
        return (float(expected) if expected else 1.0), 0.0
    k = float(PERIODS[score >= best * 0.8].max())
    # ChatGPT often draws chunkier pixels than asked, so a confident detection wins over the request;
    # the asked size only decides when the edges are too noisy to trust.
    if expected and best < 0.5:
        near = np.abs(PERIODS - expected) <= max(0.3, expected * 0.06)
        if near.any():
            k = float(PERIODS[near][np.argmax(score[near])])
    return k, float(score[np.argmin(np.abs(PERIODS - k))])


def detect_factor(rgb, mask, expected=None):
    """Up-scaling factor per axis (real: ChatGPT rarely keeps a whole number) = the largest period
    whose grid the colour edges line up with. Square pixels are assumed unless the two axes clearly
    disagree (a stretched picture)."""
    xs, ys = _edges(rgb, mask)
    cx, _ = _coherence(xs, PERIODS)
    cy, _ = _coherence(ys, PERIODS)
    kx, sx = _pick(cx, expected)
    ky, sy = _pick(cy, expected)
    if sx == 0.0 and sy > 0:
        kx = ky
    if sy == 0.0 and sx > 0:
        ky = kx
    if abs(kx - ky) / max(kx, ky) < 0.15:
        kx = ky = (kx + ky) / 2
    return (kx, ky), (sx + sy) / 2


def phase(rgb, mask, k):
    kx, ky = k
    xs, ys = _edges(rgb, mask)
    _, ax = _coherence(xs, np.array([kx]))
    _, ay = _coherence(ys, np.array([ky]))
    ox = (ax[0] / (2 * np.pi) * kx) % kx if len(xs) >= 40 else 0.0
    oy = (ay[0] / (2 * np.pi) * ky) % ky if len(ys) >= 40 else 0.0
    return ox, oy


# ---------------------------------------------------------------- sampling

def downsample(rgb, mask, k, ox, oy):
    """One colour per grid pixel (k = (kx, ky), may be fractional): mode of the central half of each block."""
    kx, ky = k
    h, w = mask.shape
    x0 = ox - kx if ox > 0.5 else ox
    y0 = oy - ky if oy > 0.5 else oy
    nw = int(np.ceil((w - x0) / kx))
    nh = int(np.ceil((h - y0) / ky))
    out = np.zeros((nh, nw, 3), np.uint8)
    op = np.zeros((nh, nw), bool)
    qx, qy = kx / 4, ky / 4
    for j in range(nh):
        ya = y0 + j * ky
        yb, ye = max(0, int(round(ya + qy))), min(h, int(round(ya + ky - qy)))
        if ye <= yb:
            continue
        for i in range(nw):
            xa = x0 + i * kx
            xb, xe = max(0, int(round(xa + qx))), min(w, int(round(xa + kx - qx)))
            if xe <= xb:
                continue
            m = mask[yb:ye, xb:xe]
            if m.mean() < 0.5:
                continue
            px = rgb[yb:ye, xb:xe][m].astype(int)
            keyed = (px[:, 0] // 6) * 4096 + (px[:, 1] // 6) * 64 + (px[:, 2] // 6)
            vals, cnt = np.unique(keyed, return_counts=True)
            sel = px[keyed == vals[np.argmax(cnt)]]
            out[j, i] = sel.mean(axis=0).round().astype(np.uint8)
            op[j, i] = True
    return out, op


# ---------------------------------------------------------------- palette, cleanup, outline

def shared_palette(cells, colors):
    pix = np.concatenate([c[0][c[1]] for c in cells if c[1].any()]) if cells else np.zeros((0, 3), np.uint8)
    if len(pix) == 0:
        return cells
    strip = Image.fromarray(pix.reshape(1, -1, 3))
    pal_img = strip.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    out = []
    for rgb, op in cells:
        q = Image.fromarray(rgb).quantize(palette=pal_img, dither=Image.Dither.NONE).convert('RGB')
        out.append((np.asarray(q).copy(), op))
    return out


def drop_lone_pixels(op):
    n = ndimage.convolve(op.astype(int), np.ones((3, 3), int), mode='constant') - op
    return op & (n > 0)


def _luma(c):
    c = np.asarray(c, float)
    return 0.299 * c[..., 0] + 0.587 * c[..., 1] + 0.114 * c[..., 2]


def has_outline(rgb, op):
    if not op.any():
        return True
    edge = op & ~ndimage.binary_erosion(op, border_value=0)
    inner = op & ~edge
    if edge.sum() < 8 or not inner.any():
        return True
    return (_luma(rgb[edge]) < _luma(rgb[inner]).mean() * 0.55).mean() > 0.6


def add_outline(rgb, op):
    """Pads by 1 and draws a 1 px outline outside the shape, darkened from the colour it touches."""
    h, w = op.shape
    R = np.zeros((h + 2, w + 2, 3), np.uint8)
    O = np.zeros((h + 2, w + 2), bool)
    R[1:-1, 1:-1], O[1:-1, 1:-1] = rgb, op
    ring = ndimage.binary_dilation(O, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]]) & ~O
    for y, x in zip(*np.nonzero(ring)):
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < h + 2 and 0 <= xx < w + 2 and O[yy, xx]:
                c = R[yy, xx].astype(float)
                R[y, x] = np.clip(c * 0.32 + np.array([10, 8, 22]), 0, 255).astype(np.uint8)
                break
    return R, O | ring


def halve(rgb, op):
    """2:1 reduction for the small variant (mode of each 2×2 block; transparent when ≥3 are)."""
    h, w = op.shape
    nh, nw = (h + 1) // 2, (w + 1) // 2
    R = np.zeros((nh, nw, 3), np.uint8)
    O = np.zeros((nh, nw), bool)
    for j in range(nh):
        for i in range(nw):
            m = op[2 * j:2 * j + 2, 2 * i:2 * i + 2]
            if m.sum() < 2:
                continue
            px = rgb[2 * j:2 * j + 2, 2 * i:2 * i + 2][m]
            dark = px[_luma(px) < 60]
            R[j, i] = dark[0] if len(dark) >= 2 else px[np.argmax([(px == p).all(axis=1).sum() for p in px])]
            O[j, i] = True
    return R, O


# ---------------------------------------------------------------- placing

def trim(rgb, op, margin):
    ys, xs = np.nonzero(op)
    if len(xs) == 0:
        return rgb[:1, :1], op[:1, :1]
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    R = np.zeros((y1 - y0 + 2 * margin, x1 - x0 + 2 * margin, 3), np.uint8)
    O = np.zeros(R.shape[:2], bool)
    R[margin:margin + y1 - y0, margin:margin + x1 - x0] = rgb[y0:y1, x0:x1]
    O[margin:margin + y1 - y0, margin:margin + x1 - x0] = op[y0:y1, x0:x1]
    return R, O


def place(rgb, op, size, anchor):
    """Puts the drawing on a fixed canvas: 'centro' centres the box; 'base' sits the bottom on the
    last row (minus 1) and centres on the lower third's mass, so animation frames do not jump."""
    W, H = size
    ys, xs = np.nonzero(op)
    R = np.zeros((H, W, 3), np.uint8)
    O = np.zeros((H, W), bool)
    if len(xs) == 0:
        return R, O
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    if anchor == 'base':
        low = ys >= y1 - max(1, (y1 - y0) // 3)
        cx = xs[low].mean()
        dx = int(round(W / 2 - cx))
        dy = (H - 1) - y1
    else:
        dx = int(round((W - (x1 - x0)) / 2)) - x0
        dy = int(round((H - (y1 - y0)) / 2)) - y0
    for y, x in zip(ys, xs):
        yy, xx = y + dy, x + dx
        if 0 <= yy < H and 0 <= xx < W:
            R[yy, xx] = rgb[y, x]
            O[yy, xx] = True
    if O.sum() < len(xs):
        print(f'  aviso: o desenho não coube em {W}×{H}; parte ficou de fora')
    return R, O


def to_image(rgb, op):
    a = np.dstack([rgb, (op * 255).astype(np.uint8)])
    a[~op] = 0
    return Image.fromarray(a, 'RGBA')


# ---------------------------------------------------------------- main

def process(path, cols, rows, cell, names, out_dir, colors=32, outline='auto', anchor='centro',
            fixed=False, margin=2, factor=None, half=False, opaque=False):
    img = Image.open(path)
    rgb, mask = remove_background(img)
    if opaque:
        # full scenes: everything but stray magenta (separator lines between cells) is picture
        mask = ~_is_magenta(rgb)
    H, W = mask.shape
    cw, ch = W / cols, H / rows
    expected = None
    if cell:
        expected = min(cw / cell[0], ch / cell[1])
    k, conf = ((float(factor), float(factor)), 1.0) if factor else detect_factor(rgb, mask, expected)
    fk = (f'{k[0]:.2f}' if k[0] == k[1] else f'{k[0]:.2f} × {k[1]:.2f}').replace('.', ',')
    fc = f'{conf:.2f}'.replace('.', ',')
    print(f'{os.path.basename(path)}: {W}×{H}, grade {cols}×{rows}, fator {fk} (confiança {fc})')
    cells = []
    for j in range(rows):
        for i in range(cols):
            xa, xb = int(round(i * cw)), int(round((i + 1) * cw))
            ya, yb = int(round(j * ch)), int(round((j + 1) * ch))
            r, m = rgb[ya:yb, xa:xb], mask[ya:yb, xa:xb]
            ox, oy = phase(r, m, k)
            cells.append(downsample(r, m, k, ox, oy))
    cells = shared_palette(cells, colors)
    os.makedirs(out_dir, exist_ok=True)
    written = []
    for idx, (r, op) in enumerate(cells):
        if idx >= len(names) or not names[idx] or names[idx] == '-':
            continue
        if not opaque:
            op = drop_lone_pixels(op)
            # keep only blobs of a meaningful size (noise and stray marks out)
            lab, n = ndimage.label(op, structure=np.ones((3, 3)))
            if n > 1:
                sizes = ndimage.sum(op, lab, range(1, n + 1))
                keep = [i + 1 for i, s in enumerate(sizes) if s >= max(4, 0.02 * sizes.max())]
                op = np.isin(lab, keep)
        if not op.any():
            print(f'  aviso: célula {idx + 1} ({names[idx]}) está vazia')
            continue
        if not opaque and (outline == 'sim' or (outline == 'auto' and not has_outline(r, op))):
            r, op = add_outline(r, op)
        if fixed and cell:
            r, op = place(r, op, cell, anchor)
        else:
            r, op = trim(r, op, margin)
        name = names[idx]
        target = os.path.join(out_dir, name + '.png')
        os.makedirs(os.path.dirname(target), exist_ok=True)
        to_image(r, op).save(target, optimize=True)
        written.append(target)
        if half:
            hr, ho = halve(r, op)
            hr, ho = trim(hr, ho, 1)
            to_image(hr, ho).save(os.path.join(out_dir, name + '_p.png'), optimize=True)
        print(f'  {name}.png  {op.shape[1]}×{op.shape[0]}')
    return written, k


def _grid(s):
    a, b = s.lower().split('x')
    return int(a), int(b)


def main(argv=None):
    ap = argparse.ArgumentParser(description='Recorta e limpa folhas de pixel art do ChatGPT (Estilo ARC).')
    ap.add_argument('folha', help='imagem da folha (PNG)')
    ap.add_argument('--pedido', help='código do pedido no manifesto (ex.: L4-01)')
    ap.add_argument('--grade', help='colunas x linhas (ex.: 3x2)')
    ap.add_argument('--celula', help='tamanho de cada célula em pixels da grade (ex.: 128x128)')
    ap.add_argument('--nomes', help='nomes dos arquivos, em ordem de leitura, separados por vírgula ("-" pula)')
    ap.add_argument('--saida', help='pasta de saída')
    ap.add_argument('--cores', type=int, default=None)
    ap.add_argument('--contorno', choices=['auto', 'sim', 'nao'], default=None)
    ap.add_argument('--ancora', choices=['centro', 'base'], default=None)
    ap.add_argument('--tamanho-fixo', action='store_true')
    ap.add_argument('--margem', type=int, default=None)
    ap.add_argument('--fator', type=float, default=None, help='força o fator de ampliação')
    ap.add_argument('--metade', action='store_true', help='grava também a versão pequena _p (2:1)')
    a = ap.parse_args(argv)

    cfg = {}
    if a.pedido:
        with open(MANIFEST, encoding='utf-8') as f:
            pedidos = {p['id']: p for p in json.load(f)['pedidos']}
        if a.pedido not in pedidos:
            sys.exit(f'Pedido {a.pedido} não está em {MANIFEST}.')
        cfg = pedidos[a.pedido]
    cols, rows = _grid(a.grade or cfg.get('grade', '1x1'))
    cell = _grid(a.celula or cfg['celula']) if (a.celula or cfg.get('celula')) else None
    names = (a.nomes.split(',') if a.nomes else cfg.get('nomes', []))
    out = a.saida or os.path.join(OUT_ROOT, cfg.get('pasta', ''))
    if not names:
        sys.exit('Faltam os nomes dos arquivos (--nomes ou --pedido).')
    process(a.folha, cols, rows, cell, names, out,
            colors=a.cores or cfg.get('cores', 32),
            outline=a.contorno or cfg.get('contorno', 'auto'),
            anchor=a.ancora or cfg.get('ancora', 'centro'),
            fixed=a.tamanho_fixo or cfg.get('tamanho_fixo', False),
            margin=a.margem if a.margem is not None else cfg.get('margem', 2), factor=a.fator, half=a.metade or cfg.get('metade', False),
            opaque=cfg.get('opaco', False))
    print('Pronto.')


if __name__ == '__main__':
    main()
