# Healer Time

Game PC 2D (Unity 6000.5.3f1, URP 2D, cửa sổ ngang 1920×1080): ghép 3 vô tận. Bàn ghép gem chiếm nửa trái màn hình; nửa phải trên là chiến trường (hero tự đánh quái, healer đứng sau hero, thanh Magic trên đầu healer), nửa phải dưới chỉ có thanh item hỗ trợ. Người chơi ghép gem để hero đánh mạnh hơn, hồi máu, nhận giáp, và nạp thanh Magic cho healer tung chiêu. Chơi đến khi hero gục; quái sau luôn mạnh hơn quái trước, cứ 5 con có một boss.

Dựng trên khung của `pc/pc-base` (cùng chuẩn: builder là nguồn của scene/prefab, mọi ảnh và âm thanh khai một lần trong `Tools/`, UI vẽ từ trạng thái, animation tắt được). Tài liệu chung của khung: `pc/pc-base/Docs/GUIDE.md`, `Docs/STRUCTURE.md`.

## Mở và chạy

1. Giữ cấu trúc workspace: manifest trỏ tới `../../../packages/...`.
2. Mở bằng Unity 6000.5.3f1, rồi **Game › Build All** (hoặc batchmode: `-executeMethod Game.Editor.Builders.BatchBuild.BuildAll`).
3. Mở `Assets/game/scenes/Boot.unity` và Play.

Scene, prefab, atlas và dữ liệu trong `Assets/game/data` (trừ `data/input`) do builder sinh, không commit. Số cân bằng nằm trong field mặc định của code (`BattleTuning`, các bảng ở `GameDataBuilder`); chỉnh trong Inspector được giữ qua mỗi lần build.

## Luồng

Boot → Splash → Main Menu (trang chủ ↔ trang Chọn nhân vật, trượt trái/phải) → Gameplay (Pause, Kết quả: Chơi lại tại chỗ hoặc về menu). Màn loading chỉ hiện khi chuyển scene lâu hơn 0,5 giây.

## Cách chơi (PC)

- Kéo một gem sang ô kề, hoặc bấm gem rồi bấm ô kề. Đổi không tạo hàng 3 thì gem trượt về; hết nước đi thì bàn tự xáo.
- Item: bấm ô item hoặc phím 1–4 (tay cầm: d-pad). Bom: bấm item rồi chọn ô trên bàn. Hết item: bấm "+" để mua bằng coin.
- Esc/Start: Pause. Hero gục là hết lượt (không hồi sinh).

| Gem | Hiệu ứng |
|---|---|
| Attack (đỏ, hình thoi) | Hero tung đòn mạnh vào quái |
| Heal (xanh lá, tròn) | Hồi máu hero |
| Defense (xanh dương, vuông) | Giáp ảo: chịu đòn trước máu, tụt dần theo thời gian |
| Magic (tím, tinh thể nhọn) | Nạp thanh Magic; đầy thì healer tung chiêu rồi thanh về 0 |

Ăn nhiều gem một lần, hoặc combo (bước rơi sau), thì hiệu ứng mạnh hơn. Trận không dừng khi ghép: mỗi đợt nổ trên bàn áp hiệu ứng ngay lúc nổ (banner tên gem và số), không đợi hết chuỗi rơi.

Mỗi đòn đánh (hero, quái, đòn mạnh từ gem Attack, chiêu healer) vung trước rồi mới trúng (`BattleTuning.AttackWindup`): số sát thương, thanh máu và rung màn hình đổi đúng lúc trúng. Đòn mạnh và chiêu tung ra khi quái kế tiếp còn đang chạy vào thì được giữ lại, đến khi quái tới nơi mới đánh, theo đúng thứ tự.

## Code

| Phần | Ở đâu |
|---|---|
| Logic match-3 (thuần, có test) | `scripts/systems/gameplay/board/` (`Match3Engine`, `MatchFinder`, `MoveFinder`) |
| Logic trận (thuần, có test) | `scripts/systems/gameplay/battle/` (`Battle`, `GemEffects`, `GemPayout`, `BattleTuning`) |
| Một lượt chơi = bàn + trận | `scripts/systems/gameplay/run/` (`Run`, `ItemEffects`, `RunFactory`) |
| Hiển thị gameplay | `scripts/systems/gameplay/views/`, `RunController` |
| Hero, healer và lựa chọn đã lưu | `scripts/contracts/roster/`, `scripts/systems/roster/` |
| Main menu, trang nhân vật | `scripts/systems/main-menu/` |
| Coin, item, mua item | package `com.paowa.economy` (ví, kho, shop catalog chỉ có item) |
| Builder | `scripts/editor/builders/` (`GameDataBuilder`, `GameplayPrefabBuilder`, `MenuPrefabBuilder`) |

## Mở rộng

- **Hero/healer mới:** thêm một dòng vào bảng `Heroes`/`Healers` của `GameDataBuilder` (sheet, màu, chỉ số) và key `hero.<id>.name/.description`; sheet mới thì khai ở `Tools/ShapeGen/generate_shapes.py`.
- **Item mới:** dòng mới ở bảng `Items`; loại hiệu ứng mới thì thêm vào `ItemEffectKind` và một `IItemEffect` trong `ItemEffects`. Mỗi item một ô (thanh item hiện có 4 ô).
- **Gem mới:** dòng mới ở bảng `Gems` (+ sprite trong ShapeGen, key `gem.<id>.name`, `gameplay.showcase.<id>`); loại hiệu ứng mới thì thêm vào `GemEffectKind` và một `IGemEffect` trong `GemEffects`.
- **Quái mới:** dòng mới ở bảng `Monsters` (sheet, màu, cỡ, chỉ số, mở từ con thứ mấy, có phải boss) và key `monster.<id>.name`.
- **Sheet nhân vật mới:** `strip_sheet(...)` trong ShapeGen, một strip PNG mỗi animation (quái: idle, run, attack, hurt, death; hero thêm attack-heavy; healer: idle, cast, hurt). Frame không vuông thì truyền `frame_width`. ShapeGen ghi chiều cao dáng idle (`height`) vào spec; thanh máu/Magic và số bay đặt theo đó.

## Art và âm thanh

Danh sách đầy đủ và nguồn từng ảnh: [Docs/ART_SPEC.md](Docs/ART_SPEC.md) (mục "Còn là placeholder"); âm thanh: [Docs/AUDIO_SPEC.md](Docs/AUDIO_SPEC.md) (toàn bộ là placeholder). Art lấy từ `workspace/sprite-asset`: `character-for-healer-time` (hero: Martial Hero, Fantasy Warrior, Hero Knight, Huntress; healer: Evil Wizard 2 và 3; quái: Goblin, Skeleton, Mushroom, Flying Eye, Fire Worm, boss Medieval King), Raven Fantasy Icons (gem, item, hiệu ứng, coin), Mega Cozy UI Pack và FantasyUIfree (nút, khung, ô, thanh máu boss). Thay art: ghi đè PNG/WAV cùng tên rồi Build All.

## Kiểm

- `python Tools/check/check.py` trước mỗi commit.
- Test EditMode: `Match3EngineTests`, `BattleTests` và các test của khung.
