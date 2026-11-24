# Phase 8 — Audit và giới hạn xác minh

## Phạm vi nâng cấp

- Thêm `CampaignProgressRules`, `CampaignManager`: đếm 99 ngày hoàn tất, điều kiện thắng/thua, boss progression, local save.
- Thêm `ArchipelagoCatalog`, `ArchipelagoManager`: sinh 6 đảo theo seed/settings riêng, chuyển đảo, mở khóa theo ngày và boss, sinh quái/boss.
- Thêm `CreatureDefinition`, `CreatureAgent`: thống nhất dữ liệu sinh vật, AI truy đuổi/đánh cận chiến, HP, phần thưởng, boss phase II.
- Mở rộng `BossDefinition` với spawn day, thông số phase, đòn diện rộng, phần thưởng và giới hạn người chơi.
- Thêm `BuildingDefinition`, `BuildingCatalog`, `BaseBuildingManager`, `PlacedBuilding`, `WarmthSource`: kiểm tra đất/vật cản/chi phí, xây bảy loại công trình, lửa trại tăng nhiệt, lưu căn cứ.
- Mở rộng `StorageContainer`: lưu nội dung rương cục bộ theo stable key.
- Thêm `CombatController`, `CampaignHUD`: đánh quái, trạng thái chiến dịch, danh sách đảo, bảng xây, bảng kết quả.
- Nâng cấp `DayNightCycle`, `IslandGenerator`, `ResourceSpawner`, `SurvivalController`, `PlayerController`, `WeatherSystem` cho event ngày, nhiều đảo, hồi phục ở đảo đang chơi, tọa độ tài nguyên từ xa, nhiệt môi trường và di chuyển.
- Sửa trùng lớp `CreatureDefinition` trong namespace `DaoSuong.Content`: giữ một định nghĩa duy nhất để tránh lỗi biên dịch kiểu trùng lặp.
- Khóa gameplay input khi bảng xây dựng/chọn đảo mở; thả khóa khi đóng/kết thúc.
- Sửa đặt độ cao quái theo chiều cao capsule thực; giai đoạn hai của boss thay thế đòn cận chiến bằng sát thương diện rộng thay vì cộng hai hit.

## Kiểm tra tĩnh đã thực hiện

- Kiểm tra JSON `Packages/manifest.json` và các `.asmdef` parse hợp lệ.
- Quét namespace/type: còn một định nghĩa `DaoSuong.Content.CreatureDefinition` và một `DaoSuong.Content.BossDefinition`.
- Quét GUID `.meta`: không có GUID trùng lặp; mọi file `Assets` cần metadata có `.meta`.
- Quét ngoặc và cân bằng delimiter/preprocessor cho các file C#.
- Kiểm tra assembly graph, không có vòng tham chiếu trong các Assembly Definition hiện có.
- `CampaignProgressRulesTests` thêm kiểm thử cho điều kiện chiến thắng 99 ngày, đếm ngày, boss gate, affordability và placement.
- Kiểm tra archive ZIP cuối cùng bằng `unzip -t` sau khi đóng gói.

## Chưa được xác minh — không được coi là kết quả đã pass

Môi trường tạo package không có Unity Editor, Unity Package Manager runtime hoặc C# compiler. Vì vậy chưa thể xác nhận:

- C# compile thực sự trong Unity 6000.3.29f1.
- Các EditMode/PlayMode tests chạy xanh.
- Scene Editor upgrade chạy hoàn chỉnh với asset đã sinh trên máy Unity.
- 6 đảo, terrain shader/mesh collider, UI và AI hoạt động đúng trong Play Mode.
- Build Android, FPS/nhiệt, cảm ứng trên thiết bị thật.
- Unity Relay/Vivox trên hai thiết bị.

Các bước nghiệm thu cần thực hiện theo `21_PHASE8_SETUP_AND_TEST.md`.

## Giới hạn thiết kế hiện tại

1. Phase 7 chỉ đồng bộ avatar pose và voice; Phase 8 gameplay state vẫn local. Cần server-authoritative Phase 19 để đồng bộ world/players.
2. Creature AI đi thẳng về Player, chưa né địa hình/vật cản bằng NavMesh; chưa có animation/model riêng.
3. Boss hiện tấn công một Player target; không có threat/aggro table nhiều người hoặc telegraph VFX riêng.
4. Không có thuyền/đường di chuyển vật lý giữa đảo; chuyển đảo bằng teleport qua HUD.
5. Công trình là primitive prototype; cửa chưa tương tác, snapping giữa tường/nền/mái và phá hủy có persistence chưa được triển khai.
6. Quái chưa phá công trình; căn cứ chưa có turret/defense automation.
7. Local save không đồng bộ cloud/multiplayer; tiến trình có thể mất khi người dùng xóa data ứng dụng.
8. Không lưu toàn bộ quái thường, pickup, thời tiết và trạng thái node thu thập khi thoát app.

## Khuyến nghị trước khi công bố build

- Chạy compile + toàn bộ Test Runner trong Unity Editor và lưu kết quả log.
- Tạo mới scene theo Phase 0–7, tạo dữ liệu Phase 8 và chạy Validate.
- Chơi thực tế tối thiểu 2–3 chu kỳ ngày/đêm trên Android; điều chỉnh cân bằng sát thương boss, lượng tài nguyên và tốc độ ngày.
- Hoàn tất Phase 19 authoritative gameplay trước khi quảng cáo tính năng co-op sinh tồn đồng bộ.
