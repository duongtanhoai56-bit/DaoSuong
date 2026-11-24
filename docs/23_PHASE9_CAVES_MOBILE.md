# Phase 9 — Hang ngầm, bí mật và giao diện điện thoại

## Mục tiêu

Mở rộng Phase 8 thành các khu vực khám phá ngầm có nội dung cốt truyện, phần thưởng và điều kiện môi trường riêng; đồng thời hoàn thiện nền tảng HUD/điều khiển cảm ứng landscape.

## Nội dung hang động

- 6 hang gắn với 6 đảo: `home`, `forest`, `swamp`, `volcano`, `frost`, `ancient`.
- Mỗi hang có 3–5 phòng nối bằng đường hầm, độ sâu mặc định 34 đơn vị world, một nhánh phụ bí mật bị chặn bởi vách đá tương tác và một kho phụ. Chạm `TÁC ĐỘNG` vào vách đá để mở lối; trạng thái mở được lưu trong lượt hiện tại.
- Mỗi hang có guardian, màu đá/tinh thể, ánh sáng, nhiệt độ môi trường và mức phơi nhiễm riêng.
- Sinh nội dung khi người chơi lần đầu bước vào; chỉ hang đang hoạt động được bật để giới hạn renderer/collider/light trên điện thoại.
- Quái mặt đất không tấn công xuyên trần hang; quy tắc cứu người chơi khỏi rơi khỏi đảo không kéo người chơi ra khỏi khu vực ngầm.
- Không thể đi sang đảo khác khi đang ở dưới lòng đất; nút/cổng thoát đưa người chơi về đúng vị trí vào hang.

### Sáu mạch truyện

1. **Hầm Ngầm Thủy Triều** — lời cảnh báo về tiếng chuông dưới đáy biển và chìa khóa di tích.
2. **Lòng Rễ Thì Thầm** — rễ cây cổ và bản đồ lối vào núi lửa.
3. **Hố Sâu Đầm Đen** — hình ảnh phản chiếu và lọ nọc độc không đáy.
4. **Trái Tim Tro Đỏ** — cỗ máy dưới dung nham đang rút ngắn thời gian.
5. **Vực Lam Tĩnh Lặng** — bóng người trong băng và dấu chân không chủ.
6. **Thánh Đường Vọng Âm** — trung tâm của quần đảo và lời giải về màn sương.

Các manh mối chỉ được ghi nhận khi người chơi thu thập xong vật phẩm gắn với chúng hoặc tương tác với ký ức. `CaveJournalHUD` hiển thị lại ghi chép đã khám phá bằng bảng cuộn.

## Vật phẩm và tiến trình bí mật

Phase 9 tạo 11 vật phẩm riêng: ngọc trai hang, nấm phát sáng, hổ phách, nọc đầm lầy, lõi hắc diện, tinh hoa băng, cổ kim, cổ ngữ vọng âm, chìa khóa di tích, mảnh hóa thạch và bụi pha lê.

Chìa khóa di tích có tỷ lệ xuất hiện 100% trong Hầm Ngầm Thủy Triều. Kho lưu trữ ở Thánh Đường Vọng Âm yêu cầu chìa khóa này. Mỗi kho báu có ID ổn định để lưu trạng thái mở/đã lấy.

### An toàn lưu kho báu

- Save format hiện tại là v4; vẫn đọc được dữ liệu v1–v3.
- Lưu ID kho đã mở, ghi chép, trạng thái đã lấy và số lượng còn lại của một chồng vật phẩm.
- Nhặt một phần rồi thoát game sẽ không tạo lại phần đã lấy.
- Nhận vật phẩm và trừ chìa khóa dùng snapshot/rollback; thất bại khi trừ chìa khóa sẽ khôi phục túi đồ.
- Số lượng tải từ save luôn bị giới hạn 0–999.
- Nút `Chơi lại` của chiến dịch gọi reset hang: hủy dungeon runtime, xóa save loot/manh mối của lượt cũ và tạo lại khi bước vào hang ở lượt mới.

## Điều khiển điện thoại

- Landscape left/right; tắt xoay dọc để tránh HUD chồng lấn.
- Joystick trái, vùng nhìn camera, nhảy, tác động/thu thập và chạy nhanh; bật multi-touch.
- `SafeAreaFitter` co giãn HUD theo `Screen.safeArea` để tránh notch, camera đục lỗ và góc bo.
- `CanvasScaler` dùng reference 1920×1080 và các panel được dựng bên trong safe-area root.
- `MobileRuntimeBootstrap`: mục tiêu 60 FPS trên máy đủ RAM; giảm xuống 30 FPS, tắt shadow/anti-aliasing và giảm LOD khi RAM hệ thống dưới ngưỡng mặc định 3.5 GB.
- Khi đóng bảng nhật ký, khóa input gameplay được nhả để không kẹt joystick/action.

Các giá trị này là cấu hình khởi điểm, không phải cam kết FPS trên mọi điện thoại. Cần đo trên thiết bị thật và tinh chỉnh chất lượng theo model.

## Giới hạn hiện tại

- Hang được dựng bằng primitive mesh/collider và material runtime để có thể thử logic không cần asset nghệ thuật mới. Đây chưa phải mô hình hang, guardian hay hoạt ảnh phát hành cuối.
- Các phòng nối theo tuyến ngang ở dưới mặt đất, không phải hệ thống nhiều tầng thẳng đứng.
- Guardian là CreatureAgent dùng AI truy đuổi cơ bản; chưa có animation graph, pathfinding/navmesh, kỹ năng boss riêng hay loot table động.
- Nhật ký, kho báu, inventory, hang và guardian hiện chạy local. Phase 7 mới đồng bộ avatar/voice; chưa có authoritative sync cho người chơi co-op.
- Chưa có scene/prefab/ScriptableObject sinh tự động trong ZIP nguồn. Chạy Editor tools theo `24_PHASE9_SETUP_AND_TEST.md` để tạo scene và dữ liệu.
