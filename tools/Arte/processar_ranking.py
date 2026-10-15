#!/usr/bin/env python3
"""Turns the owner's Ranking art (ChatGPT, 08/10/2026: FISHING_IDLE_RANKING_20_ASSETS) into the game's UI files in
client-unity/Assets/Resources/Arte/UI (addendum A-146).

Only the pieces the Ranking uses are processed: the crown, the 4 category emblems, the pier stage, the 4 shields and
the trophy. The up/down arrows (12A/12B) and the wood and plaques (13 to 19) are not used (owner's decision).

Every picture is trimmed to its content (alpha > 8, which drops the faint specks the generator leaves near the
edges) and resized with LANCZOS. Sizes are texture pixels; RankingWindow draws them smaller.

Usage: python3 tools/Arte/processar_ranking.py <folder with 01_ico_coroa.png ... 11_rk_trofeu_placar.png>
"""
import os
import sys

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Arte", "UI")
ALPHA_MIN = 8

STAGE_WIDTH = 1200
SHIELD_SIZE = (96, 108)


def load(folder, name):
    image = Image.open(os.path.join(folder, name + ".png")).convert("RGBA")
    corners = [image.getpixel(p) for p in ((0, 0), (image.width - 1, 0), (0, image.height - 1), (image.width - 1, image.height - 1))]
    if any(c[3] > 0 for c in corners):
        sys.exit(name + ": the corners are not transparent (checkerboard or solid background?). Remove the background first.")
    return image


def trimmed(image):
    return image.crop(image.getchannel("A").point(lambda v: 255 if v > ALPHA_MIN else 0).getbbox())


def fit(image, size, margin):
    """The trimmed picture centred in a transparent canvas of `size`, as large as fits inside the margin."""
    image = trimmed(image)
    scale = min((size[0] - 2 * margin) / image.width, (size[1] - 2 * margin) / image.height)
    image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return out


def save(image, name):
    image.save(os.path.join(OUT, name + ".png"), optimize=True)
    print(name + ".png", "%dx%d" % image.size)


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    folder = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)

    # Crown and the 4 category emblems: 128 px square, 4 px margin.
    save(fit(load(folder, "01_ico_coroa"), (128, 128), 4), "ui_rk_coroa")
    for src, name in (("02_rk_emblema_nivel", "ui_rk_emblema_nivel"), ("03_rk_emblema_moedas", "ui_rk_emblema_moedas"),
                      ("04_rk_emblema_conchas", "ui_rk_emblema_conchas"), ("05_rk_emblema_peixes", "ui_rk_emblema_peixes")):
        save(fit(load(folder, src), (128, 128), 4), name)

    # Pier stage: trimmed, 1200 px wide, real proportion.
    stage = trimmed(load(folder, "06_rk_palco_pier"))
    save(stage.resize((STAGE_WIDTH, round(stage.height * STAGE_WIDTH / stage.width)), Image.LANCZOS), "ui_rk_palco")

    # Shields: every one on the same 96x108 canvas, so the number the code writes sits in the same place.
    for src, name in (("07_rk_escudo_ouro", "ui_rk_escudo_ouro"), ("08_rk_escudo_prata", "ui_rk_escudo_prata"),
                      ("09_rk_escudo_bronze", "ui_rk_escudo_bronze"), ("10_rk_escudo_azul", "ui_rk_escudo_azul")):
        save(fit(load(folder, src), SHIELD_SIZE, 2), name)

    # Trophy for the header: 192 px square, 4 px margin.
    save(fit(load(folder, "11_rk_trofeu_placar"), (192, 192), 4), "ui_rk_trofeu")


if __name__ == "__main__":
    main()
