# Project Setup và cấu trúc — Đảo Sương

## Editor và package

- Unity Editor: **6000.3.29f1** (Unity 6.3 LTS).
- URP: `17.3.0`.
- Input System: `1.20.1`.
- Unity UI / uGUI: `com.unity.ugui` `2.0.0`.
- NGO, Multiplayer Services, Friends và Vivox chưa cài trong Phase 1; chỉ cài các package online khi bắt đầu giai đoạn tương ứng.

Unity tạo `Packages/packages-lock.json` sau lần resolve package đầu tiên; commit file lock sau khi Editor mở project thành công.

## Hoàn thiện URP

Project có dependency URP nhưng cần tạo/gán asset pipeline trong Editor:

1. `Assets > Create > Rendering > URP Pipeline Asset (with Universal Renderer)`.
2. Lưu trong `Assets/_Game/Settings/URP/`.
3. `Edit > Project Settings > Graphics` → gán asset vào Default Render Pipeline.
4. `Edit > Project Settings > Quality` → gán pipeline asset cho từng quality level.
5. Mở scene và kiểm tra Console/Game view; xác nhận material không màu hồng.

## Cây thư mục

```text
Assets/_Game/
  Scripts/
    Core/       # Game.Core; không tham chiếu assembly dự án khác
    Gameplay/   # Game.Gameplay; Player, World, Items, Crafting, Building, Survival, Combat, AI, Data definitions
    UI/         # Game.UI; uGUI + Input System mobile controls
    Network/    # dành cho NGO, Multiplayer Services, Friends, Vivox (chưa có asmdef)
    Editor/     # editor tools; không được gọi từ runtime
  Data/         # ScriptableObject assets thực tế (items, recipes, creatures, bosses, quests)
  Scenes/       # Bootstrap, World
  Prefabs/      # Characters, Environment, Items, Creatures, UI
  Art/          # Environment, Characters, Props, UI
  Audio/        # Music, SFX, Voice
  Settings/     # URP và Quality presets
  Tests/        # EditMode, PlayMode
Packages/ ProjectSettings/ docs/
```

## Assembly Definition

- `Scripts/Core/Game.Core.asmdef`: `references: []`; `noEngineReferences: false` vì input/settings dùng một số API Unity nền tảng.
- `Scripts/Gameplay/Game.Gameplay.asmdef`: tham chiếu `Game.Core`.
- `Scripts/UI/Game.UI.asmdef`: tham chiếu `Game.Core`, `UnityEngine.UI`, `Unity.InputSystem`.
- Không tạo asmdef riêng cho `Network`/`Social` ở Phase 1.

Tên package uGUI là `com.unity.ugui`, nhưng assembly reference được đặt trong asmdef là `UnityEngine.UI`. Input System UI module nối uGUI với pointer/touch của Input System.

## Input System cho uGUI

Đặt `Edit > Project Settings > Player > Active Input Handling` thành `Input System Package (New)` (hoặc `Both` nếu dự án dùng asset legacy). Mỗi scene phải có `EventSystem` với `InputSystemUIInputModule`; Canvas có `GraphicRaycaster`. Nếu Unity tạo `StandaloneInputModule`, dùng lệnh Replace trong Inspector để thay bằng `InputSystemUIInputModule`.

## Quy tắc code và asset

- Namespace và class: PascalCase; file khớp tên class chính.
- ID dữ liệu là ID bất biến, không dùng display name làm khóa.
- ScriptableObject chỉ là authoring/config; runtime state/save dùng DTO.
- `.meta` được commit; không commit `Library/`, `Temp/`, `Obj/`, build outputs, credentials, keystore hoặc signing files.
