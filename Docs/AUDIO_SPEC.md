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

| File | Nhóm | Độ dài | Lặp | Dùng cho |
|---|---|---|---|---|
| `ui/volume-preview.wav` | ui | 0.12 s | không | Phát ở nhóm âm của thanh âm lượng khi kéo (Settings), để nghe ngay mức mới |
| `ui/button-click.wav` | ui | 0.08 s | không | Bấm nút menu, popup |
| `ui/page-swipe.wav` | ui | 0.25 s | không | Trượt trang Main Menu ↔ Chọn nhân vật |
| `ui/coin.wav` | ui | 0.2 s | không | Nhận coin (hạ quái), mua item |
| `music/menu-theme.wav` | music | 8 s | có | Nhạc Main Menu, lặp liền mạch |
| `music/battle-theme.wav` | music | 8 s | có | Nhạc trong trận, lặp liền mạch |
| `sfx/gem-swap.wav` | sfx | 0.12 s | không | Hai gem đổi chỗ |
| `sfx/gem-invalid.wav` | sfx | 0.2 s | không | Đổi chỗ không tạo match, gem trượt về |
| `sfx/gem-match.wav` | sfx | 0.25 s | không | Một nhóm gem nổ (cao dần theo combo) |
| `sfx/board-shuffle.wav` | sfx | 0.5 s | không | Bàn tự xáo khi hết nước đi, hoặc item xáo |
| `sfx/bomb.wav` | sfx | 0.6 s | không | Item bom nổ trên bàn |
| `sfx/hero-attack.wav` | sfx | 0.18 s | không | Hero vung đòn thường |
| `sfx/hero-strike.wav` | sfx | 0.4 s | không | Đòn mạnh của hero từ gem Attack |
| `sfx/monster-attack.wav` | sfx | 0.2 s | không | Quái đánh |
| `sfx/hit.wav` | sfx | 0.15 s | không | Trúng đòn (hero hoặc quái) |
| `sfx/heal.wav` | sfx | 0.45 s | không | Hồi máu từ gem Heal hoặc bình máu |
| `sfx/shield.wav` | sfx | 0.4 s | không | Nhận giáp ảo từ gem Defense hoặc item |
| `sfx/magic-charge.wav` | sfx | 0.35 s | không | Thanh Magic của healer nạp thêm |
| `sfx/healer-skill.wav` | sfx | 0.7 s | không | Healer tung đòn hỗ trợ khi thanh Magic đầy |
| `sfx/monster-death.wav` | sfx | 0.4 s | không | Quái gục |
| `sfx/boss-appear.wav` | sfx | 0.9 s | không | Boss xuất hiện |
| `sfx/hero-death.wav` | sfx | 0.9 s | không | Hero gục, hết lượt |
| `sfx/revive.wav` | sfx | 0.8 s | không | Hồi sinh |
