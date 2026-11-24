# Quality gates — Phase 0 và các mốc sau

## Phase 0 (nền tảng)

- Project mở bằng Unity 6000.3.29f1.
- Package Manager resolve `manifest.json` không lỗi.
- Tất cả asmdef JSON hợp lệ; tên duy nhất; mọi tham chiếu assembly đều tồn tại.
- Không có compile error trong Console sau import.
- Editor validator chạy; bootstrap scene tạo được và vào Play Mode.
- Git có commit đầu tiên; `.gitignore`/`.gitattributes` hiện diện.

## Các quality gates cho vertical slice

- Domain unit tests cho inventory, crafting, damage, survival.
- Host/client integration tests: 2, 3, 4 clients; sau đó 8 clients ở stress test.
- Network conditions: 50/100/150 ms RTT, jitter, packet loss và reconnect.
- Inventory/crafting: spam, replay request, duplicate request, race conditions.
- Voice: permission denied/granted, PTT, mute, device unplug, call interruption, background/foreground, leave room.
- Mobile: nhiệt sau 20–30 phút, pin, RAM, tải CPU/GPU, low-end Android physical devices.
- Crash-free run và log không lộ token/PII.

## Performance targets (initial budgets, phải profiling)

- Target 30 FPS; không tuyên bố đạt trước khi đo trên danh sách máy chuẩn.
- 100k visible triangles là mốc khởi đầu, không phải giới hạn universal của GPU.
- RAM < 1.5 GB là mục tiêu cần tách memory budget theo Android/iOS và texture quality.
- Initial install < 300 MB; remote content có CDN/cache/versioning.
- Network 20 Hz initial; tăng 30 Hz chỉ nếu kết quả đo chứng minh cần thiết.
