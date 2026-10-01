"""Turns the owner's art for maps 3 and 4 (the "Pedidos de arte: Pantanal Dourado e Estuário das Marés"
document) into game files.

Usage:  python3 tools/Arte/processar_mapas_3_4.py <folder with Pantanal_XX_*.png, Estuario_XX_*.png, Novo_XX_*.png>

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
}

# Plants with no real pink or purple in them: any magenta cast is background that bled in.
NO_MAGENTA = {'carandaa', 'mangue'}

skipped = []


def find(folder, prefix, number):
    hits = sorted(glob.glob(os.path.join(folder, '%s_%s_*.png' % (prefix, number))))
    if not hits or os.path.getsize(hits[0]) == 0:
        skipped.append('%s %s' % (prefix, number))
        return None
    return Image.open(hits[0])


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


def scenery(folder, prefix, out_folder, file_prefix, sun_world, far_units, mid_units, far_cut=None):
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
    mid = find(folder, prefix, '04')
    if mid is not None:
        piece = P.layer_piece(mid, key_of(mid), mid_units, 120)
        P.save(P.extend_sideways(piece, 0.5, 0.0, (0.0, 0.5), (0.5, 1.0), 120), base + 'bg_mid.png')
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
    folder = sys.argv[1]
    scenery(folder, 'Pantanal', 'PantanalDourado', 'map_pantanal_dourado', (-5.0, 1.5), 0.9, 1.25)
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
