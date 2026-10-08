#!/usr/bin/env python3
"""Turns the owner's menu icons (ChatGPT, 08/10/2026: FISHING_IDLE_MENU_ICONES_11) into the game's icon
files: white on transparent, trimmed, centred in 96x96 with a 6 px margin (same frame as the old family).

Usage: python3 tools/Arte/processar_icones_menu.py <folder with 01_Pesca.png ... 11_Opcoes.png>
"""
import os
import sys

from PIL import Image

NAMES = {
    "01_Pesca": "pesca", "02_Mapa": "mapa", "03_Aquario": "aquario", "04_Arena": "arena", "05_Ranking": "ranking",
    "06_Expedicao": "expedicao", "07_Mercado": "mercado", "08_Loja": "loja", "09_Perfil": "perfil",
    "10_Avisos": "avisos", "11_Opcoes": "opcoes",
}
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Arte", "Icones")
SIZE, MARGIN = 96, 6


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    for src, name in NAMES.items():
        path = os.path.join(sys.argv[1], src + ".png")
        alpha = Image.open(path).convert("RGBA").getchannel("A")
        alpha = alpha.crop(alpha.getbbox())
        inner = SIZE - 2 * MARGIN
        scale = inner / max(alpha.size)
        alpha = alpha.resize((max(1, round(alpha.width * scale)), max(1, round(alpha.height * scale))), Image.LANCZOS)
        a = Image.new("L", (SIZE, SIZE), 0)
        a.paste(alpha, ((SIZE - alpha.width) // 2, (SIZE - alpha.height) // 2))
        out = Image.merge("LA", (Image.new("L", (SIZE, SIZE), 255), a))
        out.save(os.path.join(OUT, "ico_" + name + ".png"), optimize=True)
        print(src, "->", "ico_" + name + ".png")


if __name__ == "__main__":
    main()
