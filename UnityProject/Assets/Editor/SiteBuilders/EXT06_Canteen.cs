using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT06 canteen "blockout" model in Exterior.unity, copied from the reference photo of the
// real canteen: ONE pink block under a big red hip roof that also covers a front veranda held up by pink
// round pillars, a blue sign band along the front edge of the roof with the word CANTEEN, three windows,
// a door and a red drinks cooler at the left end; a small separate kiosk on the right; a lily pond and
// nothing but grass in front (no paths); and dark coconut palms and trees behind. (The one tall tree in front of the
// canteen is built by EXT11_WestEnd.)
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT06 Canteen
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT06_Canteen.Build -logFile ext06.log
//
// It is RE-RUNNABLE: every run deletes the old model and builds a fresh one from the constants below.
//
// What it touches: the canteen object in Exterior (named POI_Canteen the first time, then
// EXT06_Canteen). Same as the shed builders: the object is renamed, the old placeholder "Cube" under it
// is deleted, and the model is added. Its POITriggerVolume and trigger collider (poiId canteen) are left
// as they are. Unlike the shed builders it also MOVES the object to CanteenX, CanteenZ (the spot of the old
// MCA placeholder box).
//
// Layout. Units are metres, measured from the canteen object's centre; y = 0 is the ground. The front of
// the building faces -z (toward the main building).
//
//      z = -9 .. -3  lily pond          z = -3  front edge of the roof / sign band / pillars
//      z =  0        front wall         z = +8  back wall
//
// This file lives in an "Editor" folder, so it is never part of the game build. The roofs use the same
// generated hip-roof mesh as the EXT05 stage, and it needs EXT05 to have been built once first (for the
// lawn that everything stands on and the palm method).
public static class EXT06_Canteen
{
    // ------------------------------------------------------------------------------------
    // CONSTANTS (metres).
    // ------------------------------------------------------------------------------------
    const string RootName = "EXT06_Canteen";
    const string OldRootName = "POI_Canteen";
    const string ModelName = "Model";
    const string Prefix = "EXT06";

    // Where the canteen stands (world x, z). Decided by the team: on the spot where the pale MCA placeholder box
    // used to be. Set on every run; the y (height 1) of the object is left as it was. The MCA block now has its
    // own builder (EXT08_MCABlock), which places it to the west, beside the canteen.
    const float CanteenX = -41.3f;
    const float CanteenZ = 23.8f;

    // Main block (walls).
    const float BlockMinX = -9f;
    const float BlockMaxX = 9f;
    const float BlockMinZ = 0f;
    const float BlockMaxZ = 8f;
    const float WallHeight = 3.2f;
    const float PlinthHeight = 0.35f;
    const float VerandaFrontZ = -3f;        // front edge of the roof, the sign band and the pillar line
    const float VerandaFloorHeight = 0.2f;
    const float RoofOverhangSides = 1.4f;
    const float RoofBackOverhang = 1.2f;
    const float RoofRise = 3.0f;
    const int PillarCount = 6;
    const float PillarRadius = 0.2f;
    const float WindowWidth = 1.5f;
    const float WindowHeight = 1.3f;
    const float WindowY = 1.7f;             // window centre height
    static readonly float[] WindowXs = { -4.2f, 0.8f, 5.2f };
    const float SignWidth = 20.8f;
    const float SignHeight = 0.7f;

    // Kiosk (the small separate building on the right).
    const float KioskMinX = 14f;
    const float KioskMaxX = 20.5f;
    const float KioskMinZ = -2f;
    const float KioskMaxZ = 4f;
    const float KioskWallHeight = 3f;
    const float KioskRoofOverhang = 0.8f;
    const float KioskRoofRise = 1.8f;

    // Lily pond in front of the canteen (an oval) and its pads.
    const float PondX = -1f;
    const float PondZ = -9f;
    const float PondRadiusX = 7f;
    const float PondRadiusZ = 3.3f;
    const int LilyPadCount = 16;

    // Dark coconut palms and trees behind the canteen.
    const float BackMinX = -26f;
    const float BackMaxX = 30f;
    const float BackNearZ = 10f;
    const float BackFarZ = 17f;          // kept short so the trees stay clear of the MCA placeholder, which is now behind them
    const float BackSpacing = 4.5f;

    [MenuItem("Campus/Build EXT06 Canteen")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT06_Canteen", BuildInternal);
    }

    static void BuildInternal()
    {
        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        // 1. Find the canteen object: POI_Canteen the first time, EXT06_Canteen after that.
        GameObject root = SiteBuilderUtil.FindAndRenameRoot(scene, RootName, OldRootName);

        // 2. Delete the old placeholder cube and any previous model, so a re-run starts clean.
        SiteBuilderUtil.DeleteChildren(root, "Cube", ModelName);

        // Put the canteen (with its popup trigger) where the team wants it.
        root.transform.position = new Vector3(CanteenX, root.transform.position.y, CanteenZ);

        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets", "Meshes");
        SiteBuilderUtil.EnsureFolder("Assets/Meshes", "Sites");

        // 3. Materials (matt, flat colours).
        Material wall = SiteBuilderUtil.GetMaterial(Prefix, "WallPink", new Color32(236, 170, 160, 255), 0.02f);
        Material plinth = SiteBuilderUtil.GetMaterial(Prefix, "Plinth", new Color32(120, 62, 52, 255), 0.02f);
        Material pillar = SiteBuilderUtil.GetMaterial(Prefix, "PillarPink", new Color32(240, 178, 168, 255), 0.02f);
        Material roofRed = SiteBuilderUtil.GetMaterial(Prefix, "RoofRed", new Color32(150, 62, 48, 255), 0.05f);
        Material veranda = SiteBuilderUtil.GetMaterial(Prefix, "VerandaFloor", new Color32(150, 146, 140, 255), 0.02f);
        Material windowDark = SiteBuilderUtil.GetMaterial(Prefix, "WindowDark", new Color32(48, 58, 70, 255), 0.4f);
        Material frame = SiteBuilderUtil.GetMaterial(Prefix, "FrameCream", new Color32(232, 226, 206, 255), 0.05f);
        Material signBlue = SiteBuilderUtil.GetMaterial(Prefix, "SignBlue", new Color32(24, 52, 150, 255), 0.2f);
        Material signSquare = SiteBuilderUtil.GetMaterial(Prefix, "SignSquare", new Color32(70, 110, 200, 255), 0.2f);
        Material cooler = SiteBuilderUtil.GetMaterial(Prefix, "CoolerRed", new Color32(200, 38, 36, 255), 0.3f);
        Material shutter = SiteBuilderUtil.GetMaterial(Prefix, "Shutter", new Color32(150, 152, 150, 255), 0.2f);
        Material glass = SiteBuilderUtil.GetMaterial(Prefix, "Glass", new Color32(70, 112, 142, 255), 0.5f);
        Material mural = SiteBuilderUtil.GetMaterial(Prefix, "MuralWhite", new Color32(226, 224, 218, 255), 0.02f);
        Material water = SiteBuilderUtil.GetMaterial(Prefix, "PondWater", new Color32(34, 42, 46, 255), 0.95f);
        Material pad = SiteBuilderUtil.GetMaterial(Prefix, "LilyPad", new Color32(46, 96, 44, 255), 0.1f);
        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "TreeTrunk", new Color32(86, 70, 56, 255), 0.02f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeaves", new Color32(70, 124, 54, 255), 0.05f);
        Material darkLeaves = SiteBuilderUtil.GetMaterial(Prefix, "DarkLeaves", new Color32(24, 58, 28, 255), 0.05f);
        Material palmFrond = SiteBuilderUtil.GetMaterial(Prefix, "PalmFrond", new Color32(30, 78, 36, 255), 0.05f);

        // 4. The model. The root sits at y = 1, so the model is lowered by that: local y = 0 is the ground.
        Transform model = SiteBuilderUtil.CreateModel(root, ModelName);

        BuildMainBlock(model, wall, plinth, pillar, roofRed, veranda, windowDark, frame, signBlue, signSquare, cooler);
        BuildKiosk(model, wall, plinth, roofRed, shutter, glass, frame, mural);
        BuildPond(model, water, pad);
        BuildBackdrop(model, trunk, darkLeaves, palmFrond);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT06_Canteen: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    static void BuildMainBlock(Transform model, Material wall, Material plinth, Material pillar, Material roofRed,
        Material veranda, Material windowDark, Material frame, Material signBlue, Material signSquare, Material cooler)
    {
        Transform block = SiteBuilderUtil.Group(model, "MainBlock");
        float length = BlockMaxX - BlockMinX;
        float depth = BlockMaxZ - BlockMinZ;
        float centerX = (BlockMinX + BlockMaxX) / 2f;
        float centerZ = (BlockMinZ + BlockMaxZ) / 2f;

        // The walls: one solid box with a collider (you cannot walk inside the canteen), plus the plinth band.
        SiteBuilderUtil.Part(block, "Walls", PrimitiveType.Cube, new Vector3(centerX, WallHeight / 2f, centerZ),
            new Vector3(length, WallHeight, depth), wall, true);
        SiteBuilderUtil.Part(block, "Plinth", PrimitiveType.Cube, new Vector3(centerX, PlinthHeight / 2f, centerZ - 0.02f),
            new Vector3(length + 0.04f, PlinthHeight, depth + 0.04f), plinth, false);

        // Veranda floor: a low concrete step under the roof, from the wall out to the pillar line.
        float verandaDepth = BlockMinZ - VerandaFrontZ + 0.4f;
        SiteBuilderUtil.Part(block, "VerandaFloor", PrimitiveType.Cube,
            new Vector3(centerX, VerandaFloorHeight / 2f, BlockMinZ - verandaDepth / 2f),
            new Vector3(length + 1.6f, VerandaFloorHeight, verandaDepth), veranda, true);

        // Three windows with cream frames, and a door at the left end (z = BlockMinZ is the front wall).
        Transform windows = SiteBuilderUtil.Group(block, "Windows");
        for (int i = 0; i < WindowXs.Length; i++)
        {
            SiteBuilderUtil.Part(windows, "Frame_" + (i + 1), PrimitiveType.Cube,
                new Vector3(WindowXs[i], WindowY, BlockMinZ - 0.02f),
                new Vector3(WindowWidth + 0.2f, WindowHeight + 0.2f, 0.03f), frame, false);
            SiteBuilderUtil.Part(windows, "Glass_" + (i + 1), PrimitiveType.Cube,
                new Vector3(WindowXs[i], WindowY, BlockMinZ - 0.04f),
                new Vector3(WindowWidth, WindowHeight, 0.03f), windowDark, false);
        }
        SiteBuilderUtil.Part(block, "Door", PrimitiveType.Cube, new Vector3(BlockMinX + 2.2f, 1.2f, BlockMinZ - 0.03f),
            new Vector3(1.4f, 2.4f, 0.05f), windowDark, false);
        SiteBuilderUtil.Part(block, "Cooler", PrimitiveType.Cube,
            new Vector3(BlockMinX + 0.4f, VerandaFloorHeight + 0.95f, BlockMinZ - 0.5f),
            new Vector3(0.9f, 1.9f, 0.7f), cooler, true);

        // Pillars along the front of the veranda (round, pink).
        Transform pillars = SiteBuilderUtil.Group(block, "Pillars");
        float pillarHeight = WallHeight - VerandaFloorHeight;
        for (int i = 0; i < PillarCount; i++)
        {
            float x = Mathf.Lerp(BlockMinX - 0.4f, BlockMaxX + 0.4f, i / (float)(PillarCount - 1));
            SiteBuilderUtil.Part(pillars, "Pillar_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(x, VerandaFloorHeight + pillarHeight / 2f, VerandaFrontZ + 0.3f),
                new Vector3(PillarRadius * 2f, pillarHeight / 2f, PillarRadius * 2f), pillar, true);
        }

        // Blue sign band along the front edge of the roof: a dark blue strip, a row of lighter squares
        // at each end, and the word CANTEEN in white in the middle.
        Transform sign = SiteBuilderUtil.Group(block, "Sign");
        float signY = WallHeight - SignHeight / 2f + 0.15f;
        SiteBuilderUtil.Part(sign, "Band", PrimitiveType.Cube, new Vector3(centerX, signY, VerandaFrontZ),
            new Vector3(SignWidth, SignHeight, 0.1f), signBlue, false);
        for (int i = 0; i < 7; i++)
        {
            float offset = 3.1f + i * 0.95f; // squares on both sides of the lettering
            foreach (float side in new[] { -1f, 1f })
            {
                SiteBuilderUtil.Part(sign, "Square", PrimitiveType.Cube,
                    new Vector3(centerX + side * offset, signY, VerandaFrontZ - 0.055f),
                    new Vector3(0.6f, 0.45f, 0.02f), signSquare, false);
            }
        }
        BuildSignText(sign, "CANTEEN", new Vector3(centerX, signY, VerandaFrontZ - 0.08f));

        // Red hip roof, wide enough to cover the veranda: from the sign line at the front to a small
        // overhang at the back.
        float roofWidth = length + RoofOverhangSides * 2f;
        float roofFront = VerandaFrontZ;
        float roofBack = BlockMaxZ + RoofBackOverhang;
        float roofDepth = roofBack - roofFront;
        EXT05_FrontInterlock.BuildHipRoof(block, "Roof", new Vector3(centerX, WallHeight + 0.15f, (roofFront + roofBack) / 2f),
            roofWidth, roofDepth, RoofRise, Mathf.Max(0f, (roofWidth - roofDepth) / 2f), roofRed, Prefix);
    }

    // White lettering made with a TextMesh (Unity's old 3D text) using the built-in font. It reads correctly
    // for a viewer standing in front of the canteen, looking toward +z.
    static void BuildSignText(Transform parent, string text, Vector3 position)
    {
        var go = new GameObject("Lettering");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 64;
        mesh.characterSize = 0.075f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = Color.white;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    static void BuildKiosk(Transform model, Material wall, Material plinth, Material roofRed, Material shutter,
        Material glass, Material frame, Material mural)
    {
        Transform kiosk = SiteBuilderUtil.Group(model, "Kiosk");
        float width = KioskMaxX - KioskMinX;
        float depth = KioskMaxZ - KioskMinZ;
        float centerX = (KioskMinX + KioskMaxX) / 2f;
        float centerZ = (KioskMinZ + KioskMaxZ) / 2f;

        SiteBuilderUtil.Part(kiosk, "Walls", PrimitiveType.Cube, new Vector3(centerX, KioskWallHeight / 2f, centerZ),
            new Vector3(width, KioskWallHeight, depth), wall, true);
        SiteBuilderUtil.Part(kiosk, "Plinth", PrimitiveType.Cube, new Vector3(centerX, PlinthHeight / 2f, centerZ - 0.02f),
            new Vector3(width + 0.04f, PlinthHeight, depth + 0.04f), plinth, false);

        // A pale mural patch on the side facing the canteen (west wall).
        SiteBuilderUtil.Part(kiosk, "Mural", PrimitiveType.Cube, new Vector3(KioskMinX - 0.03f, 1.7f, centerZ),
            new Vector3(0.04f, 2.2f, 2.8f), mural, false);

        // Front (-z) face: rolling shutter on the left, a glass door and a window on the right.
        float frontZ = KioskMinZ - 0.03f;
        SiteBuilderUtil.Part(kiosk, "ShutterFrame", PrimitiveType.Cube, new Vector3(KioskMinX + 1.9f, 1.3f, frontZ + 0.01f),
            new Vector3(3.3f, 2.6f, 0.04f), frame, false);
        SiteBuilderUtil.Part(kiosk, "Shutter", PrimitiveType.Cube, new Vector3(KioskMinX + 1.9f, 1.25f, frontZ - 0.02f),
            new Vector3(3.0f, 2.4f, 0.04f), shutter, false);
        for (int i = 0; i < 6; i++) // the shutter's horizontal ribs
        {
            SiteBuilderUtil.Part(kiosk, "ShutterRib_" + (i + 1), PrimitiveType.Cube,
                new Vector3(KioskMinX + 1.9f, 0.3f + i * 0.4f, frontZ - 0.045f), new Vector3(3.0f, 0.03f, 0.02f), frame, false);
        }
        SiteBuilderUtil.Part(kiosk, "DoorFrame", PrimitiveType.Cube, new Vector3(KioskMaxX - 1.2f, 1.15f, frontZ + 0.01f),
            new Vector3(1.4f, 2.3f, 0.04f), frame, false);
        SiteBuilderUtil.Part(kiosk, "GlassDoor", PrimitiveType.Cube, new Vector3(KioskMaxX - 1.2f, 1.1f, frontZ - 0.02f),
            new Vector3(1.15f, 2.1f, 0.04f), glass, false);

        float roofWidth = width + KioskRoofOverhang * 2f;
        float roofDepth = depth + KioskRoofOverhang * 2f;
        EXT05_FrontInterlock.BuildHipRoof(kiosk, "Roof", new Vector3(centerX, KioskWallHeight, centerZ), roofWidth, roofDepth,
            KioskRoofRise, Mathf.Max(0f, (roofWidth - roofDepth) / 2f), roofRed, Prefix);
    }

    // A flat dark oval of water with round lily pads on it. Visual only: the avatar can walk through it.
    static void BuildPond(Transform model, Material water, Material pad)
    {
        Transform pond = SiteBuilderUtil.Group(model, "Pond");
        SiteBuilderUtil.Part(pond, "Water", PrimitiveType.Cylinder, new Vector3(PondX, 0.03f, PondZ),
            new Vector3(PondRadiusX * 2f, 0.003f, PondRadiusZ * 2f), water, false);

        var random = new System.Random(8); // fixed seed: the same pads every run
        for (int i = 0; i < LilyPadCount; i++)
        {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            float radius = Mathf.Sqrt((float)random.NextDouble()) * 0.85f;
            float size = 0.5f + (float)random.NextDouble() * 0.5f;
            SiteBuilderUtil.Part(pond, "Pad_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(PondX + Mathf.Cos(angle) * radius * PondRadiusX, 0.036f, PondZ + Mathf.Sin(angle) * radius * PondRadiusZ),
                new Vector3(size, 0.002f, size), pad, false);
        }
    }

    // The dark wall of trees behind the canteen: tall coconut palms mixed with big dark round trees.
    static void BuildBackdrop(Transform model, Material trunk, Material darkLeaves, Material palmFrond)
    {
        Transform back = SiteBuilderUtil.Group(model, "Backdrop");
        var random = new System.Random(21);
        int index = 0;
        for (float x = BackMinX; x <= BackMaxX; x += BackSpacing)
        {
            index++;
            float px = x + (float)(random.NextDouble() * 3.0 - 1.5);
            float pz = BackNearZ + (float)random.NextDouble() * (BackFarZ - BackNearZ);
            if (index % 3 == 0)
            {
                Transform tree = SiteBuilderUtil.Group(back, "Tree_" + index);
                tree.localPosition = new Vector3(px, 0f, pz);
                SiteBuilderUtil.Part(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f), new Vector3(0.6f, 2.5f, 0.6f), trunk, true);
                SiteBuilderUtil.Part(tree, "Canopy_1", PrimitiveType.Sphere, new Vector3(0f, 7f, 0f), new Vector3(7f, 6f, 7f), darkLeaves, false);
                SiteBuilderUtil.Part(tree, "Canopy_2", PrimitiveType.Sphere, new Vector3(2.2f, 6f, 1f), new Vector3(4.5f, 4f, 4.5f), darkLeaves, false);
            }
            else
            {
                float height = 9f + (float)random.NextDouble() * 5f;
                EXT05_FrontInterlock.BuildPalm(back, "Coconut_" + index, px, pz, (float)random.NextDouble() * 360f, trunk, palmFrond, height, 4.5f, false);
            }
        }
    }
}
