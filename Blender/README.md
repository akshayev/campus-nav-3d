# /Blender

This folder holds **Blender source files** (`.blend`) and the raw modeling inputs
they're built from — kept separate from `UnityProject/Assets/` because Unity only
ever needs the exported `.fbx`, not the editable Blender project behind it.

## What's in here right now
- `CUCEK_MainBuilding_Blockout.obj` + `.mtl` — a rough 5-part greybox of the main
  B.Tech building (main block, ground portico, atrium, roof cornice, rooftop mast),
  generated from team reference photos. **Dimensions are estimates, not measurements**
  — see `docs/PROJECT_KNOWLEDGE_BASE.md` Section 3 (Issue #4 covers doing a real
  measurement pass later; when that happens, this blockout gets corrected, not
  replaced from scratch).

## What should end up here next
- `CUCEK_MainBuilding.blend` — the Blender project once the OBJ is imported and
  cleaned up (see `docs/BLENDER_GUI_GUIDE_MainBuilding.md` for the step-by-step).

The finished export target is `UnityProject/Assets/Models/CUCEK_MainBuilding.fbx`
— that's the only file Unity actually loads.
