#!/usr/bin/env python3
"""Turns the Shop art requested in docs/ASSETS_PENDENTES.md (section "Loja 'Balcão do Píer'", addendum A-150) into the
game's UI files in client-unity/Assets/Resources/Arte/UI.

Expected in the folder, as the generator gave them (any size in the right proportion):
  ui_loja_parede_noite.png  opaque plank wall, repeats sideways        -> 512x768, centre-cropped to 2:3, no trim
  ui_loja_gancho.png        brass hook                                  -> 64x64, trimmed, 2 px margin
  ui_loja_balcao_pier.png   counter plank (9-slice, sides 48 px)        -> 1024x96, trimmed, stretched to size
  ui_loja_caixa.png         open tackle box (9-slice, borders 40 px)    -> 512x288, trimmed, stretched to size
  ui_loja_boia.png          bobber, the dark ring at half the height    -> 64x96, trimmed symmetrically (keeps the ring
                                                                           in the middle), 2 px margin

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

WALL_SIZE = (512, 768)
HOOK_SIZE = (64, 64)
COUNTER_SIZE = (1024, 96)
BOX_SIZE = (512, 288)
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


def trimmed_symmetric(image):
    """Trimmed by the same amount on opposite sides, so the centre of the picture stays the centre."""
    left, top, right, bottom = content_box(image)
    dx = min(left, image.width - right)
    dy = min(top, image.height - bottom)
    return image.crop((dx, dy, image.width - dx, image.height - dy))


def fit(image, size, margin):
    """The picture centred in a transparent canvas of `size`, as large as fits inside the margin."""
    scale = min((size[0] - 2 * margin) / image.width, (size[1] - 2 * margin) / image.height)
    image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return out


def centre_crop(image, ratio):
    """The largest centred piece with width / height = ratio."""
    if image.width / image.height > ratio:
        w = round(image.height * ratio)
        x = (image.width - w) // 2
        return image.crop((x, 0, x + w, image.height))
    h = round(image.width / ratio)
    y = (image.height - h) // 2
    return image.crop((0, y, image.width, y + h))


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
        save(centre_crop(wall, WALL_SIZE[0] / WALL_SIZE[1]).resize(WALL_SIZE, Image.LANCZOS), "ui_loja_parede_noite")

    hook = load(folder, "ui_loja_gancho")
    if hook is not None:
        save(fit(trimmed(hook), HOOK_SIZE, 2), "ui_loja_gancho")

    # 9-slice pieces: trimmed, then stretched to the exact size the code's borders are measured on.
    counter = load(folder, "ui_loja_balcao_pier")
    if counter is not None:
        save(trimmed(counter).resize(COUNTER_SIZE, Image.LANCZOS), "ui_loja_balcao_pier")

    box = load(folder, "ui_loja_caixa")
    if box is not None:
        save(trimmed(box).resize(BOX_SIZE, Image.LANCZOS), "ui_loja_caixa")

    bobber = load(folder, "ui_loja_boia")
    if bobber is not None:
        save(fit(trimmed_symmetric(bobber), BOBBER_SIZE, 2), "ui_loja_boia")


if __name__ == "__main__":
    main()
