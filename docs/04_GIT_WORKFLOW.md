# Git workflow

## Commit đầu tiên

Repository này đã được khởi tạo với branch `main` và commit bootstrap đầu tiên. Xác nhận bằng `git log -1 --oneline`.

## Quy tắc

- Commit nhỏ, một mục tiêu; ví dụ `feat(inventory): add stack rules`.
- Branch: `main` ổn định; `feature/<system>-<task>` cho công việc mới; `fix/<issue>` cho lỗi.
- Không commit `Library`, builds, generated IDE files, secret, keystore, provisioning profile.
- Commit tất cả `.meta`, scene, prefab, ScriptableObject và `Packages/manifest.json` + `packages-lock.json`.
- Git LFS dùng cho FBX/Blend/PSD/audio/video và binary nặng; cài Git LFS trước khi thêm các file đó.

## Máy phát triển

```bash
git lfs install
git status
git log -1 --oneline
```

Sau khi Unity resolve package lần đầu:

```bash
git add Packages/manifest.json Packages/packages-lock.json ProjectSettings Assets
git commit -m "chore(unity): lock resolved packages"
```

Không force-push `main`; không squash các thay đổi mà nhóm đã pull nếu chưa thống nhất.
