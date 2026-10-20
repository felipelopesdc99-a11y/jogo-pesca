"""Variantes dos retratos em pixel art (M25-T16): versão pequena e versão redonda.

Uso (mensagens em PT-BR, porque o proprietário roda no terminal):

  python3 tools/Arte/retratos_pixel.py

Para o retrato do pescador (Cena/retrato) e os 6 avatares (Avatares/avatar_01..06), todos 64 × 64, grava ao lado
de cada arquivo:

  <nome>_p.png    32 × 32, metade (moda de cada bloco 2 × 2, o contorno escuro ganha no empate), para molduras
                  menores que 64 px na tela (Ranking, listas);
  <nome>_r.png    64 × 64 recortado num círculo de pixels, para as molduras redondas (a interface não recorta pixel
                  art com um círculo suave);
  <nome>_r_p.png  a versão redonda pela metade.

O jogo procura "_p" sozinho (ArtAssets.Small) e "_r" pelo AvatarArt. Rodar de novo depois de trocar um retrato.
Só gera imagens: nada aqui mexe em regra do jogo.
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixel_folhas import OUT_ROOT, halve, to_image  # noqa: E402

NAMES = ['Cena/retrato'] + ['Avatares/avatar_%02d' % i for i in range(1, 7)]


def circle(op):
    """Keeps the pixels whose centre is inside the circle inscribed in the square."""
    h, w = op.shape
    ys, xs = np.mgrid[0:h, 0:w]
    r = min(w, h) / 2.0
    inside = (xs + 0.5 - w / 2.0) ** 2 + (ys + 0.5 - h / 2.0) ** 2 <= r * r
    return op & inside


def save(rgb, op, path):
    to_image(rgb, op).save(path, optimize=True)
    print('  ' + os.path.relpath(path, OUT_ROOT) + '  %d×%d' % (op.shape[1], op.shape[0]))


def main():
    print('Retratos em pixel: versões pequena (_p) e redonda (_r)')
    for name in NAMES:
        path = os.path.join(OUT_ROOT, name + '.png')
        if not os.path.exists(path):
            print('  falta ' + name + '.png (pulado)')
            continue

        a = np.asarray(Image.open(path).convert('RGBA'))
        rgb = a[..., :3].copy()
        op = a[..., 3] >= 128
        save(*halve(rgb, op), path[:-4] + '_p.png')
        round_op = circle(op)
        save(rgb, round_op, path[:-4] + '_r.png')
        save(*halve(rgb, round_op), path[:-4] + '_r_p.png')

    print('Pronto.')


if __name__ == '__main__':
    main()
