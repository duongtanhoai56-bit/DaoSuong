# Checklist nghiệm thu Phase 0

- [x] GDD ngắn và tên làm việc.
- [x] Must / Should / Could.
- [x] Definition of Vertical Slice và acceptance criteria.
- [x] Project root có `ProjectVersion.txt` và `Packages/manifest.json`.
- [x] Cấu trúc thư mục theo feature.
- [x] Assembly Definitions và dependency graph.
- [x] Editor menu validator + tạo Bootstrap Scene.
- [x] `.gitignore` và `.gitattributes`.
- [x] Git init + initial commit trong gói bàn giao.
- [ ] Mở bằng Unity Editor 6000.3.29f1 trên máy người dùng.
- [ ] Package Manager resolve thành công lần đầu; commit `packages-lock.json`.
- [ ] URP asset được tạo/gán trong Graphics và Quality.
- [ ] Chạy validator và xác nhận Console 0 compile errors.
- [ ] Tạo bootstrap scene, Play Mode chạy.

**Trạng thái bàn giao:** các mục có thể kiểm tra tĩnh đã hoàn thành. Các mục cần Unity Editor thực tế chưa thể xác nhận trong môi trường tạo gói vì không có Unity Editor/Android toolchain cài sẵn.


## Phase 1 amendment — assembly structure

Phase 1 consolidated the previous fine-grained assemblies into `Game.Core`, `Game.Gameplay` and `Game.UI`, following Unity asmdef folder scope. Deferred networking/social code lives under `Scripts/Network` without an asmdef until the online phase. See `08_PHASE1_PLAYER_CONTROLS.md`.
