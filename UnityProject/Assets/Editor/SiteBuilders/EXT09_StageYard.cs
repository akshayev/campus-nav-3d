using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT09 stage yard in Exterior.unity: everything around the stage in the reference photos.
//   - a long white block behind the stage and a second white block with a grey roof beside it, both with
//     a coral-pink base band, a front veranda on thin dark posts, windows and doors
//   - a grey dome (water tank) in the gap between the two blocks, with a leaning brown board
//   - a brick path lined with red ixora bushes (one side) and yellow shrubs in white tree guards (other side)
//   - a bare-earth volleyball court with two posts, a net and puddles
//   - a second lattice floodlight tower, a tree in a round planter, and foxtail palms
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT09 Stage Yard
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT09_StageYard.Build -logFile ext09.log
//
// It is RE-RUNNABLE: every run deletes the old EXT09_StageYard object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT09_StageYard, to Exterior.unity. Nothing that already
// exists is changed. (The old standalone volleyball net was removed from the EXT05 builder, because it stood
// where the blocks go; the new court replaces it.) Build EXT05 first: this needs its roof-mesh, palm, tower and
// bush methods and its brick picture.
//
// Layout. World units, metres. The stage (EXT05) is at x -18.3, z -22.8 and faces +z (toward the main building).
// The two blocks stand in ONE ROW with the car shed (EXT04), to its west, with their fronts facing +z too:
//
//      z = -45.7   front edge of the verandas      z = -48.7   front walls (same line as the car shed)      z = -56.7   back walls
//
// Row from west to east: long block (x -76..-39), east block (x -36..-18), the dome, then the car shed (x -9..38).
// Nothing stands in front of the car shed.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT09_StageYard
{
    const string RootName = "EXT09_StageYard";
    const string Prefix = "EXT09";

    // The two blocks (front wall z, depth). Each block's X range is given by its centre and length.
    const float FrontWallZ = -48.7f;         // the same front-wall line as the car shed (EXT04), so all the blocks make one row
    const float BlockDepth = 8f;
    const float LongBlockCenterX = -57.5f;   // x -76 .. -39
    const float LongBlockLength = 37f;
    const float EastBlockCenterX = -27f;     // x -36 .. -18, then the dome gap, then the car shed (starts at x -9)
    const float EastBlockLength = 18f;
    const float WallHeight = 3.3f;
    const float PlinthHeight = 0.8f;
    const float VerandaDepth = 3f;           // from the front wall out to the posts and roof edge

    // Dome and board, in the gap between the blocks.
    const float DomeX = -13.5f;
    const float DomeZ = -52f;

    // Brick path to the long block, with its bushes (x range of the path, z range).
    const float PathMinX = -49.2f;
    const float PathMaxX = -46.8f;
    const float PathNearZ = -21f;
    const float PathFarZ = -45.7f;           // the long block's veranda edge
    const int BushesPerSide = 7;

    // Volleyball court: a bare-earth rectangle, the net across the middle along z.
    const float CourtMinX = 8f;            // west edge of the western court
    const float CourtMaxX = 50f;           // east edge of the eastern court
    const float CourtLength = 16f;         // each court is 16 m along x; the eastern court (bare earth, puddles) ends at CourtMaxX,
    const float CourtGap = 8f;             // the western court (plain grass) is CourtGap metres west of it
    const float CourtMinZ = -34f;
    const float CourtMaxZ = -24f;
    const float NetHeight = 2.43f;

    // Second floodlight tower, tree + planter.
    const float TowerX = -19.9f;         // chosen by the team in the Unity Scene view
    const float TowerZ = -28.8f;
    const float PlanterX = -66f;
    const float PlanterZ = -41f;

    [MenuItem("Campus/Build EXT09 Stage Yard")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT09_StageYard", BuildInternal);
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
        SiteBuilderUtil.EnsureFolder("Assets", "Meshes");
        SiteBuilderUtil.EnsureFolder("Assets/Meshes", "Sites");

        // Materials (matt, flat colours).
        Material wall = SiteBuilderUtil.GetMaterial(Prefix, "WallWhite", new Color32(232, 230, 222, 255), 0.02f);
        Material plinth = SiteBuilderUtil.GetMaterial(Prefix, "PlinthCoral", new Color32(196, 104, 84, 255), 0.02f);
        Material roofDark = SiteBuilderUtil.GetMaterial(Prefix, "RoofDark", new Color32(76, 80, 86, 255), 0.1f);
        Material roofGrey = SiteBuilderUtil.GetMaterial(Prefix, "RoofGrey", new Color32(132, 138, 144, 255), 0.1f);
        Material post = SiteBuilderUtil.GetMaterial(Prefix, "PostDark", new Color32(52, 38, 36, 255), 0.1f);
        Material veranda = SiteBuilderUtil.GetMaterial(Prefix, "VerandaFloor", new Color32(150, 134, 124, 255), 0.02f);
        Material frame = SiteBuilderUtil.GetMaterial(Prefix, "FrameGreen", new Color32(50, 84, 70, 255), 0.05f);
        Material glass = SiteBuilderUtil.GetMaterial(Prefix, "Glass", new Color32(56, 70, 82, 255), 0.4f);
        Material doorBlue = SiteBuilderUtil.GetMaterial(Prefix, "DoorBlue", new Color32(40, 60, 104, 255), 0.1f);
        Material doorDark = SiteBuilderUtil.GetMaterial(Prefix, "DoorDark", new Color32(44, 36, 34, 255), 0.1f);
        Material dome = SiteBuilderUtil.GetMaterial(Prefix, "DomeGrey", new Color32(98, 110, 100, 255), 0.05f);
        Material board = SiteBuilderUtil.GetMaterial(Prefix, "BoardBrown", new Color32(124, 82, 52, 255), 0.05f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "BushGreen", new Color32(58, 118, 50, 255), 0.05f);
        Material ixora = SiteBuilderUtil.GetMaterial(Prefix, "IxoraRed", new Color32(212, 44, 36, 255), 0.1f);
        Material yellow = SiteBuilderUtil.GetMaterial(Prefix, "ShrubYellow", new Color32(236, 200, 40, 255), 0.1f);
        Material guard = SiteBuilderUtil.GetMaterial(Prefix, "TreeGuard", new Color32(236, 236, 230, 255), 0.1f);
        Material earth = SiteBuilderUtil.GetMaterial(Prefix, "CourtEarth", new Color32(168, 130, 98, 255), 0.02f);
        Material puddle = SiteBuilderUtil.GetMaterial(Prefix, "Puddle", new Color32(62, 72, 84, 255), 0.95f);
        Material wood = SiteBuilderUtil.GetMaterial(Prefix, "WoodPost", new Color32(122, 92, 62, 255), 0.05f);
        Material netWhite = SiteBuilderUtil.GetMaterial(Prefix, "NetWhite", new Color32(232, 232, 228, 255), 0.1f);
        Material steel = SiteBuilderUtil.GetMaterial(Prefix, "TowerSteel", new Color32(40, 44, 50, 255), 0.3f);
        Material lamp = SiteBuilderUtil.GetMaterial(Prefix, "TowerLamp", new Color32(90, 94, 100, 255), 0.1f);
        Material planter = SiteBuilderUtil.GetMaterial(Prefix, "PlanterWhite", new Color32(214, 210, 200, 255), 0.05f);
        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "TreeTrunk", new Color32(96, 76, 58, 255), 0.02f);
        Material canopy = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeaves", new Color32(76, 128, 58, 255), 0.05f);
        Material foxtail = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(70, 110, 60, 255), 0.05f);

        Material pathBrick = SiteBuilderUtil.GetMaterial(Prefix, "PathBrick", new Color32(255, 255, 255, 255), 0.1f);
        var brick = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (brick != null)
        {
            pathBrick.mainTexture = brick;
            pathBrick.mainTextureScale = new Vector2((PathMaxX - PathMinX) / 0.8f, (PathNearZ - PathFarZ) / 0.8f);
        }
        else
        {
            pathBrick.color = new Color32(124, 104, 94, 255); // run Campus > Build EXT05 Front Yard first for the brick picture
        }

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // The two blocks in a row behind the stage.
        BuildLongBlock(root.transform, "BlockBehindStage", LongBlockCenterX, LongBlockLength, wall, plinth, roofDark, post, veranda, frame, glass, doorBlue);
        BuildLongBlock(root.transform, "BlockEast", EastBlockCenterX, EastBlockLength, wall, plinth, roofGrey, post, veranda, frame, glass, doorDark);

        // The grey dome and its board, between the blocks.
        Transform domeGroup = SiteBuilderUtil.Group(root.transform, "Dome");
        SiteBuilderUtil.Part(domeGroup, "Dome", PrimitiveType.Sphere, new Vector3(DomeX, 0f, DomeZ), new Vector3(6.4f, 6f, 6.4f), dome, false);
        SiteBuilderUtil.Part(domeGroup, "Board", PrimitiveType.Cube, new Vector3(DomeX + 2f, 0.55f, DomeZ + 2.6f),
            new Vector3(1.3f, 1.1f, 0.05f), new Vector3(-12f, 20f, 0f), board, false);

        // (The brick path to the long block, with its bushes, was removed: they now line the path to the main entrance, EXT07.)
        BuildCourt(root.transform, earth, puddle, wood, netWhite);

        // A second lattice floodlight tower, west of the long block's front, and the one tree with its planter.
        Transform towers = SiteBuilderUtil.Group(root.transform, "Floodlight");
        EXT05_FrontInterlock.BuildTower(towers, "Tower", TowerX, TowerZ, steel, lamp);

        Transform tree = SiteBuilderUtil.Group(root.transform, "PlanterTree");
        tree.localPosition = new Vector3(PlanterX, 0f, PlanterZ);
        SiteBuilderUtil.Part(tree, "Planter", PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(2.6f, 0.3f, 2.6f), planter, true);
        SiteBuilderUtil.Part(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.4f, 1.9f, 0.4f), trunk, true);
        SiteBuilderUtil.Part(tree, "Canopy_1", PrimitiveType.Sphere, new Vector3(0f, 5.6f, 0f), new Vector3(5.5f, 4.2f, 5.5f), canopy, false);
        SiteBuilderUtil.Part(tree, "Canopy_2", PrimitiveType.Sphere, new Vector3(1.6f, 4.8f, 0.6f), new Vector3(3.2f, 2.8f, 3.2f), canopy, false);

        Transform palms = SiteBuilderUtil.Group(root.transform, "FoxtailPalms");
        float[][] palmSpots = { };   // (the foxtail palms near the volleyball court were removed by the team)
        for (int i = 0; i < palmSpots.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(palms, "Foxtail_" + (i + 1), palmSpots[i][0], palmSpots[i][1], i * 61f, trunk, foxtail, 5f, 2.8f);
        }

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT09_StageYard: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // A long single-storey white block: walls (collider), coral base band, veranda, thin dark posts, windows
    // and doors, and a low hip roof that also covers the veranda. The block is built facing -z in its own
    // frame and the group is turned 180 degrees, so its front faces world +z (toward the stage).
    static void BuildLongBlock(Transform parent, string name, float centerX, float length, Material wall, Material plinth,
        Material roof, Material post, Material veranda, Material frame, Material glass, Material door)
    {
        BuildLongBlockAt(parent, name, new Vector3(centerX, 0f, FrontWallZ), 180f, length, BlockDepth,
            wall, plinth, roof, post, veranda, frame, glass, door);
    }

    // The same block placed anywhere: 'position' is the middle of its front wall (on the ground), 'yaw' turns it
    // (180 = front faces +z, -90 = front faces +x), 'depth' is the wall depth. Other builders (EXT11) use this too.
    internal static void BuildLongBlockAt(Transform parent, string name, Vector3 position, float yaw, float length, float depth,
        Material wall, Material plinth, Material roof, Material post, Material veranda, Material frame, Material glass, Material door)
    {
        Transform block = SiteBuilderUtil.Group(parent, name);
        block.localPosition = position;
        block.localRotation = Quaternion.Euler(0f, yaw, 0f);

        SiteBuilderUtil.Part(block, "Walls", PrimitiveType.Cube, new Vector3(0f, WallHeight / 2f, depth / 2f),
            new Vector3(length, WallHeight, depth), wall, true);
        SiteBuilderUtil.Part(block, "Plinth", PrimitiveType.Cube, new Vector3(0f, PlinthHeight / 2f, depth / 2f - 0.02f),
            new Vector3(length + 0.06f, PlinthHeight, depth + 0.06f), plinth, false);
        SiteBuilderUtil.Part(block, "VerandaFloor", PrimitiveType.Cube, new Vector3(0f, 0.075f, -VerandaDepth / 2f),
            new Vector3(length + 0.6f, 0.15f, VerandaDepth + 0.2f), veranda, true);

        // Small windows and doors along the front (z = 0), one per 3 m slot; every fifth slot is a door.
        Transform openings = SiteBuilderUtil.Group(block, "Openings");
        int slots = Mathf.RoundToInt(length / 3f);
        for (int i = 0; i < slots; i++)
        {
            float x = -length / 2f + 3f * (i + 0.5f);
            if (i % 5 == 2)
            {
                SiteBuilderUtil.Part(openings, "Door_" + (i + 1), PrimitiveType.Cube, new Vector3(x, 1.2f, -0.03f),
                    new Vector3(1.0f, 2.1f, 0.05f), door, false);
                continue;
            }
            SiteBuilderUtil.Part(openings, "Frame_" + (i + 1), PrimitiveType.Cube, new Vector3(x, 1.9f, -0.02f),
                new Vector3(1.3f, 1.1f, 0.03f), frame, false);
            SiteBuilderUtil.Part(openings, "Glass_" + (i + 1), PrimitiveType.Cube, new Vector3(x, 1.9f, -0.04f),
                new Vector3(1.1f, 0.9f, 0.03f), glass, false);
        }

        // Thin dark posts along the front of the veranda, and the beam on top.
        Transform posts = SiteBuilderUtil.Group(block, "Posts");
        int postCount = Mathf.RoundToInt(length / 4f) + 1;
        for (int i = 0; i < postCount; i++)
        {
            float x = -length / 2f + i * (length / (postCount - 1));
            SiteBuilderUtil.Part(posts, "Post_" + (i + 1), PrimitiveType.Cube, new Vector3(x, 0.15f + (WallHeight - 0.15f) / 2f, -VerandaDepth + 0.2f),
                new Vector3(0.15f, WallHeight - 0.15f, 0.15f), post, false);
        }
        SiteBuilderUtil.Part(posts, "Beam", PrimitiveType.Cube, new Vector3(0f, WallHeight - 0.1f, -VerandaDepth + 0.2f),
            new Vector3(length + 0.3f, 0.2f, 0.15f), post, false);

        // Low hip roof, from the veranda edge at the front to a small overhang at the back.
        float roofBack = depth + 1f;
        float roofWidth = length + 2f;
        float roofDepth = roofBack + VerandaDepth;
        EXT05_FrontInterlock.BuildHipRoof(block, "Roof", new Vector3(0f, WallHeight, (-VerandaDepth + roofBack) / 2f),
            roofWidth, roofDepth, 1.4f, Mathf.Max(0f, (roofWidth - roofDepth) / 2f), roof, Prefix);
    }

    // The brick path from the avenue to the long block, red ixora bushes on its east side and yellow shrubs
    // in white tree guards on its west side.
    static void BuildPath(Transform parent, Material pathBrick, Material leaves, Material ixora, Material yellow, Material guard)
    {
        Transform path = SiteBuilderUtil.Group(parent, "BrickPath");
        float centerX = (PathMinX + PathMaxX) / 2f;
        float lengthZ = PathNearZ - PathFarZ;
        SiteBuilderUtil.Part(path, "Paving", PrimitiveType.Cube, new Vector3(centerX, 0.025f, (PathNearZ + PathFarZ) / 2f),
            new Vector3(PathMaxX - PathMinX, 0.05f, lengthZ), pathBrick, false);

        for (int i = 0; i < BushesPerSide; i++)
        {
            float z = PathNearZ - 1.4f - i * ((lengthZ - 2.8f) / (BushesPerSide - 1));
            EXT05_FrontInterlock.BuildFlowerBush(path, "Ixora_" + (i + 1), PathMaxX + 1.5f, z, leaves, ixora);
            EXT05_FrontInterlock.BuildFlowerBush(path, "Shrub_" + (i + 1), PathMinX - 1.5f, z, leaves, yellow);
            SiteBuilderUtil.Part(path, "Guard_" + (i + 1), PrimitiveType.Cylinder, new Vector3(PathMinX - 1.5f, 0.45f, z),
                new Vector3(0.25f, 0.45f, 0.25f), guard, false);
        }
    }

    // The two volleyball courts, copied from the reference photo. They stand in a line along x, 8 m apart, each with
    // its own net running left to right between two thin wooden posts:
    //   - the eastern court (nearer the building in the photo) is a wide patch of bare red-brown sand with puddles
    //   - the western court is on plain grass, with just a little bare ground at the foot of each post
    // In front of them is the red mud track with a big puddle, and a small wooden stand stands at the west end.
    static void BuildCourt(Transform parent, Material earth, Material puddle, Material wood, Material netWhite)
    {
        Transform court = SiteBuilderUtil.Group(parent, "VolleyballCourts");
        float centerZ = (CourtMinZ + CourtMaxZ) / 2f;
        float depth = CourtMaxZ - CourtMinZ;
        Material sand = SiteBuilderUtil.GetMaterial(Prefix, "CourtSand", new Color32(176, 126, 92, 255), 0.02f);

        float eastMinX = CourtMaxX - CourtLength;
        float westMaxX = eastMinX - CourtGap;
        float westMinX = westMaxX - CourtLength;

        // Eastern court: bare sand in overlapping ovals, with puddles.
        float eastCenterX = (eastMinX + CourtMaxX) / 2f;
        float width = CourtLength;
        float[][] ovals =
        {
            new[] {  0f,   0f,   width * 0.95f, depth * 0.62f },
            new[] { -3.2f, 1.0f, width * 0.5f,  depth * 0.85f },
            new[] {  3.4f, -0.8f, width * 0.5f,  depth * 0.8f  },
            new[] {  0.5f, -2.0f, width * 0.6f,  depth * 0.45f },
        };
        for (int i = 0; i < ovals.Length; i++)
        {
            SiteBuilderUtil.Part(court, "EastSand_" + (i + 1), PrimitiveType.Cylinder, new Vector3(eastCenterX + ovals[i][0], 0.02f + i * 0.001f, centerZ + ovals[i][1]),
                new Vector3(ovals[i][2], 0.006f, ovals[i][3]), sand, false);
        }
        float[][] puddles =
        {
            new[] { -3.8f, 0.8f, 4.4f, 1.4f }, new[] { 0.5f, -0.4f, 3.0f, 1.0f }, new[] { 4.2f, 0.9f, 4.0f, 1.5f },
        };
        for (int i = 0; i < puddles.Length; i++)
        {
            SiteBuilderUtil.Part(court, "Puddle_" + (i + 1), PrimitiveType.Cylinder, new Vector3(eastCenterX + puddles[i][0], 0.032f, centerZ + puddles[i][1]),
                new Vector3(puddles[i][2], 0.003f, puddles[i][3]), puddle, false);
        }

        // Western court: grass, with a small bare patch at the foot of each post.
        foreach (float x in new[] { westMinX - 0.5f, westMaxX + 0.5f })
        {
            SiteBuilderUtil.Part(court, "WestPatch", PrimitiveType.Cylinder, new Vector3(x, 0.02f, centerZ), new Vector3(2.8f, 0.006f, 2.2f), sand, false);
        }

        // A net and two posts for each court.
        BuildNet(court, "East", eastMinX - 0.5f, CourtMaxX + 0.5f, centerZ, wood, netWhite);
        BuildNet(court, "West", westMinX - 0.5f, westMaxX + 0.5f, centerZ, wood, netWhite);

        // The red mud track in the foreground of the photo: long streaks of wet red earth with one big puddle.
        Material mud = SiteBuilderUtil.GetMaterial(Prefix, "RedMud", new Color32(168, 82, 56, 255), 0.05f);
        float trackCenterX = (westMinX + CourtMaxX) / 2f;
        SiteBuilderUtil.Part(court, "MudTrack_1", PrimitiveType.Cylinder, new Vector3(trackCenterX - 6f, 0.024f, CourtMaxZ + 4.2f), new Vector3(24f, 0.006f, 3.2f), mud, false);
        SiteBuilderUtil.Part(court, "MudTrack_2", PrimitiveType.Cylinder, new Vector3(trackCenterX + 10f, 0.025f, CourtMaxZ + 3.2f), new Vector3(14f, 0.006f, 2.4f), mud, false);
        SiteBuilderUtil.Part(court, "MudPuddle", PrimitiveType.Cylinder, new Vector3(trackCenterX - 4f, 0.034f, CourtMaxZ + 4.2f), new Vector3(7f, 0.003f, 1.4f), puddle, false);

        // A small wooden stand at the west end (a bench-like table with a dark bag on it, as in the photo).
        Material bag = SiteBuilderUtil.GetMaterial(Prefix, "Bag", new Color32(40, 70, 130, 255), 0.1f);
        SiteBuilderUtil.Part(court, "StandTop", PrimitiveType.Cube, new Vector3(westMinX - 2.5f, 0.85f, centerZ + 1.5f), new Vector3(1.1f, 0.12f, 0.7f), wood, false);
        foreach (float sx in new[] { -0.45f, 0.45f })
        {
            SiteBuilderUtil.Part(court, "StandLeg", PrimitiveType.Cube, new Vector3(westMinX - 2.5f + sx, 0.4f, centerZ + 1.5f), new Vector3(0.1f, 0.8f, 0.6f), wood, false);
        }
        SiteBuilderUtil.Part(court, "Bag", PrimitiveType.Cube, new Vector3(westMinX - 2.6f, 1.05f, centerZ + 1.5f), new Vector3(0.45f, 0.3f, 0.3f), bag, false);
    }

    // A net running along x between two thin wooden posts at postX1 and postX2: a white band on top and a few strands below.
    static void BuildNet(Transform court, string name, float postX1, float postX2, float z, Material wood, Material netWhite)
    {
        foreach (float x in new[] { postX1, postX2 })
        {
            SiteBuilderUtil.Part(court, name + "Post", PrimitiveType.Cylinder, new Vector3(x, 1.4f, z), new Vector3(0.1f, 1.4f, 0.1f), wood, true);
        }
        float length = postX2 - postX1;
        float centerX = (postX1 + postX2) / 2f;
        SiteBuilderUtil.Part(court, name + "NetBand", PrimitiveType.Cube, new Vector3(centerX, NetHeight, z), new Vector3(length, 0.1f, 0.03f), netWhite, false);
        for (int i = 0; i < 4; i++)
        {
            SiteBuilderUtil.Part(court, name + "NetStrand_" + (i + 1), PrimitiveType.Cube, new Vector3(centerX, NetHeight - 0.25f - i * 0.22f, z),
                new Vector3(length, 0.015f, 0.015f), netWhite, false);
        }
    }
}
