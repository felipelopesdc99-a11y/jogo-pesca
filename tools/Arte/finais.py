"""Keeps the owner's final art safe from the placeholder generators.

tools/Arte/finais.txt lists the art files that are final (written by processar_pedidos.py). Calling
proteger_finais() makes every later PIL save to one of those paths a no-op, so re-running a
placeholder generator never overwrites final art.
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.normpath(os.path.join(HERE, '..', '..', 'client-unity', 'Assets', 'Resources', 'Arte'))


def lista():
    path = os.path.join(HERE, 'finais.txt')
    if not os.path.exists(path):
        return set()
    return {l.strip() for l in open(path, encoding='utf-8') if l.strip() and not l.startswith('#')}


def proteger_finais():
    finals = lista()
    original = Image.Image.save

    def guarded(self, fp, *args, **kwargs):
        if isinstance(fp, str):
            rel = os.path.relpath(os.path.abspath(fp), ART).replace(os.sep, '/')
            if rel in finals:
                return None
        return original(self, fp, *args, **kwargs)

    Image.Image.save = guarded
    return finals
