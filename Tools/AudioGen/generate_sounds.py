"""Generates a placeholder for every sound of the game, plus the sound spec read by the builders and by the composer.

Run from the repository root:  python Tools/AudioGen/generate_sounds.py   (standard library only)

Placeholders only: each sound is a synthesized WAV at the length and in the form the final sound must have (a short
tone for UI and effects, a seamless loop of notes for music, a seamless noise loop for ambience, syllable-like tones
for voice), so the game runs and every sound is heard where it plays. Final audio replaces a file keeping its name (and
Unity GUID). Output is byte-identical on every run.

Declared here, once per sound (SOUNDS): path under Assets/game/audio (the folder is the category), category, length,
loop, what it is for.

Written:
- Assets/game/audio/<category>/<name>.wav       the placeholders (16-bit mono, 44.1 kHz);
- Assets/game/audio/audio-spec.json             read by the builders (import settings per category);
- Docs/AUDIO_SPEC.md                            the brief for the composer (length, loop, channels, levels, usage).
"""
import json
import math
import pathlib
import random
import struct
import wave
from dataclasses import dataclass

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/game/audio"
SPEC_JSON = OUTPUT / "audio-spec.json"
SPEC_DOC = ROOT / "Docs/AUDIO_SPEC.md"
RATE = 44100

# What each category asks of the final sound, and how the builders import it.
CATEGORIES = {
    "music": {"channels": "stereo", "level": "khoảng -16 LUFS, đỉnh ≤ -1 dBFS", "import": "Streaming, Vorbis"},
    "ambience": {"channels": "stereo", "level": "khoảng -24 LUFS (nằm dưới nhạc)", "import": "Compressed In Memory, Vorbis"},
    "sfx": {"channels": "mono", "level": "đỉnh ≤ -1 dBFS, đều nhau giữa các tiếng", "import": "Decompress On Load, ADPCM"},
    "ui": {"channels": "mono", "level": "đỉnh ≤ -3 dBFS, nhẹ hơn sfx", "import": "Decompress On Load, ADPCM"},
    "voice": {"channels": "mono", "level": "khoảng -18 LUFS, đỉnh ≤ -1 dBFS", "import": "Compressed In Memory, Vorbis"},
}


@dataclass
class Sound:
    path: str
    seconds: float
    loop: bool
    note: str
    pitch: float = 440.0   # Base frequency of the placeholder; only tells sounds apart.

    @property
    def category(self):
        return self.path.split("/", 1)[0]


SOUNDS = [
    # ui
    Sound("ui/volume-preview", 0.12, False, "Phát ở nhóm âm của thanh âm lượng khi kéo (Settings), để nghe ngay mức mới", 660.0),
    Sound("ui/button-click", 0.08, False, "Bấm nút menu, popup", 880.0),
    Sound("ui/page-swipe", 0.25, False, "Trượt trang Main Menu ↔ Chọn nhân vật", 520.0),
    Sound("ui/coin", 0.2, False, "Nhận coin (hạ quái), mua item", 1320.0),
    # music
    Sound("music/menu-theme", 8.0, True, "Nhạc Main Menu, lặp liền mạch", 220.0),
    Sound("music/battle-theme", 8.0, True, "Nhạc trong trận, lặp liền mạch", 196.0),
    # sfx: board
    Sound("sfx/gem-swap", 0.12, False, "Hai gem đổi chỗ", 600.0),
    Sound("sfx/gem-invalid", 0.2, False, "Đổi chỗ không tạo match, gem trượt về", 160.0),
    Sound("sfx/gem-match", 0.25, False, "Một nhóm gem nổ (cao dần theo combo)", 740.0),
    Sound("sfx/board-shuffle", 0.5, False, "Bàn tự xáo khi hết nước đi, hoặc item xáo", 300.0),
    Sound("sfx/bomb", 0.6, False, "Item bom nổ trên bàn", 90.0),
    # sfx: battle
    Sound("sfx/hero-attack", 0.18, False, "Hero vung đòn thường", 330.0),
    Sound("sfx/hero-strike", 0.4, False, "Đòn mạnh của hero từ gem Attack", 250.0),
    Sound("sfx/monster-attack", 0.2, False, "Quái đánh", 140.0),
    Sound("sfx/hit", 0.15, False, "Trúng đòn (hero hoặc quái)", 180.0),
    Sound("sfx/heal", 0.45, False, "Hồi máu từ gem Heal hoặc bình máu", 523.0),
    Sound("sfx/shield", 0.4, False, "Nhận giáp ảo từ gem Defense hoặc item", 392.0),
    Sound("sfx/magic-charge", 0.35, False, "Thanh Magic của healer nạp thêm", 659.0),
    Sound("sfx/healer-skill", 0.7, False, "Healer tung đòn hỗ trợ khi thanh Magic đầy", 784.0),
    Sound("sfx/monster-death", 0.4, False, "Quái gục", 120.0),
    Sound("sfx/boss-appear", 0.9, False, "Boss xuất hiện", 98.0),
    Sound("sfx/hero-death", 0.9, False, "Hero gục, hết lượt", 110.0),
    Sound("sfx/revive", 0.8, False, "Hồi sinh", 440.0),
]


def tone(frequency, seconds, peak=0.5):
    """A sine with its octave, 5 ms attack and an exponential decay, so it clicks neither in nor out."""
    count = int(RATE * seconds)
    attack = max(1, int(RATE * 0.005))
    return [min(1.0, i / attack) * math.exp(-6.0 * (i / RATE) / seconds) * (1.0 - i / count) * peak
            * (math.sin(2 * math.pi * frequency * i / RATE) + 0.3 * math.sin(4 * math.pi * frequency * i / RATE)) / 1.3
            for i in range(count)]


def notes_loop(base, seconds):
    """Quarter notes of a major arpeggio at 120 BPM, each fading out inside its own beat, so the end meets the start."""
    beat = 0.5
    steps = [1.0, 1.25, 1.5, 2.0]
    samples = []
    for index in range(int(round(seconds / beat))):
        frequency = base * steps[index % len(steps)]
        accent = 0.6 if index % 4 == 0 else 0.4      # bar starts stand out, so the loop point is audible
        samples += tone(frequency, beat, accent)
    return samples[: int(RATE * seconds)] + [0.0] * max(0, int(RATE * seconds) - len(samples))


def noise_loop(seconds, seed):
    """Soft low noise; the last 0.5 s crossfades into the first, so the loop has no seam."""
    rng = random.Random(seed)
    count, fade = int(RATE * seconds), int(RATE * 0.5)
    raw, level = [], 0.0
    for _ in range(count + fade):
        level = 0.98 * level + 0.02 * rng.uniform(-1.0, 1.0)   # low-pass: a room rumble rather than hiss
        raw.append(level * 2.5)
    out = raw[:count]
    for i in range(fade):
        mix = i / fade
        out[i] = out[i] * mix + raw[count + i] * (1.0 - mix)
    return [max(-0.3, min(0.3, s)) for s in out]


def syllables(base, seconds):
    """Short rising and falling tones, like a voice line's rhythm."""
    samples, index = [], 0
    while len(samples) < RATE * seconds:
        samples += tone(base * (1.0 + 0.15 * (index % 3)), 0.18, 0.45) + [0.0] * int(RATE * 0.06)
        index += 1
    return samples[: int(RATE * seconds)]


def render(sound):
    category = sound.category
    if category == "music":
        return notes_loop(sound.pitch, sound.seconds)
    if category == "ambience":
        return noise_loop(sound.seconds, seed=sum(map(ord, sound.path)))
    if category == "voice":
        return syllables(sound.pitch, sound.seconds)
    return tone(sound.pitch, sound.seconds)


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(b"".join(struct.pack("<h", int(round(max(-1.0, min(1.0, s)) * 32767))) for s in samples))


def brief():
    out = [
        "# Spec âm thanh (sinh tự động)",
        "",
        "Sinh bởi `python Tools/AudioGen/generate_sounds.py`; sửa bảng trong script đó rồi chạy lại, đừng sửa file này.",
        "",
        "**Giao file:** WAV (44,1 hoặc 48 kHz, 16 hoặc 24 bit), **cùng tên và cùng thư mục** với placeholder; Unity tự nén "
        "theo nhóm khi Build All. Không đổi đuôi file (đổi đuôi là file mới, mất liên kết trong game).",
        "",
        "**Âm lặp:** điểm cuối nối liền điểm đầu (không click, không khoảng lặng); độ dài có thể khác placeholder nếu "
        "trọn ô nhịp.",
        "",
        "| Nhóm | Kênh | Mức | Unity nhập |",
        "|---|---|---|---|",
    ]
    for name, info in CATEGORIES.items():
        out.append(f"| `{name}` | {info['channels']} | {info['level']} | {info['import']} |")
    out += ["", "| File | Nhóm | Độ dài | Lặp | Dùng cho |", "|---|---|---|---|---|"]
    for s in SOUNDS:
        out.append(f"| `{s.path}.wav` | {s.category} | {s.seconds:g} s | {'có' if s.loop else 'không'} | {s.note} |")
    return "\n".join(out)


def main():
    paths = [s.path for s in SOUNDS]
    duplicates = sorted({p for p in paths if paths.count(p) > 1})
    unknown = sorted({s.category for s in SOUNDS} - set(CATEGORIES))
    if duplicates or unknown:
        raise ValueError(f"Duplicate sounds {duplicates}; unknown categories {unknown}")
    for sound in SOUNDS:
        write_wav(OUTPUT / f"{sound.path}.wav", render(sound))
    spec = {"sounds": [{"path": s.path, "category": s.category, "seconds": s.seconds, "loop": s.loop} for s in SOUNDS]}
    SPEC_JSON.write_text(json.dumps(spec, indent=2) + "\n", encoding="utf-8")
    SPEC_DOC.write_text(brief() + "\n", encoding="utf-8")
    print(f"{len(SOUNDS)} sounds; {SPEC_JSON.relative_to(ROOT)}, {SPEC_DOC.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
