#!/usr/bin/env python3
"""
Fixes the Abismo Atlântico scenery (owner's request, 08/10/2026, addendum A-138). Run ONCE on the
files produced by processar_mapas_9_10.py (running it again would brighten twice):

    python3 tools/Arte/melhorar_abismo.py

- Sky: the night sky was continued sideways with mirrored copies of the painting, so the milky way
  formed a big "V". The copies now keep their stars but lose the milky way glow, which fades out near
  the edges of the original painting. The top rows, which were one row stretched upwards (vertical
  streaks), are rebuilt from the rows below.
- Sky, water and clouds: lifted out of near-black (gamma + gain), with a soft blue glow over the
  horizon, keeping the night look.
"""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

FOLDER = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Arte", "Mapas", "AbismoAtlantico")
PREFIX = "map_abismo_atlantico_"


def path(layer):
    return os.path.join(FOLDER, PREFIX + layer + ".png")


def seams(lum, k=40):
    """Columns where the picture is mirrored (copy | original | copy)."""
    w = lum.shape[1]
    scores = []
    for c in range(k + 10, w - k - 10):
        scores.append((np.abs(lum[:, c - k:c][:, ::-1] - lum[:, c:c + k]).mean(), c))
    scores.sort()
    found = []
    for d, c in scores:
        if d > 0.5:
            break
        if all(abs(c - f) > 50 for f in found):
            found.append(c)
    return sorted(found)


def lift(rgb, gamma, gain):
    return np.clip(255.0 * gain * (np.clip(rgb, 0, 255) / 255.0) ** gamma, 0, 255)


def fix_sky():
    img = Image.open(path("bg_sky")).convert("RGBA")
    a = np.asarray(img, dtype=np.float32)
    rgb = a[..., :3].copy()
    h, w, _ = rgb.shape

    # 1. Top rows stretched from one row: rebuild them by reflecting the rows below.
    top = 0
    while top < h - 1 and np.abs(rgb[top] - rgb[top + 1]).mean() < 0.01:
        top += 1
    for r in range(top):
        rgb[r] = rgb[min(h - 1, 2 * top - r)]

    # 2. The milky way only in the original painting, fading out towards its edges. Outside it (where
    #    the old mirrored copies made a "V"), a plain night sky with stars cut from the painting.
    cuts = seams(rgb.sum(axis=2))
    low = ndimage.gaussian_filter(rgb, (28, 28, 0))
    background = ndimage.gaussian_filter(np.percentile(low, 20, axis=1, keepdims=True), (12, 0, 0))
    background = np.repeat(background, w, axis=1)
    keep = np.ones(w, dtype=np.float32)
    if len(cuts) >= 2:
        a0, a1 = cuts[0], cuts[-1]
        feather = 0.22 * (a1 - a0)
        x = np.arange(w, dtype=np.float32)
        keep = np.clip(np.minimum(x - a0, a1 - x) / feather, 0, 1)
        keep = keep * keep * (3 - 2 * keep)

        # Star stamps from the quiet part of the painting (away from the milky way and the moon).
        lum = rgb.sum(axis=2)
        glow = (low - background).sum(axis=2)
        quiet = np.zeros_like(lum, dtype=bool)
        quiet[:, a0:a1] = glow[:, a0:a1] < np.percentile(glow[:, a0:a1], 40)
        peaks = (lum == ndimage.maximum_filter(lum, 7)) & (lum > background.sum(axis=2) + 45) & quiet
        ys, xs = np.nonzero(peaks)
        ok = (ys > 4) & (ys < h - 5) & (xs > 4) & (xs < w - 5)
        ys, xs = ys[ok], xs[ok]
        stamps = [rgb[y - 4:y + 5, x - 4:x + 5] - background[y - 4:y + 5, x - 4:x + 5] for y, x in zip(ys, xs)]
        density = len(stamps) / max(1.0, float(quiet.sum()))

        field = background.copy()
        rng = np.random.default_rng(10)
        count = int(density * h * w)
        for _ in range(count):
            y, x = int(rng.integers(4, h - 5)), int(rng.integers(4, w - 5))
            st = stamps[int(rng.integers(0, len(stamps)))]
            field[y - 4:y + 5, x - 4:x + 5] = np.maximum(field[y - 4:y + 5, x - 4:x + 5], background[y - 4:y + 5, x - 4:x + 5] + st)
        rgb = rgb * keep[None, :, None] + field * (1 - keep)[None, :, None]

    # 3. Out of near-black, with a soft blue glow over the horizon.
    rgb = lift(rgb, 0.82, 1.28)
    rows = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    glow = np.array([18, 30, 62], dtype=np.float32) * rows ** 2.2
    rgb = np.clip(rgb + glow, 0, 255)

    a[..., :3] = rgb
    Image.fromarray(a.astype(np.uint8), "RGBA").save(path("bg_sky"), optimize=True)
    print("sky: top rows rebuilt =", top, "· seams =", cuts)


def fix_layer(layer, gamma, gain, glow_top=None):
    img = Image.open(path(layer)).convert("RGBA")
    a = np.asarray(img, dtype=np.float32)
    rgb = lift(a[..., :3], gamma, gain)
    if glow_top is not None:
        h = rgb.shape[0]
        rows = np.linspace(1, 0, h, dtype=np.float32)[:, None, None]
        rgb = np.clip(rgb + np.array(glow_top, dtype=np.float32) * rows ** 3, 0, 255)
    a[..., :3] = rgb
    Image.fromarray(a.astype(np.uint8), "RGBA").save(path(layer), optimize=True)
    print(layer, "lifted")


def main():
    fix_sky()
    # The water reflects the lit horizon near the top and stays deep below.
    fix_layer("water", 0.78, 1.45, glow_top=(14, 26, 52))
    for c in ("cloud_01", "cloud_02", "cloud_03"):
        fix_layer(c, 0.85, 1.25)
    for layer in ("bg_far", "bg_mid", "near_left", "near_right"):
        fix_layer(layer, 0.9, 1.15)
    # The picture on the map list follows the brighter scene.
    fix_layer("thumb", 0.85, 1.2)


if __name__ == "__main__":
    main()
