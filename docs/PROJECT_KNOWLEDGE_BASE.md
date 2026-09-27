# Project Knowledge Base — Interactive 3D Campus Navigation System

**Course:** 23-204-0713 Mini Project (Multimedia Project), CUSAT B.Tech IT
**Institution:** Cochin University College of Engineering Kuttanad (CUCEK)
**Last updated:** 2026-09-14

> This is a **living document**. Every confirmed decision goes here. If it's not written down here, treat it as undecided — do not assume. Re-upload/reference this file at the start of every new session so nothing gets lost.

---

## 1. Team

| Member | ID | Role |
|---|---|---|
| Akshay EV | 20423508 | Primary developer (Unity/C#) |
| Mohamed Hashir C | 20423540 | Primary developer (Unity/C#) |
| Theertha Manoj | 20423553 | Support — likely 3D modeling (Blender) |
| Anandhu MV | 20423514 | Support — likely 3D modeling (Blender) |
| Mohammed Hamil N | 20423541 | Support — docs/testing (tentative) |

- Exact role split beyond Akshay/Hashir as primary devs is **not fixed** — assigned per-task as work items come up.
- **Skill levels (confirmed):**
  - Akshay & Hashir: **complete beginners** in Unity and C#.
  - Theertha & Anandhu: **complete beginners** in Blender.
- Implication: plan must include learning ramp-up, not assume prior experience. Claude Code sessions should explain code, not just generate it silently.

---

## 2. Development Environment

- **OS:** Mixed across team members (not uniform) → Git-based workflow is mandatory, avoid OS-specific paths/tools where possible.
- **Primary dev machine (Akshay/Hashir):** mid-range, dedicated GPU, decent RAM — sufficient for Unity + Blender.
- **Unity:** installed. **Version locked: Unity 6 LTS (6000.6.0f1)** — switched from the originally planned 2022 LTS because only 6000.6.0f1 was available/installable on the primary dev machine; Unity 6 tutorial/community support is now mature enough to not be a significant beginner-experience regression. Need Unity Hub + WebGL Build Support + Android Build Support modules.
- **Blender:** not installed yet (assumed, given Theertha/Anandhu are beginners).
- **Version control:** GitHub. **Repo created: [github.com/akshayev/campus-nav-3d](https://github.com/akshayev/campus-nav-3d)** (assumed public — confirm/change in repo settings if private is preferred).
  - `.gitignore`, `.gitattributes` (Git LFS config), and `README.md` drafted — see `/repo-setup` deliverables.
  - Git LFS tracks binary assets (models, textures, audio); `.cs`/`.unity`/`.prefab`/`.mat`/`.meta` stay plain-text/diffable.
  - Branching convention: `main` always buildable; `feature/<name>` branches; PR + review before merge.
  - All 5 team members already have GitHub accounts — add as collaborators once repo exists.
- **Task tracking:** GitHub Issues/Projects (integrated with the repo). Initial Week 1 issue list drafted — see `WEEK1_ISSUES.md`.
- **Firebase:** not set up yet. Need to create project, enable Firestore, set security rules (public read, admin-only write).

---

## 3. Scope — MAJOR REVISION from original SRS

The original SRS (Increment 1) assumed a whole-campus navigation experience. **This has been revised** based on real reference material (a photo of the actual main building) and team input:

### In scope
- **Single-site focus:** the main B.Tech building (5-storey, pink facade, central curved glass stairwell atrium, tiled roof cornice — confirmed via photo) **and the MCA block beside it**.
- **Not** the full 42-acre campus.
- **Exterior POIs:** Main building, MCA block, bike shed, car shed, canteen, indoor badminton court.
- **Interior:** full walkable floors, room-to-room, across **all 5 floors** — this is the stated target.
  - **Risk-managed fallback (agreed approach):** guarantee **one fully detailed, fully walkable floor** (e.g. the IT department's floor) as the reliable deliverable; treat full detail on floors 2–5 as a stretch goal that expands as time allows, rather than an all-or-nothing requirement.
- **Audio:** basic ambient sound, footsteps, and UI click sounds are in core scope (satisfies course CO3).
- **Avatars:** free ready-made humanoid characters (Mixamo / Unity Asset Store), male & female, NOT custom-modeled from scratch.

### Data collection needed (team action items, not yet done)
- **No official campus map/site plan exists** — exterior building positions will be approximated from team knowledge/observation, not a formal survey.
- **No official interior floor plans exist** — each of the 5 floors needs to be walked through and sketched by hand (room names + approximate layout/dimensions + corridor and stair positions). This is a prerequisite for both Blender modeling and the Firebase `position` data.

### Explicitly out of scope
- Outdoor GPS navigation to the physical campus
- Multiplayer/social features
- Voice interaction
- Custom-modeled (non-Mixamo) characters
- Any paid assets (zero budget — free/open-source only)

---

## 4. Technical Decisions

- **Backend:** Firebase Firestore (not Realtime DB) — see SDD for schema (`buildings` collection).
- **Deployment priority:** **WebGL is primary** (easier live demo). Android APK is secondary/best-effort.
- **Budget:** **zero** — free assets and free-tier services only (Firebase Spark plan, free Unity Asset Store items, free Mixamo).
- **Character models are NOT in git** (decided 2026-09-27). The Mixamo avatars `men.fbx` (Ch08, 87 MB) and `women.fbx` (Ch26, 56 MB) together would eat most of GitHub's free Git LFS allowance (1 GB storage / 1 GB bandwidth per month) on the first few clones. So:
  - The `.fbx` files in `UnityProject/Assets/Models/Characters/` are gitignored and shared via a team shared drive instead.
  - Their `.meta` files **are** committed — they hold the Humanoid rig settings and the asset GUIDs that prefabs reference, so dropping the `.fbx` into that folder reconnects everything automatically.
  - Animation-only clips (`Assets/Models/Animations/*.fbx`, under 1 MB each) stay in git via LFS as normal.
  - Whoever runs a WebGL build must have both character files locally.

---

## 5. Timeline & Capacity

- **Target: complete within 1 month.**
- **Capacity:** Akshay + Hashir combined, ~20–35 hrs/week.
- ⚠️ **Flagged risk:** "full 5-floor walkable interior + full exterior + Firebase + WebGL/Android, built by a team with zero prior Unity/Blender experience, in 1 month" is aggressive. Mitigated via the fallback in Section 3 (guarantee 1 floor, stretch the rest) so there is always a complete, demoable project regardless of how the month goes.

### Proposed phased plan (draft, subject to change as work starts)
| Week | Focus |
|---|---|
| 1 | Tooling setup (Unity, Blender, Git, Firebase), repo + issue board created, Unity/Blender basics, exterior blockout |
| 2 | Exterior fully walkable: avatar select, joystick, NavMesh, POI popups for bike/car shed, canteen, badminton court |
| 3 | Interior: guaranteed 1 full floor walkable; expand to more floors if ahead of schedule |
| 4 | Firebase integration, WebGL (+ Android if time) builds, testing against STD, polish, report/demo prep |

---

## 6. Open Items / Not Yet Decided
- Final exact list/naming of interior rooms per floor (depends on team's walkthrough).
- Which floor is the "guaranteed fully detailed" floor (suggest: IT department floor, since that's the team's own department — but not yet confirmed).
- Whether Android build is attempted this cycle or deferred entirely.

---

## 7. Documents Produced So Far
1. `SRS_Interactive_3D_Campus_Navigation.docx` — **Revision 2**, updated to the single-building scope (main building + MCA block), interior floor/room FRs, guaranteed-floor fallback (Section 2.7), and audio requirement (FR-14). Up to date.
2. `SDD_STD_Interactive_3D_Campus_Navigation.docx` — **Revision 2**, updated Firebase schema (`exteriorPOIs`, `floors`, `floors/{id}/rooms`), per-floor additive sub-scene architecture, `FloorTransitionManager`/`AudioManager` components, and revised test cases. Up to date.
3. `README.md`, `.gitignore`, `.gitattributes`, `WEEK1_ISSUES.md` — repo scaffolding, drafted and ready to commit.

No outstanding revision action — both documents are current as of this update.

## 8. Repository & Tooling Setup
- **Repo:** github.com/akshayev/campus-nav-3d (assumed public)
- **Unity version:** 6 LTS (6000.6.0f1)
- **Planned repo structure:** `/docs` (documents), `/UnityProject` (Unity project), `/reference` (photos, floor sketches)
- **Week 1 issues drafted:** Unity scaffold, Blender onboarding, Firebase project creation, exterior reference/measurement pass, guaranteed-floor interior sketch, exterior blockout, joystick prototype, avatar selection screen, avatar/animation sourcing, NavMesh bake — see `WEEK1_ISSUES.md` for full text to paste into GitHub Issues.
