# Giai đoạn 2 — Quần đảo low-poly, nước, ngày đêm và thời tiết

## Mục tiêu và phạm vi

- Sinh đảo từ `seed` + `IslandSettings`, có bãi biển, rừng, núi, đầm lầy và tối đa 3 cửa hang giữ chỗ.
- Chia địa hình thành 4 × 4 = 16 chunk độc lập. Unity frustum-cull từng `MeshRenderer` theo bounds của chunk.
- Mặt nước là một mesh bốn đỉnh / hai tam giác; hiệu ứng sóng chỉ thay đổi normal/ánh sáng trong shader.
- Cây và đá là các GameObject riêng có Collider để Giai đoạn 3 có thể raycast, chặt/đập và xóa từng vật thể.
- Render cây/đá bằng `Graphics.RenderMeshInstanced`, chia nhóm theo mesh và chunk để không bị SRP Batcher ưu tiên thay cho instancing của `MeshRenderer`.
- Ngày đêm, mưa, bão, sương mù và sương riêng tại đầm lầy.
- Debug panel chỉ tồn tại trong Editor và Development Build.

## Tạo Scene chạy thử tự động

Mở project bằng Unity `6000.3.29f1`, chờ Package Manager và shader import hoàn tất.

**Input Handling cho Editor/Development Build:** đặt `Edit → Project Settings → Player → Active Input Handling` thành **Both** khi dùng bảng debug `OnGUI` trong Play Mode. Input System không gửi input event tới IMGUI nếu chỉ chọn `Input System Package (New)`. `InputSystemUIInputModule` vẫn xử lý joystick/uGUI bằng Input System. Khi build Release không cần debug `OnGUI`, có thể chuyển lại **Input System Package (New)**.

1. Chọn menu **Đảo Sương → Phase 2 → 1 - Create World Assets**.
2. Chọn **Đảo Sương → Phase 2 → 2 - Create Playable Island Scene**.
3. Unity tạo `Assets/_Game/Scenes/World/SCN_IslandWorld.unity`, thêm Scene vào Build Settings và tự nối các tham chiếu.
4. Mở `SCN_IslandWorld` nếu chưa tự mở, nhấn Play.

Tool tạo:

- `Assets/_Game/Data/World/IslandSettings.asset`
- `Assets/_Game/Art/Materials/M_Terrain.mat`
- `Assets/_Game/Art/Materials/M_Water.mat`
- `Assets/_Game/Art/Materials/M_Rain.mat`
- `Assets/_Game/Art/Materials/M_Player.mat`
- Player + CharacterController + PlayerController
- Main Camera + ThirdPersonCamera; Collision Mask loại layer `Player`
- World + IslandGenerator + WaterSurface + DayNightCycle + WeatherSystem + WorldDebugPanel
- Canvas, EventSystem + InputSystemUIInputModule, joystick, LookZone, nút nhảy, nút HUD và bảng chỉnh HUD

Scene giữ quy ước Player pivot ở chân: CharacterController Center `(0,1,0)`, Height `2`, Capsule Visual là con ở Local Y `1`. Không thêm `Button` vào nút nhảy.

## URP Mobile — phải kích hoạt pipeline trước khi đánh giá hình ảnh

Project có package URP nhưng Pipeline Asset phải được tạo và gán riêng:

1. Trong Project, chọn `Assets/_Game/Settings/URP`.
2. Chuột phải → **Create → Rendering → URP Asset (with Universal Renderer)**.
3. Đặt tên `URP_Mobile`.
4. Mở **Edit → Project Settings → Graphics**, gán `URP_Mobile` vào Default Render Pipeline Asset.
5. Trong **Project Settings → Quality**, kiểm tra từng quality level dùng đúng URP asset, không để quality level ghi đè bằng asset khác.
6. Trong URP Asset: HDR Off, Soft Shadows Off, Shadow Distance `45`, Cascade Count `2`, MSAA `1x / Off`.
7. Directional Light dùng Hard Shadows. Main Camera Far Clip `250`, Allow MSAA Off.
8. Trong Lighting, dùng Default-Skybox và tắt Auto Generate nếu máy Editor bị khựng khi thử giờ/thời tiết.

Unity hướng dẫn tạo và kích hoạt URP Asset qua menu Create và Graphics Settings; package hiện diện trong manifest không tự làm URP thành pipeline đang hoạt động. Tham khảo tài liệu Unity 6.3 ở phần nguồn bên dưới.

## Cây thư mục Giai đoạn 2

```text
Assets/_Game/
  Art/
    Shaders/
      LowPolyVertexColor.shader
      LowPolyWater.shader
    Materials/                 (được Editor tool tạo)
  Data/World/                  (IslandSettings.asset được tạo)
  Scripts/Gameplay/World/
    Biome.cs
    CaveEntrance.cs
    DeterministicRandom.cs
    IslandSettings.cs
    IslandNoise.cs
    MeshBuilder.cs
    LowPolyMeshes.cs
    IslandGenerator.cs
    IslandInstanceRenderer.cs
    WaterSurface.cs
    DayNightCycle.cs
    WeatherSystem.cs
    WorldDebugPanel.cs
  Scripts/Editor/
    Phase2WorldTools.cs
  Scenes/World/
    SCN_IslandWorld.unity     (được Editor tool tạo)
```

Không tạo thêm Assembly Definition runtime. Các script World nằm trong folder `Scripts/Gameplay`, nên tiếp tục thuộc `Game.Gameplay`. Tool Editor nằm trong `Scripts/Editor`, thuộc editor compilation domain.

## Cấu hình IslandSettings mặc định

| Thuộc tính | Giá trị |
|---|---:|
| World Size | 480 m × 480 m |
| Chunks | 4 × 4 = 16 |
| Cells mỗi chunk | 24 × 24 |
| Tổng cells | 96 × 96 |
| Terrain triangles | 18,432 |
| Max Trees | 400 |
| Max Rocks | 80 |
| Cave entrances | 3 |
| Prop grid step | 8 m |

Có thể tạo thủ công bằng **Create → Game → Island Settings** hoặc dùng menu tạo asset tự động. Asset có thể điều chỉnh trong Inspector. Với máy yếu, giảm `maxTrees` trước; sau đó giảm `maxRocks` hoặc `cellsPerChunk` nếu cần. Không thay đổi `chunksPerSide` nếu vẫn muốn giữ đúng 16 chunk.

## Seed và tính tái lập

- Generator algorithm version 2 dùng value-noise nội bộ từ lattice hash + smooth interpolation; offsets và vị trí props lấy từ `DeterministicRandom`, không phụ thuộc `UnityEngine.Random` / `System.Random`.
- Vị trí / biến thể cây, đá và hang dùng cùng PRNG xác định, không dựa vào Unity global random hoặc `System.Random`.
- `Checksum` hiện bao gồm generator version, height map đã lượng tử hóa theo centimet, seed, thông số sinh địa hình/biome/props và các vị trí props/hang.
- Nút **Sinh lại cùng seed** phải in cùng checksum; **Đảo mới (seed ngẫu nhiên)** phải cho checksum khác trong Console.
- Muốn kết quả đồng nhất giữa nhiều client, tất cả client phải có cùng `IslandSettings` (hoặc cùng version/hash dữ liệu), thuật toán generator và game build. Chỉ gửi seed là chưa đủ nếu client đang dùng khác phiên bản settings/code. Giai đoạn 19 sẽ bổ sung version/hash xác thực.

## Chunk, object và draw calls

- Mỗi chunk là một `MeshFilter` + `MeshRenderer` + `MeshCollider`. Unity frustum-cull phần render chunk dựa theo mesh bounds; MeshCollider vẫn tham gia physics khi chunk ngoài màn hình.
- Cây và đá có từng GameObject, Collider và Transform riêng, nhưng không có MeshRenderer khi thiết bị hỗ trợ GPU instancing.
- `IslandInstanceRenderer` gom các `Matrix4x4` theo `(mesh, chunk)` và gọi `Graphics.RenderMeshInstanced`; mỗi nhóm có bounds riêng để culling.
- Tối đa 511 instance cho mỗi lần gọi trong cấu hình hiện tại (do matrix object-to-world + world-to-object, không giả định scale đồng nhất). Các nhóm hiện nhỏ hơn nhiều.
- Nếu thiết bị không hỗ trợ instancing hoặc material chưa bật instancing, renderer tự chuyển sang `MeshRenderer` riêng làm fallback. Khi fallback, batches có thể cao hơn 300; cần giảm số cây/đá hoặc đặt target đồ họa thấp.
- `M_Terrain` được Editor tool bật `Enable GPU Instancing`; shader có `#pragma multi_compile_instancing`, macro instance ID và nhận main-light hard shadows. Cây/đá được render instanced, nhận bóng nhưng không tự đổ bóng để giữ ngân sách mobile; Player (URP/Lit) vẫn có thể đổ bóng lên địa hình gần.

## Nước

`WaterSurface` tạo một child `WaterPlane_FollowCamera` gồm bốn vertices và hai triangles. Chỉ child này đi theo camera ở Y=0. Tuyệt đối không di chuyển `World` root, vì IslandGenerator, DayNightCycle, WeatherSystem và debug đều gắn trên cùng World GameObject.

Shader tính ba lớp sóng dạng gradient để tạo normal giả, Fresnel màu nông/sâu và specular. Không có vertex displacement nên silhouette mặt nước vẫn phẳng, rẻ hơn cho mobile.

## Ngày đêm và thời tiết

- Một chu kỳ mặc định dài 720 giây (12 phút); có thể điều chỉnh `dayLengthSeconds`.
- Debug: Bình minh 06:00, Trưa 12:00, Hoàng hôn 18:00, Đêm 00:00 và tua nhanh.
- Weather presets: Clear, Fog, Rain, Storm. Chuyển trạng thái bằng blend 25 giây.
- Bình minh/hoàng hôn đổi tint skybox sang cam; đêm giảm exposure nhưng giữ ambient đủ nhìn. Storm giảm ánh sáng, tăng mưa và tạo flash; tiếng sấm sẽ bổ sung ở giai đoạn âm thanh.
- Khi Player ở biome Swamp, Fog Density tăng dần; rời đầm lầy thì giảm dần.
- Mưa là Particle System đi theo camera, không tạo object mưa trên toàn bản đồ.
- `WorldDebugPanel` dùng IMGUI `OnGUI`, chỉ compile trong Editor và Development Build; không đưa debug buttons vào Release Build. Để debug buttons nhận click/chạm trong Play Mode, đặt Active Input Handling = **Both**. Với Release đã bỏ debug panel, có thể dùng **Input System Package (New)**.
- Đồng bộ giờ/thời tiết giữa host-client chưa triển khai ở giai đoạn này; sẽ giao cho server authority tại Giai đoạn 19.

## Kiểm thử nghiệm thu

1. Chạy `Đảo Sương → Phase 2 → 3 - Validate Open Scene` trước Play.
2. Play: Player xuất hiện trên bãi biển, đứng trên mặt đất, không rơi xuyên terrain.
3. Chạy bằng joystick; xoay camera bằng ngón tay còn lại. Trong Editor có WASD/arrow + Space.
4. Quan sát vùng cát, rừng cây, núi đá/xuất hiện tuyết, đầm lầy thấp màu xanh sẫm và ba cửa hang.
5. Mở bảng debug: thử 06:00, 12:00, 18:00, 00:00; thử sương mù, mưa, bão và trời quang.
6. Vào đầm lầy khi trời quang, chờ sương tăng; chạy ra ngoài, xác nhận sương giảm.
7. Bấm **Sinh lại cùng seed**, so `checksum` trong Console; sau đó bấm **Đảo mới** và xác nhận checksum thường đổi.
8. Game View → Stats: kiểm tra triangles dưới 100k, batches dưới 300. Xem Frame Debugger để xác nhận các lệnh `Render Mesh (instanced)`.
9. Build Development lên Android thật: kiểm tra đa chạm, 30 FPS mục tiêu, mưa và camera. Ghi lại model máy, GPU API, resolution và FPS median/1% low.

### Ước tính geometry mặc định

- Địa hình: 18,432 triangles.
- Cây: tối đa khoảng 400 × 40 = 16,000 triangles.
- Đá: tối đa khoảng 80 × 12 = 960 triangles.
- Ba cửa hang: khoảng 72 triangles.
- Nước: 2 triangles.
- Tổng phần thế giới sinh procedural: khoảng **35,466 triangles** khi đủ số cây/đá, đã gồm mặt nước 2 tam giác, chưa tính Player, UI và hạt mưa. Đây là ước tính từ topology code, không phải số đo Game View/thiết bị.

## Lỗi thường gặp

- **Shader hồng:** xác nhận URP Asset đã gán trong Graphics và Quality; kiểm tra Console shader compile; kiểm tra `Shader.Find`/Material.
- **Không có M_Terrain/M_Water/M_Rain:** chạy lại menu Create World Assets; xem shader đã import chưa.
- **Đảo di chuyển theo camera:** kiểm tra đang dùng bản WaterSurface mới; chỉ `WaterPlane_FollowCamera` được di chuyển.
- **Spawn không trên bãi biển:** đảm bảo `World` Position `(0,0,0)`, Rotation `(0,0,0)`, Scale `(1,1,1)`; kiểm tra `IslandSettings` không bị chỉnh `beachMaxHeight` quá thấp.
- **Không có cây/đá:** kiểm tra settings maxTrees/maxRocks và Console; xác nhận biome có Forest/Mountain.
- **Ít hơn ba hang:** xem cảnh báo `[Island] Chỉ tìm thấy...`; khôi phục settings mặc định hoặc tăng `mountainRadius` nhẹ.
- **Mưa không thấy:** xác nhận `M_Rain` dùng `Universal Render Pipeline/Particles/Unlit` và được gán trong WeatherSystem.
- **Instancing exception:** kiểm tra `SystemInfo.supportsInstancing`, Enable GPU Instancing trên M_Terrain và shader variant; code sẽ tự fallback sang MeshRenderer khi thiết bị không hỗ trợ.
- **Địa hình đen:** kiểm tra Directional Light đã gán vào DayNightCycle; bấm Trưa 12:00.
- **FPS tụt:** kiểm tra Game View Stats/Profiler; giảm maxTrees trước, sau đó maxRocks. Kiểm thử thiết bị thật; Editor Stats không đại diện FPS Android/iOS.

## Nguồn Unity

- Unity 6.3: tạo URP Asset và gán Graphics/Quality: https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/universal-render-pipeline/urp-quality-settings/urp-asset-create
- Graphics.RenderMeshInstanced API: https://docs.unity.cn/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html
- GPU Instancing và SRP Batcher: https://docs.unity.cn/6000.3/Documentation/Manual/GPUInstancing.html
