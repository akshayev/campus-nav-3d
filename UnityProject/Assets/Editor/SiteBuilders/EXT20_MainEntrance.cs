using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT20 main entrance in Exterior.unity, copied from the team's two photos of the college's main entrance:
//   - a recessed entrance porch, 4.2 m wide and 3 m deep, with coral walls on both sides, a low soffit and a granite floor
//   - at the back of the porch: a black-framed glass door with a notice board behind it (at the left), three steps up to it,
//     a window above the right-hand wall, the black foundation plaque, a fire-hose box and a red pipe
//   - over the porch: a curved blue-glass bay with a coral curved roof band, and above it the tall arched blue-glass tower with
//     a coral arched frame
//   - a row of red pots with leafy plants along the right wall and at the foot of the steps
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT20 Main Entrance
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT20_MainEntrance.Build -logFile ext20.log
//
// It is RE-RUNNABLE: every run deletes the old EXT20_MainEntrance object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT20_MainEntrance, to Exterior.unity. Nothing that already exists is changed:
// the team's main building (MainBuilding) is not edited; this is built in front of its front wall (z 3.47), centred on x 20.
// HOW THE AVATAR GOES IN: the team's trigger StairsEntrance_ToFloor1 (x 18 .. 22, z 1 .. 5) is exactly inside this porch, so
// walking in through the porch loads the ground floor (Floor1) as before. The two side walls are solid, so the way in is the 4.2 m gap
// between them; they are on layer 2 ("Ignore Raycast") so the follow camera never gets pulled in by them. The three steps are 0.1 m
// high each, so the avatar walks up them.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT20_MainEntrance
{
    const string RootName = "EXT20_MainEntrance";
    const string Prefix = "EXT20";
    const int CameraPassThroughLayer = 2;

    const float CenterX = 20f;
    const float WallZ = 3.40f;           // the porch's back wall, just in front of the building's front wall (z 3.47)
    const float Embed = 0.15f;           // everything joined to the building runs this far INTO its wall (z 3.47), so there is no slit
    const float JoinZ = WallZ + Embed;   // z 3.55: the back edge of the side walls, soffit, bays and tower sides
    const float FrontZ = 0.4f;           // the porch's open front edge
    const float HalfWidth = 2.1f;        // the porch is 4.2 m wide
    const float SideWallThickness = 0.3f;
    const float SoffitHeight = 3.4f;

    // The curved bays above the porch.
    const float LowerBayHalfWidth = 3.7f;
    const float LowerBayDepth = 1.6f;
    const float LowerBayBottom = 3.6f;
    const float LowerBayTop = 7.4f;
    const float BandTop = 8.3f;
    const float TowerHalfWidth = 2.6f;
    const float TowerDepth = 0.9f;
    const float TowerBottom = 8.3f;
    const float TowerTop = 15.4f;

    [MenuItem("Campus/Build EXT20 Main Entrance")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT20_MainEntrance", BuildInternal);
    }

    class Mats
    {
        public Material wall, soffit, glass, mullion, floor, step, doorFrame, doorGlass, board, paper, plaque, fireBox, pipe, pot, leaves, light;
    }

    static void BuildInternal()
    {
        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == RootName)
            {
                Object.DestroyImmediate(candidate);
            }
        }

        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");

        var m = new Mats
        {
            // The porch walls use the building's own wall colour (the team's material, read only), so they match the facade.
            wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_BuildingWall.mat"),
            soffit = SiteBuilderUtil.GetMaterial(Prefix, "Soffit", new Color32(226, 150, 140, 255), 0.02f),
            glass = SiteBuilderUtil.GetMaterial(Prefix, "Glass", new Color32(70, 108, 142, 255), 0.55f),
            mullion = SiteBuilderUtil.GetMaterial(Prefix, "Mullion", new Color32(60, 40, 40, 255), 0.2f),
            floor = SiteBuilderUtil.GetMaterial(Prefix, "Granite", new Color32(112, 98, 92, 255), 0.1f),
            step = SiteBuilderUtil.GetMaterial(Prefix, "Step", new Color32(96, 74, 66, 255), 0.1f),
            doorFrame = SiteBuilderUtil.GetMaterial(Prefix, "DoorFrame", new Color32(28, 26, 28, 255), 0.3f),
            doorGlass = SiteBuilderUtil.GetMaterial(Prefix, "DoorGlass", new Color32(52, 60, 66, 255), 0.5f),
            board = SiteBuilderUtil.GetMaterial(Prefix, "NoticeBoard", new Color32(214, 208, 190, 255), 0.05f, new Color32(60, 58, 50, 255)),
            paper = SiteBuilderUtil.GetMaterial(Prefix, "Paper", new Color32(240, 240, 232, 255), 0.05f),
            plaque = SiteBuilderUtil.GetMaterial(Prefix, "Plaque", new Color32(34, 36, 36, 255), 0.3f),
            fireBox = SiteBuilderUtil.GetMaterial(Prefix, "FireBox", new Color32(206, 60, 36, 255), 0.3f),
            pipe = SiteBuilderUtil.GetMaterial(Prefix, "Pipe", new Color32(176, 60, 44, 255), 0.3f),
            pot = SiteBuilderUtil.GetMaterial(Prefix, "Pot", new Color32(190, 66, 40, 255), 0.1f),
            leaves = SiteBuilderUtil.GetMaterial(Prefix, "PotLeaves", new Color32(46, 104, 48, 255), 0.05f),
            light = SiteBuilderUtil.GetMaterial(Prefix, "SoffitLight", new Color32(255, 255, 235, 255), 0.1f, new Color32(255, 255, 220, 255)),
        };
        if (m.wall == null)
        {
            m.wall = SiteBuilderUtil.GetMaterial(Prefix, "WallFallback", new Color32(236, 164, 156, 255), 0.02f);
        }

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        BuildPorch(root.transform, m);
        BuildDoor(root.transform, m);
        BuildBays(root.transform, m);
        BuildPlants(root.transform, m);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT20_MainEntrance: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // ------------------------------------------------------------------------------------
    // The porch: side walls, back wall, soffit, floor, steps
    // ------------------------------------------------------------------------------------

    static void BuildPorch(Transform parent, Mats m)
    {
        Transform porch = SiteBuilderUtil.Group(parent, "Porch");
        float depth = JoinZ - FrontZ;
        float centreZ = (JoinZ + FrontZ) / 2f;

        // Side walls (solid, camera pass-through) and the back wall panel.
        foreach (float side in new[] { -1f, 1f })
        {
            SiteBuilderUtil.Part(porch, side < 0 ? "WallLeft" : "WallRight", PrimitiveType.Cube,
                new Vector3(CenterX + side * (HalfWidth + SideWallThickness / 2f), SoffitHeight / 2f, centreZ),
                new Vector3(SideWallThickness, SoffitHeight, depth), m.wall, true).layer = CameraPassThroughLayer;
        }
        SiteBuilderUtil.Part(porch, "BackWall", PrimitiveType.Cube, new Vector3(CenterX, SoffitHeight / 2f, WallZ + 0.075f),
            new Vector3(2f * HalfWidth, SoffitHeight, 0.15f), m.wall, false);

        // The low soffit over the porch, with a recessed light.
        SiteBuilderUtil.Part(porch, "Soffit", PrimitiveType.Cube, new Vector3(CenterX, SoffitHeight + 0.1f, centreZ),
            new Vector3(2f * HalfWidth + 2f * SideWallThickness + 0.2f, 0.2f, depth + 0.2f), m.soffit, false);
        SiteBuilderUtil.Part(porch, "SoffitLight", PrimitiveType.Cube, new Vector3(CenterX - 0.8f, SoffitHeight - 0.02f, WallZ - 1.0f),
            new Vector3(0.4f, 0.03f, 0.4f), m.light, false);

        // The granite floor over the team's red marker (so the doorway no longer shows a red mat). A flat slab, no collider.
        SiteBuilderUtil.Part(porch, "Floor", PrimitiveType.Cube, new Vector3(CenterX, 0.055f, centreZ),
            new Vector3(2f * HalfWidth, 0.11f, depth), m.floor, false);

        // Three steps up to the door, in front of the door on the left of the porch. Each is 0.1 m higher than the last.
        float stepsMinX = CenterX - HalfWidth + 0.15f;
        float stepsMaxX = CenterX - HalfWidth + 2.55f;
        for (int i = 0; i < 3; i++)
        {
            float zFront = WallZ - 0.5f * (i + 1);
            float height = 0.11f + 0.1f * (i + 1);
            SiteBuilderUtil.Part(porch, "Step_" + (i + 1), PrimitiveType.Cube,
                new Vector3((stepsMinX + stepsMaxX) / 2f, height / 2f, (zFront + WallZ) / 2f),
                new Vector3(stepsMaxX - stepsMinX, height, WallZ - zFront), m.step, true);
        }
    }

    // ------------------------------------------------------------------------------------
    // The door, the notice board, the window, the plaque and the fittings on the back wall
    // ------------------------------------------------------------------------------------

    static void BuildDoor(Transform parent, Mats m)
    {
        Transform door = SiteBuilderUtil.Group(parent, "Door");
        float z = WallZ - 0.02f;
        float doorMinX = CenterX - HalfWidth + 0.25f;
        float doorMaxX = doorMinX + 2.35f;
        float doorCentre = (doorMinX + doorMaxX) / 2f;
        float floorTop = 0.11f + 0.3f;          // the door stands on the top step
        float doorHeight = 2.5f;

        // Black frame (four bars), a glazed screen with a transom, and the glass door leaf with its handle.
        SiteBuilderUtil.Part(door, "FrameLeft", PrimitiveType.Cube, new Vector3(doorMinX, floorTop + doorHeight / 2f, z), new Vector3(0.08f, doorHeight, 0.08f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "FrameRight", PrimitiveType.Cube, new Vector3(doorMaxX, floorTop + doorHeight / 2f, z), new Vector3(0.08f, doorHeight, 0.08f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "FrameTop", PrimitiveType.Cube, new Vector3(doorCentre, floorTop + doorHeight, z), new Vector3(doorMaxX - doorMinX + 0.08f, 0.08f, 0.08f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "Transom", PrimitiveType.Cube, new Vector3(doorCentre, floorTop + doorHeight - 0.45f, z), new Vector3(doorMaxX - doorMinX, 0.06f, 0.08f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "Glazing", PrimitiveType.Cube, new Vector3(doorCentre, floorTop + doorHeight / 2f, z + 0.02f), new Vector3(doorMaxX - doorMinX, doorHeight, 0.03f), m.doorGlass, false);
        // The inside: a notice board on the left half, lit.
        SiteBuilderUtil.Part(door, "NoticeBoard", PrimitiveType.Cube, new Vector3(doorMinX + 0.62f, floorTop + 1.35f, z + 0.05f), new Vector3(0.95f, 1.2f, 0.03f), m.board, false);
        for (int i = 0; i < 6; i++)
        {
            SiteBuilderUtil.Part(door, "Paper_" + (i + 1), PrimitiveType.Cube,
                new Vector3(doorMinX + 0.32f + (i % 3) * 0.3f, floorTop + 1.7f - (i / 3) * 0.45f, z + 0.03f), new Vector3(0.22f, 0.3f, 0.01f), m.paper, false);
        }
        // The door leaf on the right half: a darker pane with a tall frame and a handle.
        SiteBuilderUtil.Part(door, "LeafFrame", PrimitiveType.Cube, new Vector3(doorMinX + 1.7f, floorTop + 1.15f, z - 0.01f), new Vector3(1.0f, 2.1f, 0.06f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "LeafGlass", PrimitiveType.Cube, new Vector3(doorMinX + 1.7f, floorTop + 1.2f, z - 0.045f), new Vector3(0.85f, 1.95f, 0.02f), m.doorGlass, false);
        SiteBuilderUtil.Part(door, "Handle", PrimitiveType.Cube, new Vector3(doorMinX + 1.35f, floorTop + 1.05f, z - 0.07f), new Vector3(0.04f, 0.5f, 0.04f), m.fireBox, false);   // a red-orange push bar, like the fittings in the photo

        // The right-hand wall of the back wall: a window high up, the black plaque, a fire-hose box and the red pipe.
        float rightX = CenterX + HalfWidth - 0.55f;
        SiteBuilderUtil.Part(door, "WindowFrame", PrimitiveType.Cube, new Vector3(CenterX + 0.75f, 2.9f, z), new Vector3(0.95f, 0.75f, 0.06f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "WindowGlass", PrimitiveType.Cube, new Vector3(CenterX + 0.75f, 2.9f, z - 0.02f), new Vector3(0.8f, 0.62f, 0.02f), m.doorGlass, false);
        SiteBuilderUtil.Part(door, "Plaque", PrimitiveType.Cube, new Vector3(CenterX + 1.0f, 1.25f, z), new Vector3(0.95f, 1.2f, 0.04f), m.plaque, false);
        SiteBuilderUtil.Part(door, "PlaqueFrame", PrimitiveType.Cube, new Vector3(CenterX + 1.0f, 1.25f, z + 0.005f), new Vector3(1.05f, 1.3f, 0.03f), m.doorFrame, false);
        SiteBuilderUtil.Part(door, "FireBox", PrimitiveType.Cube, new Vector3(rightX + 0.4f, 1.0f, z - 0.05f), new Vector3(0.42f, 0.55f, 0.16f), m.fireBox, false);
        SiteBuilderUtil.Part(door, "PipeVertical", PrimitiveType.Cylinder, new Vector3(CenterX + HalfWidth - 0.1f, SoffitHeight / 2f, z - 0.05f), new Vector3(0.1f, SoffitHeight / 2f, 0.1f), m.pipe, false);
        SiteBuilderUtil.Part(door, "PipeHorizontal", PrimitiveType.Cylinder, new Vector3(CenterX + HalfWidth - 0.65f, 0.95f, z - 0.05f), new Vector3(0.08f, 0.55f, 0.08f), new Vector3(0f, 0f, 90f), m.pipe, false);
    }

    // ------------------------------------------------------------------------------------
    // The curved glass bays above the porch
    // ------------------------------------------------------------------------------------

    static void BuildBays(Transform parent, Mats m)
    {
        Transform bays = SiteBuilderUtil.Group(parent, "GlassBays");

        // The lower bay: a half-ellipse of blue glass bulging out toward the plaza, from the underside of the porch up to a coral roof band.
        Arc(bays, "LowerGlass", CenterX, JoinZ, LowerBayHalfWidth, LowerBayDepth, LowerBayBottom, LowerBayTop, 14, 0.06f, m.glass, m.mullion, 1.0f, 1.0f);
        Arc(bays, "LowerBandBottom", CenterX, JoinZ, LowerBayHalfWidth + 0.15f, LowerBayDepth + 0.15f, LowerBayBottom - 0.2f, LowerBayBottom + 0.05f, 14, 0.25f, m.wall, null, 0f, 0f);
        Arc(bays, "RoofBand", CenterX, JoinZ, LowerBayHalfWidth + 0.3f, LowerBayDepth + 0.4f, LowerBayTop, BandTop, 14, 0.3f, m.wall, null, 0f, 0f);
        // A flat roof on the lower bay under the tower, so you cannot see through it.
        SiteBuilderUtil.Part(bays, "BayRoof", PrimitiveType.Cube, new Vector3(CenterX, BandTop - 0.05f, (WallZ - 1.8f + JoinZ) / 2f), new Vector3(2f * LowerBayHalfWidth, 0.1f, JoinZ - (WallZ - 1.8f)), m.wall, false);

        // The upper tower: a flat blue-glass front with a half-round top, a coral frame, and mullions every metre.
        float towerCentreZ = WallZ - TowerDepth;
        SiteBuilderUtil.Part(bays, "TowerGlass", PrimitiveType.Cube, new Vector3(CenterX, (TowerBottom + TowerTop) / 2f, towerCentreZ),
            new Vector3(2f * TowerHalfWidth, TowerTop - TowerBottom, 0.08f), m.glass, false);
        // Vertical and horizontal mullions in front of the glass.
        for (float x = -TowerHalfWidth + 0.8f; x < TowerHalfWidth - 0.1f; x += 0.8f)
        {
            SiteBuilderUtil.Part(bays, "TowerMullionV", PrimitiveType.Cube, new Vector3(CenterX + x, (TowerBottom + TowerTop) / 2f, towerCentreZ - 0.05f), new Vector3(0.05f, TowerTop - TowerBottom, 0.05f), m.mullion, false);
        }
        for (float y = TowerBottom + 0.9f; y < TowerTop; y += 0.9f)
        {
            SiteBuilderUtil.Part(bays, "TowerMullionH", PrimitiveType.Cube, new Vector3(CenterX, y, towerCentreZ - 0.05f), new Vector3(2f * TowerHalfWidth, 0.05f, 0.05f), m.mullion, false);
        }
        // The half-round top: a half-disc of glass with a coral arch round it.
        Transform top = SiteBuilderUtil.Group(bays, "TowerArch");
        for (int i = 0; i < 12; i++)
        {
            float t0 = Mathf.PI * i / 12f;
            float t1 = Mathf.PI * (i + 1) / 12f;
            Vector3 a = new Vector3(CenterX + TowerHalfWidth * Mathf.Cos(t0), TowerTop + TowerHalfWidth * Mathf.Sin(t0), towerCentreZ);
            Vector3 b = new Vector3(CenterX + TowerHalfWidth * Mathf.Cos(t1), TowerTop + TowerHalfWidth * Mathf.Sin(t1), towerCentreZ);
            Vector3 mid = (a + b) / 2f;
            Vector3 d = b - a;
            // full depth: from the front of the tower back into the building wall, so there is no slot behind the arch
            GameObject frame = SiteBuilderUtil.Part(top, "ArchFrame", PrimitiveType.Cube, new Vector3(mid.x, mid.y, (towerCentreZ - 0.25f + JoinZ) / 2f), new Vector3(d.magnitude + 0.05f, 0.35f, JoinZ - towerCentreZ + 0.25f), m.wall, false);
            frame.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            // A glass wedge: a thin triangle fan drawn as a slab from the centre to the arch.
            GameObject wedge = SiteBuilderUtil.Part(top, "ArchGlass", PrimitiveType.Cube, (mid + new Vector3(CenterX, TowerTop, towerCentreZ)) / 2f,
                new Vector3((mid - new Vector3(CenterX, TowerTop, towerCentreZ)).magnitude, 0.5f * d.magnitude, 0.06f), m.glass, false);
            Vector3 radial = mid - new Vector3(CenterX, TowerTop, towerCentreZ);
            wedge.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(radial.y, radial.x) * Mathf.Rad2Deg);
        }
        // Coral side frames up the tower.
        foreach (float side in new[] { -1f, 1f })
        {
            SiteBuilderUtil.Part(bays, "TowerSide", PrimitiveType.Cube, new Vector3(CenterX + side * (TowerHalfWidth + 0.15f), (TowerBottom + TowerTop) / 2f, (towerCentreZ - 0.25f + JoinZ) / 2f),
                new Vector3(0.3f, TowerTop - TowerBottom, JoinZ - towerCentreZ + 0.25f), m.wall, false);
        }
    }

    // A curved wall: a half-ellipse (open to the building) made of flat segments, from y0 to y1. 'mullionEvery' adds a vertical dark bar
    // at every segment joint, and 'rungEvery' a horizontal dark bar every that many metres up. The segments face out toward the plaza (-z).
    static void Arc(Transform parent, string name, float cx, float cz, float rx, float rz, float y0, float y1, int segments, float thickness,
        Material material, Material mullion, float mullionEvery, float rungEvery)
    {
        Transform arc = SiteBuilderUtil.Group(parent, name);
        float height = y1 - y0;
        for (int i = 0; i < segments; i++)
        {
            float t0 = Mathf.PI * i / segments;
            float t1 = Mathf.PI * (i + 1) / segments;
            Vector3 a = new Vector3(cx + rx * Mathf.Cos(t0), 0f, cz - rz * Mathf.Sin(t0));
            Vector3 b = new Vector3(cx + rx * Mathf.Cos(t1), 0f, cz - rz * Mathf.Sin(t1));
            Vector3 mid = (a + b) / 2f;
            Vector3 d = b - a;
            float yaw = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
            GameObject segment = SiteBuilderUtil.Part(arc, "Segment_" + (i + 1), PrimitiveType.Cube, new Vector3(mid.x, y0 + height / 2f, mid.z),
                new Vector3(d.magnitude + 0.02f, height, thickness), material, false);
            segment.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (mullion != null && mullionEvery > 0f)
            {
                GameObject bar = SiteBuilderUtil.Part(arc, "MullionV_" + (i + 1), PrimitiveType.Cube, new Vector3(a.x, y0 + height / 2f, a.z), new Vector3(0.05f, height, 0.07f), mullion, false);
                bar.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (rungEvery > 0f)
                {
                    for (float y = y0 + rungEvery; y < y1 - 0.01f; y += rungEvery)
                    {
                        GameObject rung = SiteBuilderUtil.Part(arc, "Rung", PrimitiveType.Cube, new Vector3(mid.x, y, mid.z), new Vector3(d.magnitude + 0.02f, 0.05f, thickness + 0.04f), mullion, false);
                        rung.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // Potted plants: a row of red pots along the back wall on the right, and a group at the foot of the steps on the left
    // ------------------------------------------------------------------------------------

    static void BuildPlants(Transform parent, Mats m)
    {
        Transform plants = SiteBuilderUtil.Group(parent, "PottedPlants");
        float[][] pots =
        {
            new[] { CenterX - 1.75f, 1.55f, 0.55f }, new[] { CenterX - 1.45f, 1.8f, 0.7f },       // at the foot of the steps (left)
            new[] { CenterX + 0.05f, WallZ - 0.5f, 0.5f }, new[] { CenterX + 0.45f, WallZ - 0.5f, 0.45f }, new[] { CenterX + 0.85f, WallZ - 0.5f, 0.45f },
            new[] { CenterX + 1.25f, WallZ - 0.5f, 0.45f }, new[] { CenterX + 1.7f, WallZ - 0.55f, 0.6f }, new[] { CenterX + 1.95f, WallZ - 0.9f, 0.55f },
        };
        var random = new System.Random(20);
        for (int i = 0; i < pots.Length; i++)
        {
            float x = pots[i][0];
            float z = pots[i][1];
            float scale = pots[i][2];
            Transform p = SiteBuilderUtil.Group(plants, "Pot_" + (i + 1));
            p.localPosition = new Vector3(x, 0.11f, z);
            SiteBuilderUtil.Part(p, "Pot", PrimitiveType.Cylinder, new Vector3(0f, 0.22f * scale * 1.4f, 0f), new Vector3(0.4f * scale * 1.2f, 0.22f * scale * 1.4f, 0.4f * scale * 1.2f), m.pot, false);
            // Six leaf blades radiating up and out.
            for (int b = 0; b < 7; b++)
            {
                Transform pivot = SiteBuilderUtil.Group(p, "Blade");
                pivot.localPosition = new Vector3(0f, 0.45f * scale, 0f);
                pivot.localRotation = Quaternion.Euler(0f, b * 51f + (float)random.NextDouble() * 20f, 0f);
                float length = (0.55f + (float)random.NextDouble() * 0.35f) * scale * 1.6f;
                GameObject blade = SiteBuilderUtil.Part(pivot, "Leaf", PrimitiveType.Cube, new Vector3(length * 0.25f, length * 0.4f, 0f), new Vector3(0.18f * scale, length, 0.02f), m.leaves, false);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, -(30f + (float)random.NextDouble() * 25f));
            }
        }
    }
}
