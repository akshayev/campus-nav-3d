# Interactive 3D Campus Navigation System

Multimedia Mini Project (23-204-0713) — B.Tech IT, Cochin University College of Engineering Kuttanad (CUCEK)

A Unity-based first-person exploration app for CUCEK's main B.Tech building and the adjoining MCA block. Users pick a male/female avatar, walk around using a virtual joystick, and get info popups for exterior facilities (bike shed, car shed, canteen, badminton court) and interior rooms/departments floor by floor. Building/POI data is served from Firebase.

## Team
- Akshay EV (20423508) — primary developer
- Mohamed Hashir C (20423540) — primary developer
- Theertha Manoj (20423553) — 3D modeling / support
- Anandhu MV (20423514) — 3D modeling / support
- Mohammed Hamil N (20423541) — docs / testing / support

## Project status & decisions
**Read `docs/PROJECT_KNOWLEDGE_BASE.md` first.** It's the living source of truth for every scope, tooling, and timeline decision made for this project. If something isn't written there, treat it as undecided.

Key documents (in `docs/`):
- `PROJECT_KNOWLEDGE_BASE.md` — living decision log (start here)
- `SRS_Interactive_3D_Campus_Navigation.docx` — Increment 1, Software Requirements Specification (Rev. 2)
- `SDD_STD_Interactive_3D_Campus_Navigation.docx` — Increment 2, Design + Test documentation (Rev. 2)

## Tech stack
- **Engine:** Unity 2022 LTS (WebGL primary build target, Android secondary/best-effort)
- **3D modeling:** Blender (free tier only)
- **Character rigging/animation:** Mixamo (free tier only)
- **Backend:** Firebase Firestore (free Spark plan)
- **Version control:** Git + Git LFS (binary assets)

## Getting started (first-time setup)

1. **Install Unity Hub**, then install **Unity 2022 LTS** with these modules:
   - WebGL Build Support
   - Android Build Support (SDK & NDK Tools, OpenJDK) — secondary target
2. **Install Git LFS**: https://git-lfs.com, then run `git lfs install` once per machine.
3. **Clone the repo:**
   ```
   git clone https://github.com/akshayev/campus-nav-3d.git
   cd campus-nav-3d
   ```
4. **Open the project** in Unity Hub by pointing it at this folder (once the Unity project scaffold exists — see the first setup issue on the board).
5. **Never commit** `google-services.json` / Firebase credentials — see `.gitignore`.

## Repo structure (planned)
```
/docs/                  Project documents (SRS, SDD/STD, knowledge base)
/UnityProject/          The actual Unity project (Assets, ProjectSettings, Packages)
/reference/             Reference photos, floor sketches, campus notes
README.md
.gitignore
.gitattributes
```

## Branching convention
- `main` — always buildable/demoable
- `feature/<short-name>` — one branch per task (e.g. `feature/joystick-movement`)
- Open a Pull Request into `main` when a feature is working; at least one other team member reviews before merge.

## Task tracking
All work is tracked via **GitHub Issues** and the repo's **Projects** board. See open issues for current tasks.
