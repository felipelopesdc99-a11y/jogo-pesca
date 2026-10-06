"""Turns the owner's art for maps 7 and 8 (the "Pedidos de arte: Corrente Azul e Banco das Baleias"
document) into game files. Same steps as processar_mapas_5_6.py, with the names of this pack.

Usage:  python3 tools/Arte/processar_mapas_7_8.py <folder> [<newer folder> ...]
        (folders with Corrente_XX_*.png, Baleias_XX_*.png, Vara4_XX_*.png; the newer wins)

The same pack also brought Coral 02, Coral 09 and Arquipelago 09: run processar_mapas_5_6.py with the
old and the new folder for those.

What it does, per request (same numbering as the maps before):
  01 photo, 02 sky, 03 far horizon, 04 + 04B middle bands (A, B, A, B), 05 / 06 shores, 07 / 08 corners,
  09 clouds, 10 / 11 fish grids, Corrente 12-14 and Baleias 12-13 living landscape,
  Vara4 01 (Shop) and 02 (scene).

In the Corrente Azul there is no land: the middle bands (04, 04B) and the shores (05, 06) are painted waves,
so only the horizon (03) has its painted sea taken out. In the Banco das Baleias the islets and the rocks
(03, 04, 04B) have it taken out, as in maps 5 and 6.
"""
import os
import sys

import numpy as np
from PIL import Image

import processar_mapas_3_4 as M
import processar_mapas_5_6 as Q
import processar_pedidos as P
import processar_vivos as V

CORRENTE_GRID_1 = ['bicuda', 'bonito_pintado', 'agulha_branca', 'cavalinha', 'atum_patudo', 'albacora_branca']
CORRENTE_GRID_2 = ['peixe_lua', 'espadarte', 'marlim_branco', 'marlim_azul']
BALEIAS_GRID_1 = ['pargo_rosa', 'namorado', 'batata', 'congro_rosa', 'cherne', 'cacao_anjo']
BALEIAS_GRID_2 = ['tubarao_lixa', 'arraia_chita', 'tubarao_martelo', 'raia_manta']

PLANTS = {
    ('Corrente', '14'): 'sargaco',
}
ANIMALS = {
    ('Corrente', '12'): [('peixe_voador', [0, 1, 2], 'center')],
    ('Corrente', '13'): [('pardela', [0, 1, 2, 3], 'beak')],
    ('Baleias', '12'): [('jubarte_salto', [0, 1, 2], 'feet')],
    ('Baleias', '13'): [('jubarte_borrifo', [0, 1, 2], 'feet')],
}
# The spout is white spray over the magenta: whatever pink shows through is background.
NO_MAGENTA_ANIMALS = {'jubarte_borrifo'}
PAINTED_SEA = {('Corrente', '03'), ('Baleias', '03'), ('Baleias', '04'), ('Baleias', '04B')}

_find = M.find


def find(folders, prefix, number, quiet=False):
    img = _find(folders, prefix, number, quiet)
    if img is not None and (prefix, number) in PAINTED_SEA:
        img = Q.drop_painted_sea(img)
    return img


def grid_by_pieces(img, cols, rows, names, folder_rel, size, margin):
    """Like M.grid, but each fish is taken as a whole shape instead of being cut at the cell lines: in
    these sheets some heads, tails and fins cross into the neighbour's cell. Each shape goes to the cell
    that holds its middle. A shape that really sits in two cells (two fish touching) is split at the
    cell line. Small loose bits join the nearest big shape."""
    from scipy import ndimage
    rgba = np.asarray(M.keyed(img)).copy()
    h, w = rgba.shape[:2]
    solid = rgba[..., 3] > 24
    labels, count = ndimage.label(solid, structure=np.ones((3, 3)))
    sizes = ndimage.sum(solid, labels, range(1, count + 1))
    centers = ndimage.center_of_mass(solid, labels, range(1, count + 1))
    ys, xs = np.mgrid[0:h, 0:w]
    cell_of = np.minimum(ys * rows // h, rows - 1) * cols + np.minimum(xs * cols // w, cols - 1)
    big = [i + 1 for i in range(count) if sizes[i] >= 0.05 * sizes.max()]
    owner_map = np.full((h, w), -1, dtype=int)
    for b in big:
        px = labels == b
        share = np.bincount(cell_of[px], minlength=cols * rows) / px.sum()
        if (share >= 0.25).sum() > 1:
            # Two fish touching: shrink the shape until the bodies come apart, then give every pixel to
            # the nearest body (a fin goes with the fish it grows from). If they never come apart, cut
            # at the cell lines.
            cores, k = ndimage.label(ndimage.binary_erosion(px, iterations=14), structure=np.ones((3, 3)))
            if k >= 2:
                csz = ndimage.sum(cores > 0, cores, range(1, k + 1))
                keep = [i + 1 for i in range(k) if csz[i] >= 0.05 * csz.max()]
                cores = np.where(np.isin(cores, keep), cores, 0)
                _, (iy, ix) = ndimage.distance_transform_edt(cores == 0, return_indices=True)
                nearest = cores[iy, ix]
                cc = ndimage.center_of_mass(cores > 0, cores, keep)
                cell_of_core = {c: min(int(cy * rows // h), rows - 1) * cols + min(int(cx * cols // w), cols - 1) for c, (cy, cx) in zip(keep, cc)}
                owner_map[px] = np.vectorize(lambda c: cell_of_core.get(c, -1))(nearest[px])
            else:
                owner_map[px] = cell_of[px]
        else:
            owner_map[px] = int(np.argmax(share))
    for i in range(1, count + 1):
        if i in big or sizes[i - 1] < 0.002 * sizes.max():
            continue
        cy, cx = centers[i - 1]
        nearest = min(big, key=lambda b: (centers[b - 1][0] - cy) ** 2 + (centers[b - 1][1] - cx) ** 2)
        owner_map[labels == i] = int(np.bincount(owner_map[labels == nearest][owner_map[labels == nearest] >= 0]).argmax())
    for cell, name in enumerate(names):
        mine = owner_map == cell
        lab, k = ndimage.label(mine, structure=np.ones((3, 3)))
        if k > 1:
            # After a split, the neighbour's fin that crossed the line is a small part: drop it.
            sz = ndimage.sum(mine, lab, range(1, k + 1))
            mine = np.isin(lab, [i + 1 for i in range(k) if sz[i] >= 0.05 * sz.max()])
        mask = ndimage.binary_dilation(mine, iterations=2)
        piece = rgba.copy()
        piece[..., 3] = np.where(mask, piece[..., 3], 0)
        P.save(P.fit(Image.fromarray(piece, 'RGBA'), size, margin), folder_rel % name)


def fish_and_rod(folder):
    for prefix, number, cols, rows, names in (('Corrente', '10', 2, 3, CORRENTE_GRID_1), ('Corrente', '11', 2, 2, CORRENTE_GRID_2),
                                              ('Baleias', '10', 2, 3, BALEIAS_GRID_1), ('Baleias', '11', 2, 2, BALEIAS_GRID_2)):
        img = find(folder, prefix, number)
        if img is not None:
            grid_by_pieces(img, cols, rows, names, 'Peixes/fish_%s_master.png', (1024, 512), 0.06)
    shop = find(folder, 'Vara4', '01')
    if shop is not None:
        P.save(P.fit(P.main_piece(M.keyed(shop)), (1024, 512), 0.04), 'Varas/rod_04.png')
    scene = find(folder, 'Vara4', '02')
    if scene is not None:
        rod = P.trim(P.main_piece(M.keyed(scene)))
        if rod.width > 1500:
            rod = rod.resize((1470, max(1, round(rod.height * 1470 / rod.width))), Image.LANCZOS)
        P.save(rod, 'Varas/Cena/rod_04.png')


def soften_inner_edges(base, fraction=0.15):
    """The big waves of the Corrente Azul end in a straight cut where the painting ended: fade the inner
    side (the right of the left wave, the left of the right wave) so they sink into the sea."""
    for name, inner_right in (('near_left', True), ('near_right', False)):
        rel = base + name + '.png'
        path = os.path.join(P.ART, rel)
        if rel not in P.written or not os.path.exists(path):
            continue
        arr = np.asarray(Image.open(path).convert('RGBA')).astype(np.float32)
        w = arr.shape[1]
        n = max(2, int(w * fraction))
        ramp = np.ones(w, dtype=np.float32)
        if inner_right:
            ramp[-n:] = np.linspace(1, 0, n)
        else:
            ramp[:n] = np.linspace(0, 1, n)
        arr[..., 3] *= ramp[None, :]
        Image.fromarray(arr.astype(np.uint8), 'RGBA').save(path, optimize=True)


def clean_spray():
    for name in NO_MAGENTA_ANIMALS:
        for n in range(1, 10):
            rel = 'Vivos/Animais/%s_%d.png' % (name, n)
            path = os.path.join(P.ART, rel)
            if rel in V.written and os.path.exists(path):
                M.despill(Image.open(path).convert('RGBA'), everywhere=True).save(path, optimize=True)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    folder = sys.argv[1:]
    M.find = find
    M.ALTERNATES = {}
    M.BANDS = {'Corrente': [('Corrente', '04'), ('Corrente', '04B')], 'Baleias': [('Baleias', '04'), ('Baleias', '04B')]}
    M.PLANTS = PLANTS
    M.ANIMALS = ANIMALS
    M.NO_MAGENTA = set()
    M.scenery(folder, 'Corrente', 'CorrenteAzul', 'map_corrente_azul', (4.8, 1.4), 0.6, 0.45, sky_whole_width=True)
    soften_inner_edges('Mapas/CorrenteAzul/map_corrente_azul_')
    M.scenery(folder, 'Baleias', 'BancoDasBaleias', 'map_banco_das_baleias', (-4.4, 0.9), 0.45, 0.5, sky_whole_width=True)
    fish_and_rod(folder)
    M.living(folder)
    clean_spray()
    old = set()
    if os.path.exists(P.FINALS):
        old = {l.strip() for l in open(P.FINALS, encoding='utf-8') if l.strip() and not l.startswith('#')}
    written = set(P.written) | set(V.written)
    with open(P.FINALS, 'w', encoding='utf-8') as f:
        f.write('# Arquivos de arte finais (feitos pelo proprietário). Os geradores de arte provisória não os sobrescrevem.\n')
        for rel in sorted(r for r in old | written if os.path.exists(os.path.join(P.ART, r))):
            f.write(rel + '\n')
    print('Arte final gravada:', len(written), 'arquivos.')
    if M.skipped:
        print('Faltaram (o provisório continua):', ', '.join(M.skipped))


if __name__ == '__main__':
    main()
