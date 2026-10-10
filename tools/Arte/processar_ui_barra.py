#!/usr/bin/env python3
"""Turns the owner's top bar and player card art (ChatGPT, 08/10/2026: FISHING_IDLE_UI_ULTIMOS_PEDIDOS) into
the game's UI files in client-unity/Assets/Resources/Arte/UI.

Every picture is trimmed to its content (alpha > 8, which drops the faint specks the generator leaves near the
edges) and resized with LANCZOS. The 9-slice pieces keep the sizes the code expects (UiSkin: NavArt*, TopBarArt*,
WalletArt*): change them together.

Usage: python3 tools/Arte/processar_ui_barra.py <folder with 02A_Botao_Menu_Normal.png ... 07_Area_Moedas.png>
"""
import os
import sys

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Arte", "UI")
ALPHA_MIN = 8

# Menu button: the face plus its lip is 112 px tall in both files; the active one keeps 12 px of glow around it.
NAV_BODY_HEIGHT = 112
NAV_GLOW = 12


def load(folder, name):
    image = Image.open(os.path.join(folder, name + ".png")).convert("RGBA")
    corners = [image.getpixel(p) for p in ((0, 0), (image.width - 1, 0), (0, image.height - 1), (image.width - 1, image.height - 1))]
    if any(c[3] > 0 for c in corners):
        sys.exit(name + ": the corners are not transparent (checkerboard or solid background?). Remove the background first.")
    return image


def content_box(image, threshold=ALPHA_MIN):
    return image.getchannel("A").point(lambda v: 255 if v > threshold else 0).getbbox()


def resize(image, size):
    return image.resize(size, Image.LANCZOS)


def by_height(image, height):
    return resize(image, (max(1, round(image.width * height / image.height)), height))


def square(image, size, margin):
    """Fits the trimmed picture in a transparent square, centred, with a margin."""
    image = image.crop(content_box(image))
    inner = size - 2 * margin
    scale = inner / max(image.size)
    image = resize(image, (max(1, round(image.width * scale)), max(1, round(image.height * scale))))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(image, ((size - image.width) // 2, (size - image.height) // 2))
    return out


def save(image, name):
    image.save(os.path.join(OUT, name + ".png"), optimize=True)
    print(name + ".png", "%dx%d" % image.size)


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    folder = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)

    # Menu button, normal: trimmed, 112 px tall (face + lip), width follows (9-slice: 32 px sides).
    normal = load(folder, "02A_Botao_Menu_Normal")
    save(by_height(normal.crop(content_box(normal)), NAV_BODY_HEIGHT), "ui_nav_button_base")

    # Menu button, active: the solid body (alpha > 240) sets the scale; the glow keeps a fixed margin around it.
    active = load(folder, "02B_Botao_Menu_Ativo")
    body = content_box(active, 240)
    scale = NAV_BODY_HEIGHT / (body[3] - body[1])
    pad = round(NAV_GLOW / scale)
    glow = active.crop((body[0] - pad, body[1] - pad, body[2] + pad, body[3] + pad))
    save(resize(glow, (round(glow.width * scale), NAV_BODY_HEIGHT + 2 * NAV_GLOW)), "ui_nav_button_active")

    # Top bar frame: 160 px tall, width follows (9-slice horizontal: 90 px ends).
    bar = load(folder, "03_Barra_Superior")
    save(by_height(bar.crop(content_box(bar)), 160), "ui_topbar_frame")

    # Logo: 720 px wide, real proportion.
    logo = load(folder, "04_Logo_Fishing_Idle")
    logo = logo.crop(content_box(logo))
    save(resize(logo, (720, round(logo.height * 720 / logo.width))), "ui_logo_fishing_idle")

    # Avatar frame: a square centred on the ring's hole (the notch at the bottom makes the bbox off-centre),
    # so the code can centre the frame on the avatar.
    ring = load(folder, "05_Moldura_Avatar")
    alpha = ring.getchannel("A")
    box = content_box(ring)
    cy = (box[1] + box[3]) // 2
    cx = (box[0] + box[2]) // 2
    row = [x for x in range(ring.width) if alpha.getpixel((x, cy)) > 128]
    col = [y for y in range(ring.height) if alpha.getpixel((cx, y)) > 128]
    hole_x = [row[i] for i in range(1, len(row)) if row[i] - row[i - 1] > 1][0], [row[i - 1] for i in range(1, len(row)) if row[i] - row[i - 1] > 1][0]
    hole_y = [col[i] for i in range(1, len(col)) if col[i] - col[i - 1] > 1][0], [col[i - 1] for i in range(1, len(col)) if col[i] - col[i - 1] > 1][0]
    hx = (hole_x[0] + hole_x[1]) / 2
    hy = (hole_y[0] + hole_y[1]) / 2
    half = max(hx - box[0], box[2] - hx, hy - box[1], box[3] - hy) + 2
    framed = ring.crop((round(hx - half), round(hy - half), round(hx + half), round(hy + half)))
    save(resize(framed, (192, 192)), "ui_avatar_frame")
    print("  hole: %.3f of the frame" % ((hole_x[0] - hole_x[1]) / (2 * half)))

    # Currencies: coloured, 128 px square with a 4 px margin.
    for src, name in (("06A_Moeda", "ui_currency_coin"), ("06B_Concha", "ui_currency_shell"), ("06C_Dolar", "ui_currency_dollar")):
        save(square(load(folder, src), 128, 4), name)

    # Wallet inset: 128 px tall, width follows (9-slice: 16 px on every side).
    inset = load(folder, "07_Area_Moedas")
    save(by_height(inset.crop(content_box(inset)), 128), "ui_wallet_inset")


if __name__ == "__main__":
    main()
