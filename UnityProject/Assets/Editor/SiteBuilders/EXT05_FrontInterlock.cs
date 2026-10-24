using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT05 front yard: the area in front of the main building, as in the reference photos.
//   - a green grass lawn laid over the whole (red placeholder) ground plane
//   - a few patches of wet red earth on the lawn
//   - the brown "interlock" brick paving in front of the main building, with white painted lines
//   - palm trees (an avenue beside a brick path, plus a few near the building), flower bushes
//   - a pink-pillared gazebo, a basketball hoop, potted plants along the building front,
//     floodlight poles and a volleyball net, all copied from the reference photos in /reference
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
    const float GrassTileSize = 2f;      // the grass texture repeats every 2 m

    // Red earth patches: x, z, radius along x, radius along z.
    static readonly float[][] EarthPatches =
    {
        new[] { -2f,  -22f, 9f,  3.5f },
        new[] { 38f,  -24f, 8f,  3f   },
        new[] { 12f,  -30f, 5f,  2.5f },
        new[] { -26f, -14f, 4f,  2.5f },
    };
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
        new[] { 24f, -16f }, new[] { 32f, -16f }, new[] { 40f, -16f }, new[] { 48f, -16f },
        new[] { -9f, -3f  }, new[] { 49f, -3f  }, new[] { -30f, -8f }, new[] { -24f, 8f },
    };

    // Brick path along the palm avenue (a narrow strip of the same paving).
    const float PathNearZ = -17.5f;
    const float PathFarZ = -19.5f;

    // Potted plants in a row along the building front (skipping the entrance gap).
    const float PotZ = 2.6f;
    const float PotSpacing = 2.5f;
    const float PotGapMinX = 16f;        // no pots between these x values (the entrance)
    const float PotGapMaxX = 24f;

    // Flower bushes (x, z, 0 = red / 1 = yellow) on the lawn beside the avenue.
    static readonly float[][] FlowerBushes =
    {
        new[] { 4f,  -23f, 0f }, new[] { 12f, -22f, 1f }, new[] { 20f, -23f, 0f }, new[] { 28f, -22f, 1f },
        new[] { 36f, -23f, 0f }, new[] { 44f, -22f, 1f }, new[] { -14f, -20f, 0f },
    };

    // Gazebo (pink pillars, red tiled roof), basketball hoop, floodlights, volleyball net: positions.
    const float GazeboX = -24f;
    const float GazeboZ = -4f;
    const float HoopX = 42f;
    const float HoopZ = -2f;
    static readonly float[][] FloodlightPositions = { new[] { -32f, -6f }, new[] { 60f, -14f } };
    const float NetCenterX = -12f;
    const float NetZ = -34f;
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
    const int GrassPx = 128;

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
        Material earth = SiteBuilderUtil.GetMaterial("EXT05", "RedEarth", new Color32(150, 78, 52, 255), 0.45f);
        Material trunk = SiteBuilderUtil.GetMaterial("EXT05", "PalmTrunk", new Color32(110, 90, 70, 255), 0.05f);
        Material frond = SiteBuilderUtil.GetMaterial("EXT05", "PalmFrond", new Color32(60, 130, 55, 255), 0.1f);

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
            BuildPalm(palms, "Palm_" + (i + 1), PalmPositions[i][0], PalmPositions[i][1], i * 47f, trunk, frond);
        }

        // Narrow brick path along the palm avenue (own material so its brick pattern is not stretched).
        Material pathPaving = SiteBuilderUtil.GetMaterial("EXT05", "InterlockPath", new Color32(255, 255, 255, 255), 0.1f);
        pathPaving.mainTexture = brickTexture;
        pathPaving.mainTextureScale = new Vector2(width / TileSize, (PathNearZ - PathFarZ) / TileSize);
        SiteBuilderUtil.Part(root.transform, "BrickPath", PrimitiveType.Cube,
            new Vector3((MinX + MaxX) / 2f, PaverThickness / 2f, (PathNearZ + PathFarZ) / 2f),
            new Vector3(width, PaverThickness, PathNearZ - PathFarZ), pathPaving, false);

        Material pink = SiteBuilderUtil.GetMaterial("EXT05", "PillarPink", new Color32(236, 168, 148, 255), 0.1f);
        Material roofRed = SiteBuilderUtil.GetMaterial("EXT05", "RoofTileRed", new Color32(150, 56, 46, 255), 0.15f);
        Material terracotta = SiteBuilderUtil.GetMaterial("EXT05", "Terracotta", new Color32(190, 98, 62, 255), 0.1f);
        Material steel = SiteBuilderUtil.GetMaterial("EXT05", "DarkSteel", new Color32(60, 64, 70, 255), 0.3f);
        Material white = SiteBuilderUtil.GetMaterial("EXT05", "BoardWhite", new Color32(235, 235, 232, 255), 0.2f);
        Material orange = SiteBuilderUtil.GetMaterial("EXT05", "RimOrange", new Color32(230, 110, 30, 255), 0.2f);
        Material flowerRed = SiteBuilderUtil.GetMaterial("EXT05", "FlowerRed", new Color32(212, 44, 36, 255), 0.1f);
        Material flowerYellow = SiteBuilderUtil.GetMaterial("EXT05", "FlowerYellow", new Color32(236, 200, 40, 255), 0.1f);
        Material net = SiteBuilderUtil.GetMaterial("EXT05", "NetGrey", new Color32(200, 200, 200, 255), 0.1f);

        BuildGazebo(root.transform, pink, roofRed);
        BuildHoop(root.transform, steel, white, orange);

        Transform pots = SiteBuilderUtil.Group(root.transform, "PottedPlants");
        for (float x = MinX + 1f; x <= MaxX - 1f; x += PotSpacing)
        {
            if (x > PotGapMinX && x < PotGapMaxX)
            {
                continue;
            }
            SiteBuilderUtil.Part(pots, "Pot", PrimitiveType.Cylinder, new Vector3(x, 0.2f, PotZ),
                new Vector3(0.45f, 0.2f, 0.45f), terracotta, false);
            SiteBuilderUtil.Part(pots, "Plant", PrimitiveType.Sphere, new Vector3(x, 0.65f, PotZ),
                new Vector3(0.7f, 0.6f, 0.7f), frond, false);
        }

        Transform bushes = SiteBuilderUtil.Group(root.transform, "FlowerBushes");
        for (int i = 0; i < FlowerBushes.Length; i++)
        {
            float[] b = FlowerBushes[i];
            BuildFlowerBush(bushes, "Bush_" + (i + 1), b[0], b[1], frond, b[2] < 0.5f ? flowerRed : flowerYellow);
        }

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

        Transform volleyball = SiteBuilderUtil.Group(root.transform, "VolleyballNet");
        SiteBuilderUtil.Part(volleyball, "PoleWest", PrimitiveType.Cylinder, new Vector3(NetCenterX - 4.5f, 1.25f, NetZ),
            new Vector3(0.12f, 1.25f, 0.12f), steel, true);
        SiteBuilderUtil.Part(volleyball, "PoleEast", PrimitiveType.Cylinder, new Vector3(NetCenterX + 4.5f, 1.25f, NetZ),
            new Vector3(0.12f, 1.25f, 0.12f), steel, true);
        SiteBuilderUtil.Part(volleyball, "Net", PrimitiveType.Cube, new Vector3(NetCenterX, 2.0f, NetZ),
            new Vector3(9f, 1f, 0.02f), net, false);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT05_FrontInterlock: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // One palm tree: a thin trunk (with a collider) and a ring of drooping flat fronds on top.
    static void BuildPalm(Transform parent, string name, float x, float z, float turnDegrees, Material trunk, Material frond)
    {
        Transform palm = SiteBuilderUtil.Group(parent, name);
        palm.localPosition = new Vector3(x, 0f, z);
        palm.localRotation = Quaternion.Euler(0f, turnDegrees, 0f);

        // A cylinder is 2 m tall at scale 1, so half the height is the y scale.
        SiteBuilderUtil.Part(palm, "Trunk", PrimitiveType.Cylinder,
            new Vector3(0f, TrunkHeight / 2f, 0f),
            new Vector3(TrunkRadius * 2f, TrunkHeight / 2f, TrunkRadius * 2f), trunk, true);

        Transform crown = SiteBuilderUtil.Group(palm, "Crown");
        crown.localPosition = new Vector3(0f, TrunkHeight, 0f);
        for (int i = 0; i < FrondCount; i++)
        {
            Transform pivot = SiteBuilderUtil.Group(crown, "Frond_" + (i + 1));
            pivot.localRotation = Quaternion.Euler(0f, i * 360f / FrondCount, 0f);
            SiteBuilderUtil.Part(pivot, "Leaf", PrimitiveType.Cube,
                new Vector3(FrondLength / 2f, 0f, 0f),
                new Vector3(FrondLength, 0.04f, FrondWidth),
                new Vector3(0f, 0f, -FrondDroop), frond, false);
        }
    }

    // Open pavilion: a low platform, four pink pillars and a stepped red tiled roof (a simple "blockout"
    // of the real gazebo). The pillars have colliders; you can walk in between them.
    static void BuildGazebo(Transform parent, Material pink, Material roofRed)
    {
        Transform g = SiteBuilderUtil.Group(parent, "Gazebo");
        g.localPosition = new Vector3(GazeboX, 0f, GazeboZ);

        SiteBuilderUtil.Part(g, "Platform", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f),
            new Vector3(6f, 0.12f, 6f), pink, false);
        foreach (float sx in new[] { -2.4f, 2.4f })
        {
            foreach (float sz in new[] { -2.4f, 2.4f })
            {
                SiteBuilderUtil.Part(g, "Pillar", PrimitiveType.Cylinder, new Vector3(sx, 1.7f, sz),
                    new Vector3(0.4f, 1.6f, 0.4f), pink, true);
            }
        }
        SiteBuilderUtil.Part(g, "RingBeam", PrimitiveType.Cube, new Vector3(0f, 3.4f, 0f),
            new Vector3(5.6f, 0.3f, 5.6f), pink, false);
        SiteBuilderUtil.Part(g, "Roof_1", PrimitiveType.Cube, new Vector3(0f, 3.65f, 0f),
            new Vector3(6.6f, 0.2f, 6.6f), roofRed, false);
        SiteBuilderUtil.Part(g, "Roof_2", PrimitiveType.Cube, new Vector3(0f, 3.95f, 0f),
            new Vector3(4.6f, 0.2f, 4.6f), roofRed, false);
        SiteBuilderUtil.Part(g, "Roof_3", PrimitiveType.Cube, new Vector3(0f, 4.25f, 0f),
            new Vector3(2.4f, 0.2f, 2.4f), roofRed, false);
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

    // A round green bush with a few small flower balls on it.
    static void BuildFlowerBush(Transform parent, string name, float x, float z, Material leaves, Material flower)
    {
        Transform b = SiteBuilderUtil.Group(parent, name);
        b.localPosition = new Vector3(x, 0f, z);
        SiteBuilderUtil.Part(b, "Leaves", PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f),
            new Vector3(1.4f, 1.1f, 1.4f), leaves, false);
        var offsets = new[]
        {
            new Vector3(0.4f, 1.0f, 0.2f), new Vector3(-0.4f, 0.95f, 0.1f), new Vector3(0.1f, 1.1f, -0.4f),
            new Vector3(0f, 1.15f, 0.3f), new Vector3(-0.2f, 0.85f, -0.5f), new Vector3(0.5f, 0.8f, -0.3f),
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            SiteBuilderUtil.Part(b, "Flower_" + (i + 1), PrimitiveType.Sphere, offsets[i],
                new Vector3(0.28f, 0.28f, 0.28f), flower, false);
        }
    }

    // Draws one tile of running-bond brick paving (each row shifted half a brick) with slightly different
    // brick colours, saves it as a PNG, and returns the imported texture.
    static Texture2D MakeBrickTexture()
    {
        int w = BrickWidthPx * BricksAcross;
        int h = BrickHeightPx * BrickRows;
        var pixels = new Color32[w * h];
        var random = new System.Random(5); // fixed seed: the same picture every run, so no churn in git

        var mortar = new Color32(72, 62, 58, 255);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = mortar;
        }

        for (int row = 0; row < BrickRows; row++)
        {
            int shift = (row % 2 == 1) ? BrickWidthPx / 2 : 0;
            for (int col = 0; col <= BricksAcross; col++)
            {
                int x0 = col * BrickWidthPx - shift;
                int y0 = row * BrickHeightPx;
                int variation = random.Next(-14, 15);
                var brick = new Color32(
                    (byte)Mathf.Clamp(150 + variation, 0, 255),
                    (byte)Mathf.Clamp(98 + variation, 0, 255),
                    (byte)Mathf.Clamp(84 + variation, 0, 255), 255);

                for (int y = y0 + MortarPx; y < y0 + BrickHeightPx; y++)
                {
                    for (int x = x0 + MortarPx; x < x0 + BrickWidthPx; x++)
                    {
                        int wrappedX = ((x % w) + w) % w; // bricks that stick out one side continue on the other
                        pixels[y * w + wrappedX] = brick;
                    }
                }
            }
        }

        return SaveTexture(BrickTexturePath, pixels, w, h);
    }

    // Draws one tile of grass: mid green with random lighter and darker speckles. Edges wrap because
    // every pixel is independent noise, so the tile repeats without a visible seam.
    static Texture2D MakeGrassTexture()
    {
        var pixels = new Color32[GrassPx * GrassPx];
        var random = new System.Random(11);
        for (int i = 0; i < pixels.Length; i++)
        {
            int v = random.Next(-22, 23);
            pixels[i] = new Color32(
                (byte)Mathf.Clamp(78 + v, 0, 255),
                (byte)Mathf.Clamp(130 + v, 0, 255),
                (byte)Mathf.Clamp(52 + v / 2, 0, 255), 255);
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
