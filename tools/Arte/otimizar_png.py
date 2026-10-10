"""Shrinks every PNG of the game without changing a single pixel (lossless, TD-031).

Usage:  python3 tools/Arte/otimizar_png.py [pasta]     (default: client-unity/Assets/Resources/Arte)

Run it after any of the art scripts (processar_*, gerar_*). It uses oxipng (pip install pyoxipng):
better compression, opaque pictures stored without the alpha channel, and no extra metadata. The
picture Unity imports is exactly the same, so this only makes the repository and the download smaller.
"""
import os
import sys

try:
    import oxipng
except ImportError:
    sys.exit('Falta o oxipng: rode  pip install pyoxipng  e tente de novo.')

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DEFAULT = os.path.join(ROOT, 'client-unity', 'Assets', 'Resources', 'Arte')


def main():
    folder = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT
    before = after = files = 0
    for base, _, names in os.walk(folder):
        for name in sorted(names):
            if not name.lower().endswith('.png'):
                continue
            path = os.path.join(base, name)
            size = os.path.getsize(path)
            oxipng.optimize(path, level=4, strip=oxipng.StripChunks.safe())
            before += size
            after += os.path.getsize(path)
            files += 1
    mb = 1024 * 1024
    print('%d imagens: %s MB -> %s MB (%s MB a menos), sem mudar nenhum pixel.' % (
        files, ('%.1f' % (before / mb)).replace('.', ','), ('%.1f' % (after / mb)).replace('.', ','),
        ('%.1f' % ((before - after) / mb)).replace('.', ',')))


if __name__ == '__main__':
    main()
