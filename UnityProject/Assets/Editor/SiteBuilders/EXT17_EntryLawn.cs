using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT17 entry lawn in Exterior.unity, copied from the reference photo taken on the lawn in front of the
// bike shed:
//   - a row of slim foxtail palms along the west side of the straight brick way, with a coconut sapling and a wooden post
//   - a painted boundary wall behind the east end of the bike shed (concrete pillars, coloured panels, two painted faces
//     and the word SATTVA), with a concrete utility pole beside it
//   - lots of tufts of long grass scattered over the lawn between the way and the east boundary
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT17 Entry Lawn
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT17_EntryLawn.Build -logFile ext17.log
//
// It is RE-RUNNABLE: every run deletes the old EXT17_EntryLawn object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT17_EntryLawn, to Exterior.unity. Nothing that already exists is
// changed. The wall, pole and palm trunks have colliders; the grass tufts and paint do not. Build EXT05 first (it needs
// the palm method) and EXT11 (sapling).
//
// Layout (world metres). The east boundary (an invisible wall) is at x = 81, the bike shed's east end at x = 76.1:
//
//      x = 79.6         the mural wall, faces west (-x), z -62 .. -34
//      x = 57.9 .. 58.8 palms beside the straight way (x 60.9 .. 63.5), z -15.5 .. -26
//      x = 52 .. 79     grass tufts, z -32 .. -14 (not on the brick way, the tower or the shed road)
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT17_EntryLawn
{
    const string RootName = "EXT17_EntryLawn";
    const string Prefix = "EXT17";

    // Mural wall.
    const float WallX = 76.4f;           // the same x as the gate's side wall (EXT19), so the two make one straight wall
    const float WallMinZ = -62f;
    const float WallMaxZ = -34f;
    const float WallHeight = 2.3f;
    const float PillarSpacing = 4f;

    // Utility pole beside the wall.
    const float PoleX = 78.4f;
    const float PoleZ = -33f;
    const float PoleHeight = 9f;

    // Palm row beside the straight way: x, z of each (alternating sides of a line, like the photo).
    static readonly float[][] Palms =
    {
        new[] { 58.6f, -15.5f }, new[] { 57.9f, -19.2f }, new[] { 58.8f, -22.8f }, new[] { 58.0f, -26.2f },
    };
    const float SaplingX = 56.2f;
    const float SaplingZ = -27.5f;
    const float PostX = 59.9f;
    const float PostZ = -17.2f;

    // Grass tufts.
    const int TuftCount = 110;
    const float TuftMinX = 52f;
    const float TuftMaxX = 79f;
    const float TuftMinZ = -32f;
    const float TuftMaxZ = -14f;

    [MenuItem("Campus/Build EXT17 Entry Lawn")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT17_EntryLawn", BuildInternal);
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

        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "PalmTrunk", new Color32(150, 142, 128, 255), 0.02f);
        Material frond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(156, 186, 74, 255), 0.05f);
        Material young = SiteBuilderUtil.GetMaterial(Prefix, "YoungFrond", new Color32(120, 170, 60, 255), 0.05f);
        Material wood = SiteBuilderUtil.GetMaterial(Prefix, "WoodPost", new Color32(96, 78, 58, 255), 0.05f);
        Material concrete = SiteBuilderUtil.GetMaterial(Prefix, "Concrete", new Color32(150, 148, 142, 255), 0.02f);
        Material cream = SiteBuilderUtil.GetMaterial(Prefix, "WallCream", new Color32(214, 208, 192, 255), 0.02f);
        Material grassTuft = SiteBuilderUtil.GetMaterial(Prefix, "TuftGreen", new Color32(70, 128, 52, 255), 0.05f);
        Material grassDry = SiteBuilderUtil.GetMaterial(Prefix, "TuftDry", new Color32(138, 150, 70, 255), 0.05f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // (The column of palms beside the straight way, the coconut sapling and the wooden post were removed: the palms now stand in a
        // horizontal row in front of the bike shed, built by EXT15, as in the reference photo.)

        BuildMuralWall(root.transform, cream, concrete);
        BuildPole(root.transform, concrete);
        BuildTufts(root.transform, grassTuft, grassDry);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT17_EntryLawn: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // The painted wall: a solid slab with concrete pillars every 4 m, coloured panels between the pillars, two painted
    // faces (a skin-colour oval, dark hair, two eyes) and the word SATTVA. The painted side faces west (-x).
    static void BuildMuralWall(Transform parent, Material cream, Material concrete)
    {
        Transform wall = SiteBuilderUtil.Group(parent, "MuralWall");
        float length = WallMaxZ - WallMinZ;
        float centerZ = (WallMinZ + WallMaxZ) / 2f;
        float faceX = WallX - 0.17f;   // the painted face of the wall

        SiteBuilderUtil.Part(wall, "Wall", PrimitiveType.Cube, new Vector3(WallX, WallHeight / 2f, centerZ), new Vector3(0.3f, WallHeight, length), cream, true);

        int pillars = Mathf.RoundToInt(length / PillarSpacing) + 1;
        for (int i = 0; i < pillars; i++)
        {
            float z = WallMinZ + i * (length / (pillars - 1));
            SiteBuilderUtil.Part(wall, "Pillar_" + (i + 1), PrimitiveType.Cube, new Vector3(WallX, (WallHeight + 0.3f) / 2f, z), new Vector3(0.45f, WallHeight + 0.3f, 0.45f), concrete, false);
        }

        // Coloured panels, one in each bay between pillars (colours: blue, grey, gold, teal, white, red, dark).
        Color32[] colours =
        {
            new Color32(60, 110, 170, 255), new Color32(190, 188, 180, 255), new Color32(214, 170, 60, 255),
            new Color32(70, 150, 140, 255), new Color32(232, 230, 224, 255), new Color32(190, 70, 60, 255), new Color32(60, 60, 72, 255),
        };
        for (int i = 0; i < pillars - 1; i++)
        {
            float z = WallMinZ + (i + 0.5f) * (length / (pillars - 1));
            Material panel = SiteBuilderUtil.GetMaterial(Prefix, "Panel" + (i % colours.Length), colours[i % colours.Length], 0.05f);
            SiteBuilderUtil.Part(wall, "Panel_" + (i + 1), PrimitiveType.Cube, new Vector3(faceX, WallHeight / 2f - 0.05f, z),
                new Vector3(0.03f, WallHeight - 0.4f, length / (pillars - 1) - 0.6f), panel, false);
        }

        // Two painted faces on the first and third bays (bays are indexed from the south end, z = WallMinZ).
        Material skin = SiteBuilderUtil.GetMaterial(Prefix, "FaceSkin", new Color32(226, 170, 130, 255), 0.05f);
        Material hair = SiteBuilderUtil.GetMaterial(Prefix, "FaceHair", new Color32(36, 30, 34, 255), 0.05f);
        Material eye = SiteBuilderUtil.GetMaterial(Prefix, "FaceEye", new Color32(250, 250, 250, 255), 0.05f);
        float bay = length / (pillars - 1);
        foreach (int index in new[] { 0, 2 })
        {
            float z = WallMinZ + (index + 0.5f) * bay;
            SiteBuilderUtil.Part(wall, "FaceHair_" + (index + 1), PrimitiveType.Sphere, new Vector3(faceX - 0.03f, 1.35f, z), new Vector3(0.04f, 1.5f, 1.4f), hair, false);
            SiteBuilderUtil.Part(wall, "Face_" + (index + 1), PrimitiveType.Sphere, new Vector3(faceX - 0.06f, 1.25f, z), new Vector3(0.04f, 1.2f, 0.9f), skin, false);
            foreach (float dz in new[] { -0.2f, 0.2f })
            {
                SiteBuilderUtil.Part(wall, "Eye", PrimitiveType.Sphere, new Vector3(faceX - 0.09f, 1.38f, z + dz), new Vector3(0.03f, 0.14f, 0.2f), eye, false);
            }
        }

        // The word SATTVA on the second bay's panel, in dark paint.
        BuildText(wall, "SATTVA", new Vector3(faceX - 0.04f, 1.15f, WallMinZ + 1.5f * bay));
    }

    // Lettering made with a TextMesh (Unity's old 3D text) and the built-in font; turned to read from the west (-x).
    static void BuildText(Transform parent, string text, Vector3 position)
    {
        var go = new GameObject("Lettering");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 64;
        mesh.characterSize = 0.075f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = new Color32(40, 40, 44, 255);
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    // A concrete pole with a short cross-arm near the top, like the pole by the wall in the photo.
    static void BuildPole(Transform parent, Material concrete)
    {
        Transform pole = SiteBuilderUtil.Group(parent, "UtilityPole");
        pole.localPosition = new Vector3(PoleX, 0f, PoleZ);
        SiteBuilderUtil.Part(pole, "Pole", PrimitiveType.Cylinder, new Vector3(0f, PoleHeight / 2f, 0f), new Vector3(0.28f, PoleHeight / 2f, 0.28f), concrete, true);
        SiteBuilderUtil.Part(pole, "CrossArm", PrimitiveType.Cube, new Vector3(0f, PoleHeight - 0.6f, 0f), new Vector3(0.1f, 0.1f, 1.6f), concrete, false);
    }

    // Tufts of long grass: each is four thin blades leaning in different directions. A fixed random seed gives the same
    // lawn every run. Tufts are skipped on the brick way (x 63..66.6), near the tower (x 53.5..57, z -25..-20) and on the
    // palm row, so nothing grows through paving or trunks.
    static void BuildTufts(Transform parent, Material green, Material dry)
    {
        Transform tufts = SiteBuilderUtil.Group(parent, "GrassTufts");
        var random = new System.Random(17);
        int made = 0;
        int guard = 0;
        while (made < TuftCount && guard < TuftCount * 20)
        {
            guard++;
            float x = TuftMinX + (float)random.NextDouble() * (TuftMaxX - TuftMinX);
            float z = TuftMinZ + (float)random.NextDouble() * (TuftMaxZ - TuftMinZ);
            if (x > 60.2f && x < 64.2f) continue;                            // the straight brick way (x 60.9 .. 63.5)
            if (z > -17.2f) continue;                                        // the compound wall (EXT19) is at z -16.5
            if (z < -30.5f) continue;                                        // the two curved ways start around z -31 to -33
            if (x > 53.5f && x < 57f && z > -25f && z < -20f) continue;   // the tower
            if (x > 56.5f && x < 60.2f && z > -28f && z < -14.5f) continue; // the palm row
            if (x > 55.2f && x < 57.2f && z > -28.5f && z < -26.5f) continue; // the sapling

            Transform tuft = SiteBuilderUtil.Group(tufts, "Tuft_" + (made + 1));
            tuft.localPosition = new Vector3(x, 0f, z);
            Material material = random.NextDouble() < 0.25 ? dry : green;
            for (int b = 0; b < 4; b++)
            {
                float height = 0.35f + (float)random.NextDouble() * 0.3f;
                float yaw = b * 90f + (float)random.NextDouble() * 40f;
                float lean = 8f + (float)random.NextDouble() * 20f;
                // A blade is a thin cube; it leans from a pivot at its base, so it is a child of a small pivot object.
                Transform pivot = SiteBuilderUtil.Group(tuft, "Pivot");
                pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
                var blade = SiteBuilderUtil.Part(pivot, "Blade", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.025f, height, 0.012f), material, false);
                blade.transform.localRotation = Quaternion.Euler(lean, 0f, 0f);
                blade.transform.localPosition = new Vector3(0f, Mathf.Cos(lean * Mathf.Deg2Rad) * height / 2f, Mathf.Sin(lean * Mathf.Deg2Rad) * height / 2f);
            }
            made++;
        }
    }
}
