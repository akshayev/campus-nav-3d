# Phase Log — Interactive 3D Campus Navigation System

Running record of every implementation session with Claude Code. Newest entry on top. This is separate from `PROJECT_KNOWLEDGE_BASE.md` (which holds *decisions*) — this file holds *what was actually built and verified*, session by session, so progress is auditable and nothing gets re-done or forgotten.

---

### Character swap: Mixamo to Quaternius CC0 (2026-10-23)
Status: complete and committed locally on feature/free-characters (not pushed).
- Replaced men.fbx / women.fbx (Mixamo, gitignored, not on every machine) with Quaternius "Universal Base Characters (Standard)": Character_Male/Female.fbx + Hair_Male/Female.fbx (Humanoid) + Eyes/Body/Hair textures and materials. About 7.7 MB via Git LFS. Licence CC0 1.0 (see docs/ASSET_CREDITS.md).
- MaleAvatar/FemaleAvatar prefabs rebuilt (unpacked, about 2,733 lines each) on the new models with the same AvatarAnimator.controller; spawner references updated in Exterior.unity. No dangling GUIDs. Idle/Walking/Running clips still Humanoid.
- Removed 28 obsolete tracked files (old Ch08/Ch26 materials and .meta). .gitignore no longer ignores Characters/*.fbx and Textures/*.png.
- Verified: WebGL batch build Succeeded, 21.1 MB (1 splash-screen cache message, not a content error). Automated browser smoke test passed (see the Verified list above); human Unity Play-mode check pending before push.
- Risks: prefabs are unpacked and will conflict if two people edit them; Mixamo animation clips in a public repo (see KB Section 6); Unity version mismatch still unresolved (6000.3.24f1 in use vs 6000.6.0f1 in docs).
- Risks (found by the smoke test, NOT caused by the swap): BUG-001 avatar turns invisible when pressed against the main building front (Facade_Front slab z 3.47-4.47 has no collider; Body collider starts at z=4.0; not confirmed in the Editor). UX-001 AvatarSelect buttons are both labelled 'Button'.
### Repo audit and first WebGL build (2026-10-22)
- Read-only audit: main in sync with origin (00d26e2); 4 scenes in Build Settings; 18 scripts + BuildScript; Firestore via REST; no credential files tracked.
- First batch WebGL build on 6000.3.24f1: Succeeded, 0 compile errors (the logged "1 error" was an internal Unity API-updater cache message), 19 MB. That run deleted 18 tracked .meta files because the Mixamo files were missing; fixed by this swap.
- Not checked then: browser run, Firestore live data.

### Phase 22: ground floor rebuilt from the hand sketch (2026-10-14)
Source: `reference/ground_floor_cs_dept/sketch_floor_plan_upright.jpeg` (+ notes in `PHOTO_NOTES.md`). The team also said: add one faculty room and one classroom between the Gents toilet and the Exam Cell; the sketch is the ground floor only (Floor2 unchanged).
- **New Floor1 layout** (rooms on BOTH sides of a 7.3 m corridor, entrance vestibule in the middle of the south wall):
  - South row (z 4.2-9.2, next to the entrance): Principal (x -13.3..3), Examination Control G8 (3..17.5), [entrance vestibule 17.5..23, coral walls], Classroom 3 (23..35), G7 Lecture Hall (35..53.3).
  - North row (z 16.8-21.8): Office + Exam Cell as ONE room with two doors (x -13.3..18), stairs bay (18..23.5) with the Electrical Panel Room G3 door, Faculty Room-1 (23.5..30), Classroom 1 (30..36), Gents (36..45), Ladies (45..53.3).
- Rebuilt from scratch under `InteriorLayout`: `Shell` (walls, partitions, door frames), `Doors` (cloned opening doors, hinge flipped for south rooms), `Rooms_South`, `Rooms_North`, `Toilets_Corridor`, `Ceilings` (per-room ceiling colours, beams, fans, tubes). Room furniture follows the photos (Principal: maroon armchairs, red desk, blind, sofa; Office: counters, desks, olive cupboards; classrooms: desk rows facing the board; toilets tiled with fittings).
- Floor materials re-tiled for the new areas (`Mat_F1Hall`, `Mat_F1Lobby`, `Mat_F1Vestibule`).
- **Room popup triggers:** Firestore still holds the OLD positions (Firebase MCP is offline), so `FloorTransitionManager` got a TEMPORARY `groundFloorPlacements` list (world positions + radius per room id) that overrides Firestore for Floor 1. Delete it once Firestore is updated.
- **Firestore text now wrong:** the Principal popup says "right wing ... end of the main corridor", Exam Cell says "left wing" etc. Needs updating with the Firestore data.
- Verified in Play mode: walked in through the main door, into the Principal's room (popup + photo) and the merged Office/Exam Cell (popup). Camera sits close to the wall in these 5 m deep rooms. Not tested: other rooms, stairs to Floor2 from the new bay, WebGL build.

### Phase 21b: photos applied to the ground floor in Unity (2026-10-14)
- New RoomPhotos imported as Sprites (1024, no mipmaps).
- Floor1: Office + Exam Cell merged into one room (partition at x=13.5 removed, both doors kept), reception counters, desks/chairs/monitors, olive cupboards, whiteboard, third window, lilac ceiling + beam.
- Principal's room rebuilt: blue-grey ceiling, red-top desk under a zebra-blind window, 12 maroon armchairs, trophy cabinet, steel cupboard, List of Principals board, floral sofa/armchairs, coffee table, AC, curtains.
- Toilets tiled (violet band, pale tiles, light-blue floor), windows, 3 urinals with dividers (gents), cream stall doors.
- Corridor: pale pink walls, coral entrance hall, Placement notice board case, hanging PRINCIPAL and EXIT signs, water cooler, Electrical Panel Room G3 door.
- Ceiling detail: corridor ceiling panel + 13 cross beams, ceiling fans and fluorescent tubes in every room.
- Shapes are simple boxes. Walked in Play mode (principal, office, gents); not yet walked in Play mode; Floor2 untouched. Uncommitted.

## Phase 21 — New Photos Organised (Principal, Office/Exam Cell, Toilets, Corridors, Entrance Hall)
**Date:** 2026-10-13
**Status:** ⚠️ Files copied and documented only: NOT yet applied in Unity (the UnityMCP link was down), not committed

**Received (17 new photos in `project collage photos`, plus 1 exact duplicate skipped):** Principal's room inside (4 + ceiling), Office inside (3), Exam Cell door, 3 corridor shots, the entrance hall (main door), toilets front view and 3 inside views.

**Done:**
- All originals saved in `/reference/ground_floor_cs_dept/` with clear names, plus `PHOTO_NOTES.md` describing what they show (colours, layout, fittings).
- Popup photos added to `Assets/Resources/RoomPhotos/` using the existing gallery naming (`<roomId>`, `_2`, `_3`, ...): `room_principal` (door + 3 inside), `room_office` (door + 3), `room_exam_cell` (door + door view + 2 office inside views, since the Exam Cell is the second section of the same room), `room_gents_toilet` (5: door, front view, urinals, 2 inside), `room_ladies_toilet` (3: front view with the LADIES TOILET sign first, 2 inside; the old corridor shot stays only in `/reference`). Total photos in that folder: 29, 4.5 MB source.
- **Still to do in the Editor:** the new files import as plain textures; they must be set to Texture Type = Sprite, Max Size 1024 before the popups can show them (same one-line importer script as before).

**Findings that change the interior (to build next, once Unity is reachable):**
- Ceilings are not white everywhere: **Principal's room and Office are painted pale blue-grey / lavender**; the corridor is off-white/pale grey with square LED panels.
- Walls: corridor **pale pink**; entrance hall **coral/salmon**; Principal's room **cream**; Office peach-pink with a yellow-cream far end; toilets tiled (blue band, pale upper tiles).
- Floors: corridor/hall polished **red-brown granite with black inlay stripes**; Principal's room pale grey-cream tile; Office cream tile; toilets light-blue tile.
- **Office and Exam Cell are ONE room with two doors**: the partition between x=11 and x=16 (at x=13.5) should go, leaving a single large room (x 8.5..18.5) with both doors.
- Principal's room has rows of maroon wooden armchairs (not a sofa-only layout), a red-top desk under a zebra-blind window, a trophy cabinet, a wooden `LIST OF PRINCIPALS` board, floral sofa and armchairs, and a wall AC.
- Toilets: blue-band tiled walls, light-blue tile floor, cream stall doors, 2 basins, 3 urinals with dividers (gents), barred window.
- Corridor extras: wooden `PLACEMENT & TRAINING NOTICE BOARD` case, blue pin boards, hanging `PRINCIPAL / Dr. PREETHA MATHEW K` sign, hanging EXIT sign, water cooler at the end.

**Next planned task:** reconnect UnityMCP (Unity: Window → MCP for Unity → Start Server), import the new photos as sprites, then apply the findings above to both floors where they apply (ground floor first).

---

## Phase 20 — Visible Ceilings, Close Indoor Camera, Visible Front Wall (No More Invisible Walls), Ceiling Lights
**Date:** 2026-10-12
**Status:** ✅ Built and Play-mode verified, not committed

**Request:** a ceiling on the ground floor coloured like the "detailed photo", and a visible wall on the side opposite the rooms/classrooms instead of invisible walls.

**Built (both floors):**
- **Indoor camera:** a visible ceiling can't be seen from the old high camera (and would block it), so inside the building the camera now drops to a close, shoulder-height position (`indoorOffset` (0,1.5,-3.2), `indoorCollisionMask` Default + layer 2 so it can't pass through walls or furniture). Outdoors it stays high. `BuildingShell.SetInside` tells `CampusCameraController.SetIndoor`. A new on-screen **View: Close / Overhead** button (`ViewButton`, under Run) calls `ToggleOverheadView()` to switch to the high overhead view inside (ceiling not visible from there). Camera offset blends over 0.5 s for a smooth swoop through the door.
- **Ceiling colour** `Mat_Ceiling` = warm off-white (236,232,224) like the photo, now self-lit (emission) so its underside doesn't look dark grey; the one-sided ceiling quad (3.25 m up) from Phase 15 is unchanged.
- **Ceiling lights:** one-sided glowing panels (`Mat_CeilingLight`): a row along the hall (every 5 m), two in the lobby, one per room.
- **Visible front (south) wall** `Perimeter_South_West/East` in the hall-wall material, 3.2 m high, with 8 paired windows, a wall piece over the entrance and **solid glass entrance doors** (`Mat_GlassDoor`, dark frame) at x 18.5..21.5 (the way out is still the red exit marker). With the north/east/west perimeter walls from Phase 15, every outer wall is now visible.
- **Invisible outer wall colliders (`InteriorWalls`) deleted** from both floor scenes. The only hidden colliders left are the box colliders under desk units/teacher desks (they belong to visible furniture).

**Verified in Play mode:** entering drops the camera to (0,1.5,-3.2) in the avatar's frame; hall view down the corridor shows the cream ceiling with light panels, front-wall windows, doors and signs; the avatar is stopped by the south wall (z 4.8), the glass doors (z 4.7), and the west/east walls (x -12.7 / 52.7); the lobby leads north to the rooms; the View button toggles to "Overhead" and the high camera works.

- **Exit marker moved (follow-up):** the Floor1 exit marker (`StairsDown_ToExterior`) is now at (20, 1, 5.4), right inside the glass entrance doors, and smaller: trigger 1.8×3×1.8 m (was 3×3×3 m at z=8), red floor marker 1.6×1.6 m. Verified: walking up to the glass doors unloads Floor1, puts the avatar outside and sets the badge to "Outside".

**Known gaps:**
- The camera pulls close when there's a wall or furniture behind the avatar (by design); in tight rooms the view can be cramped. Overhead view shows the room layout better but the new front wall partly hides the hall from it.
- The ceiling emission is a lighting shortcut (no real lights in the building, the sun lights the interior).
- No ceiling beams, pipes, fans or AC units as in the photo; ceiling lights are flat glowing panels.
- The saved Sound setting shows "Sound: Off" (stored from earlier testing in PlayerPrefs `audioMuted`): click the button to turn it on.

**Next planned task:** try the indoor camera by hand and tune `indoorOffset` for comfort; faculty/office/exam room details from new photos.

---

## Phase 19 — Normal Walking: Real Walk Speed + Animation Matching, Run Toggle
**Date:** 2026-10-12
**Status:** ✅ Built and Play-mode verified (animator states checked), not committed

**Request:** "change the walk also, walk as the normal" (the team chose: better walk animation).

**Cause found:** the AvatarAnimator switches from Walking to **Running above Speed 2.5**, and the avatar moved at 4 m/s, so it was always playing the Running animation, sliding at the wrong pace.

**Changed (`AvatarMovementController`, `AudioManager`):**
- `moveSpeed` (4) replaced by `walkSpeed` = 2 m/s (Walking animation) and `runSpeed` = 4 m/s (Running animation). Run while **Shift** is held, or with the new on-screen **Run: Off/On** button (`RunButton` on `HUDCanvas`, under the Sound button, calls `ToggleRun()`), because touch screens have no Shift key. Partial joystick pushes walk slower (analog), full push = walk speed.
- Animation playback speed now follows real movement speed so feet match the ground: `animator.speed = CurrentSpeed / referenceSpeed` (`walkAnimationReferenceSpeed` 1.6 → 1.25× at 2 m/s; `runAnimationReferenceSpeed` 4 → 1.0×). If feet look like they slide, nudge those two Inspector numbers.
- **Turning on the spot** (TurnAndWalk) plays the Walking animation gently (`turnInPlaceAnimationRate` 0.8) instead of freezing in Idle.
- **Walking backward** plays the normal walk cycle at the slower backward pace (0.75×). Tried reversing the clip (negative playback), but Unity can't blend transitions at negative speed: the avatar froze in a half-blended Idle pose, so that was removed. No true backward-walk clip exists.
- Footstep sound spacing `stepDistance` 1.4 → 0.8 m (a footfall every ~0.4 s at walking speed).

**Verified in Play mode (simulated joystick, animator state read back):** standing → idle (rate 1.0); forward → walking, 2.0 m/s, rate 1.25; Run on → running, 4.0 m/s, rate 1.0; backward → walking, 1.2 m/s, rate 0.75; turning on the spot → walking, rate 0.8; released → idle. Root motion is off, so the animation doesn't double-move the avatar.

**Known gaps / notes:**
- Walking is now 2 m/s (was effectively 4): crossing the 67 m building takes ~34 s walking, ~17 s running. Tune `walkSpeed` / `runSpeed` by feel.
- Mixamo clips can't be fetched from here: a real turn-in-place or backward-walk animation would need to be downloaded from Mixamo by the team (free) and added to `AvatarAnimator`.
- Not judged by eye in motion (only screenshots and animator state); the stride/feet-slide tuning numbers are estimates.

**Next planned task:** try it by hand and tune `walkSpeed`, `walkAnimationReferenceSpeed` and `turnSpeed` by feel.

---

## Phase 18 — Camera Turns With the Character (Turn-and-Walk Controls)
**Date:** 2026-10-12
**Status:** ✅ Built and Play-mode verified, not committed

**Request:** the camera should turn with the character: turn left and the camera turns left, same for every other direction.

**Built:**
- `AvatarMovementController`: new **Control Mode** setting. **TurnAndWalk (new default):** left/right on the joystick or A/D turns the avatar on the spot (`turnSpeed` 120°/s), up/down or W/S walks forward/back along the way it faces (backward at 60% speed, `backwardSpeedFactor`). **CameraRelative:** the original behaviour (the pushed direction is the direction walked, camera never turns), kept so it can be switched back in the Inspector. In TurnAndWalk the "face the walking direction" line is skipped so walking backward doesn't flip the avatar.
- `CampusCameraController`: new **Rotate With Target** (on by default) and **Turn Follow Smooth Time** (0.25 s). The camera offset (0,5,-8) is now measured from the way the avatar faces, so the camera stays 8 units BEHIND it and eases round when it turns (`Mathf.SmoothDampAngle`). Turn it off to get the old fixed-direction camera.
- Why this doesn't recreate the Phase 5 "feedback loop" bug: movement no longer depends on the camera in TurnAndWalk (the avatar's own facing drives both movement and camera), so there is no steer-by-camera loop.

**Verified in Play mode (simulated joystick):** hold RIGHT for 1 s → avatar turned 139°, camera swung from 0° to 144° and settled directly behind at (0,5,-8) in the avatar's own space; hold FORWARD at 144° → avatar moved along that heading (2.7,-3.8); hold BACK → moved back; hold LEFT → turned back to ~0° with the camera following. Screenshot after a 108° turn: camera behind the avatar looking the new way, the main building on the left of the screen.

**Known gaps / notes:**
- No strafing (sideways walking) in TurnAndWalk: sideways input only turns. Switch Control Mode to CameraRelative for the old feel.
- Turning in place plays no walking animation (only idle), by design for now.
- The camera still gets pulled in by walls/colliders on the Default layer behind the avatar (invisible outer walls, the building body), so turning with your back to a wall shortens the camera distance.
- Not tested on a phone touch joystick; mouse/touch drag on the joystick sends the same direction values the test used.

**Next planned task:** try it by hand (WASD/joystick) and tune `turnSpeed` and `turnFollowSmoothTime` by feel.

---

## Phase 17 — Real Lobby/Corridor Colours From the Team's "detailed photo"
**Date:** 2026-10-12
**Status:** ✅ Applied and checked in Play mode, not committed

**Source:** `detailed photo.jpeg` from the team's `project collage photos` folder: identical to `/reference/ground_floor_cs_dept/groundfloor.jpeg` (already saved), the ground-floor lobby and main corridor. What it shows: a soft off-white ceiling, very pale pink-cream walls (paler than the peach used in rooms), polished dark red-brown granite floor with a thin black inlay line near the walls and a dark granite skirting, very dark brown doors, pale cream pillars with granite bases, and a green EXIT sign at the corridor end. (The photo is dim, so colours were chosen from how the surfaces look, not the raw pixel values.)

**Changed (both floors):**
- Hall/corridor walls now use `Mat_HallWall` + `wall_hall.png` (pale pink-cream, dark granite skirting with a black inlay line); room walls keep the classroom-photo peach.
- Granite floor re-textured red-brown and polished (`floor_granite.png`, gloss 0.38, slightly darkened tint) on `Mat_FloorGraniteHall` / `Mat_FloorGraniteLobby`.
- Ceiling `Mat_Ceiling` now soft off-white (232,230,226). Doors `Mat_DoorLeaf` / `Mat_DoorFrame` now very dark brown.
- New `HallExtras` group per floor: a thin black inlay line along the corridor wall, two cream pillars with granite bases flanking the lobby opening (solid), and green EXIT signs at both ends of the hall (`Mat_BlackInlay`, `Mat_Pillar`, `Mat_PillarBase`, `Mat_ExitSign`).

**Known gaps:** no wall-mounted pipes/conduits, wall decorations or notice boards, lobby aquarium, or ceiling lights (the ceiling is invisible from the game camera). Colours are eyeballed from one dim photo.

---

## Phase 16 — Realistic Classrooms From the Team's Classroom Photos + Classroom Photo Gallery
**Date:** 2026-10-12
**Status:** ✅ Built and Play-mode verified, not committed

**Source:** two classroom photos from the team's `project collage photos` folder (`inside_view_classroom.jpeg`, `inside class room view2.jpeg`; identical to `/reference/ground_floor_cs_dept/classroom_inside_view1/2.jpeg`). What they show: dark wooden desks with thick plank sides and a front panel, a wooden bench with a back rest behind each desk, cream floor tiles, a barred black-framed window with wine-red curtains, a whiteboard on the side wall, a low wooden platform with a plank-top desk and a slatted wooden armchair at the front, peach walls, ceiling fans, tube lights and a ceiling projector.

**Built (classrooms on both floors, `InteriorLayout` rebuilt in `Floor1.unity` / `Floor2.unity`):**
- **Desk + bench units** (3 rows × 2 per room, with a 1.2 m aisle down the middle): desk top, front panel, two side panels, bench seat, bench legs and a back rest, in dark wood (`Mat_DeskWood`). Each unit has ONE invisible box collider, so the avatar is stopped by the units but the pieces stay simple.
- **Teacher's platform** (`Mat_PlatformWood`, 0.2 m high: the avatar steps onto it) with a desk (top, front panel, sides) and a wooden armchair (seat, back, arms).
- **Whiteboard** with a grey frame on the west side wall (`Mat_Whiteboard`, `Mat_BoardFrame`).
- **Windows on the north wall** of every room except toilets: black frame, glowing "daylight" glass, bars (`Mat_WindowFrame`, `Mat_WindowGlass`); **wine-red curtains** + rod in classrooms (`Mat_Curtain`).
- Cabinets on the north wall in other rooms moved 0.1 m in so they don't touch the window wall.
- **Classroom popup photo gallery:** the shared classroom photo is now a 3-photo gallery (`shared_classroom` door view, `shared_classroom_2` inside view 1, `shared_classroom_3` inside view 2). `POIManager` now finds extra `_2`, `_3`, ... photos for shared photos too, not just a room's own photo.

**Verified in Play mode:** overhead and in-game views of the G7 Lecture Hall show the desk rows, platform, whiteboard, window and curtains; the avatar walks up the aisle through the open door to the back and steps onto the platform (y 1.28); desk units block the avatar from both sides; the classroom popup holds the 3 photos (`shared_classroom`, `_2`, `_3`) and shows one in the popup.

**Known gaps:**
- Ceiling fans, tube lights and the ceiling projector from the photos are NOT modelled: the ceiling is one-sided and invisible from the game camera above, so anything hanging from it would look like it was floating.
- Desk labels (e.g. "G7.12", "CUCEK WD 202"), the side stand with cables, and wall stains/marks aren't modelled.
- Layout is still the invented plan; all classrooms (both floors) get the same furniture layout.
- Other rooms keep the simple box furniture from Phase 15; only classrooms were rebuilt from photos.

**Next planned task:** the same photo-based treatment for the faculty rooms, office and exam rooms (the team has door and entrance photos only, no inside views), or the other buildings.

---

## Phase 15 — Interior Detail: Ceilings, Opening Doors, Furniture, Wall Texture, Room Name Signs
**Date:** 2026-10-12
**Status:** ✅ Built and Play-mode verified, not committed

**Built (both floors, `InteriorLayout` in `Floor1.unity` / `Floor2.unity`, rebuilt from scratch):**
- **Wall texture:** `Assets/Textures/wall_plaster.png` (512 px, drawn by script from the interior photos): mottled peach plaster with a dark granite skirting band at the bottom. Walls are now 3.2 m tall. Visible perimeter walls added on the north, east and west sides (the south/front side stays open to the camera).
- **Ceilings:** one one-sided quad per floor (`Ceiling`, 3.25 m up, facing down). It shows from inside but is culled when the camera looks down from above, so it never blocks the view. It only shows when the camera is below it.
- **Doors that open:** `DoorController.cs` (new). Each room has a door leaf on a hinge (`Door_<x>` > `Hinge` > `Leaf`, solid, "Ignore Raycast" layer) and a 3.2×2.6×4.2 m trigger. Walking up swings it open 90° into the room (240°/s) and it closes when the avatar leaves. Soft door sound: `Assets/Audio/door_open.wav` (generated placeholder), `AudioManager.PlayDoor()` / `doorClip` slot.
- **Furniture** (solid, simple boxes; a 1.2 m aisle is always left down the middle of each room): classrooms (rows of benches, teacher desk, green board), faculty rooms (desks + monitors + cabinet), office (desks, counter, cabinets), exam rooms (long table, cabinets), principal (big desk, chairs, sofa, bookshelf), toilets (stalls + sinks), labs (long tables + monitors), HOD rooms (desk, chair, sofa, cabinet). 43 pieces on Floor1, 42 on Floor2.
- **Room name signs:** blue plates (`TextMeshPro` 3D text) above every door, in the style of the real "PRINCIPAL G1" sign, e.g. "FACULTY ROOM-1  G4", "ADMINISTRATIVE OFFICE  G2", "G7  LECTURE HALL". Texts come from the room names and the photos' door signs.
- New materials: `Mat_InteriorWall` (now textured), `Mat_Ceiling`, `Mat_DoorLeaf`, `Mat_FurnitureWood`, `Mat_Cabinet`, `Mat_Blackboard`, `Mat_Screen`, `Mat_Sofa`, `Mat_Stall`, `Mat_Sink`, `Mat_SignBlue`.

**Verified in Play mode:** the Office door is closed when away, swings fully open (−90°) as the avatar steps up, lets the avatar walk through and into the room (to the back wall), and closes after the avatar walks out; furniture and partitions stop the avatar; hall view shows signs above doors on plaster walls; overhead view shows the rooms through the ceiling; classroom view (G7 Lecture Hall) shows benches, aisle, board and door sign. Floor2 checked in the scene: 11 doors, 11 signs, ceiling, 42 furniture pieces.

**Known gaps:**
- Furniture and signs are simple boxes and flat text: no chairs/legs/detail, no fans or lights, no tile walls in toilets, no whiteboards/notices/posters.
- Everything is placed from the INVENTED layout (see Phase 14 notes), not real measurements. Sign names for Floor2 follow the Firestore room names, not photos (none exist yet).
- Test artefact worth knowing: teleporting the avatar with its `CharacterController` switched off skips the trigger-exit event, so a door/popup can stay open after a teleport. Real walking fires the exit normally.
- The invisible outer wall colliders (Default layer) pull the camera in close when the avatar is near the front of the building.
- Room popups (photos) still open only inside a room; there's no popup at the door yet.

**Next planned task:** floor 3+ (needs data), or textures for the other buildings, per team priority.

---

## Phase 14 — Main Building Looks Like the Real One (colours, facade, roof) + Interior Walls
**Date:** 2026-10-11
**Status:** ✅ Built and Play-mode verified, not committed

**Built:**
- The main building is now a solid shell in the real colours from the team's photos: salmon-pink walls, deep-red roof (flat slab + a darker cap), blue-teal glass. Facade textures were drawn by a small Python/PIL script from the photos (`Assets/Textures/facade_front|back|side.png`, 2048×536 / 552×536): paired windows in ten bays, pilasters and floor bands, the central glass stairwell and left glass tower (front only), and a dark entrance. Materials `Mat_BuildingWall/Roof/RoofCap`, `Mat_FacadeFront/Back/Side`. Scene object `MainBuilding` > `OutsideShell` (solid `Body` + 4 facade quads + roof) and `InsideCutaway` (the old translucent box).
- `BuildingShell.cs` (new): `SetInside(bool)` swaps the solid shell for the see-through cutaway. `FloorTransitionManager` calls it: inside **before** the avatar is moved in (so the solid walls can't push it), outside **after** the avatar is back out. The shell's `Body` has a collider, so from outside you can't walk through the walls, only in through the door.
- Entrance trigger widened from 1×1×1 to 4×3×4 (world x 18..22, z 1..5) and given a visible red marker on the ground in front of the door (`Marker_Entrance`). Main Building popup zone shrunk to x 5..35 so it no longer opens at the spawn point (x=0).
- **Invisible interior walls** (`InteriorWalls` in `Floor1` and `Floor2`: 4 box colliders just outside the 67×18 m footprint, 4 m tall): the avatar can no longer walk out of the building (or off the edge of the 1st-floor slab) while the shell is hidden.

**Verified in Play mode:** walking onto the door marker enters the building (shell hidden, cutaway shown, avatar stable at the spawn, no push-out); leaving shows the shell again with the avatar outside and the badge "Outside"; pushing the avatar toward each wall on both floors stops it inside the footprint (ground floor z 4.6/21.4, x -12.9/52.9; 1st floor same at y 4.6).

**Known gaps:**
- It's a box with painted-on windows and a flat roof, not a model: no real sloped tile roof, no recessed top floors, no arched ground-floor windows, no porch or glass tower depth. The real Blender model replaces all of it.
- The other buildings are still plain grey-blue boxes.
- Only the front/back/side faces are textured; the roof has no tile texture.
- (Interior walls between rooms now exist, see the next bullet block; the room plan itself is invented, below.)
- **Interior layout and colours (2026-10-11):** both floors now have a layout built from the real room x-positions, in the colours of the team's interior photos. Each floor scene has an `InteriorLayout` group: a dark granite hall along the front (z 4..11, `Mat_FloorGraniteHall`), a peach corridor wall at z=11 (`Mat_InteriorWall`) with a door gap at every room and a 5 m lobby opening at x 18.5..23.5, partition walls between rooms (rooms z 11..22), brown door frames (`Mat_DoorFrame`), a cream-tile slab (`Mat_FloorTile`, 0.6 m tiles) and a granite lobby alcove. Textures drawn by script: `Assets/Textures/floor_tile.png`, `floor_granite.png` (256 px). Walls are 3 m tall and on the "Ignore Raycast" layer: the avatar collides with them but the camera's collision check ignores them. The stairs moved into the lobby (21, y, 17); the 1st-floor arrival spawn is (21, 4.6, 13.5). `placeholderRoomOffset` is now (1,0,-5.5), so room triggers sit in the north room row (z=18.5) and you only get a room's popup by walking through its door. Verified in Play mode: the avatar walks through door gaps and the lobby gap and is stopped by blank wall sections and partitions on both floors; stairs up/down work from the lobby; 10 and 11 rooms load.
- **The interior plan is an INVENTED layout** that only follows the placeholder room x-positions (no real floor plan or measurements): one row of equal-sized rooms, a single hall, and the same plan on both floors. Real room shapes, corridor widths and the real stairwell and lift positions need the team's measurements or sketches. Floor1's east end (x 43.5..53.5) is an unused closed room; Floor2 has an extra closed room at x -13.5..-8.5.
- No ceilings (the camera looks down from above), no doors that open, no furniture, no wall texture (flat peach colour), no room name signs.

**Next planned task:** Floor3 (2nd floor) Unity side + Firestore doc, or textures for the other buildings, per team priority.

---

## Phase 13 — AudioManager: Footsteps, UI Click, Floor Chime, Ambience, Mute (FR-14)
**Date:** 2026-10-11
**Status:** ✅ Built and Play-mode verified (by state checks; sound itself not listened to), not committed

**Built:**
- `AudioManager.cs` (new, singleton, lives in Exterior as the `AudioManager` GameObject): footsteps while the avatar is walking on the ground (one step every `stepDistance` = 1.4 m, so faster walking = faster steps, with slight random pitch/volume), a UI click, a floor-change chime, and a quiet looping ambience. Two `AudioSource`s are created in code, so the only Editor setup is the four clip slots.
- Hooks: `POIManager.ShowPOI` plays the click when a popup opens; `FloorTransitionManager` plays the chime at the start of `EnterFloorRoutine` / `ExitFloorRoutine`. Both null-check `AudioManager.Instance`, so scenes without it still work.
- **Sound: On/Off button** (`SoundButton` on `HUDCanvas`, top-right) calls `AudioManager.ToggleMute()`: sets `AudioListener.volume` to 0/1, updates its label, and remembers the choice in `PlayerPrefs` (`audioMuted`). `HUDCanvas` now has a `GraphicRaycaster` so the button is clickable (the floor badge ignores clicks, so it still doesn't block the joystick).
- **Placeholder sounds** generated by a small Python script (no downloads, no licences, zero cost): `Assets/Audio/footstep.wav`, `ui_click.wav`, `floor_change.wav`, `ambience.wav` (10 s loop). Imported mono/Vorbis: effects Decompress On Load, ambience Compressed In Memory. To use better sounds later, drag other clips onto the same slots on the `AudioManager` object, no code change. `.wav` is already LFS-tracked.

**Verified in Play mode:** ambience starts by itself and loops; popup open and floor change both start the effects source; holding the joystick forward fires footsteps every ~0.36 s (14 steps in ~4.7 s, matching 1.4 m / 4 m/s); clicking the Sound button gives volume 0 + "Sound: Off" + pref 1, and clicking again restores volume 1 + "Sound: On" + pref 0.

**Known gaps:**
- Nobody has listened to the sounds yet. They are simple synthesized placeholders (soft thud, blip, two-note chime, wind-like noise) and will sound basic; levels may need tuning by ear (`Volumes` on `AudioManager`).
- Not tested in a WebGL build in a browser: browsers keep sound off until the first click/tap, which Unity handles, but confirm in the browser.
- Footsteps are the same on every surface (no different sound on tile vs grass).
- No separate volume slider, only on/off.

**Next planned task:** listen to it in the Editor (Game view, speakers on) and tune volumes; then GitHub Issues for the remaining work.

---

## Phase 12 — Floor Stacking (Exterior ↔ Floor1 ↔ Floor2)
**Date:** 2026-10-11
**Status:** ✅ Editor-verified via Play mode (Exterior → Floor1 → Floor2 → Floor1 → Exterior), not committed

**Built:**
- Design decision: leaving a floor returns the player to **wherever they were standing before they went up** (not a hand-placed spawn point). A hand-placed "down" spawn would need a Transform reference into a scene that's unloaded, which Unity can't resolve.
- `FloorTransitionManager`: a `Stack<ReturnPoint>` (scene, floorId, position) is pushed on every `EnterFloor`. `EnterFloor` now unloads the current floor (and its room triggers) before loading the next, so floors stack. `ExitFloor()` (no argument now) pops the stack: back to Exterior, or reloads the previous floor and re-fetches its rooms. Shared cleanup moved into `UnloadCurrentFloorRooms()`.
- `StairsTriggerVolume`: `spawnPoint` is only required for `EnterFloor`; `ExitFloor` ignores it.
- `Floor2.unity` created and added to Build Settings. It contains a `StairsDown_ToFloor1` (ExitFloor) trigger.
- `Floor1.unity` contains `StairsUp_ToFloor2` (EnterFloor, `FLOOR_2`) with child `SpawnPoint_Floor2Entry`, plus a new `StairsDown_ToExterior` (ExitFloor). Both floors have a ground slab and red marker cubes on every stairs trigger (triggers are 3 m wide).
- **Bug found and fixed in Play-mode testing:** `player.position = ...` was silently undone by the avatar's `CharacterController` (it keeps its own position), so every transition logged "avatar moved" but moved nobody. Added `FloorTransitionManager.TeleportPlayer()`, which disables the controller for the write. Verified: Exterior → Floor1 → Floor2 → Floor1 → Exterior with far-apart spawn points, 10/11/10 rooms loaded as expected.
- Added a 1.5 s cooldown after each transition so coming down (which lands you inside the stairs trigger you climbed from) doesn't instantly bounce you back.
- **Floors positioned inside the building:** floor scenes share the Exterior's world coordinates. `FloorTransitionManager` has `buildingOrigin` (20,0,4 = the building's front door, same spot as the Exterior entrance trigger; Firestore room x/z are measured from the door) and `storeyHeight` (4 m); floor N's ground is at y = (N-1)*4. Ground slabs are 90×40 m centred at (20,·,22), covering every room. Firestore room positions are now **relative to the building origin** and shifted to the floor's height (`RoomData.position` tooltip updated). Exterior entrance moved to the south face (20,0,4) with spawn inside (20,1,11); Floor1 exit at (20,1,8); stairs at (14,·,14).

- **Stopgap building box (temporary):** `Stopgap_BuildingMassing` in Exterior, a translucent pink 70×20×38 m box (x -15..55, z 4..42, 5 storeys × 4 m) with no collider, material `Mat_BuildingStopgap`. It stands in for the real building so the floors visibly sit inside something. Floor slabs were trimmed to the same 70×38 footprint. **Delete it when the Blender model is imported**, and set `buildingOrigin`/`storeyHeight` to match the model.
- **Camera bug fixed (found by checking the stopgap box in the Game view):** `CampusCameraController`'s collision raycast treated invisible trigger volumes (e.g. the 3 m Floor1 exit trigger) as solid and pulled the camera to 1.25 m behind the avatar. It now uses `QueryTriggerInteraction.Ignore`; camera back at its normal (0,5,-8) offset, avatar, markers and slab visible through the translucent wall.
- **Floor indicator HUD badge (SDD 4.1):** new `HUDCanvas` in Exterior (Screen Space Overlay, 1920×1080 scaler, no raycaster so it never blocks the joystick) with a dark translucent top-left `FloorBadge` + `FloorBadgeText` (TMP), assigned to `FloorTransitionManager.floorIndicatorText`. Verified in Play mode: Outside → Ground Floor — CS Department → 1st Floor — Electrical Department → Ground Floor — CS Department → Outside.
- **Room popup photos:** `POIManager` shows an optional photo on the right of the popup (new `POIPhoto` Image under `POIPopupPanel`, assigned to `popupPhoto`). Photos live in `Assets/Resources/RoomPhotos/` (loaded with `Resources.Load`, since Firebase Storage isn't on the free Spark plan) and are matched by file name: first an exact `<roomId>.jpeg`, then a shared photo by keyword: any room id containing `classroom` uses `shared_classroom.jpeg` (G7 door photo), any containing `faculty` uses `shared_faculty.jpeg` (G4 door photo), per the team (all classrooms look alike, as do faculty rooms). Exact photos: `room_exam_cell`, `room_office` (same photo, shared entrance), `room_exam_control`. No match = popup unchanged (text full width). Imported as Sprites, max 1024 px, ~450 KB total. Verified in Play mode on both floors: all 3+3 classrooms and both faculty rooms get the shared photos, Office/Exam Cell/Exam Control their own; principal, toilets, labs, HOD rooms show none. To add a photo: drop it in that folder (Texture Type = Sprite) and add a keyword to `SharedPhotos` if it should cover a room type.
- **Main building photos:** 4 exterior photos (front, back, front-left, right) added to `/reference/building_exterior/` for the modellers (LFS-tracked `.jpeg`). `building_front.jpeg` also copied to `Assets/Resources/RoomPhotos/main_building.jpeg`, so the Main Building popup shows it. `POIManager` now scales the popup photo to fit its own shape (portrait room photos and landscape building photos) and gives the text the remaining width. Verified in Play mode. Not applied to the stopgap box: perspective photos with sky/grass/people would look stretched on a flat box; use them as facade texture on the real Blender model. Note: the Main Building popup text in Firestore is still the Phase 8 test string "This text came from Firestore."
- **More room photos (2026-10-11):** added `room_principal` (Principal G1 door), `room_office` (Administrative Office G2 door; replaces the earlier Exam Cell photo, which stays on `room_exam_cell`), `room_gents_toilet` (Gents Toilet doorway) and `room_ladies_toilet` (toilet-side corridor with water coolers: team should confirm this is the right photo for the ladies' side). Originals copied to `/reference/ground_floor_cs_dept/`. All 10 ground-floor rooms now show a photo (verified in Play mode). Floor2 rooms with no photo: labs, HOD rooms, computer lab; Floor2's gents/ladies toilets share ids with the ground floor's, so they show the ground-floor toilet photos.
- **Main Building photo gallery:** all four building photos are now in the Main Building popup as `main_building` (front), `main_building_2` (back), `_3` (front-left), `_4` (right) in `Assets/Resources/RoomPhotos/`. `POIManager` treats `<id>`, `<id>_2`, `<id>_3`, ... as a gallery: it auto-advances every 3 s (unscaled time) and tapping the photo (a `Button` on `POIPhoto` calling `POIManager.NextPhoto`) goes to the next; each photo is re-fitted to its own shape. Any room/POI can use this just by adding numbered files. Verified in Play mode: cycles 1→4 and wraps; the portrait photo resizes the slot to 180×240. Single-photo rooms are unchanged.
- **Real building sizes applied (2026-10-11, from team's map measurements; no tape measurements exist):** main building 67×18 m, bike shed 21×8, car parking 47×15, MCA block 43×11, canteen 19×11, badminton court 19×8 (length × width, metres). `storeyHeight` is now 3.5 m (typical; 5 storeys = 17.5 m), so floor N is at y = (N-1)×3.5. The stopgap box is 67×17.5×18 at x -13.5..53.5, z 4..22 (front/door face at z=4, door centred at x=20); floor slabs match (67×18). The other five buildings are now solid placeholder boxes (`Mat_PlaceholderBuilding`) sized to those footprints, with the popup trigger = footprint + 4 m margin; the Main Building's popup zone is now the front plaza (40×8 at z=-2) so it doesn't overlap the interior. Ground plane enlarged to 160×160 and boundary walls moved to ±81.
  - **Building positions now from the team's map coordinates (2026-10-11):** GPS pins (main 9.460634, 76.438096; car shed 9.460062, 76.438045; bike shed 9.460121, 76.438367; badminton 9.460761, 76.438520; MCA 9.460732, 76.437538; canteen 9.460928, 76.437771) converted to metres from the main building (≈109,815 m/° lon, 110,604 m/° lat) and placed with the main building centre at world (20,13): car shed (14.4,-50.2), bike shed (49.7,-43.8), badminton (66.5,27.0), MCA (-41.3,23.8), canteen (-15.6,45.5). **Assumptions to verify:** north = +z / east = +x, the main building's long side runs east-west with the door facing south, the pins are at each building's centre, and every box is axis-aligned (real buildings may be rotated). Heights are still guesses (MCA 7 m, bike shed 3, car shed 4, canteen 4, badminton 9). No overlaps: all boxes clear each other and stay inside the ±81 boundary walls.
  - **Room positions:** Firestore's placeholder rooms (z=20 from the door) didn't fit an 18 m deep building, and Firebase MCP is offline, so `FloorTransitionManager` has a temporary `placeholderRoomOffset` (1,0,-11) shifting all rooms into the building (z=13, x -9..41 on Floor1, -4..51 on Floor2). Set it to (0,0,0) once real door-relative measurements are in Firestore. Verified in Play mode: all rooms inside the footprint on both floors; full loop Exterior → Floor1 → Floor2 → Floor1 → Exterior works with the new heights (Floor2 player y=4.6). The x offset was tuned after that run (arithmetic-checked, not re-run).
- Popup close button label said "Button" (default text, already in the committed scene); changed to "X".

**Known gaps:**
- Verified in Play mode (Editor driven with `EditorApplication.Step()` since it doesn't tick unfocused): entrance → Floor1 (10 rooms at y=0, z=24, player lands on slab) → Principal's Room popup → stairs → Floor2 (11 rooms at y=4, directly above) → HOD Room 1 popup → down trigger back to Floor1 (no bounce, 10 rooms reloaded) → exit trigger back outside. Slab widths were adjusted after this run and not re-run.
- All numbers are placeholders: the Blender model isn't imported and the `POI_MainBuilding` cube is only 6×3×6, so it doesn't visually contain the interior. When the model arrives, set `buildingOrigin`/`storeyHeight` to match it.
- `Packages/packages-lock.json` has an unrelated hash change from Unity: review before committing.

**Next planned task:** walk the loop by hand, then commit.

---

## Phase 11 — Electrical Dept (1st Floor) Firestore Data + More Ground-Floor Photos
**Date:** 2026-10-10
**Status:** ⚠️ Firestore data only — NOT enterable in-game yet, see gaps below

**Built:**
- 5 more ground-floor reference photos added to `/reference/ground_floor_cs_dept/` (closer interior shots of the G7 Lecture Hall and Faculty Room-1/G4). Folded the extra detail into those two rooms' Firestore descriptions (desks labelled G7.12/G7.16/G7.17, projector, red curtains; individual faculty desks with computer/printer setups).
- New `floors/FLOOR_2` Firestore document: `label: "1st Floor — Electrical Department"`, `floorNumber: 2`, `sceneName: "Floor2"`, `isGuaranteed: false` (ground floor already holds the one guaranteed-floor slot per `CLAUDE.md`).
- 11 `rooms` subdocuments under `floors/FLOOR_2`, per the team's description of the real layout reached by the staircase up from ground floor: **left wing** — Electrical Computer Lab, Faculty Room (Electrical Dept), Classroom 1, Gents Toilet, Ladies Toilet; **right wing** — Electrical Lab 1, Electrical Lab 2, HOD Room 1, HOD Room 2, Classroom 2, Classroom 3. Same placeholder-distance caveat as Phase 10 — no measurements, no photos for this floor yet (team confirmed the `1st floor` reference folder is still empty).

**Known gaps / NOT done — this floor cannot be entered in-game yet:**
- **No `Floor2.unity` scene exists.** `FloorTransitionManager.EnterFloor()` calls `SceneManager.LoadSceneAsync(floor.sceneName, ...)` — with no scene named `Floor2`, this will fail/log an error the moment something tries to transition there. This is Editor GUI work (File > New Scene, save as `Assets/Scenes/Floor2.unity`), not something addable by data or a script alone.
- **`Floor2` is not in Build Settings** (File > Build Settings > Scenes In Build) — required for `SceneManager.LoadSceneAsync` to find it at all, in-Editor or in a build.
- **No stairs trigger exists inside `Floor1` to reach `Floor2`.** The only `StairsTriggerVolume` today lives in `Exterior` and goes `Exterior → Floor1`. Going `Floor1 → Floor2` needs a new trigger placed inside the `Floor1` scene (same component, `direction: EnterFloor`, `floorId: FLOOR_2`), plus a `SpawnPoint` child Transform for where the avatar lands on `Floor2`.
- **No way back down yet either** — `ExitFloor()` currently always returns to a spawn position back in `Exterior`, it doesn't support floor-to-floor-to-floor stacking. Going from `Floor2` back to `Floor1` (rather than all the way outside) isn't implemented — this needs either a design decision (does leaving Floor2 go to Floor1 or all the way outside?) or an extension to `FloorTransitionManager`.

**Next planned task:** decide the up/down navigation design for 3-level stacking (Exterior ↔ Floor1 ↔ Floor2), then do the Editor-side scene creation and trigger placement to make Floor2 actually reachable. None of this is blocked on more Firestore data — the data is ready and waiting.

---

## Phase 10 — Real Ground-Floor Layout from Reference Photos
**Date:** 2026-10-10
**Status:** ✅ Complete, Firestore data corrected, not yet re-verified in-Editor after this change

**Built:**
- 11 reference photos of the real ground floor (CS department) added to `/reference/ground_floor_cs_dept/` (team had no measurements, but did have photos of every room — see knowledge base Section 3, `/reference` purpose). Confirmed via LFS: `*.jpeg` was already tracked in `.gitattributes` from day one, so no config change needed.
- Photos confirm this is a Cochin University (CUCEK) building and reveal real room numbers/signage: **G4** = Faculty Room-1, **G7** = Lecture Hall, **G8** = Examination Control Room (double-signed as a Conference Room, "Restricted Entry"), Office and Exam Cell share one entrance, Principal's Room (Dr. Preetha Mathew K) sits at the end of the main corridor, lift + aquarium near the main entrance, center staircase separate from the lobby.
- Corrected the Phase 9 Firestore room layout to match the team's description of the real walking path: entrance → lobby (stairs on the left, lift straight ahead) → corridor forks into a **left wing** (Office, Exam Cell, Classroom 1, Faculty Room-1/G4, Gents Toilet, Ladies Toilet — 6 rooms) and a **right wing** (Principal's Room, Examination Control Room/G8, Classroom 2 "G7 Lecture Hall", Classroom 3 — 4 rooms). Replaces Phase 9's 11-room two-row grid guess (which had the wrong room on the wrong side and an extra classroom) with 10 rooms whose x-coordinate sign now actually means left/right of the corridor fork, not an arbitrary row. Room descriptions now reference real signage instead of generic placeholder text.
- Positions are still placeholder *distances* (no tape-measure numbers exist), but the left/right structure and which real room is in which wing is now correct per the team's firsthand description.

**Known gaps / flagged, not acted on:**
- Haven't re-run the in-Editor test since this data change — next session should re-enter `StairsEntrance_ToFloor1` and confirm `loaded 10 room(s) for 'FLOOR_1'` (was 11) and that walking into each wing's rooms still pops up the right popup.
- Still no real distances between rooms, stairs, or the entrance — only relative left/right order is accurate.
- Stairs and lift are real, confirmed features of the lobby but aren't represented as Firestore room documents or in-scene objects yet — they're not "rooms" under the current `RoomData` schema, just noted here for whenever navigation/wayfinding around them becomes relevant.

**Next planned task:** re-verify the corrected 10-room layout in-Editor, then same as Phase 9 — Blender exterior blockout or next floor, per team priority.

---

## Phase 9 — Firestore Link + Ground-Floor Room Data + Room Trigger Spawning
**Date:** 2026-10-10
**Status:** ✅ Complete, in-Editor verified, committed on `feature/floor-interior-system` (`1c35b81`, `928fe67`, `3eca209`), not yet pushed

**Built:**
- Firebase MCP server connected and working (earlier `CONNECT_TIMEOUT` was just a cold npx cache, resolved by itself). Repo linked to the `cucek-campus-nav` Firebase project via `firebase_init` (Firestore only): `.firebaserc`, `firebase.json`, `firestore.indexes.json` added.
- `firestore.rules` fixed before committing: `firebase_init`'s generated file was the generic "open read/write until 30 days" default, which did NOT match the actually-published production rules (public read, no write) from Phase 8 — would have silently downgraded security on the next `firebase deploy`. Rewrote it to match production and validated with `firebase_validate_security_rules`.
- Bug fix: `FirebaseDataManager.ParseRoomsResponse()` read a Firestore field called `name` that doesn't exist anywhere — `RoomData.cs` and every room document use `displayName`. Room titles were coming back blank. Fixed to read `displayName`.
- Real ground-floor room data (CS department, per team's description, not yet measured — `/reference` is still empty) added to Firestore: one `floors/FLOOR_1` document (`label: "Ground Floor — CS Department"`, `sceneName: "Floor1"`, `isGuaranteed: true`) and 11 `rooms` subdocuments — one side: Office, Exam Cell, Classroom 1, Gents Toilet, Ladies Toilet, CS Dept Staffroom; opposite side: Classroom 2/3/4, Principal's Room, Exam Control Room. Positions are placeholder coordinates (two rows at z=+5/-5, spread along x) since no real measurements exist yet — will need correcting once someone measures the actual building.
- Caught and fixed a document-ID mismatch while wiring this up: `FirebaseDataManager.FetchRoomsForFloor()` builds its URL as `floors/{floorId}/rooms`, using the `floorId` *field value* directly as the Firestore *document ID* path segment — so the floor document's ID had to literally be `FLOOR_1` (matching the scene's `StairsTriggerVolume.floorId`), not an arbitrary ID like the `floor1` first used.
- `RoomTriggerVolume.cs` (new): same enter/exit → show/hide popup pattern as `POITriggerVolume.cs`, but with a public `roomId` field instead of an Inspector-set one, since it's meant to be added via `AddComponent` at runtime rather than hand-placed.
- `FloorTransitionManager.SpawnRoomTriggers()`: builds one Box Collider + `RoomTriggerVolume` per room from Firestore's `position`/`poiRadius` when a floor loads, tracks them in a list, and destroys them all on floor exit. Before this, `POIManager.AddRoomPOIs()` only registered room text in the popup lookup — nothing in the scene ever detected the Player to trigger it.
- Moved `SpawnPoint_Floor1Entry` from (0,1,50) to (0,1,0) in the Exterior scene, closer to the new room trigger positions, for easier manual testing.

**Verification performed:**
- Editor Play-mode test: walked into `StairsEntrance_ToFloor1`, Console logged `loaded 1 floor(s)`, `entered 'Ground Floor — CS Department'`, `loaded 11 room(s) for 'FLOOR_1'` — confirmed `RoomTrigger_room_*` GameObjects appear in the Hierarchy (one per room).
- Teleported the Player (via Inspector Transform, paused) to the "Office" room's exact coordinates; confirmed its popup appeared.
- Firestore rules validated via `firebase_validate_security_rules` (no errors) and cross-checked against the live published rules via `firebase_get_security_rules` before committing, so the local file can't accidentally downgrade production security on a future deploy.

**Known gaps / flagged, not acted on:**
- Room trigger positions are placeholder coordinates, not measured from the real building — `/reference` has no sketches yet.
- `Floor1` has no boundary walls yet (unlike `Exterior`), so walking off the placeholder ground has no edge — ran into this during manual testing (looked like "values gradually increasing" but was actually a stuck-keyboard-axis bug from editing the Inspector mid-Play, not a boundary issue — worth fixing properly with real boundaries once there's real floor geometry).
- Spawned `RoomTrigger_*` GameObjects land in the `Exterior` scene group in the Hierarchy (not `Floor1`), since `new GameObject()` goes into whichever scene is "active" — harmless (cleanup is done by explicit reference, not scene-unload), but worth knowing so it isn't mistaken for a bug.
- Firebase project still has no teammates added (personal Gmail account only) — carried over from Phase 8.

**Next planned task:** get real ground-floor measurements into `/reference` so room positions can be corrected, and/or continue toward whichever of the Blender exterior blockout / remaining interior floors is next per the team's plan.

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
