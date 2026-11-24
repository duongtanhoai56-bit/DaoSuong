# Quyết định kỹ thuật và rủi ro

## Quyết định

1. **Unity 6.3 LTS 6000.3.29f1:** khóa đúng patch để tái lập môi trường; không theo editor update tự động giữa sprint.
2. **URP:** phù hợp mục tiêu Android/iOS; asset pipeline phải được tạo/gán trong Project Settings.
3. **Input System:** dùng package mới để hỗ trợ touch/rebinding và nhiều layout HUD.
4. **NGO + Multiplayer Services Sessions:** NGO đồng bộ GameObject/state; Sessions phối hợp player group và kết nối; không nhầm Session với gameplay authority.
5. **Vivox v16:** adapter riêng; chỉ bắt đầu khi loop session + identity cơ bản hoạt động.
6. **Host-authoritative prototype:** giảm chi phí server ban đầu nhưng host có thể gian lận/rớt mạng; không phù hợp economy cạnh tranh hoặc PvP công bằng.
7. **Tách domain khỏi MonoBehaviour:** các luật inventory/crafting/combat được giữ thành C# thuần khi triển khai; `Game.Core` hiện vẫn tham chiếu UnityEngine API cơ bản cho Vector2 và PlayerPrefs, nhưng không tham chiếu assembly dự án hoặc SDK dịch vụ.
8. **LFS:** cấu hình ngay, chỉ thực sự cần khi thêm binary nặng.

## Package plan (xác minh theo Unity 6000.3)

- Input System: `com.unity.inputsystem` 1.20.1.
- uGUI: `com.unity.ugui` 2.0.0; asmdef reference name `UnityEngine.UI`.
- URP: `com.unity.render-pipelines.universal` 17.3.0.
- NGO: chỉ cài bản **Released** tương thích với Unity 6000.3 khi bước vào multiplayer. Tại thời điểm khóa Phase 0, tài liệu Unity 6000.3 liệt kê NGO 2.14.0 trong nhóm **pre-release**; không khóa phiên bản preview cho nền tảng sản phẩm.
- Multiplayer Services: `com.unity.services.multiplayer` 2.5.0.
- Friends: `com.unity.services.friends` 1.4.0.
- Vivox: SDK v16; package version được kiểm tra qua Package Manager lúc triển khai, hiện tài liệu v16 có release 16.13.0.

Không thêm package online vào Phase 0 để giữ compile graph đơn giản. Kiểm tra lại compatibility/version trước mỗi phase có cài package; không tự động sao chép package version cho Editor khác.

## Rủi ro / cắt giảm

- **Một người làm + 34 hệ thống:** quá lớn để hoàn thành như một MVP ngắn. Ưu tiên vertical slice một vùng và một boss.
- **Voice chat mobile:** có quyền mic, background/foreground, Bluetooth/call interruptions, moderation và privacy; bản đầu chỉ group PTT + mute, kiểm thử thiết bị thật.
- **8 người:** không đồng nghĩa 8 người chỉ đơn giản hơn 4; số networked entities, combat, voice và host upload phải stress test.
- **Host migration:** không tự động mang gameplay state; cần snapshot/restore, host election và reconnect flows riêng. Nếu quá tốn kém, bản đầu đóng phòng an toàn khi host rời và hỗ trợ tải lại snapshot.
- **Dedicated server:** không phụ thuộc Unity Multiplay Game Server Hosting; dịch vụ này đã ngừng vận hành sau 31/03/2026. Thiết kế adapter để chọn hosting provider về sau.
- **Cloud Save:** không phải database thế giới lớn; tách profile, world snapshot và session metadata.
- **Performance budgets:** triangle count/draw calls/RAM không thể khẳng định bằng số lý thuyết; phải profile trên máy mục tiêu.

## Quyền sở hữu tài sản

Chỉ dùng nội dung tự tạo hoặc license hợp lệ. Ghi tên asset, nguồn URL, tác giả, license, ngày tải và chỉnh sửa trong `ASSET_CREDITS.md` trước khi đưa vào build.


### Phase 7 package compatibility amendment

- The original early plan listed Multiplayer Services 2.5.0. For the Unity 6000.3.29f1 project baseline, Phase 7 supersedes that choice with Multiplayer Services 2.4.1 + Authentication 3.8.1 because MPS 2.5.0 declares Authentication 3.9.0 as a dependency. Re-check the exact dependency graph in Package Manager before upgrading this pair.
- NGO 2.13.3 is the baseline used by Phase 7. Unity 6000.3.26f1 release notes explicitly update NGO from 2.13.2 to 2.13.3; this project uses 6000.3.29f1. Package Manager resolution and NGO ILPP/code-generation must still be verified in the target Editor.
