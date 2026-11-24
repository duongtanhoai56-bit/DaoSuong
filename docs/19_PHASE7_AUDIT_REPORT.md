# Báo cáo audit Phase 7 — Online co-op và Vivox

## Lỗi/phần thiếu được phát hiện và chỉnh

1. **Thiếu Game.Gameplay reference trong Game.Network.asmdef**: `NetworkPlayerAvatar` dùng `IslandGenerator`; thêm tham chiếu một chiều `Game.Network → Game.Gameplay → Game.Core`, không tạo vòng assembly.
2. **Biến `bound` chưa khai báo trong OnlineSessionHUD**: thay bằng so sánh reference thật giữa service registry và các service đã subscribe.
3. **PTT đặt trong panel ONLINE có thể bị ẩn khi panel đóng**: đưa PTT thành HUD button riêng; chỉ hiển thị khi đã tham gia voice channel và panel cấu hình đang đóng.
4. **Nút ONLINE có thể bị panel vẽ phủ**: đưa nút toggle lên sibling cuối để vẫn có thể đóng panel.
5. **Online panel không khóa điều khiển gameplay**: tích hợp `GameplayInputGate`; khi đóng/tắt panel luôn nhả lock và release PTT.
6. **IsConnected bị dùng nhầm như trạng thái session**: thêm `HasSession`, tách session tồn tại khỏi trạng thái transport đang lắng nghe; khi mất Relay người chơi vẫn có thể Rời phòng và không tạo/join session trùng.
7. **NetworkManager có thể bị hủy khi scene unload**: trước khi khởi động session, đưa NetworkManager về root và `DontDestroyOnLoad`.
8. **Runtime network preflight thiếu kiểm tra**: kiểm tra UnityTransport và NetworkPlayerAvatar prefab có đủ NetworkObject/NetworkTransform/NetworkPlayerAvatar.
9. **Vivox PTT có thể race giữa press/release/mute**: serialize trạng thái transmit bằng SemaphoreSlim; state desired được cập nhật trước khi chờ SDK, và được đánh giá lại sau await.
10. **Vivox auto transmission swap**: đặt `DisableAutomaticChannelTransmissionSwap = true`, giữ `TransmissionMode.None` mặc định và chỉ chọn `Single` khi PTT được giữ.
11. **Voice có thể còn ở channel sau khi disconnect game**: thêm cleanup voice khi local NGO client mất kết nối; voice cũng rời channel khi session rời.
12. **Background/privacy**: PTT tự nhả và force silent khi app pause/lost focus.
13. **Join voice rollback**: nếu JoinGroupChannelAsync thất bại sau khi đã khởi tạo yêu cầu, cố gắng leave channel và xóa trạng thái pending.
14. **Display name/mã phòng**: chuẩn hóa tên 24 ký tự, lọc control/surrogate, join code bỏ khoảng trắng và dấu gạch nối trong nội dung paste; channel ID được loại ký tự không an toàn và giới hạn độ dài.
15. **Microphone permission**: Android permission denied và denied-and-don't-ask-again đều trả kết quả từ chối; Editor tool tạo/check InputSystemUIInputModule và kiểm tra RECORD_AUDIO/iOS usage description.
16. **Package baseline**: dùng NGO 2.13.3; Unity 6000.3.26f1 release notes ghi package được cập nhật từ 2.13.2 lên 2.13.3. Dùng Vivox 16.12.1, MPS 2.4.1 và Authentication 3.8.1. MPS 2.5.0 khai báo dependency Authentication 3.9.0, trong khi baseline 6000.3 đang chọn Authentication 3.8.1; vì vậy giữ MPS 2.4.1 để tránh dependency graph không khớp. MPS 2.4.1 và Authentication 3.8.1 được Unity liệt kê Released cho Editor 6000.3. Package Manager và NGO ILPP vẫn phải xác minh trong Editor thực tế.
17. **PTT khi đã mute**: nút PTT bị disable và handler cũng kiểm tra `IsMuted`/`IsInChannel` để không hiện trạng thái đang nói giả.
18. **Giữ lựa chọn mute qua lần đổi phòng**: join Vivox channel mới không tự đặt `muted=false`; trạng thái mute của người chơi được giữ.
19. **Network avatar packet ordering**: thêm sequence number dạng uint và phép so sánh wrap-safe để bỏ gói Unreliable cũ, tránh proxy giật lùi khi packet đến sai thứ tự.
20. **Network avatar speed validation**: bỏ slack cố định theo từng packet; dung sai tốc độ được tính theo thời gian thực, cộng một packet slack nhỏ. Thêm `NetworkAvatarRules` pure class và 16 test avatar (gồm kiểm tra first-spawn radius và world-ready gate); thêm 8 test cho privacy gate voice.
21. **NetworkManager event rebind**: theo dõi chính object NetworkManager đang subscribe; nếu singleton được thay, unsubscribe cũ và subscribe mới thay vì giữ cờ bool lỗi thời.
22. **Android manifest path**: dùng `Application.dataPath` để validate đường dẫn tuyệt đối, không phụ thuộc current working directory của Editor.
23. **Unity Services environment**: trước đó tài liệu yêu cầu chọn `development` nhưng code gọi `InitializeAsync()` mặc định. Đã thêm `OnlineServices.EnvironmentName` và truyền `InitializationOptions.SetEnvironmentName()` cho cả Session và Vivox. Mặc định là `development`, có thể đổi trường `Environment Name` trên NetworkSessionManager.
24. **PTT input safety**: `VoicePushToTalkButton` kiểm tra channel và mute trước khi nhận PointerDown; exception của async pointer callback được bắt và ghi log.
25. **Network connection readiness**: `IsConnected` và `WaitForNetworkAsync` không chỉ kiểm tra `IsListening`; chúng còn yêu cầu host/client đã thực sự connected, tránh báo kết nối sớm khi NGO client mới bắt đầu listen/connect.
26. **Initial avatar teleport**: lần pose đầu của client phải nằm trong bán kính cho phép quanh spawn xác định trên server; thêm 3 test thuần cho trường hợp trong/ngoài vùng và dữ liệu không hữu hạn.
27. **World generation readiness**: từ chối pose đầu tiên nếu server chưa sinh xong đảo, thay vì dùng world bounds fallback rồi vô tình đánh dấu client đã có pose hợp lệ. Thêm test cho world-ready gate.
28. **Microphone permission fallback**: từ chối RECORD_AUDIO không còn chặn người chơi vào voice channel; họ được nghe ở listen-only, effective mute giữ PTT khóa và nút Bật Micro xin quyền lại.
29. **PTT bị kẹt sau khi app xuống nền**: pointer-up không phải lúc nào cũng được phát khi Android/iOS chuyển app sang background; nút PTT cũ có thể giữ cờ pressed và chặn lần nhấn kế tiếp. Đã ép nhả PTT khi mất focus/pause.
30. **Input microphone vẫn muted sau khi người chơi cấp quyền lại trong Settings**: đã thêm kiểm tra quyền khi app foreground trở lại và đồng bộ trạng thái Mute/Unmute thiết bị với quyền OS + lựa chọn mute trong game.
31. **Voice privacy gate**: tách luật thuần `VoiceTransmissionRules`, yêu cầu đồng thời logged-in, ở trong channel, không rời channel, không muted, có quyền microphone, app foreground và PTT đang giữ; bổ sung 8 test.

## Kết quả kiểm tra tĩnh

- JSON `Packages/manifest.json`: parse hợp lệ (10 dependencies; Authentication 3.8.1 phù hợp Unity 6000.3).
- Các Assembly Definition: parse hợp lệ; graph dự kiến `Core <- Gameplay`, `Core/Gameplay <- UI`, `Core/Gameplay/UGS/NGO <- Network`, test tham chiếu Core/Gameplay/Network.
- `Game.Network` phải tham chiếu `Game.Gameplay` để biên dịch NetworkPlayerAvatar.
- Test assembly tham chiếu `Game.Network` để kiểm thử `NetworkAvatarRules`.
- Kiểm tra tĩnh: 96 file C#, 208 GUID duy nhất, 0 GUID trùng, 0 file dưới Assets thiếu `.meta`, 0 vòng tham chiếu assembly.
- Android manifest parse XML hợp lệ và có RECORD_AUDIO; `git diff --check` sạch.
- 124 EditMode test case được khai báo: 79 Phase 0–6 + 21 OnlineSessionRules + 16 NetworkAvatarRules + 8 VoiceTransmissionRules; chưa chạy Unity Test Runner.
- Có Android `RECORD_AUDIO` manifest; iOS usage string được Editor upgrade ghi vào PlayerSettings.
- Không chứa Vivox secret/token trong source/manifest.

## Chưa thể xác nhận trong môi trường đóng gói

Không có Unity Editor, Unity Package Manager runtime, C# compiler hoặc thiết bị Android/iOS trong môi trường tạo archive. Vì vậy chưa thể khẳng định:

- Package Manager đã resolve thành công trên máy phát triển.
- Unity C# compilation và ILPP/RPC codegen của NGO đã chạy thành công.
- Scene upgrade menu chạy được trong Editor.
- Không thể xác nhận số test xanh trong môi trường đóng gói; cần chạy Unity Test Runner. Mục tiêu nghiệm thu là 124 Passed, 0 Failed.
- Relay session tạo/join thành công trên internet thật.
- Vivox voice hoạt động qua hai microphone/thiết bị thực.
- Android/iOS build và microphone prompt hoạt động trên thiết bị.

Các xác nhận này phải được hoàn tất bằng Unity Editor và hai thiết bị/client thật theo `18_PHASE7_SETUP_AND_TEST.md`.

## Giới hạn gameplay/networking

Phase 7 là online connection + avatar pose + group voice vertical slice. Gameplay data chưa authoritative và chưa đồng bộ. Phase 19 vẫn là cổng bắt buộc trước khi gọi đây là co-op survival đầy đủ hoặc phát hành.

## Git

- Commit Phase 7: `bc4c5c3 feat: add online sessions and vivox group voice`.
- Các commit Phase 0–6 được giữ trong cùng repository.
- Working tree sạch sau commit; `git diff --check` không còn trailing whitespace.
