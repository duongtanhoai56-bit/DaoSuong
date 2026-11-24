# Scope — Must / Should / Could và Vertical Slice

## Must — phạm vi bản chơi thử

1. Di chuyển nhân vật bằng joystick cảm ứng và camera góc thứ ba; UI có thể đổi vị trí/kích cỡ, hỗ trợ tay trái.
2. Một vùng đảo nhỏ có địa hình và điểm tài nguyên.
3. Thu thập 3 loại tài nguyên cơ bản; một công cụ có độ bền đơn giản.
4. Túi đồ cảm ứng: chọn/di chuyển stack, giới hạn ô và số lượng.
5. Chế tạo 2–3 công thức ở bàn chế tạo cấp đầu.
6. Sinh tồn: máu, đói, khát, thể lực; một luật hạ gục/rơi đồ/hồi sinh.
7. Xây căn cứ cơ bản: nền, tường, cửa; snap điểm đặt; xác thực phía host.
8. 1–2 loại quái; 1 boss với telegraph và ít nhất hai kiểu tấn công có thể né.
9. Co-op host-client 2–4 người qua session/Relay; host-authoritative cho kết quả gameplay.
10. Friends tối thiểu: mã người chơi, gửi/nhận lời mời hoặc lời mời vào phòng.
11. Voice nhóm: push-to-talk mặc định, mute cá nhân, mute toàn đội, trạng thái mic rõ ràng.
12. Lưu/thoát: host snapshot có schema version cho prototype; không yêu cầu Cloud Save trong Must.

**Lưu ý cắt giảm:** “Kết bạn” ở Must chỉ là luồng thêm bạn/mời bạn đơn giản, không phải hệ thống xã hội đầy đủ. Voice Must chỉ là group voice; positional voice, auto device switching nâng cao và moderation tooling đầy đủ nằm sau vertical slice. Boss Must là một encounter ngắn với cơ chế rõ, không cần cinematic dài hoặc nhiều phase phức tạp.

## Should — ưu tiên kế tiếp

- Chu kỳ ngày/đêm và một kiểu thời tiết có tác động gameplay.
- Nhiều công cụ/vũ khí và độ bền cân bằng.
- Hồi sinh đồng đội bằng vật phẩm/động tác có rủi ro.
- Cloud Save đồng bộ hồ sơ và snapshot phù hợp; resume/reconnect.
- Chơi thử 5–8 người trong môi trường stress test; chỉ quảng bá 8 khi đạt tiêu chí.
- Chat chữ, ping bản đồ, voice theo khoảng cách và UI voice chi tiết.

## Could — không nằm trong vertical slice

- Nông trại, chăn nuôi, thú cưng và thú cưỡi.
- Hang động/dungeon sinh ngẫu nhiên, nhiều boss và biome.
- Bang hội, kho chung, chợ, trao đổi vật phẩm.
- PvP opt-in, sự kiện thế giới theo mùa, leaderboard và battle pass.
- Dedicated server production, host migration tự động và matchmaking quy mô lớn.

## Definition of Vertical Slice

Một phòng riêng có 2–4 người, vào được từ Android build, nhóm nghe nhau qua voice và có thể hoàn thành một chuyến đi 10–20 phút:

1. Tạo hoặc tham gia phòng bằng mã.
2. Spawn ở Vịnh Mù.
3. Chọn một mục tiêu nhóm: khai thác, dựng căn cứ hoặc hạ quái tinh anh/boss.
4. Thu thập, dùng túi đồ và chế tạo ít nhất một vật phẩm hữu ích.
5. Xây/nâng cấp ít nhất một cấu kiện ở căn cứ.
6. Rời trận an toàn; khi host lưu và vào lại, snapshot bản thử khôi phục đúng dữ liệu được hỗ trợ.

## Acceptance criteria

- Android Development build chạy trên ít nhất 2 thiết bị vật lý khác nhau.
- Host + 1 client kết nối, di chuyển và nhận kết quả gameplay nhất quán; mở rộng 4 client sau.
- Một luồng voice group PTT hoạt động; có thể mute và rời kênh khi thoát phòng.
- Không thể nhân đôi item bằng spam request/độ trễ trong test cơ bản.
- Không có lỗi chặn tiến trình trong 3 lượt chơi liên tiếp; phiên test kéo dài 20 phút.
- 30 FPS mục tiêu trên thiết bị chuẩn đã công bố; máy yếu có preset Low được đo rõ.
- Người mới hoàn thành một mục tiêu mà không cần developer hướng dẫn trực tiếp.
