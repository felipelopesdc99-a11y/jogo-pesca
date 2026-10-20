"""Cuts the owner's pixel-art sheets (made with ChatGPT, Estilo ARC) into clean game files.

Uso (mensagens em PT-BR, porque o proprietário roda no terminal):

  python3 tools/Arte/pixel_folhas.py FOLHA.png --pedido L4-01a
      usa o pedido do manifesto tools/Arte/pixel_pedidos.json (grade, célula, nomes, modo, alvo,
      contorno, cores) e grava em client-unity/Assets/Resources/ArtePixel/<nome>.png

  python3 tools/Arte/pixel_folhas.py FOLHA.png --pedido L4-02a --pular Peixes/fish_jau_master
      "--pular" deixa de fora desenhos que vieram ruins (para pedir de novo)

  python3 tools/Arte/pixel_folhas.py FOLHA.png --grade 3x2 --celula 128x128 \\
          --nomes a,b,c,d,e,f --saida /tmp/saida [--modo objetos] [--alvo 112x100] [--cores 32]

Why not a fixed grid (TD-060): ChatGPT never splits the sheet into equal cells nor keeps a perfect
pixel grid. So the drawings are found as connected shapes on the magenta background, the size of the
drawing decides the scale, and every output pixel is the most common colour of its block.

Modes (field "modo" of each request):
  objetos  each drawing on its own: shapes on the background, small bits (whiskers, splashes, stars)
           joined to the nearest drawing, ordered row by row, checked against the number of names.
           "alvo" is the box each drawing must fit (or {name: box}); "escala" says whether each
           drawing fits on its own ("objeto"), the whole sheet shares one scale ("folha") or each
           row does ("linha"). "tamanho_fixo" puts it on a canvas of the cell size ("ancora").
  quadros  animation frames: one scale and one palette for the whole sheet, every frame on the same
           canvas with the feet on the same row and the body on the same column, nothing cut.
  faixas   scenery strips: one strip per row band, all at the width "largura" (384), kept whole.
  cena     a full opaque scene reduced to the native size (no cutting into shapes).
  paineis  opaque panels split by the magenta lines between them, each reduced to the cell size.

Every sprite: magenta and pink halo out, one palette (≤ "cores") per drawing (per sheet for
animation frames), mode sampling with a bias that keeps dark outlines, stray pixels removed, 1 px
holes filled, 1 px outline only where the drawing lacks one, binary alpha.

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
EIGHT = np.ones((3, 3), bool)


def _fmt(x):
    return f'{x:.2f}'.replace('.', ',')


# ---------------------------------------------------------------- background

def _pink(rgb):
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    return np.minimum(r, b) - g, np.abs(r - b)


def load(path):
    """→ rgb (uint8), foreground mask, vote mask (pixels trusted for colour: not the pink halo)."""
    img = Image.open(path)
    a = np.asarray(img.convert('RGBA'))
    rgb = a[..., :3].copy()
    pink, rb = _pink(rgb)
    bg = (pink > 120) & (rb < 80)
    if (a[..., 3] < 128).mean() > 0.02:
        bg |= a[..., 3] < 128
    fg = ~bg
    near_bg = ndimage.binary_dilation(bg, iterations=3)
    halo = near_bg & (pink > 35) & (rb < 90)
    return rgb, fg, fg & ~halo


def _luma(c):
    c = np.asarray(c, float)
    return 0.299 * c[..., 0] + 0.587 * c[..., 1] + 0.114 * c[..., 2]


# ---------------------------------------------------------------- finding the drawings

class Obj:
    def __init__(self, mask, y0, x0):
        self.mask, self.y0, self.x0 = mask, y0, x0     # mask is the bbox crop

    @property
    def h(self):
        return self.mask.shape[0]

    @property
    def w(self):
        return self.mask.shape[1]

    @property
    def cy(self):
        return self.y0 + self.h / 2

    @property
    def cx(self):
        return self.x0 + self.w / 2

    def merge(self, o):
        y0, x0 = min(self.y0, o.y0), min(self.x0, o.x0)
        y1, x1 = max(self.y0 + self.h, o.y0 + o.h), max(self.x0 + self.w, o.x0 + o.w)
        m = np.zeros((y1 - y0, x1 - x0), bool)
        m[self.y0 - y0:self.y0 - y0 + self.h, self.x0 - x0:self.x0 - x0 + self.w] |= self.mask
        m[o.y0 - y0:o.y0 - y0 + o.h, o.x0 - x0:o.x0 - x0 + o.w] |= o.mask
        return Obj(m, y0, x0)

    def gap(self, o):
        dx = max(0, max(self.x0, o.x0) - min(self.x0 + self.w, o.x0 + o.w))
        dy = max(0, max(self.y0, o.y0) - min(self.y0 + self.h, o.y0 + o.h))
        return float(np.hypot(dx, dy))


def _objects_from_labels(lab, n, keep):
    out = []
    sl = ndimage.find_objects(lab)
    for i in keep:
        s = sl[i - 1]
        out.append(Obj(lab[s] == i, s[0].start, s[1].start))
    return out


def find_objects(fg, expected, cols, rows, log):
    H, W = fg.shape
    lab, n = ndimage.label(fg, structure=EIGHT)
    if n == 0:
        return []
    sizes = ndimage.sum(fg, lab, range(1, n + 1))
    main = [i + 1 for i, s in enumerate(sizes) if s >= 0.02 * sizes.max()]
    small = [i + 1 for i, s in enumerate(sizes) if s < 0.02 * sizes.max() and s >= 12]
    objs = _objects_from_labels(lab, n, main)
    # small bits (whiskers cut off by the halo, splashes, sparkles) join the nearest drawing when close
    reach = 0.035 * np.hypot(H, W)
    for o in _objects_from_labels(lab, n, small):
        d = [o.gap(m) for m in objs]
        j = int(np.argmin(d))
        if d[j] <= reach:
            objs[j] = objs[j].merge(o)
    # too many: the smallest drawing goes into its nearest neighbour
    while len(objs) > expected:
        areas = [o.mask.sum() for o in objs]
        i = int(np.argmin(areas))
        o = objs.pop(i)
        j = int(np.argmin([o.gap(m) for m in objs]))
        log(f'  junta um pedaço de {o.w}×{o.h} px ao desenho vizinho')
        objs[j] = objs[j].merge(o)
    # too few: drawings that touch are split where they are thinnest, using the grid as a hint
    if len(objs) < expected:
        ys, xs = np.nonzero(fg)
        cw = (xs.max() - xs.min() + 1) / cols
        ch = (ys.max() - ys.min() + 1) / rows
        new = []
        for o in objs:
            px = max(1, int(round(o.w / cw))) if o.w > 1.35 * cw else 1
            py = max(1, int(round(o.h / ch))) if o.h > 1.35 * ch else 1
            if px * py > 1:
                log(f'  desenhos encostados: divide um bloco de {o.w}×{o.h} px em {px}×{py}')
                new.extend(_split(o, px, py))
            else:
                new.append(o)
        objs = new
    return order(objs)


def _cuts(profile, parts):
    """Cut positions (parts-1) at the thinnest column/row near each nominal split."""
    n = len(profile)
    cuts = []
    for p in range(1, parts):
        c = n * p / parts
        a, b = int(c - n / parts * 0.3), int(c + n / parts * 0.3)
        a, b = max(1, a), min(n - 1, b)
        seg = profile[a:b]
        cuts.append(a + int(np.argmin(seg)))
    return [0] + cuts + [n]


def _split(o, px, py):
    out = []
    yc = _cuts(o.mask.sum(axis=1), py)
    for j in range(py):
        band = o.mask[yc[j]:yc[j + 1]]
        xc = _cuts(band.sum(axis=0), px)
        for i in range(px):
            m = band[:, xc[i]:xc[i + 1]]
            if m.any():
                ys, xs = np.nonzero(m)
                out.append(Obj(m[ys.min():ys.max() + 1, xs.min():xs.max() + 1],
                               o.y0 + yc[j] + ys.min(), o.x0 + xc[i] + xs.min()))
    return out


def order(objs):
    """Reading order: rows by vertical centre, then left to right."""
    if not objs:
        return objs
    objs = sorted(objs, key=lambda o: o.cy)
    medh = np.median([o.h for o in objs])
    rows, cur = [], [objs[0]]
    for o in objs[1:]:
        if o.cy - np.mean([c.cy for c in cur]) > 0.5 * medh:
            rows.append(cur)
            cur = [o]
        else:
            cur.append(o)
    rows.append(cur)
    out = []
    for r in rows:
        out.extend(sorted(r, key=lambda o: o.cx))
    for i, o in enumerate(out):
        o.row = next(j for j, r in enumerate(rows) if any(x is o for x in r))
    return out


# ---------------------------------------------------------------- palette and sampling

def make_palette(pixels, colors):
    if len(pixels) > 250000:
        pixels = pixels[np.random.default_rng(1).choice(len(pixels), 250000, replace=False)]
    strip = Image.fromarray(np.ascontiguousarray(pixels.reshape(1, -1, 3)))
    return strip.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, kmeans=2, dither=Image.Dither.NONE)


def palette_rgb(pal_img):
    p = np.array(pal_img.getpalette()[:768], np.uint8).reshape(-1, 3)
    return p


def to_index(rgb_crop, pal_img):
    return np.asarray(Image.fromarray(np.ascontiguousarray(rgb_crop)).quantize(
        palette=pal_img, dither=Image.Dither.NONE)).astype(np.int64)


def sample(idx, fgm, vote, ox, oy, k, W, H, pal, theta=0.34, cover=0.5):
    """One palette colour per output pixel. Source pixel (x, y) of the crop falls in block
    ((x - ox) / k, (y - oy) / k). Colour = most voted palette entry (centre of the block counts
    double); when dark pixels make up ≥ theta of the block, the most voted dark entry wins, which
    keeps the 1 px outlines and inner lines. Opaque when the drawing covers ≥ cover of the block
    (thin dark lines from 30%)."""
    h, w = idx.shape
    P = len(pal)
    gx = (np.arange(w) + 0.5 - ox) / k
    gy = (np.arange(h) + 0.5 - oy) / k
    bx = np.floor(gx).astype(int)
    by = np.floor(gy).astype(int)
    fx = np.abs(gx - bx - 0.5) < 0.25
    fy = np.abs(gy - by - 0.5) < 0.25
    okx = (bx >= 0) & (bx < W)
    oky = (by >= 0) & (by < H)
    BX, BY = np.meshgrid(bx, by)
    OK = np.outer(oky, okx)
    blk = (BY * W + BX)[OK]
    cnt = np.bincount(blk, minlength=W * H).astype(float)
    cov = np.bincount(blk, weights=fgm[OK].astype(float), minlength=W * H) / np.maximum(cnt, 1)
    wgt = (1.0 + np.outer(fy, fx))[OK] * vote[OK]
    key = blk * P + idx[OK]
    hist = np.bincount(key, weights=wgt, minlength=W * H * P).reshape(W * H, P)
    tot = hist.sum(axis=1)
    dark = _luma(pal) < 58
    dsum = hist[:, dark].sum(axis=1)
    dfrac = dsum / np.maximum(tot, 1e-9)
    pick = np.argmax(hist, axis=1)
    if dark.any():
        hd = np.where(dark[None, :], hist, -1)
        pick = np.where(dfrac >= theta, np.argmax(hd, axis=1), pick)
    alpha = ((cov >= cover) | ((cov >= 0.3) & (dfrac >= 0.5))) & (tot > 0)
    out = pal[pick].reshape(H, W, 3)
    return out, alpha.reshape(H, W)


# ---------------------------------------------------------------- cleanup and outline

def clean(rgb, op, passes=2):
    rgb = rgb.copy()
    op = op.copy()
    for _ in range(passes):
        # pink halo on the border
        edge = op & ~ndimage.binary_erosion(op, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]], border_value=0)
        pink, rb = _pink(rgb)
        op &= ~(edge & (pink > 45) & (rb < 70))
        # lone pixels
        n = ndimage.convolve(op.astype(int), np.ones((3, 3), int), mode='constant') - op
        op &= ~(op & (n == 0))
    # 1 px holes
    n4 = ndimage.convolve(op.astype(int), np.array([[0, 1, 0], [1, 0, 1], [0, 1, 0]]), mode='constant')
    holes = ~op & (n4 == 4)
    for y, x in zip(*np.nonzero(holes)):
        nb = [rgb[y + dy, x + dx] for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0))]
        vals, cnt = np.unique(np.array(nb), axis=0, return_counts=True)
        rgb[y, x] = vals[np.argmax(cnt)]
        op[y, x] = True
    # tiny islands of 1–2 pixels far from the drawing
    lab, n = ndimage.label(op, structure=EIGHT)
    if n > 1:
        sizes = ndimage.sum(op, lab, range(1, n + 1))
        op &= np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s >= 3])
    return rgb, op


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


def limit_colors(rgb, op, colors):
    px = rgb[op]
    if len(px) == 0 or len(np.unique(px, axis=0)) <= colors:
        return rgb
    pal = make_palette(px, colors)
    q = np.asarray(Image.fromarray(rgb).quantize(palette=pal, dither=Image.Dither.NONE).convert('RGB')).copy()
    return np.where(op[..., None], q, rgb)


def halve(rgb, op):
    """2:1 reduction for the small variant (mode of each 2×2 block, dark wins on ties)."""
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
    """Fixed canvas: 'centro' centres the drawing; 'base' sits it on the last row but one."""
    W, H = size
    rgb, op = trim(rgb, op, 0)
    h, w = op.shape
    R = np.zeros((H, W, 3), np.uint8)
    O = np.zeros((H, W), bool)
    dx = (W - w) // 2
    dy = (H - 1 - h) if anchor == 'base' else (H - h) // 2
    ys, xs = np.nonzero(op)
    yy, xx = ys + dy, xs + dx
    ok = (yy >= 0) & (yy < H) & (xx >= 0) & (xx < W)
    R[yy[ok], xx[ok]] = rgb[ys[ok], xs[ok]]
    O[yy[ok], xx[ok]] = True
    return R, O, int((~ok).sum())


def to_image(rgb, op):
    a = np.dstack([rgb, (op * 255).astype(np.uint8)])
    a[~op] = 0
    return Image.fromarray(a, 'RGBA')


# ---------------------------------------------------------------- one drawing

def render(rgb, fgm, vote, o, k, colors, pal_img=None, pad=None, size=None, origin=None, theta=0.34):
    """Samples one object: o (in sheet coordinates) at scale k. Returns rgb, opaque."""
    Hs, Ws = fgm.shape
    if origin is None:
        W = int(np.ceil(o.w / k)) + 2
        H = int(np.ceil(o.h / k)) + 2
        ox_s = o.x0 + o.w / 2 - W * k / 2
        oy_s = o.y0 + o.h / 2 - H * k / 2
    else:
        (W, H), (ox_s, oy_s) = size, origin
    xa, ya = int(np.floor(max(0, ox_s))), int(np.floor(max(0, oy_s)))
    xb, yb = int(np.ceil(min(Ws, ox_s + W * k))), int(np.ceil(min(Hs, oy_s + H * k)))
    crop = rgb[ya:yb, xa:xb]
    om = np.zeros((yb - ya, xb - xa), bool)
    # only this object's pixels count
    sy0, sx0 = o.y0 - ya, o.x0 - xa
    ty0, tx0 = max(0, sy0), max(0, sx0)
    ty1, tx1 = min(om.shape[0], sy0 + o.h), min(om.shape[1], sx0 + o.w)
    om[ty0:ty1, tx0:tx1] = o.mask[ty0 - sy0:ty1 - sy0, tx0 - sx0:tx1 - sx0]
    om &= fgm[ya:yb, xa:xb]
    vm = om & vote[ya:yb, xa:xb]
    if pal_img is None:
        pal_img = make_palette(crop[vm], colors)
    pal = palette_rgb(pal_img)
    idx = to_index(crop, pal_img)
    idx = np.minimum(idx, len(pal) - 1)
    out, op = sample(idx, om, vm, ox_s - xa, oy_s - ya, k, W, H, pal, theta=theta)
    return out, op


def finish(r, op, outline, colors, keep_size=False):
    r, op = clean(r, op)
    if outline == 'sim' or (outline == 'auto' and not has_outline(r, op)):
        r, op = add_outline(r, op)
        if keep_size:
            r, op = r[1:-1, 1:-1], op[1:-1, 1:-1]
    r = limit_colors(r, op, colors)
    return r, op


def _box(s):
    a, b = str(s).lower().split('x')
    return float(a), float(b)


# ---------------------------------------------------------------- modes

def process(path, cols, rows, cell, names, out_dir, colors=32, outline='auto', anchor='centro',
            fixed=False, margin=2, half=False, mode='objetos', alvo=None, escala='objeto',
            alvo_altura=48, largura=384, ancora_x=None, skip=(), log=print):
    rgb, fg, vote = load(path)
    Hs, Ws = fg.shape
    wanted = [n for n in names]
    expected = len([n for n in wanted if n and n != '-'])
    log(f'{os.path.basename(path)}: {Ws}×{Hs}, modo {mode}, {expected} desenhos esperados')
    written = []

    def save(name, r, op):
        if name in skip:
            log(f'  {name}: deixado de fora (pedir de novo)')
            return
        target = os.path.join(out_dir, name + '.png')
        os.makedirs(os.path.dirname(target), exist_ok=True)
        to_image(r, op).save(target, optimize=True)
        written.append(target)
        if half:
            hr, ho = halve(r, op)
            hr, ho = trim(hr, ho, 1)
            to_image(hr, ho).save(os.path.join(out_dir, name + '_p.png'), optimize=True)
        log(f'  {name}.png  {op.shape[1]}×{op.shape[0]}')

    if mode == 'cena':
        W, H = cell
        k = min(Ws / W, Hs / H)
        ox, oy = (Ws - W * k) / 2, (Hs - H * k) / 2
        pal_img = make_palette(rgb.reshape(-1, 3), colors)
        pal = palette_rgb(pal_img)
        idx = np.minimum(to_index(rgb, pal_img), len(pal) - 1)
        allm = np.ones_like(fg)
        r, op = sample(idx, allm, allm, ox, oy, k, W, H, pal, theta=0.5)
        log(f'  escala {_fmt(k)}')
        save(names[0], r, np.ones((H, W), bool))
        return written

    if mode == 'paineis':
        W, H = cell
        pink, rb = _pink(rgb)
        sep = (pink > 80) & (rb < 90)
        ch, cw = Hs / rows, Ws / cols
        n = 0
        for j in range(rows):
            for i in range(cols):
                ya, yb, xa, xb = int(j * ch), int((j + 1) * ch), int(i * cw), int((i + 1) * cw)
                s = sep[ya:yb, xa:xb]
                rr = np.nonzero(s.mean(axis=1) < 0.3)[0]
                cc = np.nonzero(s.mean(axis=0) < 0.3)[0]
                ya2, yb2, xa2, xb2 = ya + rr.min(), ya + rr.max() + 1, xa + cc.min(), xa + cc.max() + 1
                crop = rgb[ya2:yb2, xa2:xb2]
                k = min(crop.shape[1] / W, crop.shape[0] / H)
                ox, oy = (crop.shape[1] - W * k) / 2, (crop.shape[0] - H * k) / 2
                pal_img = make_palette(crop.reshape(-1, 3), colors)
                pal = palette_rgb(pal_img)
                idx = np.minimum(to_index(crop, pal_img), len(pal) - 1)
                allm = np.ones(crop.shape[:2], bool)
                r, _ = sample(idx, allm, allm, ox, oy, k, W, H, pal, theta=0.5)
                if n < len(names) and names[n] != '-':
                    save(names[n], r, np.ones((H, W), bool))
                n += 1
        return written

    if mode == 'faixas':
        # row bands separated by empty rows; one common scale from the widest band
        prof = fg.sum(axis=1) > max(3, 0.004 * Ws)
        lab, nb = ndimage.label(prof)
        bands = [(s[0].start, s[0].stop) for s in ndimage.find_objects(lab)]
        bands = [b for b in bands if b[1] - b[0] > 0.02 * Hs]
        while len(bands) > expected:      # join the thinnest band to its neighbour
            i = int(np.argmin([b[1] - b[0] for b in bands]))
            j = i - 1 if i > 0 and (i == len(bands) - 1 or bands[i][0] - bands[i - 1][1] < bands[i + 1][0] - bands[i][1]) else i + 1
            a, b = sorted([i, j])
            bands[a:b + 1] = [(bands[a][0], bands[b][1])]
        cols_on = np.nonzero(fg.sum(axis=0) > 0)[0]
        x0, x1 = cols_on.min(), cols_on.max() + 1
        k = (x1 - x0) / largura
        log(f'  {len(bands)} faixas, escala {_fmt(k)}')
        for n, (ya, yb) in enumerate(bands):
            if n >= len(names) or names[n] == '-':
                continue
            m = np.zeros((yb - ya, x1 - x0), bool)
            m[:] = fg[ya:yb, x0:x1]
            o = Obj(m, ya, x0)
            H = int(np.ceil((yb - ya) / k))
            r, op = render(rgb, fg, vote, o, k, colors, size=(largura, H), origin=(x0, ya), theta=0.45)
            r, op = clean(r, op)
            r, op = trim(r, op, 0)
            # keep the full width (the strip tiles / its ends sit on the screen edges)
            if op.shape[1] < largura:
                R = np.zeros((op.shape[0], largura, 3), np.uint8)
                O = np.zeros((op.shape[0], largura), bool)
                ys, xs = np.nonzero(op)
                R[ys, xs], O[ys, xs] = r[ys, xs], True
                r, op = R, O
            save(names[n], r, op)
        return written

    objs = find_objects(fg, expected, cols, rows, log)
    if len(objs) != expected:
        log(f'  aviso: achei {len(objs)} desenhos, o pedido tem {expected}; confira a ordem')
    real = [n for n in wanted if n and n != '-']

    if mode == 'quadros':
        W, H = cell
        pal_img = make_palette(np.concatenate([rgb[o.y0:o.y0 + o.h, o.x0:o.x0 + o.w][o.mask & vote[o.y0:o.y0 + o.h, o.x0:o.x0 + o.w]] for o in objs]), colors)
        # anchor: bottom row of the drawing, x = centre of the lowest fifth (feet / seat)
        anc = []
        for o in objs:
            ys, xs = np.nonzero(o.mask)
            low = ys >= o.h - max(2, o.h // 5)
            anc.append((o.x0 + xs[low].mean(), o.y0 + o.h))
        k = objs[0].h / alvo_altura
        L = max(ax - o.x0 for o, (ax, ay) in zip(objs, anc))
        R_ = max(o.x0 + o.w - ax for o, (ax, ay) in zip(objs, anc))
        T = max(ay - o.y0 for o, (ax, ay) in zip(objs, anc))
        if ancora_x:
            # the same column on every sheet of the character, so switching animations never jumps
            ax_out = float(ancora_x)
            k = max(k, L / (ax_out - 1), R_ / (W - 1 - ax_out), T / (H - 2))
        else:
            k = max(k, (L + R_) / (W - 2), T / (H - 2))
            ax_out = (W - (L + R_) / k) / 2 + L / k
        ay_out = H - 1
        log(f'  escala {_fmt(k)} (figura de {objs[0].h / k:.0f} px de altura), âncora em x={ax_out:.1f}')
        for o, (ax, ay), name in zip(objs, anc, real):
            r, op = render(rgb, fg, vote, o, k, colors, pal_img=pal_img, size=(W, H),
                           origin=(ax - ax_out * k, ay - ay_out * k))
            r, op = finish(r, op, outline, colors, keep_size=True)
            save(name, r, op)
        return written

    # objetos
    boxes = []
    for o, name in zip(objs, real):
        a = alvo.get(name) if isinstance(alvo, dict) else alvo
        boxes.append(_box(a) if a else (cell[0] - 2, cell[1] - 2))
    ks = [max(o.w / bw, o.h / bh) for o, (bw, bh) in zip(objs, boxes)]
    if escala == 'folha':
        ks = [max(ks)] * len(ks)
    elif escala == 'linha':
        for rr in set(o.row for o in objs):
            m = max(k for k, o in zip(ks, objs) if o.row == rr)
            ks = [m if o.row == rr else k for k, o in zip(ks, objs)]
    for o, k, name in zip(objs, ks, real):
        r, op = render(rgb, fg, vote, o, k, colors)
        r, op = finish(r, op, outline, colors)
        if fixed and cell:
            r, op, lost = place(r, op, cell, anchor)
            if lost:
                log(f'  aviso: {name} não coube em {cell[0]}×{cell[1]} ({lost} px de fora)')
        else:
            r, op = trim(r, op, margin)
        save(name, r, op)
    return written


def _grid(s):
    a, b = s.lower().split('x')
    return int(a), int(b)


def run_request(cfg, folha, out_root=OUT_ROOT, skip=(), log=print):
    cols, rows = _grid(cfg.get('grade', '1x1'))
    cell = _grid(cfg['celula']) if cfg.get('celula') else None
    return process(folha, cols, rows, cell, cfg['nomes'], os.path.join(out_root, cfg.get('pasta', '')),
                   colors=cfg.get('cores', 32), outline=cfg.get('contorno', 'auto'),
                   anchor=cfg.get('ancora', 'centro'), fixed=cfg.get('tamanho_fixo', False),
                   margin=cfg.get('margem', 2), half=cfg.get('metade', False),
                   mode=cfg.get('modo', 'cena' if cfg.get('opaco') else 'objetos'),
                   alvo=cfg.get('alvo'), escala=cfg.get('escala', 'objeto'),
                   alvo_altura=cfg.get('alvo_altura', 48), largura=cfg.get('largura', 384),
                   ancora_x=cfg.get('ancora_x'),
                   skip=set(skip), log=log)


def main(argv=None):
    ap = argparse.ArgumentParser(description='Recorta e limpa folhas de pixel art do ChatGPT (Estilo ARC).')
    ap.add_argument('folha', help='imagem da folha (PNG)')
    ap.add_argument('--pedido', help='código do pedido no manifesto (ex.: L4-01a)')
    ap.add_argument('--grade', help='colunas x linhas (ex.: 3x2)')
    ap.add_argument('--celula', help='tamanho de cada célula em pixels da grade (ex.: 128x128)')
    ap.add_argument('--nomes', help='nomes dos arquivos, em ordem de leitura, separados por vírgula ("-" pula)')
    ap.add_argument('--saida', help='pasta de saída (padrão: Resources/ArtePixel)')
    ap.add_argument('--modo', choices=['objetos', 'quadros', 'faixas', 'cena', 'paineis'])
    ap.add_argument('--alvo', help='caixa em que cada desenho tem de caber (ex.: 112x100)')
    ap.add_argument('--cores', type=int)
    ap.add_argument('--contorno', choices=['auto', 'sim', 'nao'])
    ap.add_argument('--pular', default='', help='nomes que ficam de fora, separados por vírgula')
    a = ap.parse_args(argv)

    cfg = {}
    if a.pedido:
        with open(MANIFEST, encoding='utf-8') as f:
            pedidos = {p['id']: p for p in json.load(f)['pedidos']}
        if a.pedido not in pedidos:
            sys.exit(f'Pedido {a.pedido} não está em {MANIFEST}.')
        cfg = dict(pedidos[a.pedido])
    for key, val in (('grade', a.grade), ('celula', a.celula), ('modo', a.modo), ('alvo', a.alvo),
                     ('cores', a.cores), ('contorno', a.contorno)):
        if val:
            cfg[key] = val
    if a.nomes:
        cfg['nomes'] = a.nomes.split(',')
    if not cfg.get('nomes'):
        sys.exit('Faltam os nomes dos arquivos (--nomes ou --pedido).')
    out_root = a.saida or OUT_ROOT
    run_request(cfg, a.folha, out_root=out_root, skip=[s for s in a.pular.split(',') if s])
    print('Pronto.')


if __name__ == '__main__':
    main()
