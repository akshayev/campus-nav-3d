# Phase Log — Interactive 3D Campus Navigation System

Running record of every implementation session with Claude Code. Newest entry on top. This is separate from `PROJECT_KNOWLEDGE_BASE.md` (which holds *decisions*) — this file holds *what was actually built and verified*, session by session, so progress is auditable and nothing gets re-done or forgotten.

---

## Phase 8 — Firebase/Firestore Integration for Exterior POIs
**Date:** 2026-10-10
**Status:** ✅ Complete, build-verified in a real browser, committed and pushed (`ee9e4c1` on `feature/firebase-poi-integration`)

**Built:**
- Firebase project `cucek-campus-nav` created (Firestore in `asia-south1`/Mumbai, production mode), security rules published as public-read / no client write (`allow read: if true; allow write: if false;`), web app registered.
- Decided against the official Firebase Unity SDK: it's built on native code and can't compile to WebAssembly, so it has no WebGL support, and WebGL is this project's primary target per CLAUDE.md.
- `FirebaseDataManager.cs`: fetches the `exteriorPOIs` Firestore collection over plain HTTPS using the Firestore REST API + `UnityWebRequest` (not the SDK). Parses Firestore's field-wrapper JSON (`{"stringValue": "..."}`) with the Newtonsoft Json package (added as an explicit `manifest.json` dependency). The public Firebase web API key is stored directly as an Inspector default — this is fine, these keys are meant to be public; security comes from the Firestore rules, not from hiding the key.
- `POIManager.ReplacePOIList()`: only swaps in Firestore data on a confirmed, non-empty fetch — a failed fetch, no internet, or an empty collection all just leave the Phase 7 hardcoded POI list in place untouched. This is what makes the hardcoded list a real offline fallback rather than a placeholder that breaks the moment Firebase is involved.
- All six `exteriorPOIs` documents now exist in Firestore (`main_building`, `mca_block`, `bike_shed`, `car_shed`, `canteen`, `badminton_court`), each with `id`/`displayName`/`description` string fields, added via the Firebase Console (no code change needed for this).

**Verification performed:**
- Editor test against live Firestore: all six POI popups showed Firestore-sourced title/description text, Console logged `FirebaseDataManager: loaded 6 POI(s) from Firestore.`, zero errors.
- Batch-mode WebGL build succeeded (same benign licensing-telemetry "error" line as every other batch build, unrelated to the build itself).
- Real browser test of the WebGL build served locally over HTTP (not `file://`): hit and fixed two unrelated local-server bugs along the way (missing `Content-Encoding: gzip` header for Unity's gzip-compressed build output, and a stale-cache issue where the browser kept reusing a 304-cached broken response after the header was fixed — solved with a `Cache-Control: no-store` header). Once fixed: app loads past the progress bar, all six POI popups show Firestore-sourced text, no CORS errors in DevTools.

**Known gaps / flagged, not acted on (user's call):**
- Firebase project currently has no teammates added (personal Gmail account only).
- The local test server script (`UnityProject/Builds/WebGL/serve_gzip.py`) lives inside the gitignored `Builds/` folder and will be deleted the next time someone builds — worth moving to a persistent, tracked location (e.g. a `/tools` folder) if the team wants to keep reusing it for local WebGL testing.

**Next planned task:** pick between whichever finishes first — the Blender exterior blockout, or starting on the floor/interior system (`floors`, `floors/{floorId}/rooms` collections).

---

## Phase 7 — Exterior Boundary Walls + POI Trigger Popups (Hardcoded Data)
**Date:** 2026-10-04 to 2026-10-10
**Status:** ✅ Complete, build-verified, committed and pushed (`f27c596`, PR #3 on `feature/exterior-poi-boundary`)

**Built:**
- Four invisible solid Box Colliders (`Wall_North/South/East/West`, children of a `Boundaries` empty) placed 1 unit past each edge of the 100×100 `GroundPlane`. No script — pure Editor setup. The avatar can no longer walk off the edge and fall (previously-documented known gap from Phase 2, now closed).
- `POITriggerVolume.cs`: sits on a trigger Box Collider, holds a `poiId` string, calls `POIManager.Instance.ShowPOI`/`HidePOI` on Player enter/exit. Only reacts to objects tagged `Player`.
- `POIData.cs` / `POIManager.cs`: six hardcoded POIs (main building, MCA block, bike shed, car shed, canteen, indoor badminton court) as a `List<POIData>` singleton, same `Instance` pattern as `PlayerMovementInput`. Chose a plain `[Serializable]` class over a ScriptableObject since one Inspector list is simpler than six asset files for this data size, and it maps directly onto a future Firestore document read without needing `POITriggerVolume` or the scene's trigger objects to change.
- `POICanvas` / `POIPopupPanel`: a dismissible popup (title, description, close button) anchored top-center, clear of the joystick's bottom-left footprint (per SRS usability requirement).
- Six `POI_*` GameObjects placed around the ground plane (inside the new boundary), each a trigger volume plus a child `Cube` placeholder marker — no real building geometry exists yet (Blender exterior blockout track, tracked separately, still in progress as of this phase).
- `Player` GameObject tagged `Player` (was `Untagged` since it was first created in Phase 2) — required for any trigger volume to fire.

**Bug found and fixed during this phase:**
- My own instructions for the popup's title/description text Y positions assumed the text elements would anchor to the panel's top edge, but Unity's default anchor for new UI text is the parent's center. This made the description text overhang 40px past the bottom of the panel. Caught via the user's manual test; fixed by recalculating both Y values for a center-anchored layout (title 85, description -10) rather than changing the anchor itself. Confirmed by computing both elements' vertical spans against the panel's bounds directly from the saved scene file, rather than re-trusting a re-test.

**Verification performed:**
- Scene file parsed directly (not just taken on the tester's word) after each step: boundary wall positions/sizes/IsTrigger, POI trigger positions/sizes/poiId strings, POIManager's three Inspector field references, cube marker parenting and absence of duplicate colliders — all confirmed to match spec exactly.
- Manual in-Editor test (reported by user, consistent with the file-level checks): boundary walls stop the avatar at all four edges; all six POIs show the correct popup on entry and hide it on exit; close button works; no red Console errors.
- Batch-mode WebGL build: `Build Finished, Result: Success`, 0 real compile errors (the one "error" BuildReport counts is the same benign `[Licensing::Module]` telemetry log line seen in every batch build this project, unrelated to the build itself).

**Known gaps (not blockers, carried over):**
- POI positions are placeholders on bare ground — no real building geometry yet (separate Blender track).
- POI data is hardcoded in C#, not yet backed by Firestore (deliberately out of scope for this task).
- Unity version mismatch (6000.3.24f1 on disk vs 6000.6.0f1 in CLAUDE.md) still unresolved.
- Generated IDE files (`*.csproj`, `.slnx`) are gitignored now (fixed in PR #2) but Unity still regenerates untracked local copies each session — harmless, just noise in `git status`.

**Next planned task:** depends on which finishes first — Theertha/Anandhu completing the Blender exterior blockout (after which the placeholder cube markers get replaced with real geometry and POI positions get corrected to match), or starting the Firebase/Firestore integration to replace the hardcoded POI list.

---

## Phase 6 — Sync with origin/main (Hashir's Phases 4–5 merged locally)
**Date:** 2026-09-28
**Status:** ✅ Complete (no code changed)

**Done:** stashed local PHASE_LOG edits, fast-forwarded 35f34e9..eb59040 (8 commits by Hashirc + merge eb59040 by Akshay EV), reapplied stash cleanly, no conflict markers. Renamed local "Phase 4/4.1" to "Blender Track B1/B1.1" to avoid number collision.

**Verified (by reading):** AvatarSpawner.cs, AvatarSelectorController.cs, BuildScript.cs contain real logic. AvatarSelect scene populated (Canvas, AvatarSelector, EventSystem). Exterior has Player (renamed from AvatarPlaceholder). Floor1 still empty. Build order unchanged. No credentials tracked.

**Not verified:** WebGL build on this machine; avatar spawning (men.fbx/women.fbx/textures absent locally, shared via team drive).

**Open decisions:** Unity version (ProjectVersion.txt = 6000.3.24f1 vs CLAUDE.md = 6000.6.0f1); AGENTS.md drift from CLAUDE.md; gitignore generated .csproj/.slnx; obtain character binaries from drive.

---

## Blender Track B1.1 — Cleanup: stray folder removal + guide filename check
**Date:** 2026-09-25
**Status:** ✅ Complete

**1. Deleted `~/Desktop/multimedia` entirely.** Confirmed gone (`ls` on the path
now returns "No such file or directory"). This was the non-git stray folder
that caused the file-location mixups in Phase 3 (loose stale doc copies) and
Phase 4 (OBJ/MTL not actually in the repo). Before deleting, found one file in
there that hadn't been rescued yet — `files (3)/preview_angle2.png`, a rendered
preview of the greybox massing from a second angle — copied it into `Blender/`
so it isn't lost. Everything else in that folder (stale README/CLAUDE.md/docs
copies) was superseded by the real files already in this repo, so nothing else
needed saving.

**2. Checked `docs/BLENDER_GUI_GUIDE_MainBuilding.md` Step 1 against `Blender/`'s
actual contents.** Guide references `Blender/CUCEK_MainBuilding_Blockout.obj`;
actual file in `Blender/` is `CUCEK_MainBuilding_Blockout.obj` (confirmed via
directory listing) — **exact match, no fix needed.** `Blender/` now also
contains `CUCEK_MainBuilding_Blockout.mtl`, `README.md`, and the rescued
`preview_angle2.png`, none of which conflict with anything the guide names.

**Next planned task:** unchanged — Theertha/Anandhu work through the guide.

---

## Blender Track B1 — Main Building Blockout: Blender Setup (Issue #6, in progress)
**Date:** 2026-09-25
**Status:** ⚠️ Partially complete — file staged, manual-GUI path chosen, actual modeling not yet done

**Path taken:** Checked for Blender CLI first, per instructions — `blender --version` returned `command not found`; Blender is not installed on this machine. The CLI-automated path (import/cleanup/export scripted end-to-end) was therefore **not possible**. Fell back to writing a numbered, beginner-level manual GUI guide for Theertha and Anandhu instead of attempting to fake or approximate the Blender steps.

**Also found while starting this task:** the source `CUCEK_MainBuilding_Blockout.obj`/`.mtl` files referenced in the task were not actually in the repo yet (not in `UnityProject/Assets/Models/`, not in `/reference`) — they were sitting in `~/Downloads/files (4)/` and a stray `~/Desktop/multimedia/files (3)/` copy. Copied both into a new `Blender/` folder at the repo root (kept separate from `UnityProject/Assets`, since Unity only needs the final FBX).

**Built:**
- `Blender/` folder created at repo root, with `Blender/README.md` explaining its purpose (raw Blender sources, not Unity assets).
- `Blender/CUCEK_MainBuilding_Blockout.obj` + `.mtl` — the 5-part greybox (`MainBlock`, `GroundPortico`, `Atrium`, `AtriumMast`, `RoofCornice`), confirmed by inspecting the OBJ directly: units meters, Z-up, origin at footprint center/ground level, with per-object bounding boxes matching the "rough box massing" description (e.g. `MainBlock` ≈ 44×16×20 m, `RoofCornice` ≈ 45.5×17.5×0.8 m). Material colors extracted from the `.mtl` and converted to hex for the guide (`facade_pink #DBA39E`, `atrium_glass #4D9E9E`, `roof_maroon #5C2924`, `trim_white #EDE6D9`, `mast_grey #8C8C8C`).
- `docs/BLENDER_GUI_GUIDE_MainBuilding.md` — step-by-step manual instructions covering: OBJ import + metric units setup, normal recalculation + merge-by-distance cleanup, inset/loop-cut arch openings on `GroundPortico`, inset/loop-cut window-recess grid on `MainBlock` (5 rows, one per floor), applying the real material colors, and — the step most likely to bite beginners — FBX export with the Blender-Z-up → Unity-Y-up axis settings (`Forward: -Z`, `Up: Y`) spelled out explicitly, plus an Apply-Transforms step beforehand to avoid hidden-scale bugs.
- No LFS/gitattributes changes needed for `.obj`, `.blend`, and `.fbx` — already covered by the existing `.gitattributes` rules. `.mtl` is plain text (483 bytes) and is not LFS-tracked; this is intentional and harmless at this size.

**Not yet done (this is the actual state — nothing below has been verified, only prepared):**
- The OBJ has **not** been imported into Blender by a human yet — no `.blend` file exists.
- No arch openings, no window recesses, no material colors have actually been applied — the guide describes how to do this, it hasn't been executed.
- `UnityProject/Assets/Models/CUCEK_MainBuilding.fbx` **does not exist yet** — so the Unity re-import/scale/orientation verification (step 8 of the guide) has **not** been performed. No dimension/scale/orientation issues can be reported yet because that check hasn't happened.
- Building dimensions in the source OBJ remain estimates from reference photos, not real measurements (Issue #4, still open) — flagged again in the new guide so this doesn't get forgotten once modeling starts.

**Next planned task:** Theertha/Anandhu work through `docs/BLENDER_GUI_GUIDE_MainBuilding.md` steps 0–8 and report back what they actually observe at the Unity re-import checkpoint (upright? correctly scaled against the 100×100 `GroundPlane`? at the origin?) — that report becomes the next PHASE_LOG entry, written from their actual findings, not assumed.

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
**Status:** ✅ Complete, build-verified, and committed (`dc8384a`, `646aa55`)

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

## Phase 5 — Mixamo Avatars, Animator, Avatar Select Screen (Issue: avatar/animation sourcing)
**Date:** 2026-09-27 to 2026-09-28
**Status:** ✅ Complete, build-verified, committed (`646aa55`, `ecb7d21`, `be28820`, `57f7d5f`)

**Built:**
- Downloaded Mixamo `Y Bot`/`X Bot`-equivalent characters (`men.fbx` = Ch08, `women.fbx` = Ch26, With Skin) and shared Idle/Walking/Running clips (Without Skin, In Place), imported as Humanoid.
- `AvatarAnimator.controller`: shared Animator Controller — Idle/Walk/Run states driven by a float `Speed` (thresholds 0.1 / 2.5), no exit-time waits, 0.15s blends.
- `AvatarMovementController.cs`: added `SetAnimator()` + per-frame `animator.SetFloat("Speed", CurrentSpeed)`. Movement/gravity/rotation logic otherwise unchanged.
- `AvatarSelectorController.cs` + `AvatarSelect.unity`: Male/Female buttons save the choice via `PlayerPrefs` and load `Exterior`.
- `AvatarSpawner.cs`: reads the saved choice at scene start, instantiates the matching Humanoid prefab (`MaleAvatar`/`FemaleAvatar`, Prefab Variants of `men`/`women`) as a child of `Player`, hides the placeholder capsule's `MeshRenderer`, disables root motion, hands the model's `Animator` to `AvatarMovementController`.
- Extracted Mixamo's embedded materials/textures (`Extract Textures...` / `Extract Materials...`) — fixes a plain-white-material import quirk.
- Imported TMP Essentials (needed for the Avatar Select buttons' text).
- Added `Assets/Editor/BuildScript.cs` (`BuildScript.BuildWebGL`) so the batch-mode command in CLAUDE.md actually works — it didn't exist before this phase.

**Bugs found and fixed during this phase:**
- **Movement freeze/drift:** `AvatarMovementController` was reading `cameraTransform.forward/right` live every frame for camera-relative movement. Since `CampusCameraController` re-aims at the avatar every frame (`LookAt`) and its position lags via `SmoothDamp`, this created a feedback loop (avatar steers by camera → camera re-aims at avatar → basis changes → avatar steers differently) that made movement drift and stop responding correctly after sustained input. Fixed by snapshotting the camera's facing **once**, in `Awake()`, since this camera has a fixed offset and never orbits under player control.
- **Four nonexistent packages in `manifest.json`** (`com.unity.pipeline`, `com.unity.modules.physicscore2d`, `.tetgen`, `.timelinefoundation`) had been silently broken since the very first scaffold commit (Phase 1) — surfaced as "Project has invalid dependencies" the first time the Editor tried a fresh package resolve this phase. Removed; see commit `646aa55`.
- **`Assets/Scripts/` got dragged inside `Assets/Scenes/`** at some point during manual Editor testing (an easy Project-window drag-and-drop slip). Moved back at the filesystem level with Unity closed — `.meta` GUIDs were unaffected, confirmed unchanged before/after.
- Two stray `Animator` components + auto-generated controllers got added to `GroundPlane` and `JoystickCanvas` from an accidental clip-onto-object drag; removed.

**Decisions made this phase (see `PROJECT_KNOWLEDGE_BASE.md` Section 4 for full reasoning):**
- Character `.fbx` files (143MB combined) and their extracted textures (~132MB) are **gitignored** and shared via team drive instead of Git LFS, to stay inside GitHub's free 1GB/month LFS quota. Only `.meta` files and the small `.mat` files are committed, so GUIDs stay consistent across the team once the shared binaries are dropped into the same paths.

**Verification performed:**
- Batch-mode WebGL build: `Build Finished, Result: Success`, 0 real errors (one "error" reported by BuildReport is a benign `[Licensing::Module]` telemetry log line, unrelated to the build).
- Manually tested in Editor: movement in all directions for an extended period without freezing/drift; both Male and Female avatars spawn correctly from Avatar Select; Idle/Walk/Run animations switch correctly; characters are fully textured (not white/pink).
- Confirmed avatar height matches the old placeholder capsule.

**Known gaps (not blockers):**
- Falling off the edge of the unbounded 100×100 `GroundPlane` still happens (no NavMesh/boundary yet — tied to Issue #6, not this phase). At sustained fall speed the camera's smooth-follow catches up and matches the avatar's velocity, which can *look* like a freeze even though both are actually in freefall — worth knowing if it comes up again during testing.
- Unity version mismatch is still unresolved: this phase was built/verified on 6000.3.24f1, but CLAUDE.md specifies 6000.6.0f1. Needs a team decision before more version-file churn happens.
- Generated IDE files (`*.csproj`, `.slnx`) are still tracked in git since `0773af1`; should be gitignored.

**Next planned task:** Issue #6 (exterior Blender blockout) — real geometry is needed before NavMesh/movement boundaries and meaningful camera-collision testing can happen.

---

<!-- Add new entries above this line, newest first. -->
