"""Turns the owner's "paisagem viva" art (Vivo_XX_*.png, the second ChatGPT document) into game files.

Usage:  python3 tools/Arte/processar_vivos.py <folder with Vivo_XX_*.png>

Each sheet has a flat green or magenta background and several drawings. The script keys the
background out, finds every drawing (loose bits such as a cattail's seed head join the nearest
drawing), and writes them to Resources/Arte/Vivos:

  Plantas/<name>_NN.png   one file per plant, trimmed, base at the bottom edge
  Animais/<name>_N.png    the frames of one animal, all the same size and lined up on one anchor
                          (the beak for birds in flight, the feet for animals standing), so a frame
                          swap never makes the body jump

Every file written is added to tools/Arte/finais.txt, so the placeholder generators never overwrite it.
The source pictures are not stored in the repository (they are large); keep them with the owner.
"""
import glob
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

import processar_pedidos as P

OUT = 'Vivos'

# Longest side, in pixels, of each kind of file (the game shows them at 50–250 px on a 1080p screen).
PLANT_MAX = 420
ANIMAL_MAX = 300

# sheet number → (kind, name, anchor). Plants are listed in reading order (rows top to bottom).
# Animals: a list of frame groups; each group is (file name, [indices in reading order], anchor).
PLANTS = {
    '01': 'pinheiro',
    '02': 'arvore',
    '03': 'junco',
    '04': 'vitoria',
    '05': 'tropical',
    '06': 'folhagem',
}
ANIMALS = {
    '07': [('garca_voo', [0, 1, 2, 3], 'beak')],
    '08': [('pato_voo', [0, 1, 2, 3], 'beak')],
    '09': [('martim_voo', [0, 1], 'beak'), ('martim_pouso', [2], 'feet'), ('martim_mergulho', [3], 'center')],
    '10': [('andorinha', [0, 1, 2], 'beak')],
    '11': [('arara', [0, 1, 2, 3], 'beak'), ('tucano', [4, 5, 6, 7], 'beak')],
    '12': [('garca', [0, 1, 2], 'feet')],
    '13': [('capivara', [0, 1], 'hind'), ('capivara_filhote', [2], 'feet')],
    '14': [('tartaruga', [0, 1, 2], 'hind')],
    '15': [('sapo', [0, 1], 'feet'), ('sapo_pulo', [2], 'center')],
    '16': [('pato', [0, 1, 2], 'hind')],
    '17': [('jacare_nadando', [0], 'feet'), ('jacare_margem', [1], 'feet')],
    '18': [('macaco', [0], 'feet'), ('macaco_pendurado', [1], 'top'), ('bugio', [2], 'feet')],
}

# Drawings that came with a painted water band under them: cut at that fraction of the height
# (the game's own water is underneath), with a soft edge.
WATERLINE = {'jacare_nadando': 0.6}

# Drawings on a branch where a piece of the branch must stay in the scene after the animal leaves:
# the visible brown wood below this fraction of the height is also written on its own
# (<name>_galho.png, same size and place, drawn under the animal).
BRANCHES = {'martim_pouso': 0.68}

written = []


def split_branch(img, fraction):
    import colorsys
    arr = np.asarray(img).astype(np.float32) / 255.0
    h, w = arr.shape[:2]
    hsv = np.array([colorsys.rgb_to_hsv(*px) for px in arr[..., :3].reshape(-1, 3)]).reshape(h, w, 3)
    wood = (hsv[..., 0] > 0.04) & (hsv[..., 0] < 0.15) & (hsv[..., 1] > 0.2) & (hsv[..., 1] < 0.8) & (hsv[..., 2] < 0.86)
    wood &= np.arange(h)[:, None] >= int(h * fraction)
    wood &= arr[..., 3] > 0.05
    wood = ndimage.binary_opening(wood, iterations=1)
    labels, count = ndimage.label(wood)
    if count:
        sizes = ndimage.sum(wood, labels, range(1, count + 1))
        wood = labels == (int(np.argmax(sizes)) + 1)
        wood = ndimage.binary_closing(wood, iterations=2)
    soft = ndimage.gaussian_filter(wood.astype(np.float32), 0.7)
    bird = arr.copy()
    bird[..., 3] *= 1 - soft
    branch = arr.copy()
    branch[..., 3] *= soft
    to_img = lambda a: Image.fromarray((a.clip(0, 1) * 255).astype(np.uint8), 'RGBA')
    return to_img(bird), to_img(branch)


def cut_waterline(img, fraction):
    arr = np.asarray(img).astype(np.float32)
    h = arr.shape[0]
    cut = int(h * fraction)
    fade = max(2, int(h * 0.06))
    ramp = np.ones(h, dtype=np.float32)
    ramp[cut:] = 0
    ramp[cut - fade:cut] = np.linspace(1, 0, fade)
    arr[..., 3] *= ramp[:, None]
    out = Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA')
    return out.crop((0, 0, out.width, cut))


def sheet(folder, number):
    hits = sorted(glob.glob(os.path.join(folder, 'Vivo_%s_*.png' % number)))
    if not hits:
        raise SystemExit('Faltou a imagem Vivo %s na pasta.' % number)
    img = Image.open(hits[0]).convert('RGB')
    key = P.GREEN if img.getpixel((3, 3))[1] > 200 else P.MAGENTA
    return P.key_out(img, key)


def pieces(rgba):
    """The drawings of a sheet in reading order, each as a full-sheet RGBA with only that drawing."""
    arr = np.asarray(rgba)
    solid = arr[..., 3] > 40
    labels, count = ndimage.label(solid, structure=np.ones((3, 3)))
    sizes = ndimage.sum(solid, labels, range(1, count + 1))
    boxes = ndimage.find_objects(labels)
    # A drawing is big next to the others; loose bits (a seed head, a leaf tip) are much smaller.
    candidates = [i for i in range(count) if sizes[i] > 1500]
    typical = float(np.median([sizes[i] for i in candidates]))
    big = [i for i in candidates if sizes[i] > 0.25 * typical]
    owner = {}
    for i in range(count):
        if i in big or sizes[i] < 12:
            continue
        # A loose bit joins the big drawing whose box is closest.
        by, bx = boxes[i]
        cy, cx = (by.start + by.stop) / 2, (bx.start + bx.stop) / 2

        def gap(j):
            jy, jx = boxes[j]
            dx = max(jx.start - cx, 0, cx - jx.stop)
            dy = max(jy.start - cy, 0, cy - jy.stop)
            return dx * dx + dy * dy
        best = min(big, key=gap)
        if gap(best) < 120 ** 2:
            owner[i] = best

    rows_h = rgba.height / 2
    items = []
    for j in big:
        by, bx = boxes[j]
        items.append((j, (by.start + by.stop) / 2, (bx.start + bx.stop) / 2))
    # Reading order: by row band (the sheets have 1 or 2 rows), then left to right.
    two_rows = max(c[1] for c in items) - min(c[1] for c in items) > rows_h * 0.6
    items.sort(key=lambda c: ((c[1] > rows_h) if two_rows else 0, c[2]))

    out = []
    for j, _, _ in items:
        members = [j] + [i for i, o in owner.items() if o == j]
        mask = np.isin(labels, [m + 1 for m in members])
        mask = ndimage.binary_dilation(mask, iterations=2)
        a = arr.copy()
        a[..., 3] = np.where(mask, a[..., 3], 0)
        out.append(Image.fromarray(a, 'RGBA'))
    return out


def bbox(img, threshold=24):
    a = np.asarray(img)[..., 3]
    ys, xs = np.where(a > threshold)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def anchor(img, kind):
    """The point that must stay still between frames, in sheet pixels."""
    a = np.asarray(img)[..., 3] > 60
    ys, xs = np.where(a)
    x0, y0, x1, y1 = xs.min(), ys.min(), xs.max(), ys.max()
    if kind == 'beak':
        # The tip of the beak: the rightmost solid pixels.
        tip = xs >= x1 - 3
        return float(x1), float(ys[tip].mean())
    if kind == 'feet':
        low = ys >= y1 - max(3, (y1 - y0) * 0.06)
        return float(xs[low].mean()), float(y1)
    if kind == 'hind':
        # Animals on the water line or walking: the back end and the base stay put.
        return float(x0), float(y1)
    if kind == 'top':
        high = ys <= y0 + max(3, (y1 - y0) * 0.06)
        return float(xs[high].mean()), float(y0)
    return (x0 + x1) / 2.0, (y0 + y1) / 2.0


def save(img, rel):
    P.save(img, os.path.join(OUT, rel))
    written.append(os.path.join(OUT, rel).replace(os.sep, '/'))


def shrink(img, longest):
    s = min(1.0, longest / max(img.size))
    if s < 1.0:
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    return img


def plants(folder):
    for number, name in PLANTS.items():
        for n, piece in enumerate(pieces(sheet(folder, number)), start=1):
            save(shrink(piece.crop(bbox(piece, 12)), PLANT_MAX), 'Plantas/%s_%02d.png' % (name, n))


def animals(folder):
    for number, groups in ANIMALS.items():
        found = pieces(sheet(folder, number))
        for name, indices, kind in groups:
            frames = [found[i] for i in indices]
            anchors = [anchor(f, kind) for f in frames]
            boxes = [bbox(f, 12) for f in frames]
            # One canvas for all frames, every frame placed so its anchor lands on the same point.
            left = max(ax - b[0] for (ax, _), b in zip(anchors, boxes))
            right = max(b[2] - ax for (ax, _), b in zip(anchors, boxes))
            up = max(ay - b[1] for (_, ay), b in zip(anchors, boxes))
            down = max(b[3] - ay for (_, ay), b in zip(anchors, boxes))
            w, h = int(np.ceil(left + right)), int(np.ceil(up + down))
            s = min(1.0, ANIMAL_MAX / max(w, h))
            for n, (frame, (ax, ay), b) in enumerate(zip(frames, anchors, boxes), start=1):
                canvas = Image.new('RGBA', (w, h), (0, 0, 0, 0))
                part = frame.crop(b)
                canvas.alpha_composite(part, (int(round(left - (ax - b[0]))), int(round(up - (ay - b[1])))))
                if s < 1.0:
                    canvas = canvas.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
                if name in WATERLINE:
                    canvas = cut_waterline(canvas, WATERLINE[name])
                if name in BRANCHES:
                    _, branch = split_branch(canvas, BRANCHES[name])
                    save(branch, 'Animais/%s_galho.png' % name)
                rel = 'Animais/%s_%d.png' % (name, n) if len(frames) > 1 else 'Animais/%s.png' % name
                save(canvas, rel)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    folder = sys.argv[1]
    plants(folder)
    animals(folder)
    old = set()
    if os.path.exists(P.FINALS):
        old = {l.strip() for l in open(P.FINALS, encoding='utf-8') if l.strip() and not l.startswith('#')}
    with open(P.FINALS, 'w', encoding='utf-8') as f:
        f.write('# Arquivos de arte finais (feitos pelo proprietário). Os geradores de arte provisória não os sobrescrevem.\n')
        for rel in sorted(r for r in old | set(written) if os.path.exists(os.path.join(P.ART, r))):
            f.write(rel + '\n')
    print('Paisagem viva gravada:', len(written), 'arquivos.')


if __name__ == '__main__':
    main()
