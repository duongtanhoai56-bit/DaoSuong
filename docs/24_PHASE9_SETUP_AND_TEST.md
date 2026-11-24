# Phase 9 — Setup và kiểm thử hang ngầm / điện thoại

## Điều kiện

- Unity Editor `6000.3.29f1`.
- Android Build Support (SDK, NDK, OpenJDK) cài qua Unity Hub nếu build Android.
- Package Manager resolve thành công; không còn compile error trước khi chạy Editor tools.
- Tạo và gán URP Pipeline Asset theo `03_PROJECT_SETUP.md`.
- `Active Input Handling = Input System Package (New)`; chọn `Both` nếu cần chạy panel debug IMGUI cũ.

> ZIP là gói source Unity. Nó không chứa scene `.unity`, prefab hoặc ScriptableObject đã sinh sẵn. Các phase trước sử dụng Editor tools để tạo `SCN_IslandWorld` và dữ liệu. Chưa có APK cài thẳng vào điện thoại trong gói này.

## Tạo scene nền theo đúng thứ tự

1. `Đảo Sương → Phase 0 → Create Bootstrap Scene`.
2. `Đảo Sương → Phase 2 → 1 - Create World Assets`.
3. `Đảo Sương → Phase 2 → 2 - Create Playable Island Scene`; mở scene `SCN_IslandWorld`.
4. `Game → Giai đoạn 3 → Tạo dữ liệu mẫu`.
5. `Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD`.
6. `Đảo Sương → Giai đoạn 3 → 2 - Kiểm tra Action + Gather HUD`.
7. `Đảo Sương → Giai đoạn 4 → 1 - Nâng cấp Inventory cho Scene đang mở`, sau đó `2 - Kiểm tra Inventory Scene`.
8. `Đảo Sương → Giai đoạn 5 → 1 - Tạo dữ liệu công thức mẫu`, `2 - Nâng cấp Scene chế tạo`, `3 - Kiểm tra Scene chế tạo`.
9. `Đảo Sương → Giai đoạn 6 → 1 - Tạo dữ liệu sinh tồn`, `2 - Nâng cấp Scene sinh tồn`, `3 - Kiểm tra Scene sinh tồn`.
10. Phase 7 online là tùy chọn nếu cần thử mã phòng và voice; xem `18_PHASE7_SETUP_AND_TEST.md`.
11. Phase 8: `Game → Giai đoạn 8 → 1 - Tạo nội dung quần đảo 99 ngày`, `2 - Nâng cấp Scene hiện tại`, `3 - Kiểm tra Scene`; xem `21_PHASE8_SETUP_AND_TEST.md`.

## Tạo dữ liệu Phase 9

1. Sao lưu scene đang mở trước khi nâng cấp.
2. Chạy `Game → Giai đoạn 9 → 1 - Tạo hang động bí mật và vật phẩm`.
   - Tạo/cập nhật 6 CaveDefinition, 6 guardian, CaveCatalog và 11 ItemData.
   - Nếu asset cùng đường dẫn đã tồn tại, công cụ cập nhật asset đó; kiểm tra các chỉnh sửa thủ công trước khi chạy lại.
3. Chạy `Game → Giai đoạn 9 → 2 - Nâng cấp Scene cho hang + điện thoại`.
   - Gắn CaveManager vào ArchipelagoManager.
   - Wire Player, PlayerInventory, CampaignManager và CaveCatalog.
   - Tạo Safe Area root, đặt lại controls vào root, thêm MobileRuntimeBootstrap và CaveJournalHUD.
   - Đặt Player Settings landscape và thêm scene hiện tại vào Build Settings.
4. Chạy `Game → Giai đoạn 9 → 3 - Kiểm tra Scene + cấu hình mobile`.
5. Lưu Scene và toàn bộ asset mới.

## Play Mode acceptance

### Hang và đường đi

1. Bắt đầu ở Đảo Khởi Đầu; xác nhận các hang sinh sau khi ArchipelagoManager tạo đảo.
2. Đứng trước cửa hang, nhìn về cửa và chạm `TÁC ĐỘNG`; người chơi phải được đưa xuống khu vực ngầm.
3. Đi qua các phòng/đường hầm. Kiểm tra không bị IslandGenerator kéo ngược lên khi Y âm.
4. Tìm vách đá có thể tương tác trong phòng có nhánh phụ; chạm `TÁC ĐỘNG` để đẩy vách đá, đi vào hốc bí mật và mở kho phụ.
5. Tương tác tinh thể ký ức / mảnh truyện; mở `NHẬT KÝ`, kiểm tra ghi chép xuất hiện và có thể cuộn.
6. Đi về portal ở đầu hang, chạm `TÁC ĐỘNG`; người chơi trở lại đúng vị trí cửa hang.
7. Thử vào lại cùng hang qua cửa hang thứ hai trên cùng đảo; dungeon phải nằm đúng dưới cửa đang dùng, nhưng loot/lore đã lấy vẫn giữ nguyên.
8. Thử mở bản đồ đảo khi đang ở trong hang; travel phải bị từ chối.
9. Thử để quái mặt đất tiến gần khi người chơi đang dưới đất; chúng không được gây sát thương xuyên trần.

### Chìa khóa và lưu vật phẩm

1. Vào Hầm Ngầm Thủy Triều; chìa khóa di tích phải xuất hiện (spawn chance 100%).
2. Đến Thánh Đường Vọng Âm trước khi có chìa khóa; kho lưu trữ phải báo cần `Chìa Khóa Di Tích`.
3. Lấy chìa khóa rồi mở kho; một chìa bị tiêu thụ, kho chuyển sang trạng thái đã mở.
4. Nếu túi đầy, dọn chỗ trước; không được mất chìa khóa/vật phẩm khi nhận thưởng thất bại.
5. Với một chồng thưởng lớn hơn sức chứa còn lại, nhặt một phần, thoát Play Mode rồi vào lại; số còn lại phải đúng, không hoàn lại phần đã nhặt.
6. Mở game lại; trạng thái kho, chìa khóa đã dùng và ghi chép đã khám phá phải được giữ.
7. Kết thúc/chọn `Chơi lại`; vào hang lần nữa và xác nhận kho báu, chìa khóa và manh mối được khởi tạo lại cho lượt 99 ngày mới.

### Điện thoại / UI

1. Dùng Game View ở landscape và thử aspect ratio rộng/hẹp.
2. Kiểm tra `MobileSafeAreaRoot`, joystick, LookZone, JumpButton, ActionButton, SprintButton không lọt notch hoặc camera đục lỗ.
3. Giữ joystick và xoay camera đồng thời; thử chạy + nhảy + tác động multi-touch.
4. Mở/đóng Inventory, Crafting, Build, Island và Journal; kiểm tra input gate không bị kẹt.
5. Đưa app ra nền rồi quay lại; xác nhận input vẫn hoạt động và trạng thái game không bị điều khiển giả.
6. Kiểm tra FPS/bộ nhớ, nhiệt máy và thời lượng pin trên điện thoại thật.

## Build Android

1. Cài Android Build Support, SDK, NDK và OpenJDK qua Unity Hub.
2. `File → Build Profiles → Android`; chuyển platform sang Android.
3. Kiểm tra Product Name, Package Name, Minimum API Level và ARM64 theo mục tiêu phân phối.
4. Dùng Development Build ở vòng kiểm thử đầu để thu log; chỉ bỏ Development Build sau khi test.
5. Xác nhận `Default Orientation` là Landscape Left và scene `SCN_IslandWorld` đã nằm trong Build Settings.
6. Build APK/AAB từ Unity Editor, cài lên thiết bị Android, thử touch/màn hình thật và đọc Android Logcat khi có lỗi.

## Automated tests

- `Window → General → Test Runner → EditMode → Run All`.
- Phase 9 thêm `CaveRulesTests` cho số phòng, tỉ lệ loot, dữ liệu không hữu hạn, điều kiện vào hang, chìa khóa, lượng loot nhận, stable ID, số lượng save và không cứu người chơi ra khỏi hang.
- Bản đóng gói khai báo 171 test attribute trong toàn bộ suite. Con số này là số khai báo `[Test]` / `[TestCase]` / tương đương, không phải kết quả đã chạy.
- Phải ghi lại Passed/Failed từ Unity Test Runner sau khi chạy thực tế; không coi static scan là test pass.

## Giới hạn nghiệm thu

Môi trường đóng gói không có Unity Editor, Unity Package Manager runtime, C# compiler hay thiết bị Android. Do đó chưa thể xác nhận C# compile, Play Mode, Test Runner, build APK, FPS/heat/battery hay tương thích các hãng điện thoại. Phase 7 cũng chưa đồng bộ inventory, cave state, AI/guardian, combat hoặc xây dựng giữa các client.
