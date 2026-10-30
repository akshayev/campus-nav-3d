using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT08 MCA block "blockout" model in Exterior.unity, copied from the reference photos:
// a long single-storey white block with a coral-pink base band, a low grey metal roof that also covers
// a front veranda held up by thin dark maroon steel columns, rows of windows with doors between them,
// a few air-conditioner boxes, a narrow brick path in front, small
// trees on the lawn and a dark wall of coconut palms behind.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT08 MCA Block
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT08_MCABlock.Build -logFile ext08.log
//
// It is RE-RUNNABLE: every run deletes the old model and builds a fresh one from the constants below.
//
// What it touches: the MCA block object in Exterior (named POI_MCABlock the first time, then
// EXT08_MCABlock). Same as the shed builders: the object is renamed, the old placeholder "Cube" under it
// is deleted, and the model is added. Its POITriggerVolume and trigger collider (poiId mca_block, a box
// 51 x 3 x 19 m) are left as they are. Unlike the shed builders it also MOVES and TURNS the object: it is
// placed at RootX, RootZ and rotated by RootTurnY so the long wing runs north-south along the lawn, west of
// the canteen, with its veranda facing east (the trigger box turns with it and covers the building).
//
// Layout in the model's own frame (metres, y = 0 is the ground; the model's front faces -z):
//
//      z = -3     front edge of the roof and the columns        z = 0   front wall      z = +8   back wall
//      x = -24 .. +24   the length of the block (one building, 48 m)
//
// With RootTurnY = -90 the model's +x runs along world +z and the model's -z (front) faces world +x.
//
// This file lives in an "Editor" folder, so it is never part of the game build. Build EXT05 first (for the
// lawn, the roof-mesh method and the brick picture).
public static class EXT08_MCABlock
{
    // ------------------------------------------------------------------------------------
    // CONSTANTS (metres).
    // ------------------------------------------------------------------------------------
    const string RootName = "EXT08_MCABlock";
    const string OldRootName = "POI_MCABlock";
    const string ModelName = "Model";
    const string Prefix = "EXT08";

    // Where the object stands and which way it faces (world). The y (height 1) is left as it was.
    const float RootX = -61f;
    const float RootZ = 10f;
    const float RootTurnY = -90f;

    // The block.
    const float HalfLength = 24f;           // the block runs from x = -HalfLength to +HalfLength in its own frame (one 48 m building)
    const float ModelShift = 0f;            // the model can be moved along x; 0 here, so the building spans model x -24..24 = world z -14..34: it joins up with the old west-end block (EXT11, now removed) at the south and ends beside the canteen (z 23.8..31.8) at the north
    const float WallDepth = 8f;             // from the front wall (z = 0) back to the back wall
    const float WallHeight = 3.4f;
    const float PlinthHeight = 0.9f;        // the coral band at the bottom of the walls
    const float VerandaFrontZ = -3f;        // front edge of the roof, beam and columns
    const float VerandaFloorHeight = 0.15f;
    const float RoofOverhang = 1f;
    const float RoofRise = 1.5f;            // a low pitch
    const float ColumnSpacing = 4f;
    const float WindowSpacing = 3f;
    const float WindowWidth = 1.5f;
    const float WindowHeight = 1.2f;
    const float WindowY = 1.9f;
    static readonly int[] DoorSlots = { 3, 6, 9 };   // these window slots get a door instead
    static readonly float[] AirConditionerXs = { -10.5f, 1.5f, 13.5f };

    // Narrow brick path along the front of the veranda.
    const float YardDepth = 2.4f;          // a narrow brick path only (as in the photo), not a wide yard
    const float TileSize = 0.8f;

    // Dark coconut palms behind the block (model z).
    const float BackNearZ = 11f;
    const float BackFarZ = 14.5f;

    [MenuItem("Campus/Build EXT08 MCA Block")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT08_MCABlock", BuildInternal);
    }

    static void BuildInternal()
    {
        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        // 1. Find the MCA object: POI_MCABlock the first time, EXT08_MCABlock after that.
        GameObject root = SiteBuilderUtil.FindAndRenameRoot(scene, RootName, OldRootName);

        // 2. Delete the old placeholder cube and any previous model, so a re-run starts clean.
        SiteBuilderUtil.DeleteChildren(root, "Cube", ModelName);

        // Put the object (with its popup trigger) where the team wants it, turned to face east.
        root.transform.position = new Vector3(RootX, root.transform.position.y, RootZ);
        root.transform.rotation = Quaternion.Euler(0f, RootTurnY, 0f);

        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets", "Meshes");
        SiteBuilderUtil.EnsureFolder("Assets/Meshes", "Sites");

        // 3. Materials (matt, flat colours).
        Material wall = SiteBuilderUtil.GetMaterial(Prefix, "WallWhite", new Color32(232, 228, 218, 255), 0.02f);
        Material plinth = SiteBuilderUtil.GetMaterial(Prefix, "PlinthCoral", new Color32(196, 108, 92, 255), 0.02f);
        Material roof = SiteBuilderUtil.GetMaterial(Prefix, "RoofGrey", new Color32(128, 134, 140, 255), 0.1f);
        Material maroon = SiteBuilderUtil.GetMaterial(Prefix, "SteelMaroon", new Color32(92, 40, 38, 255), 0.1f);
        Material veranda = SiteBuilderUtil.GetMaterial(Prefix, "VerandaFloor", new Color32(150, 134, 124, 255), 0.02f);
        Material frame = SiteBuilderUtil.GetMaterial(Prefix, "FrameWhite", new Color32(236, 236, 232, 255), 0.05f);
        Material glass = SiteBuilderUtil.GetMaterial(Prefix, "Glass", new Color32(62, 78, 92, 255), 0.4f);
        Material door = SiteBuilderUtil.GetMaterial(Prefix, "DoorDark", new Color32(52, 40, 36, 255), 0.1f);
        Material ac = SiteBuilderUtil.GetMaterial(Prefix, "AirCon", new Color32(196, 198, 196, 255), 0.2f);
        Material steel = SiteBuilderUtil.GetMaterial(Prefix, "HoopSteel", new Color32(156, 160, 162, 255), 0.25f);
        Material board = SiteBuilderUtil.GetMaterial(Prefix, "Backboard", new Color32(214, 206, 188, 255), 0.1f);
        Material rim = SiteBuilderUtil.GetMaterial(Prefix, "Rim", new Color32(220, 100, 30, 255), 0.2f);
        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "TreeTrunk", new Color32(86, 70, 56, 255), 0.02f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeaves", new Color32(68, 120, 54, 255), 0.05f);
        Material palmFrond = SiteBuilderUtil.GetMaterial(Prefix, "PalmFrond", new Color32(30, 78, 36, 255), 0.05f);
        Material foxtail = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(158, 184, 60, 255), 0.05f);

        Material yard = SiteBuilderUtil.GetMaterial(Prefix, "YardBrick", new Color32(255, 255, 255, 255), 0.1f);
        var brick = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (brick != null)
        {
            yard.mainTexture = brick;
            yard.mainTextureScale = new Vector2(HalfLength * 2f / TileSize, YardDepth / TileSize);
        }
        else
        {
            yard.color = new Color32(124, 104, 94, 255); // run Campus > Build EXT05 Front Yard first to get the brick picture
        }

        // 4. The model. The root sits at y = 1, so the model is lowered by that: local y = 0 is the ground.
        Transform model = SiteBuilderUtil.CreateModel(root, ModelName);
        model.localPosition += new Vector3(ModelShift, 0f, 0f);

        BuildBlock(model, wall, plinth, roof, maroon, veranda, frame, glass, door, ac);
        BuildYard(model, yard, steel, board, rim);

        // (The small foxtail palms that BuildLawnTrees used to add were removed: they stood in the canteen's pond.)
        BuildPalms(model, trunk, palmFrond);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT08_MCABlock: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    static void BuildBlock(Transform model, Material wall, Material plinth, Material roof, Material maroon,
        Material veranda, Material frame, Material glass, Material door, Material ac)
    {
        Transform block = SiteBuilderUtil.Group(model, "Block");
        float length = HalfLength * 2f;

        // Walls: one solid box with a collider, plus the coral base band.
        SiteBuilderUtil.Part(block, "Walls", PrimitiveType.Cube, new Vector3(0f, WallHeight / 2f, WallDepth / 2f),
            new Vector3(length, WallHeight, WallDepth), wall, true);
        SiteBuilderUtil.Part(block, "Plinth", PrimitiveType.Cube, new Vector3(0f, PlinthHeight / 2f, WallDepth / 2f - 0.02f),
            new Vector3(length + 0.06f, PlinthHeight, WallDepth + 0.06f), plinth, false);

        // Veranda floor (a low step, with a collider) from the wall out to the column line.
        float verandaDepth = -VerandaFrontZ + 0.3f;
        SiteBuilderUtil.Part(block, "VerandaFloor", PrimitiveType.Cube,
            new Vector3(0f, VerandaFloorHeight / 2f, -verandaDepth / 2f),
            new Vector3(length + 0.6f, VerandaFloorHeight, verandaDepth), veranda, true);

        // Windows and doors along the front wall (z = 0), one per slot.
        Transform openings = SiteBuilderUtil.Group(block, "Openings");
        int slots = Mathf.RoundToInt(length / WindowSpacing);
        for (int i = 0; i < slots; i++)
        {
            float x = -HalfLength + WindowSpacing * (i + 0.5f);
            if (System.Array.IndexOf(DoorSlots, i) >= 0)
            {
                SiteBuilderUtil.Part(openings, "Door_" + (i + 1), PrimitiveType.Cube, new Vector3(x, VerandaFloorHeight + 1.05f, -0.03f),
                    new Vector3(1.1f, 2.1f, 0.05f), door, false);
                continue;
            }
            SiteBuilderUtil.Part(openings, "Frame_" + (i + 1), PrimitiveType.Cube, new Vector3(x, WindowY, -0.02f),
                new Vector3(WindowWidth + 0.15f, WindowHeight + 0.15f, 0.03f), frame, false);
            SiteBuilderUtil.Part(openings, "Glass_" + (i + 1), PrimitiveType.Cube, new Vector3(x, WindowY, -0.04f),
                new Vector3(WindowWidth, WindowHeight, 0.03f), glass, false);
        }

        // Air conditioners on the wall, high up.
        foreach (float x in AirConditionerXs)
        {
            SiteBuilderUtil.Part(block, "AirCon", PrimitiveType.Cube, new Vector3(x, 2.95f, -0.2f),
                new Vector3(0.9f, 0.5f, 0.35f), ac, false);
        }

        // Thin dark maroon columns along the front, with a beam across the top.
        Transform columns = SiteBuilderUtil.Group(block, "Columns");
        int columnCount = Mathf.RoundToInt(length / ColumnSpacing) + 1;
        for (int i = 0; i < columnCount; i++)
        {
            float x = -HalfLength + i * (length / (columnCount - 1));
            SiteBuilderUtil.Part(columns, "Column_" + (i + 1), PrimitiveType.Cube,
                new Vector3(x, VerandaFloorHeight + (WallHeight - VerandaFloorHeight) / 2f, VerandaFrontZ + 0.2f),
                new Vector3(0.2f, WallHeight - VerandaFloorHeight, 0.2f), maroon, false);
        }
        SiteBuilderUtil.Part(columns, "Beam", PrimitiveType.Cube, new Vector3(0f, WallHeight - 0.1f, VerandaFrontZ + 0.2f),
            new Vector3(length + 0.4f, 0.25f, 0.2f), maroon, false);

        BuildEndWing(block, wall, plinth, roof, frame, glass);
        BuildTruss(block, maroon);

        // Low-pitched grey metal roof over the block and the veranda.
        float roofWidth = length + RoofOverhang * 2f;
        float roofBack = WallDepth + RoofOverhang;
        float roofDepth = roofBack - VerandaFrontZ;
        EXT05_FrontInterlock.BuildHipRoof(block, "Roof", new Vector3(0f, WallHeight, (VerandaFrontZ + roofBack) / 2f),
            roofWidth, roofDepth, RoofRise, Mathf.Max(0f, (roofWidth - roofDepth) / 2f), roof, Prefix);
    }

    // The end wing at the south end (model x = -HalfLength), sticking out forward past the veranda line,
    // with a big gridded window on its end face and on its front, like the end of the real block.
    static void BuildEndWing(Transform block, Material wall, Material plinth, Material roof, Material frame, Material glass)
    {
        Transform wing = SiteBuilderUtil.Group(block, "EndWing");
        const float wingLength = 7f;
        const float wingFrontZ = -2.6f;         // flush with the veranda posts, so it reads as part of the same building
        const float wingHeight = WallHeight;    // same height as the walls: the main roof covers it, so there is one roof, not two
        float centerX = -HalfLength - 0.5f + wingLength / 2f;
        float depth = WallDepth - wingFrontZ;
        float centerZ = wingFrontZ + depth / 2f;
        SiteBuilderUtil.Part(wing, "Walls", PrimitiveType.Cube, new Vector3(centerX, wingHeight / 2f, centerZ),
            new Vector3(wingLength, wingHeight, depth), wall, true);
        SiteBuilderUtil.Part(wing, "Plinth", PrimitiveType.Cube, new Vector3(centerX, PlinthHeight / 2f, centerZ - 0.02f),
            new Vector3(wingLength + 0.06f, PlinthHeight, depth + 0.06f), plinth, false);

        // Gridded windows: four panes side by side, on the end face (-x) and on the front (-z).
        for (int i = 0; i < 4; i++)
        {
            float t = -1.9f + i * 1.25f;
            SiteBuilderUtil.Part(wing, "EndFrame_" + (i + 1), PrimitiveType.Cube, new Vector3(centerX - wingLength / 2f - 0.02f, 2.0f, centerZ + t * 1.1f),
                new Vector3(0.03f, 1.6f, 1.2f), frame, false);
            SiteBuilderUtil.Part(wing, "EndGlass_" + (i + 1), PrimitiveType.Cube, new Vector3(centerX - wingLength / 2f - 0.04f, 2.0f, centerZ + t * 1.1f),
                new Vector3(0.03f, 1.4f, 1.0f), glass, false);
            SiteBuilderUtil.Part(wing, "FrontFrame_" + (i + 1), PrimitiveType.Cube, new Vector3(centerX + t * 0.9f + 0.4f, 2.0f, wingFrontZ - 0.02f),
                new Vector3(1.0f, 1.6f, 0.03f), frame, false);
            SiteBuilderUtil.Part(wing, "FrontGlass_" + (i + 1), PrimitiveType.Cube, new Vector3(centerX + t * 0.9f + 0.4f, 2.0f, wingFrontZ - 0.04f),
                new Vector3(0.85f, 1.4f, 0.03f), glass, false);
        }
        // (No roof of its own: the building's main roof covers the wing.)
    }

    // Steel trusses under the veranda roof: at every column a horizontal chord from the wall to the beam and
    // a diagonal brace, as in the photo. Thin maroon bars, no colliders.
    static void BuildTruss(Transform block, Material maroon)
    {
        Transform truss = SiteBuilderUtil.Group(block, "Trusses");
        float length = HalfLength * 2f;
        int columnCount = Mathf.RoundToInt(length / ColumnSpacing) + 1;
        float chordY = WallHeight - 0.45f;
        float span = -VerandaFrontZ - 0.2f;
        for (int i = 0; i < columnCount; i++)
        {
            float x = -HalfLength + i * (length / (columnCount - 1));
            SiteBuilderUtil.Part(truss, "Chord_" + (i + 1), PrimitiveType.Cube, new Vector3(x, chordY, VerandaFrontZ / 2f - 0.1f),
                new Vector3(0.08f, 0.08f, span), maroon, false);
            float diagonal = Mathf.Sqrt(span * span + 0.45f * 0.45f);
            float angle = Mathf.Atan2(0.45f, span) * Mathf.Rad2Deg;
            SiteBuilderUtil.Part(truss, "Brace_" + (i + 1), PrimitiveType.Cube, new Vector3(x, chordY + 0.22f, VerandaFrontZ / 2f - 0.1f),
                new Vector3(0.06f, 0.06f, diagonal), new Vector3(-angle, 0f, 0f), maroon, false);
        }
    }

    // The narrow brick path in front of the veranda, and a puddle with stepping stones. (The truss basketball hoop
    // that stood here was removed: the team wants only grass and the pond in front of the canteen.)
    static void BuildYard(Transform model, Material yard, Material steel, Material board, Material rim)
    {
        Transform y = SiteBuilderUtil.Group(model, "Yard");
        float frontZ = VerandaFrontZ - 0.3f;
        SiteBuilderUtil.Part(y, "Paving", PrimitiveType.Cube,
            new Vector3(0f, 0.02f, frontZ - YardDepth / 2f), new Vector3(HalfLength * 2f + 1f, 0.04f, YardDepth), yard, false);

        // A puddle with three grey stepping stones beside the end wing (as in the photo).
        // (The puddle beside the end wing was removed: the team does not want blue patches on the brick.)
        Material stone = SiteBuilderUtil.GetMaterial(Prefix, "StepStone", new Color32(128, 126, 120, 255), 0.05f);
        for (int i = 0; i < 3; i++)
        {
            SiteBuilderUtil.Part(y, "StepStone_" + (i + 1), PrimitiveType.Cube, new Vector3(-HalfLength - 0.2f + i * 1.3f, 0.12f, frontZ - 1.5f - i * 0.3f),
                new Vector3(0.7f, 0.2f, 0.5f), new Vector3(0f, i * 12f, 0f), stone, false);
        }
    }

    // A few young yellow-green palms on the lawn in front of the yard.
    static void BuildLawnTrees(Transform model, Material trunk, Material leaves, Material foxtail)
    {
        Transform trees = SiteBuilderUtil.Group(model, "LawnTrees");
        // (The two big leafy trees that stood here were removed: the team wants only the one tall tree in front of the canteen, EXT11.)
        float[][] palms = { new[] { 10f, -17f }, new[] { -4f, -16f }, new[] { 16f, -21f } };
        for (int i = 0; i < palms.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(trees, "Foxtail_" + (i + 1), palms[i][0], palms[i][1], i * 53f, trunk, foxtail, 2.4f, 1.6f);
        }
    }

    // A dark wall of tall coconut palms behind the block (far from the boundary wall).
    static void BuildPalms(Transform model, Material trunk, Material palmFrond)
    {
        Transform back = SiteBuilderUtil.Group(model, "Backdrop");
        var random = new System.Random(34);
        int index = 0;
        for (float x = -HalfLength - 4f; x <= HalfLength + 4f; x += 4.2f)
        {
            index++;
            float px = x + (float)(random.NextDouble() * 2.0 - 1.0);
            float pz = BackNearZ + (float)random.NextDouble() * (BackFarZ - BackNearZ);
            float height = 9f + (float)random.NextDouble() * 4f;
            EXT05_FrontInterlock.BuildPalm(back, "Coconut_" + index, px, pz, (float)random.NextDouble() * 360f, trunk, palmFrond, height, 4.4f, false);
        }
    }
}
