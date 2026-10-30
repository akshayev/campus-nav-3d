using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT19 front gate in Exterior.unity, copied from the team's photos of the college's main gate (looking east along the brick
// road from the plaza):
//   - the gate: two thick grey-brown pillars with a wide cladded beam across the top, and a pair of dark closed gate leaves
//   - the road narrowed to the gate by two grass strips with slim yellow-green palms
//   - one straight white side wall running south from the gate's south pillar (grey base, grey-brown pillars, painted mural near the gate)
//     to the bike shed's mural wall (EXT17), and a tall billboard
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT19 Front Gate
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT19_FrontGate.Build -logFile ext19.log
//
// It is RE-RUNNABLE: every run deletes the old EXT19_FrontGate object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT19_FrontGate, to Exterior.unity. Nothing that already exists is changed (the gate
// that EXT18 used to build is now built here, with the leaves and the cladding). Build EXT05 and EXT11 first (palm and brick helpers).
// The road (EXT13) is x 52.7 .. 79, z -12.6 .. 3.4, so the gate stands in its middle, z -4.6.
//
// The wall, gate pillars and gate leaves stop the avatar but are on layer 2 ("Ignore Raycast"), which the follow camera's collision ray
// ignores, so they can never pull the camera in against the avatar; the wall also gets the team's ShedRoofFader so it hides while it is
// between the camera and the avatar.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT19_FrontGate
{
    const string RootName = "EXT19_FrontGate";
    const string Prefix = "EXT19";
    const int CameraPassThroughLayer = 2;

    // The gate.
    const float GateX = 76.4f;
    const float GateCenterZ = -4.6f;       // the middle of the road
    const float OpeningWidth = 6.0f;
    const float PillarSize = 1.2f;
    const float PillarHeight = 4.6f;
    const float LeafHeight = 2.7f;

    // The grass strips that narrow the road to the gate (x range, and the z range of each), and their palms.
    const float StripMinX = 66f;
    const float StripMaxX = 79f;
    const float NorthStripMinZ = -0.6f;
    const float NorthStripMaxZ = 3.4f;
    const float SouthStripMinZ = -12.6f;
    const float SouthStripMaxZ = -8.6f;
    static readonly float[][] StripPalms =
    {
        new[] { 68.4f, 1.3f }, new[] { 72.0f, 1.1f },
        new[] { 68.0f, -10.6f }, new[] { 71.6f, -10.8f }, new[] { 75.2f, -10.6f },
    };

    // The side wall: ONE straight wall at x = GateX, running south from the gate's south pillar to where the bike shed's mural wall (EXT17)
    // begins (z -34). It replaces the L-shaped wall (a wall along z -16.5 plus a cross wall) that stood here before. The mural panels
    // are on its west face, near the gate, like the painted wall in the photos.
    const float WallMinZ = -34f;
    const float WallHeight = 2.4f;
    const float PillarSpacing = 4f;
    // Palms between the road and the wall (x, z): clear of the floodlight pole at (66, -14).
    static readonly float[][] WallPalms = { new[] { 57.6f, -14.4f }, new[] { 70.4f, -14.4f }, new[] { 74.2f, -14.2f } };

    // The billboard behind the wall.
    const float BillboardX = 73.5f;
    const float BillboardZ = -20.5f;

    [MenuItem("Campus/Build EXT19 Front Gate")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT19_FrontGate", BuildInternal);
    }

    class Mats
    {
        public Material cladding, claddingDark, leaf, gold, concrete, lawn;
        public Material wallWhite, wallBase, wallPillar, mural1, mural2, mural3, mural4, faceSkin, faceHair;
        public Material billboard, billboardGold, billboardSkin, post;
        public Material palmTrunk, palmFrond, shrub;
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
            cladding = SiteBuilderUtil.GetMaterial(Prefix, "Cladding", new Color32(150, 140, 126, 255), 0.05f),
            claddingDark = SiteBuilderUtil.GetMaterial(Prefix, "CladdingDark", new Color32(106, 98, 88, 255), 0.05f),
            leaf = SiteBuilderUtil.GetMaterial(Prefix, "GateLeaf", new Color32(52, 54, 58, 255), 0.3f),
            gold = SiteBuilderUtil.GetMaterial(Prefix, "GateOrnament", new Color32(186, 128, 70, 255), 0.3f),
            concrete = SiteBuilderUtil.GetMaterial(Prefix, "Concrete", new Color32(170, 166, 158, 255), 0.02f),
            lawn = SiteBuilderUtil.GetMaterial(Prefix, "StripLawn", new Color32(255, 255, 255, 255), 0.05f),
            wallWhite = SiteBuilderUtil.GetMaterial(Prefix, "WallWhite", new Color32(236, 234, 226, 255), 0.02f),
            wallBase = SiteBuilderUtil.GetMaterial(Prefix, "WallBase", new Color32(150, 142, 130, 255), 0.02f),
            wallPillar = SiteBuilderUtil.GetMaterial(Prefix, "WallPillar", new Color32(130, 120, 108, 255), 0.02f),
            mural1 = SiteBuilderUtil.GetMaterial(Prefix, "MuralTeal", new Color32(60, 150, 140, 255), 0.05f),
            mural2 = SiteBuilderUtil.GetMaterial(Prefix, "MuralDark", new Color32(40, 42, 52, 255), 0.05f),
            mural3 = SiteBuilderUtil.GetMaterial(Prefix, "MuralWhite", new Color32(232, 228, 216, 255), 0.05f),
            mural4 = SiteBuilderUtil.GetMaterial(Prefix, "MuralGold", new Color32(206, 168, 70, 255), 0.05f),
            faceSkin = SiteBuilderUtil.GetMaterial(Prefix, "FaceSkin", new Color32(210, 170, 130, 255), 0.05f),
            faceHair = SiteBuilderUtil.GetMaterial(Prefix, "FaceHair", new Color32(34, 30, 36, 255), 0.05f),
            billboard = SiteBuilderUtil.GetMaterial(Prefix, "BillboardMaroon", new Color32(120, 40, 52, 255), 0.1f),
            billboardGold = SiteBuilderUtil.GetMaterial(Prefix, "BillboardGold", new Color32(214, 176, 90, 255), 0.1f),
            billboardSkin = SiteBuilderUtil.GetMaterial(Prefix, "BillboardSkin", new Color32(224, 184, 150, 255), 0.1f),
            post = SiteBuilderUtil.GetMaterial(Prefix, "BillboardPost", new Color32(80, 84, 90, 255), 0.3f),
            palmTrunk = SiteBuilderUtil.GetMaterial(Prefix, "PalmTrunk", new Color32(150, 142, 128, 255), 0.02f),
            palmFrond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(156, 186, 74, 255), 0.05f),
            shrub = SiteBuilderUtil.GetMaterial(Prefix, "ShrubGreen", new Color32(70, 140, 60, 255), 0.05f),
        };
        var grass = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Grass.png");
        if (grass != null)
        {
            m.lawn.mainTexture = grass;
            m.lawn.mainTextureScale = new Vector2((StripMaxX - StripMinX) / 8f, 4f / 8f);
        }
        else
        {
            m.lawn.color = new Color32(96, 140, 64, 255);
        }

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        BuildGate(root.transform, m);
        // (The cell tower behind the gate was removed: the team does not want it.)
        BuildStrips(root.transform, m);
        Transform wall = BuildWall(root.transform, m);
        BuildBillboard(root.transform, m);
        // (The white car at the gate was removed: the team does not want it.)

        // The compound wall hides while it is between the camera and the avatar (the team's ShedRoofFader, unchanged; zero footprint, so it
        // is never hidden just because you stand near it).
        var fader = wall.gameObject.AddComponent<ShedRoofFader>();
        var serialized = new SerializedObject(fader);
        serialized.FindProperty("roofGroup").objectReferenceValue = wall;
        serialized.FindProperty("footprintHalfSize").vector2Value = new Vector2(0.01f, 0.01f);
        serialized.FindProperty("footprintCenter").vector2Value = new Vector2(-1000f, -1000f);
        serialized.FindProperty("margin").floatValue = 0f;
        serialized.FindProperty("boundsHeightPadding").floatValue = 0.2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT19_FrontGate: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // ------------------------------------------------------------------------------------
    // The gate
    // ------------------------------------------------------------------------------------

    static void BuildGate(Transform parent, Mats m)
    {
        Transform gate = SiteBuilderUtil.Group(parent, "Gate");
        float southPillarZ = GateCenterZ - OpeningWidth / 2f - PillarSize / 2f;
        float northPillarZ = GateCenterZ + OpeningWidth / 2f + PillarSize / 2f;

        foreach (float z in new[] { southPillarZ, northPillarZ })
        {
            SiteBuilderUtil.Part(gate, "Pillar", PrimitiveType.Cube, new Vector3(GateX, PillarHeight / 2f, z), new Vector3(PillarSize, PillarHeight, PillarSize), m.cladding, true).layer = CameraPassThroughLayer;
            SiteBuilderUtil.Part(gate, "PillarBase", PrimitiveType.Cube, new Vector3(GateX, 0.3f, z), new Vector3(PillarSize + 0.15f, 0.6f, PillarSize + 0.15f), m.claddingDark, false);
        }

        // The wide beam across the top, with a darker cap and vertical slats on the front (west) face, like the photos.
        float beamLength = northPillarZ - southPillarZ + PillarSize + 0.5f;
        SiteBuilderUtil.Part(gate, "Beam", PrimitiveType.Cube, new Vector3(GateX, PillarHeight - 0.45f, GateCenterZ), new Vector3(PillarSize, 0.9f, beamLength), m.cladding, false);
        SiteBuilderUtil.Part(gate, "BeamCap", PrimitiveType.Cube, new Vector3(GateX, PillarHeight + 0.08f, GateCenterZ), new Vector3(PillarSize + 0.3f, 0.16f, beamLength + 0.3f), m.claddingDark, false);
        int slats = Mathf.RoundToInt(beamLength / 0.45f);
        for (int i = 0; i < slats; i++)
        {
            float z = GateCenterZ - beamLength / 2f + (i + 0.5f) * beamLength / slats;
            SiteBuilderUtil.Part(gate, "Slat", PrimitiveType.Cube, new Vector3(GateX - PillarSize / 2f - 0.02f, PillarHeight - 0.45f, z), new Vector3(0.03f, 0.74f, 0.07f), m.claddingDark, false);
        }

        // The two gate leaves, closed: dark vertical bars between a top and bottom rail, with a gold ornament where they meet. They stop the
        // avatar (thin colliders) but are camera pass-through.
        float half = OpeningWidth / 2f;
        for (int side = 0; side < 2; side++)
        {
            float z0 = side == 0 ? GateCenterZ - half : GateCenterZ;
            float z1 = side == 0 ? GateCenterZ : GateCenterZ + half;
            Transform leaf = SiteBuilderUtil.Group(gate, side == 0 ? "LeafSouth" : "LeafNorth");
            float centre = (z0 + z1) / 2f;
            GameObject frame = SiteBuilderUtil.Part(leaf, "LeafCollider", PrimitiveType.Cube, new Vector3(GateX, LeafHeight / 2f, centre), new Vector3(0.1f, LeafHeight, z1 - z0), m.leaf, true);
            frame.layer = CameraPassThroughLayer;
            frame.GetComponent<MeshRenderer>().enabled = false;   // the bars below are what you see
            SiteBuilderUtil.Part(leaf, "RailTop", PrimitiveType.Cube, new Vector3(GateX, LeafHeight - 0.08f, centre), new Vector3(0.12f, 0.16f, z1 - z0), m.leaf, false);
            SiteBuilderUtil.Part(leaf, "RailBottom", PrimitiveType.Cube, new Vector3(GateX, 0.25f, centre), new Vector3(0.12f, 0.3f, z1 - z0), m.leaf, false);
            int bars = Mathf.RoundToInt((z1 - z0) / 0.18f);
            for (int i = 0; i < bars; i++)
            {
                float z = z0 + (i + 0.5f) * (z1 - z0) / bars;
                float top = LeafHeight - 0.16f;   // straight tops; keeps the leaf simple
                SiteBuilderUtil.Part(leaf, "Bar", PrimitiveType.Cube, new Vector3(GateX, (top + 0.4f) / 2f, z), new Vector3(0.05f, top - 0.4f, 0.04f), m.leaf, false);
            }
        }
        SiteBuilderUtil.Part(gate, "Ornament", PrimitiveType.Cylinder, new Vector3(GateX - 0.08f, 1.5f, GateCenterZ), new Vector3(0.9f, 0.03f, 0.9f), new Vector3(0f, 0f, 90f), m.gold, false);
    }

    // ------------------------------------------------------------------------------------
    // Grass strips, wall, billboard, car
    // ------------------------------------------------------------------------------------

    // The two grass strips laid on the road paving, narrowing it to the gate, each with slim palms; plus palms and shrubs between the road
    // and the wall.
    static void BuildStrips(Transform parent, Mats m)
    {
        Transform strips = SiteBuilderUtil.Group(parent, "RoadStrips");
        float lawnY = 0.06f;
        SiteBuilderUtil.Part(strips, "LawnNorth", PrimitiveType.Cube, new Vector3((StripMinX + StripMaxX) / 2f, lawnY / 2f, (NorthStripMinZ + NorthStripMaxZ) / 2f),
            new Vector3(StripMaxX - StripMinX, lawnY, NorthStripMaxZ - NorthStripMinZ), m.lawn, false);
        SiteBuilderUtil.Part(strips, "LawnSouth", PrimitiveType.Cube, new Vector3((StripMinX + StripMaxX) / 2f, lawnY / 2f, (SouthStripMinZ + SouthStripMaxZ) / 2f),
            new Vector3(StripMaxX - StripMinX, lawnY, SouthStripMaxZ - SouthStripMinZ), m.lawn, false);
        for (int i = 0; i < StripPalms.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(strips, "Palm_" + (i + 1), StripPalms[i][0], StripPalms[i][1], i * 53f, m.palmTrunk, m.palmFrond, 5.4f + (i % 3) * 0.4f, 3.2f, true, 0.09f);
        }
        for (int i = 0; i < WallPalms.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(strips, "WallPalm_" + (i + 1), WallPalms[i][0], WallPalms[i][1], i * 71f, m.palmTrunk, m.palmFrond, 5.8f, 3.3f, true, 0.09f);
        }
    }

    // The white compound wall: a grey base band, white upper wall, grey-brown pillars with caps every 4 m, and mural panels at the gate end.
    static Transform BuildWall(Transform parent, Mats m)
    {
        Transform wall = SiteBuilderUtil.Group(parent, "SideWall");
        float zMax = GateCenterZ - OpeningWidth / 2f - PillarSize + 0.1f;   // the gate's south pillar, from its outer face
        float length = zMax - WallMinZ;
        float centre = (zMax + WallMinZ) / 2f;

        SiteBuilderUtil.Part(wall, "Wall", PrimitiveType.Cube, new Vector3(GateX, WallHeight / 2f, centre), new Vector3(0.25f, WallHeight, length), m.wallWhite, true).layer = CameraPassThroughLayer;
        SiteBuilderUtil.Part(wall, "Base", PrimitiveType.Cube, new Vector3(GateX, 0.3f, centre), new Vector3(0.29f, 0.6f, length + 0.02f), m.wallBase, false);
        SiteBuilderUtil.Part(wall, "Coping", PrimitiveType.Cube, new Vector3(GateX, WallHeight + 0.05f, centre), new Vector3(0.4f, 0.1f, length + 0.1f), m.wallPillar, false);
        int pillars = Mathf.Max(2, Mathf.RoundToInt(length / PillarSpacing) + 1);
        for (int i = 0; i < pillars; i++)
        {
            float z = WallMinZ + i * length / (pillars - 1);
            SiteBuilderUtil.Part(wall, "Pillar", PrimitiveType.Cube, new Vector3(GateX, (WallHeight + 0.3f) / 2f, z), new Vector3(0.5f, WallHeight + 0.3f, 0.5f), m.wallPillar, false);
        }

        // The mural: painted panels on the wall's west face (the lawn side), from the gate end back about 12 m.
        float muralMaxZ = zMax - 0.4f;
        Material[] panels = { m.mural1, m.mural2, m.mural3, m.mural4 };
        float panelWidth = 2.7f;
        int count = 4;
        for (int i = 0; i < count; i++)
        {
            float z = muralMaxZ - (i + 0.5f) * panelWidth;
            float x = GateX - 0.14f;
            SiteBuilderUtil.Part(wall, "MuralPanel", PrimitiveType.Cube, new Vector3(x, 1.5f, z), new Vector3(0.03f, 1.6f, panelWidth - 0.15f), panels[i % panels.Length], false);
            // A painted face on every other panel: dark hair, a skin oval and two eyes.
            if (i % 2 == 0)
            {
                SiteBuilderUtil.Part(wall, "FaceHair", PrimitiveType.Sphere, new Vector3(x - 0.03f, 1.55f, z), new Vector3(0.04f, 1.5f, 1.3f), m.faceHair, false);
                SiteBuilderUtil.Part(wall, "Face", PrimitiveType.Sphere, new Vector3(x - 0.06f, 1.48f, z), new Vector3(0.04f, 1.15f, 0.9f), m.faceSkin, false);
                foreach (float dz in new[] { -0.2f, 0.2f })
                {
                    SiteBuilderUtil.Part(wall, "Eye", PrimitiveType.Sphere, new Vector3(x - 0.09f, 1.62f, z + dz), new Vector3(0.03f, 0.1f, 0.2f), m.mural3, false);
                }
            }
        }
        return wall;
    }

    // A tall billboard behind the wall: two steel posts and a big maroon board with a gold border and a simple face, facing the road (north).
    static void BuildBillboard(Transform parent, Mats m)
    {
        Transform board = SiteBuilderUtil.Group(parent, "Billboard");
        foreach (float dx in new[] { -2.4f, 2.4f })
        {
            SiteBuilderUtil.Part(board, "Post", PrimitiveType.Cylinder, new Vector3(BillboardX + dx, 4.5f, BillboardZ), new Vector3(0.25f, 4.5f, 0.25f), m.post, false);
        }
        SiteBuilderUtil.Part(board, "Board", PrimitiveType.Cube, new Vector3(BillboardX, 7.6f, BillboardZ), new Vector3(6.6f, 3.6f, 0.2f), m.billboard, false);
        SiteBuilderUtil.Part(board, "Border", PrimitiveType.Cube, new Vector3(BillboardX, 7.6f, BillboardZ + 0.001f), new Vector3(6.8f, 3.8f, 0.18f), m.billboardGold, false);
        SiteBuilderUtil.Part(board, "BoardFront", PrimitiveType.Cube, new Vector3(BillboardX, 7.6f, BillboardZ + 0.11f), new Vector3(6.3f, 3.3f, 0.03f), m.billboard, false);
        SiteBuilderUtil.Part(board, "Portrait", PrimitiveType.Sphere, new Vector3(BillboardX - 1.4f, 7.9f, BillboardZ + 0.14f), new Vector3(1.6f, 2.1f, 0.04f), m.billboardSkin, false);
        SiteBuilderUtil.Part(board, "Jewellery", PrimitiveType.Sphere, new Vector3(BillboardX - 1.4f, 6.8f, BillboardZ + 0.14f), new Vector3(1.9f, 0.5f, 0.04f), m.billboardGold, false);
        SiteBuilderUtil.Part(board, "TextBar", PrimitiveType.Cube, new Vector3(BillboardX + 1.4f, 8.0f, BillboardZ + 0.13f), new Vector3(2.6f, 0.45f, 0.03f), m.billboardGold, false);
        SiteBuilderUtil.Part(board, "TextBar2", PrimitiveType.Cube, new Vector3(BillboardX + 1.4f, 7.2f, BillboardZ + 0.13f), new Vector3(2.0f, 0.25f, 0.03f), m.billboardGold, false);
    }
}
