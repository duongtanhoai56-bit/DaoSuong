# Đảo Sương — Complete Game Hotfix 0.9.1

## Tóm tắt

Đây là bản rà soát sau khi hợp nhất Phase 0–9. Dùng Phase 0–9 Complete Game làm nguồn chính; không overlay ZIP các phase cũ. Bản hotfix tập trung vào reset một lượt 99 ngày mới, persistence rương, loot hang và an toàn của trình tạo Scene tổng hợp.

## Lỗi và thay đổi

- Reset chiến dịch trước đây không xóa công trình và rương trong căn cứ; thêm reset cho `BaseBuildingManager`, xóa các `StorageContainer` của công trình và dữ liệu PlayerPrefs liên quan. Khi object xây dựng không tải được do thiếu catalog, reset vẫn đọc build-save để xóa khóa rương cũ theo ID/tọa độ.
- Sau khi xóa persistence, rương không được phép nạp/ghi lại dữ liệu cũ từ callback pause/quit; thêm `persistenceCleared` và test hồi quy.
- Loot đã hết số lượng không sinh pickup `x0`; chỉ giữ tương tác nếu còn lore/secret.
- Full Setup Wizard nay dừng đúng khi phase thất bại, Scene bị hủy/sai hoặc thiếu component bắt buộc; Phase 2 kiểm tra PlayerController, MobileInput và IslandGenerator.
- Static Audit quét toàn bộ Assets và kiểm tra metadata, tên/refs asmdef nội bộ, dependency cycles và đường dẫn Scene đang bật trong Build Settings.
- Phiên bản mã nguồn: `0.9.1-hotfix`.

## Static QA thực hiện

| Kiểm tra | Kết quả |
|---|---:|
| C# source files | 122 |
| Assembly Definition | 5 |
| Test attributes khai báo | 180 |
| Asset `.meta` trong Assets | 239 |
| GUID duy nhất | 239 |
| Thiếu / mồ côi / lỗi định dạng `.meta` | 0 / 0 / 0 |
| GUID trùng | 0 |
| JSON + asmdef parse được | 6 / 6 |
| Tham chiếu assembly `Game.*` bị thiếu | 0 |
| Chu trình assembly | 0 |
| Lỗi cân bằng delimiter/preprocessor lexical scan | 0 |
| Scene enabled trong Build Settings | 0 (Scene được sinh bằng Unity Editor) |

Số 180 là số thuộc tính test tìm được trong source, không chứng minh Test Runner đã chạy.

## Giới hạn và các bước nghiệm thu còn bắt buộc

Môi trường này không cung cấp Unity Editor, compiler C#, Unity Package Manager runtime hoặc Android SDK/thiết bị. Chưa thể xác nhận compile, ILPP, EditMode Test Runner, Play Mode, build/install Android, multi-touch, hiệu năng, nhiệt/pin, Relay hoặc Vivox.

Sau khi mở project bằng Unity `6000.3.29f1`: resolve Package Manager; tạo URP Asset; chạy `Đảo Sương → 00 - Trình tạo game tổng hợp (Phase 0–9)`; chạy Static Audit; chạy EditMode Test Runner; vào Play Mode thử một lượt mới/99 ngày, căn cứ/rương, boss, hang và touch; cuối cùng build ARM64 Development APK và kiểm tra trên thiết bị thật.
