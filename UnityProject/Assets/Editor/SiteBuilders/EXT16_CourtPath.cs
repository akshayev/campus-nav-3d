using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT16 court paths in Exterior.unity, copied from the reference photos of the bike shed front: two brick
// ("interlock") ways branch off the straight way and lead into the bike shed, one at each end.
//   - the RIGHT way: a smooth curve that leaves the way and runs to the right part of the shed front
//   - the LEFT way: a smooth curve that leaves the way, runs west along the lawn and curves down into the gap between the
//     bike shed and the car shed, ending in a short straight piece between the two sheds
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT16 Court Path
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT16_CourtPath.Build -logFile ext16.log
//
// It is RE-RUNNABLE: every run deletes the old EXT16_CourtPath object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT16_CourtPath, to Exterior.unity. Nothing that already exists
// is changed. The paths are visual only (no collider). It needs the brick picture made by EXT05, so build EXT05 first.
//
// Each curve is a cubic Bezier laid as many overlapping slabs, each turned to follow it, so the brick looks like one
// continuous strip. Neighbouring slabs are raised by 3 mm alternately, so where they overlap their tops never flicker
// against each other. Change the points below to bend the ways differently.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT16_CourtPath
{
    const string RootName = "EXT16_CourtPath";
    const string Prefix = "EXT16";

    // The straight way (EXT15) and the road to the shed are centred on x 62.2 and 2.6 m wide; both curves start on its
    // centre line, hidden under it, so they join it without a seam.
    const float WayX = 62.2f;

    // RIGHT way: leaves the way at z -31.5, swings east and then south to the right part of the shed front (z -41.9).
    static readonly Vector2[] Right =
    {
        new Vector2(WayX, -31.5f), new Vector2(68f, -31.8f), new Vector2(70.5f, -35.5f), new Vector2(70.5f, -41.9f),
    };

    // LEFT way: leaves the way at z -32.5, runs west south of the volleyball court and curves into the gap between the
    // bike shed (west end at x 43.1) and the car shed (east end at about x 39).
    static readonly Vector2[] Left =
    {
        new Vector2(WayX, -32.5f), new Vector2(54f, -33f), new Vector2(46f, -35.5f), new Vector2(41.0f, -41.5f),
    };

    // Short straight piece between the two sheds, continuing the left way south (x, z start, z end).
    const float GapX = 41.0f;
    const float GapNearZ = -41.5f;
    const float GapFarZ = -49.5f;

    const float PathWidth = 2.6f;
    const float GapWidth = 2.2f;         // narrower, to fit between the sheds
    const float Thickness = 0.045f;
    const float BaseHeight = 0.003f;     // the curves sit 3 mm above the straight way, so they never flicker against it
    const int Slabs = 48;
    const float Overlap = 1.6f;          // each slab is this much longer than the step between slabs
    const float TileSize = 0.8f;

    [MenuItem("Campus/Build EXT16 Court Path")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT16_CourtPath", BuildInternal);
    }

    static Vector2 Point(Vector2[] p, float t)
    {
        float u = 1f - t;
        return u * u * u * p[0] + 3f * u * u * t * p[1] + 3f * u * t * t * p[2] + t * t * t * p[3];
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

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        float rightLength = BuildCurve(root.transform, "Right", Right);
        float leftLength = BuildCurve(root.transform, "Left", Left);
        BuildGapStub(root.transform);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT16_CourtPath: built " + SiteBuilderUtil.CountObjects(root) + " objects, right way about " + rightLength.ToString("0")
            + " m, left way about " + leftLength.ToString("0") + " m.");
    }

    // Lays one curve as overlapping slabs and returns its length. Each curve has its own brick material so the brick keeps its size.
    static float BuildCurve(Transform parent, string name, Vector2[] points)
    {
        Transform group = SiteBuilderUtil.Group(parent, name);

        float length = 0f;
        for (int i = 0; i < Slabs; i++)
        {
            length += Vector2.Distance(Point(points, i / (float)Slabs), Point(points, (i + 1) / (float)Slabs));
        }
        float slabLength = length / Slabs * Overlap;

        Material brick = SiteBuilderUtil.GetMaterial(Prefix, name + "Brick", new Color32(255, 255, 255, 255), 0.1f);
        ApplyBrick(brick, PathWidth, slabLength);

        for (int i = 0; i < Slabs; i++)
        {
            Vector2 a = Point(points, i / (float)Slabs);
            Vector2 b = Point(points, (i + 1) / (float)Slabs);
            Vector2 mid = (a + b) / 2f;
            Vector2 direction = (b - a).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg; // the slab's long side (z) points along the path
            float lift = BaseHeight + (i % 2 == 0 ? 0f : 0.003f);

            GameObject slab = SiteBuilderUtil.Part(group, "Slab_" + (i + 1).ToString("00"), PrimitiveType.Cube,
                new Vector3(mid.x, Thickness / 2f + lift, mid.y), new Vector3(PathWidth, Thickness, slabLength), brick, false);
            slab.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        return length;
    }

    static void BuildGapStub(Transform parent)
    {
        Material brick = SiteBuilderUtil.GetMaterial(Prefix, "GapBrick", new Color32(255, 255, 255, 255), 0.1f);
        float length = GapNearZ - GapFarZ + 0.5f;    // starts 0.5 m inside the left curve's end, so there is no seam
        ApplyBrick(brick, GapWidth, length);
        SiteBuilderUtil.Part(parent, "GapWay", PrimitiveType.Cube, new Vector3(GapX, Thickness / 2f + BaseHeight, (GapNearZ + 0.5f + GapFarZ) / 2f),
            new Vector3(GapWidth, Thickness, length), brick, false);
    }

    static void ApplyBrick(Material brick, float width, float length)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (texture != null)
        {
            brick.mainTexture = texture;
            brick.mainTextureScale = new Vector2(width / TileSize, length / TileSize);
        }
        else
        {
            brick.color = new Color32(110, 96, 90, 255); // run Campus > Build EXT05 Front Yard first for the brick picture
        }
    }
}
