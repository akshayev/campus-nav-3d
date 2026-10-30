using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT15 bike shed front in Exterior.unity, copied from the reference photo taken in front of the bike
// shed: a brick road, a strip of lawn with two slim foxtail palms and a small leafy tree, and behind them (under
// the shed roof) a long mural wall and a row of parked motorbikes.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT15 Bike Shed Front
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT15_BikeShedFront.Build -logFile ext15.log
//
// It is RE-RUNNABLE: every run deletes the old EXT15_BikeShedFront object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT15_BikeShedFront, to Exterior.unity, and MOVES the team's bike
// shed object (EXT03_BikeShed) to BikeShedX, BikeShedZ. The shed's model is not changed: the mural wall and the bikes are separate objects placed
// inside its footprint. Build EXT05 first (brick picture, palm method), and EXT11 (sapling method).
//
// Layout (world metres). The bike shed stands at x 39.2 .. 60.2, z -47.8 .. -39.8 and its open front faces +z:
//
//      z = -47.4   mural wall along the back of the shed (x 39.7 .. 59.7)
//      z = -37.5   strip of lawn with the palms and the tree
//      z = -36.2 .. -30   brick road (x 51 .. 70)
//      x = 59.2 .. 61.8   straight brick way from the plaza edge (z -12.6) down to the shed front (z -39.8)
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT15_BikeShedFront
{
    const string RootName = "EXT15_BikeShedFront";
    const string Prefix = "EXT15";

    // Brick road in front of the shed.
    // The bike shed was moved by the team to BikeShedX, BikeShedZ (it was built at 49.7, -43.8; moved +5.4, -3.1 and then
    // -1.5, -0.3). Everything here follows it: the road, plants, wall and bikes below are the old numbers plus those shifts.
    // Layer 2 is "Ignore Raycast": the follow camera's collision ray ignores it, while the avatar still bumps into it. The wall and the bikes
    // stand where the camera would otherwise be pulled in right next to the avatar.
    const int CameraPassThroughLayer = 2;
    const string ShedRootName = "EXT03_BikeShed";
    const float BikeShedX = 59.6f;       // the shed is now 33 m long; its west end is still at x 43.1
    const float BikeShedZ = -47.2f;
    // The road to the shed: a north-south way that continues the straight way (x 62.2) straight down to the shed front
    // (z -42.9), same width: centre, turn (degrees around y), length (along its own x) and width.
    const float RoadCenterX = 62.2f;
    const float RoadCenterZ = -35.65f;
    const float RoadYaw = -90f;
    const float RoadLength = 14.9f;
    const float RoadWidth = 2.6f;
    const float RoadThickness = 0.04f;   // a little above the earth patch that lies here (0.02)
    const float TileSize = 0.8f;

    // Straight brick way from the plaza edge to the front of the shed, passing just east of the tower (x 55.3) and
    // across the road above. It ends at the shed front.
    const float WayMinX = 60.9f;         // centred on x 62.2, the spot the team picked in the Unity Scene view
    const float WayMaxX = 63.5f;
    const float WayNearZ = -12.4f;       // overlaps the plaza edge (z -12.6), so there is no gap
    const float WayFarZ = -28.3f;        // the straight part ends here; EXT16 bends it on to the shed front

    // Plants on the lawn strip: two slim foxtail palms and a small leafy tree (x, z).
    // As in the reference photo: two slim palms well apart on the lawn strip, and one small round tree at the right end.
    // The way comes in at x 62.2 (60.9 .. 63.5), so one palm stands each side of it, level with each other.
    static readonly float[][] Palms = { new[] { 53.5f, -40.4f }, new[] { 70.0f, -40.6f } };
    const float TreeX = 73.5f;
    const float TreeZ = -40.8f;

    // Brick apron in front of the lawn strip (so the plaza-style brick comes right up to the lawn, as in the photo), with
    // the lettering that now lives on the plaza.
    const float ApronMinX = 55f;
    const float ApronMaxX = 76.5f;
    const float ApronMinZ = -36.6f;
    const float ApronMaxZ = -29f;
    const float ApronThickness = 0.03f;      // below the ways (0.04+) so they show on top

    // Mural wall along the back of the shed.
    const float WallMinX = 43.6f;
    const float WallMaxX = 75.6f;
    const float WallZ = -50.8f;
    const float WallHeight = 1.9f;

    // Parked motorbikes: x positions along the shed (z fixed, nose toward the front).
    const int BikeCount = 14;
    const float BikeFirstX = 44.9f;
    const float BikeSpacing = 2.2f;
    const float BikeZ = -48.7f;

    [MenuItem("Campus/Build EXT15 Bike Shed Front")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT15_BikeShedFront", BuildInternal);
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

        // Move the team's bike shed (root EXT03_BikeShed) to its new spot. Only its position changes; its trigger and the
        // poi id stay as they are. Nothing happens if the shed is missing.
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == ShedRootName)
            {
                candidate.transform.position = new Vector3(BikeShedX, candidate.transform.position.y, BikeShedZ);
            }
        }

        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");

        Material brick = SiteBuilderUtil.GetMaterial(Prefix, "RoadBrick", new Color32(255, 255, 255, 255), 0.1f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (texture != null)
        {
            brick.mainTexture = texture;
            brick.mainTextureScale = new Vector2(RoadLength / TileSize, RoadWidth / TileSize);
        }
        else
        {
            brick.color = new Color32(110, 96, 90, 255); // run Campus > Build EXT05 Front Yard first for the brick picture
        }

        Material trunk = SiteBuilderUtil.GetMaterial(Prefix, "PalmTrunk", new Color32(150, 142, 128, 255), 0.02f);
        Material frond = SiteBuilderUtil.GetMaterial(Prefix, "FoxtailFrond", new Color32(156, 186, 74, 255), 0.05f);
        Material treeTrunk = SiteBuilderUtil.GetMaterial(Prefix, "TreeTrunk", new Color32(96, 76, 58, 255), 0.02f);
        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "TreeLeaves", new Color32(80, 140, 56, 255), 0.05f);
        Material wallBase = SiteBuilderUtil.GetMaterial(Prefix, "WallBase", new Color32(200, 196, 188, 255), 0.02f);
        Material bikeBody = SiteBuilderUtil.GetMaterial(Prefix, "BikeBody", new Color32(40, 42, 48, 255), 0.3f);
        Material bikeRed = SiteBuilderUtil.GetMaterial(Prefix, "BikeRed", new Color32(150, 36, 36, 255), 0.3f);
        Material tyre = SiteBuilderUtil.GetMaterial(Prefix, "Tyre", new Color32(24, 24, 26, 255), 0.05f);
        Material[] mural =
        {
            SiteBuilderUtil.GetMaterial(Prefix, "MuralGreen", new Color32(58, 150, 110, 255), 0.05f),
            SiteBuilderUtil.GetMaterial(Prefix, "MuralRed", new Color32(196, 56, 54, 255), 0.05f),
            SiteBuilderUtil.GetMaterial(Prefix, "MuralOrange", new Color32(226, 140, 52, 255), 0.05f),
            SiteBuilderUtil.GetMaterial(Prefix, "MuralTeal", new Color32(50, 128, 150, 255), 0.05f),
            SiteBuilderUtil.GetMaterial(Prefix, "MuralDark", new Color32(40, 38, 48, 255), 0.05f),
            SiteBuilderUtil.GetMaterial(Prefix, "MuralPink", new Color32(214, 120, 150, 255), 0.05f),
        };

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        GameObject road = SiteBuilderUtil.Part(root.transform, "Road", PrimitiveType.Cube,
            new Vector3(RoadCenterX, RoadThickness / 2f, RoadCenterZ),
            new Vector3(RoadLength, RoadThickness, RoadWidth), brick, false);
        road.transform.localRotation = Quaternion.Euler(0f, RoadYaw, 0f);

        // The apron, and the big white letters painted on it.
        Material apronBrick = SiteBuilderUtil.GetMaterial(Prefix, "ApronBrick", new Color32(255, 255, 255, 255), 0.1f);
        if (texture != null)
        {
            apronBrick.mainTexture = texture;
            apronBrick.mainTextureScale = new Vector2((ApronMaxX - ApronMinX) / TileSize, (ApronMaxZ - ApronMinZ) / TileSize);
        }
        else
        {
            apronBrick.color = new Color32(110, 96, 90, 255);
        }
        SiteBuilderUtil.Part(root.transform, "Apron", PrimitiveType.Cube,
            new Vector3((ApronMinX + ApronMaxX) / 2f, ApronThickness / 2f, (ApronMinZ + ApronMaxZ) / 2f),
            new Vector3(ApronMaxX - ApronMinX, ApronThickness, ApronMaxZ - ApronMinZ), apronBrick, false);
        // (The big white lettering was moved to the main plaza, EXT05: it is painted on the main interlock, not on this apron.)

        // The straight way from the plaza (its own material, so the brick keeps its size).
        Material wayBrick = SiteBuilderUtil.GetMaterial(Prefix, "WayBrick", new Color32(255, 255, 255, 255), 0.1f);
        if (texture != null)
        {
            wayBrick.mainTexture = texture;
            wayBrick.mainTextureScale = new Vector2((WayMaxX - WayMinX) / TileSize, (WayNearZ - WayFarZ) / TileSize);
        }
        else
        {
            wayBrick.color = new Color32(110, 96, 90, 255);
        }
        SiteBuilderUtil.Part(root.transform, "StraightWay", PrimitiveType.Cube,
            new Vector3((WayMinX + WayMaxX) / 2f, RoadThickness / 2f + 0.005f, (WayNearZ + WayFarZ) / 2f),
            new Vector3(WayMaxX - WayMinX, RoadThickness, WayNearZ - WayFarZ), wayBrick, false);

        Transform plants = SiteBuilderUtil.Group(root.transform, "StripPlants");
        for (int i = 0; i < Palms.Length; i++)
        {
            EXT05_FrontInterlock.BuildPalm(plants, "FoxtailPalm_" + (i + 1), Palms[i][0], Palms[i][1], i * 33f, trunk, frond, 7.0f, 3.6f, true, 0.09f);   // tall, slim trunks, like the photo
        }
        BuildSmallTree(plants, treeTrunk, leaves);

        Transform muralWall = BuildMuralWall(root.transform, wallBase, mural);
        // The team's ShedRoofFader, used unchanged: it hides the wall while the wall is between the camera and the avatar (the wall is
        // passed through by the camera, so it would otherwise cover the avatar). It is never hidden for "standing inside" (zero footprint).
        var wallFader = muralWall.gameObject.AddComponent<ShedRoofFader>();
        var wallSerialized = new SerializedObject(wallFader);
        wallSerialized.FindProperty("roofGroup").objectReferenceValue = muralWall;
        wallSerialized.FindProperty("footprintHalfSize").vector2Value = new Vector2(0.01f, 0.01f);
        wallSerialized.FindProperty("footprintCenter").vector2Value = new Vector2(-1000f, -1000f);
        wallSerialized.FindProperty("margin").floatValue = 0f;
        wallSerialized.FindProperty("boundsHeightPadding").floatValue = 0.2f;
        wallSerialized.ApplyModifiedPropertiesWithoutUndo();
        BuildBikes(root.transform, bikeBody, bikeRed, tyre);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT15_BikeShedFront: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // A small round tree, like the one at the right of the photo: a short trunk and a few green balls.
    static void BuildSmallTree(Transform parent, Material trunk, Material leaves)
    {
        Transform tree = SiteBuilderUtil.Group(parent, "SmallTree");
        tree.localPosition = new Vector3(TreeX, 0f, TreeZ);
        SiteBuilderUtil.Part(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(0.2f, 1.0f, 0.2f), trunk, true);
        SiteBuilderUtil.Part(tree, "Leaves_1", PrimitiveType.Sphere, new Vector3(0f, 2.6f, 0f), new Vector3(2.6f, 2.2f, 2.6f), leaves, false);
        SiteBuilderUtil.Part(tree, "Leaves_2", PrimitiveType.Sphere, new Vector3(0.7f, 2.0f, 0.3f), new Vector3(1.6f, 1.4f, 1.6f), leaves, false);
    }

    // A low wall along the back of the shed, painted in blocks of colour like the mural in the photo: a pale plinth
    // and a row of coloured panels, each a slightly different width. Has a collider (the bikes stand in front of it).
    static Transform BuildMuralWall(Transform parent, Material baseMat, Material[] mural)
    {
        Transform wall = SiteBuilderUtil.Group(parent, "MuralWall");
        float length = WallMaxX - WallMinX;
        SiteBuilderUtil.Part(wall, "Wall", PrimitiveType.Cube, new Vector3((WallMinX + WallMaxX) / 2f, WallHeight / 2f, WallZ),
            new Vector3(length, WallHeight, 0.3f), baseMat, true).layer = CameraPassThroughLayer;
        var random = new System.Random(15);
        float x = WallMinX;
        int index = 0;
        while (x < WallMaxX - 0.5f)
        {
            float w = Mathf.Min(1.4f + (float)random.NextDouble() * 1.6f, WallMaxX - x);
            SiteBuilderUtil.Part(wall, "Panel_" + (index + 1), PrimitiveType.Cube, new Vector3(x + w / 2f, WallHeight * 0.55f, WallZ + 0.17f),
                new Vector3(w - 0.08f, WallHeight * 0.8f, 0.04f), mural[index % mural.Length], false);
            x += w;
            index++;
        }
        return wall;
    }

    // A row of parked motorbikes standing nose-forward (toward +z): a dark body with a red tank, a seat, two wheels,
    // handlebars and a headlight. Each bike has one thin box collider. Colours alternate between dark and red.
    static void BuildBikes(Transform parent, Material body, Material red, Material tyre)
    {
        Transform bikes = SiteBuilderUtil.Group(parent, "ParkedBikes");
        for (int i = 0; i < BikeCount; i++)
        {
            Transform bike = SiteBuilderUtil.Group(bikes, "Bike_" + (i + 1));
            bike.localPosition = new Vector3(BikeFirstX + i * BikeSpacing, 0f, BikeZ);
            bike.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 8f, 0f);
            Material paint = i % 3 == 0 ? red : body;

            foreach (float z in new[] { -0.7f, 0.7f })
            {
                SiteBuilderUtil.Part(bike, "Wheel", PrimitiveType.Cylinder, new Vector3(0f, 0.32f, z), new Vector3(0.64f, 0.06f, 0.64f),
                    new Vector3(0f, 0f, 90f), tyre, false);
            }
            SiteBuilderUtil.Part(bike, "Engine", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.35f, 0.4f, 0.8f), body, false);
            SiteBuilderUtil.Part(bike, "Tank", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.1f), new Vector3(0.32f, 0.3f, 0.6f), paint, false);
            SiteBuilderUtil.Part(bike, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.9f, -0.35f), new Vector3(0.3f, 0.1f, 0.6f), body, false);
            SiteBuilderUtil.Part(bike, "Handlebar", PrimitiveType.Cube, new Vector3(0f, 1.1f, 0.65f), new Vector3(0.7f, 0.05f, 0.05f), body, false);

            bike.gameObject.layer = CameraPassThroughLayer;
            var collider = bike.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.7f, 1.1f, 1.9f);
            collider.center = new Vector3(0f, 0.55f, 0f);
        }
    }
}
