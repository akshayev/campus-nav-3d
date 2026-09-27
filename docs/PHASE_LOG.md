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
**Status:** ✅ Complete — pushed to `main` (`6aae9ea..6034336`)

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
- ~~Commit and push today's input-layer work.~~ Done — see Phase 3.

**Next planned task:** `CampusCameraController` (camera-follow) — see Phase 3.

---

## Phase 3 — CampusCameraController: Smooth Third-Person Follow (SDD 5.2)
**Date:** 2026-09-14
**Status:** ✅ Complete and functionally verified — pending commit/push

**Built:**
- `CampusCameraController.cs` — attached to Main Camera; follows `AvatarPlaceholder` using `Vector3.SmoothDamp` (eased position tracking, not rigid-lock) plus a raycast-based collision check that pulls the camera in front of any obstacle between it and the target (currently only the ground plane; will matter more once building geometry exists)
- Added a `Player` layer and moved `AvatarPlaceholder` onto it, so the camera's own collision raycast can never mistake the avatar's body for an obstacle
- Bug fix in `AvatarMovementController.cs`: gravity accumulation had no terminal velocity cap. Found this via testing (see below) — added `maxFallSpeed` (20 units/sec) as a general robustness fix, independent of the specific incident that surfaced it.

**Verification performed:**
- Confirmed smooth-follow is real (not rigid-lock): mid-movement, camera position measurably lagged the ideal `target + offset` position by ~0.23 units rather than matching it exactly.
- Confirmed collision avoidance: forced the desired camera position through the ground plane (offset pointed straight down) — corrected camera Y landed just above the surface (~0.30) instead of clipping through to the uncorrected ~-8.92.
- Fresh WebGL rebuild after the change — succeeded, 0 errors.

**Anomaly encountered and resolved:**
- During an early test (holding simulated joystick input across several real-world seconds between tool calls, combined with the then-uncapped gravity), `GroundPlane`'s transform drifted from `(0,0,0)` to `(1.5,-3.4,0)` and did **not** revert when Play mode was stopped — unusual, since Play-mode-only changes normally discard automatically. Leading theory: PhysX numerical instability from an extreme, uncapped fall velocity (the avatar's fall speed was unbounded and reached an extreme value during that test) corrupting unrelated collider state; not fully root-caused. Manually reset `GroundPlane` to `(0,0,0)`, added the terminal velocity cap, and re-ran a bounded version of the same test — this time everything reverted correctly on Stop, as expected. Flagging in case this resurfaces; if it does, it's a strong signal to look at physics step size / substepping settings rather than gameplay script logic.

**Known gaps (not blockers):**
- Camera has no obstacle-avoidance test against real building geometry yet — only the ground plane exists, so the collision check is unexercised against walls/corners until Issue #6 blockout geometry lands.
- Camera doesn't yet rotate to track the avatar's heading — it holds a fixed world-space offset behind/above the origin area, which is simpler and avoids swinging wildly if the avatar turns quickly. Revisit if playtesting says it needs to.

**Not yet done:**
- Commit and push Phase 3.

**Next planned task:** Issue #6 (exterior Blender blockout) — needed before the collision-avoidance logic can be meaningfully tested against real geometry, and before NavMesh/movement boundaries can be added.

---

## Phase 4 — Movement/Input/Camera Spec Alignment
**Date:** 2026-09-27
**Status:** ✅ Complete and build-verified — not yet committed

**Built / changed:**
- `Exterior.unity`: avatar capsule renamed `AvatarPlaceholder` → `Player` (still on the `Player` layer, at (0,1,0), with CharacterController).
- `AvatarMovementController.cs`: movement is now **camera-relative** (flattened camera forward/right), uses `Camera.main` unless a camera is assigned; new public `CurrentSpeed` property (horizontal units/sec) for the future Animator. No animation code.
- `VirtualJoystick.cs`: public output renamed `InputDirection` → `Direction`.
- `PlayerMovementInput.cs`: joystick + keyboard now converge in one method, `ReadMoveInput()` (joystick wins when active, else WASD/arrows).
- New `Assets/Editor/BuildScript.cs` with `BuildScript.BuildWebGL` — the batch-mode command in CLAUDE.md now actually works. Output: `UnityProject/Builds/WebGL/` (gitignored).

**Deliberate deviations from the task spec:**
- Kept `PlayerMovementInput` as the single input convergence point instead of having `AvatarMovementController` read the joystick directly — same outcome, no scene rewiring.
- Kept camera collision handling from Phase 3 even though the spec said "no camera collision handling". Remove if the team prefers.

**Verification performed:**
- Batch-mode WebGL build: `Build Finished, Result: Success`, 0 errors, exit code 0.

**Open issue — needs a team decision:**
- Commit `0773af1` saved the project with **Unity 6000.3.24f1**, but CLAUDE.md specifies 6000.6.0f1. Building with 6000.6.0f1 upgraded `ProjectVersion.txt` and `packages-lock.json`. Everyone must agree on one editor version before committing.
- Generated IDE files (`*.csproj`, `*.slnx`) are tracked in git since `0773af1`; they should be gitignored.

**Next planned task:** Issue #6 (exterior Blender blockout) — real geometry is needed to test movement boundaries/NavMesh and camera collision.

---

<!-- Add new entries above this line, newest first. -->
