#!/usr/bin/env python3
"""Turns the Shop art requested in docs/ASSETS_PENDENTES.md (section "Loja 'Balcão do Píer'", addendum A-150) into the
game's UI files in client-unity/Assets/Resources/Arte/UI.

Expected in the folder, as the generator gave them (any size in the right proportion):
  ui_loja_parede_noite.png  opaque plank wall, repeats sideways        -> cut from seam to seam (whole planks, so the
                                                                           tile joins on a seam), 768 high
  ui_loja_gancho.png        brass hook                                  -> 64x64, trimmed, 2 px margin
  ui_loja_balcao_pier.png   counter plank (9-slice, sides 48 px)        -> 1024x96, trimmed, stretched to size
  ui_loja_caixa.png         open tackle box (9-slice, borders 40 px)    -> 512x288, trimmed, stretched to size
  ui_loja_boia.png          bobber                                      -> 64x96, trimmed, 2 px margin; prints where the
                                                                           dark ring ended up (ShopWindow.BobberRing)

Every picture with transparency is trimmed to its content (alpha > 8, which drops the faint specks the generator
leaves near the edges) and resized with LANCZOS. Missing files are skipped with a message. Nothing to run until the
owner has the pictures.

Usage: python3 tools/Arte/processar_loja.py <folder with the ui_loja_*.png files>
"""
import os
import sys

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Arte", "UI")
ALPHA_MIN = 8

WALL_HEIGHT = 768
HOOK_SIZE = (64, 64)
COUNTER_HEIGHT = 96
BOX_WIDTH = 512
BOBBER_SIZE = (64, 96)


def load(folder, name, transparent=True):
    path = os.path.join(folder, name + ".png")
    if not os.path.exists(path):
        print(name + ".png: não encontrado, pulado.")
        return None
    image = Image.open(path).convert("RGBA")
    if transparent:
        corners = [image.getpixel(p) for p in ((0, 0), (image.width - 1, 0), (0, image.height - 1), (image.width - 1, image.height - 1))]
        if any(c[3] > 0 for c in corners):
            sys.exit(name + ": os cantos não são transparentes (fundo xadrez ou sólido?). Tire o fundo antes.")
    return image


def content_box(image):
    return image.getchannel("A").point(lambda v: 255 if v > ALPHA_MIN else 0).getbbox()


def trimmed(image):
    return image.crop(content_box(image))


def fit(image, size, margin):
    """The picture centred in a transparent canvas of `size`, as large as fits inside the margin."""
    scale = min((size[0] - 2 * margin) / image.width, (size[1] - 2 * margin) / image.height)
    image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return out


def seam_crop(image):
    """From the first plank seam to the last one, so the tile repeats with a seam at the join."""
    import numpy as np
    lum = np.asarray(image.convert("L"), dtype=np.float32).mean(axis=0)
    dark = np.nonzero(lum < lum.mean() - 12)[0]
    if len(dark) < 2:
        return image
    groups = np.split(dark, np.nonzero(np.diff(dark) > 8)[0] + 1)
    centres = [int(g.mean()) for g in groups]
    if len(centres) < 2:
        return image
    return image.crop((centres[0], 0, centres[-1], image.height))


def ring_position(image):
    """Fraction from the top where the bobber's dark ring is (darkest opaque row in the middle column band)."""
    import numpy as np
    a = np.asarray(image, dtype=np.float32)
    band = a[:, a.shape[1] // 3: 2 * a.shape[1] // 3]
    lum = np.where(band[..., 3] > 200, band[..., :3].mean(axis=2), 255).mean(axis=1)
    lum[: len(lum) // 4] = 255
    return float(np.argmin(lum)) / a.shape[0]


def save(image, name):
    image.save(os.path.join(OUT, name + ".png"), optimize=True)
    print(name + ".png", "%dx%d" % image.size)


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    folder = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)

    wall = load(folder, "ui_loja_parede_noite", transparent=False)
    if wall is not None:
        wall = seam_crop(wall)
        save(wall.resize((round(wall.width * WALL_HEIGHT / wall.height), WALL_HEIGHT), Image.LANCZOS), "ui_loja_parede_noite")

    hook = load(folder, "ui_loja_gancho")
    if hook is not None:
        save(fit(trimmed(hook), HOOK_SIZE, 2), "ui_loja_gancho")

    # 9-slice pieces: trimmed and scaled keeping their proportion (the code reads the size from the texture).
    counter = load(folder, "ui_loja_balcao_pier")
    if counter is not None:
        counter = trimmed(counter)
        save(counter.resize((round(counter.width * COUNTER_HEIGHT / counter.height), COUNTER_HEIGHT), Image.LANCZOS), "ui_loja_balcao_pier")

    box = load(folder, "ui_loja_caixa")
    if box is not None:
        box = trimmed(box)
        save(box.resize((BOX_WIDTH, round(box.height * BOX_WIDTH / box.width)), Image.LANCZOS), "ui_loja_caixa")

    bobber = load(folder, "ui_loja_boia")
    if bobber is not None:
        bobber = fit(trimmed(bobber), BOBBER_SIZE, 2)
        save(bobber, "ui_loja_boia")
        print("  anel da boia a %.3f da altura (de cima)" % ring_position(bobber))


if __name__ == "__main__":
    main()
