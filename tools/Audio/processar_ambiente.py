"""Prepares long ambience recordings (the owner's copyright-free files) for the game.

Usage:  python3 tools/Audio/processar_ambiente.py <mapa> <arquivo> [<arquivo> ...]
        <mapa> = lago | rio

Each file (mp3, wav, ogg, m4a…) becomes Resources/Sons/Ambiente/<mapa>_NN.ogg, numbered in the order
given: stereo, 44.1 kHz, Ogg Vorbis, trimmed to at most 3.5 minutes, the loudness evened out so all
tracks sound equally loud (EBU R128, -26 LUFS: calm, under the effects), and a 3-second fade at each
end so the crossfade in the game is seamless. Listed in Resources/Sons/ambiente.json by name.

Needs ffmpeg: either on the PATH or through  pip install imageio-ffmpeg
"""
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '..', '..', 'client-unity', 'Assets', 'Resources', 'Sons', 'Ambiente'))
MAX_SECONDS = 210
FADE = 3.0


def ffmpeg():
    exe = shutil.which('ffmpeg')
    if exe:
        return exe
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        raise SystemExit('Precisa do ffmpeg. Instale com:  pip install imageio-ffmpeg')


def duration(exe, path):
    probe = subprocess.run([exe, '-i', path], capture_output=True, text=True).stderr
    for line in probe.splitlines():
        line = line.strip()
        if line.startswith('Duration:'):
            h, m, s = line.split(',')[0].split()[1].split(':')
            return int(h) * 3600 + int(m) * 60 + float(s)
    raise SystemExit('Não consegui ler a duração de ' + path)


def main():
    if len(sys.argv) < 3 or sys.argv[1] not in ('lago', 'rio'):
        print(__doc__)
        sys.exit(1)
    exe = ffmpeg()
    os.makedirs(OUT, exist_ok=True)
    for n, path in enumerate(sys.argv[2:], start=1):
        length = min(duration(exe, path), MAX_SECONDS)
        target = os.path.join(OUT, '%s_%02d.ogg' % (sys.argv[1], n))
        filters = 'loudnorm=I=-26:TP=-3:LRA=11,afade=t=in:d=%g,afade=t=out:st=%g:d=%g' % (FADE, max(0, length - FADE), FADE)
        subprocess.run([exe, '-y', '-v', 'error', '-i', path, '-t', str(length), '-af', filters,
                        '-ar', '44100', '-ac', '2', '-c:a', 'libvorbis', '-q:a', '4', target], check=True)
        print('  %s  %d:%02d' % (os.path.relpath(target, os.path.join(HERE, '..', '..')), length // 60, length % 60))
    print('Pronto. Confira os nomes em Resources/Sons/ambiente.json.')


if __name__ == '__main__':
    main()
