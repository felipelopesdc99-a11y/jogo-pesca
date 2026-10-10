#!/usr/bin/env python3
"""
Builds the map ambience recordings (Resources/Sons/Ambiente/*.ogg) from real nature recordings.

Source: the sound files of the open-source project Moodist (github.com/remvze/moodist,
public/sounds), which its README lists as CC0 or Pixabay Content License — both allow use inside a
game without attribution. See docs/ASSETS_PENDENTES.md (Som ambiente) for the license note.

Usage:
    git clone --depth 1 --filter=blob:none --sparse https://github.com/remvze/moodist <dir>
    git -C <dir> sparse-checkout set public/sounds/nature public/sounds/animals
    python3 tools/Audio/mixar_ambiente.py <dir>/public/sounds

Each map gets one ~150 s track: a soft bed (water, wind, insects) plus a few sparse calls (birds,
gulls, whales) placed at seeded random times. Loud or harsh layers are low-passed and kept quiet:
the ambience sits far behind the game sounds. The playlist crossfades the track into itself.
"""
import os
import subprocess
import sys

import numpy as np

RATE = 44100
LENGTH_S = 150
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "client-unity", "Assets", "Resources", "Sons", "Ambiente")

# (file, gain, lowpass Hz or None)
BEDS = {
    "lago":        [("nature/river.mp3", 0.22, 2500), ("animals/birds.mp3", 0.55, None), ("nature/wind-in-trees.mp3", 0.25, None)],
    "rio":         [("nature/river.mp3", 0.45, 4000), ("animals/birds.mp3", 0.45, None)],
    "pantanal":    [("animals/frog.mp3", 0.35, None), ("animals/birds.mp3", 0.40, None), ("nature/jungle.mp3", 0.30, None)],
    "estuario":    [("nature/waves.mp3", 0.35, 1800), ("nature/wind.mp3", 0.15, 1500)],
    "coral":       [("nature/waves.mp3", 0.40, 2200), ("nature/wind-in-trees.mp3", 0.18, None)],
    "arquipelago": [("nature/waves.mp3", 0.35, 2000), ("nature/wind-in-trees.mp3", 0.25, None), ("animals/birds.mp3", 0.20, None)],
    "corrente":    [("nature/waves.mp3", 0.40, 1600), ("nature/wind.mp3", 0.22, 1200)],
    "baleias":     [("nature/waves.mp3", 0.35, 1400), ("nature/wind.mp3", 0.15, 1000)],
    "talude":      [("nature/waves.mp3", 0.28, 1100), ("nature/wind.mp3", 0.20, 900)],
    "abismo":      [("nature/waves.mp3", 0.22, 800), ("nature/wind.mp3", 0.25, 700)],
}

# (file, gain, how many times in the track)
CALLS = {
    "estuario":    [("animals/seagulls.mp3", 0.30, 3)],
    "coral":       [("animals/seagulls.mp3", 0.30, 3)],
    "arquipelago": [("animals/seagulls.mp3", 0.25, 2)],
    "corrente":    [("animals/seagulls.mp3", 0.18, 1)],
    "baleias":     [("animals/whale.mp3", 0.35, 3)],
    "talude":      [("animals/seagulls.mp3", 0.10, 1)],
    "abismo":      [("animals/whale.mp3", 0.25, 2)],
}


def decode(path, lowpass=None):
    filters = ["aresample=%d" % RATE]
    if lowpass:
        filters.append("lowpass=f=%d" % lowpass)
    raw = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", path, "-af", ",".join(filters), "-ac", "2", "-f", "f32le", "-"],
        check=True, capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()


def looped(clip, frames, fade_s=3.0):
    """Repeats a clip to `frames` long, crossfading each seam so the loop never clicks."""
    fade = int(fade_s * RATE)
    if len(clip) <= fade * 2:
        return np.resize(clip, (frames, 2))
    ramp = np.linspace(0, 1, fade, dtype=np.float32)[:, None]
    out = clip.copy()
    while len(out) < frames:
        head = clip[:fade] * ramp
        out[-fade:] = out[-fade:] * (1 - ramp) + head
        out = np.concatenate([out, clip[fade:]])
    return out[:frames]


def place(track, clip, gain, count, rng):
    fade = int(0.8 * RATE)
    clip = clip.copy()
    if len(clip) > fade * 2:
        ramp = np.linspace(0, 1, fade, dtype=np.float32)[:, None]
        clip[:fade] *= ramp
        clip[-fade:] *= ramp[::-1]
    span = len(track) - len(clip) - 10 * RATE
    if span <= 0 or count <= 0:
        return
    slots = np.linspace(5 * RATE, 5 * RATE + span, count + 1)
    for i in range(count):
        start = int(rng.uniform(slots[i], slots[i + 1]))
        pan = rng.uniform(0.6, 1.0)
        side = np.array([pan, 1.0], dtype=np.float32) if rng.random() < 0.5 else np.array([1.0, pan], dtype=np.float32)
        track[start:start + len(clip)] += clip * gain * side


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    src = sys.argv[1]
    os.makedirs(OUT_DIR, exist_ok=True)
    frames = LENGTH_S * RATE
    for i, (name, layers) in enumerate(BEDS.items()):
        rng = np.random.default_rng(1000 + i)
        track = np.zeros((frames, 2), dtype=np.float32)
        for file, gain, lp in layers:
            if gain <= 0:
                continue
            clip = decode(os.path.join(src, file), lp)
            offset = int(rng.uniform(0, max(1, len(clip) - 1)))
            clip = np.concatenate([clip[offset:], clip[:offset]])
            track += looped(clip, frames) * gain
        for file, gain, count in CALLS.get(name, []):
            if gain > 0 and count > 0:
                place(track, decode(os.path.join(src, file)), gain, count, rng)

        # Calm, consistent level across maps (RMS about -26 dBFS), never clipping; soft ends.
        rms = float(np.sqrt(np.mean(track ** 2))) or 1.0
        track *= (10 ** (-26 / 20)) / rms
        peak = float(np.max(np.abs(track)))
        if peak > 0.89:
            track *= 0.89 / peak
        edge = int(4 * RATE)
        ramp = np.linspace(0, 1, edge, dtype=np.float32)[:, None]
        track[:edge] *= ramp
        track[-edge:] *= ramp[::-1]

        out = os.path.join(OUT_DIR, name + "_01.ogg")
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(RATE), "-ac", "2", "-i", "-",
                        "-c:a", "libvorbis", "-q:a", "2", out], input=track.astype(np.float32).tobytes(), check=True)
        print("%-12s -> %s (%.1f MB)" % (name, os.path.basename(out), os.path.getsize(out) / 1e6))


if __name__ == "__main__":
    main()
