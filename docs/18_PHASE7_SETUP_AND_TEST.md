# Giai đoạn 7 — Setup, cấu hình dịch vụ và kiểm thử

## 0. Trước khi mở project

Mở đúng thư mục Unity project bằng Unity Hub:

- Editor: `6000.3.29f1`.
- Chọn Android Build Support (SDK, NDK, OpenJDK) nếu build Android.
- iOS build cần macOS và Xcode.
- Đảm bảo mạng Internet ổn định để Unity Package Manager tải SDK.

## 1. Package Manager

Mở `Packages/manifest.json` và chờ package resolve. Các package chính:

| Package | Version |
|---|---:|
| `com.unity.services.multiplayer` | 2.4.1 |
| `com.unity.netcode.gameobjects` | 2.13.3 |
| `com.unity.transport` | 2.7.5 |
| `com.unity.services.authentication` | 3.8.1 |
| `com.unity.services.vivox` | 16.12.1 |
| `com.unity.multiplayer.playmode` | 2.0.2 |

MPS 2.4.1 được chọn thay cho 2.5.0 để tránh kéo Authentication 3.9.0 trong khi baseline Unity 6000.3 dùng Authentication 3.8.1. Nếu Package Manager báo dependency conflict, không tự ý nâng riêng NGO/Vivox/Authentication. Lưu `Packages/manifest.json`, `Packages/packages-lock.json` và toàn bộ lỗi Package Manager để kiểm tra đúng version graph.

## 2. Link Unity project với Dashboard

1. Mở `Edit → Project Settings → Services` (hoặc Unity Dashboard theo giao diện Editor hiện hành).
2. Tạo/chọn Unity Cloud Project và link project local với đúng Project ID.
3. Chọn đúng Environment (ví dụ `development`). Tất cả máy test phải dùng cùng project và environment. Trong Scene, chọn `OnlineServices` và kiểm tra `NetworkSessionManager → Environment Name` trùng tên environment trên Dashboard; mặc định là `development`. Nếu đổi sang `production`, hãy đổi giống nhau trên mọi client trước khi Unity Services được khởi tạo.
4. Bật Unity Authentication và Anonymous Sign-In.
5. Bật Multiplayer/Relay cho project.
6. Cấu hình Vivox Voice and Text cho cùng project/environment theo hướng dẫn Unity Dashboard.
7. Không tạo hoặc dán secret/token Vivox vào script, `manifest.json`, Git hay APK. Với Unity Authentication, Vivox SDK tự dùng flow đăng nhập được hỗ trợ.

Nếu Dashboard chưa bật service hoặc project chưa link, code có thể compile nhưng Create/Join/Voice sẽ thất bại lúc runtime.

## 3. Tạo Scene theo thứ tự các giai đoạn

Nếu mở project lần đầu hoặc scene chưa được sinh, chạy các menu giai đoạn trước theo hướng dẫn tương ứng:

1. Phase 2: tạo World Assets và `SCN_IslandWorld`.
2. Phase 3: tạo Gather data và Action Button/Gather HUD.
3. Phase 4: nâng cấp Inventory/Storage.
4. Phase 5: tạo Crafting data và nâng cấp Scene.
5. Phase 6: tạo Survival data/HUD và nâng cấp Scene.
6. Phase 7:
   - `Game → Giai đoạn 7 → 1 - Tạo Network Player Prefab`.
   - `Game → Giai đoạn 7 → 2 - Nâng cấp Scene hiện tại`.
   - `Game → Giai đoạn 7 → 3 - Validate Multiplayer Scene`.

Lưu Scene sau mỗi lần nâng cấp. Sao lưu Scene trước khi chạy Editor tool trên scene bạn đã chỉnh sửa thủ công.

### Thành phần scene được yêu cầu

- `NetworkManager` với `UnityTransport`.
- `NetworkConfig.PlayerPrefab` trỏ đến `NetworkPlayerAvatar.prefab` có `NetworkObject`, `NetworkTransform`, `NetworkPlayerAvatar`.
- `NetworkManager.NetworkConfig.TickRate = 20`.
- `NetworkSessionManager` và `VivoxVoiceManager` trên GameObject `OnlineServices`. `NetworkSessionManager.Environment Name` phải trùng UGS Dashboard.
- `OnlineSessionHUD` trong Canvas.
- `EventSystem` với `InputSystemUIInputModule`.
- Scene local Player có tag `Player`.

## 4. Test nhanh hai client

### Test network avatar

1. Mở `Window → Multiplayer → Multiplayer Play Mode`.
2. Tạo Virtual Player 2 (có thể mở thêm Virtual Player 3/4 nếu máy đủ RAM).
3. Chạy cùng scene trên các instance.
4. Client A mở `ONLINE`, nhập tên và chọn `TẠO PHÒNG RIÊNG`.
5. Sao chép join code từ A.
6. Client B mở `ONLINE`, nhập mã và chọn `VÀO PHÒNG`.
7. Chờ cả hai client báo trạng thái kết nối; session manager chờ trạng thái NGO client thực sự connected, không chỉ NetworkManager đang listening.
8. Khi kết nối thành công, mỗi client phải nhìn thấy proxy nhân vật của người còn lại.
9. Di chuyển/đổi hướng ở A; B phải nhận được transform tương ứng, và ngược lại.
10. Đóng panel ONLINE: PTT nổi phải còn hiển thị khi đã vào Vivox channel.

**Lưu ý seed:** Phase 7 chưa gửi seed đảo qua session. Tất cả client phải dùng seed mặc định giống nhau (`12345`) khi kiểm tra avatar. Không bấm `Đảo mới` riêng trên một client; khi lệch seed, server sẽ từ chối pose đầu tiên nằm ngoài bán kính spawn dự kiến. Đồng bộ seed qua session properties thuộc Phase 19.

### Test voice

1. Dùng hai thiết bị hoặc hai client có thiết bị microphone/loopback phù hợp. MPPM hữu ích để test networking; voice trên cùng máy có thể chia sẻ một thiết bị microphone và không đại diện cho kiểm thử hai thiết bị thật.
2. Cấp quyền microphone khi Android/iOS hỏi. Nếu từ chối, người chơi vẫn vào được kênh ở chế độ chỉ nghe; PTT không truyền.
3. Cả hai client phải báo đã vào voice nhóm (hoặc chế độ chỉ nghe nếu chưa cấp quyền).
4. Giữ PTT: người bên kia nghe được giọng nói.
5. Nhả PTT: âm thanh truyền phải dừng.
6. Bật `TẮT MICRO`: PTT không truyền. Nếu hệ điều hành đã từ chối quyền mic, bấm `BẬT MICRO` sẽ xin quyền lại; khi vẫn bị từ chối, người chơi tiếp tục nghe được đồng đội nhưng không thể nói.
7. Mở panel ONLINE: PTT nổi tạm ẩn; đóng panel: PTT hiện lại nếu còn trong voice channel.
8. Đưa app xuống nền khi đang giữ PTT: transmission phải tự dừng.
9. Rời session: Vivox channel phải tự rời.
10. Khi đang giữ PTT, đưa app xuống nền rồi quay lại: truyền âm phải dừng, nút PTT không bị kẹt và có thể nhấn giữ lại. Nếu thu hồi/cấp lại microphone trong Settings, trạng thái input phải được đồng bộ theo quyền hệ điều hành.

## 5. Android/iOS microphone

### Android

- `Assets/Plugins/Android/AndroidManifest.xml` khai báo `android.permission.RECORD_AUDIO`.
- Runtime xin quyền bằng `UnityEngine.Android.Permission`.
- Nếu người dùng từ chối, game online vẫn chơi được và người chơi vẫn nghe được voice nhóm ở chế độ listen-only; chỉ microphone truyền đi bị khóa.

### iOS

- Phase 7 Editor upgrade đặt `PlayerSettings.iOS.microphoneUsageDescription`.
- Trước khi archive/build, kiểm tra giá trị `NSMicrophoneUsageDescription` có trong Info.plist cuối.
- Kiểm thử cấp quyền, từ chối quyền, thu hồi quyền trong Settings, đổi app background/foreground.

## 6. EditMode test và validate

- Chạy `Game → Giai đoạn 7 → 3 - Validate Multiplayer Scene`.
- Mở `Window → General → Test Runner → EditMode → Run All`.
- Kỳ vọng: tất cả test pass, không có compile errors trong Console.
- Test count hiện được khai báo trong project: 124; đây chưa phải kết quả test đã chạy.

## 7. Giới hạn đã biết

Giai đoạn này chỉ đồng bộ avatar pose. Inventory, gathering, crafting, survival, resource respawn, xây dựng, AI/boss và thời tiết vẫn chưa sync. Không dùng hai client để khai thác/craft cùng một world và kỳ vọng thay đổi xuất hiện ở client còn lại. Cần Phase 19 server-authoritative gameplay.

Hiện chưa có Friends list, party, text chat, proximity voice, speaker indicator, individual volume, block/report, network reconnect tự động, host migration, persistent world server hoặc dedicated server. Người chơi kết nối bằng mã mời; voice là group channel.
