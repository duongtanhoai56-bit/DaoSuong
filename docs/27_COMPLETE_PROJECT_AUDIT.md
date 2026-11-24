# Báo cáo hợp nhất — Đảo Sương Complete Game (Phase 0–9)

## Nguồn hợp nhất

- Dùng `DaoSuong_Phase9_Caves_Mobile_UnityProject.zip` làm nguồn chính vì README bên trong xác nhận đây là project tích lũy Phase 0–9.
- Đối chiếu với archive Phase 7: 384 đường dẫn file chung; 21 file đã được chỉnh sửa trong Phase 9 (các sửa lỗi trước đó), 56 file bổ sung, hai file CreatureDefinition cũ bị loại bỏ do trùng lớp/tên và được thay bằng định nghĩa ở namespace/gameplay folder mới. Không overlay archive cũ lên Phase 9 để tránh phục hồi mã lỗi.
- Phase 0 archive chỉ là bootstrap nền cũ; không được dùng để ghi đè nội dung Phase 9.

## Nội dung

- Phase 0–1: kiến trúc, core, player controls/mobile input.
- Phase 2: procedural world, world assets và Editor scene generator.
- Phase 3–6: gather, inventory, crafting, survival.
- Phase 7: UGS/Relay + NGO avatar + Vivox voice slice.
- Phase 8: campaign 99 ngày, 6 đảo, quái/boss, combat/building.
- Phase 9: hang nhiều phòng, hidden branches, loot/lore, guardians, save/journal, Safe Area/mobile.
- EditMode test suite, docs từng phase và Editor tools.

## Hợp nhất bổ sung

1. Thêm `DaoSuongFullGameSetupWizard.cs`: một cửa sổ gọi đúng các Editor tool hiện có theo thứ tự. Có thao tác riêng lẻ, chạy tất cả bước offline, validate từng phase, static file/GUID scan và cài Phase 7 online tùy chọn.
2. Cập nhật Phase 0 validator: kiến trúc hiện có 4 runtime asmdef (Core/Gameplay/UI/Network) và 1 EditMode test asmdef; loại bỏ thông báo cũ cho rằng chỉ được có đúng 3 asmdef.
3. Cập nhật README và thêm tài liệu setup/audit thống nhất.
4. Archive chính sử dụng tên project `DaoSuong_Complete_Game`.
5. Static source scan after integration: 120 C# files, 5 asmdefs, 171 test attributes, 234 unique GUIDs, no missing `.meta`, duplicate GUID, JSON/asmdef parsing or delimiter/preprocessor issues.

## Kiểm tra đã làm tại môi trường đóng gói

- Archive nguồn Phase 9 mở được và CRC đọc được; 506 ZIP entries / 970,096 bytes uncompressed trước khi thêm wizard.
- Source tree before integration wizard: 440 files, 119 `.cs`; after adding the wizard: 120 `.cs`; không có `.unity`, `.prefab` hoặc generated ScriptableObject `.asset` trong ZIP nguồn.
- Project yêu cầu package versions tại `Packages/manifest.json`; ProjectVersion là `6000.3.29f1`.
- Runtime assembly graph: `Game.Core` → `Game.Gameplay` → UI; Game.Network tham chiếu Core/Gameplay và package assemblies. EditMode test assembly tham chiếu Core/Gameplay/Network.
- Cần kiểm tra lại số file/GUID/test sau khi tạo scene assets trong Unity; wizard cung cấp Static Audit cho bước đó.

## Giới hạn quan trọng

- Không có Unity Editor, Unity Package Manager runtime, C# compiler, Android SDK hoặc Android device trong môi trường đóng gói.
- Do đó chưa thể thực thi Unity compilation, NGO ILPP/RPC codegen, EditMode Test Runner, Play Mode, Android build/install, Relay/Vivox multi-device, FPS/RAM/thermal/battery benchmark.
- ZIP không chứa Scene `.unity`, prefab, Material/ScriptableObject data assets đã sinh, hoặc APK. Các file này được tạo trong Unity bằng Wizard/Editor tools.
- Phase 7 chưa phải co-op gameplay authoritative. Gameplay state vẫn local: inventory, gather, crafting, survival, AI/boss, building, cave/loot và campaign progress.

## Tiêu chí release

Không đánh dấu PASS trước khi: (1) Package Manager resolve, (2) không có C# compile errors, (3) toàn bộ EditMode tests pass, (4) Play Mode acceptance các hệ thống 99 ngày/hang/mobile pass, (5) Android ARM64 Development build cài và chạy trên thiết bị thật, (6) đo được FPS/RAM/thermal/battery và kiểm thử multi-touch. Phase 7 chỉ công bố co-op đầy đủ sau khi authoritative state replication được triển khai và thử trên nhiều thiết bị.


## Hotfix 0.9.1 — rà soát và sửa lỗi sau hợp nhất

### Lỗi đã sửa

1. **Chơi lại không xóa căn cứ:** `CampaignHUD.RestartCampaign()` reset ngày/boss/quái nhưng `BaseBuildingManager` vẫn giữ công trình và rương. Thêm `BaseBuildingManager.ResetForNewRun()` để xóa các công trình đang đặt, gọi `StorageContainer.ClearPersistence()` cho rương thuộc công trình, xóa save căn cứ và lưu PlayerPrefs.
2. **Dữ liệu rương có thể sống lại sau reset:** thêm cờ `persistenceCleared`; sau khi xóa, `EnsureLoaded` không nạp lại và `SavePersistence` không ghi ngược dữ liệu cũ khi pause/quit. `ConfigurePersistence` có thể mở lại persistence cho một lượt mới.
3. **Loot 0 vật phẩm:** các dòng loot có amount còn lại bằng 0 không còn sinh nút nhặt `x0`; lore/khóa vẫn được hiển thị thành tương tác bí mật không có item reward.
4. **Wizard tiếp tục sau lỗi phase:** `Run` trả về thành công/thất bại; `RunAllOffline` dừng ngay khi thao tác thất bại, Scene không phải `SCN_IslandWorld` hoặc thiếu `PlayerController`, `MobileInput`, `IslandGenerator` / component cốt lõi của phase.
5. **Audit quá hẹp:** static scan chuyển từ `Assets/_Game` sang toàn bộ `Assets`, kiểm tra meta mồ côi, tên assembly trùng, tham chiếu `Game.*` không tồn tại, vòng phụ thuộc và Scene không tồn tại trong Build Settings.
6. Cập nhật `GameBuildInfo.Version` lên `0.9.1-hotfix`.

### Regression tests mới

- `StorageContainerPersistenceTests`: xóa save rương, ngăn pause/quit ghi lại dữ liệu cũ, và cho phép cấu hình persistence mới sau reset.
- `BaseBuildingManagerResetTests`: reset lượt xóa công trình và save của rương.
- `CaveRulesTests`: thêm các trường hợp tạo loot có/không có item, lượng 0 và lore.

### Kết quả kiểm tra tĩnh trên gói hiện tại

- 122 tệp C#; 5 Assembly Definition; 180 thuộc tính test (`Test`, `TestCase`, `UnityTest`) được khai báo. Đây là số thuộc tính khai báo, **không phải** số test đã chạy/pass.
- Trong `Assets/`: 239 file `.meta`, 239 GUID duy nhất; 0 GUID trùng, 0 meta mồ côi, 0 asset/thư mục thiếu `.meta`, 0 GUID sai định dạng.
- 6 tệp JSON/asmdef parse hợp lệ; 0 tham chiếu `Game.*` bị thiếu; 0 vòng phụ thuộc assembly.
- Quét lexical dấu ngoặc và chỉ thị preprocessor C#: 0 vấn đề phát hiện.
- Editor Build Settings hiện có 0 Scene bật vì ZIP nguồn chưa có Scene được Unity sinh tự động. Wizard Phase 2–9 phải tạo Scene trước khi build.
- ZIP trước phát hành được kiểm tra CRC sau khi đóng gói.

### Chưa được xác minh runtime

Môi trường đóng gói không có Unity Editor, Unity Package Manager runtime, C# compiler, Android SDK/adb hoặc điện thoại thật. Vì vậy chưa có bằng chứng cho C# compile, NGO ILPP, 180 thuộc tính test được khai báo (chưa thực thi), Play Mode, Android build/install, multi-touch, FPS/RAM/nhiệt/pin hoặc Relay/Vivox. Không được gọi đây là bản đã kiểm thử runtime hoặc đã sẵn sàng phát hành.
