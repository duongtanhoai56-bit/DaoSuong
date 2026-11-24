# ĐẢO SƯƠNG — COMPLETE GAME PROJECT (Phase 0–9)

Unity 6.3 LTS (`6000.3.29f1`) · URP · Android/iOS · Mobile-first survival · 99-day campaign · source hotfix `0.9.1-hotfix`.

## What is included

This is the cumulative source project for the earlier Phase 0–9 work. Use this archive as the single source of truth; do not overlay the older phase ZIPs on top of it.

- Phase 0–1: project structure, architecture, input and mobile player controls.
- Phase 2: procedural low-poly island, terrain, water, day/night, weather and playable base scene generator.
- Phase 3: gathering, chopping, mining, fishing, item drops and action HUD.
- Phase 4: inventory slots, stack/weight rules, containers and transfer UI.
- Phase 5: crafting recipes, workbench and forge.
- Phase 6: health, hunger, thirst, stamina, temperature, consumables and survival HUD.
- Phase 7: optional Unity Authentication/Multiplayer Services/Relay, network avatar and Vivox group voice.
- Phase 8: six islands, seven creatures, six bosses, 99-day win condition, combat, base building, storage and warmth.
- Phase 9: six multi-room underground caves, hidden branches, guardians, loot, keys, six story threads, journal and mobile Safe Area/touch support.
- Cumulative EditMode test suite and audit/setup documents.
- Hotfix 0.9.1: fixed fresh-run reset for built bases/chest persistence, zero-count cave treasure spawning, strengthened full setup stop conditions and whole-Assets assembly/meta audit.

## Create the game in Unity

1. Install Unity Hub + Editor `6000.3.29f1`. During installation select Android Build Support (SDK, NDK, OpenJDK) for Android builds.
2. Open this folder (`Assets`, `Packages`, `ProjectSettings`) as a Unity project and wait for Package Manager to resolve.
3. Follow `docs/26_COMPLETE_GAME_SETUP.md` to create and wire generated Unity assets. The project includes an integrated Editor wizard:
   - `Đảo Sương → 00 - Trình tạo game tổng hợp (Phase 0–9)`
   - Use the ordered steps or the full offline setup button.
4. Create and assign a URP Pipeline Asset as described in `docs/03_PROJECT_SETUP.md`.
5. Run the Phase 2–9 generation steps in order; the main playable scene is `Assets/_Game/Scenes/World/SCN_IslandWorld.unity`.
6. Run the wizard validators and `Window → General → Test Runner → EditMode → Run All`. The hotfix includes regression tests for new-run base/chest reset and persistence clearing; the full suite must be executed in Unity to confirm results.
7. Enter Play Mode, confirm there are no Console errors, then create an Android Development APK and test on a real device.

## Important distinction: source vs generated game assets

The ZIP contains source scripts, packages, Editor tools and docs. It does **not** contain a pre-generated `.unity` scene, `.prefab`, `.asset` ScriptableObjects or APK. Unity Editor must generate them through the included tools. This environment has no Unity Editor/C# compiler/Android SDK, so it cannot honestly certify a successful compile, Test Runner pass, Play Mode session or APK build.

## Online status

Phase 7 can create invite-code sessions, Relay transport, network avatars and group voice. It is a network/voice vertical slice, **not** complete synchronized co-op survival. Health, inventory, gathering, crafting, boss/AI, cave loot, base construction and campaign progression are not yet server-authoritative or replicated. Configure Unity Dashboard before enabling Phase 7. Do not advertise full co-op gameplay until those systems are implemented and tested on separate devices.

## Android target

- Default orientation: Landscape (set by Phase 9 Editor upgrade).
- Touch controls, Safe Area and multi-touch are included in source/tools.
- Use ARM64 and a Development Build for initial testing.
- Performance target is 30 FPS on baseline devices, but FPS, RAM, thermal behavior and battery use require physical-device profiling.

## Main docs

- `docs/26_COMPLETE_GAME_SETUP.md` — integrated setup and build.
- `docs/27_COMPLETE_PROJECT_AUDIT.md` — merge rationale, current static checks and known limitations.
- `docs/28_HOTFIX1_REAUDIT.md` — latest defect review, fixes, scan results and unverified release gates.
- `docs/03_PROJECT_SETUP.md` — URP and Input System setup.
- `docs/18_PHASE7_SETUP_AND_TEST.md` — optional online setup.
- `docs/21_PHASE8_SETUP_AND_TEST.md` — 99-day campaign.
- `docs/24_PHASE9_SETUP_AND_TEST.md` — caves/mobile acceptance.

## Hotfix 0.9.1 — re-audit and fixes

- A fresh campaign run now clears placed base structures, their chest saves and orphaned chest keys reconstructed from the saved building list.
- Cleared chests cannot reload or resave old contents during app pause/quit; a new persistence configuration can start cleanly.
- Empty cave loot rows no longer create an unusable `x0` pickup; lore-only secrets remain interactable.
- The all-phases setup wizard stops on a failed step or an incomplete Phase 2 scene. Its audit checks the full `Assets` tree, GUID/meta pairing, internal assembly references, assembly cycles and enabled Build Settings scenes.
- Static scan: 122 C# files, 5 asmdefs, 180 test attributes declared, 239 unique Assets GUIDs, zero missing/orphan/duplicate metadata, zero internal assembly reference/cycle findings, and zero lexical delimiter/preprocessor findings.
- These are source/static checks only. Unity compilation, Test Runner, Play Mode, Android build and physical-device behavior have not been executed in this environment.
