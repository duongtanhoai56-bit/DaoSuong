# Phase 8 — Setup và kiểm thử

## Điều kiện

- Unity Editor `6000.3.29f1` (Unity 6.3 LTS).
- Mở đúng thư mục có `Assets/`, `Packages/`, `ProjectSettings/`.
- Đợi Package Manager resolve xong; xử lý mọi lỗi package trước khi dùng Editor tools.
- Tạo URP Pipeline Asset và gán ở Graphics/Quality theo `03_PROJECT_SETUP.md`.
- Bật `Active Input Handling = Input System Package (New)`; nếu cần chạy IMGUI debug panel cũ trong Editor, chọn `Both`.

> Archive Phase 8 giữ source và Editor tools, không chứa scene/assets sinh tự động của Phase 0–7. Cần tạo scene và dữ liệu nền trong Unity Editor trước khi chạy nâng cấp Phase 8.

## Tạo scene nền theo thứ tự

1. `Đảo Sương → Phase 0 → Create Bootstrap Scene`.
2. `Đảo Sương → Phase 2 → 1 - Create World Assets`.
3. `Đảo Sương → Phase 2 → 2 - Create Playable Island Scene`; mở `SCN_IslandWorld` vừa tạo.
4. `Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD`.
5. `Đảo Sương → Giai đoạn 3 → 2 - Kiểm tra Action + Gather HUD`.
6. `Đảo Sương → Giai đoạn 4 → 1 - Nâng cấp Inventory cho Scene đang mở`; sau đó chạy `2 - Kiểm tra Inventory Scene`.
7. `Đảo Sương → Giai đoạn 5 → 1 - Tạo dữ liệu công thức mẫu`; sau đó `2 - Nâng cấp Scene chế tạo` và `3 - Kiểm tra Scene chế tạo`.
8. `Đảo Sương → Giai đoạn 6 → 1 - Tạo dữ liệu sinh tồn`; sau đó `2 - Nâng cấp Scene sinh tồn` và `3 - Kiểm tra Scene sinh tồn`.
9. **Tùy chọn online avatar/voice:** chạy Phase 7 theo `18_PHASE7_SETUP_AND_TEST.md`. Phase 7 cần link Unity Dashboard và cấu hình Multiplayer/Relay/Vivox nếu muốn tạo phòng và nói chuyện. Không cần Phase 7 để thử solo Phase 8.

## Sinh dữ liệu và nâng cấp Phase 8

1. Mở đúng scene chơi được có `Player`, `IslandGenerator`, `DayNightCycle`, `Canvas`, `M_Terrain.mat`.
2. Chọn `Game → Giai đoạn 8 → 1 - Tạo nội dung quần đảo 99 ngày`.
   - Tạo/cập nhật dữ liệu 6 đảo, 7 sinh vật, 6 boss, item thưởng, vật liệu hiếm và 7 công trình.
   - Tự gọi Phase 3 data creator để bảo đảm `GatherSettings` và dữ liệu cây/đá tồn tại.
   - Công cụ giữ lại asset đã có; nếu đã sửa thủ công asset thì kiểm tra lại cấu hình trước khi dùng.
3. Chọn `Game → Giai đoạn 8 → 2 - Nâng cấp Scene hiện tại`.
   - Tool gắn `CampaignManager`, `ArchipelagoManager`, `BaseBuildingManager`, `CombatController`, `CampaignHUD` và các reference.
   - Đồng hồ được đặt mặc định 360 giây/ngày (6 phút/ngày), có thể chỉnh tại `DayNightCycle` trong Inspector.
   - Nếu báo thiếu thành phần, hoàn tất Phase 2–6 trước; không bỏ qua lỗi reference.
4. Chọn `Game → Giai đoạn 8 → 3 - Kiểm tra Scene`.
5. Lưu Scene và các asset mới vào source control.

## Chạy thử trong Play Mode

### Vòng chơi / ngày 99

1. Nhấn Play; xác nhận chỉ một `ArchipelagoManager`, sáu đảo sinh thành công và Player bắt đầu ở Đảo Khởi Đầu.
2. HUD phải hiện `NGÀY SINH TỒN: 0/99` và tên đảo.
3. Với `Day Length Seconds = 360`, ngày đầu tiên hoàn tất sau phần còn lại của ngày đầu theo `startTime01`; những ngày sau cách nhau 6 phút thực tế.
4. Sống sót tới hết ngày 98: HUD phải báo `98/99`, chưa chiến thắng.
5. Sống hết ngày 99: hiện bảng `CHIẾN THẮNG`; thời gian bị dừng.
6. Dùng nút `CHƠI LẠI`; ngày về 0, boss progress của chiến dịch được reset và người chơi trở lại đảo đầu. Căn cứ đã lưu vẫn được giữ.
7. Thử giảm HP về 0 trong Development test: hiện `THẤT BẠI`, thời gian dừng; không được cộng thêm ngày sau khi chết.

### Quái / boss / đảo

1. Quái thường có mặt trên từng đảo; khi đến gần chúng đuổi theo và gây sát thương trong tầm.
2. Nút `TẤN CÔNG` chỉ đánh mục tiêu phía trước trong phạm vi; kiểm tra sát thương cơ bản và công cụ đang sở hữu.
3. Boss đầu tiên dự kiến xuất hiện từ ngày 2. Kiểm tra thanh HP ở đầu HUD.
4. Khi boss xuống dưới 50% máu, xác nhận chuyển giai đoạn hai: di chuyển nhanh hơn và đòn đánh diện rộng có tầm xa hơn, nhưng chỉ áp dụng một gói sát thương mỗi nhịp.
5. Hạ boss, xác nhận ID boss được lưu; khi đủ ngày và hạ đúng boss, đảo kế tiếp mở khóa.
6. Đảo khóa không thể dịch chuyển tới bằng nút HUD. Đảo hiện tại được đánh dấu riêng.

### Xây căn cứ / rương / nhiệt

1. Thu đủ gỗ và đá bằng hệ thống chặt cây/đập đá; mở `XÂY DỰNG`.
2. Chọn nền/tường/cửa/mái/thùng kho/lửa trại/tháp canh; thiếu nguyên liệu phải bị từ chối mà không mất đồ.
3. Xây ở trên đất trống; thử xây trên biển, bên trong đá/cây và quá xa để xác nhận bị chặn.
4. Mở rương bằng tương tác Storage hiện có; chuyển vật phẩm vào rương, thoát Play Mode rồi chạy lại. Nội dung rương được khôi phục từ local save.
5. Đứng gần lửa trại: kiểm tra `BodyTemperatureC` trong Survival HUD tăng so với khi ở xa.
6. Đóng bảng xây dựng/chọn đảo: xác nhận điều khiển nhân vật hoạt động lại. Mở bảng: joystick/nhìn/đánh phải bị khóa.

## Automated tests

- `Window → General → Test Runner → EditMode → Run All`.
- Phase 8 thêm `CampaignProgressRulesTests` bao gồm điều kiện ngày 99, đếm ngày không trùng, người chơi đã chết, điều kiện mở đảo theo ngày + boss, chi phí và vị trí xây.
- Các bài test mới được khai báo trong source nhưng chưa được chạy bởi môi trường đóng gói; ghi nhận số Passed/Failed thực tế sau khi chạy trong Unity Editor.

## Android kiểm thử

- Build Android Development trước, mở Console không có compile error.
- Test thiết bị thật: điều khiển cảm ứng, panel, inventory/rương, chiến đấu, FPS, nhiệt và tiêu thụ pin.
- Mục tiêu ban đầu 30 FPS trên thiết bị chuẩn; giảm số cây/đá trong từng `IslandSettings` nếu máy yếu.
- Nếu bật Phase 7, test hai máy thật để xác minh voice/Relay. Điều này vẫn không chứng minh gameplay Phase 8 được đồng bộ; các trạng thái chiến đấu/xây dựng hiện local.
