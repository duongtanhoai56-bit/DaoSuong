# Giai đoạn 5 — Chế tạo

## Mục tiêu

Tạo vòng lặp biến tài nguyên thu thập được thành vật phẩm/công cụ, mở khóa theo cấp bàn chế tạo. Phase 5 là crafting nền tảng, không phải hệ thống sản xuất hoàn chỉnh.

## Kiến trúc

- `RecipeData`: định nghĩa công thức bằng ScriptableObject.
- `CraftingCatalog`: danh mục các công thức cho HUD.
- `RecipeIngredient`: một đầu vào (ItemData + số lượng).
- `CraftingRules`: logic thuần C#, không phụ thuộc MonoBehaviour, Scene, UI hay Time.
- `CraftingStation`: adapter Unity cho trạm cấp 1 (Bàn chế tạo) và cấp 2 (Lò rèn).
- `CraftingHUD`: uGUI runtime, tự tạo nút mở và danh sách cuộn; khi mở sẽ lấy một owner lock từ `GameplayInputGate` trong `Game.Core`.
- `InventoryModel.Clone/RestoreFrom/CanAdd`: hỗ trợ kiểm tra giao dịch chế tạo trước khi tiêu hao nguyên liệu.

`CraftingRules` gộp các nguyên liệu trùng ItemData, kiểm tra cấp trạm, kiểm tra đủ nguyên liệu, mô phỏng tiêu hao đầu vào, sau đó kiểm tra chỗ trống/khối lượng cho đầu ra. Khi chế tạo thật, có snapshot để hoàn nguyên nếu bước cuối không thành công.

## Cấp trạm

- Tier 0: chế tạo tay, không cần trạm.
- Tier 1: Bàn chế tạo.
- Tier 2: Lò rèn; hỗ trợ công thức tier 1 và tier 2.

Nếu có nhiều trạm gần, HUD ưu tiên trạm cấp cao nhất trong phạm vi tương tác, sau đó mới ưu tiên trạm gần nhất cùng cấp.

## Dữ liệu mẫu

Chạy `Game → Giai đoạn 3 → Tạo dữ liệu mẫu` trước. Sau đó chạy `Đảo Sương → Giai đoạn 5 → 1 - Tạo dữ liệu công thức mẫu`.

Tạo 8 công thức:

| Công thức | Nguyên liệu | Trạm | Đầu ra |
|---|---|---|---|
| Dây thừng | Sợi cỏ ×3 | Tay | Dây thừng ×1 |
| Xẻ ván gỗ | Gỗ ×2 | Bàn cấp 1 | Ván gỗ ×1 |
| Rìu đá | Gỗ ×3, Đá ×2, Cành cây ×2 | Bàn cấp 1 | Rìu đá ×1 |
| Cuốc đá | Gỗ ×3, Đá ×3, Cành cây ×2 | Bàn cấp 1 | Cuốc đá ×1 |
| Cần câu | Cành cây ×3, Sợi cỏ ×2 | Bàn cấp 1 | Cần câu ×1 |
| Luyện thỏi sắt | Quặng sắt ×2, Gỗ ×1 | Lò rèn cấp 2 | Thỏi sắt ×1 |
| Rìu sắt | Ván gỗ ×2, Thỏi sắt ×3, Dây thừng ×1 | Lò rèn cấp 2 | Rìu sắt ×1 |
| Cuốc sắt | Ván gỗ ×2, Thỏi sắt ×3, Dây thừng ×1 | Lò rèn cấp 2 | Cuốc sắt ×1 |

Dữ liệu tạo ở `Assets/_Game/Data/Crafting/`.

## Nâng cấp Scene

1. Mở `SCN_IslandWorld`.
2. Chạy `Đảo Sương → Giai đoạn 5 → 2 - Nâng cấp Scene chế tạo`.
3. Xác nhận lưu Scene. Tool thêm `CraftingHUD` vào Canvas, gán PlayerInventory/Catalog/Player, tạo trạm test bàn chế tạo và lò rèn cạnh điểm xuất hiện. Gameplay input được khóa qua `GameplayInputGate`, không cần reference trực tiếp tới `MobileInput`.
4. Chạy `Đảo Sương → Giai đoạn 5 → 3 - Kiểm tra Scene chế tạo`.
5. Play, nhận tài nguyên qua Gathering HUD, bấm `CHẾ TẠO` hoặc phím `C`.

Hai trạm được tạo bằng primitive cube để kiểm thử phạm vi/tier. Đây là placeholder, chưa phải asset bàn chế tạo hoàn thiện. Vị trí được tính khi IslandGenerator tạo xong đảo.

## EditMode tests

`CraftingRulesTests.cs` có 10 test:

- Tiêu hao đầu vào và nhận đầu ra.
- Thiếu nguyên liệu không làm thay đổi inventory.
- Kiểm tra tier trạm và tương thích tier cao hơn.
- Dùng slot được giải phóng bởi nguyên liệu để chứa đầu ra.
- Không cho đầu ra vượt giới hạn khối lượng.
- Gộp nguyên liệu trùng nhau trước khi kiểm tra.
- Công cụ đầu ra chiếm slot riêng và có độ bền mặc định.
- Công thức thiếu dữ liệu bị từ chối.
- Thiếu một trong nhiều loại nguyên liệu không tiêu hao bất kỳ loại nào.

Chạy `Window → General → Test Runner → EditMode → Run All`. Tổng số test trong project hiện là 49 (15 gathering/rules + 22 inventory + 2 input-gate + 10 crafting). Đây là số test đã viết; chỉ được coi là pass sau khi chạy Test Runner trong Unity.

## Giới hạn có chủ đích

- Chế tạo hiện hoàn tất tức thì; chưa có queue/timer sản xuất.
- Lò rèn hiện là trạm tier 2 cho công thức; chưa có nhiên liệu, nhiệt độ, hoạt ảnh luyện kim.
- Chưa có cooking/buff, nâng cấp công thức hoặc kỹ năng nghề.
- Hai trạm là placeholder được tạo bằng Editor tool; hệ thống xây dựng trạm sẽ được nối ở giai đoạn Building.
- Thao tác crafting hiện chạy local; khi làm multiplayer phải chuyển xác thực/tiêu hao/trao output sang server-authoritative.

## Chất lượng và nghiệm thu

Kiểm tra tĩnh: source files, JSON/asmdef, metadata GUID, tham chiếu editor tools, số test và Git. Unity Editor compile, Test Runner, Play Mode, Android/iOS build và profiling thiết bị phải được thực hiện trên máy có Unity; không suy diễn chúng từ kiểm tra tĩnh.
