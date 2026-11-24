# Hướng dẫn tạo game Đảo Sương tổng hợp — Phase 0–9

## 1. Môi trường

- Unity Hub + Editor `6000.3.29f1`.
- Cài Android Build Support gồm SDK, NDK và OpenJDK nếu cần build APK/AAB.
- Mở đúng thư mục project có `Assets/`, `Packages/`, `ProjectSettings/`.
- Chờ Package Manager resolve xong; xử lý tất cả lỗi package/compile trước khi tạo Scene.
- Tạo/gán URP Pipeline Asset theo `03_PROJECT_SETUP.md`.
- `Active Input Handling`: Input System Package (New); chọn Both khi cần IMGUI debug tools cũ.

## 2. Dùng trình tạo game tổng hợp

Mở menu:

`Đảo Sương → 00 - Trình tạo game tổng hợp (Phase 0–9)`

Chạy lần lượt các bước sau, giữ đúng thứ tự. Các tool cũ sẽ hiển thị hộp thoại xác nhận, đặc biệt khi tạo HUD hoặc thay Scene. Nếu Scene đã có dữ liệu riêng, sao lưu trước và không chọn Thay thế khi chưa chắc chắn.

1. **Phase 2 — Thế giới và Scene:** tạo IslandSettings/materials và `SCN_IslandWorld` (bao gồm Player, Camera, Canvas, MobileInput, world/day-night/weather base).
2. **Phase 3 — Thu thập:** tạo `GatherSettings`, item/tool/resource data; tạo ActionButton + GatherHud.
3. **Phase 4 — Inventory:** thêm PlayerInventory, HUD, rương và nối PlayerGatherer/ResourceSpawner.
4. **Phase 5 — Crafting:** tạo recipe/catalog, thêm CraftingHUD, bàn chế tạo và lò rèn.
5. **Phase 6 — Survival:** tạo SurvivalSettings/consumables, thêm SurvivalController, SurvivalHUD và Sprint.
6. **Phase 8 — 99 ngày:** tạo sáu đảo, quái, sáu boss, vật liệu và bảy công trình; gắn CampaignManager, ArchipelagoManager, BaseBuildingManager, CombatController và CampaignHUD. Phase 7 online không bắt buộc để chơi solo.
7. **Phase 9 — Hang/mobile:** tạo sáu hang, guardian, loot/lore/keys; gắn CaveManager, Journal HUD, Safe Area và cấu hình landscape.

Có thể bấm **CHẠY TẤT CẢ BƯỚC OFFLINE THEO THỨ TỰ**. Hãy chấp nhận các hộp thoại xác nhận ở từng bước. Nếu hủy một thao tác, chạy lại riêng bước đó và kiểm tra validator trước khi tiếp tục. Không chạy khi Editor đang Play Mode.

## 3. Kiểm tra cấu hình

Trong cửa sổ wizard, bấm **Kiểm tra toàn bộ reference và dữ liệu các Phase**, sau đó kiểm tra Console. Chạy **Quét cấu trúc mã nguồn, asmdef, GUID và test count**. Hotfix 0.9.1 kiểm tra toàn bộ `Assets/`, parse Assembly Definition, xác minh tham chiếu `Game.*`, phát hiện vòng phụ thuộc, GUID/meta mồ côi/trùng và Scene thiếu trong Build Settings.

Các kiểm tra từng phase:

- Phase 2: World/Island/Water/Weather references.
- Phase 3: ActionButton, MobileInput, GatherHud.
- Phase 4: PlayerInventory, ResourceSpawner, InventoryHUD/EventSystem.
- Phase 5: CraftingCatalog, CraftingHUD, stations.
- Phase 6: SurvivalSettings, SurvivalController, SurvivalHUD.
- Phase 8: six islands, 99-day campaign, bosses, buildings and Player references.
- Phase 9: six cave definitions, stable IDs, keys/loot, guardian, CaveManager, journal and mobile Safe Area.

## 4. EditMode và Play Mode acceptance

1. `Window → General → Test Runner → EditMode → Run All`.
2. Ghi lại số Passed/Failed thật từ Unity. Không xem số test khai báo là số test đã pass.
3. Mở `SCN_IslandWorld.unity`, nhấn Play.
4. Thử joystick + camera + jump + action đồng thời; mở/đóng Inventory/Crafting/Build/Journal.
5. Thu thập gỗ/đá, câu cá, chế tạo, dùng đồ ăn/nước, kiểm tra hunger/thirst/stamina/temperature.
6. Thử xây nền/tường/cửa/kho/lửa trại/tháp canh; xác nhận thiếu tài nguyên hoặc vị trí cấm không làm mất đồ.
7. Kiểm tra ngày, chết/thắng, 99 ngày, boss và mở khóa đảo.
8. Đi vào 6 hang; thử nhánh bí mật, guardian, chìa khóa, rương, loot từng phần, nhật ký, thoát/vào lại và chơi lại.
9. Trong Scene Game View landscape, thử tỉ lệ màn hình hẹp/rộng, notch, multi-touch và app pause/resume.
10. Chơi đến chết/thắng rồi chọn Chơi lại: xác nhận ngày/boss/quái/hang/loot, công trình và đồ trong rương đều reset; thử thoát game khi rương chứa đồ để xác nhận save không hồi sinh sau khi reset.
11. Không còn Console error trước khi build Android.

## 5. Build Android

1. `File → Build Profiles → Android`; switch platform.
2. Chọn Landscape; kiểm tra package identifier, Minimum API Level và ARM64.
3. Thêm/kiểm tra `SCN_IslandWorld.unity` trong Build Settings (Editor tools sẽ thêm scene khi nâng cấp Phase 8/9).
4. Build Development APK trước. Cài trên máy thật, đọc log bằng Android Logcat.
5. Đo FPS, RAM, nhiệt độ, pin và test touch ở nhiều độ phân giải. Mục tiêu 30 FPS là mục tiêu thiết kế, chưa phải kết quả benchmark.
6. Chỉ tạo release APK/AAB sau khi đã chạy đủ Test Runner, Play Mode và thiết bị thật.

## 6. Phase 7 Online — tùy chọn, không phải authoritative co-op

Bấm `Cài lớp kết nối phòng + voice (Phase 7)` sau khi đã link Unity Cloud Project và bật Authentication, Multiplayer/Relay và Vivox trên đúng Environment. Xem `18_PHASE7_SETUP_AND_TEST.md`.

Phase 7 đồng bộ avatar pose và voice group. Các trạng thái gameplay quan trọng (inventory, gathering, crafting, survival, AI/boss, building, cave loot, campaign save) vẫn local. Không dùng chức năng online như bằng chứng các client chơi cùng một thế giới nhất quán.

## 7. Phục hồi khi có lỗi

- Đọc đầu tiên lỗi compile đầu tiên trong Console; lỗi sau có thể là hậu quả dây chuyền.
- Không xóa `.meta` hoặc tạo lại GUID cho asset đã có reference.
- Nếu package resolve lỗi, giữ nguyên version trong `Packages/manifest.json` và xem dependency conflict trước khi nâng version.
- Trước khi chạy lại Phase 2 thay thế Scene, sao lưu `.unity`/`.meta` của scene.
- Chạy lại các bước dữ liệu/upgrader riêng lẻ; các tool có xu hướng upsert và nối lại component/reference, nhưng asset do bạn tùy chỉnh nên được sao lưu.

## 8. Hotfix 0.9.1 — reset lượt mới và kiểm tra cấu trúc

- `Chơi lại` xóa công trình đã đặt, dữ liệu kho/rương thuộc công trình và save căn cứ để lượt 99 ngày mới bắt đầu sạch.
- Rương đã xóa persistence sẽ không ghi lại dữ liệu cũ khi app pause/quit; cấu hình persistence mới có thể bật lại trạng thái lưu.
- Loot hang có số lượng còn lại bằng 0 không tạo vật phẩm `x0`; phần lore/khóa vẫn có thể tồn tại dưới dạng tương tác bí mật.
- Trình tạo tất cả dừng ngay khi một phase lỗi, Scene sai hoặc thiếu thành phần bắt buộc; Phase 2 yêu cầu PlayerController, MobileInput và IslandGenerator.
- Static Audit quét toàn bộ `Assets/`, parse asmdef, kiểm tra tham chiếu assembly nội bộ/vòng phụ thuộc và Build Settings.
- Đây vẫn là source project: Scene/Prefab/ScriptableObject được tạo trong Unity; kiểm tra cấu trúc không thay thế biên dịch hoặc test runtime.
