# Kiến trúc nền tảng — Đảo Sương

## Nguyên tắc

- Luật gameplay như inventory, crafting, damage và survival phải có thể test độc lập, không đặt trong MonoBehaviour.
- Unity components là adapter nhận input, gọi logic và hiển thị; không biến UI thành nguồn quyết định gameplay.
- Client gửi ý định; host/server kiểm chứng item mutation, crafting, damage, loot, quyền xây dựng và tiến triển.
- ScriptableObject là dữ liệu thiết kế; save runtime dùng DTO có schemaVersion, không serialize trực tiếp asset Unity.
- Chỉ thêm assembly khi có ranh giới phụ thuộc thực sự; không tạo assembly cho từng tính năng nhỏ.

## Assembly Definition hiện tại

```text
Game.Core       (Scripts/Core)
  references: []
      ^                 ^
      |                 |
Game.Gameplay        Game.UI
(Scripts/Gameplay)   (Scripts/UI)
  references:          references:
  - Game.Core          - Game.Core
                       - UnityEngine.UI
                       - Unity.InputSystem

Scripts/Network/  (chưa có asmdef; để dành cho NGO / Friends / Vivox)
```

`Game.Core` có danh sách project-assembly references rỗng. Ở giai đoạn hiện tại nó vẫn dùng một số API Unity cơ bản (`Vector2`, `PlayerPrefs`, `JsonUtility`) cho input/settings. Nếu cần Core thuần .NET tuyệt đối, sẽ chuyển phần lưu cài đặt sang adapter khi có yêu cầu test/runtime đa nền tảng.

`Game.Gameplay` gom Player, World, Items, Crafting, Building, Survival, Combat, AI và ScriptableObject definitions. Các module vẫn có namespace riêng để dễ đọc nhưng cùng biên dịch trong một assembly.

`Game.UI` chứa HUD, touch input và `MobileInput`; tham chiếu đúng tên assembly uGUI `UnityEngine.UI` (package ID là `com.unity.ugui`) và `Unity.InputSystem`.

`Scripts/Network` chưa chứa code runtime hoặc asmdef trong Phase 1. Khi triển khai online, tạo assembly Network riêng, chuyển các port vào đó, sau đó thêm adapter NGO / Multiplayer Services / Friends / Vivox. Không để assembly runtime phụ thuộc vào `Assembly-CSharp`.

## Online authority (giai đoạn sau)

- Vertical slice online: host-authoritative listen server qua Relay.
- NGO đảm nhiệm replication; Multiplayer Services quản lý session/connection lifecycle, không thay NGO.
- Host/server xác minh resource gathering, durability, inventory mutation, crafting, damage/death/loot, building permission và world save.
- Client prediction trước hết chỉ dành cho cảm giác điều khiển nhân vật; kết quả gameplay quan trọng phải reconcile với host.
- Network tick khởi đầu 20 Hz; chỉ gửi state cần thiết khi thay đổi.
- Không giả định host migration tự chuyển state; phải có snapshot/restore và test riêng.

## Lưu dữ liệu (giai đoạn sau)

- Profile: settings, progression cá nhân, unlock.
- World snapshot: seed, world version, structures, containers, resource nodes và boss progress.
- Session state: người trong phòng, host và join code; không dùng như database vĩnh viễn.
- Mọi save có schemaVersion, worldVersion, savedAtUtc và owner/world ID; thay đổi schema cần migration tests.
- Host local snapshot chỉ là prototype; production cần authoritative backend/storage để tránh phụ thuộc thiết bị host.

## Voice / social (giai đoạn sau)

- Friends và party đi qua service adapter; UI không gọi SDK trực tiếp.
- Vivox adapter quản lý join/leave/mute/PTT và lifecycle khi disconnect/background.
- Stable player ID dùng chung giữa session và voice channel.
- Positional voice chỉ thêm sau group voice đã ổn định; cần test quyền mic, tai nghe Bluetooth, cuộc gọi và app xuống nền.

## Kiểm thử và quan sát

- Unit tests cho inventory, crafting, survival và damage.
- PlayMode tests cho player movement và controls.
- Device tests cho multi-touch, safe area, pin/nhiệt và performance.
- Log có category/severity; không log token hoặc credential.
