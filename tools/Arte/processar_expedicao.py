#!/usr/bin/env python3
"""Turns the owner's Expedition art (ChatGPT, 08/10/2026: nautical chart, mission card and harbour examples) into
the game's UI files in client-unity/Assets/Resources/Arte/UI (ui_exp_*.png).

The transparent pictures are trimmed to their content (alpha > 8, which drops the faint specks the generator
leaves near the edges) and resized with LANCZOS. The two scenes (chart and harbour) are opaque and only resized.
The code reads the holes of the frames as fractions (ExpeditionWindow: MedallionHole, CardInset, CompassDial,
SealFace): the script prints the first two so they can be checked when the art changes.

The destination sign (placa de destino) is not used yet and is not copied.

Usage: python3 tools/Arte/processar_expedicao.py <folder with the owner's files>
The folder holds the files named as in FILES below (the names the uploads arrived with).
"""
import os
import sys

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Arte", "UI")
ALPHA_MIN = 8

# Source file for each piece. The uploads came with random names; the content decides which is which.
FILES = {
    "carta": "08def8a3-image.png",       # nautical chart, opaque
    "medalhao": "2d977f0f-image.png",    # brass porthole ring, transparent centre
    "rosa": "51404501-image.png",        # compass rose
    "marcador": "37e03d09-image.png",    # Cardume map pin
    "pier": "a4681193-image.png",        # pier seen from above
    "moldura_carta": "51b67233-image.png",  # mission card frame, transparent centre
    "selo": "074b4cde-image.png",        # fish chance seal, plain face
    "porto": "a88da3aa-image.png",       # harbour at dusk, opaque
    "bussola": "7d5c7abf-image.png",     # open pocket compass, empty dial
}


def load(folder, key, transparent=True):
    image = Image.open(os.path.join(folder, FILES[key])).convert("RGBA" if transparent else "RGB")
    if transparent:
        corners = [image.getpixel(p) for p in ((0, 0), (image.width - 1, 0), (0, image.height - 1), (image.width - 1, image.height - 1))]
        if any(c[3] > 0 for c in corners):
            sys.exit(key + ": the corners are not transparent. Remove the background first.")
    return image


def content_box(image, threshold=ALPHA_MIN):
    return image.getchannel("A").point(lambda v: 255 if v > threshold else 0).getbbox()


def trimmed(image):
    return image.crop(content_box(image))


def resize(image, size):
    return image.resize(size, Image.LANCZOS)


def by_width(image, width):
    return resize(image, (width, max(1, round(image.height * width / image.width))))


def by_height(image, height):
    return resize(image, (max(1, round(image.width * height / image.height)), height))


def square(image, size, margin=2):
    """Fits the trimmed picture in a transparent square, centred, with a margin."""
    image = trimmed(image)
    inner = size - 2 * margin
    scale = inner / max(image.size)
    image = resize(image, (max(1, round(image.width * scale)), max(1, round(image.height * scale))))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(image, ((size - image.width) // 2, (size - image.height) // 2))
    return out


def save(image, name):
    image.save(os.path.join(OUT, name + ".png"), optimize=True)
    print(name + ".png", "%dx%d" % image.size)


def hole(image, cx, cy, horizontal=True, solid=128):
    """The see-through (or dark) run through (cx, cy) along a line: returns (start, end) in pixels."""
    alpha = image.getchannel("A")
    if horizontal:
        line = [alpha.getpixel((x, cy)) > solid for x in range(image.width)]
        c = cx
    else:
        line = [alpha.getpixel((cx, y)) > solid for y in range(image.height)]
        c = cy
    a = c
    while a > 0 and not line[a - 1]:
        a -= 1
    b = c
    while b < len(line) - 1 and not line[b + 1]:
        b += 1
    return a, b


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    folder = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)

    # The two scenes: opaque, 1600 px wide.
    save(by_width(load(folder, "carta", False), 1600), "ui_exp_carta")
    save(by_width(load(folder, "porto", False), 1600), "ui_exp_porto")

    # Square pieces, centred.
    save(square(load(folder, "rosa"), 256), "ui_exp_rosa")
    save(square(load(folder, "pier"), 256), "ui_exp_pier")
    save(square(load(folder, "marcador"), 128), "ui_exp_marcador")
    save(square(load(folder, "selo"), 160), "ui_exp_selo")

    medallion = square(load(folder, "medalhao"), 256)
    save(medallion, "ui_exp_medalhao")
    a, b = hole(medallion, 128, 128)
    print("  medallion hole: %.3f of the frame (diameter)" % ((b - a + 1) / 256))

    # Mission card frame: about 400 x 600, real proportion.
    card = by_height(trimmed(load(folder, "moldura_carta")), 600)
    save(card, "ui_exp_moldura_carta")
    a, b = hole(card, card.width // 2, card.height // 2)
    c, d = hole(card, card.width // 2, card.height // 2, horizontal=False)
    print("  card inset: left %.3f right %.3f top %.3f bottom %.3f" % (a / card.width, 1 - (b + 1) / card.width, c / card.height, 1 - (d + 1) / card.height))

    # Compass: 320 px tall, real proportion. The dial (dark disc) and the seal's plain face were measured by eye
    # (ExpeditionWindow.CompassDial and SealFace): check them if this art changes.
    compass = by_height(trimmed(load(folder, "bussola")), 320)
    save(compass, "ui_exp_bussola")

if __name__ == "__main__":
    main()
