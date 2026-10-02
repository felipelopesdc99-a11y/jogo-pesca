"""Turns the owner's art for maps 3 and 4 (the "Pedidos de arte: Pantanal Dourado e Estuário das Marés"
document) into game files.

Usage:  python3 tools/Arte/processar_mapas_3_4.py <folder> [<newer folder> ...]
        (folders with Pantanal_XX_*.png, Estuario_XX_*.png, Novo_XX_*.png, Refazer_XX_*.png; the newer wins,
        and a broken file is replaced by the copy in an older folder)

What it does, per request:
  Pantanal 01 / Estuário 01   map photo for the Map screen (thumb)
  02                          sky, placed so its sun sits where the scene expects it
  03, 04                      far horizon and middle band, keyed out and continued sideways
  05, 06                      the two shore groups
  07, 08                      foreground corners
  09                          three clouds (blue background)
  10, 11                      fish grids (2 x 3 and 2 x 2), in the order of the document
  Pantanal 12-18, Estuário 12-17   living landscape: animals (frames lined up) and plants
  Novo 01                     Vara 2
  Novo 02                     the six boats (3 x 2)
  Novo 03                     boat and bait icons, turned into white menu glyphs
  Refazer 01-04               two different middle bands per map, laid A, B, A, B (replace request 04)
  Refazer 05, 06              Pantanal 06 (right shore) and 16 (butterflies) redone

Every file written is listed in tools/Arte/finais.txt, so the placeholder generators never overwrite it.
A missing or empty picture is skipped with a warning; the placeholder stays.
"""
import glob
import os
import sys

import numpy as np
from PIL import Image

import processar_pedidos as P
import processar_vivos as V

BLUE = (0, 0, 255)

PANTANAL_GRID_1 = ['mandi_amarelo', 'pacu_peva', 'jundia', 'jurupensem', 'mucum', 'piavucu']
PANTANAL_GRID_2 = ['piraputanga', 'jurupoca', 'armado', 'barbado']
ESTUARIO_GRID_1 = ['parati', 'tainha', 'carapeba', 'corvina', 'bagre_marinho', 'robalo_peva']
ESTUARIO_GRID_2 = ['pescada_amarela', 'xareu', 'camurupim', 'mero']
BOATS = ['boat_00', 'boat_01', 'boat_02', 'boat_03', 'boat_04', 'boat_05']

# (prefix, number) -> plant file name (one file per drawing, reading order).
PLANTS = {
    ('Pantanal', '17'): 'aguape',
    ('Pantanal', '18'): 'carandaa',
    ('Estuario', '16'): 'beira_mangue',
    ('Estuario', '17'): 'mangue',
}
# (prefix, number) -> frame groups: (file name, indices in reading order, anchor).
ANIMALS = {
    ('Pantanal', '12'): [('tuiuiu_voo', [0, 1, 2, 3], 'beak')],
    ('Pantanal', '13'): [('tuiuiu', [0, 1, 2], 'feet')],
    ('Pantanal', '14'): [('arara_azul', [0, 1, 2, 3], 'beak')],
    ('Pantanal', '15'): [('colhereiro_voo', [0, 1, 2, 3], 'beak')],
    ('Estuario', '12'): [('guara_voo', [0, 1, 2, 3], 'beak')],
    ('Estuario', '13'): [('caranguejo', [0, 1, 2], 'feet')],
    ('Estuario', '14'): [('boto', [0, 1, 2], 'center')],
    ('Estuario', '15'): [('trinta_reis', [0, 1, 2, 3], 'beak')],
    ('Pantanal', '16'): [('borboleta_limao', [0, 1, 2], 'center'), ('borboleta_laranja', [3, 4, 5], 'center')],
}

# Plants with no real pink or purple in them: any magenta cast is background that bled in.
NO_MAGENTA = {'carandaa', 'mangue'}

skipped = []


# A redone picture ("Refazer" section of the document) stands in for the original request.
ALTERNATES = {
    ('Pantanal', '06'): ('Refazer', '05'),
    ('Pantanal', '16'): ('Refazer', '06'),
}
# Wide middle bands: two different paintings laid one after the other (Refazer 01-04).
BANDS = {
    'Pantanal': [('Refazer', '01'), ('Refazer', '02')],
    'Estuario': [('Refazer', '03'), ('Refazer', '04')],
}


def find(folders, prefix, number, quiet=False):
    """The picture of a request: the redone version first, then the newest folder that has a readable file."""
    for want in ([ALTERNATES[(prefix, number)]] if (prefix, number) in ALTERNATES else []) + [(prefix, number)]:
        for folder in reversed(folders):
            for path in sorted(glob.glob(os.path.join(folder, '%s_%s_*.png' % want))):
                if os.path.getsize(path) == 0:
                    continue
                try:
                    img = Image.open(path)
                    img.load()
                    return img
                except Exception:
                    print('   aviso: %s está corrompido; procuro outra cópia' % os.path.basename(path))
    if not quiet:
        skipped.append('%s %s' % (prefix, number))
    return None


def key_of(img):
    """The flat background colour, voted along the whole border (drawings may touch a corner)."""
    rgb = np.asarray(img.convert('RGB')).astype(int)
    border = np.concatenate([rgb[2], rgb[-3], rgb[:, 2], rgb[:, -3]])
    votes = {}
    for key in (P.GREEN, BLUE, P.MAGENTA):
        votes[key] = int((np.abs(border - np.array(key)).sum(axis=1) < 90).sum())
    return max(votes, key=votes.get)


def keyed(img):
    img = only_keyed_area(img)
    out = P.key_out(img, key_of(img))
    return despill(out) if key_of(img) == P.MAGENTA else out


def only_keyed_area(img):
    """Some sheets come with an unpainted band (black or transparent) under the keyed background: keep
    only the rows whose left edge is the background colour."""
    rgb = np.asarray(img.convert('RGB'))
    if int(rgb[-3, 3].astype(int).sum()) > 30:
        return img  # the bottom is painted, not an empty band
    key = np.array(key_of(img))
    edge = np.abs(rgb[:, :4].astype(int) - key).sum(axis=2).min(axis=1) < 90
    rows = np.where(edge)[0]
    if len(rows) and rows.max() < img.height - 8:
        return img.crop((0, 0, img.width, rows.max() + 1))
    return img


def despill(img, everywhere=False):
    """Magenta that shows through thin or see-through parts (barbels, windscreens) turned to neutral grey."""
    arr = np.asarray(img).astype(np.float32)
    rgb = arr[..., :3] / 255.0
    # Inside a drawing only a strong magenta cast counts; on its rim (where the background bled in)
    # any magenta cast is taken out.
    from scipy import ndimage
    solid = arr[..., 3] > 250
    rim = ~ndimage.binary_erosion(solid, iterations=4)
    threshold = np.where(rim | everywhere, 0.0, 0.12)
    excess = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - threshold, 0, 1)
    grey = rgb.mean(axis=2, keepdims=True)
    k = np.clip(excess * 4, 0, 1)[..., None]
    rgb = rgb * (1 - k) + grey * k
    arr[..., :3] = rgb * 255
    return Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA')


def has_background(img):
    """Whether at least a quarter of the border is one of the flat key colours."""
    rgb = np.asarray(img.convert('RGB')).astype(int)
    border = np.concatenate([rgb[2], rgb[-3], rgb[:, 2], rgb[:, -3]])
    return max(int((np.abs(border - np.array(k)).sum(axis=1) < 90).sum()) for k in (P.GREEN, BLUE, P.MAGENTA)) > len(border) * 0.25


def cut_at(img, fraction):
    """Drops the rows below `fraction` of the height, with a short soft edge (painted water under a layer)."""
    return V.cut_waterline(img, fraction)


def scenery(folder, prefix, out_folder, file_prefix, sun_world, far_units, mid_units, far_cut=None, remix_mid=False):
    base = 'Mapas/%s/%s_' % (out_folder, file_prefix)
    photo = find(folder, prefix, '01')
    if photo is not None:
        P.thumb(photo, base + 'thumb.png')
    sky = find(folder, prefix, '02')
    if sky is not None:
        a = np.asarray(sky.convert('RGB'), dtype=float).sum(axis=2)
        ys, xs = np.where(a >= np.percentile(a, 99.7))
        P.sky(sky, (xs.mean() / a.shape[1], ys.mean() / a.shape[0]), sun_world, 7.5, base + 'bg_sky.png')
    far = find(folder, prefix, '03')
    if far is not None:
        piece = keyed(far)
        if far_cut:
            piece = cut_at(piece, far_cut)
        piece = P.trim(piece)
        h = int(round(far_units * 110))
        piece = piece.resize((max(1, round(piece.width * h / piece.height)), h), Image.LANCZOS)
        P.save(P.extend_sideways(piece, 0.5, 0.0, (0.0, 0.5), (0.5, 1.0), 110), base + 'bg_far.png')
    bands = [img for img in (find(folder, p, n, quiet=True) for p, n in BANDS.get(prefix, [])) if img is not None]
    mid = None if bands else find(folder, prefix, '04')
    if bands:
        P.save(alternate(bands, mid_units, 120), base + 'bg_mid.png')
    if mid is not None:
        piece = P.trim(keyed(mid))
        h = int(round(mid_units * 120))
        piece = piece.resize((max(1, round(piece.width * h / piece.height)), h), Image.LANCZOS)
        band = remix(piece, 120, seed=hash(prefix) % 1000) if remix_mid else P.extend_sideways(piece, 0.5, 0.0, (0.0, 0.5), (0.5, 1.0), 120)
        P.save(band, base + 'bg_mid.png')
    for number, name in (('05', 'near_left'), ('06', 'near_right'), ('07', 'fg_left'), ('08', 'fg_right')):
        img = find(folder, prefix, number)
        if img is not None and not has_background(img):
            # A full painting instead of a keyed piece: the mirror of the other side stands in.
            print('   aviso: %s %s veio sem fundo chapado; uso o lado oposto espelhado' % (prefix, number))
            skipped.append('%s %s (sem fundo chapado)' % (prefix, number))
            img = None
        if img is not None:
            P.save(P.trim(keyed(img)), base + name + '.png')
        else:
            other = {'near_left': 'near_right', 'near_right': 'near_left', 'fg_left': 'fg_right', 'fg_right': 'fg_left'}[name]
            twin = os.path.join(P.ART, base + other + '.png')
            if os.path.exists(twin) and (base + other + '.png') in P.written:
                P.save(Image.open(twin).transpose(Image.FLIP_LEFT_RIGHT), base + name + '.png')
    clouds = find(folder, prefix, '09')
    if clouds is not None:
        cloud_sheet(keyed(clouds), base[:-1])


def cloud_sheet(rgba, prefix):
    """Three clouds side by side or one under the other: split where the picture is emptiest."""
    a = np.asarray(rgba)[..., 3].astype(np.float32)
    cols, rows = a.sum(axis=0), a.sum(axis=1)

    def plan(profile):
        n = len(profile)
        cuts = [0]
        for lo, hi in ((0.22, 0.45), (0.55, 0.78)):
            i0, i1 = int(lo * n), int(hi * n)
            cuts.append(i0 + int(np.argmin(profile[i0:i1])))
        cuts.append(n)
        # How much drawing the two cuts go through, relative to the fullest line.
        return cuts, (profile[cuts[1]] + profile[cuts[2]]) / max(1.0, profile.max())

    # Side by side, the cuts between clouds cross empty columns; one under the other, empty rows.
    by_cols, cost_cols = plan(cols)
    by_rows, cost_rows = plan(rows)
    stacked = cost_rows < cost_cols
    cuts = by_rows if stacked else by_cols
    for i in range(3):
        box = (0, cuts[i], rgba.width, cuts[i + 1]) if stacked else (cuts[i], 0, cuts[i + 1], rgba.height)
        P.save(P.fit(P.main_piece(rgba.crop(box)), (640, 320), 0.04), prefix + '_cloud_%02d.png' % (i + 1))


def alternate(paintings, height_units, ppu):
    """Several different band paintings, keyed and scaled to the same height, laid A, B, A, B... across
    the 26 units, each joined to the next with a short soft overlap (the paintings were asked with
    matching ends)."""
    pieces = []
    for img in paintings:
        piece = P.trim(keyed(img))
        h = int(round(height_units * ppu))
        pieces.append(piece.resize((max(1, round(piece.width * h / piece.height)), h), Image.LANCZOS))
    h = max(p.height for p in pieces)
    total = int(round(2 * P.HALF_W * ppu))
    canvas = Image.new('RGBA', (total, h), (0, 0, 0, 0))
    overlap = int(0.2 * ppu)
    # Start so the first seam does not fall in the middle of the screen.
    x, i = -int(pieces[0].width * 0.3), 0
    while x < total:
        piece = pieces[i % len(pieces)]
        a = np.asarray(piece).astype(np.float32)
        if x > -piece.width:
            ramp = np.ones(piece.width, dtype=np.float32)
            ramp[:overlap] = np.linspace(0, 1, overlap)
            a[..., 3] *= ramp[None, :]
        layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
        layer.paste(Image.fromarray(a.astype(np.uint8), 'RGBA'), (x, h - piece.height))
        canvas = Image.alpha_composite(canvas, layer)
        x += piece.width - overlap
        i += 1
    return canvas


def remix(piece, ppu, seed):
    """A band of separate groups (capões) rebuilt to 26 units without a visible pattern.

    The piece is cut where its silhouette is lowest (the open grass between groups), and the groups
    are laid side by side in a shuffled order, some mirrored, each a little bigger or smaller,
    overlapping with a soft edge. The same group never comes twice in a row.
    """
    rng = np.random.default_rng(seed)
    arr = np.asarray(piece)
    alpha = arr[..., 3] > 40
    height = np.where(alpha.any(axis=0), piece.height - alpha.argmax(axis=0), 0).astype(float)
    smooth = np.convolve(height, np.ones(15) / 15, mode='same')
    # Cut points: low places at least ~0.9 units apart.
    order = np.argsort(smooth)
    cuts = []
    for x in order:
        if x < 20 or x > piece.width - 20:
            continue
        if all(abs(x - c) > 0.9 * ppu for c in cuts):
            cuts.append(int(x))
        if len(cuts) >= 8:
            break
    cuts = [0] + sorted(cuts) + [piece.width]
    groups = [piece.crop((cuts[i], 0, cuts[i + 1], piece.height)) for i in range(len(cuts) - 1) if cuts[i + 1] - cuts[i] > 30]
    total = int(round(2 * P.HALF_W * ppu))
    canvas = Image.new('RGBA', (total, piece.height), (0, 0, 0, 0))
    overlap = int(0.25 * ppu)
    x, last = -overlap, -1
    while x < total:
        i = int(rng.integers(len(groups)))
        if i == last and len(groups) > 1:
            continue
        last = i
        g = groups[i]
        if rng.random() < 0.5:
            g = g.transpose(Image.FLIP_LEFT_RIGHT)
        k = float(rng.uniform(0.82, 1.08))
        g = g.resize((max(1, int(g.width * k)), max(1, int(g.height * k))), Image.LANCZOS)
        # Soft left and right edges so neighbours blend into each other.
        a = np.asarray(g).astype(np.float32)
        ramp = np.ones(g.width, dtype=np.float32)
        n = min(overlap, g.width // 3)
        if n > 1:
            ramp[:n] = np.linspace(0, 1, n)
            ramp[-n:] = np.linspace(1, 0, n)
        a[..., 3] *= ramp[None, :]
        g = Image.fromarray(a.astype(np.uint8), 'RGBA')
        layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
        layer.paste(g, (x, piece.height - g.height))
        canvas = Image.alpha_composite(layer, canvas) if rng.random() < 0.5 else Image.alpha_composite(canvas, layer)
        x += g.width - overlap
    return canvas


def grid(img, cols, rows, names, folder_rel, size, margin):
    for cell, name in zip(P.cells(keyed(img), cols, rows), names):
        P.save(P.fit(P.main_piece(cell), size, margin), folder_rel % name)


def fish_and_gear(folder):
    for prefix, number, cols, rows, names in (('Pantanal', '10', 2, 3, PANTANAL_GRID_1), ('Pantanal', '11', 2, 2, PANTANAL_GRID_2),
                                              ('Estuario', '10', 2, 3, ESTUARIO_GRID_1), ('Estuario', '11', 2, 2, ESTUARIO_GRID_2)):
        img = find(folder, prefix, number)
        if img is not None:
            grid(img, cols, rows, names, 'Peixes/fish_%s_master.png', (1024, 512), 0.06)
    rod = find(folder, 'Novo', '01')
    if rod is not None:
        P.save(P.fit(P.main_piece(keyed(rod)), (1024, 512), 0.04), 'Varas/rod_02.png')
    boats = find(folder, 'Novo', '02')
    if boats is not None:
        grid(boats, 3, 2, BOATS, 'Barcos/%s.png', (1024, 512), 0.05)
    icons = find(folder, 'Novo', '03')
    if icons is not None:
        for cell, name in zip(P.cells(keyed(icons), 2, 1), ('barco', 'isca')):
            P.save(glyph(P.fit(P.main_piece(cell), (384, 384), 0.06)).resize((96, 96), Image.LANCZOS), 'Icones/ico_%s.png' % name)


def glyph(img):
    """A painted icon turned into the menu's style: a white shape the game tints, with its dark
    details (windows, the eye, the hooks' inside) cut out so it still reads at 20 pixels."""
    from scipy import ndimage
    arr = np.asarray(img).astype(np.float32)
    lum = arr[..., :3].mean(axis=2) / 255.0
    shape = ndimage.binary_closing(arr[..., 3] > 128, iterations=3)
    # Only bigger dark areas become holes; specks of shading stay solid.
    dark = ndimage.binary_opening((lum < 0.25) & shape, iterations=3)
    alpha = np.where(shape & ~dark, 255.0, 0.0)
    alpha = ndimage.gaussian_filter(alpha, 1.2)
    out = np.dstack([np.full(lum.shape, 255.0), np.full(lum.shape, 255.0), np.full(lum.shape, 255.0), alpha])
    return Image.fromarray(out.astype(np.uint8), 'RGBA')


def living(folder):
    for (prefix, number), name in PLANTS.items():
        img = find(folder, prefix, number)
        if img is None:
            continue
        for n, piece in enumerate(V.pieces(keyed(img)), start=1):
            piece = piece.crop(V.bbox(piece, 12))
            if name in NO_MAGENTA:
                piece = despill(piece, everywhere=True)
            V.save(V.shrink(piece, V.PLANT_MAX), 'Plantas/%s_%02d.png' % (name, n))
    for (prefix, number), groups in ANIMALS.items():
        img = find(folder, prefix, number)
        if img is None:
            continue
        found = V.pieces(keyed(img))
        for name, indices, kind in groups:
            frames = [found[i] for i in indices if i < len(found)]
            if len(frames) != len(indices):
                print('   aviso: %s %s tem %d desenhos, esperava %d' % (prefix, number, len(found), len(indices)))
                continue
            anchors = [V.anchor(f, kind) for f in frames]
            boxes = [V.bbox(f, 12) for f in frames]
            left = max(ax - b[0] for (ax, _), b in zip(anchors, boxes))
            right = max(b[2] - ax for (ax, _), b in zip(anchors, boxes))
            up = max(ay - b[1] for (_, ay), b in zip(anchors, boxes))
            down = max(b[3] - ay for (_, ay), b in zip(anchors, boxes))
            w, h = int(np.ceil(left + right)), int(np.ceil(up + down))
            s = min(1.0, V.ANIMAL_MAX / max(w, h))
            for n, (frame, (ax, ay), b) in enumerate(zip(frames, anchors, boxes), start=1):
                canvas = Image.new('RGBA', (w, h), (0, 0, 0, 0))
                canvas.alpha_composite(frame.crop(b), (int(round(left - (ax - b[0]))), int(round(up - (ay - b[1])))))
                if s < 1.0:
                    canvas = canvas.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
                V.save(canvas, 'Animais/%s_%d.png' % (name, n))


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    # Several folders: the later ones win (a newer pack over an older one); broken files are skipped.
    folder = sys.argv[1:]
    scenery(folder, 'Pantanal', 'PantanalDourado', 'map_pantanal_dourado', (-5.0, 1.5), 0.9, 1.25, remix_mid=True)
    scenery(folder, 'Estuario', 'EstuarioDasMares', 'map_estuario_das_mares', (2.4, 1.8), 1.3, 1.2, far_cut=0.80)
    fish_and_gear(folder)
    living(folder)
    old = set()
    if os.path.exists(P.FINALS):
        old = {l.strip() for l in open(P.FINALS, encoding='utf-8') if l.strip() and not l.startswith('#')}
    written = set(P.written) | set(V.written)
    with open(P.FINALS, 'w', encoding='utf-8') as f:
        f.write('# Arquivos de arte finais (feitos pelo proprietário). Os geradores de arte provisória não os sobrescrevem.\n')
        for rel in sorted(r for r in old | written if os.path.exists(os.path.join(P.ART, r))):
            f.write(rel + '\n')
    print('Arte final gravada:', len(written), 'arquivos.')
    if skipped:
        print('Faltaram (o provisório continua):', ', '.join(skipped))


if __name__ == '__main__':
    main()
