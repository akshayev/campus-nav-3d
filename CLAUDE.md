# CLAUDE.md — Interactive 3D Campus Navigation System

This file is read automatically by Claude Code at the start of every session in this repo. Keep it up to date as decisions change — if something here conflicts with `docs/PROJECT_KNOWLEDGE_BASE.md`, the knowledge base wins; update this file to match.

## What this project is
A Unity WebGL (primary) / Android (secondary) app letting a user walk a 3D avatar around CUCEK's main B.Tech building + MCA block, exterior and interior, with Firebase-backed info popups for rooms/facilities. Full context: `docs/PROJECT_KNOWLEDGE_BASE.md`, `docs/SRS_Interactive_3D_Campus_Navigation.docx`, `docs/SDD_STD_Interactive_3D_Campus_Navigation.docx`.

## CRITICAL: team skill level
**Akshay and Hashir (primary developers) have never used Unity or C# before this project. Theertha and Anandhu (3D modeling) have never used Blender before.** This changes how you should work:
- Do not silently generate large blocks of code. Explain what each script does and why, in plain terms, as you write it.
- Prefer small, working increments over large speculative builds. After adding a feature, say clearly how to test it in the Unity Editor before moving on.
- When something requires a Unity Editor GUI action (attaching a script to a GameObject, baking a NavMesh, setting up a Prefab), say so explicitly and explain the click-path — don't assume it happened just because the script exists.
- Favor well-documented, common Unity patterns over clever/unusual ones. Boring and readable beats elegant here.

## Tech stack & hard constraints
- **Engine:** Unity 6 LTS (6000.6.0f1 installed). Do not suggest or require a different version.
- **Build targets:** WebGL is primary and must always work. Android is secondary/best-effort — don't let Android-specific work block WebGL progress.
- **Backend:** Firebase Firestore, free Spark plan. No paid Firebase features.
- **Budget: zero.** Never suggest a paid Unity Asset Store package, paid plugin, or paid service. Free/open-source only (Mixamo free characters, Unity Asset Store free tier, free Blender).
- **Version control:** Git + Git LFS. Any new binary asset type (models, textures, audio, video) must be added to `.gitattributes` LFS tracking, not committed raw.
  - **Exception:** the Mixamo character `.fbx` files in `Assets/Models/Characters/` and their extracted `.png` textures in `Assets/Models/Characters/Textures/` are deliberately gitignored (too big for the free LFS quota) and shared via drive; only `.meta` files and the small `.mat` files are committed. Don't "fix" this by committing them. See knowledge base Section 4.
- Never commit Firebase credentials (`google-services.json`, service account keys, etc.) — these must stay out of git per `.gitignore`.

## Current scope (see docs/PROJECT_KNOWLEDGE_BASE.md Section 3 for full detail)
- Single site: main B.Tech building (5 storeys) + MCA block. NOT the full campus.
- Exterior POIs: main building, MCA block, bike shed, car shed, canteen, indoor badminton court.
- Interior: full walkable floors, room-to-room. **One floor is guaranteed fully detailed** (candidate: IT department floor); remaining floors are a stretch goal — implement the floor system so adding more floors later is just "bake NavMesh + add Firestore docs," not a redesign.
- No official campus map or floor plans exist — layout data comes from team sketches/measurements in `/reference`.

## Architecture (see SDD for full detail)
- Three-layer: Presentation (Unity UI/rendering) → Application (C# controllers) → Data (Firebase).
- Scenes: `AvatarSelect` → `Exterior` (always loaded) → one additively-loaded sub-scene per floor (`Floor1`, `Floor2`, ...), loaded/unloaded on stairs/lift trigger. Never load all floors simultaneously — this exists specifically to keep WebGL build size and memory sane.
- Firestore collections: `exteriorPOIs`, `floors`, `floors/{floorId}/rooms`. Exact fields are in the SDD Section 3.
- Core scripts (see SDD 5.2 for responsibilities): `AvatarSelectorController`, `VirtualJoystick`, `AvatarMovementController`, `FloorTransitionManager`, `POITriggerVolume`, `POIManager`, `FirebaseDataManager`, `CampusCameraController`, `AudioManager`.

## Repo structure
```
/docs/           Project documents (SRS, SDD/STD, knowledge base) — read these before big decisions
/UnityProject/   The actual Unity project (Assets, ProjectSettings, Packages)
/reference/      Reference photos, hand-sketched floor plans, measurement notes
```

## Working conventions
- `docs/PHASE_LOG.md` tracks what was actually built/verified, session by session (newest entry first) — read it at the start of a session to see what's already done, and append a new entry after completing a task so nothing gets re-done or forgotten. It's a build log, not a decision record — decisions still go in `docs/PROJECT_KNOWLEDGE_BASE.md`.
- Branch per feature: `feature/<short-name>`, PR into `main`, at least one teammate reviews before merge.
- `main` should always build for WebGL without errors — don't leave it broken between sessions.
- Task tracking is via GitHub Issues/Projects, not this file — check open issues for current priorities.
- When you finish a task, state plainly what was built, how to test it in-editor, and what the next logical task is — this team is relying on you to sequence the work, not just execute isolated requests.

## Unity CLI / batch mode
Prefer using Unity's batch-mode CLI for repeatable checks where possible, e.g.:
```
Unity -batchmode -quit -projectPath ./UnityProject -executeMethod BuildScript.BuildWebGL -logFile build.log
```
Use this to verify a WebGL build still succeeds after changes, rather than assuming. Check `build.log` for compile errors before reporting a task as done.

## Explicitly out of scope — do not implement
- Outdoor GPS navigation to the physical campus
- Multiplayer/social/networked features
- Voice interaction
- Custom-modeled (non-Mixamo) characters
- Any building/block beyond the main building + MCA block
