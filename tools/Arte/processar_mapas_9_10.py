"""Turns the owner's art for maps 9 and 10 (the "Pedidos de arte: Talude Noturno e Abismo Atlântico"
document) into game files. Same steps as processar_mapas_7_8.py, with the names of this pack.

Usage:  python3 tools/Arte/processar_mapas_9_10.py <folder> [<newer folder> ...]
        (folders with Talude_XX_*.png, Abismo_XX_*.png, Vara5_XX_*.png and the Corrente 02 and 11 that
        were missing; the newer wins)

The night pictures come on a green background. The horizon bands of these maps are night clouds and
swells, so nothing is cut under them. Corrente 02 and 11 go through processar_mapas_7_8.py: run it with
the old and the new folder.
"""
import os
import sys

import numpy as np
from PIL import Image

import processar_mapas_3_4 as M
import processar_mapas_7_8 as R
import processar_pedidos as P
import processar_vivos as V

TALUDE_GRID_1 = ['escolar', 'peixe_espada', 'abrotea_fundo', 'congro_negro', 'peixe_lanceta', 'quimera']
TALUDE_GRID_2 = ['tubarao_lanterna', 'peixe_opah', 'tubarao_duende', 'tubarao_seis_guelras']
ABISMO_GRID_1 = ['peixe_machado', 'peixe_dragao', 'peixe_vibora', 'enguia_pelicano', 'peixe_ogro', 'peixe_pescador_abissal']
ABISMO_GRID_2 = ['quimera_azul', 'tubarao_cobra', 'peixe_fita_gigante', 'tubarao_boca_grande']

ANIMALS = {
    ('Talude', '12'): [('petrel', [0, 1, 2, 3], 'beak')],
    ('Talude', '13'): [('vulto', [0, 1, 2], 'center')],
    ('Abismo', '12'): [('vulto_gigante', [0, 1, 2], 'center')],
}


def keyed(img):
    """As M.keyed, without the "unpainted band" check (the night waves reach the bottom corner in a
    colour almost black, which that check took for an empty band) and with the green that bled into
    the soft edges turned neutral."""
    key = M.key_of(img)
    out = P.key_out(img, key)
    if key == P.GREEN:
        out = despill_green(out)
    elif key == P.MAGENTA:
        out = M.despill(out)
    return out


def despill_green(img):
    """Green that shows through soft edges (spray, clouds, glow) turned to the colour around it."""
    from scipy import ndimage
    arr = np.asarray(img).astype(np.float32)
    rgb = arr[..., :3] / 255.0
    solid = arr[..., 3] > 250
    rim = ~ndimage.binary_erosion(solid, iterations=4)
    excess = np.clip(rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2]) - np.where(rim, 0.0, 0.10), 0, 1)
    k = np.clip(excess * 4, 0, 1)[..., None]
    target = rgb.copy()
    target[..., 1] = np.maximum(rgb[..., 0], rgb[..., 2])
    rgb = rgb * (1 - k) + target * k
    arr[..., :3] = rgb * 255
    return Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA')


def night_sky(folder, prefix, moon_world, box, rel, height_units=7.5, ppu=96):
    """A night sky continued sideways with mirrored copies of the painting (stars and the milky way go
    on), instead of a flat colour; the moon is wiped out of the copies so there is only one."""
    from scipy import ndimage
    img = M.find(folder, prefix, '02', quiet=True)
    if img is None:
        return
    a = np.asarray(img.convert('RGB'), dtype=float).sum(axis=2)
    h, w = a.shape
    x0, y0, x1, y1 = int(box[0] * w), int(box[1] * h), int(box[2] * w), int(box[3] * h)
    part = a[y0:y1, x0:x1]
    ys, xs = np.where(part >= np.percentile(part, 99.7))
    frac = ((xs.mean() + x0) / w, (ys.mean() + y0) / h)
    W, H = int(2 * P.HALF_W * ppu), int(6.0 * ppu)
    iw = int(round(w * height_units * ppu / h))
    ih = int(round(height_units * ppu))
    pic = np.asarray(img.convert('RGB').resize((iw, ih), Image.LANCZOS), dtype=np.float32)
    # The copies without the moon: the disc and its halo filled with the blurred sky around them.
    lum = pic.sum(axis=2)
    cy, cx = frac[1] * ih, frac[0] * iw
    disc = lum > np.percentile(lum, 99.7)
    r = max(6.0, np.sqrt(disc.sum() / np.pi)) * 3.5
    yy, xx = np.mgrid[0:ih, 0:iw]
    mask = (yy - cy) ** 2 + (xx - cx) ** 2 < r * r
    filled = pic.copy()
    filled[mask] = np.median(pic[~mask & ((yy - cy) ** 2 + (xx - cx) ** 2 < (r * 2) ** 2)], axis=0)
    soft = ndimage.gaussian_filter(mask.astype(np.float32), r / 4)[..., None]
    moonless = pic * (1 - soft) + ndimage.gaussian_filter(filled, (r / 3, r / 3, 0)) * soft
    left = int(round((moon_world[0] + P.HALF_W) * ppu - frac[0] * iw))
    bottom = moon_world[1] - (1 - frac[1]) * height_units
    top_row = int(round((6.0 - (bottom + height_units - (P.HORIZON - 0.2))) * ppu))
    rows = np.clip(np.arange(H) - top_row, 0, ih - 1)
    out = np.zeros((H, W, 3), dtype=np.float32)
    for x in range(W):
        k, sx = divmod(x - left, iw)
        if k == 0:
            out[:, x] = pic[rows, sx]
        else:
            col = (iw - 1 - sx) if k % 2 else sx
            out[:, x] = moonless[rows, col]
    P.save(Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), 'RGB').convert('RGBA'), rel)


def fish_and_rod(folder):
    for prefix, number, cols, rows, names in (('Talude', '10', 2, 3, TALUDE_GRID_1), ('Talude', '11', 2, 2, TALUDE_GRID_2),
                                              ('Abismo', '10', 2, 3, ABISMO_GRID_1), ('Abismo', '11', 2, 2, ABISMO_GRID_2)):
        img = M.find(folder, prefix, number)
        if img is not None:
            R.grid_by_pieces(img, cols, rows, names, 'Peixes/fish_%s_master.png', (1024, 512), 0.06)
    shop = M.find(folder, 'Vara5', '01')
    if shop is not None:
        P.save(P.fit(P.main_piece(M.keyed(shop)), (1024, 512), 0.04), 'Varas/rod_05.png')
    scene = M.find(folder, 'Vara5', '02')
    if scene is not None:
        rod = P.trim(P.main_piece(M.keyed(scene)))
        if rod.width > 1500:
            rod = rod.resize((1470, max(1, round(rod.height * 1470 / rod.width))), Image.LANCZOS)
        P.save(rod, 'Varas/Cena/rod_05.png')


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    folder = sys.argv[1:]
    M.ALTERNATES = {}
    M.BANDS = {'Talude': [('Talude', '04'), ('Talude', '04B')], 'Abismo': [('Abismo', '04'), ('Abismo', '04B')]}
    M.PLANTS = {}
    M.ANIMALS = ANIMALS
    M.NO_MAGENTA = set()
    M.keyed = keyed
    # The moon is the brightest spot inside the box (stars are brighter than the sky around them).
    M.scenery(folder, 'Talude', 'TaludeNoturno', 'map_talude_noturno', (-4.6, 2.6), 0.5, 0.45, sky_whole_width=True,
              sun_box=(0, 0.2, 0.6, 0.85))
    R.soften_inner_edges('Mapas/TaludeNoturno/map_talude_noturno_')
    R.soften_inner_edges('Mapas/TaludeNoturno/map_talude_noturno_', 0.2, ('fg_left', 'fg_right'))
    M.scenery(folder, 'Abismo', 'AbismoAtlantico', 'map_abismo_atlantico', (5.0, 4.0), 0.35, 0.45, sky_whole_width=True,
              sun_box=(0.5, 0, 1, 0.5))
    R.soften_inner_edges('Mapas/AbismoAtlantico/map_abismo_atlantico_')
    R.soften_inner_edges('Mapas/AbismoAtlantico/map_abismo_atlantico_', 0.2, ('fg_left', 'fg_right'))
    night_sky(folder, 'Talude', (-4.6, 2.6), (0, 0.2, 0.6, 0.85), 'Mapas/TaludeNoturno/map_talude_noturno_bg_sky.png')
    night_sky(folder, 'Abismo', (5.0, 4.0), (0.5, 0, 1, 0.5), 'Mapas/AbismoAtlantico/map_abismo_atlantico_bg_sky.png')
    fish_and_rod(folder)
    M.living(folder)
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
