using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT11 west end in Exterior.unity, copied from the reference photo taken looking west along the
// brick paving: a long white block with a grey roof, a coral-pink base band and a front veranda on thin dark
// posts, standing across the far (west) end of the paving and facing it, with leafy trees on the lawn beside
// the paving on the way there.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT11 West End
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT11_WestEnd.Build -logFile ext11.log
//
// It is RE-RUNNABLE: every run deletes the old EXT11_WestEnd object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT11_WestEnd, to Exterior.unity. Nothing that already
// exists is changed. The trees stand on the lawn beside the paving, none on the brick. Build EXT05, EXT09 and
// EXT10 first: this reuses EXT09's block builder, and the paving (EXT10) is what the block faces.
//
// Layout (world metres). The paving band ends at x = -68 (z -12.6 .. 3.4). The block stands across that end:
//
//      x = -68.5   front edge of the veranda (and roof)      x = -71.5   front wall      x = -77.5   back wall
//      z = -18 .. +8     the length of the block (26 m)
//
// The boundary wall is at x -81, so the roof's back edge (x -78.5) clears it.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT11_WestEnd
{
    const string RootName = "EXT11_WestEnd";
    const string Prefix = "EXT11";

    // The block: the middle of its front wall (x, z), its length (along z) and wall depth.
    const float FrontWallX = -71.5f;
    const float CenterZ = -5f;
    const float Length = 26f;
    const float Depth = 6f;

    // The one tall slim tree in front of the canteen (the team wants only this one tree there): x, z.
    // The canteen is at (-41.3, 23.8) and faces -z; its pond is about 8 m in front of it.
    const float TreeX = -49f;
    const float TreeZ = 11f;
    const float TrunkRadius = 0.3f;
    const float TrunkHeight = 4.5f;      // the trunk forks into branches at this height

    // The young yellow-green tree and the small foxtail palm beside it (as in the photo): x, z.
    const float SaplingX = -47.5f;
    const float SaplingZ = 6.5f;
    const float FoxtailX = -44.5f;
    const float FoxtailZ = 9.5f;

    [MenuItem("Campus/Build EXT11 West End")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT11_WestEnd", BuildInternal);
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

        Material wall = SiteBuilderUtil.GetMaterial(Prefix, "WallWhite", new Color32(234, 232, 224, 255), 0.02f);
        Material plinth = SiteBuilderUtil.GetMaterial(Prefix, "PlinthCoral", new Color32(198, 108, 88, 255), 0.02f);
        Material roof = SiteBuilderUtil.GetMaterial(Prefix, "RoofGrey", new Color32(138, 144, 150, 255), 0.1f);
        Material post = SiteBuilderUtil.GetMaterial(Prefix, "PostDark", new Color32(54, 40, 38, 255), 0.1f);
        Material veranda = SiteBuilderUtil.GetMaterial(Prefix, "VerandaFloor", new Color32(150, 134, 124, 255), 0.02f);
        Material frame = SiteBuilderUtil.GetMaterial(Prefix, "FrameGreen", new Color32(50, 84, 70, 255), 0.05f);
        Material glass = SiteBuilderUtil.GetMaterial(Prefix, "Glass", new Color32(56, 70, 82, 255), 0.4f);
        Material door = SiteBuilderUtil.GetMaterial(Prefix, "DoorDark", new Color32(44, 36, 34, 255), 0.1f);
        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "TreeTrunk", new Color32(92, 74, 58, 255), 0.02f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeaves", new Color32(80, 132, 58, 255), 0.05f);
        Material leavesDark = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeavesDark", new Color32(58, 106, 48, 255), 0.05f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // (The long west-end block that stood here was removed: it is now part of the single MCA building, EXT08.)

        BuildTallTree(root.transform, "TallTree", TreeX, TreeZ, trunk, leaves, leavesDark);

        Material young = SiteBuilderUtil.GetMaterial(Prefix, "YoungLeaves", new Color32(150, 190, 60, 255), 0.05f);
        Material young2 = SiteBuilderUtil.GetMaterial(Prefix, "YoungLeavesPale", new Color32(196, 210, 80, 255), 0.05f);
        Material palmFrond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(170, 190, 60, 255), 0.05f);
        BuildSapling(root.transform, "Sapling", SaplingX, SaplingZ, trunk, young, young2);
        EXT05_FrontInterlock.BuildPalm(root.transform, "FoxtailPalm", FoxtailX, FoxtailZ, 20f, trunk, palmFrond, 2.4f, 1.7f);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT11_WestEnd: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // The big tree from the photo: a thick trunk that forks into three branches, and a wide crown of many
    // flat leaf clumps at different heights and distances, so it looks full but still airy. Fixed random seed,
    // so every run gives the same tree.
    static void BuildTallTree(Transform parent, string name, float x, float z, Material trunk, Material leaves, Material leavesDark)
    {
        Transform tree = SiteBuilderUtil.Group(parent, name);
        tree.localPosition = new Vector3(x, 0f, z);
        SiteBuilderUtil.Part(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, TrunkHeight / 2f, 0f),
            new Vector3(TrunkRadius * 2f, TrunkHeight / 2f, TrunkRadius * 2f), trunk, true);

        // Three branches leaning out from the top of the trunk (each: lean sideways in x and z, length).
        float[][] branches = { new[] { -2.4f, 0.5f, 4.2f }, new[] { 2.2f, -0.9f, 4.4f }, new[] { 0.3f, 2.3f, 3.9f } };
        for (int i = 0; i < branches.Length; i++)
        {
            float bx = branches[i][0];
            float bz = branches[i][1];
            float length = branches[i][2];
            Vector3 direction = new Vector3(bx, length, bz);
            GameObject branch = SiteBuilderUtil.Part(tree, "Branch_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(0f, TrunkHeight, 0f) + direction / 2f,
                new Vector3(0.22f, direction.magnitude / 2f, 0.22f), trunk, false);
            branch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
        }

        // The crown: leaf clumps scattered over a wide, shallow dome above the forks.
        var random = new System.Random(7);
        for (int i = 0; i < 26; i++)
        {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            float radius = Mathf.Sqrt((float)random.NextDouble()) * 5.2f;
            float height = TrunkHeight + 1.8f + (1f - radius / 5.2f) * 3.2f + (float)random.NextDouble() * 1.4f;
            float size = 1.9f + (float)random.NextDouble() * 1.5f;
            SiteBuilderUtil.Part(tree, "Leaves_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius),
                new Vector3(size * 1.4f, size * 0.7f, size * 1.4f), i % 3 == 0 ? leaves : leavesDark, false);
        }
    }

    // A young tree like the one in the photo: a thin bare trunk with a few layers of small yellow-green leaves.
    internal static void BuildSapling(Transform parent, string name, float x, float z, Material trunk, Material leaves, Material pale)
    {
        Transform tree = SiteBuilderUtil.Group(parent, name);
        tree.localPosition = new Vector3(x, 0f, z);
        SiteBuilderUtil.Part(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(0.1f, 2f, 0.1f), trunk, false);
        for (int i = 0; i < 4; i++)
        {
            float y = 1.5f + i * 0.7f;
            float spread = 1.4f - i * 0.22f;
            SiteBuilderUtil.Part(tree, "Layer_" + (i + 1), PrimitiveType.Sphere, new Vector3(0.1f * (i % 2 == 0 ? 1 : -1), y, 0f),
                new Vector3(spread, 0.3f, spread), i % 2 == 0 ? leaves : pale, false);
        }
    }
}
