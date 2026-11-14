# Spec ảnh (sinh tự động)

Sinh bởi `python Tools/ShapeGen/generate_shapes.py`; sửa bảng trong script đó rồi chạy lại, đừng sửa file này.
Ảnh thật thay ảnh hiện có **cùng tên, cùng kích thước**; Build All áp viền 9-slice, pivot và cắt sheet theo spec.
Một đơn vị thế giới = 128 px. Thư mục và pack theo chuẩn của pc-base (`Docs/STRUCTURE.md` của pc-base).

- **Nguồn:** ảnh lấy từ pack trong `workspace/sprite-asset` (phóng nguyên lần, giữ nét pixel) hoặc **placeholder** (hình cơ bản ghi tên, cần art thật).
- **9-slice N:** N px ở mỗi cạnh giữ nguyên khi kéo giãn (góc), phần giữa kéo giãn; `a/b/c/d` = trái/dưới/phải/trên.
- **Pivot:** điểm đặt của ảnh trong thế giới (nhân vật: giữa chân).
- **Sheet:** mỗi hàng một tag (trên xuống dưới theo bảng), frame từ trái sang phải, mỗi ô đúng một frame, không chừa khe.

## Còn là placeholder (cần art)

- `backgrounds/splash-background`
- `backgrounds/menu-background`
- `backgrounds/loading-background`
- `screens/splash/splash-logo`
- `game/props/battlefield`
- `game/props/camp`
- `game/props/board-backdrop`

Glyph phím/tay cầm (`ui-glyphs/`) cũng là placeholder, ít ưu tiên.

## `ui-common`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `button-primary.png` | 136×136 | rounded | 32 | giữa | Nút chính (Play, OK, Resume) | Mega Cozy UI Pack (dobo_ui, bản demo): `Buttons/Inflated/InflatedlButton1_marine.png` ×4 |
| `button-secondary.png` | 136×136 | rounded | 32 | giữa | Nút phụ, tab | Mega Cozy UI Pack (dobo_ui, bản demo): `Buttons/Square/SquareButton1_wood.png` ×4 |
| `focus-frame.png` | 136×136 | outline | 32 | giữa | Khung quanh control đang chọn (phím/tay cầm) | Mega Cozy UI Pack (dobo_ui, bản demo): `Frames/Frame1_yellow.png` ×4, khoét giữa |
| `popup-backdrop.png` | 128×128 | rect | — | giữa | Nền mờ phía sau popup | lớp màu phẳng (không cần art) |
| `popup-background.png` | 284×268 | rounded | 44 | giữa | Khung panel của popup (bảng gỗ) | Fantasy UI free: `freefantasy.png` vùng (165, 72, 236, 139) ×4 |
| `popup-header.png` | 424×84 | rounded | 56/0/56/0 | giữa | Dải tiêu đề popup (ruy băng) | Mega Cozy UI Pack (dobo_ui, bản demo): `Headers/Header1_green.png` ×4 |
| `toast-background.png` | 264×148 | rounded | 32 | giữa | Nền thông báo nhỏ (toast) | Mega Cozy UI Pack (dobo_ui, bản demo): `Containers/SimpleColorContainers/SimpleColorContainer1_cream.png` ×4 |
| `row-background.png` | 264×148 | rounded | 32 | giữa | Nền một hàng danh sách (settings) | Mega Cozy UI Pack (dobo_ui, bản demo): `Containers/DoubleStripeContainer/DoubleStripeContainer1_cream.png` ×4 |
| `panel-wood.png` | 135×87 | rounded | 24 | giữa | Biển gỗ của ô thông tin (HUD, ví coin) | Fantasy UI free: `freefantasy.png` vùng (138, 2, 183, 31) ×3 |
| `coin-icon.png` | 64×64 | ellipse | — | giữa | Icon đồng coin | Raven Fantasy Icons (Clockwork Raven): ô (8, 2) ×2 |

## `backgrounds`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `splash-background.png` | 1920×1080 | rect | — | giữa | Nền splash | **placeholder** |
| `menu-background.png` | 1920×1080 | rect | — | giữa | Nền Main Menu (đồng cỏ, trời) | **placeholder** |
| `loading-background.png` | 1920×1080 | rect | — | giữa | Nền màn loading | **placeholder** |

## `screens/splash`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `splash-logo.png` | 320×320 | rounded | — | giữa | Logo studio | **placeholder** |

## `screens/loading`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `loading-bar-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh tiến độ | lớp màu phẳng (không cần art) |
| `loading-bar-mask.png` | 64×32 | bar | 12 | giữa | Mặt nạ bo đầu thanh (không hiện) | lớp màu phẳng (không cần art) |
| `loading-bar-fill.png` | 128×128 | rect | — | giữa | Phần đầy của thanh tiến độ | lớp màu phẳng (không cần art) |

## `screens/settings`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `slider-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh trượt | lớp màu phẳng (không cần art) |
| `slider-fill.png` | 64×32 | bar | 12 | giữa | Phần đầy thanh trượt | lớp màu phẳng (không cần art) |
| `slider-handle.png` | 40×40 | ellipse | — | giữa | Núm thanh trượt | lớp màu phẳng (không cần art) |
| `stepper-left.png` | 54×102 | rect | — | giữa | Mũi tên trái của ô chọn ◄ giá trị ► | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow6_cream.png` ×3 |
| `stepper-right.png` | 54×102 | rect | — | giữa | Mũi tên phải của ô chọn ◄ giá trị ► | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow7_cream.png` ×3 |

## `screens/main-menu`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `character-card.png` | 160×220 | rounded | — | giữa | Thẻ một nhân vật trong danh sách | Mega Cozy UI Pack (dobo_ui, bản demo): `Cards/Card1_gray.png` ×4 |
| `character-card-selected.png` | 160×220 | rounded | — | giữa | Thẻ nhân vật đang chọn | Mega Cozy UI Pack (dobo_ui, bản demo): `Cards/Card1_purple.png` ×4 |
| `arrow-left.png` | 102×102 | rect | — | giữa | Nút quay về (trang nhân vật → menu) | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow3_wood.png` ×3 |
| `arrow-right.png` | 102×102 | rect | — | giữa | Nút sang trang nhân vật | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow4_wood.png` ×3 |
| `stage.png` | 528×120 | ellipse | — | giữa | Bệ đứng của nhân vật ở menu (bóng cỏ) | lớp màu phẳng (không cần art) |

## `screens/gameplay`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `gem-attack.png` | 96×96 | rect | — | giữa | Gem Attack: đỏ, hình thoi | Raven Fantasy Icons (Clockwork Raven): ô (10, 4) ×3, đổi màu 140° |
| `gem-heal.png` | 96×96 | ellipse | — | giữa | Gem Heal: xanh lá, hình tròn | Raven Fantasy Icons (Clockwork Raven): ô (10, 6) ×3 |
| `gem-defense.png` | 96×96 | rect | — | giữa | Gem Defense: xanh dương, hình khiên vuông | Raven Fantasy Icons (Clockwork Raven): ô (10, 0) ×3 |
| `gem-magic.png` | 96×96 | rect | — | giữa | Gem Magic: tím, hình tam giác (tinh thể nhọn) | Raven Fantasy Icons (Clockwork Raven): ô (10, 12) ×3 |
| `board-cell.png` | 66×66 | rounded | 12 | giữa | Ô nền của bàn ghép | Mega Cozy UI Pack (dobo_ui, bản demo): `Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png` ×2 |
| `board-cell-selected.png` | 70×70 | outline | 14 | giữa | Ô đang chọn trên bàn ghép | Mega Cozy UI Pack (dobo_ui, bản demo): `Inventory/ItemSlots/ItemSlotsSelected/SlotHover1_cream.png` ×2 |
| `board-frame.png` | 284×268 | outline | 44 | giữa | Khung gỗ quanh bàn ghép (chỉ vẽ viền) | Fantasy UI free: `freefantasy.png` vùng (165, 72, 236, 139) ×4 |
| `item-slot.png` | 132×132 | rounded | 24 | giữa | Ô item | Mega Cozy UI Pack (dobo_ui, bản demo): `Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png` ×4 |
| `item-potion.png` | 96×96 | rect | — | giữa | Item bình máu | Raven Fantasy Icons (Clockwork Raven): ô (17, 8) ×3 |
| `item-bomb.png` | 96×96 | ellipse | — | giữa | Item bom xoá một vùng 3×3 | Raven Fantasy Icons (Clockwork Raven): ô (47, 11) ×3 |
| `item-shuffle.png` | 96×96 | ellipse | — | giữa | Item xáo trộn bàn | Raven Fantasy Icons (Clockwork Raven): ô (49, 1) ×3 |
| `item-armor.png` | 96×96 | rect | — | giữa | Item tăng giáp tức thì | Raven Fantasy Icons (Clockwork Raven): ô (53, 7) ×3 |
| `icon-plus.png` | 56×56 | rounded | — | giữa | Nút + trên ô item hết (mua bằng coin) | Fantasy UI free: `freefantasy.png` vùng (1, 193, 15, 207) ×4 |
| `button-pause.png` | 84×84 | rounded | — | giữa | Nút Pause | Fantasy UI free: `freefantasy.png` vùng (33, 177, 47, 191) ×6 |
| `enemy-bar-empty.png` | 448×128 | bar | — | giữa | Thanh máu quái trên HUD: khung rỗng | Fantasy UI free: `Dragonhpbar.png` vùng (784, 0, 896, 32) ×4 |
| `enemy-bar-full.png` | 448×128 | bar | — | giữa | Thanh máu quái trên HUD: đầy (cắt theo máu còn lại) | Fantasy UI free: `Dragonhpbar.png` vùng (0, 0, 112, 32) ×4 |

## `game/props`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `battlefield.png` | 1088×648 | rect | — | giữa | Nền chiến trường (nửa phải, phía trên): thấy 960×520 ở giữa, tràn 64 px mỗi cạnh để rung không lộ mép; mặt đất cách đáy 104 px | **placeholder** |
| `camp.png` | 960×560 | rect | — | giữa | Nền trại của healer (nửa phải, phía dưới: item, healer) | **placeholder** |
| `board-backdrop.png` | 960×1080 | rect | — | giữa | Nền sau bàn ghép (nửa trái) | **placeholder** |
| `hp-bar-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh máu trên đầu nhân vật | lớp màu phẳng (không cần art) |
| `hp-bar-fill.png` | 64×32 | bar | 12 | 0.0, 0.5 | Máu của hero | lớp màu phẳng (không cần art) |
| `hp-bar-fill-enemy.png` | 64×32 | bar | 12 | 0.0, 0.5 | Máu của quái | lớp màu phẳng (không cần art) |
| `magic-bar-fill.png` | 64×32 | bar | 12 | 0.0, 0.5 | Thanh Magic trên đầu healer | lớp màu phẳng (không cần art) |
| `armor-bar-fill.png` | 64×32 | bar | 12 | 0.0, 0.5 | Giáp ảo của hero (chồng trên thanh máu) | lớp màu phẳng (không cần art) |

## `game/effects`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `fx-hit.png` | 128×128 | ellipse | — | giữa | Hiệu ứng trúng đòn | Raven Fantasy Icons (Clockwork Raven): ô (47, 14) ×4 |
| `fx-heal.png` | 128×128 | ellipse | — | giữa | Hiệu ứng hồi máu | Raven Fantasy Icons (Clockwork Raven): ô (48, 12) ×4 |
| `fx-shield.png` | 128×128 | rect | — | giữa | Hiệu ứng nhận giáp | Raven Fantasy Icons (Clockwork Raven): ô (53, 7) ×4 |
| `fx-magic.png` | 128×128 | ellipse | — | giữa | Hiệu ứng phép của healer nổ trên quái | Raven Fantasy Icons (Clockwork Raven): ô (48, 5) ×4 |
| `fx-magic-orb.png` | 150×150 | ellipse | — | giữa | Viên phép bay (gem Magic → healer, healer → quái) | character-for-healer-time (LuizMelo, CC0): `Evil Wizard 3/Evil Wizard 3/Sprites/Projectile/Moving.png` vùng (0, 0, 50, 50) ×3 |

## `ui-glyphs`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `key.png` | 64×64 | rounded | 16 | giữa | Phím bàn phím (chữ phím do prompt viết) | **placeholder** |
| `key-arrow-up.png` | 64×64 | rounded | — | giữa | Phím mũi tên | **placeholder** |
| `key-arrow-down.png` | 64×64 | rounded | — | giữa | Phím mũi tên | **placeholder** |
| `key-arrow-left.png` | 64×64 | rounded | — | giữa | Phím mũi tên | **placeholder** |
| `key-arrow-right.png` | 64×64 | rounded | — | giữa | Phím mũi tên | **placeholder** |
| `mouse-left.png` | 64×64 | rounded | — | giữa | Chuột: phần được dùng | **placeholder** |
| `mouse-right.png` | 64×64 | rounded | — | giữa | Chuột: phần được dùng | **placeholder** |
| `mouse-middle.png` | 64×64 | rounded | — | giữa | Chuột: phần được dùng | **placeholder** |
| `mouse-scroll.png` | 64×64 | rounded | — | giữa | Chuột: phần được dùng | **placeholder** |
| `mouse-move.png` | 64×64 | rounded | — | giữa | Chuột: phần được dùng | **placeholder** |
| `xbox-face-south.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `xbox-face-east.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `xbox-face-west.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `xbox-face-north.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `playstation-face-south.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `playstation-face-east.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `playstation-face-west.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `playstation-face-north.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `switch-face-south.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `switch-face-east.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `switch-face-west.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `switch-face-north.png` | 64×64 | ellipse | 24 | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `generic-face-south.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `generic-face-east.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `generic-face-west.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `generic-face-north.png` | 64×64 | ellipse | — | giữa | Nút mặt tay cầm (vị trí: south = dưới) | **placeholder** |
| `gamepad-shoulder-left.png` | 64×40 | rounded | 16 | giữa | Nút vai trái (LB/L1/L) | **placeholder** |
| `gamepad-shoulder-right.png` | 64×40 | rounded | 16 | giữa | Nút vai phải | **placeholder** |
| `gamepad-trigger-left.png` | 64×48 | rounded | 20 | giữa | Cò trái (LT/L2/ZL) | **placeholder** |
| `gamepad-trigger-right.png` | 64×48 | rounded | 20 | giữa | Cò phải | **placeholder** |
| `gamepad-stick-left.png` | 64×64 | ellipse | 24 | giữa | Cần trái | **placeholder** |
| `gamepad-stick-right.png` | 64×64 | ellipse | 24 | giữa | Cần phải | **placeholder** |
| `gamepad-dpad.png` | 64×64 | rect | — | giữa | D-pad | **placeholder** |
| `gamepad-dpad-up.png` | 64×64 | rect | — | giữa | D-pad: một hướng | **placeholder** |
| `gamepad-dpad-down.png` | 64×64 | rect | — | giữa | D-pad: một hướng | **placeholder** |
| `gamepad-dpad-left.png` | 64×64 | rect | — | giữa | D-pad: một hướng | **placeholder** |
| `gamepad-dpad-right.png` | 64×64 | rect | — | giữa | D-pad: một hướng | **placeholder** |
| `gamepad-start.png` | 64×40 | rounded | 16 | giữa | Nút Start/Menu | **placeholder** |
| `gamepad-select.png` | 64×40 | rounded | 16 | giữa | Nút Select/View | **placeholder** |

## Sheet animation

| Ảnh | Frame (px) | Sheet (px) | Pivot | Tag (hàng): frame × ms, lặp | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `game/characters/martial-hero/martial-hero.png` | 132×73 | 1056×438 | 0.2311, 0.0274 | 1. idle: 8 × 90 ms; 2. run: 8 × 80 ms; 3. attack: 6 × 70 ms, không lặp; 4. attack-heavy: 6 × 70 ms, không lặp; 5. hurt: 4 × 80 ms, không lặp; 6. death: 6 × 110 ms, không lặp | Hero (quay phải; game lật khi đứng bên phải) | character-for-healer-time (LuizMelo, CC0): `Martial Hero/Martial Hero/Sprites` (Idle, Run, Attack1, Attack2, Take Hit, Death), hiện ×4 |
| `game/characters/fantasy-warrior/fantasy-warrior.png` | 141×103 | 1410×618 | 0.4362, 0.0194 | 1. idle: 10 × 90 ms; 2. run: 8 × 80 ms; 3. attack: 7 × 70 ms, không lặp; 4. attack-heavy: 8 × 70 ms, không lặp; 5. hurt: 3 × 80 ms, không lặp; 6. death: 7 × 110 ms, không lặp | Hero (quay phải; game lật khi đứng bên phải) | character-for-healer-time (LuizMelo, CC0): `Fantasy Warrior/Fantasy Warrior/Sprites` (Idle, Run, Attack1, Attack3, Take hit, Death), hiện ×4 |
| `game/characters/hero-knight/hero-knight.png` | 135×85 | 1485×510 | 0.3815, 0.0353 | 1. idle: 11 × 90 ms; 2. run: 8 × 80 ms; 3. attack: 7 × 70 ms, không lặp; 4. attack-heavy: 7 × 70 ms, không lặp; 5. hurt: 4 × 80 ms, không lặp; 6. death: 11 × 110 ms, không lặp | Hero (quay phải; game lật khi đứng bên phải) | character-for-healer-time (LuizMelo, CC0): `Hero Knight/Hero Knight/Sprites` (Idle, Run, Attack1, Attack2, Take Hit, Death), hiện ×4 |
| `game/characters/huntress/huntress.png` | 95×69 | 760×414 | 0.4895, 0.029 | 1. idle: 8 × 90 ms; 2. run: 8 × 80 ms; 3. attack: 5 × 70 ms, không lặp; 4. attack-heavy: 7 × 70 ms, không lặp; 5. hurt: 3 × 80 ms, không lặp; 6. death: 8 × 110 ms, không lặp | Hero (quay phải; game lật khi đứng bên phải) | character-for-healer-time (LuizMelo, CC0): `Huntress/Huntress/Sprites` (Idle, Run, Attack1, Attack3, Take hit, Death), hiện ×4 |
| `game/characters/evil-wizard-2/evil-wizard-2.png` | 166×145 | 1328×435 | 0.3193, 0.0138 | 1. idle: 8 × 100 ms; 2. cast: 8 × 70 ms, không lặp; 3. hurt: 3 × 80 ms, không lặp | Healer (đứng cạnh thanh Magic, quay trái) | character-for-healer-time (LuizMelo, CC0): `EVil Wizard 2/EVil Wizard 2/Sprites` (Idle, Attack1, Take hit), hiện ×3 |
| `game/characters/evil-wizard-3/evil-wizard-3.png` | 92×70 | 1196×210 | 0.2554, 0.0286 | 1. idle: 10 × 100 ms; 2. cast: 13 × 70 ms, không lặp; 3. hurt: 3 × 80 ms, không lặp | Healer (đứng cạnh thanh Magic, quay trái) | character-for-healer-time (LuizMelo, CC0): `Evil Wizard 3/Evil Wizard 3/Sprites` (Idle, Attack, Get hit), hiện ×6.00001 |
| `game/characters/goblin/goblin.png` | 92×50 | 736×250 | 0.4837, 0.04 | 1. idle: 4 × 100 ms; 2. run: 8 × 80 ms; 3. attack: 8 × 75 ms, không lặp; 4. hurt: 4 × 80 ms, không lặp; 5. death: 4 × 110 ms, không lặp | Quái (quay phải, đi vào từ trái) | character-for-healer-time (LuizMelo, CC0): `Monsters_Creatures_Fantasy/Monsters_Creatures_Fantasy/Goblin` (Idle, Run, Attack, Take Hit, Death), hiện ×5 |
| `game/characters/skeleton/skeleton.png` | 102×63 | 816×315 | 0.299, 0.0635 | 1. idle: 4 × 100 ms; 2. run: 4 × 80 ms; 3. attack: 8 × 75 ms, không lặp; 4. hurt: 4 × 80 ms, không lặp; 5. death: 4 × 110 ms, không lặp | Quái (quay phải, đi vào từ trái) | character-for-healer-time (LuizMelo, CC0): `Monsters_Creatures_Fantasy/Monsters_Creatures_Fantasy/Skeleton` (Idle, Walk, Attack, Take Hit, Death), hiện ×4 |
| `game/characters/mushroom/mushroom.png` | 75×49 | 600×245 | 0.4333, 0.0408 | 1. idle: 4 × 100 ms; 2. run: 8 × 80 ms; 3. attack: 8 × 75 ms, không lặp; 4. hurt: 4 × 80 ms, không lặp; 5. death: 4 × 110 ms, không lặp | Quái (quay phải, đi vào từ trái) | character-for-healer-time (LuizMelo, CC0): `Monsters_Creatures_Fantasy/Monsters_Creatures_Fantasy/Mushroom` (Idle, Run, Attack, Take Hit, Death), hiện ×5 |
| `game/characters/flying-eye/flying-eye.png` | 60×45 | 480×225 | 0.55, 0.2444 | 1. idle: 8 × 100 ms; 2. run: 8 × 80 ms; 3. attack: 8 × 75 ms, không lặp; 4. hurt: 4 × 80 ms, không lặp; 5. death: 4 × 110 ms, không lặp | Quái (quay phải, đi vào từ trái) | character-for-healer-time (LuizMelo, CC0): `Monsters_Creatures_Fantasy/Monsters_Creatures_Fantasy/Flying eye` (Flight, Flight, Attack, Take Hit, Death), hiện ×5 |
| `game/characters/fire-worm/fire-worm.png` | 79×58 | 1264×290 | 0.3734, 0.0345 | 1. idle: 9 × 100 ms; 2. run: 9 × 80 ms; 3. attack: 16 × 75 ms, không lặp; 4. hurt: 3 × 80 ms, không lặp; 5. death: 8 × 110 ms, không lặp | Quái (quay phải, đi vào từ trái) | character-for-healer-time (LuizMelo, CC0): `Fire Worm/Fire Worm/Sprites/Worm` (Idle, Walk, Attack, Get Hit, Death), hiện ×4 |
| `game/characters/fallen-king/fallen-king.png` | 114×72 | 912×360 | 0.3333, 0.0278 | 1. idle: 8 × 90 ms; 2. run: 8 × 80 ms; 3. attack: 4 × 75 ms, không lặp; 4. hurt: 4 × 80 ms, không lặp; 5. death: 6 × 110 ms, không lặp | Boss (quay phải) | character-for-healer-time (LuizMelo, CC0): `Medieval King Pack 2/Medieval King Pack 2/Sprites` (Idle, Run, Attack1, Take Hit, Death), hiện ×5 |

