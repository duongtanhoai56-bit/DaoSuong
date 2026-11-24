# Phase 8 — Quần đảo sinh tồn 99 ngày

## Mục tiêu

Mở rộng bản sinh tồn Đảo Sương thành một chiến dịch PvE có tiến trình rõ: khám phá nhiều đảo, thu thập vật liệu, chiến đấu với sinh vật và boss, dựng căn cứ, sống sót qua 99 ngày. Hệ thống dùng dữ liệu `ScriptableObject` để thay đổi cấu hình mà không sửa thuật toán runtime.

## Vòng chơi

`Sinh tồn tại đảo khởi đầu → Thu thập/chế tạo → Dựng căn cứ → Hạ boss canh giữ → Mở đảo mới → Nâng cấp trang bị/căn cứ → Sống tới hết ngày 99`.

- Mỗi ngày được tính khi đồng hồ thế giới hoàn tất một vòng ngày và người chơi vẫn còn sống.
- Đếm đủ 99 ngày: chiến thắng và đóng băng thời gian.
- Người chơi chết trước ngày 99: thất bại; lượt chơi kết thúc.
- Tiến độ ngày, trạng thái thắng/thua và danh sách boss đã hạ được lưu cục bộ bằng `PlayerPrefs`.
- Bản chơi mới có thể khởi động từ bảng kết quả. Căn cứ đã xây và nội dung rương được lưu riêng, không tự xóa khi bắt đầu lượt mới.

## Sáu đảo

| ID | Đảo | Điều kiện mở | Boss canh giữ | Đặc trưng |
|---|---|---|---|---|
| `home` | Đảo Khởi Đầu | Mở sẵn | Khổng lồ thủy triều, ngày 2 | Rừng/bãi cát, tài nguyên cơ bản |
| `forest` | Đảo Rừng Sâu | Từ ngày 2 và hạ Khổng lồ thủy triều | Hộ vệ muối, ngày 4 | Rừng dày, lợn rừng, dơi hang |
| `swamp` | Đảo Đầm Lầy | Từ ngày 4 và hạ Hộ vệ muối | Nữ hoàng đầm lầy, ngày 9 | Đầm lầy, bò sát, nọc độc |
| `volcano` | Đảo Núi Lửa | Từ ngày 9 và hạ Nữ hoàng đầm lầy | Titan tro tàn, ngày 16 | Địa hình cao, chó tro, tinh thể tro |
| `frost` | Đảo Băng Tuyết | Từ ngày 16 và hạ Titan tro tàn | Long băng, ngày 23 | Nhiệt độ thấp, sói băng, mảnh băng |
| `ancient` | Đảo Cổ Đại | Từ ngày 23 và hạ Long băng | Thủ hộ lõi cổ đại, ngày 36 | Kẻ canh giữ cổ đại, lõi cổ đại |

Các đảo được sinh từ seed cố định và `IslandSettings` riêng. Tọa độ đảo được tách biệt để người chơi không nhìn thấy nhiều đảo chồng lên nhau. Hệ thống tái sử dụng cùng thuật toán địa hình, nhưng palette, độ cao, đầm lầy, tuyết, số lượng cây/đá và seed khác nhau.

## Sinh vật và boss

Sinh vật thường có máu, tốc độ, sát thương, tầm phát hiện, tầm đánh, thời gian hồi đòn và vật phẩm rơi. AI Phase 8 dùng truy đuổi trực tiếp, phù hợp bản prototype mobile; chưa có NavMesh/pathfinding tránh vật cản hoặc hoạt ảnh riêng.

Sáu boss có thông số tăng dần, phần thưởng vật liệu hiếm và giai đoạn hai khi máu còn 50% trở xuống. Giai đoạn hai tăng tốc độ, nhịp tấn công và mở rộng tầm đánh bằng đòn diện rộng. Đòn diện rộng thay thế đòn cận chiến trong nhịp đó, không cộng dồn hai gói sát thương để tránh sát thương tức thời quá lớn.

Boss có thể xuất hiện khi đến ngày quy định. Đảo mới không mở chỉ vì đủ ngày: điều kiện ngày và boss bắt buộc đều phải thỏa mãn.

## Xây dựng căn cứ

Các mẫu công trình được tạo bằng menu Phase 8:

- Nền gỗ, tường gỗ, cửa căn cứ, mái che.
- Thùng kho có thể mở qua nút tương tác rương hiện có của Inventory HUD.
- Lửa trại tạo vùng tăng nhiệt độ cơ thể khi người chơi đứng gần.
- Tháp canh.

Vị trí xây được căn theo lưới, đặt ở vùng đất của đảo, kiểm tra vật cản và khoảng cách, đồng thời kiểm tra đủ nguyên liệu trước khi trừ đồ. Nếu bước tạo công trình thất bại, kho đồ được khôi phục từ snapshot. Công trình và nội dung rương được lưu cục bộ.

## Điều khiển / HUD

- `TẤN CÔNG`: tấn công sinh vật/boss trong tầm và phía trước; rìu/cuốc đang có trong túi được dùng làm chỉ số cộng sát thương.
- `XÂY DỰNG`: mở danh sách công trình và chi phí.
- `CÁC ĐẢO`: mở bảng di chuyển; đảo khóa/đảo hiện tại được thể hiện riêng.
- HUD hiển thị ngày sống sót, boss đã hạ, đảo hiện tại và máu boss gần nhất.
- Khi bảng xây dựng hoặc chọn đảo mở, `GameplayInputGate` khóa di chuyển/nhìn/đánh để tránh thao tác UI đồng thời điều khiển nhân vật.

## Lưu dữ liệu

| Dữ liệu | Cơ chế |
|---|---|
| Ngày sống sót, thắng/thua, boss đã hạ | `DAO_SUONG_CAMPAIGN_V1` |
| Công trình đã đặt | `DAO_SUONG_BASE_V1` |
| Vật phẩm trong từng rương | Khóa `DAO_SUONG_CHEST_V1_...` theo ID công trình và tọa độ |

Đây là lưu cục bộ trên thiết bị; xóa dữ liệu ứng dụng/PlayerPrefs có thể xóa tiến trình. Đồ rơi và trạng thái quái thường chưa được lưu xuyên phiên.

## Giới hạn cần biết

Phase 8 là gameplay prototype local chạy trên client. Phase 7 mới cung cấp tạo/tham gia phòng, avatar pose và voice channel; nó **chưa đồng bộ** máu, túi đồ, thu thập, chế tạo, thời tiết, công trình, AI, boss hoặc tiến trình 99 ngày. Các client hiện không có một world state authority chung. Không phát hành như game co-op đồng bộ hoàn chỉnh cho tới khi hoàn tất Phase 19 server-authoritative gameplay.

Ngoài ra, bản này chưa có thuyền/đường đi vật lý giữa đảo, không có AI phá căn cứ, cửa chưa có animation/tương tác đóng mở, nền/tường/mái chưa có snap-to-neighbor, và mô hình quái/boss hiện là primitive prototype. Đây là điểm mở rộng kế tiếp chứ không được xem là đã hoàn thiện.
