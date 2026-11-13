"""Generates every image of the game, plus the image spec read by the builders and by artists.

Run from the repository root:  python Tools/ShapeGen/generate_shapes.py   (needs Pillow)

Each image is declared once (SPRITES, SHEETS) and comes from one of two places:
- art: an image (or a crop of one) from the third-party packs in workspace/sprite-asset, scaled up by a whole factor with
  nearest-neighbour sampling (pixel art stays crisp), optionally recoloured or with its centre cleared;
- placeholder: a plain shape (rectangle, rounded rectangle, ellipse, outline, bar) at the exact size the final art must
  have, in a colour per role, with only its name written in the middle (inside the 9-slice centre for sliced images; for
  animation sheets the frame's sprite name <sheet>_<tag>_<n> in each cell).
When the packs are not on this machine, an image whose art is missing keeps the PNG already in the repository (or gets a
placeholder when there is none), so the script never throws away art. Output is byte-identical on every run.

Declared per image:
- path under art/sprites (the folder is the pack, see pc-base Docs/STRUCTURE.md), pixel size, shape, colour;
- 9-slice border (one value for every side, or left, bottom, right, top in pixels) for images drawn Sliced;
- pivot (0..1 from the bottom-left) for world images;
- animation sheets: frame size and tags (name, frames, milliseconds per frame, loop); one row per tag, top to bottom,
  frames left to right, every cell exactly one frame.

Written:
- Assets/game/art/sprites/<path>.png                the images;
- Assets/game/art/art-spec.json                     read by the builders (borders, pivots, sheet slicing, animations);
- Docs/ART_SPEC.md                                  the brief for artists (sizes, borders, pivots, tags, frames, source).
"""
import colorsys
import json
import pathlib
from dataclasses import dataclass, field

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT.parents[1] / "sprite-asset"
OUTPUT = ROOT / "Assets/game/art/sprites"
SPEC_JSON = ROOT / "Assets/game/art/art-spec.json"
SPEC_DOC = ROOT / "Docs/ART_SPEC.md"
PIXELS_PER_UNIT = 128   # One world unit in pixels; the builders import every sprite with it.
SUPERSAMPLE = 4         # Shapes are drawn at 4x, then downscaled for smooth edges.

CENTER = (0.5, 0.5)
BOTTOM = (0.5, 0.0)

# Colours per role of the placeholders, close to the cozy pixel UI pack so a screen of placeholders still reads.
CREAM = "#FFF4D6"
WOOD = "#B9704E"
WOOD_DARK = "#6E3B2A"
LEAF = "#7FB548"
SKY = "#9AD3E6"
MAGIC = "#B48CFF"
HEALTH = "#E5484D"
ARMOR = "#4C8DE0"
FOCUS = "#FFD166"
PANEL = "#4A3B52"
LIGHT = "#FFF8E8"
CHARACTER = "#C9A7F2"

# Third-party packs (each pack's licence is in its folder; Docs/ART_SPEC.md names the pack of every image).
COZY = "DEMO_MegaCozyUIPack_doboui - copia/DEMO_MegaCozyUIPack_doboui - copia"
RAVEN = "Free - Raven Fantasy Icons/Free - Raven Fantasy Icons/Full Spritesheet/32x32.png"
TINY_RPG = ("Tiny RPG Character Asset Pack v1.03b -Free Soldier&Orc/Tiny RPG Character Asset Pack v1.03 -Free Soldier&Orc/"
            "Characters(100x100)")
PACK_NAMES = {COZY: "Mega Cozy UI Pack (dobo_ui, bản demo)", RAVEN: "Raven Fantasy Icons (Clockwork Raven)",
              TINY_RPG: "Tiny RPG Character Pack (Soldier & Orc)"}


@dataclass(frozen=True)
class Art:
    """An image of a pack: file (or Raven icon cell), crop box, whole-number scale, hue shift in degrees, cleared centre."""
    pack: str
    file: str = ""
    cell: tuple = None          # Raven icon (row, column) on the 32 px sheet
    scale: int = 1
    hue: int = 0
    clear_inset: int = 0        # >0: pixels this far inside the edge become transparent (a frame from a filled box)
    rows: tuple = ()            # sheets: source rows of a character sheet, in tag order
    crop: tuple = None          # sheets: (left, top, right, bottom) inside each 100x100 source cell


@dataclass
class Sprite:
    path: str
    size: tuple
    shape: str            # rect, rounded, ellipse, outline (rounded ring), bar (rounded, small radius)
    colour: str
    note: str
    border: object = 0    # 9-slice border: one value for every side, or (left, bottom, right, top) in pixels
    pivot: tuple = CENTER
    opacity: float = 1.0
    art: Art = None


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
    art: Art = None


def units(width, height):
    """A world size in units, in pixels."""
    return round(width * PIXELS_PER_UNIT), round(height * PIXELS_PER_UNIT)


def px(side):
    return side, side


def cozy(file, scale, clear_inset=0):
    return Art(COZY, file=file, scale=scale, clear_inset=clear_inset)


def raven(row, column, scale, hue=0):
    return Art(RAVEN, cell=(row, column), scale=scale, hue=hue)


# Sizes are the size the art is drawn at: the size shown at the 1920x1080 reference for pictures, the source size
# (with its border) for 9-slice frames. Pixel art is scaled by a whole factor, so its declared size is a multiple of the
# source. Glyphs show at 48 px and are drawn at 64 for sharper downscaling.
S, BAR, GLYPH = (128, 128), (64, 32), (64, 64)
PILL, TRIGGER = (64, 40), (64, 48)
SCREEN = (1920, 1080)
GEM, ICON = px(64), px(64)
# Character frames: a 56x44 window of the 100x100 source cells (feet at y 57), six times larger.
CHARACTER_SCALE = 6
CHARACTER_CROP = (24, 24, 80, 68)
CHARACTER_FRAME = ((CHARACTER_CROP[2] - CHARACTER_CROP[0]) * CHARACTER_SCALE, (CHARACTER_CROP[3] - CHARACTER_CROP[1]) * CHARACTER_SCALE)
CHARACTER_PIVOT = (round((48.5 - CHARACTER_CROP[0]) / (CHARACTER_CROP[2] - CHARACTER_CROP[0]), 4),
                   round((CHARACTER_CROP[3] - 57) / (CHARACTER_CROP[3] - CHARACTER_CROP[1]), 4))

SPRITES = [
    # ui-common: shared UI (several screens).
    Sprite("ui-common/button-primary", (136, 136), "rounded", "#3A9C8F", "Nút chính (Play, OK, Resume)", border=32,
           art=cozy("Buttons/Inflated/InflatedlButton1_marine.png", 4)),
    Sprite("ui-common/button-secondary", (136, 136), "rounded", WOOD, "Nút phụ, tab", border=32,
           art=cozy("Buttons/Square/SquareButton1_wood.png", 4)),
    Sprite("ui-common/focus-frame", (136, 136), "outline", FOCUS, "Khung quanh control đang chọn (phím/tay cầm)", border=32,
           art=cozy("Frames/Frame1_yellow.png", 4, clear_inset=5)),
    Sprite("ui-common/popup-backdrop", S, "rect", "#000000", "Nền mờ phía sau popup", opacity=0.6),
    Sprite("ui-common/popup-background", (136, 136), "rounded", CREAM, "Khung panel của popup", border=32,
           art=cozy("Frames/Frame1_wood.png", 4)),
    Sprite("ui-common/popup-header", (424, 84), "rounded", LEAF, "Dải tiêu đề popup (ruy băng)", border=(56, 0, 56, 0),
           art=cozy("Headers/Header1_green.png", 4)),
    Sprite("ui-common/toast-background", (264, 148), "rounded", CREAM, "Nền thông báo nhỏ (toast)", border=32,
           art=cozy("Containers/SimpleColorContainers/SimpleColorContainer1_cream.png", 4)),
    Sprite("ui-common/row-background", (264, 148), "rounded", CREAM, "Nền một hàng danh sách (settings)", border=32,
           art=cozy("Containers/DoubleStripeContainer/DoubleStripeContainer1_cream.png", 4)),
    Sprite("ui-common/panel-wood", (264, 148), "rounded", WOOD, "Nền ô thông tin (HUD, ví coin)", border=32,
           art=cozy("Containers/SimpleColorContainers/SimpleColorContainer1_wood.png", 4)),
    Sprite("ui-common/coin-icon", ICON, "ellipse", FOCUS, "Icon đồng coin", art=raven(8, 2, 2)),
    # backgrounds: full screen, never packed.
    Sprite("backgrounds/splash-background", SCREEN, "rect", "#F3DDB3", "Nền splash"),
    Sprite("backgrounds/menu-background", SCREEN, "rect", "#F3DDB3", "Nền Main Menu (đồng cỏ, trời)"),
    Sprite("backgrounds/loading-background", SCREEN, "rect", "#F3DDB3", "Nền màn loading"),
    # screens/splash
    Sprite("screens/splash/splash-logo", px(320), "rounded", MAGIC, "Logo studio"),
    # screens/loading
    Sprite("screens/loading/loading-bar-track", BAR, "bar", WOOD_DARK, "Rãnh thanh tiến độ", border=12),
    Sprite("screens/loading/loading-bar-mask", BAR, "bar", "#FFFFFF", "Mặt nạ bo đầu thanh (không hiện)", border=12),
    Sprite("screens/loading/loading-bar-fill", S, "rect", LEAF, "Phần đầy của thanh tiến độ"),
    # screens/settings
    Sprite("screens/settings/slider-track", BAR, "bar", WOOD_DARK, "Rãnh thanh trượt", border=12),
    Sprite("screens/settings/slider-fill", BAR, "bar", "#3A9C8F", "Phần đầy thanh trượt", border=12),
    Sprite("screens/settings/slider-handle", px(40), "ellipse", LIGHT, "Núm thanh trượt"),
    Sprite("screens/settings/stepper-left", (54, 102), "rect", LIGHT, "Mũi tên trái của ô chọn ◄ giá trị ►",
           art=cozy("Arrows/Arrow6_cream.png", 3)),
    Sprite("screens/settings/stepper-right", (54, 102), "rect", LIGHT, "Mũi tên phải của ô chọn ◄ giá trị ►",
           art=cozy("Arrows/Arrow7_cream.png", 3)),
    # screens/main-menu: wallet, character pages.
    Sprite("screens/main-menu/character-card", (160, 220), "rounded", CREAM, "Thẻ một nhân vật trong danh sách",
           art=cozy("Cards/Card1_gray.png", 4)),
    Sprite("screens/main-menu/character-card-selected", (160, 220), "rounded", MAGIC, "Thẻ nhân vật đang chọn",
           art=cozy("Cards/Card1_purple.png", 4)),
    Sprite("screens/main-menu/arrow-left", (102, 102), "rect", WOOD, "Nút quay về (trang nhân vật → menu)",
           art=cozy("Arrows/Arrow3_wood.png", 3)),
    Sprite("screens/main-menu/arrow-right", (102, 102), "rect", WOOD, "Nút sang trang nhân vật",
           art=cozy("Arrows/Arrow4_wood.png", 3)),
    Sprite("screens/main-menu/stage", (528, 120), "ellipse", "#7FB548", "Bệ đứng của nhân vật ở menu (bóng cỏ)", opacity=0.8),
    # screens/gameplay: board, items, bars, HUD.
    Sprite("screens/gameplay/gem-attack", GEM, "rect", HEALTH, "Gem Attack: đỏ, hình thoi", art=raven(10, 4, 2, hue=140)),
    Sprite("screens/gameplay/gem-heal", GEM, "ellipse", "#46A758", "Gem Heal: xanh lá, hình tròn", art=raven(10, 6, 2)),
    Sprite("screens/gameplay/gem-defense", GEM, "rect", ARMOR, "Gem Defense: xanh dương, hình khiên vuông", art=raven(10, 0, 2)),
    Sprite("screens/gameplay/gem-magic", GEM, "rect", MAGIC, "Gem Magic: tím, hình tam giác (tinh thể nhọn)", art=raven(10, 12, 2)),
    Sprite("screens/gameplay/board-cell", (66, 66), "rounded", CREAM, "Ô nền của bàn ghép", border=12,
           art=cozy("Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png", 2)),
    Sprite("screens/gameplay/board-cell-selected", (70, 70), "outline", FOCUS, "Ô đang chọn trên bàn ghép", border=14,
           art=cozy("Inventory/ItemSlots/ItemSlotsSelected/SlotHover1_cream.png", 2)),
    Sprite("screens/gameplay/board-frame", (358, 132), "outline", WOOD, "Khung gỗ quanh bàn ghép", border=24,
           art=cozy("Frames/EmptyWoodenFrame.png", 2)),
    Sprite("screens/gameplay/board-mask", S, "rect", "#000000", "Lớp che bàn ghép khi đang diễn hiệu ứng", opacity=0.7),
    Sprite("screens/gameplay/item-slot", (132, 132), "rounded", CREAM, "Ô item", border=24,
           art=cozy("Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png", 4)),
    Sprite("screens/gameplay/item-potion", ICON, "rect", HEALTH, "Item bình máu", art=raven(17, 8, 2)),
    Sprite("screens/gameplay/item-bomb", ICON, "ellipse", PANEL, "Item bom xoá một vùng 3×3", art=raven(47, 11, 2)),
    Sprite("screens/gameplay/item-shuffle", ICON, "ellipse", ARMOR, "Item xáo trộn bàn", art=raven(49, 1, 2)),
    Sprite("screens/gameplay/item-armor", ICON, "rect", ARMOR, "Item tăng giáp tức thì", art=raven(53, 7, 2)),
    Sprite("screens/gameplay/icon-plus", px(48), "rounded", LEAF, "Dấu + trên ô item hết (mua bằng coin)"),
    Sprite("screens/gameplay/icon-pause", (48, 54), "rect", LIGHT, "Icon nút Pause", art=cozy("Icons/IconsFlat_16px/IconFlat10.png", 3)),
    Sprite("screens/gameplay/bar-track", BAR, "bar", WOOD_DARK, "Rãnh thanh Magic của healer", border=12),
    Sprite("screens/gameplay/bar-fill-magic", BAR, "bar", MAGIC, "Phần đầy thanh Magic", border=12),
    # game: world images. Size from the world size; bars are sliced, characters are sheets (below).
    Sprite("game/props/battlefield", (1920, 448), "rect", SKY, "Nền chiến trường nhìn ngang (trời, cỏ), mặt đất ở 1/3 dưới",
           pivot=CENTER),
    Sprite("game/props/camp", (1920, 704), "rect", "#8C5A3C", "Nền phần dưới màn gameplay: trại của healer, sau bàn ghép và item",
           pivot=CENTER),
    Sprite("game/props/hp-bar-track", BAR, "bar", WOOD_DARK, "Rãnh thanh máu trên đầu nhân vật", border=12),
    Sprite("game/props/hp-bar-fill", BAR, "bar", "#46A758", "Máu của hero", border=12, pivot=(0.0, 0.5)),
    Sprite("game/props/hp-bar-fill-enemy", BAR, "bar", HEALTH, "Máu của quái", border=12, pivot=(0.0, 0.5)),
    Sprite("game/props/armor-bar-fill", BAR, "bar", ARMOR, "Giáp ảo của hero (chồng trên thanh máu)", border=12, pivot=(0.0, 0.5)),
    Sprite("game/effects/fx-hit", px(128), "ellipse", "#FF9A3C", "Hiệu ứng trúng đòn", art=raven(47, 14, 4)),
    Sprite("game/effects/fx-heal", px(128), "ellipse", "#46A758", "Hiệu ứng hồi máu", art=raven(48, 12, 4)),
    Sprite("game/effects/fx-shield", px(128), "rect", ARMOR, "Hiệu ứng nhận giáp", art=raven(53, 7, 4)),
    Sprite("game/effects/fx-magic", px(128), "ellipse", MAGIC, "Hiệu ứng phép của healer nổ trên quái", art=raven(48, 5, 4)),
    Sprite("game/effects/fx-magic-orb", px(96), "ellipse", MAGIC, "Viên phép bay (gem Magic → healer, healer → quái)",
           art=raven(48, 1, 3)),
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
    for family, colour in (("xbox", "#46A758"), ("playstation", PANEL), ("switch", "#4A4660"), ("generic", PANEL))
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
]

SHEETS = [
    Sheet("game/characters/soldier/soldier", CHARACTER_FRAME, "rounded", ARMOR, "Hero Soldier (quay phải; game lật khi đứng bên phải)",
          tags=[Tag("idle", 6, 100), Tag("walk", 8, 90), Tag("attack", 6, 70, loop=False), Tag("attack-heavy", 6, 70, loop=False),
                Tag("hurt", 4, 80, loop=False), Tag("death", 4, 140, loop=False)],
          pivot=CHARACTER_PIVOT, art=Art(TINY_RPG, file="Soldier/Soldier/Soldier.png", scale=CHARACTER_SCALE,
                                         rows=(0, 1, 2, 3, 5, 6), crop=CHARACTER_CROP)),
    Sheet("game/characters/orc/orc", CHARACTER_FRAME, "rounded", HEALTH, "Quái Orc (mọi loại quái và boss dùng chung, đổi màu và cỡ)",
          tags=[Tag("idle", 6, 100), Tag("walk", 8, 90), Tag("attack", 6, 80, loop=False), Tag("attack-heavy", 6, 80, loop=False),
                Tag("hurt", 4, 80, loop=False), Tag("death", 4, 140, loop=False)],
          pivot=CHARACTER_PIVOT, art=Art(TINY_RPG, file="Orc/Orc/Orc.png", scale=CHARACTER_SCALE,
                                         rows=(0, 1, 2, 3, 4, 5), crop=CHARACTER_CROP)),
    Sheet("game/characters/healer/healer", (128, 192), "rounded", CHARACTER, "Healer (khối tím nhạt, khác dáng hero)",
          tags=[Tag("idle", 4, 160), Tag("cast", 6, 80, loop=False), Tag("hurt", 3, 80, loop=False)]),
]


# ----- Drawing placeholders.

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


def borders(border):
    return list(border) if isinstance(border, tuple) else [border] * 4


def name_box(width, height, border):
    """Where the name goes: the centre of the 9-slice (inside the border, 2 px in), or the whole image without one."""
    left, bottom, right, top = borders(border)
    box = (left + 2, top + 2, width - right - 2, height - bottom - 2)
    if box[2] - box[0] < 4 or box[3] - box[1] < 4:
        box = (left, top, width - right, height - bottom)
    if box[2] - box[0] < 4 or box[3] - box[1] < 4:
        box = (0, 0, width, height)
    return box


def name_of(path):
    return path.rsplit("/", 1)[-1]


def placeholder_sprite(sprite):
    width, height = sprite.size
    image = shape(width, height, sprite.shape, sprite.colour, sprite.opacity)
    write_lines(image, name_box(width, height, sprite.border), [name_of(sprite.path)], text_colour(sprite.colour, sprite.shape))
    return image


def placeholder_sheet(sheet):
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


# ----- Art from the packs.

def source(art):
    path = PACKS / art.pack / art.file if art.file else PACKS / art.pack
    return path if path.exists() else None


def shift_hue(image, degrees):
    out = image.copy()
    pixels = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            r2, g2, b2 = colorsys.hsv_to_rgb((h + degrees / 360) % 1.0, s, v)
            pixels[x, y] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), a)
    return out


def art_sprite(sprite, path):
    """The pack image scaled by its whole factor and centred on a transparent canvas of the declared size."""
    art = sprite.art
    image = Image.open(path).convert("RGBA")
    if art.cell:
        row, column = art.cell
        image = image.crop((column * 32, row * 32, column * 32 + 32, row * 32 + 32))
    if art.hue:
        image = shift_hue(image, art.hue)
    if art.clear_inset:
        inset = art.clear_inset
        clear = Image.new("RGBA", (image.width - 2 * inset, image.height - 2 * inset), (0, 0, 0, 0))
        image.paste(clear, (inset, inset))
    image = image.resize((image.width * art.scale, image.height * art.scale), Image.NEAREST)
    if image.size == tuple(sprite.size):
        return image
    if image.width > sprite.size[0] or image.height > sprite.size[1]:
        raise ValueError(f"{sprite.path}: art {image.size} is larger than the declared size {sprite.size}")
    canvas = Image.new("RGBA", sprite.size, (0, 0, 0, 0))
    canvas.paste(image, ((sprite.size[0] - image.width) // 2, (sprite.size[1] - image.height) // 2))
    return canvas


def art_sheet(sheet, path):
    """Rows of a 100x100-cell character sheet in tag order, each cell cropped to the window and scaled."""
    art = sheet.art
    source_image = Image.open(path).convert("RGBA")
    fw, fh = sheet.frame
    columns = max(tag.frames for tag in sheet.tags)
    image = Image.new("RGBA", (fw * columns, fh * len(sheet.tags)), (0, 0, 0, 0))
    left, top, right, bottom = art.crop
    for row, (tag, source_row) in enumerate(zip(sheet.tags, art.rows)):
        for column in range(tag.frames):
            cell = source_image.crop((column * 100 + left, source_row * 100 + top, column * 100 + right, source_row * 100 + bottom))
            if cell.getbbox() is None:
                raise ValueError(f"{sheet.path}: tag {tag.name} frame {column} is empty in the source")
            cell = cell.resize((fw, fh), Image.NEAREST)
            image.paste(cell, (column * fw, row * fh))
    return image


def render(entry, target, draw_art, draw_placeholder):
    """The image of one entry: art when its pack is here, else the PNG already on disk (art kept), else a placeholder."""
    if entry.art is not None:
        path = source(entry.art)
        if path is not None:
            return draw_art(entry, path), "art"
        if target.exists():
            print(f"  {entry.path}: pack not found, keeping the existing PNG")
            return None, "kept"
    return draw_placeholder(entry), "placeholder"


# ----- Spec.

def spec():
    return {
        "pixelsPerUnit": PIXELS_PER_UNIT,
        "sprites": [
            {"path": s.path, "width": s.size[0], "height": s.size[1], "border": borders(s.border), "pivot": list(s.pivot)}
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


def border_text(border):
    values = borders(border)
    if not any(values):
        return "—"
    return str(values[0]) if len(set(values)) == 1 else "/".join(map(str, values))


def source_text(entry):
    if entry.art is None:
        return "**placeholder**"
    art = entry.art
    where = f"ô ({art.cell[0]}, {art.cell[1]})" if art.cell else f"`{art.file}`"
    extra = (f", đổi màu {art.hue}°" if art.hue else "") + (", khoét giữa" if art.clear_inset else "")
    return f"{PACK_NAMES[art.pack]}: {where} ×{art.scale}{extra}"


def brief():
    """Docs/ART_SPEC.md: what to draw, per pack, and where each current image comes from."""
    out = [
        "# Spec ảnh (sinh tự động)",
        "",
        "Sinh bởi `python Tools/ShapeGen/generate_shapes.py`; sửa bảng trong script đó rồi chạy lại, đừng sửa file này.",
        "Ảnh thật thay ảnh hiện có **cùng tên, cùng kích thước**; Build All áp viền 9-slice, pivot và cắt sheet theo spec.",
        f"Một đơn vị thế giới = {PIXELS_PER_UNIT} px. Thư mục và pack theo chuẩn của pc-base (`Docs/STRUCTURE.md` của pc-base).",
        "",
        "- **Nguồn:** ảnh lấy từ pack trong `workspace/sprite-asset` (phóng nguyên lần, giữ nét pixel) hoặc **placeholder** "
        "(hình cơ bản ghi tên, cần art thật).",
        "- **9-slice N:** N px ở mỗi cạnh giữ nguyên khi kéo giãn (góc), phần giữa kéo giãn; `a/b/c/d` = trái/dưới/phải/trên.",
        "- **Pivot:** điểm đặt của ảnh trong thế giới (nhân vật: giữa chân).",
        "- **Sheet:** mỗi hàng một tag (trên xuống dưới theo bảng), frame từ trái sang phải, mỗi ô đúng một frame, không chừa khe.",
        "",
    ]
    placeholders = [s.path for s in SPRITES + SHEETS if s.art is None and not s.path.startswith("ui-glyphs/")]
    out += ["## Còn là placeholder (cần art)", ""] + [f"- `{p}`" for p in placeholders] + [
        "", "Glyph phím/tay cầm (`ui-glyphs/`) cũng là placeholder, ít ưu tiên.", ""]
    packs = {}
    for sprite in SPRITES:
        packs.setdefault(sprite.path.rsplit("/", 1)[0], []).append(sprite)
    for pack, sprites in packs.items():
        out += [f"## `{pack}`", "", "| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |", "|---|---|---|---|---|---|---|"]
        for s in sprites:
            out.append(f"| `{name_of(s.path)}.png` | {s.size[0]}×{s.size[1]} | {s.shape} | {border_text(s.border)} | "
                       f"{pivot_text(s.pivot)} | {s.note} | {source_text(s)} |")
        out.append("")
    if SHEETS:
        out += ["## Sheet animation", "", "| Ảnh | Frame (px) | Sheet (px) | Pivot | Tag (hàng): frame × ms, lặp | Dùng cho | Nguồn |",
                "|---|---|---|---|---|---|---|"]
        for s in SHEETS:
            columns = max(t.frames for t in s.tags)
            tags = "; ".join(f"{i + 1}. {t.name}: {t.frames} × {t.frame_ms} ms{'' if t.loop else ', không lặp'}"
                             for i, t in enumerate(s.tags))
            src = "**placeholder**" if s.art is None else f"{PACK_NAMES[s.art.pack]}: `{s.art.file}` hàng {list(s.art.rows)}, ×{s.art.scale}"
            out.append(f"| `{s.path}.png` | {s.frame[0]}×{s.frame[1]} | {s.frame[0] * columns}×{s.frame[1] * len(s.tags)} | "
                       f"{pivot_text(s.pivot)} | {tags} | {s.note} | {src} |")
        out.append("")
    return "\n".join(out)


def main():
    paths = [s.path for s in SPRITES] + [s.path for s in SHEETS]
    duplicates = sorted({p for p in paths if paths.count(p) > 1})
    if duplicates:
        raise ValueError(f"Duplicate image paths: {duplicates}")
    declared = {f"{p}.png" for p in paths}
    for stale in sorted(OUTPUT.rglob("*.png")):
        if str(stale.relative_to(OUTPUT)).replace("\\", "/") not in declared:
            stale.unlink()
            print(f"  removed {stale.relative_to(ROOT)} (no longer declared)")
    counts = {"art": 0, "kept": 0, "placeholder": 0}
    for entries, draw_art, draw_placeholder in ((SPRITES, art_sprite, placeholder_sprite), (SHEETS, art_sheet, placeholder_sheet)):
        for entry in entries:
            target = OUTPUT / f"{entry.path}.png"
            target.parent.mkdir(parents=True, exist_ok=True)
            image, kind = render(entry, target, draw_art, draw_placeholder)
            counts[kind] += 1
            if image is not None:
                image.save(target)
    for folder in sorted((p for p in OUTPUT.rglob("*") if p.is_dir()), key=lambda p: -len(p.parts)):
        if not any(folder.iterdir()):
            folder.rmdir()
    SPEC_JSON.write_text(json.dumps(spec(), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    SPEC_DOC.parent.mkdir(parents=True, exist_ok=True)
    SPEC_DOC.write_text(brief() + "\n", encoding="utf-8")
    print(f"{len(SPRITES)} sprites, {len(SHEETS)} sheets ({counts['art']} from packs, {counts['kept']} kept, "
          f"{counts['placeholder']} placeholders); {SPEC_JSON.relative_to(ROOT)}, {SPEC_DOC.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
