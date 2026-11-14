# Spec âm thanh (sinh tự động)

Sinh bởi `python Tools/AudioGen/generate_sounds.py`; sửa bảng trong script đó rồi chạy lại, đừng sửa file này.

**Giao file:** WAV (44,1 hoặc 48 kHz, 16 hoặc 24 bit), **cùng tên và cùng thư mục** với placeholder; Unity tự nén theo nhóm khi Build All. Không đổi đuôi file (đổi đuôi là file mới, mất liên kết trong game).

**Âm lặp:** điểm cuối nối liền điểm đầu (không click, không khoảng lặng); độ dài có thể khác placeholder nếu trọn ô nhịp.

| Nhóm | Kênh | Mức | Unity nhập |
|---|---|---|---|
| `music` | stereo | khoảng -16 LUFS, đỉnh ≤ -1 dBFS | Streaming, Vorbis |
| `ambience` | stereo | khoảng -24 LUFS (nằm dưới nhạc) | Compressed In Memory, Vorbis |
| `sfx` | mono | đỉnh ≤ -1 dBFS, đều nhau giữa các tiếng | Decompress On Load, ADPCM |
| `ui` | mono | đỉnh ≤ -3 dBFS, nhẹ hơn sfx | Decompress On Load, ADPCM |
| `voice` | mono | khoảng -18 LUFS, đỉnh ≤ -1 dBFS | Compressed In Memory, Vorbis |

| File | Nhóm | Độ dài | Lặp | Dùng cho | Nguồn |
|---|---|---|---|---|---|
| `ui/volume-preview.wav` | ui | 0.04 s | không | Phát ở nhóm âm của thanh âm lượng khi kéo (Settings), để nghe ngay mức mới | Comigo's Bleeps n Bloops: `Press_04.wav` |
| `ui/button-click.wav` | ui | 0.16 s | không | Bấm nút menu, popup | Comigo's Bleeps n Bloops: `Press_03.wav` |
| `ui/page-swipe.wav` | ui | 0.39 s | không | Trượt trang Main Menu ↔ Chọn nhân vật | Comigo's Bleeps n Bloops: `Press_01.wav` |
| `ui/coin.wav` | ui | 0.2 s | không | Nhận coin (hạ quái), mua item | **placeholder** |
| `music/menu-theme.wav` | music | 8 s | có | Nhạc Main Menu, lặp liền mạch | **placeholder** |
| `music/battle-theme.wav` | music | 8 s | có | Nhạc trong trận, lặp liền mạch | **placeholder** |
| `sfx/gem-swap.wav` | sfx | 0.02 s | không | Hai gem đổi chỗ | Comigo's Bleeps n Bloops: `Press_05.wav` |
| `sfx/gem-invalid.wav` | sfx | 0.2 s | không | Đổi chỗ không tạo match, gem trượt về | **placeholder** |
| `sfx/gem-match.wav` | sfx | 0.36 s | không | Một nhóm gem nổ (cao dần theo combo) | Comigo's Bleeps n Bloops: `BreakGlass_01.wav` |
| `sfx/board-shuffle.wav` | sfx | 0.5 s | không | Bàn tự xáo khi hết nước đi, hoặc item xáo | **placeholder** |
| `sfx/bomb.wav` | sfx | 0.61 s | không | Item bom nổ trên bàn | Comigo's Bleeps n Bloops: `BlastImpact_01.wav` |
| `sfx/hero-attack.wav` | sfx | 0.31 s | không | Hero vung đòn thường | Comigo's Bleeps n Bloops: `ShootShort.wav` |
| `sfx/hero-strike.wav` | sfx | 0.65 s | không | Đòn mạnh của hero từ gem Attack | Comigo's Bleeps n Bloops: `HeavyShot_01.wav` |
| `sfx/monster-attack.wav` | sfx | 0.86 s | không | Quái đánh | Comigo's Bleeps n Bloops: `Shoot_02.wav` |
| `sfx/hit.wav` | sfx | 0.24 s | không | Trúng đòn (hero hoặc quái) | Comigo's Bleeps n Bloops: `Impact_01.wav` |
| `sfx/heal.wav` | sfx | 0.81 s | không | Hồi máu từ gem Heal hoặc bình máu | Comigo's Bleeps n Bloops: `PowerUp_01.wav` |
| `sfx/shield.wav` | sfx | 1.06 s | không | Nhận giáp ảo từ gem Defense hoặc item | Comigo's Bleeps n Bloops: `MetalBars_01.wav` |
| `sfx/magic-charge.wav` | sfx | 0.37 s | không | Thanh Magic của healer nạp thêm | Comigo's Bleeps n Bloops: `EnergyZap.wav` |
| `sfx/healer-skill.wav` | sfx | 0.74 s | không | Healer tung đòn hỗ trợ khi thanh Magic đầy | Comigo's Bleeps n Bloops: `HeavyZap.wav` |
| `sfx/monster-death.wav` | sfx | 0.72 s | không | Quái gục | Comigo's Bleeps n Bloops: `BlastImpact_02.wav` |
| `sfx/boss-appear.wav` | sfx | 1.41 s | không | Boss xuất hiện | Comigo's Bleeps n Bloops: `Alarm_01.wav` |
| `sfx/hero-death.wav` | sfx | 2.75 s | không | Hero gục, hết lượt | Comigo's Bleeps n Bloops: `Shutdown.wav` |
