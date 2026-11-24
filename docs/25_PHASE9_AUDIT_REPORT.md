# Phase 9 — Audit và remediation report

## Hạng mục triển khai

- Thêm CaveCatalog, CaveDefinition, CaveManager, CaveEntrance, CaveExitPortal và CaveTreasure.
- Sinh sáu hang ngầm gắn với sáu island ID; mỗi hang có 3–5 phòng nối bằng đường hầm, nhánh phụ bí mật, kho phụ, ghi chép và guardian.
- Thêm 11 vật phẩm và 6 guardian definitions qua Editor content generator.
- Chìa khóa di tích có tỷ lệ 100% ở hang đảo đầu; kho lưu trữ đảo cổ đại yêu cầu chìa khóa đó.
- Thêm CaveJournalHUD cuộn, có thể xem lại ghi chép đã khám phá và dùng safe area.
- Sinh hang theo nhu cầu, chỉ bật hang đang dùng; giảm chi phí renderer/collider/light khi người chơi chưa ở hang.
- Thêm MobileRuntimeBootstrap, SafeAreaFitter, landscape orientation, multi-touch và giảm chất lượng ở máy RAM thấp.
- Tích hợp CaveManager với ArchipelagoManager, IslandGenerator, PlayerGatherer, CreatureAgent và SurvivalController.

## Lỗi được rà soát và sửa trong Phase 9

1. CaveDefinition tham chiếu CreatureDefinition ở namespace DaoSuong.Content nhưng thiếu `using`; đã thêm reference namespace.
2. Khóa cứu rơi của IslandGenerator có thể đưa người chơi khỏi hang do dungeon nằm ở Y âm; thêm CaveRules gate.
3. Sinh vật mặt đất có thể xử lý attack khi người chơi ở dưới hang; thêm kiểm tra actor có thuộc root hang đang hoạt động không.
4. Có thể travel sang đảo trong khi ở dưới đất; từ chối travel cho đến khi thoát hang.
5. Có thể câu cá khi đang ở dưới hang; PlayerGatherer không bắt đầu fishing khi CaveManager báo underground.
6. Vật phẩm có chìa khóa có thể cộng thưởng rồi thất bại khi trừ chìa; dùng InventoryModel snapshot/rollback.
7. Nhánh nhận vật phẩm có thể chạy nhánh tiêu thụ chìa khóa lần hai; chuyển cờ unlock sau giao dịch để chỉ tiêu thụ một lần.
8. Nhặt một phần chồng rồi thoát có thể tạo lại phần đã nhặt; save v4 lưu lượng còn lại, mở khóa, đã thu thập và ghi chép.
9. Dữ liệu số lượng save có thể âm/quá lớn; clamp về 0–999.
10. Exit/unload scene khi đang dưới hang có thể để nhân vật persistent ở tọa độ âm; exit và OnDestroy có đường khôi phục an toàn.
11. Collider tinh thể trang trí có thể tồn tại tới cuối frame và chặn người chơi; disable collider trước khi Destroy.
12. Guardian bắt đầu thấp hơn mặt sàn do scale 1.6; đặt vị trí local Y theo nửa chiều cao.
13. HUD được chuyển vào Safe Area nhưng validator Phase 3/4/6 vẫn tìm control ở Canvas root; cập nhật tìm kiếm theo `MobileSafeAreaRoot` và fallback cũ.
14. Validator Phase 9 kiểm tra đủ 6 ID hang/đảo, stable ID, story, room bounds, guardian stats, loot entries, chìa khóa bắt buộc, references, HUD, controls và landscape.
15. Validator content bổ sung kiểm tra trùng ItemData ID và CreatureDefinition stable ID trên toàn project sau khi Editor assets được sinh.
16. Khi thêm loot vào túi, chỉ số slot chìa khóa có thể thay đổi; tìm lại slot bằng ItemData sau khi Add trước khi trừ chìa, nếu thất bại thì restore snapshot.
17. Nút nhật ký ban đầu nằm sát vùng thao tác góc dưới; chuyển lên cụm HUD góc trên bên phải, không chồng joystick/nút tấn công/xây dựng.
18. Nút `Chơi lại` ban đầu không xóa tiến trình loot hang; thêm `CaveManager.ResetForNewRun` để hủy dungeon đã sinh, xóa save loot/manh mối cũ và tạo lại dungeon theo yêu cầu. Tránh gọi `Destroy` hai lần trên guardian thuộc dungeon.
19. Nhật ký có thể mở chồng một modal khác; thêm kiểm tra `GameplayInputGate.IsLockedByOther` và thông báo đóng bảng hiện tại trước khi mở nhật ký.
20. Mỗi đảo sinh nhiều cửa hang nhưng runtime chỉ lưu một điểm neo đầu tiên; khi dùng cửa khác có thể xuất hiện lệch vị trí. Cập nhật dungeon dùng chung được đặt lại tại entrance thực tế mỗi lần vào, đồng thời giữ nguyên trạng thái loot/lore trong cùng lượt.
21. Nhánh phụ trước đó có thể bị nhìn thấy ngay từ phòng chính; bổ sung vách đá vật lý dùng `CaveTreasure` làm tương tác một lần, lưu trạng thái mở và ghi manh mối vào nhật ký.
22. Scene cũ có thể đã chứa `MobileSafeAreaRoot` nhưng thiếu `SafeAreaFitter`; `GetOrCreate` trước đó trả root hiện hữu mà không sửa thiếu sót. Bổ sung tự gắn `SafeAreaFitter` khi gặp root cũ để Safe Area hoạt động nhất quán sau khi nâng cấp lặp lại.

## Static checks thực hiện tại môi trường đóng gói

Các kết quả dưới đây là kiểm tra cấu trúc/mã nguồn, không phải Unity compile hoặc runtime acceptance test.

- C# files: 119 tổng cộng (109 script runtime/Editor + 10 test source).
- Assembly definitions: 5; `Packages/manifest.json` và toàn bộ `.asmdef` parse hợp lệ; tham chiếu tới project assembly đều hợp lệ và không có vòng phụ thuộc.
- `.meta`: 233 tệp, 233 GUID duy nhất; 0 tệp/thư mục bên dưới `Assets/` thiếu `.meta`; 0 GUID trùng.
- Kiểm tra lexical trên 119 C# files: ngoặc `{}`, `[]`, `()` cân bằng; không có literal/comment dang dở; khối `#if/#endif` cân bằng.
- Kiểm tra kiểu trùng: không có khai báo top-level trùng; hai helper `SaveData` cùng tên nằm lồng trong hai lớp quản lý khác nhau nên không xung đột.
- Phase 9 content generator: 6 island/cave ID, 6 guardian ID và 11 ItemData ID; tất cả ID trong từng nhóm đều duy nhất.
- EditMode suite: 171 test attribute được khai báo. Không có test nào được đánh dấu Passed vì không chạy Unity Test Runner.
- Scene/prefab/ScriptableObject asset đã sinh sẵn trong source ZIP: 0/0/0; Editor tools từ các phase tạo chúng sau khi import project.
- ZIP cuối: `unzip -t` báo `No errors detected in compressed data`; CRC kiểm tra toàn bộ mục trong archive thành công.

## Chưa thể xác nhận

- Unity Editor compilation, UGUI/Input System event wiring runtime và NGO ILPP.
- Scene upgrade tools chạy trong Editor và không xung đột với scene do người dùng sửa thủ công.
- Unity Test Runner pass/fail thật.
- Android APK/AAB, ARM64, runtime permissions, multi-touch, notch/safe-area.
- FPS, memory, nhiệt và pin trên thiết bị thật.
- Multiplayer server-authoritative cho inventory, loot, guardian, survival, combat hoặc xây dựng.

Lý do: runtime hiện tại không có Unity Editor, Unity Package Manager, C# compiler hay Android SDK/device. Dự án còn cần chạy tuần tự Editor tools từ Phase 0 đến Phase 9 để tạo scene và ScriptableObject data; ZIP source không phải APK cài trực tiếp.
