# Phase Log — Interactive 3D Campus Navigation System

Running record of every implementation session with Claude Code. Newest entry on top. This is separate from `PROJECT_KNOWLEDGE_BASE.md` (which holds *decisions*) — this file holds *what was actually built and verified*, session by session, so progress is auditable and nothing gets re-done or forgotten.

---

## Phase 1 — Unity Project Scaffold (Issue #1)
**Date:** 2026-09-14
**Status:** ✅ Complete, committed pending push

**Built:**
- Folder structure under `Assets/`: `Scenes/`, `Scripts/Controllers/`, `Scripts/Managers/`, `Prefabs/`, `Materials/`, `Models/`, `Animations/`, `Audio/`, `Firebase/` (empty folders hold `.gitkeep`)
- 3 scenes created and added to Build Settings in order: `AvatarSelect.unity`, `Exterior.unity`, `Floor1.unity`
- `Exterior.unity`: `GroundPlane` GameObject added, scaled to 100×100 units
- WebGL build target set active; test build to `UnityProject/Builds/WebGL-test/` — **succeeded**, 0 errors, 1 benign warning, 5.2 MB output

**Deviation from plan:**
- **Unity version changed from 2022 LTS to Unity 6000.6.0f1** — 2022 LTS wasn't installable on the dev machine; Unity 6 is also LTS-designated, so accepted as a lateral swap. Updated in `PROJECT_KNOWLEDGE_BASE.md` Section 2 and in `CLAUDE.md`.

**Known gaps (not blockers, just not done yet):**
- `Exterior.unity` has no Main Camera or Directional Light — scene will appear black until added (planned alongside the joystick/camera-follow work in Issue #7).

**Verification performed:**
- Confirmed live via Unity Editor connection: folders, scene registration, GroundPlane transform/components, Build Settings order.
- WebGL test build completed successfully.

**Not yet done:**
- Git commit shown for review, then push to `origin main`.

**Next planned task:** Issue #7 (Virtual Joystick prototype) and/or Issue #6 (exterior Blender blockout, parallel track for Theertha/Anandhu).

---

## Phase 1.1 — Git Commit & Push (Issue #1 follow-up)
**Date:** 2026-09-14
**Status:** ✅ Complete — pushed to `main` (`9ad630f..6aae9ea`)

**Corrected before push:**
- Root `.gitignore` lacked effective Unity ignore rules — `Library/` (336 MB), `Temp/`, `Logs/`, `Builds/`, `UserSettings/` would have been staged. Fixed; verified only `Assets/`, `Packages/`, `ProjectSettings/` are tracked.
- Removed a broken leftover `.git` directory inside `UnityProject/` (debris from an earlier stalled Unity Hub attempt).

**Now on `main`:** `.gitignore` (corrected), `CLAUDE.md`, `docs/PHASE_LOG.md`, `docs/PROJECT_KNOWLEDGE_BASE.md`, `UnityProject/Assets/`, `Packages/manifest.json` + lock file, `UnityProject/ProjectSettings/`.

**Recommended follow-up (not blocking):** have a second team member do a fresh clone to confirm the corrected `.gitignore` produces a clean working checkout.

**Next planned task:** Issue #7 (Virtual Joystick prototype).

---

## Phase 2 — Input Layer: Joystick + WASD Fallback + Movement (Issue #7)
**Date:** 2026-09-14
**Status:** ✅ Built and functionally verified — **not yet committed/pushed**

**Built:**
- `VirtualJoystick.cs` — on-screen touch joystick (Canvas/EventSystem, background + handle), drag → normalized direction vector
- `PlayerMovementInput.cs` — shared input source so joystick and WASD/arrow-key fallback both feed the same movement pipeline (per NFR 3.2.2 and our explicit decision to support both)
- `AvatarMovementController.cs` — consumes the shared input, moves the placeholder capsule
- `Exterior.unity` — added Main Camera (static, no follow yet) and Directional Light (scene no longer renders black); placeholder capsule avatar in place of the real Mixamo model

**Verification performed:**
- Simulated joystick drag traced through the full chain: direction vector correctly clamped to magnitude 1, movement speed and gravity both confirmed correct (avatar walked off the unbounded 100×100 plane and fell — expected, since NavMesh/collision (FR-3) is intentionally not implemented yet, pending Issue #6 geometry)
- No compile or runtime errors across the play-mode test session
- **Fresh WebGL rebuild after adding the input layer — succeeded, 0 errors**

**Known gaps (not blockers, tracked for later issues):**
- Camera has no follow behavior yet (`CampusCameraController` per SDD 5.2) — was paused awaiting a scope call, decided below.
- No NavMesh, no POI system, no Firebase integration, no real avatar model, no Blender assets yet.
- `AvatarSelect` and `Floor1` scenes remain empty placeholders.
- Android build untried — WebGL is the only verified target so far (consistent with WebGL-primary decision).

**Not yet done:**
- Commit and push today's input-layer work.

**Next planned task:** Commit/push Phase 2, then `CampusCameraController` (camera-follow), run in parallel with kicking off Issue #6 (exterior Blender blockout) for Theertha/Anandhu.

---

<!-- Add new entries above this line, newest first. -->
