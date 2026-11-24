# Giai đoạn 6 — Sinh tồn

## Phạm vi

- Máu, đói, khát, thể lực và nhiệt độ cơ thể.
- Đói/khát giảm theo thời gian; chạy nhanh làm tiêu hao nhanh hơn.
- Khi đói/khát về 0, nhân vật mất máu dần.
- Thể lực tiêu hao khi giữ nút CHẠY NHANH và hồi phục khi không chạy.
- Nhiệt độ môi trường được tính từ ngày/đêm, biome, độ cao và thời tiết; nhiệt độ cơ thể có độ trễ, không đồng nhất với nhiệt độ không khí.
- Chảy máu, bệnh, lạnh, quá nóng, no lâu, bù nước và kiệt sức được hiển thị.
- Vật phẩm tiêu thụ có thể hồi phục chỉ số, điều chỉnh nhiệt độ, chữa chảy máu/bệnh và áp dụng buff giảm tiêu hao. Nước dừa phục hồi khát (không cộng nhầm vào đói). Buff mạnh thay buff yếu; buff cùng mức làm mới thời gian; buff yếu không kéo dài buff mạnh.
- Nút DÙNG nằm trong Inventory; vật phẩm không bị tiêu hao nếu không đem lại tác dụng hoặc người chơi đã gục.

## Kiến trúc

- `SurvivalVitals.cs`: snapshot bất biến và enum trạng thái.
- `SurvivalModel` (trong `SurvivalVitals.cs`): mô phỏng thuần C#, không phụ thuộc Scene/Time/Physics/UI.
- `SurvivalSettings.cs`: cấu hình ScriptableObject và chuyển đổi sang tuning cho model.
- `ConsumableData.cs`: dữ liệu vật phẩm có thể sử dụng.
- `SurvivalController.cs`: adapter Unity, lấy môi trường từ IslandGenerator/DayNightCycle/WeatherSystem, xử lý tiêu thụ và phát sự kiện.
- `SurvivalHUD.cs`: HUD runtime-built trên Canvas.
- `SurvivalDebugPanel.cs`: chỉ xuất hiện trong Editor/Development Build.
- `Phase6SurvivalTools.cs`: tạo dữ liệu, nút chạy nhanh, nối các component và validate Scene.

## Thiết lập trong Unity

1. Mở project bằng Unity `6000.3.29f1` và đợi Package Manager hoàn tất.
2. Hoàn tất Scene của Phase 2–5: Player có `PlayerController`, `PlayerInventory`; Canvas có `MobileInput`, `InventoryHUD`, `GatherHud`; World có `IslandGenerator`, `DayNightCycle`, `WeatherSystem`.
3. Chạy `Đảo Sương > Giai đoạn 6 > 1 - Tạo dữ liệu sinh tồn`.
4. Chạy `Đảo Sương > Giai đoạn 6 > 2 - Nâng cấp Scene sinh tồn`, xác nhận lưu Scene.
5. Chạy `Đảo Sương > Giai đoạn 6 > 3 - Kiểm tra Scene sinh tồn`.
6. Nhấn Play. Dùng WASD + Left Shift hoặc giữ nút CHẠY NHANH. Mở TÚI, chọn thực phẩm/nước và bấm DÙNG.
7. Trong Editor/Development Build, bảng debug cho phép nhận vật phẩm thử, gây chảy máu/bệnh, nhận sát thương và hồi chỉ số.

## Kiểm thử

`Window > General > Test Runner > EditMode > Run All`. Hiện có 79 test case trong source: Gather 15, Inventory 24, Crafting 10, Input Gate 2, Survival Model 22 và Survival Controller 6. Chưa có kết quả chạy Unity Test Runner từ môi trường tạo project.

Bộ test bao gồm drain đói/khát, sprint multiplier, starvation/dehydration, stamina spend/regen, nhiệt độ, status effects có thời hạn, consumable/buff, chữa bệnh/chảy máu, death state, dữ liệu NaN, từ chối giá trị hồi phục âm và quy tắc làm mới buff.

## Giá trị mặc định

- Hunger drain: `0.0125` điểm/giây (100 điểm mất khoảng 133 phút nếu đứng yên).
- Thirst drain: `0.020` điểm/giây (100 điểm mất khoảng 83 phút nếu đứng yên).
- Sprint làm tăng drain đói `1.25x`, khát `1.5x`.
- Sprint speed mặc định: `7 m/s`; stamina cost `13 điểm/giây` (khoảng 7,7 giây chạy liên tục từ đầy); hồi `17 điểm/giây`.
- Nhiệt độ cơ thể mặc định `37°C`, ngưỡng lạnh `35.5°C`, quá nóng `38.5°C`.
- Model giới hạn delta simulation tối đa 2 giây mỗi tick để tránh một frame hitch lớn gây mất máu bất thường. Pause/background time chưa được tính là thời gian sinh tồn offline.

## Giới hạn đã biết

- Chưa có shelter/clothing/armor để điều chỉnh exposure; tích hợp sau khi xây dựng căn cứ hoàn thành.
- Death event đã được phát để các hệ thống sau (đồng đội hồi sinh/căn cứ) tích hợp; khi gục, di chuyển và tương tác thu thập/câu cá bị chặn; chưa có drop loot, revive hoặc respawn gameplay.
- Consumable là item data; đồ ăn nấu/recipe buff cần mở rộng trong các giai đoạn nấu ăn.
- Nhiệt độ là mô hình gameplay đơn giản hóa, không phải mô phỏng sinh lý.
- Phần Scene/Play/Android/iOS phải được kiểm tra trong Unity Editor và thiết bị thật; kiểm tra tĩnh ngoài Editor không thay thế compile/runtime.
