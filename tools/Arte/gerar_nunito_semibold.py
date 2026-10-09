#!/usr/bin/env python3
"""Makes client-unity/Assets/Resources/Fontes/Nunito-SemiBold.ttf (weight 600) from the variable Nunito font.

The project ships Nunito Regular and Bold as static files (v3.602). The interface's body text uses SemiBold since
the legibility pass of 08/10/2026 (GDD_ADENDO A-149, TD-037): Regular came out too thin once the 1080-tall virtual
canvas is scaled down to 600-840 px screens. Google Fonts only publishes Nunito as a variable font, so the
SemiBold file is a static instance of it at wght = 600, with the names set to "Nunito SemiBold". Same license
(SIL OFL 1.1, OFL-Nunito.txt in the same folder; Nunito has no reserved font name).

Usage: python3 tools/Arte/gerar_nunito_semibold.py <Nunito[wght].ttf>
The variable font is at github.com/google/fonts, ofl/nunito/Nunito[wght].ttf. Needs fontTools (pip install fonttools).
"""
import os
import sys

from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Fontes", "Nunito-SemiBold.ttf")


def main():
    if len(sys.argv) != 2:
        print("Uso: python3 tools/Arte/gerar_nunito_semibold.py <arquivo Nunito[wght].ttf>")
        sys.exit(1)

    font = TTFont(sys.argv[1])
    if "fvar" not in font:
        print("Este arquivo não é a fonte variável da Nunito (falta a tabela fvar).")
        sys.exit(1)

    semibold = instancer.instantiateVariableFont(font, {"wght": 600}, updateFontNames=True)
    semibold["OS/2"].usWeightClass = 600
    semibold.save(OUT)
    names = semibold["name"]
    print("Gerado:", os.path.normpath(OUT))
    print("Nome:", names.getDebugName(4), "· versão:", names.getDebugName(5))


if __name__ == "__main__":
    main()
