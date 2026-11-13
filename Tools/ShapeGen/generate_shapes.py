"""Generates a placeholder for every image of the game, plus the image spec read by the builders and by artists.

Run from the repository root:  python Tools/ShapeGen/generate_shapes.py   (needs Pillow)

Placeholders only: each image is a plain shape (rectangle, rounded rectangle, ellipse, outline, bar) at the exact size the
final art must have, in a colour per role, with only its name written in the middle: inside the centre of the 9-slice
border for sliced images (the part that scales evenly, so the name is never torn apart by the slicing), and for
animation sheets the frame's sprite name (<sheet>_<tag>_<n>) in each cell. Sizes, borders, pivots and tags are in the
spec, not on the image. Final art replaces a file keeping its name, size and Unity GUID. Output is byte-identical on
every run.

Declared here, once per image (SPRITES, SHEETS):
- path under art/sprites (the folder is the pack, Docs/STRUCTURE.md), pixel size, shape, colour;
- 9-slice border (left, bottom, right, top, in pixels) for images drawn Sliced;
- pivot (0..1 from the bottom-left) for world images;
- animation sheets: frame size and tags (name, frames, milliseconds per frame, loop); one row per tag, top to bottom,
  frames left to right, every cell exactly one frame.

Written:
- Assets/game/art/sprites/<path>.png                the placeholders;
- Assets/game/art/art-spec.json                     read by the builders (borders, pivots, sheet slicing, animations);
- Docs/ART_SPEC.md                                  the brief for artists (sizes, borders, pivots, tags, frames).
"""
import json
import pathlib
from dataclasses import dataclass, field

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/game/art/sprites"
SPEC_JSON = ROOT / "Assets/game/art/art-spec.json"
SPEC_DOC = ROOT / "Docs/ART_SPEC.md"
PIXELS_PER_UNIT = 128   # One world unit in pixels; the builders import every sprite with it.
SUPERSAMPLE = 4         # Shapes are drawn at 4x, then downscaled for smooth edges.

CENTER = (0.5, 0.5)
BOTTOM = (0.5, 0.0)

# Colours per role, so a screen of placeholders still reads (ground, panel, control, accent, glyph, world).
GROUND = "#14121F"
PANEL = "#2E2A45"
CONTROL = "#3A3550"
PRIMARY = "#46A758"
ACCENT = "#9B7BFF"
LIGHT = "#E8E4FF"
FOCUS = "#FFD166"
WARN = "#FFB224"
WORLD = "#3E8E9E"
CHARACTER = "#D9822B"
EFFECT = "#E5484D"


@dataclass
class Sprite:
    path: str
    size: tuple
    shape: str            # rect, rounded, ellipse, outline (rounded ring), bar (rounded, small radius)
    colour: str
    note: str
    border: int = 0       # 9-slice border on every side, in pixels (0 = none)
    pivot: tuple = CENTER
    opacity: float = 1.0


@dataclass
class Tag:
    name: str
    frames: int
    frame_ms: int
    loop: bool = True


@dataclass
class Sheet:
    path: str
    frame: tuple
    shape: str
    colour: str
    note: str
    tags: list = field(default_factory=list)
    pivot: tuple = BOTTOM


def units(width, height):
    """A world size in units, in pixels."""
    return round(width * PIXELS_PER_UNIT), round(height * PIXELS_PER_UNIT)


# Sizes are the size the art is drawn at: the size shown at the 1920x1080 reference for pictures, the source size
# (with its border) for 9-slice frames. Glyphs show at 48 px and are drawn at 64 for sharper downscaling.
S, BAR, GLYPH = (128, 128), (64, 32), (64, 64)
PILL, TRIGGER = (64, 40), (64, 48)
SCREEN = (1920, 1080)


def px(side):
    return side, side


SPRITES = [
    # ui-common: shared UI (several screens).
    Sprite("ui-common/button-primary", S, "rounded", PRIMARY, "Nút chính (Play, OK, Apply)", border=32),
    Sprite("ui-common/button-secondary", S, "rounded", CONTROL, "Nút phụ, tab", border=32),
    Sprite("ui-common/focus-frame", S, "outline", FOCUS, "Khung quanh control đang chọn (phím/tay cầm)", border=32),
    Sprite("ui-common/popup-backdrop", S, "rect", "#000000", "Nền mờ phía sau popup", opacity=0.6),
    Sprite("ui-common/popup-background", S, "rounded", PANEL, "Khung panel của popup", border=32),
    Sprite("ui-common/popup-header", S, "rounded", "#4A4660", "Dải tiêu đề popup", border=32),
    Sprite("ui-common/toast-background", S, "rounded", PANEL, "Nền thông báo nhỏ (toast)", border=32, opacity=0.95),
    Sprite("ui-common/row-background", S, "rounded", "#332F48", "Nền một hàng danh sách (settings, achievements)", border=32),
    # backgrounds: full screen, never packed.
    Sprite("backgrounds/splash-background", SCREEN, "rect", "#0B0A12", "Nền splash"),
    Sprite("backgrounds/menu-background", SCREEN, "rect", "#1B1830", "Nền Main Menu"),
    Sprite("backgrounds/loading-background", SCREEN, "rect", GROUND, "Nền màn loading"),
    Sprite("backgrounds/first-launch-background", SCREEN, "rect", "#16142A", "Nền first launch"),
    Sprite("backgrounds/gameplay-background", SCREEN, "rect", "#12262A", "Nền gameplay demo"),
    # screens/splash
    Sprite("screens/splash/splash-logo", px(320), "rounded", ACCENT, "Logo studio"),
    # screens/loading
    Sprite("screens/loading/loading-bar-track", BAR, "bar", PANEL, "Rãnh thanh tiến độ", border=12),
    Sprite("screens/loading/loading-bar-mask", BAR, "bar", "#FFFFFF", "Mặt nạ bo đầu thanh (không hiện)", border=12),
    Sprite("screens/loading/loading-bar-fill", S, "rect", ACCENT, "Phần đầy của thanh tiến độ"),
    # screens/settings
    Sprite("screens/settings/slider-track", BAR, "bar", "#1E1B2E", "Rãnh thanh trượt", border=12),
    Sprite("screens/settings/slider-fill", BAR, "bar", ACCENT, "Phần đầy thanh trượt", border=12),
    Sprite("screens/settings/slider-handle", px(40), "ellipse", LIGHT, "Núm thanh trượt"),
    Sprite("screens/settings/stepper-left", px(56), "rect", LIGHT, "Mũi tên trái của ô chọn ◄ giá trị ►"),
    Sprite("screens/settings/stepper-right", px(56), "rect", LIGHT, "Mũi tên phải của ô chọn ◄ giá trị ►"),
    # screens/save-slots
    Sprite("screens/save-slots/slot-card-frame", S, "rounded", "#2A2640", "Khung thẻ save slot", border=32),
    Sprite("screens/save-slots/slot-empty-icon", px(96), "ellipse", LIGHT, "Icon slot trống"),
    Sprite("screens/save-slots/slot-corrupted-icon", px(96), "rect", WARN, "Icon slot hỏng"),
    Sprite("screens/save-slots/thumbnail-placeholder", (320, 180), "rect", "#1E1B2E", "Ảnh thay khi slot chưa có ảnh chụp (16:9)"),
    # screens/first-launch
    Sprite("screens/first-launch/choice-chosen", S, "outline", PRIMARY, "Viền đánh dấu lựa chọn đang chọn", border=32),
    Sprite("screens/first-launch/photosensitivity-icon", px(80), "rect", WARN, "Icon cảnh báo nhạy sáng"),
    # screens/achievements
    Sprite("screens/achievements/achievement-first-action", px(96), "ellipse", FOCUS, "Huy hiệu achievement"),
    Sprite("screens/achievements/achievement-all-actions", px(96), "ellipse", FOCUS, "Huy hiệu achievement"),
    Sprite("screens/achievements/achievement-first-save", px(96), "ellipse", FOCUS, "Huy hiệu achievement"),
    Sprite("screens/achievements/lock-icon", px(40), "rect", LIGHT, "Ổ khoá của achievement chưa mở"),
    # screens/credits
    Sprite("screens/credits/credits-viewport", S, "rounded", "#1E1B2E", "Nền vùng chữ chạy", border=32, opacity=0.6),
    # screens/gameplay-hud
    Sprite("screens/gameplay-hud/action-tile", S, "rounded", "#2A2640", "Ô action", border=32),
    Sprite("screens/gameplay-hud/action-tile-lit", S, "rounded", PRIMARY, "Ô action khi đang bấm (vẽ đè)", border=32, opacity=0.45),
    Sprite("screens/gameplay-hud/value-fill", S, "rect", ACCENT, "Phần đầy của thanh giá trị"),
    Sprite("screens/gameplay-hud/stick-area", px(150), "ellipse", "#1E1B2E", "Vùng stick"),
    Sprite("screens/gameplay-hud/stick-knob", px(44), "ellipse", LIGHT, "Núm stick"),
    Sprite("screens/gameplay-hud/overlay-panel", S, "rect", "#0B0A12", "Nền bảng lịch sử input (dev)", opacity=0.85),
    Sprite("screens/gameplay-hud/crosshair", px(48), "outline", FOCUS, "Tâm ngắm của ô Look"),
] + [
    Sprite(f"screens/gameplay-hud/direction-{side}", px(34), "rect", FOCUS, "Mũi tên hướng của ô Move")
    for side in ("up", "down", "left", "right")
] + [
    Sprite(f"screens/gameplay-hud/icon-{action}", px(56), "rect", LIGHT, "Icon của action trên ô")
    for action in ("move", "look", "jump", "interact", "attack", "alt-attack", "sprint", "crouch", "pause")
] + [
    # ui-glyphs: button prompts. A glyph with a border is a frame the prompt writes the key or button name on.
    Sprite("ui-glyphs/key", GLYPH, "rounded", PANEL, "Phím bàn phím (chữ phím do prompt viết)", border=16),
] + [
    Sprite(f"ui-glyphs/key-arrow-{side}", GLYPH, "rounded", PANEL, "Phím mũi tên") for side in ("up", "down", "left", "right")
] + [
    Sprite(f"ui-glyphs/mouse-{part}", GLYPH, "rounded", PANEL, "Chuột: phần được dùng")
    for part in ("left", "right", "middle", "scroll", "move")
] + [
    Sprite(f"ui-glyphs/{family}-face-{side}", GLYPH, "ellipse", colour, "Nút mặt tay cầm (vị trí: south = dưới)",
           border=24 if family in ("xbox", "switch") else 0)
    for family, colour in (("xbox", PRIMARY), ("playstation", PANEL), ("switch", "#4A4660"), ("generic", PANEL))
    for side in ("south", "east", "west", "north")
] + [
    Sprite("ui-glyphs/gamepad-shoulder-left", PILL, "rounded", "#4A4660", "Nút vai trái (LB/L1/L)", border=16),
    Sprite("ui-glyphs/gamepad-shoulder-right", PILL, "rounded", "#4A4660", "Nút vai phải", border=16),
    Sprite("ui-glyphs/gamepad-trigger-left", TRIGGER, "rounded", "#4A4660", "Cò trái (LT/L2/ZL)", border=20),
    Sprite("ui-glyphs/gamepad-trigger-right", TRIGGER, "rounded", "#4A4660", "Cò phải", border=20),
    Sprite("ui-glyphs/gamepad-stick-left", GLYPH, "ellipse", PANEL, "Cần trái", border=24),
    Sprite("ui-glyphs/gamepad-stick-right", GLYPH, "ellipse", PANEL, "Cần phải", border=24),
    Sprite("ui-glyphs/gamepad-dpad", GLYPH, "rect", "#4A4660", "D-pad"),
] + [
    Sprite(f"ui-glyphs/gamepad-dpad-{side}", GLYPH, "rect", "#4A4660", "D-pad: một hướng") for side in ("up", "down", "left", "right")
] + [
    Sprite("ui-glyphs/gamepad-start", PILL, "rounded", "#5B5677", "Nút Start/Menu", border=16),
    Sprite("ui-glyphs/gamepad-select", PILL, "rounded", "#5B5677", "Nút Select/View", border=16),
    # game: world images (samples; replace or delete). Size from the world size; pivot at the feet.
    Sprite("game/props/crate", units(1, 1), "rect", WORLD, "Mẫu: vật thể 1×1 đơn vị", pivot=BOTTOM),
]

SHEETS = [
    Sheet("game/characters/hero/hero", units(1, 2), "rounded", CHARACTER, "Mẫu: nhân vật 1×2 đơn vị",
          tags=[Tag("idle", 4, 150), Tag("run", 6, 100)]),
    Sheet("game/effects/hit-spark", units(1, 1), "ellipse", EFFECT, "Mẫu: hiệu ứng trúng đòn, chạy một lần",
          tags=[Tag("play", 5, 60, loop=False)], pivot=CENTER),
]


def rgba(colour, opacity=1.0):
    value = int(colour[1:], 16)
    return (value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF, round(opacity * 255)


def shape(width, height, kind, colour, opacity):
    """The shape filling a width x height image; the transparent ground keeps the fill's RGB so edges do not darken."""
    fill = rgba(colour, opacity)
    image = Image.new("RGBA", (width * SUPERSAMPLE, height * SUPERSAMPLE), fill[:3] + (0,))
    pen = ImageDraw.Draw(image)
    box = [0, 0, image.width - 1, image.height - 1]
    side = min(image.width, image.height)
    if kind == "rect":
        pen.rectangle(box, fill=fill)
    elif kind == "rounded":
        pen.rounded_rectangle(box, radius=side // 4, fill=fill)
    elif kind == "bar":
        pen.rounded_rectangle(box, radius=12 * SUPERSAMPLE, fill=fill)
    elif kind == "ellipse":
        pen.ellipse(box, fill=fill)
    elif kind == "outline":
        pen.rounded_rectangle(box, radius=side // 4, outline=fill, width=max(SUPERSAMPLE, side // 12))
    else:
        raise ValueError(f"Unknown shape {kind}")
    return image.resize((width, height), Image.LANCZOS)


def font(size):
    return ImageFont.load_default(size=size)


def text_colour(colour, kind):
    """Dark text on light fills, light text elsewhere (an outline leaves the ground transparent: light)."""
    r, g, b, _ = rgba(colour)
    light = kind != "outline" and (0.299 * r + 0.587 * g + 0.114 * b) > 150
    return (20, 18, 31, 255) if light else (255, 255, 255, 255)


def write_lines(image, box, lines, colour):
    """Writes the lines centred in box, at the largest size that fits (up to a sixth of the short side, at least 14 px,
    at most 64 px; down to 6 px); long names wrap at dashes."""
    pen = ImageDraw.Draw(image)
    left, top, right, bottom = box
    width, height = right - left, bottom - top
    for size in range(min(64, max(14, min(width, height) // 6)), 5, -1):
        typeface = font(size)
        wrapped = []
        for line in lines:
            if pen.textlength(line, font=typeface) <= width - 2 or "-" not in line:
                wrapped.append(line)
                continue
            words, current = line.split("-"), ""
            for word in words:
                candidate = f"{current}-{word}" if current else word
                if current and pen.textlength(candidate, font=typeface) > width - 2:
                    wrapped.append(current + "-")
                    current = word
                else:
                    current = candidate
            wrapped.append(current)
        line_height = size + 2
        if all(pen.textlength(line, font=typeface) <= width - 2 for line in wrapped) and line_height * len(wrapped) <= height:
            break
    y = top + (height - line_height * len(wrapped)) / 2
    shadow = (0, 0, 0, 160) if colour[0] > 128 else (255, 255, 255, 120)
    for line in wrapped:
        x = left + (width - pen.textlength(line, font=typeface)) / 2
        pen.text((x + 1, y + 1), line, font=typeface, fill=shadow)
        pen.text((x, y), line, font=typeface, fill=colour)
        y += line_height


def name_box(width, height, border):
    """Where the name goes: the centre of the 9-slice (inside the border, 2 px in), or the whole image without one."""
    inset = border + 2 if border else 2
    if width - 2 * inset < 4 or height - 2 * inset < 4:
        inset = border if border else 0
    return inset, inset, width - inset, height - inset


def name_of(path):
    return path.rsplit("/", 1)[-1]


def draw_sprite(sprite):
    width, height = sprite.size
    image = shape(width, height, sprite.shape, sprite.colour, sprite.opacity)
    write_lines(image, name_box(width, height, sprite.border), [name_of(sprite.path)], text_colour(sprite.colour, sprite.shape))
    return image


def draw_sheet(sheet):
    fw, fh = sheet.frame
    columns = max(tag.frames for tag in sheet.tags)
    image = Image.new("RGBA", (fw * columns, fh * len(sheet.tags)), rgba(sheet.colour, 0.0))
    ink = text_colour(sheet.colour, sheet.shape)
    for row, tag in enumerate(sheet.tags):
        for column in range(tag.frames):
            cell = shape(fw, fh, sheet.shape, sheet.colour, 1.0)
            write_lines(cell, name_box(fw, fh, 0), [f"{name_of(sheet.path)}_{tag.name}_{column}"], ink)
            image.paste(cell, (column * fw, row * fh))
    return image


def spec():
    return {
        "pixelsPerUnit": PIXELS_PER_UNIT,
        "sprites": [
            {"path": s.path, "width": s.size[0], "height": s.size[1], "border": [s.border] * 4, "pivot": list(s.pivot)}
            for s in SPRITES
        ],
        "sheets": [
            {"path": s.path, "frameWidth": s.frame[0], "frameHeight": s.frame[1], "pivot": list(s.pivot),
             "tags": [{"name": t.name, "frames": t.frames, "frameMs": t.frame_ms, "loop": t.loop} for t in s.tags]}
            for s in SHEETS
        ],
    }


def pivot_text(pivot):
    return {CENTER: "giữa", BOTTOM: "giữa đáy"}.get(tuple(pivot), f"{pivot[0]}, {pivot[1]}")


def brief():
    """Docs/ART_SPEC.md: what to draw, per pack."""
    out = [
        "# Spec ảnh (sinh tự động)",
        "",
        "Sinh bởi `python Tools/ShapeGen/generate_shapes.py`; sửa bảng trong script đó rồi chạy lại, đừng sửa file này.",
        "Ảnh thật thay placeholder **cùng tên, cùng kích thước**; Build All áp viền 9-slice, pivot và cắt sheet theo spec.",
        f"Một đơn vị thế giới = {PIXELS_PER_UNIT} px. Cấu trúc thư mục và pack: [STRUCTURE.md](STRUCTURE.md).",
        "",
        "- **9-slice N:** N px ở mỗi cạnh giữ nguyên khi kéo giãn (góc), phần giữa kéo giãn: vẽ góc trong N px, giữa trơn.",
        "- **Pivot:** điểm đặt của ảnh trong thế giới (nhân vật: giữa chân).",
        "- **Sheet:** mỗi hàng một tag (trên xuống dưới theo bảng), frame từ trái sang phải, mỗi ô đúng một frame, không chừa khe.",
        "",
    ]
    packs = {}
    for sprite in SPRITES:
        packs.setdefault(sprite.path.rsplit("/", 1)[0], []).append(sprite)
    for pack, sprites in packs.items():
        out += [f"## `{pack}`", "", "| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho |", "|---|---|---|---|---|---|"]
        for s in sprites:
            out.append(f"| `{name_of(s.path)}.png` | {s.size[0]}×{s.size[1]} | {s.shape} | {s.border or '—'} | "
                       f"{pivot_text(s.pivot)} | {s.note} |")
        out.append("")
    if SHEETS:
        out += ["## Sheet animation", "", "| Ảnh | Frame (px) | Sheet (px) | Pivot | Tag (hàng): frame × ms, lặp | Dùng cho |",
                "|---|---|---|---|---|---|"]
        for s in SHEETS:
            columns = max(t.frames for t in s.tags)
            tags = "; ".join(f"{i + 1}. {t.name}: {t.frames} × {t.frame_ms} ms{'' if t.loop else ', không lặp'}"
                             for i, t in enumerate(s.tags))
            out.append(f"| `{s.path}.png` | {s.frame[0]}×{s.frame[1]} | {s.frame[0] * columns}×{s.frame[1] * len(s.tags)} | "
                       f"{pivot_text(s.pivot)} | {tags} | {s.note} |")
        out.append("")
    return "\n".join(out)


def main():
    paths = [s.path for s in SPRITES] + [s.path for s in SHEETS]
    duplicates = sorted({p for p in paths if paths.count(p) > 1})
    if duplicates:
        raise ValueError(f"Duplicate image paths: {duplicates}")
    for sprite in SPRITES:
        target = OUTPUT / f"{sprite.path}.png"
        target.parent.mkdir(parents=True, exist_ok=True)
        draw_sprite(sprite).save(target)
    for sheet in SHEETS:
        target = OUTPUT / f"{sheet.path}.png"
        target.parent.mkdir(parents=True, exist_ok=True)
        draw_sheet(sheet).save(target)
    SPEC_JSON.write_text(json.dumps(spec(), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    SPEC_DOC.write_text(brief() + "\n", encoding="utf-8")
    print(f"{len(SPRITES)} sprites, {len(SHEETS)} sheets; {SPEC_JSON.relative_to(ROOT)}, {SPEC_DOC.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
