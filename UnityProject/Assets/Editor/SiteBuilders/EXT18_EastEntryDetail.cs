using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT18 east entry detail in Exterior.unity, copied from the reference photos of the college's first entry
// (looking at the old store shed from the plaza):
//   - a row of slim foxtail palms and small shrubs along the grass strip in front of the shed
//   - weathering on the shed's front wall (peeling plaster, damp stains) and a green wheelbarrow in front of it
//   - a small white security cabin with a grey-blue hip roof, and a grey gate frame (two pillars and a beam) beside it
//   - coconut palms and leafy trees behind the shed
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT18 East Entry Detail
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT18_EastEntryDetail.Build -logFile ext18.log
//
// It is RE-RUNNABLE: every run deletes the old EXT18_EastEntryDetail object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT18_EastEntryDetail, to Exterior.unity. Nothing that already
// exists is changed. The shed is EXT13's StoreShed (x 58..74, z 9..17, front wall at z 9, facing -z). Build EXT05 and EXT13
// first (palm and hip-roof methods).
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT18_EastEntryDetail
{
    const string RootName = "EXT18_EastEntryDetail";
    const string Prefix = "EXT18";

    // Palms on the strip in front of the shed (x, z): a cluster of four toward the cabin. (The palm that stood at the west
    // end, x 56.5, was removed: that spot is now brick, the gap between the main building and the shed.)
    static readonly float[][] Palms =
    {
        new[] { 66.8f, 5.0f }, new[] { 69.7f, 5.6f }, new[] { 75.2f, 5.1f }, new[] { 77.7f, 5.6f },
    };
    // Small shrubs between the palms (x, z).
    static readonly float[][] Shrubs =
    {
        new[] { 68.3f, 6.1f }, new[] { 71.1f, 5.9f }, new[] { 73.9f, 6.2f }, new[] { 76.6f, 5.9f },
    };

    // Shed front wall (EXT13: ShedMinX 65 .. ShedMaxX 74, ShedMinZ 9).
    const float ShedMinX = 65f;
    const float ShedMaxX = 74f;
    const float ShedFrontZ = 9f;

    // Brick in the gap between the main building's east end (x 53) and the shed (x 58), running north from the plaza.
    const float GapMinX = 52.8f;
    const float GapMaxX = 66f;           // brick right across to the shed's west end: an open walkway, nothing planted on it
    const float GapMinZ = 3.4f;
    const float GapMaxZ = 21f;

    // Security cabin and gate.
    const float CabinX = 78.2f;
    const float CabinZ = 6.8f;

    [MenuItem("Campus/Build EXT18 East Entry Detail")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT18_EastEntryDetail", BuildInternal);
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

        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "PalmTrunk", new Color32(150, 142, 128, 255), 0.02f);
        Material frond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(156, 186, 74, 255), 0.05f);
        Material shrub = SiteBuilderUtil.GetMaterial(Prefix, "ShrubGreen", new Color32(70, 140, 60, 255), 0.05f);
        Material stem = SiteBuilderUtil.GetMaterial(Prefix, "ShrubStem", new Color32(110, 120, 70, 255), 0.05f);
        Material peel = SiteBuilderUtil.GetMaterial(Prefix, "Peel", new Color32(190, 186, 176, 255), 0.02f);
        Material stain = SiteBuilderUtil.GetMaterial(Prefix, "Stain", new Color32(150, 126, 110, 255), 0.02f);
        Material barrowGreen = SiteBuilderUtil.GetMaterial(Prefix, "BarrowGreen", new Color32(58, 130, 70, 255), 0.1f);
        Material tyre = SiteBuilderUtil.GetMaterial(Prefix, "Tyre", new Color32(30, 30, 32, 255), 0.05f);
        Material white = SiteBuilderUtil.GetMaterial(Prefix, "CabinWhite", new Color32(232, 234, 232, 255), 0.05f);
        Material roofBlue = SiteBuilderUtil.GetMaterial(Prefix, "CabinRoof", new Color32(120, 136, 152, 255), 0.1f);
        Material dark = SiteBuilderUtil.GetMaterial(Prefix, "DoorDark", new Color32(46, 52, 56, 255), 0.3f);
        Material gateGrey = SiteBuilderUtil.GetMaterial(Prefix, "GateGrey", new Color32(112, 114, 116, 255), 0.05f);
        Material bark = SiteBuilderUtil.GetMaterial(Prefix, "CoconutTrunk", new Color32(96, 82, 66, 255), 0.02f);
        Material coconutFrond = SiteBuilderUtil.GetMaterial(Prefix, "CoconutFrond", new Color32(44, 100, 48, 255), 0.05f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeaves", new Color32(54, 108, 46, 255), 0.05f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // Palms and shrubs on the strip.
        Transform plants = SiteBuilderUtil.Group(root.transform, "StripPlants");
        for (int i = 0; i < Palms.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(plants, "Foxtail_" + (i + 1), Palms[i][0], Palms[i][1], i * 57f, trunk, frond, 5.0f + (i % 3) * 0.5f, 3.0f, true, 0.09f);
        }
        for (int i = 0; i < Shrubs.Length; i++)
        {
            BuildShrub(plants, "Shrub_" + (i + 1), Shrubs[i][0], Shrubs[i][1], i, shrub, stem);
        }

        Material gapBrick = SiteBuilderUtil.GetMaterial(Prefix, "GapBrick", new Color32(255, 255, 255, 255), 0.1f);
        var brickTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (brickTexture != null)
        {
            gapBrick.mainTexture = brickTexture;
            gapBrick.mainTextureScale = new Vector2((GapMaxX - GapMinX) / 0.8f, (GapMaxZ - GapMinZ) / 0.8f);
        }
        else
        {
            gapBrick.color = new Color32(110, 96, 90, 255);
        }
        SiteBuilderUtil.Part(root.transform, "GapPaving", PrimitiveType.Cube,
            new Vector3((GapMinX + GapMaxX) / 2f, 0.035f / 2f, (GapMinZ + GapMaxZ) / 2f),
            new Vector3(GapMaxX - GapMinX, 0.035f, GapMaxZ - GapMinZ), gapBrick, false);

        // (The lawn patch, palm and shrubs at the strip's west end were removed: they blocked the walkway between the buildings.)

        BuildWeathering(root.transform, peel, stain);
        BuildWheelbarrow(root.transform, barrowGreen, tyre);
        BuildCabin(root.transform, white, roofBlue, dark);
        // (The gate is built by EXT19_FrontGate now, with the cladded beam, the closed leaves and the tower behind it.)
        // (The pink two-storey building behind the shed was removed: the team does not want it, and it covered the badminton hall.)
        BuildBackdrop(root.transform, bark, coconutFrond, leaves);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT18_EastEntryDetail: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // A small dracaena-like shrub: several tall thin leaf blades radiating from a short stem.
    static void BuildShrub(Transform parent, string name, float x, float z, int seed, Material leaves, Material stem)
    {
        Transform s = SiteBuilderUtil.Group(parent, name);
        s.localPosition = new Vector3(x, 0f, z);
        SiteBuilderUtil.Part(s, "Stem", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.06f, 0.4f, 0.06f), stem, false);
        var random = new System.Random(seed * 13 + 5);
        for (int i = 0; i < 7; i++)
        {
            Transform pivot = SiteBuilderUtil.Group(s, "Blade");
            pivot.localPosition = new Vector3(0f, 0.7f, 0f);
            pivot.localRotation = Quaternion.Euler(0f, i * 51f + (float)random.NextDouble() * 20f, 0f);
            float length = 0.7f + (float)random.NextDouble() * 0.4f;
            GameObject blade = SiteBuilderUtil.Part(pivot, "Leaf", PrimitiveType.Cube, new Vector3(length / 2f * 0.5f, length / 2f * 0.8f, 0f),
                new Vector3(0.05f, length, 0.015f), leaves, false);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, -(35f + (float)random.NextDouble() * 20f));
        }
    }

    // Weathering on the shed's front wall: pale peeling-plaster patches and darker damp stains in fixed positions, drawn
    // just in front of the wall (behind the windows and doors, which stand a little further out).
    static void BuildWeathering(Transform parent, Material peel, Material stain)
    {
        Transform w = SiteBuilderUtil.Group(parent, "Weathering");
        var random = new System.Random(18);
        float z = ShedFrontZ - 0.035f;
        for (int i = 0; i < 16; i++)
        {
            float x = ShedMinX + 0.5f + (float)random.NextDouble() * (ShedMaxX - ShedMinX - 1f);
            float y = 0.4f + (float)random.NextDouble() * 2.6f;
            float width = 0.6f + (float)random.NextDouble() * 1.6f;
            float height = 0.4f + (float)random.NextDouble() * 1.2f;
            SiteBuilderUtil.Part(w, (i % 3 == 0 ? "Peel_" : "Stain_") + (i + 1), PrimitiveType.Cube, new Vector3(x, y, z),
                new Vector3(width, height, 0.015f), i % 3 == 0 ? peel : stain, false);
        }
        // A dark damp band along the foot of the wall.
        SiteBuilderUtil.Part(w, "DampBand", PrimitiveType.Cube, new Vector3((ShedMinX + ShedMaxX) / 2f, 0.3f, z),
            new Vector3(ShedMaxX - ShedMinX, 0.5f, 0.015f), stain, false);
    }

    // A green wheelbarrow in front of the shed: a tub (a cube), one wheel and two handles.
    static void BuildWheelbarrow(Transform parent, Material green, Material tyre)
    {
        Transform b = SiteBuilderUtil.Group(parent, "Wheelbarrow");
        b.localPosition = new Vector3(70.0f, 0f, 7.4f);
        b.localRotation = Quaternion.Euler(0f, 20f, 0f);
        SiteBuilderUtil.Part(b, "Tub", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.4f, 1.1f), green, false);
        SiteBuilderUtil.Part(b, "Wheel", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, -0.7f), new Vector3(0.5f, 0.05f, 0.5f), new Vector3(0f, 0f, 90f), tyre, false);
        foreach (float x in new[] { -0.3f, 0.3f })
        {
            SiteBuilderUtil.Part(b, "Handle", PrimitiveType.Cube, new Vector3(x, 0.7f, 0.9f), new Vector3(0.05f, 0.05f, 0.8f), green, false);
        }
    }

    // The small white security cabin with a grey-blue hip roof, a window, a glazed door and a step.
    static void BuildCabin(Transform parent, Material white, Material roof, Material dark)
    {
        Transform c = SiteBuilderUtil.Group(parent, "SecurityCabin");
        c.localPosition = new Vector3(CabinX, 0f, CabinZ);
        SiteBuilderUtil.Part(c, "Walls", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(4.2f, 3f, 3.4f), white, true);
        SiteBuilderUtil.Part(c, "Step", PrimitiveType.Cube, new Vector3(0.2f, 0.12f, -2.1f), new Vector3(2.4f, 0.24f, 1f), white, true);
        SiteBuilderUtil.Part(c, "Door", PrimitiveType.Cube, new Vector3(0.9f, 1.15f, -1.72f), new Vector3(1.0f, 2.1f, 0.05f), dark, false);
        SiteBuilderUtil.Part(c, "Window_1", PrimitiveType.Cube, new Vector3(-1.1f, 1.7f, -1.72f), new Vector3(1.0f, 0.9f, 0.05f), dark, false);
        SiteBuilderUtil.Part(c, "Window_2", PrimitiveType.Cube, new Vector3(-2.12f, 1.7f, 0f), new Vector3(0.05f, 0.9f, 1.0f), dark, false);
        EXT05_FrontInterlock.BuildHipRoof(c, "Roof", new Vector3(0f, 3f, 0f), 5.2f, 4.4f, 1.3f, 0.4f, roof, Prefix);
    }


    // Coconut palms and leafy trees behind the pink building and the shed, to fill the background of the photos.
    static void BuildBackdrop(Transform parent, Material bark, Material coconutFrond, Material leaves)
    {
        Transform b = SiteBuilderUtil.Group(parent, "Backdrop");
        // (Moved clear of the badminton hall, which stands at x 55..63, z 21..40.)
        float[][] palms = { new[] { 67.5f, 25f }, new[] { 66f, 33f }, new[] { 70f, 34f }, new[] { 75f, 30f }, new[] { 77f, 22f }, new[] { 60f, 46f } };
        for (int i = 0; i < palms.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(b, "Coconut_" + (i + 1), palms[i][0], palms[i][1], i * 61f, bark, coconutFrond, 10f + (i % 3) * 1.8f, 4.2f, false);
        }
        float[][] trees = { new[] { 68.5f, 21.5f }, new[] { 74.5f, 24f }, new[] { 67f, 41f } };
        for (int i = 0; i < trees.Length; i++)
        {
            Transform t = SiteBuilderUtil.Group(b, "Tree_" + (i + 1));
            t.localPosition = new Vector3(trees[i][0], 0f, trees[i][1]);
            SiteBuilderUtil.Part(t, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(0.5f, 2f, 0.5f), bark, true);
            SiteBuilderUtil.Part(t, "Canopy_1", PrimitiveType.Sphere, new Vector3(0f, 5.8f, 0f), new Vector3(6f, 5f, 6f), leaves, false);
            SiteBuilderUtil.Part(t, "Canopy_2", PrimitiveType.Sphere, new Vector3(2f, 5f, 1f), new Vector3(4f, 3.4f, 4f), leaves, false);
        }
    }
}
