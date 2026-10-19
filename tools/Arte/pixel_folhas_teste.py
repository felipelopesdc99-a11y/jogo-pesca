"""Teste do pixel_folhas.py com uma folha sintética (não precisa de nenhuma arte do proprietário).

Uso:  python3 tools/Arte/pixel_folhas_teste.py

Builds a 3×2 sheet of small "fish" drawn on a 64×64 grid, scaled up 8× without smoothing, then
resized to a non-integer size with bilinear filtering and sprinkled with noise (what ChatGPT really
delivers), on flat magenta. The cutter must find a factor near 8, give back drawings about the
native size, with ≤ 32 colours, transparent background and a dark 1 px outline.
"""
import os
import shutil
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
import pixel_folhas as PF  # noqa: E402

NATIVE = 64
FISH_W, FISH_H = 40, 20
COLOURS = [((70, 150, 90), (40, 100, 60), (150, 210, 160)), ((220, 170, 40), (160, 110, 20), (250, 230, 140)),
           ((60, 110, 200), (30, 60, 140), (150, 190, 250)), ((200, 80, 70), (130, 40, 40), (250, 160, 150)),
           ((150, 150, 160), (90, 90, 100), (220, 220, 230)), ((120, 70, 160), (70, 40, 110), (200, 160, 230))]


def fish(base, dark, light, outlined):
    im = Image.new('RGBA', (NATIVE, NATIVE), (255, 0, 255, 255))
    d = ImageDraw.Draw(im)
    x0, y0 = (NATIVE - FISH_W) // 2, (NATIVE - FISH_H) // 2
    d.ellipse([x0, y0, x0 + FISH_W - 8, y0 + FISH_H - 1], fill=base)
    d.polygon([(x0 + FISH_W - 10, y0 + FISH_H // 2), (x0 + FISH_W - 1, y0), (x0 + FISH_W - 1, y0 + FISH_H - 1)], fill=dark)
    d.rectangle([x0 + 4, y0 + FISH_H // 2 + 2, x0 + FISH_W - 14, y0 + FISH_H - 4], fill=dark)
    d.rectangle([x0 + 6, y0 + 3, x0 + 18, y0 + 5], fill=light)
    d.rectangle([x0 + 5, y0 + 7, x0 + 6, y0 + 8], fill=(10, 10, 20))
    if outlined:
        a = np.asarray(im).copy()
        m = ~((a[..., 0] == 255) & (a[..., 1] == 0) & (a[..., 2] == 255))
        from scipy import ndimage
        ring = ndimage.binary_dilation(m) & ~m
        a[ring] = (20, 30, 25, 255)
        im = Image.fromarray(a)
    return im


def build_sheet(path):
    sheet = Image.new('RGBA', (3 * NATIVE, 2 * NATIVE), (255, 0, 255, 255))
    for i, c in enumerate(COLOURS):
        sheet.paste(fish(*c, outlined=(i % 2 == 0)), ((i % 3) * NATIVE, (i // 3) * NATIVE))
    big = sheet.resize((sheet.width * 8, sheet.height * 8), Image.NEAREST)
    big = big.resize((1500, 1000), Image.BILINEAR).convert('RGB')
    rng = np.random.default_rng(7)
    a = np.asarray(big).astype(int) + rng.integers(-6, 7, (1000, 1500, 3))
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(path)


def main():
    tmp = tempfile.mkdtemp(prefix='pixel_folhas_')
    try:
        src = os.path.join(tmp, 'folha.png')
        build_sheet(src)
        names = [f'peixe_{i + 1}' for i in range(6)]
        out = os.path.join(tmp, 'saida')
        written, k = PF.process(src, 3, 2, (NATIVE, NATIVE), names, out, colors=32, half=True)
        problems = []
        k = (k[0] + k[1]) / 2
        if not 7 <= k <= 9:
            problems.append(f'fator detectado {k}, esperado perto de 8')
        if len(written) != 6:
            problems.append(f'{len(written)} arquivos, esperados 6')
        for p in written:
            im = np.asarray(Image.open(p).convert('RGBA'))
            h, w = im.shape[:2]
            op = im[..., 3] > 0
            cols = len(np.unique(im[op][:, :3], axis=0))
            if not (FISH_W - 2 <= w <= FISH_W + 8 and FISH_H - 2 <= h <= FISH_H + 8):
                problems.append(f'{os.path.basename(p)}: {w}×{h}, esperado perto de {FISH_W}×{FISH_H} (+contorno e margem)')
            if cols > 32:
                problems.append(f'{os.path.basename(p)}: {cols} cores')
            if set(np.unique(im[..., 3])) - {0, 255}:
                problems.append(f'{os.path.basename(p)}: transparência não é binária')
            if not PF.has_outline(im[..., :3], op):
                problems.append(f'{os.path.basename(p)}: sem contorno')
            if not os.path.exists(p[:-4] + '_p.png'):
                problems.append(f'{os.path.basename(p)}: faltou a versão _p')
        # Second sheet: whole-number factor 6, no size given, stretched to twice the height
        # (non-square pixels), as the manifest-free command line would see it.
        sheet = Image.new('RGBA', (3 * NATIVE, NATIVE), (255, 0, 255, 255))
        for i, c in enumerate(COLOURS[:3]):
            sheet.paste(fish(*c, outlined=False), (i * NATIVE, 0))
        src2 = os.path.join(tmp, 'esticada.png')
        sheet.resize((sheet.width * 6, sheet.height * 12), Image.NEAREST).save(src2)
        written2, _ = PF.process(src2, 3, 1, None, ['a', 'b', 'c'], os.path.join(tmp, 'saida2'))
        for p in written2:
            w, h = Image.open(p).size
            if not (FISH_W - 2 <= w <= FISH_W + 8 and FISH_H - 2 <= h <= FISH_H + 8):
                problems.append(f'folha esticada, {os.path.basename(p)}: {w}×{h}, esperado perto de {FISH_W}×{FISH_H}')
        if problems:
            print('FALHOU:')
            for p in problems:
                print('  -', p)
            sys.exit(1)
        fk = f'{k:.2f}'.replace('.', ',')
        print(f'OK: fator {fk}, 6 peixes recortados, com contorno, até 32 cores e versão _p.')
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == '__main__':
    main()
