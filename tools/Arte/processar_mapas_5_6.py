"""Turns the owner's art for maps 5 and 6 (the "Pedidos de arte: Costa de Coral e Arquipélago do Sol"
document) into game files. Same steps as processar_mapas_3_4.py, with the names of this pack.

Usage:  python3 tools/Arte/processar_mapas_5_6.py <folder> [<newer folder> ...]
        (folders with Coral_XX_*.png, Arquipelago_XX_*.png, Vara3_XX_*.png; the newer wins)

What it does, per request:
  Coral 01 / Arquipélago 01   map photo for the Map screen (thumb)
  02                          sky, placed so its sun sits where the scene expects it
  03                          far horizon (cliffs / distant islands)
  04 + 04B                    two middle bands laid A, B, A, B
  05, 06                      the two shore groups
  07, 08                      foreground corners
  09                          three clouds
  10, 11                      fish grids (2 x 3 and 2 x 2), in the order of the document
  Coral 12-15 / Arquipélago 12-15   living landscape: animals (frames lined up) and plants
  Vara3 01                    Corrente Mestra for the Shop (Varas/rod_03.png)
  Vara3 02                    Corrente Mestra thin, for the scene (Varas/Cena/rod_03.png)

The horizon and the middle bands came with a strip of painted sea under them: those rows are turned
into the background colour before keying (the game's own water is underneath).
Every file written is listed in tools/Arte/finais.txt, so the placeholder generators never overwrite it.
A missing picture is skipped with a warning; the placeholder stays.
"""
import os
import sys

import numpy as np
from PIL import Image

import processar_mapas_3_4 as M
import processar_pedidos as P
import processar_vivos as V

CORAL_GRID_1 = ['sargo', 'salema', 'peixe_porco', 'pampo', 'sargentinho', 'ariaco']
CORAL_GRID_2 = ['cioba', 'badejo', 'dentao', 'caranha']
ARQUIPELAGO_GRID_1 = ['olhete', 'peixe_galo', 'beijupira', 'bonito_cachorro', 'serra', 'cavala_verdadeira']
ARQUIPELAGO_GRID_2 = ['olho_de_boi', 'dourado_do_mar', 'atum_amarelo', 'veleiro']

PLANTS = {
    ('Coral', '14'): 'restinga',
    ('Coral', '15'): 'coqueiros',
    ('Arquipelago', '15'): 'vegetacao',
}
ANIMALS = {
    ('Coral', '12'): [('gaivota', [0, 1, 2, 3], 'beak')],
    ('Coral', '13'): [('tartaruga_marinha', [0, 1, 2], 'center')],
    ('Arquipelago', '12'): [('atoba', [0, 1, 2, 3], 'beak')],
    ('Arquipelago', '13'): [('fragata', [0, 1, 2], 'beak')],
    ('Arquipelago', '14'): [('golfinho', [0, 1, 2], 'center')],
}
# Pictures with painted sea under the drawing.
PAINTED_SEA = {('Coral', '03'), ('Coral', '04'), ('Coral', '04B'),
               ('Arquipelago', '03'), ('Arquipelago', '04'), ('Arquipelago', '04B')}


def drop_painted_sea(img):
    """Rows of painted sea at the bottom of the drawing become the background colour."""
    rgb = np.asarray(img.convert('RGB')).astype(int)
    key = np.array(M.key_of(img))
    is_key = np.abs(rgb - key).sum(axis=2) < 120
    sea = (~is_key) & (rgb[..., 2] > rgb[..., 0] + 40)
    drawn = np.where(is_key.mean(axis=1) < 0.95)[0]
    if not len(drawn):
        return img
    y = drawn.max()
    while y > 0 and sea[y].mean() > 0.5:
        y -= 1
    if y == drawn.max():
        return img
    out = np.array(img.convert('RGB'))
    out[y + 1:] = key
    print('   %d linhas de mar pintado retiradas' % (drawn.max() - y))
    return Image.fromarray(out.astype(np.uint8), 'RGB')


_find = M.find


def find(folders, prefix, number, quiet=False):
    img = _find(folders, prefix, number, quiet)
    if img is not None and (prefix, number) in PAINTED_SEA:
        img = drop_painted_sea(img)
    return img


def fish_and_rod(folder):
    for prefix, number, cols, rows, names in (('Coral', '10', 2, 3, CORAL_GRID_1), ('Coral', '11', 2, 2, CORAL_GRID_2),
                                              ('Arquipelago', '10', 2, 3, ARQUIPELAGO_GRID_1), ('Arquipelago', '11', 2, 2, ARQUIPELAGO_GRID_2)):
        img = find(folder, prefix, number)
        if img is not None:
            M.grid(img, cols, rows, names, 'Peixes/fish_%s_master.png', (1024, 512), 0.06)
    shop = find(folder, 'Vara3', '01')
    if shop is not None:
        P.save(P.fit(P.main_piece(M.keyed(shop)), (1024, 512), 0.04), 'Varas/rod_03.png')
    scene = find(folder, 'Vara3', '02')
    if scene is not None:
        rod = P.trim(P.main_piece(M.keyed(scene)))
        # Same length as the other thin scene rods (about 1470 px).
        if rod.width > 1500:
            rod = rod.resize((1470, max(1, round(rod.height * 1470 / rod.width))), Image.LANCZOS)
        P.save(rod, 'Varas/Cena/rod_03.png')


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    folder = sys.argv[1:]
    M.find = find
    M.ALTERNATES = {}
    M.BANDS = {'Coral': [('Coral', '04'), ('Coral', '04B')], 'Arquipelago': [('Arquipelago', '04'), ('Arquipelago', '04B')]}
    M.PLANTS = PLANTS
    M.ANIMALS = ANIMALS
    M.NO_MAGENTA = {'vegetacao'}
    M.scenery(folder, 'Coral', 'CostaDeCoral', 'map_costa_de_coral', (-5.0, 3.8), 0.55, 0.95, sky_whole_width=True,
              sun_box=(0, 0, 0.5, 0.5))
    M.scenery(folder, 'Arquipelago', 'ArquipelagoDoSol', 'map_arquipelago_do_sol', (5.2, 3.6), 1.4, 0.9, sky_whole_width=True)
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
