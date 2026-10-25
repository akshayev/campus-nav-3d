using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT13 east entry in Exterior.unity, copied from the reference photos of the college's first entry:
// the wide brick ("interlock") road carried on east from the front plaza to the edge of the site, a chain fence
// on posts along its north side with a few slim foxtail palms behind it on the grass, and an old store shed
// (peach walls with graffiti patches, red corrugated roof over an open front) across the courtyard.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT13 East Entry
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT13_EastEntry.Build -logFile ext13.log
//
// It is RE-RUNNABLE: every run deletes the old EXT13_EastEntry object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT13_EastEntry, to Exterior.unity. Nothing that already
// exists is changed. The paving has no collider (the avatar still walks on the GroundPlane). Build EXT05 first:
// this reuses its brick picture, palm method and foxtail material.
//
// Layout (world metres). The plaza ends at x = 53 (z -12.6 .. 3.4); the east boundary wall is at x = 81:
//
//      x = 52.7 .. 79   brick road, same width as the plaza (z -12.6 .. 3.4)
//      z = 3.9          chain fence along the north edge
//      x = 58 .. 74     store shed, z 9 .. 17, facing the road
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT13_EastEntry
{
    const string RootName = "EXT13_EastEntry";
    const string Prefix = "EXT13";

    const float RoadMinX = 52.7f;        // overlaps the plaza edge (x 53) a little, so there is no gap
    const float RoadMaxX = 79f;
    const float RoadMinZ = -12.6f;
    const float RoadMaxZ = 3.4f;
    const float Thickness = 0.03f;       // same as the plaza paving
    const float TileSize = 0.8f;

    const float FenceZ = 3.9f;
    const float FenceMinX = 65.4f;       // the rope starts at the shed's west end, so the walkway between the buildings stays open
    const float FencePostSpacing = 4f;
    const float FenceHeight = 0.95f;

    // Slim foxtail palms behind the fence (x, z).
    // One slim foxtail palm and a young tree on the grass strip beside the fence (x, z); the courtyard brick starts behind them.
    const float PalmX = 72.6f;
    const float PalmZ = 4.7f;
    const float SaplingX = 68.6f;
    const float SaplingZ = 4.5f;

    // Courtyard brick between the grass strip and the shed.
    const float CourtMinX = 54f;
    const float CourtMaxX = 78f;
    const float CourtMinZ = 5.6f;
    const float CourtMaxZ = 9.5f;

    // Store shed: x range, z range (front wall at ShedMinZ, facing the road).
    // The shed stands 11.5 m east of the main building's east wall (x 53.5), as in the reference frame: a wide brick
    // walkway runs between them. It is 9 m long (it used to be 16 m, x 58..74).
    const float ShedMinX = 65f;
    const float ShedMaxX = 74f;
    const float ShedMinZ = 9f;
    const float ShedMaxZ = 17f;
    const float ShedWallHeight = 3.2f;

    [MenuItem("Campus/Build EXT13 East Entry")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT13_EastEntry", BuildInternal);
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

        float width = RoadMaxX - RoadMinX;
        float depth = RoadMaxZ - RoadMinZ;

        Material brick = SiteBuilderUtil.GetMaterial(Prefix, "RoadBrick", new Color32(255, 255, 255, 255), 0.1f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (texture != null)
        {
            brick.mainTexture = texture;
            brick.mainTextureScale = new Vector2(width / TileSize, depth / TileSize);
        }
        else
        {
            brick.color = new Color32(110, 96, 90, 255); // run Campus > Build EXT05 Front Yard first for the brick picture
        }
        Material post = SiteBuilderUtil.GetMaterial(Prefix, "FencePost", new Color32(130, 134, 136, 255), 0.3f);
        Material chain = SiteBuilderUtil.GetMaterial(Prefix, "Chain", new Color32(226, 214, 158, 255), 0.4f);   // cream-yellow rope, like the rope fence in the photos
        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "PalmTrunk", new Color32(150, 142, 128, 255), 0.02f);
        Material frond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(156, 186, 74, 255), 0.05f);
        Material wall = SiteBuilderUtil.GetMaterial(Prefix, "ShedWall", new Color32(226, 196, 168, 255), 0.02f);
        Material roof = SiteBuilderUtil.GetMaterial(Prefix, "ShedRoof", new Color32(160, 62, 50, 255), 0.1f);
        Material beam = SiteBuilderUtil.GetMaterial(Prefix, "ShedPost", new Color32(120, 110, 100, 255), 0.1f);
        Material graffiti1 = SiteBuilderUtil.GetMaterial(Prefix, "GraffitiRed", new Color32(190, 54, 56, 255), 0.05f);
        Material graffiti2 = SiteBuilderUtil.GetMaterial(Prefix, "GraffitiDark", new Color32(50, 52, 60, 255), 0.05f);
        Material window = SiteBuilderUtil.GetMaterial(Prefix, "WindowDark", new Color32(48, 52, 56, 255), 0.3f);
        Material clutter = SiteBuilderUtil.GetMaterial(Prefix, "Clutter", new Color32(110, 120, 100, 255), 0.05f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        SiteBuilderUtil.Part(root.transform, "Road", PrimitiveType.Cube,
            new Vector3((RoadMinX + RoadMaxX) / 2f, Thickness / 2f, (RoadMinZ + RoadMaxZ) / 2f),
            new Vector3(width, Thickness, depth), brick, false);

        BuildFence(root.transform, post, chain);

        SiteBuilderUtil.Part(root.transform, "Courtyard", PrimitiveType.Cube,
            new Vector3((CourtMinX + CourtMaxX) / 2f, Thickness / 2f, (CourtMinZ + CourtMaxZ) / 2f),
            new Vector3(CourtMaxX - CourtMinX, Thickness, CourtMaxZ - CourtMinZ), brick, false);

        Transform plants = SiteBuilderUtil.Group(root.transform, "StripPlants");
        EXT05_FrontInterlock.BuildPalm(plants, "FoxtailPalm", PalmX, PalmZ, 20f, trunk, frond, 5.4f, 3.2f, true, 0.09f);
        Material young = SiteBuilderUtil.GetMaterial(Prefix, "YoungLeaves", new Color32(150, 190, 60, 255), 0.05f);
        Material young2 = SiteBuilderUtil.GetMaterial(Prefix, "YoungLeavesPale", new Color32(196, 210, 80, 255), 0.05f);
        EXT11_WestEnd.BuildSapling(plants, "Sapling", SaplingX, SaplingZ, trunk, young, young2);

        BuildShed(root.transform, wall, roof, beam, graffiti1, graffiti2, window, clutter);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT13_EastEntry: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // A chain fence along the north edge of the road: grey posts every 4 m, and between each pair two thin chain
    // lines that sag a little in the middle (three short straight pieces each). No colliders.
    static void BuildFence(Transform parent, Material post, Material chain)
    {
        Transform fence = SiteBuilderUtil.Group(parent, "ChainFence");
        int count = Mathf.RoundToInt((RoadMaxX - FenceMinX) / FencePostSpacing) + 1;
        for (int i = 0; i < count; i++)
        {
            float x = FenceMinX + i * FencePostSpacing;
            SiteBuilderUtil.Part(fence, "Post_" + (i + 1), PrimitiveType.Cylinder, new Vector3(x, FenceHeight / 2f, FenceZ),
                new Vector3(0.12f, FenceHeight / 2f, 0.12f), post, false);
            if (i == count - 1)
            {
                break;
            }
            foreach (float top in new[] { FenceHeight - 0.08f, FenceHeight - 0.5f })
            {
                // Three pieces: a short one down, a flat one along the middle, a short one back up.
                float seg = FencePostSpacing / 3f;
                float sag = 0.22f;
                SiteBuilderUtil.Part(fence, "ChainA", PrimitiveType.Cube, new Vector3(x + seg / 2f, top - sag / 2f, FenceZ),
                    new Vector3(seg, 0.02f, 0.02f), new Vector3(0f, 0f, -Mathf.Atan2(sag, seg) * Mathf.Rad2Deg), chain, false);
                SiteBuilderUtil.Part(fence, "ChainB", PrimitiveType.Cube, new Vector3(x + FencePostSpacing / 2f, top - sag, FenceZ),
                    new Vector3(seg, 0.02f, 0.02f), chain, false);
                SiteBuilderUtil.Part(fence, "ChainC", PrimitiveType.Cube, new Vector3(x + FencePostSpacing - seg / 2f, top - sag / 2f, FenceZ),
                    new Vector3(seg, 0.02f, 0.02f), new Vector3(0f, 0f, Mathf.Atan2(sag, seg) * Mathf.Rad2Deg), chain, false);
            }
        }
    }

    // The old store shed: a solid peach block (collider) with a low red corrugated roof that sticks out over an open
    // front veranda held up by thin posts, windows, graffiti patches and a little clutter, like the photo.
    static void BuildShed(Transform parent, Material wall, Material roof, Material beam, Material g1, Material g2, Material window, Material clutter)
    {
        Transform shed = SiteBuilderUtil.Group(parent, "StoreShed");
        float length = ShedMaxX - ShedMinX;
        float depth = ShedMaxZ - ShedMinZ;
        float centerX = (ShedMinX + ShedMaxX) / 2f;
        float centerZ = (ShedMinZ + ShedMaxZ) / 2f;
        const float verandaDepth = 3f;

        SiteBuilderUtil.Part(shed, "Walls", PrimitiveType.Cube, new Vector3(centerX, ShedWallHeight / 2f, centerZ),
            new Vector3(length, ShedWallHeight, depth), wall, true);
        SiteBuilderUtil.Part(shed, "Step", PrimitiveType.Cube, new Vector3(centerX, 0.08f, ShedMinZ - verandaDepth / 2f),
            new Vector3(length, 0.16f, verandaDepth), wall, true);

        // Front: two windows, a doorway at each end, and two graffiti patches.
        float frontZ = ShedMinZ - 0.03f;
        for (int i = 0; i < 2; i++)
        {
            SiteBuilderUtil.Part(shed, "Window_" + (i + 1), PrimitiveType.Cube, new Vector3(ShedMinX + 3.0f + i * 3.4f, 1.8f, frontZ),
                new Vector3(1.6f, 1.2f, 0.05f), window, false);
        }
        foreach (float x in new[] { ShedMinX + 1.2f, ShedMaxX - 1.2f })
        {
            SiteBuilderUtil.Part(shed, "Doorway", PrimitiveType.Cube, new Vector3(x, 1.15f, frontZ), new Vector3(1.2f, 2.3f, 0.05f), window, false);
        }
        SiteBuilderUtil.Part(shed, "Graffiti_1", PrimitiveType.Cube, new Vector3(ShedMinX + 4.5f, 2.75f, frontZ - 0.02f), new Vector3(3.2f, 0.8f, 0.03f), g1, false);
        SiteBuilderUtil.Part(shed, "Graffiti_2", PrimitiveType.Cube, new Vector3(ShedMinX + 4.5f, 2.75f, frontZ - 0.045f), new Vector3(2.4f, 0.4f, 0.03f), g2, false);

        // Thin posts along the veranda edge, and a few boxes of clutter on the step.
        int posts = Mathf.RoundToInt(length / 4f) + 1;
        for (int i = 0; i < posts; i++)
        {
            float x = ShedMinX + i * (length / (posts - 1));
            SiteBuilderUtil.Part(shed, "Post_" + (i + 1), PrimitiveType.Cylinder, new Vector3(x, ShedWallHeight / 2f, ShedMinZ - verandaDepth + 0.2f),
                new Vector3(0.14f, ShedWallHeight / 2f, 0.14f), beam, false);
        }
        SiteBuilderUtil.Part(shed, "Clutter_1", PrimitiveType.Cube, new Vector3(ShedMaxX - 4f, 0.7f, ShedMinZ - 1.2f), new Vector3(1.4f, 1.0f, 0.8f), clutter, false);
        SiteBuilderUtil.Part(shed, "Clutter_2", PrimitiveType.Cube, new Vector3(ShedMaxX - 6f, 0.5f, ShedMinZ - 1.5f), new Vector3(1.0f, 0.7f, 0.7f), clutter, false);

        // Low red roof: from the veranda edge at the front to a small overhang at the back.
        float roofWidth = length + 2f;
        float roofFront = ShedMinZ - verandaDepth;
        float roofBack = ShedMaxZ + 0.8f;
        float roofDepth = roofBack - roofFront;
        EXT05_FrontInterlock.BuildHipRoof(shed, "Roof", new Vector3(centerX, ShedWallHeight, (roofFront + roofBack) / 2f),
            roofWidth, roofDepth, 1.3f, Mathf.Max(0f, (roofWidth - roofDepth) / 2f), roof, Prefix);
    }
}
