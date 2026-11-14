"""Generates every image of the game, plus the image spec read by the builders and by artists.

Run from the repository root:  python Tools/ShapeGen/generate_shapes.py   (needs Pillow)

Each image is declared once (SPRITES, SHEETS) and comes from one of two places:
- art: an image (or a crop of one) from the third-party packs in workspace/sprite-asset, scaled up by a whole factor with
  nearest-neighbour sampling (pixel art stays crisp), optionally recoloured or with its centre cleared;
- placeholder: a plain shape (rectangle, rounded rectangle, ellipse, outline, bar) at the exact size the final art must
  have, in a colour per role, with no text in it (Docs/ART_SPEC.md names every image; animation sheets get one shape per
  frame cell).
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

from PIL import Image, ImageDraw

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
FANTASY_UI = "FantasyUIfree/FantasyUIfree"
CHARACTERS = "character-for-healer-time"
IMPACT_FX = "Free Effect Bullet Impact Explosion 32x32 V1"
DUNGEON = "0x72_DungeonTilesetII_v1.7/0x72_DungeonTilesetII_v1.7"
PACK_NAMES = {COZY: "Mega Cozy UI Pack (dobo_ui, bản demo)", RAVEN: "Raven Fantasy Icons (Clockwork Raven)",
              FANTASY_UI: "Fantasy UI free", CHARACTERS: "character-for-healer-time (LuizMelo, CC0)",
              IMPACT_FX: "Free Effect Bullet Impact Explosion 32x32",
              DUNGEON: "0x72 Dungeon Tileset II (CC0)"}


@dataclass(frozen=True)
class Art:
    """An image of a pack: file (or Raven icon cell), crop box, whole-number scale, hue shift in degrees, cleared centre."""
    pack: str
    file: str = ""
    cell: tuple = None          # Raven icon (row, column) on the 32 px sheet
    box: tuple = None           # (left, top, right, bottom) taken from the file
    scale: int = 1
    hue: int = 0
    clear_inset: int = 0        # >0: pixels this far inside the edge become transparent (a frame from a filled box)
    strips: tuple = ()          # sheets: one animation strip file per tag (frames left to right), in tag order
    frame_width: int = 0        # sheets: width of a frame in the strips (0: square frames, as wide as the strip is high)
    crop: tuple = None          # sheets: (left, top, right, bottom) taken from every frame of the strips
    tiles: str = ""             # a backdrop laid out from dungeon tiles: hall (wall over a floor), floor, wall
    ground: int = 0             # hall: height of the floor band under the wall, in tile-sheet pixels
    shade: float = 0.0          # darkening over the whole backdrop (0..1), so characters and UI stand out


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
    plain: bool = False   # a flat colour shape that needs no art (dimmers, simple bars): not listed as missing art


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
    ppu: float = 0.0      # texture pixels per world unit of this sheet (0: PIXELS_PER_UNIT); pixel art shown large uses fewer
    point: bool = False   # nearest filtering (pixel art shown larger than its texture)
    height: float = 0.0   # world height of the idle pose over the feet (bars and effects sit by it)


def units(width, height):
    """A world size in units, in pixels."""
    return round(width * PIXELS_PER_UNIT), round(height * PIXELS_PER_UNIT)


def px(side):
    return side, side


def cozy(file, scale, clear_inset=0):
    return Art(COZY, file=file, scale=scale, clear_inset=clear_inset)


def raven(row, column, scale, hue=0):
    return Art(RAVEN, cell=(row, column), scale=scale, hue=hue)


def fantasy(box, scale, file="freefantasy.png"):
    return Art(FANTASY_UI, file=file, box=box, scale=scale)


def dungeon(tiles, ground=0, shade=0.0):
    return Art(DUNGEON, file="0x72_DungeonTilesetII_v1.7.png", tiles=tiles, ground=ground, shade=shade, scale=4)


def effect_frames(name, colour, row, column, fill, note):
    """Four 32 px frames of one effect of the impact pack (one row, `column` the first), shown x4: game/effects/<name>-<n>."""
    file = f"{colour} Effect Bullet Impact Explosion 32x32.png"
    return [Sprite(f"game/effects/{name}-{i}", px(128), "ellipse", fill, f"{note} (frame {i + 1}/4)",
                   art=Art(IMPACT_FX, file=file, box=((column + i) * 32, row * 32, (column + i + 1) * 32, (row + 1) * 32),
                           scale=4)) for i in range(4)]


def previous_sheet(path):
    """The sheet's entry in the art spec already written (used when the packs are not on this machine)."""
    if SPEC_JSON.exists():
        for sheet in json.loads(SPEC_JSON.read_text(encoding="utf-8")).get("sheets", []):
            if sheet["path"] == path:
                return sheet
    return None


def strip_sheet(path, folder, tags, scale, note, colour=CHARACTER, frame_width=0):
    """
    A character sheet from a folder of animation strips (one PNG per animation, frames left to right, square unless
    `frame_width` says otherwise): one row per
    tag, every frame cut to the box holding the character in every frame of these strips (2 px margin), pivot under the
    feet of the first tag (idle). Shown `scale` screen pixels per texture pixel (point filtered). Its height is the first
    idle frame's, from the feet to the top of the head.
    tags: (name, strip file without extension, ms per frame, loop).
    """
    root = PACKS / CHARACTERS / folder
    files = tuple(f"{file}.png" for _, file, _, _ in tags)
    art = Art(CHARACTERS, file=folder, strips=files, frame_width=frame_width)
    if root.exists():
        counts, union, idle = [], None, None
        for index, file in enumerate(files):
            image = Image.open(root / file).convert("RGBA")
            side, wide = image.height, frame_width or image.height
            counts.append(image.width // wide)
            strip_box = None
            for column in range(image.width // wide):
                box = image.crop((column * wide, 0, column * wide + wide, side)).getbbox()
                if box:
                    strip_box = box if strip_box is None else (min(strip_box[0], box[0]), min(strip_box[1], box[1]),
                                                                max(strip_box[2], box[2]), max(strip_box[3], box[3]))
            if index == 0:
                # The feet: the opaque columns of the lowest 8 rows of the first idle frame (capes and weapons left out).
                first = image.crop((0, 0, wide, side))
                first_box = first.getbbox()
                feet = first.crop((0, first_box[3] - 8, wide, first_box[3])).getbbox()
                idle = (feet[0], first_box[1], feet[2], first_box[3])
            union = strip_box if union is None else (min(union[0], strip_box[0]), min(union[1], strip_box[1]),
                                                     max(union[2], strip_box[2]), max(union[3], strip_box[3]))
        left, top = max(0, union[0] - 2), max(0, union[1] - 2)
        right, bottom = min(wide, union[2] + 2), min(side, union[3] + 2)
        frame = (right - left, bottom - top)
        pivot = (round(((idle[0] + idle[2]) / 2 - left) / frame[0], 4), round((bottom - idle[3]) / frame[1], 4))
        art = Art(CHARACTERS, file=folder, strips=files, frame_width=frame_width, crop=(left, top, right, bottom))
        height = round((idle[3] - idle[1]) * scale / PIXELS_PER_UNIT, 4)
    else:
        old = previous_sheet(path)
        if old is None:
            raise FileNotFoundError(f"{path}: pack folder {root} missing and no earlier spec entry")
        counts = [tag["frames"] for tag in old["tags"]]
        frame = (old["frameWidth"], old["frameHeight"])
        pivot = tuple(old["pivot"])
        height = old.get("height", 0.0)
    return Sheet(path, frame, "rounded", colour, note,
                 tags=[Tag(name, count, ms, loop) for (name, _, ms, loop), count in zip(tags, counts)],
                 pivot=pivot, art=art, ppu=round(PIXELS_PER_UNIT / scale, 4), point=True, height=height)


# Sizes are the size the art is drawn at: the size shown at the 1920x1080 reference for pictures, the source size
# (with its border) for 9-slice frames. Pixel art is scaled by a whole factor, so its declared size is a multiple of the
# source. Glyphs show at 48 px and are drawn at 64 for sharper downscaling.
S, BAR, GLYPH = (128, 128), (64, 32), (64, 64)
PILL, TRIGGER = (64, 40), (64, 48)
SCREEN = (1920, 1080)
GEM, ICON = px(96), px(96)
HANGING_BOARD = (165, 72, 236, 139)       # the blank hanging board of freefantasy.png, without its chain
DRAGON_BAR_FULL = (0, 0, 112, 32)         # first and last of the eight states of Dragonhpbar.png
DRAGON_BAR_EMPTY = (784, 0, 896, 32)

SPRITES = [
    # ui-common: shared UI (several screens).
    Sprite("ui-common/button-primary", (136, 136), "rounded", "#3A9C8F", "Nút chính (Play, OK, Resume)", border=32,
           art=cozy("Buttons/Inflated/InflatedlButton1_marine.png", 4)),
    Sprite("ui-common/button-secondary", (136, 136), "rounded", WOOD, "Nút phụ, tab", border=32,
           art=cozy("Buttons/Square/SquareButton1_wood.png", 4)),
    Sprite("ui-common/focus-frame", (136, 136), "outline", FOCUS, "Khung quanh control đang chọn (phím/tay cầm)", border=32,
           art=cozy("Frames/Frame1_yellow.png", 4, clear_inset=5)),
    Sprite("ui-common/popup-backdrop", S, "rect", "#000000", "Nền mờ phía sau popup", opacity=0.6, plain=True),
    Sprite("ui-common/popup-background", (284, 268), "rounded", WOOD, "Khung panel của popup (bảng gỗ)", border=44,
           art=fantasy(HANGING_BOARD, 4)),
    Sprite("ui-common/popup-header", (424, 84), "rounded", LEAF, "Dải tiêu đề popup (ruy băng)", border=(56, 0, 56, 0),
           art=cozy("Headers/Header1_green.png", 4)),
    Sprite("ui-common/toast-background", (264, 148), "rounded", CREAM, "Nền thông báo nhỏ (toast)", border=32,
           art=cozy("Containers/SimpleColorContainers/SimpleColorContainer1_cream.png", 4)),
    Sprite("ui-common/row-background", (264, 148), "rounded", CREAM, "Nền một hàng danh sách (settings)", border=32,
           art=cozy("Containers/DoubleStripeContainer/DoubleStripeContainer1_cream.png", 4)),
    Sprite("ui-common/panel-wood", (135, 87), "rounded", WOOD, "Biển gỗ của ô thông tin (HUD, ví coin)", border=24,
           art=fantasy((138, 2, 183, 31), 3)),
    Sprite("ui-common/coin-icon", px(64), "ellipse", FOCUS, "Icon đồng coin", art=raven(8, 2, 2)),
    # backgrounds: full screen, never packed.
    Sprite("backgrounds/splash-background", SCREEN, "rect", "#F3DDB3", "Nền splash"),
    Sprite("backgrounds/menu-background", SCREEN, "rect", "#F3DDB3", "Nền Main Menu (sảnh dungeon)",
           art=dungeon("hall", ground=72, shade=0.25)),
    Sprite("backgrounds/loading-background", SCREEN, "rect", "#F3DDB3", "Nền màn loading", art=dungeon("wall", shade=0.55)),
    # screens/splash
    Sprite("screens/splash/splash-logo", px(320), "rounded", MAGIC, "Logo studio"),
    # screens/loading
    Sprite("screens/loading/loading-bar-track", BAR, "bar", WOOD_DARK, "Rãnh thanh tiến độ", border=12, plain=True),
    Sprite("screens/loading/loading-bar-mask", BAR, "bar", "#FFFFFF", "Mặt nạ bo đầu thanh (không hiện)", border=12, plain=True),
    Sprite("screens/loading/loading-bar-fill", S, "rect", LEAF, "Phần đầy của thanh tiến độ", plain=True),
    # screens/settings
    Sprite("screens/settings/slider-track", BAR, "bar", WOOD_DARK, "Rãnh thanh trượt", border=12, plain=True),
    Sprite("screens/settings/slider-fill", BAR, "bar", "#3A9C8F", "Phần đầy thanh trượt", border=12, plain=True),
    Sprite("screens/settings/slider-handle", px(40), "ellipse", LIGHT, "Núm thanh trượt", plain=True),
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
    Sprite("screens/main-menu/stage", (528, 120), "ellipse", "#000000", "Bóng dưới chân nhân vật ở menu", opacity=0.45,
           plain=True),
    # screens/gameplay: board, items, bars, HUD.
    Sprite("screens/gameplay/gem-attack", GEM, "rect", HEALTH, "Gem Attack: đỏ, hình thoi", art=raven(10, 4, 3, hue=140)),
    Sprite("screens/gameplay/gem-heal", GEM, "ellipse", "#46A758", "Gem Heal: xanh lá, hình tròn", art=raven(10, 6, 3)),
    Sprite("screens/gameplay/gem-defense", GEM, "rect", ARMOR, "Gem Defense: xanh dương, hình khiên vuông", art=raven(10, 0, 3)),
    Sprite("screens/gameplay/gem-magic", GEM, "rect", MAGIC, "Gem Magic: tím, hình tam giác (tinh thể nhọn)", art=raven(10, 12, 3)),
    Sprite("screens/gameplay/board-cell", (66, 66), "rounded", CREAM, "Ô nền của bàn ghép", border=12,
           art=cozy("Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png", 2)),
    Sprite("screens/gameplay/board-cell-selected", (70, 70), "outline", FOCUS, "Ô đang chọn trên bàn ghép", border=14,
           art=cozy("Inventory/ItemSlots/ItemSlotsSelected/SlotHover1_cream.png", 2)),
    Sprite("screens/gameplay/board-frame", (284, 268), "outline", WOOD, "Khung gỗ quanh bàn ghép (chỉ vẽ viền)", border=44,
           art=fantasy(HANGING_BOARD, 4)),
    Sprite("screens/gameplay/item-slot", (132, 132), "rounded", CREAM, "Ô item", border=24,
           art=cozy("Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png", 4)),
    Sprite("screens/gameplay/item-potion", ICON, "rect", HEALTH, "Item bình máu", art=raven(17, 8, 3)),
    Sprite("screens/gameplay/item-bomb", ICON, "ellipse", PANEL, "Item bom xoá một vùng 3×3", art=raven(47, 11, 3)),
    Sprite("screens/gameplay/item-shuffle", ICON, "ellipse", ARMOR, "Item xáo trộn bàn", art=raven(49, 1, 3)),
    Sprite("screens/gameplay/item-armor", ICON, "rect", ARMOR, "Item tăng giáp tức thì", art=raven(53, 7, 3)),
    Sprite("screens/gameplay/icon-plus", px(56), "rounded", LEAF, "Nút + trên ô item hết (mua bằng coin)",
           art=fantasy((1, 193, 15, 207), 4)),
    Sprite("screens/gameplay/button-pause", px(84), "rounded", WOOD, "Nút Pause", art=fantasy((33, 177, 47, 191), 6)),
    Sprite("screens/gameplay/enemy-bar-empty", (448, 128), "bar", WOOD_DARK, "Thanh máu quái trên HUD: khung rỗng",
           art=fantasy(DRAGON_BAR_EMPTY, 4, file="Dragonhpbar.png")),
    Sprite("screens/gameplay/enemy-bar-full", (448, 128), "bar", HEALTH, "Thanh máu quái trên HUD: đầy (cắt theo máu còn lại)",
           art=fantasy(DRAGON_BAR_FULL, 4, file="Dragonhpbar.png")),
    # game: world images. Backgrounds at their screen size; bars are sliced; characters are sheets (below).
    Sprite("game/props/battlefield", (1088, 648), "rect", SKY,
           "Nền chiến trường (nửa phải, phía trên): thấy 960×520 ở giữa, tràn 64 px mỗi cạnh để rung không lộ mép; "
           "mặt đất cách đáy 104 px",
           pivot=CENTER, art=dungeon("hall", ground=26)),
    Sprite("game/props/camp", (960, 560), "rect", "#8C5A3C", "Nền khu item (nửa phải, phía dưới)",
           pivot=CENTER, art=dungeon("floor", shade=0.35)),
    Sprite("game/props/board-backdrop", (960, 1080), "rect", "#5B3A2B", "Nền sau bàn ghép (nửa trái)", pivot=CENTER,
           art=dungeon("wall", shade=0.55)),
    Sprite("game/props/hp-bar-track", BAR, "bar", WOOD_DARK, "Rãnh thanh máu trên đầu nhân vật", border=12, plain=True),
    Sprite("game/props/hp-bar-fill", BAR, "bar", "#46A758", "Máu của hero", border=12, pivot=(0.0, 0.5), plain=True),
    Sprite("game/props/hp-bar-fill-enemy", BAR, "bar", HEALTH, "Máu của quái", border=12, pivot=(0.0, 0.5), plain=True),
    Sprite("game/props/magic-bar-fill", BAR, "bar", MAGIC, "Thanh Magic trên đầu healer", border=12, pivot=(0.0, 0.5), plain=True),
    Sprite("game/props/armor-bar-fill", BAR, "bar", ARMOR, "Giáp ảo của hero (chồng trên thanh máu)", border=12, pivot=(0.0, 0.5),
           plain=True),
] + effect_frames("fx-hit", "Red", 11, 16, "#FF9A3C", "Hiệu ứng trúng đòn") \
  + effect_frames("fx-heal", "Green", 7, 6, "#46A758", "Hiệu ứng hồi máu") \
  + effect_frames("fx-shield", "Blue", 7, 16, ARMOR, "Hiệu ứng nhận giáp") \
  + effect_frames("fx-magic", "Purple", 12, 16, MAGIC, "Hiệu ứng phép nổ (nạp Magic, chiêu healer)") \
  + effect_frames("fx-magic-orb", "Purple", 2, 11, MAGIC, "Viên phép bay từ healer tới quái, lặp trong lúc bay") + [
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


def hero(name, folder, heavy, hurt):
    return strip_sheet(f"game/characters/{name}/{name}", folder,
                       [("idle", "Idle", 90, True), ("run", "Run", 80, True), ("attack", "Attack1", 70, False),
                        ("attack-heavy", heavy, 70, False), ("hurt", hurt, 80, False), ("death", "Death", 110, False)],
                       4, "Hero (quay phải; game lật khi đứng bên phải)", ARMOR)


def healer(name, folder, cast, hurt, scale):
    return strip_sheet(f"game/characters/{name}/{name}", folder,
                       [("idle", "Idle", 100, True), ("cast", cast, 70, False), ("hurt", hurt, 80, False)],
                       scale, "Healer (đứng cạnh thanh Magic, quay trái)", CHARACTER)


def monster(name, folder, idle, run, hurt, scale, note="Quái (quay phải, đi vào từ trái)"):
    return strip_sheet(f"game/characters/{name}/{name}", folder,
                       [("idle", idle, 100, True), ("run", run, 80, True), ("attack", "Attack", 75, False),
                        ("hurt", hurt, 80, False), ("death", "Death", 110, False)],
                       scale, note, HEALTH)


MONSTERS = "Monsters_Creatures_Fantasy/Monsters_Creatures_Fantasy/"
SHEETS = [
    hero("martial-hero", "Martial Hero/Martial Hero/Sprites", "Attack2", "Take Hit"),
    hero("fantasy-warrior", "Fantasy Warrior/Fantasy Warrior/Sprites", "Attack3", "Take hit"),
    hero("hero-knight", "Hero Knight/Hero Knight/Sprites", "Attack2", "Take Hit"),
    hero("huntress", "Huntress/Huntress/Sprites", "Attack3", "Take hit"),
    healer("evil-wizard-2", "EVil Wizard 2/EVil Wizard 2/Sprites", "Attack1", "Take hit", 3),
    healer("evil-wizard-3", "Evil Wizard 3/Evil Wizard 3/Sprites", "Attack", "Get hit", 6),
    monster("goblin", MONSTERS + "Goblin", "Idle", "Run", "Take Hit", 5),
    monster("skeleton", MONSTERS + "Skeleton", "Idle", "Walk", "Take Hit", 4),
    monster("mushroom", MONSTERS + "Mushroom", "Idle", "Run", "Take Hit", 5),
    monster("flying-eye", MONSTERS + "Flying eye", "Flight", "Flight", "Take Hit", 5),
    monster("fire-worm", "Fire Worm/Fire Worm/Sprites/Worm", "Idle", "Walk", "Get Hit", 4),
    strip_sheet("game/characters/fallen-king/fallen-king", "Medieval King Pack 2/Medieval King Pack 2/Sprites",
                [("idle", "Idle", 90, True), ("run", "Run", 80, True), ("attack", "Attack1", 75, False),
                 ("hurt", "Take Hit", 80, False), ("death", "Death", 110, False)],
                5, "Boss (quay phải)", HEALTH, frame_width=160),
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


def borders(border):
    return list(border) if isinstance(border, tuple) else [border] * 4


def name_of(path):
    return path.rsplit("/", 1)[-1]


def placeholder_sprite(sprite):
    width, height = sprite.size
    return shape(width, height, sprite.shape, sprite.colour, sprite.opacity)


def placeholder_sheet(sheet):
    fw, fh = sheet.frame
    columns = max(tag.frames for tag in sheet.tags)
    image = Image.new("RGBA", (fw * columns, fh * len(sheet.tags)), rgba(sheet.colour, 0.0))
    cell = shape(fw, fh, sheet.shape, sheet.colour, 1.0)
    for row, tag in enumerate(sheet.tags):
        for column in range(tag.frames):
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
    """The pack image (or its icon cell, or a box of it) scaled by its whole factor, centred on a canvas of the declared size."""
    art = sprite.art
    image = Image.open(path).convert("RGBA")
    if art.tiles:
        return dungeon_backdrop(sprite, image, art)
    if art.cell:
        row, column = art.cell
        image = image.crop((column * 32, row * 32, column * 32 + 32, row * 32 + 32))
    if art.box:
        image = image.crop(art.box)
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
    """One row per tag from its animation strip, every frame cut to the sheet's box (texture pixels, unscaled)."""
    art = sheet.art
    fw, fh = sheet.frame
    columns = max(tag.frames for tag in sheet.tags)
    image = Image.new("RGBA", (fw * columns, fh * len(sheet.tags)), (0, 0, 0, 0))
    left, top, right, bottom = art.crop
    for row, (tag, file) in enumerate(zip(sheet.tags, art.strips)):
        strip = Image.open(path / file).convert("RGBA")
        wide = art.frame_width or strip.height
        for column in range(tag.frames):
            cell = strip.crop((column * wide + left, top, column * wide + right, bottom))
            image.paste(cell, (column * fw, row * fh))
    return image


# Tiles of the 0x72 dungeon sheet: (left, top, width, height).
WALL, WALL_TOP = (32, 16, 16, 16), (32, 0, 16, 16)
BANNERS = [(16, 32, 16, 16), (32, 32, 16, 16), (16, 48, 16, 16), (32, 48, 16, 16)]
COLUMN = (80, 80, 16, 48)
FLOORS = [(16, 64, 16, 16)] * 30 + [(32, 64, 16, 16), (48, 64, 16, 16), (16, 80, 16, 16), (32, 80, 16, 16),
                                    (48, 80, 16, 16), (16, 96, 16, 16), (32, 96, 16, 16)]


def dungeon_backdrop(sprite, sheet, art):
    """
    A backdrop laid out from dungeon tiles at the sheet's size, then scaled by its whole factor (crisp pixels). hall: brick
    wall over a floor band `ground` px high, banners every fourth tile and columns between them, the top shaded; floor:
    floor tiles only; wall: bricks only. Tiles are placed from the bottom-left so the floor line is exact; varied floor tiles
    come from a fixed seed (the same image every run). `shade` darkens it all.
    """
    import random
    width, height = -(-sprite.size[0] // art.scale), -(-sprite.size[1] // art.scale)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 255))
    tile = lambda box: sheet.crop((box[0], box[1], box[0] + box[2], box[1] + box[3]))
    floor_top = height - art.ground if art.tiles == "hall" else (0 if art.tiles == "floor" else height)
    pick = random.Random(sprite.path)
    for y in range(floor_top, height, 16):
        for x in range(0, width, 16):
            canvas.paste(tile(pick.choice(FLOORS)), (x, y))
    for row, y in enumerate(range(floor_top - 16, -16, -16)):
        for column, x in enumerate(range(0, width, 16)):
            canvas.paste(tile(WALL), (x, y))
            if art.tiles == "hall" and row == 5 and column % 8 == 3:
                canvas.paste(tile(BANNERS[(column // 8) % len(BANNERS)]), (x, y))
    if art.tiles == "hall":
        for x in range(7 * 16, width, 8 * 16):
            canvas.alpha_composite(tile(COLUMN), (x, floor_top - COLUMN[3]))
        # Darker towards the top: the eye stays on the floor where characters stand.
        fade = Image.new("L", (1, height))
        for y in range(height):
            fade.putpixel((0, y), int(150 * max(0.0, 1.0 - y / max(1, floor_top))))
        canvas = Image.composite(Image.new("RGBA", canvas.size, (0, 0, 0, 255)), canvas, fade.resize(canvas.size))
    if art.shade:
        canvas = Image.blend(canvas, Image.new("RGBA", canvas.size, (0, 0, 0, 255)), art.shade)
    image = canvas.resize((width * art.scale, height * art.scale), Image.NEAREST)
    return image.crop((0, image.height - sprite.size[1], sprite.size[0], image.height))


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
             "pixelsPerUnit": s.ppu, "point": s.point, "height": s.height,
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
    if getattr(entry, "plain", False):
        return "lớp màu phẳng (không cần art)"
    if entry.art is None:
        return "**placeholder**"
    art = entry.art
    if art.tiles:
        return f"{PACK_NAMES[art.pack]}: ghép tile ({art.tiles}) ×{art.scale}"
    where = f"ô ({art.cell[0]}, {art.cell[1]})" if art.cell else f"`{art.file}`"
    if art.box:
        where += f" vùng {art.box}"
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
        "(hình khối màu trơn, không chữ, cần art thật).",
        "- **9-slice N:** N px ở mỗi cạnh giữ nguyên khi kéo giãn (góc), phần giữa kéo giãn; `a/b/c/d` = trái/dưới/phải/trên.",
        "- **Pivot:** điểm đặt của ảnh trong thế giới (nhân vật: giữa chân).",
        "- **Sheet:** mỗi hàng một tag (trên xuống dưới theo bảng), frame từ trái sang phải, mỗi ô đúng một frame, không chừa khe.",
        "",
    ]
    placeholders = [s.path for s in SPRITES + SHEETS
                    if s.art is None and not getattr(s, "plain", False) and not s.path.startswith("ui-glyphs/")]
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
            scale = PIXELS_PER_UNIT / s.ppu if s.ppu else 1
            src = "**placeholder**" if s.art is None else (f"{PACK_NAMES[s.art.pack]}: `{s.art.file}` "
                                                         f"({', '.join(f[:-4] for f in s.art.strips)}), hiện ×{scale:g}")
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
