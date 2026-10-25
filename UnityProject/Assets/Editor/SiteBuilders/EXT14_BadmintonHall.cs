using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT14 indoor badminton hall in Exterior.unity, copied from the reference photo of the east courtyard:
// a long hall with low dark-green walls and a dark-green barrel (half-round) roof, a small white portable cabin
// beside it and a pair of white football goals in front of it.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT14 Badminton Hall
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT14_BadmintonHall.Build -logFile ext14.log
//
// It is RE-RUNNABLE: every run deletes the old model and builds a fresh one.
//
// What it touches: the badminton court object in Exterior (named POI_BadmintonCourt the first time, then
// EXT14_BadmintonHall). Same as the shed builders: the object is renamed and the old placeholder "Cube" under it is
// deleted. Its position, its POITriggerVolume and trigger collider (poiId badminton_court) are left as they are.
//
// The hall, cabin, goals and container were placed from a reference frame taken on the plaza looking north through the gap
// between the main building and the old shed (see the constants below). The hall's POI object is MOVED to RootX, RootZ.
//
// Layout in the model's own frame (metres; y = 0 is the ground, the object stands at world x 66.5, z 27):
//
//      x = -9.5 .. +9.5   the length of the hall      z = -4 .. +4   its width
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT14_BadmintonHall
{
    const string RootName = "EXT14_BadmintonHall";
    const string OldRootName = "POI_BadmintonCourt";
    const string ModelName = "Model";
    const string Prefix = "EXT14";

    // Where the hall stands (world x, z), copied from the reference frame: its west end is just east of the main building's
    // east wall (x 53.5), behind the gap between the building and the old shed. The y (height 1) of the object is left as it was.
    const float RootX = 64.3f;
    const float RootZ = 26f;

    // The small white cabin, the two goals and the rust container (world x, z), from the same frame: the cabin stands in the
    // gap in front of the hall, the goals beside it in front of the shed's west end, the container behind the cabin.
    // The cabin stands against the old shed's west end (x 60.6 .. 63.8, z 9.7 .. 14.7), like the white room in the team's Unity view, with
    // its door facing west toward the walkway. It used to stand in front of the hall and cover it.
    const float CabinWorldX = 62.2f;
    const float CabinWorldZ = 12.2f;
    const float GoalWorldX = 57.6f;      // west of the cabin, so they do not overlap it
    static readonly float[] GoalWorldZs = { 10.4f, 14.4f };
    // Moved by the team in the Unity Scene view to local (-1.05, -0.91, -9.37) and shortened to 1.82 m. The height (-0.91) buried it,
    // so it is kept on the ground here; x, z and the size are exactly as the team set them.
    const float ContainerWorldX = 63.25f;
    const float ContainerWorldZ = 16.63f;
    const float ContainerLength = 1.82f;

    const float HalfLength = 9.5f;
    const float HalfWidth = 4f;
    const float WallHeight = 2.6f;
    const float RoofRise = 4.6f;         // the barrel roof rises this far above the walls (top = 7.2 m)
    const int RoofSegments = 16;         // the round roof is built from this many slices across its width

    [MenuItem("Campus/Build EXT14 Badminton Hall")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT14_BadmintonHall", BuildInternal);
    }

    static void BuildInternal()
    {
        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        GameObject root = SiteBuilderUtil.FindAndRenameRoot(scene, RootName, OldRootName);
        SiteBuilderUtil.DeleteChildren(root, "Cube", ModelName);
        root.transform.position = new Vector3(RootX, root.transform.position.y, RootZ);

        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets", "Meshes");
        SiteBuilderUtil.EnsureFolder("Assets/Meshes", "Sites");

        Material green = SiteBuilderUtil.GetMaterial(Prefix, "HallGreen", new Color32(34, 78, 62, 255), 0.1f);
        Material greenLight = SiteBuilderUtil.GetMaterial(Prefix, "HallGreenLight", new Color32(58, 104, 84, 255), 0.1f);
        Material white = SiteBuilderUtil.GetMaterial(Prefix, "CabinWhite", new Color32(232, 234, 232, 255), 0.05f);
        Material dark = SiteBuilderUtil.GetMaterial(Prefix, "DoorDark", new Color32(46, 52, 56, 255), 0.3f);
        Material frame = SiteBuilderUtil.GetMaterial(Prefix, "GoalWhite", new Color32(238, 238, 234, 255), 0.2f);
        Material net = SiteBuilderUtil.GetMaterial(Prefix, "GoalNet", new Color32(200, 204, 204, 255), 0.1f);

        Transform model = SiteBuilderUtil.CreateModel(root, ModelName);

        // Low walls (one solid box with a collider) and a lighter band of panels along the front (-z) side.
        SiteBuilderUtil.Part(model, "Walls", PrimitiveType.Cube, new Vector3(0f, WallHeight / 2f, 0f),
            new Vector3(HalfLength * 2f, WallHeight, HalfWidth * 2f), green, true);
        for (int i = 0; i < 6; i++)
        {
            SiteBuilderUtil.Part(model, "Panel_" + (i + 1), PrimitiveType.Cube,
                new Vector3(-HalfLength + 2.2f + i * 3.1f, 1.6f, -HalfWidth - 0.03f), new Vector3(1.5f, 1.3f, 0.05f), greenLight, false);
        }
        SiteBuilderUtil.Part(model, "Door", PrimitiveType.Cube, new Vector3(HalfLength - 2f, 1.1f, -HalfWidth - 0.04f), new Vector3(1.8f, 2.2f, 0.06f), dark, false);

        BuildBarrelRoof(model, green);
        BuildCabin(model, white, dark);
        BuildGoals(model, frame, net);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT14_BadmintonHall: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // The round roof: a half-ellipse cross-section (across z) pulled along x, plus a half-ellipse cap at each end.
    // Every triangle is added facing both ways, so it is solid seen from inside or out. No collider.
    static void BuildBarrelRoof(Transform model, Material material)
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var indices = new System.Collections.Generic.List<int>();

        System.Action<Vector3, Vector3, Vector3> addTriangle = (a, b, c) =>
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, a, c, b });
            indices.AddRange(new[] { start, start + 1, start + 2, start + 3, start + 4, start + 5 });
        };

        for (int i = 0; i < RoofSegments; i++)
        {
            float t0 = Mathf.PI * i / RoofSegments;
            float t1 = Mathf.PI * (i + 1) / RoofSegments;
            Vector3 a0 = new Vector3(-HalfLength, RoofRise * Mathf.Sin(t0), -HalfWidth * Mathf.Cos(t0));
            Vector3 a1 = new Vector3(-HalfLength, RoofRise * Mathf.Sin(t1), -HalfWidth * Mathf.Cos(t1));
            Vector3 b0 = new Vector3(HalfLength, a0.y, a0.z);
            Vector3 b1 = new Vector3(HalfLength, a1.y, a1.z);
            addTriangle(a0, a1, b1);
            addTriangle(a0, b1, b0);
            addTriangle(new Vector3(-HalfLength, 0f, 0f), a1, a0);     // west cap
            addTriangle(new Vector3(HalfLength, 0f, 0f), b0, b1);      // east cap
        }

        var mesh = new Mesh { name = "EXT14_Hall_Roof" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        const string path = "Assets/Meshes/Sites/EXT14_Hall_Roof.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);

        var go = new GameObject("BarrelRoof");
        go.transform.SetParent(model, false);
        go.transform.localPosition = new Vector3(0f, WallHeight, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    // The small white portable cabin beside the hall (west end), with a window and a door, and a flat roof.
    static void BuildCabin(Transform model, Material white, Material dark)
    {
        Transform cabin = SiteBuilderUtil.Group(model, "Cabin");
        cabin.localPosition = new Vector3(CabinWorldX - RootX, 0f, CabinWorldZ - RootZ);
        cabin.localRotation = Quaternion.Euler(0f, 90f, 0f);   // long side along z, front (door) facing west
        SiteBuilderUtil.Part(cabin, "Walls", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(5f, 2.8f, 3.2f), white, true);
        SiteBuilderUtil.Part(cabin, "Roof", PrimitiveType.Cube, new Vector3(0f, 2.9f, 0f), new Vector3(5.4f, 0.2f, 3.6f), white, false);
        SiteBuilderUtil.Part(cabin, "Window", PrimitiveType.Cube, new Vector3(-0.8f, 1.6f, -1.63f), new Vector3(0.9f, 1.0f, 0.05f), dark, false);
        SiteBuilderUtil.Part(cabin, "Door", PrimitiveType.Cube, new Vector3(1.4f, 1.1f, -1.63f), new Vector3(0.9f, 2.1f, 0.05f), dark, false);

        // Two potted plants by the door.
        Material pot = SiteBuilderUtil.GetMaterial(Prefix, "Pot", new Color32(180, 90, 60, 255), 0.1f);
        Material plant = SiteBuilderUtil.GetMaterial(Prefix, "PotPlant", new Color32(60, 120, 50, 255), 0.05f);
        foreach (float x in new[] { -0.2f, 2.0f })
        {
            SiteBuilderUtil.Part(cabin, "Pot", PrimitiveType.Cylinder, new Vector3(x, 0.2f, -2.2f), new Vector3(0.4f, 0.2f, 0.4f), pot, false);
            SiteBuilderUtil.Part(cabin, "Plant", PrimitiveType.Sphere, new Vector3(x, 0.6f, -2.2f), new Vector3(0.5f, 0.5f, 0.5f), plant, false);
        }

        // The rust-brown shipping container behind the cabin.
        Material rust = SiteBuilderUtil.GetMaterial(Prefix, "Container", new Color32(150, 80, 56, 255), 0.1f);
        SiteBuilderUtil.Part(model, "Container", PrimitiveType.Cube, new Vector3(ContainerWorldX - RootX, 1.3f, ContainerWorldZ - RootZ), new Vector3(2.3f, 2.6f, ContainerLength), rust, true);
    }

    // Two white football goals standing in front of the hall: a frame of three thin posts with a back bar and a net panel.
    static void BuildGoals(Transform model, Material frame, Material net)
    {
        Transform goals = SiteBuilderUtil.Group(model, "FootballGoals");
        goals.localPosition = Vector3.zero;
        for (int g = 0; g < 2; g++)
        {
            Transform goal = SiteBuilderUtil.Group(goals, "Goal_" + (g + 1));
            goal.localPosition = new Vector3(GoalWorldX - RootX, 0f, GoalWorldZs[g] - RootZ);
            goal.localRotation = Quaternion.Euler(0f, 90f, 0f);   // the open front faces west, the net is behind (east)
            foreach (float x in new[] { -1.5f, 1.5f })
            {
                SiteBuilderUtil.Part(goal, "Post", PrimitiveType.Cylinder, new Vector3(x, 1.0f, 0f), new Vector3(0.08f, 1.0f, 0.08f), frame, false);
            }
            SiteBuilderUtil.Part(goal, "Bar", PrimitiveType.Cube, new Vector3(0f, 2.0f, 0f), new Vector3(3.1f, 0.08f, 0.08f), frame, false);
            SiteBuilderUtil.Part(goal, "Net", PrimitiveType.Cube, new Vector3(0f, 1.0f, 1.0f), new Vector3(3f, 2f, 0.02f), net, false);
        }
    }
}
