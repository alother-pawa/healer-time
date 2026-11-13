# Spec ảnh (sinh tự động)

Sinh bởi `python Tools/ShapeGen/generate_shapes.py`; sửa bảng trong script đó rồi chạy lại, đừng sửa file này.
Ảnh thật thay ảnh hiện có **cùng tên, cùng kích thước**; Build All áp viền 9-slice, pivot và cắt sheet theo spec.
Một đơn vị thế giới = 128 px. Thư mục và pack theo chuẩn của pc-base (`Docs/STRUCTURE.md` của pc-base).

- **Nguồn:** ảnh lấy từ pack trong `workspace/sprite-asset` (phóng nguyên lần, giữ nét pixel) hoặc **placeholder** (hình cơ bản ghi tên, cần art thật).
- **9-slice N:** N px ở mỗi cạnh giữ nguyên khi kéo giãn (góc), phần giữa kéo giãn; `a/b/c/d` = trái/dưới/phải/trên.
- **Pivot:** điểm đặt của ảnh trong thế giới (nhân vật: giữa chân).
- **Sheet:** mỗi hàng một tag (trên xuống dưới theo bảng), frame từ trái sang phải, mỗi ô đúng một frame, không chừa khe.

## Còn là placeholder (cần art)

- `ui-common/popup-backdrop`
- `backgrounds/splash-background`
- `backgrounds/menu-background`
- `backgrounds/loading-background`
- `backgrounds/gameplay-lower`
- `screens/splash/splash-logo`
- `screens/loading/loading-bar-track`
- `screens/loading/loading-bar-mask`
- `screens/loading/loading-bar-fill`
- `screens/settings/slider-track`
- `screens/settings/slider-fill`
- `screens/settings/slider-handle`
- `screens/main-menu/stage`
- `screens/gameplay/board-mask`
- `screens/gameplay/icon-plus`
- `screens/gameplay/bar-track`
- `screens/gameplay/bar-fill-magic`
- `game/props/battlefield`
- `game/props/hp-bar-track`
- `game/props/hp-bar-fill`
- `game/props/hp-bar-fill-enemy`
- `game/props/armor-bar-fill`
- `game/characters/healer/healer`

Glyph phím/tay cầm (`ui-glyphs/`) cũng là placeholder, ít ưu tiên.

## `ui-common`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `button-primary.png` | 136×136 | rounded | 32 | giữa | Nút chính (Play, OK, Resume) | Mega Cozy UI Pack (dobo_ui, bản demo): `Buttons/Inflated/InflatedlButton1_marine.png` ×4 |
| `button-secondary.png` | 136×136 | rounded | 32 | giữa | Nút phụ, tab | Mega Cozy UI Pack (dobo_ui, bản demo): `Buttons/Square/SquareButton1_wood.png` ×4 |
| `focus-frame.png` | 136×136 | outline | 32 | giữa | Khung quanh control đang chọn (phím/tay cầm) | Mega Cozy UI Pack (dobo_ui, bản demo): `Frames/Frame1_yellow.png` ×4, khoét giữa |
| `popup-backdrop.png` | 128×128 | rect | — | giữa | Nền mờ phía sau popup | **placeholder** |
| `popup-background.png` | 136×136 | rounded | 32 | giữa | Khung panel của popup | Mega Cozy UI Pack (dobo_ui, bản demo): `Frames/Frame1_wood.png` ×4 |
| `popup-header.png` | 424×84 | rounded | 56/0/56/0 | giữa | Dải tiêu đề popup (ruy băng) | Mega Cozy UI Pack (dobo_ui, bản demo): `Headers/Header1_green.png` ×4 |
| `toast-background.png` | 264×148 | rounded | 32 | giữa | Nền thông báo nhỏ (toast) | Mega Cozy UI Pack (dobo_ui, bản demo): `Containers/SimpleColorContainers/SimpleColorContainer1_cream.png` ×4 |
| `row-background.png` | 264×148 | rounded | 32 | giữa | Nền một hàng danh sách (settings) | Mega Cozy UI Pack (dobo_ui, bản demo): `Containers/DoubleStripeContainer/DoubleStripeContainer1_cream.png` ×4 |
| `panel-wood.png` | 264×148 | rounded | 32 | giữa | Nền ô thông tin (HUD, ví coin) | Mega Cozy UI Pack (dobo_ui, bản demo): `Containers/SimpleColorContainers/SimpleColorContainer1_wood.png` ×4 |
| `coin-icon.png` | 64×64 | ellipse | — | giữa | Icon đồng coin | Raven Fantasy Icons (Clockwork Raven): ô (8, 2) ×2 |

## `backgrounds`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `splash-background.png` | 1920×1080 | rect | — | giữa | Nền splash | **placeholder** |
| `menu-background.png` | 1920×1080 | rect | — | giữa | Nền Main Menu (đồng cỏ, trời) | **placeholder** |
| `loading-background.png` | 1920×1080 | rect | — | giữa | Nền màn loading | **placeholder** |
| `gameplay-lower.png` | 1920×720 | rect | — | giữa | Nền phần dưới màn gameplay (sau bàn ghép, item, healer) | **placeholder** |

## `screens/splash`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `splash-logo.png` | 320×320 | rounded | — | giữa | Logo studio | **placeholder** |

## `screens/loading`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `loading-bar-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh tiến độ | **placeholder** |
| `loading-bar-mask.png` | 64×32 | bar | 12 | giữa | Mặt nạ bo đầu thanh (không hiện) | **placeholder** |
| `loading-bar-fill.png` | 128×128 | rect | — | giữa | Phần đầy của thanh tiến độ | **placeholder** |

## `screens/settings`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `slider-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh trượt | **placeholder** |
| `slider-fill.png` | 64×32 | bar | 12 | giữa | Phần đầy thanh trượt | **placeholder** |
| `slider-handle.png` | 40×40 | ellipse | — | giữa | Núm thanh trượt | **placeholder** |
| `stepper-left.png` | 54×102 | rect | — | giữa | Mũi tên trái của ô chọn ◄ giá trị ► | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow6_cream.png` ×3 |
| `stepper-right.png` | 54×102 | rect | — | giữa | Mũi tên phải của ô chọn ◄ giá trị ► | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow7_cream.png` ×3 |

## `screens/main-menu`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `character-card.png` | 160×220 | rounded | — | giữa | Thẻ một nhân vật trong danh sách | Mega Cozy UI Pack (dobo_ui, bản demo): `Cards/Card1_gray.png` ×4 |
| `character-card-selected.png` | 160×220 | rounded | — | giữa | Thẻ nhân vật đang chọn | Mega Cozy UI Pack (dobo_ui, bản demo): `Cards/Card1_purple.png` ×4 |
| `arrow-left.png` | 102×102 | rect | — | giữa | Nút quay về (trang nhân vật → menu) | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow3_wood.png` ×3 |
| `arrow-right.png` | 102×102 | rect | — | giữa | Nút sang trang nhân vật | Mega Cozy UI Pack (dobo_ui, bản demo): `Arrows/Arrow4_wood.png` ×3 |
| `stage.png` | 528×120 | ellipse | — | giữa | Bệ đứng của nhân vật ở menu (bóng cỏ) | **placeholder** |

## `screens/gameplay`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `gem-attack.png` | 64×64 | rect | — | giữa | Gem Attack: đỏ, hình thoi | Raven Fantasy Icons (Clockwork Raven): ô (10, 4) ×2, đổi màu 140° |
| `gem-heal.png` | 64×64 | ellipse | — | giữa | Gem Heal: xanh lá, hình tròn | Raven Fantasy Icons (Clockwork Raven): ô (10, 6) ×2 |
| `gem-defense.png` | 64×64 | rect | — | giữa | Gem Defense: xanh dương, hình khiên vuông | Raven Fantasy Icons (Clockwork Raven): ô (10, 0) ×2 |
| `gem-magic.png` | 64×64 | rect | — | giữa | Gem Magic: tím, hình tam giác (tinh thể nhọn) | Raven Fantasy Icons (Clockwork Raven): ô (10, 12) ×2 |
| `board-cell.png` | 66×66 | rounded | 12 | giữa | Ô nền của bàn ghép | Mega Cozy UI Pack (dobo_ui, bản demo): `Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png` ×2 |
| `board-cell-selected.png` | 70×70 | outline | 14 | giữa | Ô đang chọn trên bàn ghép | Mega Cozy UI Pack (dobo_ui, bản demo): `Inventory/ItemSlots/ItemSlotsSelected/SlotHover1_cream.png` ×2 |
| `board-frame.png` | 358×132 | outline | 24 | giữa | Khung gỗ quanh bàn ghép | Mega Cozy UI Pack (dobo_ui, bản demo): `Frames/EmptyWoodenFrame.png` ×2 |
| `board-mask.png` | 128×128 | rect | — | giữa | Lớp che bàn ghép khi đang diễn hiệu ứng | **placeholder** |
| `item-slot.png` | 132×132 | rounded | 24 | giữa | Ô item | Mega Cozy UI Pack (dobo_ui, bản demo): `Inventory/ItemSlots/ItemSlotsDefault/SlotDefaultl1_cream.png` ×4 |
| `item-potion.png` | 64×64 | rect | — | giữa | Item bình máu | Raven Fantasy Icons (Clockwork Raven): ô (17, 8) ×2 |
| `item-bomb.png` | 64×64 | ellipse | — | giữa | Item bom xoá một vùng 3×3 | Raven Fantasy Icons (Clockwork Raven): ô (47, 11) ×2 |
| `item-shuffle.png` | 64×64 | ellipse | — | giữa | Item xáo trộn bàn | Raven Fantasy Icons (Clockwork Raven): ô (49, 1) ×2 |
| `item-armor.png` | 64×64 | rect | — | giữa | Item tăng giáp tức thì | Raven Fantasy Icons (Clockwork Raven): ô (53, 7) ×2 |
| `icon-plus.png` | 48×48 | rounded | — | giữa | Dấu + trên ô item hết (mua bằng coin) | **placeholder** |
| `icon-pause.png` | 48×54 | rect | — | giữa | Icon nút Pause | Mega Cozy UI Pack (dobo_ui, bản demo): `Icons/IconsFlat_16px/IconFlat10.png` ×3 |
| `bar-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh Magic của healer | **placeholder** |
| `bar-fill-magic.png` | 64×32 | bar | 12 | giữa | Phần đầy thanh Magic | **placeholder** |

## `game/props`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `battlefield.png` | 1920×448 | rect | — | giữa | Nền chiến trường nhìn ngang (trời, cỏ), mặt đất ở 1/3 dưới | **placeholder** |
| `hp-bar-track.png` | 64×32 | bar | 12 | giữa | Rãnh thanh máu trên đầu nhân vật | **placeholder** |
| `hp-bar-fill.png` | 64×32 | bar | 12 | 0.0, 0.5 | Máu của hero | **placeholder** |
| `hp-bar-fill-enemy.png` | 64×32 | bar | 12 | 0.0, 0.5 | Máu của quái | **placeholder** |
| `armor-bar-fill.png` | 64×32 | bar | 12 | 0.0, 0.5 | Giáp ảo của hero (chồng trên thanh máu) | **placeholder** |

## `game/effects`

| Ảnh | Kích thước (px) | Hình | 9-slice | Pivot | Dùng cho | Nguồn |
|---|---|---|---|---|---|---|
| `fx-hit.png` | 128×128 | ellipse | — | giữa | Hiệu ứng trúng đòn | Raven Fantasy Icons (Clockwork Raven): ô (47, 14) ×4 |
| `fx-heal.png` | 128×128 | ellipse | — | giữa | Hiệu ứng hồi máu | Raven Fantasy Icons (Clockwork Raven): ô (48, 12) ×4 |
| `fx-shield.png` | 128×128 | rect | — | giữa | Hiệu ứng nhận giáp | Raven Fantasy Icons (Clockwork Raven): ô (53, 7) ×4 |
| `fx-magic.png` | 128×128 | ellipse | — | giữa | Hiệu ứng phép của healer nổ trên quái | Raven Fantasy Icons (Clockwork Raven): ô (48, 5) ×4 |
| `fx-magic-orb.png` | 96×96 | ellipse | — | giữa | Viên phép bay (gem Magic → healer, healer → quái) | Raven Fantasy Icons (Clockwork Raven): ô (48, 1) ×3 |

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
| `game/characters/soldier/soldier.png` | 336×264 | 2688×1584 | 0.4375, 0.25 | 1. idle: 6 × 100 ms; 2. walk: 8 × 90 ms; 3. attack: 6 × 70 ms, không lặp; 4. attack-heavy: 6 × 70 ms, không lặp; 5. hurt: 4 × 80 ms, không lặp; 6. death: 4 × 140 ms, không lặp | Hero Soldier (quay phải; game lật khi đứng bên phải) | Tiny RPG Character Pack (Soldier & Orc): `Soldier/Soldier/Soldier.png` hàng [0, 1, 2, 3, 5, 6], ×6 |
| `game/characters/orc/orc.png` | 336×264 | 2688×1584 | 0.4375, 0.25 | 1. idle: 6 × 100 ms; 2. walk: 8 × 90 ms; 3. attack: 6 × 80 ms, không lặp; 4. attack-heavy: 6 × 80 ms, không lặp; 5. hurt: 4 × 80 ms, không lặp; 6. death: 4 × 140 ms, không lặp | Quái Orc (mọi loại quái và boss dùng chung, đổi màu và cỡ) | Tiny RPG Character Pack (Soldier & Orc): `Orc/Orc/Orc.png` hàng [0, 1, 2, 3, 4, 5], ×6 |
| `game/characters/healer/healer.png` | 128×192 | 768×576 | giữa đáy | 1. idle: 4 × 160 ms; 2. cast: 6 × 80 ms, không lặp; 3. hurt: 3 × 80 ms, không lặp | Healer (khối tím nhạt, khác dáng hero) | **placeholder** |

