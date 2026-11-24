# Giai đoạn 1 — Nhân vật, joystick ảo và camera góc thứ ba

## Mục tiêu

- Di chuyển theo hướng camera bằng joystick cảm ứng.
- Nhảy bằng thao tác chạm xuống (`IPointerDownHandler`), không dùng `Button.onClick`.
- Xoay camera bằng vùng cảm ứng bên phải; joystick và camera hoạt động đồng thời bằng đa chạm.
- Có WASD + Space để kiểm tra nhanh trong Editor.
- Cho phép kéo vị trí, đổi kích thước joystick/nút nhảy, đổi tay trái và lưu khi bấm **Xong**.
- Camera dùng SphereCast để tránh xuyên tường; loại layer Player khỏi Collision Mask. Khi vật cản quá gần, camera được phép tiến sát pivot thay vì ép qua bề mặt.

## Assembly Definition

Chỉ còn 3 assembly runtime của dự án:

| Folder | Assembly | References |
|---|---|---|
| `Scripts/Core` | `Game.Core` | không có project assembly reference |
| `Scripts/Gameplay` | `Game.Gameplay` | `Game.Core` |
| `Scripts/UI` | `Game.UI` | `Game.Core`, `UnityEngine.UI`, `Unity.InputSystem` |

`Scripts/Network` giữ trống/placeholder ở giai đoạn này, chưa có asmdef. Assembly uGUI được đặt là `UnityEngine.UI`; tên package tương ứng là `com.unity.ugui`.

## File C# Giai đoạn 1

```text
Assets/_Game/Scripts/
  Core/
    Game.Core.asmdef
    IPlayerInput.cs
    ControlsSettings.cs
  Gameplay/
    Game.Gameplay.asmdef
    Player/
      PlayerController.cs
      ThirdPersonCamera.cs
    ... các module gameplay cũ đã gom vào cùng assembly
  UI/
    Game.UI.asmdef
    HudEditMode.cs
    HudElement.cs
    VirtualJoystick.cs
    TouchLookZone.cs
    JumpButton.cs
    MobileInput.cs
    HudEditPanel.cs
  Network/
    (để dành cho NGO, Friends, Vivox ở giai đoạn online)
```

## Tạo Scene thử nghiệm

1. Tạo Scene `Assets/_Game/Scenes/World/SCN_PlayerControls.unity`.
2. Tạo `Plane`, đặt Position `(0,0,0)`, Scale `(10,1,10)`.
3. Tạo GameObject rỗng tên `Player`, Transform Position `(0,0,0)`, Layer `Player` (tạo layer mới nếu chưa có). Thêm `CharacterController` với Height `2`, Radius `0.5`, Center `(0,1,0)` và `PlayerController`. Tạo Capsule primitive làm con tên `Visual`, đặt Local Position `(0,1,0)`, xóa `CapsuleCollider` của Visual, đặt Layer `Player`. Cách này giữ pivot Player ở chân để camera height mặc định `1.6` hợp lý. Nếu muốn dùng Capsule primitive trực tiếp làm Player và đặt Transform Y=1 như cách nhanh, phải xóa CapsuleCollider, đặt CharacterController Center `(0,0,0)` và giảm camera Height xuống khoảng `0.6`; không trộn hai quy ước vì sẽ làm nhân vật lơ lửng.
4. Tạo `Main Camera`, thêm `Camera` và `ThirdPersonCamera`. Kéo Player vào Target. Trong Collision Mask bỏ chọn layer `Player`. Để `Camera.main` tự tìm, Tag camera là `MainCamera`.
5. Tạo `Canvas` (Screen Space - Overlay), thêm `CanvasScaler` Scale With Screen Size, Reference Resolution `1920 x 1080`, Match `0.5`; giữ `GraphicRaycaster`.
6. Tạo `EventSystem` và dùng `InputSystemUIInputModule` (nếu Unity tạo `StandaloneInputModule`, chọn **Replace with InputSystemUIInputModule**).
7. Trong Hierarchy, sắp xếp con Canvas từ trên xuống: `LookZone`, `Joystick`, `JumpButton`, `EditController`. Unity vẽ sibling nằm sau lên trên sibling trước, vì vậy `EditController` nằm trên cùng. Các vùng nhận chạm cần `Image` bật Raycast Target; riêng ảnh Handle tắt Raycast Target.
8. `LookZone`: anchors Min `(0.5,0)`, Max `(1,1)`, offsets 0, Image alpha 0 nhưng Raycast Target bật. Gắn `TouchLookZone` và `HudElement` với ID `look`, bỏ chọn Customizable.
9. `Joystick`: Image tròn `300 x 300`, anchors Min/Max `(0,0)`, pivot `(0.5,0.5)`, anchored position `(260,260)`. Tạo con `Handle` `120 x 120`, pivot/anchors giữa, Raycast Target tắt. Gắn `VirtualJoystick`, kéo Handle vào field `Handle`. Gắn `HudElement` ID `joystick`.
10. `JumpButton`: Image tròn `180 x 180`, anchors Min/Max `(1,0)`, pivot `(0.5,0.5)`, anchored position `(-200,260)`. Gắn `JumpButton` và `HudElement` ID `jump`. **Không thêm component Button vào JumpButton.**
11. Tạo GameObject trống `Input`, thêm `MobileInput`, kéo `VirtualJoystick`, `TouchLookZone`, `JumpButton` tương ứng vào Inspector.
12. Tạo nút ⚙ có `Button` bình thường để mở bảng chỉnh HUD; nút ⚙ không phải nút nhảy. Gắn thêm `HudElement` ID `gear` và bỏ chọn Customizable để nút ⚙ cũng phản chiếu khi bật tay trái nhưng không bị kéo/scale. Tạo `EditController` gắn `HudEditPanel`, gán Panel, Toggle tay trái, Slider kích cỡ và Slider độ nhạy. Panel tắt lúc bắt đầu.
13. Cấu hình Slider Scale Min `0.5`, Max `2`; Sensitivity Min `0.05`, Max `0.4`. Toggle `On Value Changed (Boolean)` → `HudEditPanel.SetLeftHanded`; Slider Scale `On Value Changed (Single)` → `SetScale`; Slider Sensitivity `On Value Changed (Single)` → `SetSensitivity`. Dùng nhóm Dynamic Functions ở đầu danh sách.
14. Nút Reset `OnClick` → `ResetAll`; nút Xong `OnClick` → `Done`; nút ⚙ `OnClick` → `OpenEdit`.

`HudElement` tự yêu cầu `CanvasGroup` bằng `[RequireComponent]`; không cần thêm thủ công.

## Quy tắc lưu và lật tay trái

- Chỉnh sửa chỉ thay đổi dữ liệu trong bộ nhớ; bấm **Xong** mới gọi `PlayerPrefs.Save()`.
- Đóng Play Mode mà chưa bấm Xong sẽ không lưu thay đổi đang thử.
- Khi tay trái bật, anchorMin/anchorMax/pivot và tọa độ X được phản chiếu so với bố cục gốc; do vậy không tích lũy sai lệch sau nhiều lần bật/tắt.
- Offset được clamp theo RectTransform cha, tránh kéo joystick/nút nhảy ra ngoài màn hình.
- Kích thước lưu riêng cho mỗi ID (`joystick`, `jump`); LookZone không cho kéo/scale nhưng vẫn được phản chiếu.

## Kiểm thử

1. Mở Scene, Play: WASD/arrow di chuyển, Space nhảy; joystick cảm ứng di chuyển, kéo vùng phải xoay camera.
2. Bấm ⚙, chạm joystick/nút nhảy để chọn (đang chọn sáng, phần khác mờ), kéo sang vị trí mới, chỉnh scale.
3. Bật tay trái: joystick và nút nhảy đổi vị trí đối xứng, LookZone chuyển sang nửa trái.
4. Bấm Xong, Stop Play, Play lại: xác nhận layout và sensitivity còn giữ.
5. Build và chạy trên thiết bị thật: giữ joystick bằng ngón cái trái, đồng thời kéo LookZone bằng ngón tay phải. Editor chuột không giả lập được hai ngón tay.
6. Kiểm tra va chạm: đặt tường sau Player, xoay camera vào tường; camera phải tiến lại gần thay vì xuyên tường.

## Lỗi thường gặp

- **Không có EventSystems / UI không nhận chạm:** kiểm tra `Game.UI.asmdef` reference `UnityEngine.UI`, Canvas có `GraphicRaycaster`, Scene có `EventSystem` và `InputSystemUIInputModule`.
- **Input System báo lỗi:** Project Settings > Player > Active Input Handling chọn Input System Package (New) hoặc Both; xóa `StandaloneInputModule` nếu đang dùng Input System và thay bằng `InputSystemUIInputModule`.
- **Joystick không kéo:** Image của Joystick phải bật Raycast Target; Canvas phải có GraphicRaycaster; EventSystem phải có input module đúng.
- **Handle không nhận chạm:** tắt Raycast Target trên Image của Handle để sự kiện truyền tới Joystick cha.
- **Camera bị kẹt hoặc xuyên tường:** kiểm tra Player nằm ở layer `Player` và layer đó đã bỏ khỏi Collision Mask; chỉnh SphereCast radius/buffer nếu cần.
- **Nhân vật lơ lửng:** dùng Player root ở Y=0 với CharacterController Center Y=1; Capsule hiển thị là child ở Local Y=1. Nếu dùng Capsule làm root đặt Y=1 thì CharacterController Center phải là Y=0.
- **Không lưu layout:** phải bấm nút Xong; đóng Play Mode trước đó sẽ bỏ các thay đổi chưa lưu.

## Giới hạn bản này

- Chưa có animation controller, slope tuning nâng cao, root motion hoặc camera Cinemachine.
- Camera hiện theo target trực tiếp; không thêm Cinemachine chỉ cho một camera cơ bản để tránh dependency và overhead.
- Đây là input cho một local player; không có multiplayer/network authority ở Phase 1.
- Cần build trên Android/iOS thật để nghiệm thu đa chạm, độ trễ và safe area.
