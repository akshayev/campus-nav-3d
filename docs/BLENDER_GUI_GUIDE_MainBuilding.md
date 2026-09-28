# Blender GUI Guide — Main Building Blockout

**For:** Theertha & Anandhu (first time in Blender — every step below explains the
concept, not just the click)
**Goal:** turn `Blender/CUCEK_MainBuilding_Blockout.obj` into
`UnityProject/Assets/Models/CUCEK_MainBuilding.fbx`, upright and correctly scaled
in Unity.

Why you're doing this manually: Blender's command-line tool (which would let
Claude Code do this automatically) isn't installed on this machine, so this has
to be done by hand in the Blender app. That's completely normal — most Blender
work happens in the GUI anyway.

Do the steps **in order**. Save your work (Ctrl+S / Cmd+S) after each numbered
section, not just at the end — if Blender crashes mid-way you don't want to redo
everything.

---

## 0. Install Blender (if you haven't)

Free download: https://www.blender.org/download/ — get the current stable
version (4.x). No paid version exists; anything asking for money is not
official Blender.

---

## 1. What you're starting with

The model is a **greybox** — 5 simple boxes standing in for the real building,
built from team reference photos, not real measurements yet:

| Object name | What it represents | Rough size (meters) |
|---|---|---|
| `MainBlock` | The 5-storey main building volume | 44 wide × 16 deep × 20 tall |
| `GroundPortico` | The covered walkway/arches at ground level, in front | 40 wide × 3 deep × 4.5 tall |
| `Atrium` | The curved glass stairwell tower | 6 wide × 4 deep × 23.5 tall |
| `AtriumMast` | Small rooftop mast/antenna on top of the atrium | ~1.2 × 1.2 × 2 tall |
| `RoofCornice` | The tiled roof overhang/cornice on top of MainBlock | 45.5 wide × 17.5 deep × 0.8 thick |

Each already has a material name attached (`facade_pink`, `atrium_glass`,
`roof_maroon`, `trim_white`, `mast_grey`) — you'll give these actual colors in
step 6.

**Everything here is a placeholder for scale/shape only.** Don't spend time
making it pretty — the whole point of a "blockout"/"greybox" is that it's rough
on purpose, so the team can start testing movement and camera collision against
*something* while the real measurements (Issue #4) are still being collected.

---

## 2. Import the OBJ and set up units

1. Open Blender. You'll see a default scene with a cube, a light, and a camera.
2. Select all three of those (drag a box around them, or press `A`) and press
   `X` → **Delete** to clear the scene. We don't need Blender's defaults.
3. Go to **File → Import → Wavefront (.obj)**.
4. Navigate to and select `Blender/CUCEK_MainBuilding_Blockout.obj` (in the repo
   root, not inside `UnityProject`).
5. Before clicking Import, check the import options panel (bottom-left of the
   file browser):
   - **Forward Axis:** `-Y`
   - **Up Axis:** `Z`
   (These are Blender's defaults for Wavefront OBJ, and match what this file was
   exported with — the file's header comment says "Z-up, meters", which is
   Blender's own native coordinate system, so no axis correction is needed at
   this stage. The Y-up conversion only matters later, at FBX export, because
   that's when we're handing the model to Unity, which uses Y-up.)
6. Click **Import**. You should see 5 grey boxes appear, roughly forming a
   building shape (a long low block with a tower on one side).
7. **Set scene units to metric:** open the **Scene Properties** tab (the
   printer-and-cone icon in the Properties panel, right side). Under **Units**,
   set **Unit System** to `Metric` and **Unit Scale** to `1.0`. This matters
   because the SDD assumes "1 Blender/Unity unit = 1 meter" everywhere — if this
   is wrong, the building will import into Unity at the wrong scale relative to
   the avatar and the 100×100 ground plane.
8. Save as a new file: **File → Save As** → save it inside the `Blender/` folder
   as `CUCEK_MainBuilding.blend`. From now on, save this file, not the OBJ.

**Checkpoint:** you should have 5 objects in the Outliner (top-right panel):
`MainBlock`, `GroundPortico`, `Atrium`, `AtriumMast`, `RoofCornice`.

---

## 3. Clean up the geometry

Two beginner Blender concepts you need here:

- **Normals** are little arrows, one per face, that say "this side of the face
  is the outside." If a normal points the wrong way, that face looks invisible
  or black from outside and only visible from inside — a very common import
  glitch. "Recalculate normals" tells Blender to look at the whole mesh and
  point every normal outward automatically.
- **Merging vertices by distance** fuses points that are sitting on top of each
  other (or extremely close) into one point. When boxes were built separately
  and then placed next to each other, their corners often don't perfectly
  share a vertex even if they look touching — this cleans that up and prevents
  tiny gaps/seams from showing later.

Do this for **each of the 5 objects, one at a time**:

1. Click the object in the Outliner (or in the 3D viewport) to select it.
2. Press **Tab** to enter **Edit Mode** (this lets you edit the mesh's points/
   faces, instead of moving the whole object). You'll know you're in Edit Mode
   because the header at the top-left of the viewport says "Edit Mode" and you
   can see orange vertex dots.
3. Press `A` to select all vertices in this object.
4. **Merge duplicates:** press `M` → choose **By Distance**. Blender will pop up
   a small message like "Removed 0 vertices" (or some small number) — either is
   fine, it just means it found few or no duplicates for that shape.
5. **Recalculate normals:** press `Shift+N`. Nothing will visibly change if
   normals were already correct — that's expected and fine.
6. Press **Tab** again to return to **Object Mode**.
7. Repeat for the next object.

**Checkpoint:** switch the viewport shading to **Solid** with **Face Orientation**
overlay on (top-right of viewport, the overlay dropdown → Face Orientation) —
every face should look blue (outward-facing). Red faces mean a normal is still
flipped; select that object, Edit Mode, `A`, `Shift+N` again.

---

## 4. Arch openings on GroundPortico

**What an "inset" and a "loop cut" are**, since you'll use both here:

- **Inset Faces** (`I`) shrinks a selected face inward, creating a smaller face
  surrounded by a thin border — like a picture frame. Useful for carving a
  recessed opening into a wall.
- A **Loop Cut** (`Ctrl+R`) slices a new edge loop across a mesh, giving you more
  faces to work with without manually adding vertices one by one.

Goal: a simple row of rectangular arch-like openings along `GroundPortico`'s
front face (the long face facing outward, away from `MainBlock`). Keep this
low-poly — rectangles standing in for arches, not actual curved arch geometry.
That level of detail can come later once this is confirmed to look right at a
distance in-game.

1. Select `GroundPortico`, Tab into Edit Mode.
2. Switch to **Face Select** mode (press `3`, or click the face icon at the
   top-left of the viewport).
3. Click the long front-facing face (the 40m-wide face).
4. Use **Ctrl+R**, hover over the face until you see a vertical preview line,
   scroll your mouse wheel to increase the number of cuts to **9** (this splits
   the face into 10 even vertical strips), left-click to confirm, then
   right-click (or press Esc) to cancel the slide so the cuts stay evenly
   spaced.
5. Switch back to Face Select (`3`). You now have 10 face strips. Select every
   **other** strip (click one, then Shift-click alternating ones) — these
   become your openings, roughly 5 openings evenly spaced.
6. With those faces selected, press **I** for Inset, move the mouse slightly to
   shrink the selection inward (leaves a visible frame/pillar around each
   opening), left-click to confirm.
7. With the smaller inset faces still selected, press **E** for Extrude, then
   type `-0.3` and Enter to push them slightly inward (creating a shallow
   recess look, since we're not modeling a full walk-through arcade at
   blockout level).

**Checkpoint:** the front of `GroundPortico` should show a row of shallow
rectangular recesses, evenly spaced, standing in for arches.

---

## 5. Window recesses on MainBlock

Same Inset+Extrude technique, applied to `MainBlock`'s front and two side
faces, across 5 rows (one per floor — `MainBlock` is 20m tall ÷ 5 floors = 4m
per floor).

1. Select `MainBlock`, Tab into Edit Mode, Face Select (`3`).
2. Select the front face. Use **Ctrl+R** twice:
   - First loop cut: horizontal, count = **4** (splits the face into 5 even
     horizontal floor-bands).
   - Second loop cut: vertical, count = however many window bays looks
     reasonable for a 44m-wide face — **7** is a reasonable starting point
     (8 columns). Adjust later if it looks too dense/sparse next to the
     avatar's scale.
3. This grid of faces is now your per-floor, per-bay window grid. Select all
   the faces in this grid (Face Select, box-select across the whole face).
4. **Inset** (`I`), shrink slightly so each window face has a visible frame/
   mullion around it.
5. **Extrude** (`E`), type `-0.2`, Enter — shallow recess, same idea as the
   portico arches.
6. Repeat for the two visible side faces of `MainBlock` (left and right ends).
   The back face (facing away from the courtyard/camera) can be left plain —
   not worth the modeling time at blockout stage.

**Checkpoint:** `MainBlock` should show a visible 5-row grid of shallow window
recesses on the faces the player will actually see and walk past.

---

## 6. Materials — set the actual colors

Each object already has a material *name* attached from the OBJ import
(`facade_pink`, `atrium_glass`, `roof_maroon`, `trim_white`, `mast_grey`), but
OBJ import in Blender sometimes brings materials in flat grey until you set
the actual color. Do this for each of the 5 materials:

1. Select the object that uses it (e.g. `MainBlock` for `facade_pink`).
2. Open the **Material Properties** tab (the checkered sphere icon in
   Properties panel).
3. Click the material name to select it. Under **Base Color**, click the color
   swatch and enter these values (these are the exact colors from the
   original reference-photo material file, converted to standard hex):

   | Material | Base Color (hex) |
   |---|---|
   | `facade_pink` | `#DBA39E` |
   | `atrium_glass` | `#4D9E9E` |
   | `roof_maroon` | `#5C2924` |
   | `trim_white` | `#EDE6D9` |
   | `mast_grey` | `#8C8C8C` |

4. Leave **Roughness** and **Metallic** at their defaults — this is a
   blockout, not final-detail material work (real textures/PBR materials are a
   later task, not part of this pass).
5. Switch the viewport shading mode to **Material Preview** (the shaded sphere
   icon, top-right of the viewport, next to Solid/Wireframe) to confirm the
   colors are showing correctly on each part.

**Checkpoint:** the model should now roughly resemble the real building's color
scheme — pink main facade, teal atrium glass, dark maroon roof, white trim/
pillars, grey mast — even though the shapes are still blocky.

---

## 7. Export to FBX for Unity

This is the step most likely to go wrong for beginners, because **Blender is
Z-up and Unity is Y-up** — if the axis settings are wrong on export, the whole
building will appear to Unity lying on its side or facing the wrong way. Don't
just accept Blender's default FBX export settings without checking these two
fields.

1. First, **apply transforms** on all 5 objects: select all (`A` in Object
   Mode), then **Object → Apply → All Transforms**. This "bakes in" any
   rotation/scale so the FBX exports clean numbers instead of carrying hidden
   transform data — a common source of "why is my scale doubled in Unity"
   bugs.
2. With all 5 objects still selected, **File → Export → FBX (.fbx)**.
3. Navigate to `UnityProject/Assets/Models/` (inside the Unity project, not the
   `Blender/` folder) and name the file `CUCEK_MainBuilding.fbx`.
4. In the export options panel (bottom-left), check/set:
   - **Forward:** `-Z`
   - **Up:** `Y`
   - Under **Geometry**, leave smoothing/normals on their defaults.
   - Make sure **Selected Objects Only** is checked (so you don't accidentally
     export anything else from the scene later).
5. Click **Export FBX**.

**Checkpoint:** `UnityProject/Assets/Models/CUCEK_MainBuilding.fbx` should now
exist (check the file's there and has a non-zero size).

---

## 8. Verify in Unity

1. Open the Unity project, switch to the `Exterior` scene.
2. Unity should auto-detect the new FBX in `Assets/Models/` and generate a
   `.meta` file for it (you'll see it appear in the Project window, possibly
   after a few seconds while it imports).
3. Drag `CUCEK_MainBuilding` from the Project window into the scene Hierarchy.
4. In the **Inspector**, set its Transform **Position** to `(0, 0, 0)` if it
   isn't already there.
5. Check, with the Scene view:
   - **Upright?** The building should stand vertically, not lying on its side
     or rotated 90°. If it's on its side, the FBX export axis settings in
     step 7 were wrong — re-export with Forward `-Z` / Up `Y` exactly as
     listed above.
   - **Scale sane against the ground plane?** `GroundPlane` is 100×100 units
     (meters). `MainBlock` is ~44m wide — it should look like a large building
     comfortably sitting within the plane, not a speck or something that
     overflows the plane entirely. If it looks roughly 10× too big or too
     small, the Scene Units setting from step 2 (Metric, Unit Scale 1.0) was
     probably not applied before modeling/export — check that setting in
     Blender and re-export rather than trying to fix it by scaling in Unity
     (scaling in Unity instead of fixing it at the source causes problems
     later with physics/collision sizing).
   - **At the origin, not offset?** All 5 parts should look like one connected
     building, not floating apart from each other — if a piece is offset,
     that object's transform likely wasn't at the world origin before you
     applied transforms in step 7; re-check that object's position in Blender.

Write down whatever you actually observe here (upright or not, size relative
to the ground plane, any part that's out of place) and report it back —
**don't just say "looks fine"** if something looks even slightly off, since
this greybox is what the camera-collision and future NavMesh work will be
tested against next.
