# GDD ngắn — Đảo Sương

- **Tên làm việc:** Đảo Sương (chưa chốt thương hiệu/tên phát hành).
- **Codename:** `DAO_SUONG`.
- **Thể loại:** Sinh tồn, chế tạo, khám phá, xây dựng căn cứ và co-op PvE 3D.
- **Nền tảng:** Android/iOS; điều khiển cảm ứng là mặc định.
- **Engine:** Unity 6.3 LTS, C#, URP.
- **Quy mô co-op mục tiêu:** 2–4 người ở bản đầu; kiến trúc và kiểm thử mở rộng tối đa 8 sau.
- **Định hướng hình ảnh:** low-poly cách điệu, silhouette rõ, chất liệu tái sử dụng, nội dung gốc.

## High concept

Một nhóm người sống sót dạt vào quần đảo bị bao phủ bởi màn sương biến đổi theo thủy triều. Những cột đá cổ phát tín hiệu khi bão đến; sinh vật canh giữ xuất hiện quanh các mạch năng lượng dưới đảo. Người chơi phải phối hợp dựng nơi trú ẩn, khai thác tài nguyên và tìm ra nguyên nhân khiến quần đảo mất cân bằng.

**Lời hứa trải nghiệm:** Mỗi lần vào đảo cùng bạn bè đều hoàn thành được một mục tiêu nhỏ và mang về tiến triển nhìn thấy được.

## Trụ cột thiết kế

1. **Co-op là trung tâm:** chia vai trò tự nhiên; ping và voice giúp nhóm phối hợp; solo vẫn có thể tiến triển.
2. **Phiên ngắn có ý nghĩa:** 10–20 phút hoàn thành 1–2 mục tiêu cụ thể.
3. **Sinh tồn công bằng:** nguy hiểm có dấu hiệu cảnh báo và cách chuẩn bị; ba độ khó Dễ/Thường/Khó.
4. **Không pay-to-win:** không bán sát thương, chỉ số, tài nguyên hiếm hoặc lợi thế chiến đấu.
5. **Mobile-first:** 30 FPS mục tiêu trên thiết bị chuẩn; tùy chọn đồ họa; giới hạn nhiệt, pin, RAM và tải mạng.

## Vòng lặp chính

`Khám phá → Thu thập → Chế tạo → Xây/nâng cấp căn cứ → Đối đầu quái/boss → Mở vùng mới`.

- **Vòng ngắn (2–5 phút):** chọn điểm đến, thu thập/đánh trả, quay về.
- **Vòng phiên (10–20 phút):** vào phòng cùng bạn, chọn 1–2 nhiệm vụ như dựng tường, săn quái tinh anh hoặc lấy nguyên liệu, lưu trạng thái rồi thoát.
- **Vòng dài:** nâng cấp bàn chế tạo, mở khu vực, tìm manh mối, đánh boss cuối và khám phá bí mật đảo; sau khi kết thúc có thể chơi tự do.

## Bối cảnh và nội dung gốc

- Một khu vực đầu tiên: **Vịnh Mù** (tên vùng tạm), có bãi biển, rừng thưa và lối vào di tích.
- Nguyên liệu cơ bản: gỗ trôi, đá bazan, sợi cây; tài nguyên có điểm sinh và thời gian hồi.
- Kẻ địch thường: 1–2 biến thể đơn giản; boss đầu tiên có telegraph rõ, 2 đòn chính và một giai đoạn tăng áp lực.
- Không sao chép tên, nhân vật, quái, UI, bản đồ, âm nhạc hoặc hình ảnh từ sản phẩm khác.

## Sinh tồn và độ khó

Bản chơi thử dùng **Máu, Đói, Khát, Thể lực**. Nhiệt độ, bệnh, chảy máu và hiệu ứng thời tiết mở rộng sau.

- **Dễ:** tiêu hao chậm, tài nguyên dồi dào, mất ít đồ khi bị hạ.
- **Thường:** mức cân bằng mặc định.
- **Khó:** tiêu hao và áp lực quái cao hơn, nhưng không có cơ chế bất ngờ không thể đối phó.

Khi bị hạ, người chơi rơi một phần hành trang theo luật rõ ràng. Đồng đội có thể cứu trong cửa sổ thời gian; nếu không được cứu, hồi sinh tại căn cứ/điểm hồi sinh. Không rơi đồ quan trọng cốt truyện hoặc vật phẩm trả phí.

## Co-op, xã hội và lưu dữ liệu

- Vertical slice giới hạn **4 người/phòng**; 8 người là mốc mở rộng sau stress test.
- Host-authoritative cho prototype: host/server xác minh nhặt đồ, chế tạo, sát thương, xây/phá và loot.
- Friends phiên bản đầu: tìm/thêm bạn bằng mã người chơi, gửi lời mời vào phòng; chưa làm party xuyên game phức tạp.
- Voice phiên bản đầu: kênh nhóm, push-to-talk mặc định, mute từng người và mute all; positional voice, lọc nâng cao và moderation tooling mở rộng sau.
- Lưu tối thiểu: snapshot thế giới và hồ sơ người chơi có schema version. Cloud Save là mục Should; bản đầu có thể dùng local host snapshot để kiểm thử, không coi local host save là giải pháp phát hành.

## Monetization và quyền riêng tư

Prototype không có monetization. Định hướng phát hành chỉ cosmetic, battle pass không tăng sức mạnh và quảng cáo tùy chọn nếu không làm gián đoạn co-op. Voice phải xin quyền micro, thông báo minh bạch, có nút tắt/mute/report; tài khoản trẻ em mặc định không bật voice cho tới khi có chính sách tuổi/consent được xác minh.

## Mục tiêu hiệu năng (giả thuyết cần profiling)

- 30 FPS ổn định ở preset phù hợp với máy mục tiêu.
- Mốc ngân sách khởi đầu: ~100k tam giác nhìn thấy; RAM mục tiêu dưới 1.5 GB; build đầu dưới 300 MB; tải thêm nội dung bằng Addressables khi cần.
- Network simulation 20 Hz trước; tăng lên 30 Hz chỉ khi gameplay cần và ngân sách băng thông cho phép.
- Ping mục tiêu 150 ms phải được kiểm thử với lag simulation; voice bitrate/packet loss phải đo riêng, không cộng mơ hồ vào gameplay tick.

## Điều kiện kết thúc game

Hạ boss cuối và khôi phục mạch cân bằng của quần đảo sẽ mở bí mật chính. Sau credits, người chơi được quay lại thế giới ở chế độ tự do để xây dựng, khám phá và săn boss.
