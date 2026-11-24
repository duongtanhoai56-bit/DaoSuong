# Giai đoạn 7 — Online co-op và voice nhóm

## Phạm vi đã triển khai trong vertical slice

- Unity Gaming Services Authentication đăng nhập ẩn danh.
- Multiplayer Services Sessions tạo phòng riêng và tham gia bằng join code.
- Relay networking kết nối host/client qua Internet.
- Netcode for GameObjects (NGO) tạo PlayerPrefab cho mỗi client.
- NetworkPlayerAvatar gửi vị trí/góc quay của Player cục bộ lên server ở 20 Hz; server kiểm tra giá trị hữu hạn, bounds, quaternion, lần xuất hiện đầu gần spawn xác định, tốc độ theo elapsed time và sequence number trước khi cập nhật proxy network.
- Vivox group channel dùng chung theo Session ID.
- Push-to-talk (PTT): mặc định không truyền âm thanh; chỉ truyền khi người chơi giữ nút mic.
- Mute microphone được giữ qua lần đổi phòng; nút PTT bị vô hiệu hóa khi mute.
- Xin quyền microphone Android/iOS; nếu bị từ chối vẫn được nghe voice ở chế độ listen-only, PTT bị khóa và có thể xin quyền lại khi người chơi bật mic. Khi app xuống nền PTT tự nhả; khi foreground trở lại quyền OS và trạng thái mute input device được kiểm tra lại.
- HUD tạo runtime: ONLINE, display name, join code, copy code, create/join/leave, PTT và mute.

## Ranh giới kiến trúc

`Game.Core` chứa các hợp đồng transport-neutral (`IOnlineSessionPort`, `IVoiceChatPort`) và quy tắc kiểm tra join code/tên người chơi.

`Game.Network` triển khai adapter UGS Sessions/Relay/NGO và Vivox. Assembly này tham chiếu `Game.Core` và `Game.Gameplay` vì `NetworkPlayerAvatar` cần đọc biên đảo từ `IslandGenerator`.

`Game.UI` chỉ gọi hợp đồng trong `Game.Core`, không tham chiếu trực tiếp SDK network hoặc Vivox.

## Những gì CHƯA đồng bộ

Đây là nền tảng online đầu tiên, không phải toàn bộ hệ thống multiplayer Phase 19. Hiện tại chỉ proxy vị trí và góc quay nhân vật được đồng bộ. Các trạng thái sau vẫn ở local client và chưa phải server-authoritative:

- inventory, durability, chest, item pickup;
- resource node, drop loot, respawn, câu cá;
- crafting và station;
- health, hunger, thirst, stamina, temperature, buff/debuff, death/revive;
- building, AI, boss, quest, world time/weather, world save.

Không dùng bản này để phát hành hoặc cho rằng dữ liệu gameplay đã chống gian lận. Phase 19 phải chuyển các hành động kinh tế/gameplay quan trọng sang server validation và đồng bộ trạng thái theo ID ổn định.

Chưa có Friends list, Party, text chat, voice proximity/3D, speaker indicator, per-player volume, report/block, reconnect tự động, host migration hoặc dedicated server. Người chơi mời bạn bằng join code. Voice hiện là group voice, mọi người trong cùng session nghe nhau.

## Quyết định package

- Unity Editor: `6000.3.29f1`.
- Multiplayer Services: `2.4.1` (Released cho Unity 6000.3; dùng cùng Authentication 3.8.1).
- NGO: `2.13.3` (Unity 6000.3.26f1 release notes ghi rõ package được cập nhật từ 2.13.2 lên 2.13.3; project dùng Editor 6000.3.29f1. Vẫn phải xác minh Package Manager resolve và ILPP/codegen trong đúng Editor trước khi nghiệm thu).
- Unity Transport: `2.7.5`.
- Authentication: `3.8.1` (bản Unity liệt kê Released cho Editor 6000.3; không dùng `3.9.0`, vì bản này không xuất hiện trong danh mục Released của 6000.3).
- Vivox: `16.12.1` (bản sửa lỗi Android quan trọng so với 16.12.0).
- Multiplayer Play Mode: `2.0.2`.

Package versions được khai báo ở `Packages/manifest.json`. Không thêm token, Vivox key hoặc service secret vào client project.

## Network flow

1. Người tạo phòng bấm `TẠO PHÒNG RIÊNG`.
2. Client khởi tạo Unity Services và đăng nhập Authentication ẩn danh.
3. Client kiểm tra NetworkManager, UnityTransport và PlayerPrefab.
4. `CreateSessionAsync(SessionOptions.WithRelayNetwork())` tạo session riêng, Relay và tự khởi động NGO.
5. Session code hiển thị trên HUD để người chơi sao chép gửi cho bạn.
6. Người bạn nhập mã và chọn `VÀO PHÒNG`; `JoinSessionByCodeAsync` đưa họ vào cùng Session/Relay/NGO.
7. NGO spawn NetworkPlayerAvatar cho từng client.
8. Sau khi vào game session, client xin quyền microphone, khởi tạo Vivox bằng Unity Authentication và join cùng group channel được đặt tên từ Session ID.
9. Vivox transmission mode được đặt về `None`; PTT đổi sang `Single` chỉ khi giữ nút. Nhả nút, mute, app background hoặc network disconnect đều trả về `None`.

## Network authority hiện tại

Local Player của Phase 1 vẫn là controller/camera. Mỗi client sở hữu một NetworkPlayerAvatar proxy; client gửi pose qua ServerRpc Unreliable 20 Hz. Server xác thực pose và cập nhật transform; NetworkTransform server-authoritative phát proxy đến các client khác.

Đây là giải pháp bước đầu cho avatar. Gói pose đầu tiên phải ở gần spawn xác định trên server; các pose tiếp theo qua kiểm tra tốc độ và sequence. Tất cả máy phải dùng cùng seed đảo mặc định trong vertical slice này; seed do host tự đổi chưa được gửi qua Session properties và sẽ được xử lý ở Phase 19. Chưa phải client prediction/lag compensation hoàn chỉnh, chưa đồng bộ hitbox/animation/interaction. Phase 19 cần thay thế bằng luồng authoritative cho movement và gameplay.
