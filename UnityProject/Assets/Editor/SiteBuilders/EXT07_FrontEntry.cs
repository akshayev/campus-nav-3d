using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT07 front entrance of the main building, copied from the reference photos:
//   - a porch with a red-tiled hip roof on pink round pillars, in front of the left wing of the building,
//     with three steps at the front and a ramp with a steel railing along the building wall
//   - a brick path with red / yellow flowering bushes across the lawn south of the plaza (the plaza itself has no plants)
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT07 Front Entry
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT07_FrontEntry.Build -logFile ext07.log
//
// It is RE-RUNNABLE: every run deletes the old EXT07_FrontEntry object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT07_FrontEntry, to Exterior.unity. Nothing that
// already exists is changed, and the main building itself is not edited: the porch simply stands in front
// of its front wall (z = 3.47). The entrance doorway (x 16..24) is left completely clear.
//
// Layout. World units, metres. The building's front faces -z; the porch sticks out toward -z.
//
//      z = +3.4   building wall          z = -3.4   front edge of the porch floor
//
// This file lives in an "Editor" folder, so it is never part of the game build. It needs EXT05 to have
// been built once first (it reuses EXT05's roof-mesh and palm methods).
public static class EXT07_FrontEntry
{
    const string RootName = "EXT07_FrontEntry";
    const string Prefix = "EXT07";

    // Porch.
    const float PorchMinX = -13f;        // in front of the left glass wing, as in the photo (and clear of the avatar's spawn at x 0)
    const float PorchMaxX = -1f;
    const float WallZ = 3.4f;            // the building's front wall
    const float PorchFrontZ = -3.4f;     // front edge of the porch floor
    const float FloorHeight = 0.6f;      // porch floor height above the plaza
    const float PillarHeight = 3.4f;     // from the floor up to the roof beam
    const float PillarRadius = 0.28f;
    const float RoofOverhang = 1.2f;
    const float RoofRise = 2.4f;
    const int StepCount = 3;
    const float StepRun = 0.4f;
    const float StepsMinX = -8.5f;       // the steps run across the middle of the front
    const float StepsMaxX = -5.5f;

    // Ramp along the wall, rising from the plaza at the east end up to the porch floor.
    const float RampStartX = 8f;         // low end, on the ground (well short of the entrance gap, which starts at x 16)
    const float RampMinZ = 1.4f;
    const float RampMaxZ = 3.0f;

    // Foxtail palms and ixora bushes beside the main entrance (x of each, in front of the wall).
    static readonly float[] PalmXs = { 27f, 31f, 35f };
    const float PalmZ = -3f;
    const float PalmHeight = 4.4f;
    static readonly float[][] Bushes =
    {
        new[] { 25f, -6f, 0f }, new[] { 29f, -6.5f, 1f }, new[] { 33f, -6f, 0f }, new[] { 37f, -6.5f, 1f },
        new[] { 41f, -6f, 0f },
    };

    [MenuItem("Campus/Build EXT07 Front Entry")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT07_FrontEntry", BuildInternal);
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

        Material pink = SiteBuilderUtil.GetMaterial(Prefix, "PillarPink", new Color32(236, 160, 142, 255), 0.02f);
        Material roofTile = SiteBuilderUtil.GetMaterial(Prefix, "RoofTile", new Color32(160, 54, 44, 255), 0.05f);
        Material floor = SiteBuilderUtil.GetMaterial(Prefix, "PorchFloor", new Color32(118, 108, 102, 255), 0.02f);
        Material steel = SiteBuilderUtil.GetMaterial(Prefix, "RailSteel", new Color32(170, 174, 178, 255), 0.3f);
        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailTrunk", new Color32(150, 142, 128, 255), 0.02f);
        Material frond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(146, 176, 64, 255), 0.05f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "BushGreen", new Color32(58, 120, 50, 255), 0.05f);
        Material red = SiteBuilderUtil.GetMaterial(Prefix, "IxoraRed", new Color32(212, 44, 36, 255), 0.1f);
        Material yellow = SiteBuilderUtil.GetMaterial(Prefix, "IxoraYellow", new Color32(236, 200, 40, 255), 0.1f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        BuildPorch(root.transform, pink, roofTile, floor);
        BuildRamp(root.transform, floor, steel);

        BuildFlowerPath(root.transform, leaves, red, yellow);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT07_FrontEntry: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // The porch: raised floor (collider), three steps, six round pillars on square bases, a pink beam and
    // a red tiled hip roof.
    static void BuildPorch(Transform parent, Material pink, Material roofTile, Material floor)
    {
        Transform porch = SiteBuilderUtil.Group(parent, "Porch");
        float width = PorchMaxX - PorchMinX;
        float depth = WallZ - PorchFrontZ;
        float centerX = (PorchMinX + PorchMaxX) / 2f;
        float centerZ = (WallZ + PorchFrontZ) / 2f;

        SiteBuilderUtil.Part(porch, "Floor", PrimitiveType.Cube, new Vector3(centerX, FloorHeight / 2f, centerZ),
            new Vector3(width, FloorHeight, depth), floor, true);

        // Steps on the front, middle. The top step is the highest and nearest the porch.
        float stepRise = FloorHeight / (StepCount + 1);
        for (int i = 0; i < StepCount; i++)
        {
            float height = stepRise * (StepCount - i);
            SiteBuilderUtil.Part(porch, "Step_" + (i + 1), PrimitiveType.Cube,
                new Vector3((StepsMinX + StepsMaxX) / 2f, height / 2f, PorchFrontZ - StepRun * (i + 0.5f)),
                new Vector3(StepsMaxX - StepsMinX, height, StepRun), floor, true);
        }

        // Pillars: front row and back row, each on a square base.
        float[] xs = { PorchMinX + 1f, centerX, PorchMaxX - 1f };
        float[] zs = { PorchFrontZ + 0.8f, WallZ - 0.9f };
        foreach (float z in zs)
        {
            foreach (float x in xs)
            {
                SiteBuilderUtil.Part(porch, "PillarBase", PrimitiveType.Cube, new Vector3(x, FloorHeight + 0.35f, z),
                    new Vector3(0.9f, 0.7f, 0.9f), pink, false);
                SiteBuilderUtil.Part(porch, "Pillar", PrimitiveType.Cylinder,
                    new Vector3(x, FloorHeight + 0.7f + (PillarHeight - 0.7f) / 2f, z),
                    new Vector3(PillarRadius * 2f, (PillarHeight - 0.7f) / 2f, PillarRadius * 2f), pink, true);
            }
        }

        // A pink beam round the top of the pillars, then the red tiled hip roof.
        float beamY = FloorHeight + PillarHeight + 0.15f;
        SiteBuilderUtil.Part(porch, "Beam", PrimitiveType.Cube, new Vector3(centerX, beamY, centerZ),
            new Vector3(width + 0.8f, 0.3f, depth + 0.4f), pink, false);
        float roofWidth = width + RoofOverhang * 2f;
        float roofDepth = depth + RoofOverhang * 2f;
        EXT05_FrontInterlock.BuildHipRoof(porch, "Roof", new Vector3(centerX, beamY + 0.15f, centerZ), roofWidth, roofDepth,
            RoofRise, Mathf.Max(0f, (roofWidth - roofDepth) / 2f), roofTile, Prefix);
    }

    // The ramp: a sloping slab along the wall from the ground at the east up to the porch floor, with a
    // steel railing on its open (front) side. The slab has a collider; its slope is gentle (about 6 degrees).
    static void BuildRamp(Transform parent, Material floor, Material steel)
    {
        Transform ramp = SiteBuilderUtil.Group(parent, "Ramp");
        float run = RampStartX - PorchMaxX;
        float angle = Mathf.Atan2(FloorHeight, run) * Mathf.Rad2Deg;
        float length = Mathf.Sqrt(run * run + FloorHeight * FloorHeight);
        float midX = (RampStartX + PorchMaxX) / 2f;
        float zCenter = (RampMinZ + RampMaxZ) / 2f;

        // Rotating by -angle about z lifts the west (porch) end.
        SiteBuilderUtil.Part(ramp, "Slab", PrimitiveType.Cube, new Vector3(midX, FloorHeight / 2f - 0.05f, zCenter),
            new Vector3(length, 0.1f, RampMaxZ - RampMinZ), new Vector3(0f, 0f, -angle), floor, true);

        // Railing on the front side (the -z edge of the ramp).
        float railZ = RampMinZ;
        int posts = 5;
        for (int i = 0; i < posts; i++)
        {
            float t = i / (float)(posts - 1);
            float x = Mathf.Lerp(RampStartX, PorchMaxX, t);
            float groundY = FloorHeight * t;
            SiteBuilderUtil.Part(ramp, "Post_" + (i + 1), PrimitiveType.Cylinder, new Vector3(x, groundY + 0.45f, railZ),
                new Vector3(0.06f, 0.45f, 0.06f), steel, false);
        }
        foreach (float h in new[] { 0.9f, 0.5f })
        {
            SiteBuilderUtil.Part(ramp, "Bar", PrimitiveType.Cube, new Vector3(midX, FloorHeight / 2f + h, railZ),
                new Vector3(length, 0.05f, 0.05f), new Vector3(0f, 0f, -angle), steel, false);
        }
    }

    // The brick path with flowers, copied from the reference photo: it leaves the plaza at x -12.8 (the spot the team picked) and
    // runs south across the lawn, with red ixora bushes along its east side and yellow shrubs in white tree guards
    // along its west side. The college front itself (the plaza) has no plants, as in the photo of the front.
    static void BuildFlowerPath(Transform parent, Material leaves, Material red, Material yellow)
    {
        Transform garden = SiteBuilderUtil.Group(parent, "FlowerPath");
        const float pathMinX = -14.3f;      // path centred on x -12.8 (chosen by the team in the Unity Scene view)
        const float pathMaxX = -11.3f;
        const float nearZ = -12.6f;         // starts at the plaza edge
        const float farZ = -36.2f;           // centred on z -24.4
        const float thickness = 0.05f;

        Material brick = SiteBuilderUtil.GetMaterial(Prefix, "PathBrick", new Color32(255, 255, 255, 255), 0.1f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (texture != null)
        {
            brick.mainTexture = texture;
            brick.mainTextureScale = new Vector2((pathMaxX - pathMinX) / 0.8f, (nearZ - farZ) / 0.8f);
        }
        else
        {
            brick.color = new Color32(124, 104, 94, 255);
        }
        Material guard = SiteBuilderUtil.GetMaterial(Prefix, "TreeGuard", new Color32(236, 236, 230, 255), 0.1f);

        SiteBuilderUtil.Part(garden, "Paving", PrimitiveType.Cube, new Vector3((pathMinX + pathMaxX) / 2f, thickness / 2f, (nearZ + farZ) / 2f),
            new Vector3(pathMaxX - pathMinX, thickness, nearZ - farZ), brick, false);

        const int perSide = 8;
        for (int i = 0; i < perSide; i++)
        {
            float z = nearZ - 2.2f - i * ((nearZ - farZ - 4.4f) / (perSide - 1));
            EXT05_FrontInterlock.BuildFlowerBush(garden, "Ixora_" + (i + 1), pathMaxX + 1.3f, z, leaves, red);
            EXT05_FrontInterlock.BuildFlowerBush(garden, "Shrub_" + (i + 1), pathMinX - 1.3f, z, leaves, yellow);
            SiteBuilderUtil.Part(garden, "Guard_" + (i + 1), PrimitiveType.Cylinder, new Vector3(pathMinX - 1.3f, 0.4f, z),
                new Vector3(0.25f, 0.4f, 0.25f), guard, false);
        }
    }
}
