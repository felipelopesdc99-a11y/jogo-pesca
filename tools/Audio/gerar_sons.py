#!/usr/bin/env python3
"""
Generates the placeholder sounds of Fishing Idle (V0.2) as WAV files in
client-unity/Assets/Resources/Sons. The game loads them by name, so any file can be replaced by a
real recording with the same name and nothing else changes.

Everything is synthesised from a fixed seed (the output is identical on every run):
  - ambiente_mar:   40 s seamless loop, slow waves that swell, break softly and recede, stereo.
  - ambiente_vento: 40 s seamless loop, a very soft breeze with slow gusts, stereo.
  - captura, captura_rara, recorde, subir_nivel, moedas, importante, aviso, clique: short effects.

Needs Python 3 with numpy and scipy:  pip install numpy scipy
Run from the repository root:          python3 tools/Audio/gerar_sons.py
"""
import os
import sys
import wave

import numpy as np
from scipy import signal

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "client-unity", "Assets", "Resources", "Sons")
RNG = np.random.default_rng(20260929)

AMBIENT_RATE = 22050
EFFECT_RATE = 44100


# ---------------------------------------------------------------- helpers

def write_wav(name, data, rate):
    """data: float array in [-1, 1], shape (n,) mono or (n, 2) stereo."""
    data = np.clip(data, -1.0, 1.0)
    channels = 1 if data.ndim == 1 else data.shape[1]
    pcm = (data * 32767.0).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm.tobytes())
    return path


def db(x):
    return 20 * np.log10(max(1e-9, x))


def rms(x):
    return float(np.sqrt(np.mean(np.square(x))))


def normalize_rms(x, target_db):
    return x * (10 ** (target_db / 20) / max(1e-9, rms(x)))


def limit_peak(x, peak_db):
    peak = np.max(np.abs(x))
    limit = 10 ** (peak_db / 20)
    return x * (limit / peak) if peak > limit else x


def band(x, rate, low, high, order=2):
    sos = signal.butter(order, [low, high], btype="bandpass", fs=rate, output="sos")
    return signal.sosfilt(sos, x)


def lowpass(x, rate, cutoff, order=2):
    return signal.sosfilt(signal.butter(order, cutoff, btype="lowpass", fs=rate, output="sos"), x)


def highpass(x, rate, cutoff, order=2):
    return signal.sosfilt(signal.butter(order, cutoff, btype="highpass", fs=rate, output="sos"), x)


def pink(n):
    """Pink noise (−3 dB/octave) by shaping white noise in the frequency domain."""
    spectrum = np.fft.rfft(RNG.standard_normal(n))
    f = np.arange(len(spectrum))
    f[0] = 1
    return np.fft.irfft(spectrum / np.sqrt(f), n)


def smooth_random(n, rate, changes_per_second):
    """A slow random curve in [0, 1] (smoothed noise), for gusts and swells."""
    points = max(4, int(n / rate * changes_per_second) + 4)
    knots = RNG.random(points)
    curve = signal.resample(knots, n + n // 4)[: n]
    curve -= curve.min()
    return curve / max(1e-9, curve.max())


def seamless(x, loop_len, fade_len):
    """x has loop_len + fade_len samples; the tail is cross-faded into the head so the loop has no seam."""
    head = x[:fade_len]
    tail = x[loop_len : loop_len + fade_len]
    t = np.linspace(0, 1, fade_len)
    shape = (slice(None),) + (None,) * (x.ndim - 1)
    w_in = np.sin(t * np.pi / 2)[shape]
    w_out = np.cos(t * np.pi / 2)[shape]
    out = x[:loop_len].copy()
    out[:fade_len] = head * w_in + tail * w_out
    return out


def envelope(n, rate, attack, decay):
    t = np.arange(n) / rate
    return np.minimum(1.0, t / max(1e-4, attack)) * np.exp(-t / max(1e-4, decay))


# ---------------------------------------------------------------- ambience

def sea():
    rate, seconds, fade = AMBIENT_RATE, 40.0, 3.0
    loop = int(rate * seconds)
    n = loop + int(rate * fade)
    out = np.zeros((n, 2))

    # A quiet, constant bed of distant water: pink noise, low-passed, different per ear.
    for ch in range(2):
        bed = lowpass(highpass(pink(n), rate, 60), rate, 900, order=4)
        out[:, ch] += normalize_rms(bed, -35)

    # Waves every 6–9 s. Each: a low swell that builds, a soft wash that breaks, a fizz that recedes.
    t = 0.5
    while t < seconds + fade - 1:
        start = int(t * rate)
        length = int(rate * 7.5)
        pan = RNG.uniform(-0.35, 0.35)
        strength = RNG.uniform(0.5, 0.85)
        tt = np.arange(length) / rate

        swell_env = np.clip(tt / 2.2, 0, 1) ** 2 * np.exp(-np.clip(tt - 2.2, 0, None) / 1.6)
        swell = lowpass(pink(length), rate, 350, order=4) * swell_env

        wash_env = np.clip((tt - 1.8) / 0.9, 0, 1) ** 1.5 * np.exp(-np.clip(tt - 2.7, 0, None) / 1.7)
        wash_common = band(pink(length), rate, 250, 2000, order=2)
        wash_sides = [band(pink(length), rate, 250, 2000, order=2) for _ in range(2)]

        fizz_env = np.clip((tt - 2.6) / 0.6, 0, 1) * np.exp(-np.clip(tt - 3.2, 0, None) / 1.3)
        fizz = band(RNG.standard_normal(length), rate, 1800, 5500, order=2) * fizz_env
        fizz *= 0.6 + 0.4 * (RNG.random(length) < 0.02)  # a few tiny bubbles in the foam

        end = min(n, start + length)
        for ch, side_gain in enumerate([np.sqrt(0.5 - pan / 2), np.sqrt(0.5 + pan / 2)]):
            # Part of the wash is the same in both ears, part is not: a wide, soft shore.
            wash = (0.7 * wash_common + 0.5 * wash_sides[ch]) * wash_env
            wave_sound = normalize_rms(swell, -29) + normalize_rms(wash, -31) + normalize_rms(fizz, -42)
            out[start:end, ch] += (wave_sound * strength)[: end - start] * side_gain * 1.2
        t += RNG.uniform(6.0, 9.0)

    out = seamless(out, loop, int(rate * fade))
    out = normalize_rms(out, -24)
    return limit_peak(out, -4)


def wind():
    rate, seconds, fade = AMBIENT_RATE, 40.0, 4.0
    loop = int(rate * seconds)
    n = loop + int(rate * fade)
    out = np.zeros((n, 2))
    gust = 0.35 + 0.65 * smooth_random(n, rate, 0.12) ** 1.5
    for ch in range(2):
        # A soft breeze: pink noise, mostly low-mids, with the colour drifting slowly (two bands
        # cross-faded by a slow curve) so it breathes instead of hissing.
        source = highpass(pink(n), rate, 120)
        dark = band(source, rate, 180, 520, order=2)
        light = band(source, rate, 350, 1100, order=2)
        mix = smooth_random(n, rate, 0.08)
        breeze = normalize_rms(dark, -30) * (1 - mix) + normalize_rms(light, -33) * mix
        out[:, ch] = breeze * gust
    out = seamless(out, loop, int(rate * fade))
    out = normalize_rms(out, -30)
    return limit_peak(out, -8)


# ---------------------------------------------------------------- effects

def note_freq(name):
    names = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names[pitch] + 12 * (octave - 4) - 9) / 12)


def marimba(freq, seconds=0.9, rate=EFFECT_RATE):
    """Warm wooden bar: fundamental, a 4× partial that dies fast, soft mallet attack."""
    n = int(rate * seconds)
    t = np.arange(n) / rate
    tone = np.sin(2 * np.pi * freq * t) * np.exp(-t / 0.45)
    tone += 0.35 * np.sin(2 * np.pi * freq * 3.98 * t) * np.exp(-t / 0.06)
    tone += 0.12 * np.sin(2 * np.pi * freq * 9.9 * t) * np.exp(-t / 0.02)
    return tone * np.minimum(1, t / 0.003)


def bell(freq, seconds=1.6, rate=EFFECT_RATE):
    """Soft glassy bell (a gentle FM), for sparkles and important moments."""
    n = int(rate * seconds)
    t = np.arange(n) / rate
    index = 1.6 * np.exp(-t / 0.35)
    tone = np.sin(2 * np.pi * freq * t + index * np.sin(2 * np.pi * freq * 2.0 * t))
    tone += 0.25 * np.sin(2 * np.pi * freq * 2.76 * t) * np.exp(-t / 0.5)
    return tone * np.exp(-t / 0.7) * np.minimum(1, t / 0.004)


def splash(seconds=0.45, rate=EFFECT_RATE, size=1.0):
    """A small, soft water splash with a few rising droplets."""
    n = int(rate * seconds)
    t = np.arange(n) / rate
    body = band(RNG.standard_normal(n), rate, 500, 3500, order=2) * np.minimum(1, t / 0.008) * np.exp(-t / (0.09 * size))
    out = normalize_rms(body, -22)
    for _ in range(4):
        start = RNG.uniform(0.03, 0.2)
        dur = RNG.uniform(0.025, 0.05)
        f0, f1 = RNG.uniform(700, 1100), RNG.uniform(1400, 2200)
        m = (t >= start) & (t < start + dur)
        local = t[m] - start
        phase = 2 * np.pi * (f0 * local + (f1 - f0) * local ** 2 / (2 * dur))
        out[m] += 0.12 * np.sin(phase) * np.sin(np.pi * local / dur)
    return out


def place(buffer, sound, at, rate=EFFECT_RATE, gain=1.0):
    start = int(at * rate)
    end = min(len(buffer), start + len(sound))
    buffer[start:end] += sound[: end - start] * gain
    return buffer


def effect(seconds):
    return np.zeros(int(EFFECT_RATE * seconds))


def finish(x, target_db=-18, peak_db=-3):
    x = x - np.mean(x)
    fade = int(EFFECT_RATE * 0.02)
    x[-fade:] *= np.linspace(1, 0, fade)
    return limit_peak(normalize_rms(x, target_db), peak_db)


def catch_common():
    x = effect(0.9)
    place(x, splash(), 0.0, gain=0.9)
    place(x, marimba(note_freq("E5")), 0.10, gain=0.30)
    return finish(x, -21)


def catch_rare():
    x = effect(1.8)
    place(x, splash(size=1.3), 0.0, gain=0.9)
    for i, n in enumerate(["C6", "E6", "G6", "C7"]):
        place(x, bell(note_freq(n), 1.3), 0.12 + i * 0.09, gain=0.28 - i * 0.03)
    return finish(x, -19)


def record():
    x = effect(2.0)
    place(x, marimba(note_freq("G5"), 0.8), 0.0, gain=0.5)
    place(x, marimba(note_freq("C6"), 1.2), 0.16, gain=0.55)
    place(x, marimba(note_freq("E6"), 1.2), 0.16, gain=0.30)
    for i, n in enumerate(["G6", "C7", "E7"]):
        place(x, bell(note_freq(n), 1.2), 0.30 + i * 0.07, gain=0.16)
    return finish(x, -18)


def level_up():
    x = effect(2.4)
    for i, n in enumerate(["C5", "E5", "G5", "C6"]):
        place(x, marimba(note_freq(n), 1.1), i * 0.11, gain=0.45)
    # A soft chord that blooms underneath and fades away.
    n = int(EFFECT_RATE * 1.8)
    t = np.arange(n) / EFFECT_RATE
    pad = sum(np.sin(2 * np.pi * note_freq(k) * t) for k in ["C5", "E5", "G5"])
    pad *= np.clip(t / 0.35, 0, 1) * np.exp(-np.clip(t - 0.35, 0, None) / 0.6)
    place(x, lowpass(pad, EFFECT_RATE, 2500), 0.35, gain=0.10)
    place(x, bell(note_freq("C7"), 1.4), 0.46, gain=0.12)
    return finish(x, -18)


def coins():
    x = effect(0.6)
    for at, base in [(0.0, 2350.0), (0.09, 2650.0)]:
        n = int(EFFECT_RATE * 0.4)
        t = np.arange(n) / EFFECT_RATE
        clink = sum(a * np.sin(2 * np.pi * base * r * t) * np.exp(-t / d)
                    for r, a, d in [(1.0, 1.0, 0.09), (1.47, 0.6, 0.06), (2.09, 0.35, 0.04)])
        place(x, clink * np.minimum(1, t / 0.001), at, gain=0.4)
    return finish(x, -22)


def important():
    x = effect(1.8)
    place(x, bell(note_freq("E6"), 1.4), 0.0, gain=0.35)
    place(x, bell(note_freq("B6"), 1.4), 0.14, gain=0.28)
    return finish(x, -20)


def warning():
    x = effect(0.8)
    place(x, marimba(note_freq("A4"), 0.5), 0.0, gain=0.5)
    place(x, marimba(note_freq("E4"), 0.6), 0.14, gain=0.5)
    return finish(x, -22)


def click():
    x = effect(0.12)
    place(x, marimba(note_freq("C6"), 0.1), 0.0, gain=0.4)
    return finish(x, -28, -10)


def main():
    os.makedirs(OUT, exist_ok=True)
    sounds = [
        ("ambiente_mar", sea(), AMBIENT_RATE),
        ("ambiente_vento", wind(), AMBIENT_RATE),
        ("captura", catch_common(), EFFECT_RATE),
        ("captura_rara", catch_rare(), EFFECT_RATE),
        ("recorde", record(), EFFECT_RATE),
        ("subir_nivel", level_up(), EFFECT_RATE),
        ("moedas", coins(), EFFECT_RATE),
        ("importante", important(), EFFECT_RATE),
        ("aviso", warning(), EFFECT_RATE),
        ("clique", click(), EFFECT_RATE),
    ]
    for name, data, rate in sounds:
        path = write_wav(name, data, rate)
        seconds = len(data) / rate
        print("%-16s %5.1f s  pico %6.1f dB  média %6.1f dB  %s" % (
            name, seconds, db(np.max(np.abs(data))), db(rms(data)), os.path.relpath(path, ROOT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
