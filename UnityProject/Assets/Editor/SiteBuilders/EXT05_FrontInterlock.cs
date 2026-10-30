using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT05 front yard: the area in front of the main building, as in the reference photos.
//   - a green grass lawn laid over the whole (red placeholder) ground plane
//   - a few patches of wet red earth on the lawn
//   - the brown "interlock" brick paving in front of the main building, with white painted lines
//   - palm trees (an avenue beside a brick path, plus a few near the building), flower bushes
//   - a basketball hoop, potted plants along the building front, floodlight poles and a
//     volleyball net, all copied from the reference photos in /reference
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT05 Front Yard
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT05_FrontInterlock.Build -logFile ext05.log
//
// It is RE-RUNNABLE: every run deletes the old EXT05_FrontInterlock object and builds a fresh one from
// the constants below, so you can change a number, run it again, and never get duplicates.
//
// What it touches: it ADDS one new top-level object, EXT05_FrontInterlock, to Exterior.unity, plus two
// generated textures and a few materials. Nothing that already exists in the scene is changed (the
// existing GroundPlane is NOT edited; the lawn is a thin layer laid just above it). The lawn, earth and
// paving have no colliders, so the avatar still walks on the existing GroundPlane. Palm trunks do have
// a thin collider so the avatar cannot walk through them.
//
// Layers, from the bottom (world y, metres): GroundPlane 0 / lawn 0.01 / red earth 0.02 / paving 0.03.
// The main building's front wall is at z = 3.5 and its front faces -z (toward the sheds).
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT05_FrontInterlock
{
    // ------------------------------------------------------------------------------------
    // CONSTANTS - size and look of the front yard (metres).
    // ------------------------------------------------------------------------------------
    const string RootName = "EXT05_FrontInterlock";

    // Lawn: covers the whole 160 m x 160 m ground plane.
    const float LawnHalfSize = 80f;
    const float LawnThickness = 0.01f;
    const float GrassTileSize = 8f;      // the grass texture repeats every 8 m (it has big soft patches, so it must not repeat too often)

    // Dry earth patches: x, z, radius along x, radius along z.
    static readonly float[][] EarthPatches =
    {
        new[] { -2f,  -22f, 9f,  3.5f },
        new[] { 38f,  -24f, 8f,  3f   },
        new[] { 12f,  -30f, 5f,  2.5f },
        new[] { -26f, -14f, 4f,  2.5f },
        new[] { -27f, -33f, 8f,  3f   },
        new[] { -8f,  -46f, 10f, 3.5f },
        new[] { 8f,   -36f, 6f,  2.5f },
        new[] { 52f,  -34f, 7f,  3f   },
        new[] { -42f, -48f, 6f,  3f   },
    };

    // Open-air stage (grey metal hip roof on steel poles, raised platform, steps on the front right),
    // beside the car shed. It faces +z. Position is the middle of the platform.
    const float StageX = -53.9f;         // chosen by the team in the Unity Scene view: on the west lawn, beside the brick path to the long block
    const float StageZ = -26.8f;
    const float StageWidth = 18f;        // along x
    const float StageDepth = 12f;        // along z
    const float StageHeight = 1.1f;      // platform height
    const float StageRoofHeight = 4.6f;  // underside of the roof
    const int StageSteps = 5;

    // Tall lattice floodlight tower beside the stage (as in the photos).
    const float TowerX = 55.3f;        // chosen by the team in the Unity Scene view (was 14)
    const float TowerZ = -22.6f;       // (was -30)

    // Tall coconut palms and a few trees in a belt behind the sheds, like the treeline in the photos.
    const float TreeBeltMinX = -66f;
    const float TreeBeltMaxX = 78f;
    const float TreeBeltNearZ = -64f;
    const float TreeBeltFarZ = -76f;
    const float TreeBeltSpacing = 7f;
    const float EarthThickness = 0.01f;

    // Interlock paving in front of the main building.
    const float MinX = -13f;             // west edge of the paving (the building spans about x -13..53)
    const float MaxX = 53f;              // east edge
    const float NearZ = 3.4f;            // edge next to the building's front wall
    const float FarZ = -12.6f;           // far edge, out toward the sheds
    const float PaverThickness = 0.03f;
    const float TileSize = 0.8f;         // the brick texture repeats every 0.8 m (4 bricks of 0.2 m across)

    // White painted lines on the paving.
    const int LineCount = 5;
    const float LineLength = 6f;         // along x
    const float LineWidth = 0.25f;       // along z
    const float LineSpacing = 0.9f;      // gap between line centres along z
    const float LineCenterX = 20f;       // in front of the main entrance
    const float LineFirstZ = -6f;        // z of the first line
    const float LineThickness = 0.005f;  // painted on top of the pavers

    // Palm trees: x, z. An avenue of eight along the far edge of the plaza (beside the brick path),
    // plus a few near the building and on the left lawn. Kept away from the entrance at x 20.
    static readonly float[][] PalmPositions =
    {
        new[] { -8f, -16f }, new[] { 0f,  -16f }, new[] { 8f,  -16f }, new[] { 16f, -16f },
        new[] { 24f, -16f }, new[] { 32f, -16f }, new[] { 40f, -16f },   // (the palm at 48,-16 was removed: the EXT16 brick path crosses there)
        // (The palms that stood at (-17,-6), (-30,-8) and (-24,8) were removed: that ground is now brick paving, EXT10.)
    };

    // Brick path along the palm avenue (a narrow strip of the same paving).
    const float PathNearZ = -17.5f;
    const float PathFarZ = -19.5f;

    // Potted plants in a row along the building front (skipping the entrance gap).
    const float PotZ = 2.6f;
    const float PotSpacing = 2.5f;
    const float PotGapMinX = 16f;        // no pots between these x values (the entrance)
    const float PotGapMaxX = 24f;
    const float PotGap2MinX = -14.5f;    // ... nor under the EXT07 porch and its ramp
    const float PotGap2MaxX = 9f;

    // Flower bushes (x, z, 0 = red / 1 = yellow) on the lawn beside the avenue.
    static readonly float[][] FlowerBushes =
    {
        new[] { 4f,  -23f, 0f }, new[] { 12f, -22f, 1f }, new[] { 20f, -23f, 0f }, new[] { 28f, -22f, 1f },
        new[] { 36f, -23f, 0f }, new[] { 44f, -22f, 1f }, new[] { -32f, -22f, 0f },
    };

    // Plaza lettering (centre x, z; letter size): on the main interlock, near its east end.
    const float LetterX = 38f;
    const float LetterZ = -4.6f;
    const float LetterHeight = 2.2f;     // each letter is this tall (along x) ...
    const float LetterWidth = 1.5f;      // ... and this wide (along z)
    const float LetterGap = 0.5f;        // space between letters
    const float LetterStroke = 0.35f;    // thickness of the painted strokes

    // Basketball hoop and floodlights: positions. (The volleyball court is built by EXT09_StageYard.)
    const float HoopX = 42f;
    const float HoopZ = -2f;
    static readonly float[][] FloodlightPositions = { new[] { -32f, -6f }, new[] { 66f, -14f } };
    const float TrunkHeight = 6f;
    const float TrunkRadius = 0.18f;
    const int FrondCount = 8;
    const float FrondLength = 3.2f;
    const float FrondWidth = 0.55f;
    const float FrondDroop = 22f;        // degrees the fronds hang down

    // Texture settings.
    const int BrickWidthPx = 64;
    const int BrickHeightPx = 32;
    const int BricksAcross = 4;
    const int BrickRows = 8;
    const int MortarPx = 2;
    const int GrassPx = 256;

    const string TextureFolder = "Assets/Textures/Sites";
    const string BrickTexturePath = TextureFolder + "/EXT05_Interlock.png";
    const string GrassTexturePath = TextureFolder + "/EXT05_Grass.png";

    [MenuItem("Campus/Build EXT05 Front Yard")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT05_FrontInterlock", BuildInternal);
    }

    static void BuildInternal()
    {
        if (!SiteBuilderUtil.TryOpenExteriorScene(out Scene scene))
        {
            return;
        }

        // Start clean: remove the object from a previous run, if any.
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == RootName)
            {
                Object.DestroyImmediate(candidate);
            }
        }

        SiteBuilderUtil.EnsureFolder("Assets", "Textures");
        SiteBuilderUtil.EnsureFolder("Assets/Textures", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets", "Meshes");
        SiteBuilderUtil.EnsureFolder("Assets/Meshes", "Sites");

        Texture2D brickTexture = MakeBrickTexture();
        Texture2D grassTexture = MakeGrassTexture();

        float lawnSize = LawnHalfSize * 2f;
        Material grass = SiteBuilderUtil.GetMaterial("EXT05", "Grass", new Color32(255, 255, 255, 255), 0.05f);
        grass.mainTexture = grassTexture;
        grass.mainTextureScale = new Vector2(lawnSize / GrassTileSize, lawnSize / GrassTileSize);

        float width = MaxX - MinX;
        float depth = NearZ - FarZ;
        Material paving = SiteBuilderUtil.GetMaterial("EXT05", "Interlock", new Color32(255, 255, 255, 255), 0.1f);
        paving.mainTexture = brickTexture;
        paving.mainTextureScale = new Vector2(width / TileSize, depth / TileSize);

        Material paint = SiteBuilderUtil.GetMaterial("EXT05", "WhitePaint", new Color32(235, 235, 230, 255), 0.1f);
        Material earth = SiteBuilderUtil.GetMaterial("EXT05", "RedEarth", new Color32(138, 84, 56, 255), 0.02f);
        Material trunk = SiteBuilderUtil.GetMaterial("EXT05", "PalmTrunk", new Color32(110, 90, 70, 255), 0.05f);
        Material frond = SiteBuilderUtil.GetMaterial("EXT05", "PalmFrond", new Color32(60, 130, 55, 255), 0.1f);
        Material foxtail = SiteBuilderUtil.GetMaterial("EXT05", "FoxtailGrey", new Color32(156, 186, 74, 255), 0.05f);   // pale yellow-green, like the reference palms
        Material cage = SiteBuilderUtil.GetMaterial("EXT05", "WireCage", new Color32(24, 28, 26, 255), 0.2f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // Lawn: one big thin slab over the ground plane. No collider (visual only).
        SiteBuilderUtil.Part(root.transform, "Lawn", PrimitiveType.Cube,
            new Vector3(0f, LawnThickness / 2f, 0f),
            new Vector3(lawnSize, LawnThickness, lawnSize), grass, false);

        // Red earth patches, flat ovals sitting on the lawn.
        Transform patches = SiteBuilderUtil.Group(root.transform, "EarthPatches");
        for (int i = 0; i < EarthPatches.Length; i++)
        {
            float[] p = EarthPatches[i];
            SiteBuilderUtil.Part(patches, "Earth_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(p[0], LawnThickness + EarthThickness / 2f, p[1]),
                new Vector3(p[2] * 2f, EarthThickness / 2f, p[3] * 2f), earth, false);
        }

        // Interlock paving: one slab; the texture tiles across it.
        SiteBuilderUtil.Part(root.transform, "Paving", PrimitiveType.Cube,
            new Vector3((MinX + MaxX) / 2f, PaverThickness / 2f, (NearZ + FarZ) / 2f),
            new Vector3(width, PaverThickness, depth), paving, false);

        // (The rain puddles on the paving were removed: the team does not want the blue patches on the interlock.)

        Transform lines = SiteBuilderUtil.Group(root.transform, "PaintedLines");
        for (int i = 0; i < LineCount; i++)
        {
            SiteBuilderUtil.Part(lines, "Line_" + (i + 1), PrimitiveType.Cube,
                new Vector3(LineCenterX, PaverThickness + LineThickness / 2f, LineFirstZ - i * LineSpacing),
                new Vector3(LineLength, LineThickness, LineWidth), paint, false);
        }

        Transform palms = SiteBuilderUtil.Group(root.transform, "PalmTrees");
        for (int i = 0; i < PalmPositions.Length; i++)
        {
            BuildCagedFoxtail(palms, "Palm_" + (i + 1), PalmPositions[i][0], PalmPositions[i][1], i * 47f, trunk, foxtail, cage);
        }

        // (The narrow brick path along the palm avenue was removed: the path and its plants now run to the main entrance, EXT07.)

        Material terracotta = SiteBuilderUtil.GetMaterial("EXT05", "Terracotta", new Color32(190, 98, 62, 255), 0.1f);
        Material steel = SiteBuilderUtil.GetMaterial("EXT05", "DarkSteel", new Color32(60, 64, 70, 255), 0.3f);
        Material white = SiteBuilderUtil.GetMaterial("EXT05", "BoardWhite", new Color32(235, 235, 232, 255), 0.2f);
        Material orange = SiteBuilderUtil.GetMaterial("EXT05", "RimOrange", new Color32(230, 110, 30, 255), 0.2f);
        Material flowerRed = SiteBuilderUtil.GetMaterial("EXT05", "FlowerRed", new Color32(212, 44, 36, 255), 0.1f);
        Material flowerYellow = SiteBuilderUtil.GetMaterial("EXT05", "FlowerYellow", new Color32(236, 200, 40, 255), 0.1f);

        // (The basketball hoop on the plaza was removed: the team wants nothing standing on the brick.)

        BuildPlazaLettering(root.transform, "SATTVA", LetterX, LetterZ);

        // (The row of potted plants along the building front was removed: the team wants no plants on the brick.)

        // (The lawn flower bushes were removed: the plants now line the path to the main entrance, EXT07.)

        Transform lights = SiteBuilderUtil.Group(root.transform, "Floodlights");
        for (int i = 0; i < FloodlightPositions.Length; i++)
        {
            float x = FloodlightPositions[i][0];
            float z = FloodlightPositions[i][1];
            SiteBuilderUtil.Part(lights, "Pole_" + (i + 1), PrimitiveType.Cylinder, new Vector3(x, 4.5f, z),
                new Vector3(0.3f, 4.5f, 0.3f), steel, true);
            SiteBuilderUtil.Part(lights, "Lamp_" + (i + 1), PrimitiveType.Cube, new Vector3(x, 9.2f, z),
                new Vector3(1.8f, 0.8f, 1.2f), steel, false);
        }

        // Matt colours (low smoothness): shiny ones reflected the sky and looked pale blue-white.
        Material stageRoof = SiteBuilderUtil.GetMaterial("EXT05", "StageRoofGrey", new Color32(84, 90, 98, 255), 0.02f);
        Material stageFloor = SiteBuilderUtil.GetMaterial("EXT05", "StageConcrete", new Color32(150, 146, 138, 255), 0.02f);
        Material stageWall = SiteBuilderUtil.GetMaterial("EXT05", "StageStone", new Color32(138, 133, 124, 255), 0.02f);
        BuildStage(root.transform, stageRoof, stageFloor, stageWall, steel);

        Material lampGlow = SiteBuilderUtil.GetMaterial("EXT05", "TowerLamp", new Color32(150, 150, 140, 255), 0.1f);
        BuildLadderTower(root.transform, "FloodlightTower", TowerX, TowerZ, steel, lampGlow);

        Material canopy = SiteBuilderUtil.GetMaterial("EXT05", "TreeCanopy", new Color32(52, 98, 44, 255), 0.05f);
        BuildTreeBelt(root.transform, trunk, frond, canopy);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT05_FrontInterlock: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // One palm tree: a thin trunk (with a collider) and a ring of drooping flat fronds on top.
    // Height and frond length have defaults for the ordinary palms; the tall coconut palms pass bigger ones.
    // A slim foxtail palm standing in a black wire cage, like the palms along the plaza edge in the photo: a tall
    // slender trunk, a crown of long drooping pale-green fronds, and a round cage (a few rings and vertical bars,
    // no collider) around the lower trunk.
    internal static void BuildCagedFoxtail(Transform parent, string name, float x, float z, float turnDegrees, Material trunk, Material frond, Material cage)
    {
        BuildPalm(parent, name, x, z, turnDegrees, trunk, frond, 6.4f, 3.8f, true, 0.09f);   // slim trunk, like the reference photo
        Transform cageGroup = SiteBuilderUtil.Group(parent.Find(name), "Cage");
        const float radius = 0.55f;
        const float height = 1.9f;
        for (int i = 0; i < 3; i++)
        {
            SiteBuilderUtil.Part(cageGroup, "Ring_" + (i + 1), PrimitiveType.Cylinder, new Vector3(0f, 0.2f + i * 0.85f, 0f),
                new Vector3(radius * 2f, 0.015f, radius * 2f), cage, false);
        }
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            SiteBuilderUtil.Part(cageGroup, "Bar_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(Mathf.Cos(angle) * radius, height / 2f, Mathf.Sin(angle) * radius), new Vector3(0.025f, height / 2f, 0.025f), cage, false);
        }
    }

    // 'detailed' = true gives each frond two arched pieces (rises, then hangs down) and more fronds, which
    // looks like a real palm; false keeps the cheap single flat piece (used for the far treeline).
    internal static void BuildPalm(Transform parent, string name, float x, float z, float turnDegrees, Material trunk, Material frond,
        float height = TrunkHeight, float frondLength = FrondLength, bool detailed = true, float trunkRadius = TrunkRadius)
    {
        Transform palm = SiteBuilderUtil.Group(parent, name);
        palm.localPosition = new Vector3(x, 0f, z);
        palm.localRotation = Quaternion.Euler(0f, turnDegrees, 0f);

        // A cylinder is 2 m tall at scale 1, so half the height is the y scale.
        SiteBuilderUtil.Part(palm, "Trunk", PrimitiveType.Cylinder,
            new Vector3(0f, height / 2f, 0f),
            new Vector3(trunkRadius * 2f, height / 2f, trunkRadius * 2f), trunk, true).layer = 2;   // layer 2: the follow camera's collision ray ignores it (the avatar still bumps into it)

        Transform crown = SiteBuilderUtil.Group(palm, "Crown");
        crown.localPosition = new Vector3(0f, height, 0f);

        int count = detailed ? 11 : FrondCount;
        for (int i = 0; i < count; i++)
        {
            Transform pivot = SiteBuilderUtil.Group(crown, "Frond_" + (i + 1));
            pivot.localRotation = Quaternion.Euler(0f, i * 360f / count, 0f);
            if (!detailed)
            {
                SiteBuilderUtil.Part(pivot, "Leaf", PrimitiveType.Cube,
                    new Vector3(frondLength / 2f, 0f, 0f),
                    new Vector3(frondLength, 0.04f, FrondWidth),
                    new Vector3(0f, 0f, -FrondDroop), frond, false);
                continue;
            }

            // Inner half rises a little, the outer half (the tip) hangs down: an arch.
            float inner = frondLength * 0.55f;
            float outer = frondLength * 0.5f;
            float rise = 12f * Mathf.Deg2Rad;
            float lean = (i % 3) * 6f; // a little variety: some fronds droop more
            SiteBuilderUtil.Part(pivot, "Leaf_Inner", PrimitiveType.Cube,
                new Vector3(inner / 2f * Mathf.Cos(rise), inner / 2f * Mathf.Sin(rise), 0f),
                new Vector3(inner, 0.04f, FrondWidth * 0.8f),
                new Vector3(0f, 0f, 12f), frond, false);
            Transform tip = SiteBuilderUtil.Group(pivot, "Tip");
            tip.localPosition = new Vector3(inner * Mathf.Cos(rise), inner * Mathf.Sin(rise), 0f);
            tip.localRotation = Quaternion.Euler(0f, 0f, -(35f + lean));
            SiteBuilderUtil.Part(tip, "Leaf_Outer", PrimitiveType.Cube,
                new Vector3(outer / 2f, 0f, 0f),
                new Vector3(outer, 0.04f, FrondWidth * 0.55f), frond, false);
        }
    }

    // Open-air stage: a raised stone-faced platform, steel poles all round, a stepped grey metal roof,
    // and a short flight of steps (with a rail) at the front right. The platform and steps have colliders
    // so the avatar can walk up the steps and onto the stage.
    static void BuildStage(Transform parent, Material roof, Material floor, Material stone, Material steel)
    {
        Transform s = SiteBuilderUtil.Group(parent, "Stage");
        s.localPosition = new Vector3(StageX, 0f, StageZ);

        SiteBuilderUtil.Part(s, "Platform", PrimitiveType.Cube, new Vector3(0f, StageHeight / 2f, 0f),
            new Vector3(StageWidth, StageHeight, StageDepth), stone, true);
        SiteBuilderUtil.Part(s, "PlatformTop", PrimitiveType.Cube, new Vector3(0f, StageHeight + 0.02f, 0f),
            new Vector3(StageWidth + 0.2f, 0.04f, StageDepth + 0.2f), floor, false);

        // Poles: five along the front and back, standing on the platform edge.
        for (int i = 0; i < 5; i++)
        {
            float px = -StageWidth / 2f + 0.5f + i * ((StageWidth - 1f) / 4f);
            foreach (float pz in new[] { -StageDepth / 2f + 0.4f, StageDepth / 2f - 0.4f })
            {
                float poleHeight = StageRoofHeight - StageHeight;
                SiteBuilderUtil.Part(s, "Pole", PrimitiveType.Cylinder,
                    new Vector3(px, StageHeight + poleHeight / 2f, pz),
                    new Vector3(0.14f, poleHeight / 2f, 0.14f), steel, true);
            }
        }

        // Real sloping hip roof: four sloping faces meeting at a short ridge, wider than the platform.
        BuildHipRoof(s, "Roof", new Vector3(0f, StageRoofHeight, 0f), StageWidth + 2.4f, StageDepth + 2.4f, 3.0f, 1.4f, roof); // steeper, like the real roof

        // Railings round the back and both ends, and along the front except where the steps are.
        float railTop = StageHeight + 0.9f;
        float frontRailEnd = StageWidth / 2f - 2.6f; // the steps start here
        BuildRail(s, "Rail_Back", new Vector3(0f, 0f, -StageDepth / 2f + 0.1f), StageWidth, true, railTop, steel);
        BuildRail(s, "Rail_West", new Vector3(-StageWidth / 2f + 0.1f, 0f, 0f), StageDepth, false, railTop, steel);
        BuildRail(s, "Rail_East", new Vector3(StageWidth / 2f - 0.1f, 0f, 0f), StageDepth, false, railTop, steel);
        BuildRail(s, "Rail_Front", new Vector3((-StageWidth / 2f + frontRailEnd) / 2f, 0f, StageDepth / 2f - 0.1f),
            frontRailEnd + StageWidth / 2f, true, railTop, steel);

        // Steps on the front (+z) side, near the right end. Each step is a little higher and further in.
        float stepRun = 0.4f;
        float stepRise = StageHeight / StageSteps;
        float stepX = StageWidth / 2f - 1.4f;
        for (int i = 0; i < StageSteps; i++)
        {
            float height = stepRise * (i + 1);
            SiteBuilderUtil.Part(s, "Step_" + (i + 1), PrimitiveType.Cube,
                new Vector3(stepX, height / 2f, StageDepth / 2f + stepRun * (StageSteps - i - 0.5f)),
                new Vector3(2f, height, stepRun), floor, true);
        }
        foreach (float rx in new[] { stepX - 1.05f, stepX + 1.05f })
        {
            SiteBuilderUtil.Part(s, "Rail", PrimitiveType.Cube,
                new Vector3(rx, StageHeight + 0.1f, StageDepth / 2f + stepRun * StageSteps / 2f),
                new Vector3(0.05f, 0.05f, stepRun * StageSteps), steel, false);
        }
    }

    // A belt of tall coconut palms (and a few round trees) behind the car shed and bike shed. Random but
    // with a fixed seed, so every run gives the same layout.
    static void BuildTreeBelt(Transform parent, Material trunk, Material frond, Material canopy)
    {
        Transform belt = SiteBuilderUtil.Group(parent, "TreeBelt");
        var random = new System.Random(3);
        int index = 0;
        for (float x = TreeBeltMinX; x <= TreeBeltMaxX; x += TreeBeltSpacing)
        {
            index++;
            float px = x + (float)(random.NextDouble() * 4.0 - 2.0);
            float pz = TreeBeltFarZ + (float)random.NextDouble() * (TreeBeltNearZ - TreeBeltFarZ);
            if (index % 4 == 0)
            {
                // A round tree: a trunk with a clump of dark green balls on top.
                Transform tree = SiteBuilderUtil.Group(belt, "Tree_" + index);
                tree.localPosition = new Vector3(px, 0f, pz);
                SiteBuilderUtil.Part(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f),
                    new Vector3(0.5f, 2f, 0.5f), trunk, true);
                SiteBuilderUtil.Part(tree, "Canopy_1", PrimitiveType.Sphere, new Vector3(0f, 5.5f, 0f),
                    new Vector3(5.5f, 4.5f, 5.5f), canopy, false);
                SiteBuilderUtil.Part(tree, "Canopy_2", PrimitiveType.Sphere, new Vector3(1.6f, 4.8f, 0.8f),
                    new Vector3(3.5f, 3f, 3.5f), canopy, false);
            }
            else
            {
                float height = 9f + (float)random.NextDouble() * 4f;
                BuildPalm(belt, "Coconut_" + index, px, pz, (float)random.NextDouble() * 360f, trunk, frond, height, 4.2f, false);
            }
        }
    }

    // A real hip roof made from a generated mesh: the eave rectangle (width x depth) sits at 'eave', and
    // the four sloping faces rise by 'rise' to a ridge that is 2 x ridgeHalfLength long (a pyramid when
    // that is 0). Every triangle is added facing both ways so the roof is solid seen from above or below.
    // The mesh is saved as an asset (Assets/Meshes/Sites) and has no collider.
    // 'prefix' names the saved mesh file (EXT06_Canteen builds its roofs with the same method).
    internal static void BuildHipRoof(Transform parent, string name, Vector3 eave, float width, float depth, float rise, float ridgeHalfLength, Material material, string prefix = "EXT05")
    {
        float hw = width / 2f;
        float hd = depth / 2f;
        var a = new Vector3(-hw, 0f, -hd);
        var b = new Vector3(hw, 0f, -hd);
        var c = new Vector3(hw, 0f, hd);
        var d = new Vector3(-hw, 0f, hd);
        var r1 = new Vector3(-ridgeHalfLength, rise, 0f);
        var r2 = new Vector3(ridgeHalfLength, rise, 0f);

        // Each face as triangles (a ridge of zero length just makes two thin triangles, which is fine).
        Vector3[][] triangles =
        {
            new[] { a, r1, r2 }, new[] { a, r2, b },     // front slope (-z)
            new[] { c, r2, r1 }, new[] { c, r1, d },     // back slope (+z)
            new[] { b, r2, c },                          // east end (+x)
            new[] { d, r1, a },                          // west end (-x)
        };

        var vertices = new System.Collections.Generic.List<Vector3>();
        var indices = new System.Collections.Generic.List<int>();
        foreach (Vector3[] t in triangles)
        {
            int start = vertices.Count;
            vertices.AddRange(t);                        // one side
            indices.AddRange(new[] { start, start + 1, start + 2 });
            vertices.AddRange(new[] { t[0], t[2], t[1] }); // the same triangle, other side
            indices.AddRange(new[] { start + 3, start + 4, start + 5 });
        }

        var mesh = new Mesh { name = prefix + "_" + parent.name + "_" + name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = "Assets/Meshes/Sites/" + prefix + "_" + parent.name + "_" + name + ".asset";
        AssetDatabase.DeleteAsset(path); // replace the old file of a previous run
        AssetDatabase.CreateAsset(mesh, path);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = eave;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    // A railing: two horizontal bars and posts every 2 m. 'length' runs along x if alongX, else along z.
    // 'center' is the middle of the railing; its y is ignored (the bars use topY and half of it).
    static void BuildRail(Transform parent, string name, Vector3 center, float length, bool alongX, float topY, Material steel)
    {
        Transform rail = SiteBuilderUtil.Group(parent, name);
        foreach (float y in new[] { topY, topY * 0.55f })
        {
            Vector3 size = alongX ? new Vector3(length, 0.05f, 0.05f) : new Vector3(0.05f, 0.05f, length);
            SiteBuilderUtil.Part(rail, "Bar", PrimitiveType.Cube, new Vector3(center.x, y, center.z), size, steel, false);
        }
        int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float t = -length / 2f + i * length / (posts - 1);
            Vector3 position = alongX ? new Vector3(center.x + t, topY / 2f, center.z) : new Vector3(center.x, topY / 2f, center.z + t);
            SiteBuilderUtil.Part(rail, "Post", PrimitiveType.Cylinder, position, new Vector3(0.06f, topY / 2f, 0.06f), steel, false);
        }
    }

    // The big tower beside the volleyball courts, copied from the reference photos: a straight, narrow ladder-like
    // column (two rails with rungs and diagonal braces), a lamp platform on top roofed with a tilted sheet of corrugated
    // tin, and a red and white banner tied across its base. Visual only (no colliders).
    internal static void BuildLadderTower(Transform parent, string name, float x, float z, Material steel, Material lamp)
    {
        Transform t = SiteBuilderUtil.Group(parent, name);
        t.localPosition = new Vector3(x, 0f, z);
        const float height = 12f;
        const float halfWidth = 0.55f;
        Material tin = SiteBuilderUtil.GetMaterial("EXT05", "TowerTin", new Color32(150, 156, 160, 255), 0.3f);
        Material bannerWhite = SiteBuilderUtil.GetMaterial("EXT05", "BannerWhite", new Color32(236, 236, 232, 255), 0.05f);
        Material bannerRed = SiteBuilderUtil.GetMaterial("EXT05", "BannerRed", new Color32(200, 40, 44, 255), 0.05f);

        // Four straight rails, with a rung across every 0.8 m on the two long faces and a diagonal in each bay.
        foreach (float sx in new[] { -1f, 1f })
        {
            foreach (float sz in new[] { -1f, 1f })
            {
                SiteBuilderUtil.Part(t, "Rail", PrimitiveType.Cylinder, new Vector3(sx * halfWidth, height / 2f, sz * halfWidth * 0.6f),
                    new Vector3(0.1f, height / 2f, 0.1f), steel, false);
            }
        }
        for (float y = 0.6f; y < height; y += 0.8f)
        {
            SiteBuilderUtil.Part(t, "Rung", PrimitiveType.Cube, new Vector3(0f, y, halfWidth * 0.6f), new Vector3(halfWidth * 2f, 0.05f, 0.05f), steel, false);
            SiteBuilderUtil.Part(t, "Rung", PrimitiveType.Cube, new Vector3(0f, y, -halfWidth * 0.6f), new Vector3(halfWidth * 2f, 0.05f, 0.05f), steel, false);
            GameObject brace = SiteBuilderUtil.Part(t, "Brace", PrimitiveType.Cube, new Vector3(0f, y + 0.4f, halfWidth * 0.6f),
                new Vector3(halfWidth * 2.2f, 0.04f, 0.04f), steel, false);
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, ((int)(y / 0.8f) % 2 == 0) ? 36f : -36f);
        }

        // Lamp platform and the tilted sheet of tin on top, with a small lamp underneath.
        SiteBuilderUtil.Part(t, "Platform", PrimitiveType.Cube, new Vector3(0f, height + 0.1f, 0f), new Vector3(1.6f, 0.12f, 1.2f), steel, false);
        SiteBuilderUtil.Part(t, "Lamp", PrimitiveType.Cube, new Vector3(0f, height + 0.5f, 0f), new Vector3(0.9f, 0.4f, 0.7f), lamp, false);
        SiteBuilderUtil.Part(t, "TinRoof", PrimitiveType.Cube, new Vector3(0.2f, height + 1.2f, 0f), new Vector3(3.4f, 0.07f, 2.3f),
            new Vector3(0f, 0f, 14f), tin, false);

        // The banner across the base: white board with a red band at the bottom, on the -z face (toward the volleyball courts).
        SiteBuilderUtil.Part(t, "Banner", PrimitiveType.Cube, new Vector3(0f, 2.4f, -halfWidth * 0.6f - 0.12f), new Vector3(2.2f, 1.2f, 0.04f), bannerWhite, false);
        SiteBuilderUtil.Part(t, "BannerBand", PrimitiveType.Cube, new Vector3(0f, 2.0f, -halfWidth * 0.6f - 0.15f), new Vector3(2.2f, 0.4f, 0.03f), bannerRed, false);
    }

    // A tall lattice floodlight tower: four legs that lean in toward the top, horizontal frames every 4 m,
    // and a lamp head. Visual only, like the towers in the photos.
    internal static void BuildTower(Transform parent, string name, float x, float z, Material steel, Material lamp)
    {
        Transform t = SiteBuilderUtil.Group(parent, name);
        t.localPosition = new Vector3(x, 0f, z);
        const float height = 14f;          // slender like the towers in the reference photo
        const float baseHalf = 0.55f;
        const float topHalf = 0.3f;
        foreach (float sx in new[] { -1f, 1f })
        {
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 p0 = new Vector3(sx * baseHalf, 0f, sz * baseHalf);
                Vector3 p1 = new Vector3(sx * topHalf, height, sz * topHalf);
                Vector3 direction = p1 - p0;
                GameObject leg = SiteBuilderUtil.Part(t, "Leg", PrimitiveType.Cylinder, (p0 + p1) / 2f,
                    new Vector3(0.08f, direction.magnitude / 2f, 0.08f), steel, false);
                leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
            }
        }
        for (float y = 3f; y < height; y += 3f)
        {
            float half = Mathf.Lerp(baseHalf, topHalf, y / height);
            SiteBuilderUtil.Part(t, "Frame_X1", PrimitiveType.Cube, new Vector3(0f, y, half), new Vector3(half * 2f, 0.07f, 0.07f), steel, false);
            SiteBuilderUtil.Part(t, "Frame_X2", PrimitiveType.Cube, new Vector3(0f, y, -half), new Vector3(half * 2f, 0.07f, 0.07f), steel, false);
            SiteBuilderUtil.Part(t, "Frame_Z1", PrimitiveType.Cube, new Vector3(half, y, 0f), new Vector3(0.07f, 0.07f, half * 2f), steel, false);
            SiteBuilderUtil.Part(t, "Frame_Z2", PrimitiveType.Cube, new Vector3(-half, y, 0f), new Vector3(0.07f, 0.07f, half * 2f), steel, false);
        }
        SiteBuilderUtil.Part(t, "Platform", PrimitiveType.Cube, new Vector3(0f, height + 0.1f, 0f), new Vector3(1.1f, 0.15f, 1.1f), steel, false);
        SiteBuilderUtil.Part(t, "LampHead", PrimitiveType.Cube, new Vector3(0f, height + 0.4f, 0f), new Vector3(1.7f, 0.4f, 1.7f), lamp, false);
    }

    // The big white letters painted on the main interlock, like the lettering in the reference photos. Each letter is drawn from
    // flat white bars that lie ON the brick (1.2 cm thick, no collider), so they are real solid geometry like the paving: walls and
    // characters hide them correctly, and they cannot show through the building. (An earlier version used Unity's old TextMesh, whose
    // shader ignores depth, so the word showed through walls and through the avatar.)
    // The text runs north along the plaza (+z) and its tops point west (-x), so it reads correctly to someone standing at the east
    // end of the plaza looking west.
    static void BuildPlazaLettering(Transform parent, string text, float x, float z)
    {
        Transform word = SiteBuilderUtil.Group(parent, "PlazaLettering");
        Material paint = SiteBuilderUtil.GetMaterial("EXT05", "PlazaPaint", new Color32(238, 236, 228, 255), 0.02f);

        float total = text.Length * LetterWidth + (text.Length - 1) * LetterGap;
        float u = -total / 2f;     // along the text (-> world +z), measured from the middle of the word
        for (int i = 0; i < text.Length; i++)
        {
            Transform letter = SiteBuilderUtil.Group(word, "Letter_" + text[i] + "_" + (i + 1));
            AddLetter(letter, paint, text[i], x, z, u);
            u += LetterWidth + LetterGap;
        }
    }

    // Draws one block letter (S, A, T or V) as bars. (u, v) is a point on the letter: u across (0 .. LetterWidth), v up (0 .. LetterHeight).
    static void AddLetter(Transform parent, Material paint, char letter, float x, float z, float u0)
    {
        float w = LetterWidth;
        float h = LetterHeight;
        switch (letter)
        {
            case 'S':
                Stroke(parent, paint, x, z, u0 + 0f, h, u0 + w, h);                  // top
                Stroke(parent, paint, x, z, u0 + 0f, h, u0 + 0f, h / 2f);            // upper left
                Stroke(parent, paint, x, z, u0 + 0f, h / 2f, u0 + w, h / 2f);        // middle
                Stroke(parent, paint, x, z, u0 + w, h / 2f, u0 + w, 0f);             // lower right
                Stroke(parent, paint, x, z, u0 + 0f, 0f, u0 + w, 0f);                // bottom
                break;
            case 'A':
                Stroke(parent, paint, x, z, u0 + 0f, 0f, u0 + 0f, h);                // left
                Stroke(parent, paint, x, z, u0 + w, 0f, u0 + w, h);                  // right
                Stroke(parent, paint, x, z, u0 + 0f, h, u0 + w, h);                  // top
                Stroke(parent, paint, x, z, u0 + 0f, h / 2f, u0 + w, h / 2f);        // middle
                break;
            case 'T':
                Stroke(parent, paint, x, z, u0 + 0f, h, u0 + w, h);                  // top
                Stroke(parent, paint, x, z, u0 + w / 2f, 0f, u0 + w / 2f, h);        // stem
                break;
            case 'V':
                Stroke(parent, paint, x, z, u0 + 0f, h, u0 + w / 2f, 0f);            // left slope
                Stroke(parent, paint, x, z, u0 + w, h, u0 + w / 2f, 0f);             // right slope
                break;
            default:
                throw new System.Exception("AddLetter: no shape for the letter '" + letter + "'");
        }
    }

    // One painted bar from (u1, v1) to (u2, v2) (the ends are made a stroke-width longer so bars overlap at the corners).
    // u runs along world +z, v runs along world -x; the bar lies flat on the plaza paving (top at y 0.03).
    static void Stroke(Transform parent, Material paint, float x, float z, float u1, float v1, float u2, float v2)
    {
        float du = u2 - u1;
        float dv = v2 - v1;
        float length = Mathf.Sqrt(du * du + dv * dv) + LetterStroke;
        float centerU = (u1 + u2) / 2f;
        float centerV = (v1 + v2) / 2f;
        // v (up the letter) maps to world -x, u (along the word) to world +z; so a direction (du, dv) is (-dv, du) in (x, z).
        float yaw = Mathf.Atan2(-dv, du) * Mathf.Rad2Deg;
        GameObject bar = SiteBuilderUtil.Part(parent, "Bar", PrimitiveType.Cube,
            new Vector3(x - (centerV - LetterHeight / 2f), 0.036f, z + centerU),
            new Vector3(LetterStroke, 0.012f, length), paint, false);
        bar.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // Basketball hoop on a steel pole, with a white backboard and an orange rim, facing the building.
    static void BuildHoop(Transform parent, Material steel, Material board, Material rim)
    {
        Transform h = SiteBuilderUtil.Group(parent, "BasketballHoop");
        h.localPosition = new Vector3(HoopX, 0f, HoopZ);
        SiteBuilderUtil.Part(h, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f),
            new Vector3(0.14f, 1.6f, 0.14f), steel, true);
        SiteBuilderUtil.Part(h, "Backboard", PrimitiveType.Cube, new Vector3(0f, 3.0f, 0.2f),
            new Vector3(1.8f, 1.05f, 0.05f), board, false);
        SiteBuilderUtil.Part(h, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 2.75f, 0.5f),
            new Vector3(0.45f, 0.01f, 0.45f), rim, false);
    }

    // A garden bush like the ones beside the brick path in the photos: several thin, bare stems that lean
    // outward, each with a small clump of leaves at the top and a cluster of flowers (small balls) on it.
    // Red ixora bushes are about 1.7 m tall; the same shape in yellow stands for the allamanda shrubs.
    // Fixed random seed per position, so every run gives the same bush.
    internal static void BuildFlowerBush(Transform parent, string name, float x, float z, Material leaves, Material flower)
    {
        Transform b = SiteBuilderUtil.Group(parent, name);
        b.localPosition = new Vector3(x, 0f, z);
        var random = new System.Random((int)(x * 31f + z * 17f));
        Material stem = leaves; // thin stems in the leaf colour, darkened by being thin
        int stems = 5;
        for (int i = 0; i < stems; i++)
        {
            float angle = i * (360f / stems) * Mathf.Deg2Rad + (float)random.NextDouble() * 0.5f;
            float lean = 0.35f + (float)random.NextDouble() * 0.35f;
            float height = 1.2f + (float)random.NextDouble() * 0.6f;
            Vector3 top = new Vector3(Mathf.Cos(angle) * lean, height, Mathf.Sin(angle) * lean);
            GameObject stemObject = SiteBuilderUtil.Part(b, "Stem_" + (i + 1), PrimitiveType.Cylinder, top / 2f,
                new Vector3(0.05f, top.magnitude / 2f, 0.05f), stem, false);
            stemObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, top);
            // Leaves just under the flowers, and the flower cluster on top (4 small balls).
            SiteBuilderUtil.Part(b, "Leaves_" + (i + 1), PrimitiveType.Sphere, top + new Vector3(0f, -0.15f, 0f),
                new Vector3(0.55f, 0.32f, 0.55f), leaves, false);
            for (int f = 0; f < 4; f++)
            {
                Vector3 spot = top + new Vector3((float)(random.NextDouble() - 0.5) * 0.3f, 0.08f + (float)random.NextDouble() * 0.12f, (float)(random.NextDouble() - 0.5) * 0.3f);
                SiteBuilderUtil.Part(b, "Flower_" + (i + 1) + "_" + (f + 1), PrimitiveType.Sphere, spot, new Vector3(0.2f, 0.2f, 0.2f), flower, false);
            }
        }
        // A low skirt of leaves at the base.
        SiteBuilderUtil.Part(b, "Base", PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.5f, 0.9f), leaves, false);
    }

    // Draws one tile of interlocking "dog-bone" pavers, copied from the team's photo of the main road: dark grey-brown pavers with thin dark joints. Each paver is 0.2 m x 0.1 m: wide ends and a slightly narrower waist, so a small dark notch is left at the waist
    // on both long edges. Rows are staggered by half a paver, and the ends of one row sit over the waists of the next, so the notches line up in a
    // zig-zag across the road. A fixed random seed gives the same picture every run. Saved as a PNG; every brick surface in the scene uses this one file.
    static Texture2D MakeBrickTexture()
    {
        int w = BrickWidthPx * BricksAcross;
        int h = BrickHeightPx * BrickRows;
        var pixels = new Color32[w * h];
        var random = new System.Random(5);

        var mortar = new Color32(44, 40, 36, 255);   // dark joints
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = mortar;
        }

        const int endWidth = 14;      // the wide ends of the dog-bone, in pixels
        const int notchDepth = 4;     // how far the waist is cut in from each long edge

        for (int row = 0; row < BrickRows; row++)
        {
            int shift = (row % 2 == 1) ? BrickWidthPx / 2 : 0;
            for (int col = 0; col <= BricksAcross; col++)
            {
                int x0 = col * BrickWidthPx - shift;
                int y0 = row * BrickHeightPx;
                int variation = random.Next(-12, 13);
                // About every 5th paver is a darker, wet-stained one (the plaza photo has dark patches); every 12th is a paler, dusty one.
                int tone = random.Next(0, 5) == 0 ? -28 : (random.Next(0, 12) == 0 ? 14 : 0);
                // Dark grey-brown, like the main plaza in the team's photo (about 122, 106, 94 on screen). The game's lighting makes surfaces
                // warmer and bluer than the picture, so the picture is set much darker than the result wanted.
                var brick = new Color32(
                    (byte)Mathf.Clamp(92 + variation + tone, 0, 255),
                    (byte)Mathf.Clamp(76 + variation + tone, 0, 255),
                    (byte)Mathf.Clamp(56 + variation + tone, 0, 255), 255);

                for (int ly = MortarPx; ly < BrickHeightPx; ly++)
                {
                    for (int lx = MortarPx; lx < BrickWidthPx; lx++)
                    {
                        bool inWaist = lx >= endWidth && lx < BrickWidthPx - endWidth;
                        if (inWaist && (ly < MortarPx + notchDepth || ly >= BrickHeightPx - notchDepth))
                        {
                            continue;   // the notch at the waist stays joint-coloured
                        }
                        int x = x0 + lx;
                        int y = y0 + ly;
                        int wrappedX = ((x % w) + w) % w;   // pavers that stick out one side continue on the other
                        int grain = random.Next(-5, 6);      // fine weathering speckle
                        pixels[y * w + wrappedX] = new Color32(
                            (byte)Mathf.Clamp(brick.r + grain, 0, 255), (byte)Mathf.Clamp(brick.g + grain, 0, 255), (byte)Mathf.Clamp(brick.b + grain, 0, 255), 255);
                    }
                }
            }
        }

        return SaveTexture(BrickTexturePath, pixels, w, h);
    }

    // Draws one tile of patchy grass: a duller green with big soft lighter/darker/dry-yellow patches
    // (made from sine waves with whole-number frequencies, so the tile repeats without a visible seam)
    // plus fine random speckle and a few bare-earth dots. Same fixed seed every run.
    static Texture2D MakeGrassTexture()
    {
        var pixels = new Color32[GrassPx * GrassPx];
        var random = new System.Random(11);
        for (int y = 0; y < GrassPx; y++)
        {
            for (int x = 0; x < GrassPx; x++)
            {
                float u = x / (float)GrassPx;
                float v = y / (float)GrassPx;
                // Soft patches, -1..1.
                float patch = 0.5f * Mathf.Sin(2f * Mathf.PI * (2f * u + 1.3f * v) + 0.7f)
                            + 0.3f * Mathf.Sin(2f * Mathf.PI * (3f * v - 2f * u) + 2.1f)
                            + 0.2f * Mathf.Sin(2f * Mathf.PI * (5f * u + 4f * v) + 4.0f);
                int speckle = random.Next(-10, 11);

                float r = 52f + patch * 8f + speckle;        // deeper, more even green (the reference lawn)
                float g = 108f + patch * 11f + speckle;
                float b = 38f + patch * 4f + speckle / 2f;

                // Dry, yellowish areas where the patch value is low.
                if (patch < -0.75f)
                {
                    r += 28f;
                    g += 6f;
                }
                // A few bare-earth dots.
                if (random.Next(0, 400) == 0)
                {
                    r = 150f;
                    g = 100f;
                    b = 70f;
                }

                pixels[y * GrassPx + x] = new Color32(
                    (byte)Mathf.Clamp(r, 0f, 255f), (byte)Mathf.Clamp(g, 0f, 255f), (byte)Mathf.Clamp(b, 0f, 255f), 255);
            }
        }
        return SaveTexture(GrassTexturePath, pixels, GrassPx, GrassPx);
    }

    // Saves pixels as a PNG, imports it as a tiling texture and returns it.
    static Texture2D SaveTexture(string path, Color32[] pixels, int w, int h)
    {
        var made = new Texture2D(w, h, TextureFormat.RGBA32, false);
        made.SetPixels32(pixels);
        File.WriteAllBytes(path, made.EncodeToPNG());
        Object.DestroyImmediate(made);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 8; // keeps the pattern sharp when seen at a low angle
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
