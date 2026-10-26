using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT14 indoor badminton hall in Exterior.unity, copied from the team's photos of the real hall:
//   OUTSIDE  - green corrugated walls on a pale concrete base, a pale mesh vent strip along each side, a grey barrel roof that
//              overhangs, pale windows high up on the arched end walls, a green door (swung open) at the left of the south end,
//              and an orange corrugated lean-to hut with red band and two doors against the east side
//   INSIDE   - walkable: grey corrugated walls over a low concrete wall, steel arch ribs and purlins under the roof, a green and
//              orange court with white lines and a net, ceiling lights, and bright high windows in the end walls
// plus the small white cabin, two white football goals and the rust container that stand in front of it.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT14 Badminton Hall
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT14_BadmintonHall.Build -logFile ext14.log
//
// It is RE-RUNNABLE: every run deletes the old model and builds a fresh one.
//
// What it touches: the badminton court object in Exterior (named POI_BadmintonCourt the first time, then EXT14_BadmintonHall).
// The object is renamed, the old placeholder "Cube" under it is deleted, and the object is MOVED to RootX, RootZ and TURNED to face
// south (its POITriggerVolume and the 27 x 3 x 16 m trigger box turn with it, so the trigger still covers the hall). Its poiId is
// not changed.
//
// Layout. The hall is built in its own frame (metres; y = 0 is the ground): the long axis is x (-9.5 .. +9.5), the width is z
// (-4 .. +4), and the arched END WALL with the door is the +x end. The root is turned 90 degrees, so that end faces world south (-z),
// toward the walkway between the main building and the old shed. The cabin, goals and container sit in a second group, "Extras",
// which turns with the root; their world positions are converted to its frame by ToLocal.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT14_BadmintonHall
{
    const string RootName = "EXT14_BadmintonHall";
    const string OldRootName = "POI_BadmintonCourt";
    const string ModelName = "Model";
    const string ExtrasName = "Extras";
    const string Prefix = "EXT14";

    // Where the hall stands (world x, z): its south end (the door end) at z 21.2, right behind the walkway between the main
    // building (east wall at x 54) and the old shed (x 65..74). The y (height 1) of the object is left as it was.
    const float RootX = 59.0f;
    const float RootZ = 30.7f;
    const float RootYaw = 90f;

    // The small white cabin, the two goals and the container (world x, z).
    // The cabin stands against the old shed's west end (x 60.6 .. 63.8, z 9.7 .. 14.7) with its door facing west toward the walkway.
    const float CabinWorldX = 62.2f;
    const float CabinWorldZ = 12.2f;
    const float GoalWorldX = 57.6f;      // west of the cabin, so they do not overlap it
    static readonly float[] GoalWorldZs = { 10.4f, 14.4f };
    // The container was moved by the team in the Unity Scene view (x, z and 1.82 m length are theirs; the height was buried, so it stands on the ground).
    const float ContainerWorldX = 63.25f;
    const float ContainerWorldZ = 16.63f;
    const float ContainerLength = 1.82f;

    // Hall size.
    const float HalfLength = 9.5f;       // inside, along x
    const float HalfWidth = 4.0f;        // inside, along z
    const float WallThickness = 0.16f;
    const float WallHeight = 3.4f;       // where the arch springs
    const float Rise = 4.0f;             // the arch rises this far above the walls (the ridge is at 7.4 m)
    const float BaseHeight = 1.0f;       // the low concrete wall inside
    const int ArchSegments = 18;

    // Door in the +x (south) end wall, at the left when seen from outside (world west).
    const float DoorZMin = -3.0f;
    const float DoorZMax = -1.4f;
    const float DoorHeight = 2.4f;

    // Badminton court (doubles): 13.4 m x 6.1 m, singles width 5.18 m; the net is at x = 0.
    const float CourtHalfLength = 6.7f;
    const float CourtHalfWidth = 3.05f;
    const float SinglesHalfWidth = 2.59f;
    const float ShortServiceX = 1.98f;
    const float LongServiceX = 5.94f;
    const float LineWidth = 0.05f;

    // Corrugated sheet texture: 8 ridges across 64 px.
    const string SheetTexturePath = "Assets/Textures/Sites/EXT14_Sheet.png";
    const float RidgePitch = 0.075f;     // metres between ridges

    [MenuItem("Campus/Build EXT14 Badminton Hall")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT14_BadmintonHall", BuildInternal);
    }

    static Vector3 ToLocal(float worldX, float worldZ)
    {
        // Root turned by +90 degrees: world x = local z, world z = -local x (relative to the root).
        return new Vector3(-(worldZ - RootZ), 0f, worldX - RootX);
    }

    class Mats
    {
        public Material wallGreenLong, wallGreenEnd, wallGreyLong, wallGreyEnd, gableOut, gableIn, concrete, vent, roof, steel;
        public Material floor, courtOrange, courtGreen, line, netWhite, window, lamp, doorGreen, doorMark, post;
        public Material leanOrange, leanOrangeSide, leanDark, leanRed, leanRoof;
        public Material cabinWhite, dark, pot, plant, goalFrame, goalNet, rust;
    }

    static void BuildInternal()
    {
        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        GameObject root = SiteBuilderUtil.FindAndRenameRoot(scene, RootName, OldRootName);
        SiteBuilderUtil.DeleteChildren(root, "Cube", ModelName, ExtrasName);
        root.transform.position = new Vector3(RootX, root.transform.position.y, RootZ);
        root.transform.rotation = Quaternion.Euler(0f, RootYaw, 0f);

        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets", "Meshes");
        SiteBuilderUtil.EnsureFolder("Assets/Meshes", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets", "Textures");
        SiteBuilderUtil.EnsureFolder("Assets/Textures", "Sites");

        Texture2D sheet = MakeSheetTexture();
        Mats m = MakeMaterials(sheet);

        Transform model = SiteBuilderUtil.CreateModel(root, ModelName);
        Transform extras = SiteBuilderUtil.CreateModel(root, ExtrasName);

        BuildFloorAndCourt(model, m);
        Transform sideWalls = BuildSideWalls(model, m);
        Transform endWalls = BuildEndWalls(model, m);
        Transform roofGroup = BuildRoofAndRibs(model, m);
        BuildLamps(model, m);
        BuildLeanTo(model, m);
        BuildCabin(extras, m);
        BuildGoals(extras, m);

        // The roof fader (the team's ShedRoofFader, used unchanged on the sheds): the follow camera looks down from above and behind,
        // so inside the hall the roof and walls would hide the avatar. This switches the roof and both sets of walls OFF while the
        // Player is inside the hall (plus 1.5 m round it) or they are between the camera and the Player, and back ON a moment after
        // neither is true. It works in the Model's own space, so it follows the hall's turn.
        var fader = model.gameObject.AddComponent<ShedRoofFader>();
        var serialized = new SerializedObject(fader);
        serialized.FindProperty("roofGroup").objectReferenceValue = roofGroup;
        SerializedProperty alsoHide = serialized.FindProperty("alsoHide");
        alsoHide.arraySize = 2;
        alsoHide.GetArrayElementAtIndex(0).objectReferenceValue = sideWalls;
        alsoHide.GetArrayElementAtIndex(1).objectReferenceValue = endWalls;
        serialized.FindProperty("footprintHalfSize").vector2Value = new Vector2(HalfLength + WallThickness, HalfWidth + WallThickness);
        serialized.FindProperty("margin").floatValue = 1.5f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT14_BadmintonHall: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // ------------------------------------------------------------------------------------
    // Materials and the sheet texture
    // ------------------------------------------------------------------------------------

    // A grey picture of vertical ridges (light on the ridge, darker in the groove). Tinted by each material's colour.
    static Texture2D MakeSheetTexture()
    {
        const int w = 64;
        const int h = 8;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float phase = (x % 8) / 8f;
                float value = 190f + 45f * Mathf.Sin(phase * Mathf.PI * 2f);
                byte v = (byte)Mathf.Clamp(value, 0f, 255f);
                pixels[y * w + x] = new Color32(v, v, v, 255);
            }
        }
        var made = new Texture2D(w, h, TextureFormat.RGBA32, false);
        made.SetPixels32(pixels);
        File.WriteAllBytes(SheetTexturePath, made.EncodeToPNG());
        Object.DestroyImmediate(made);

        AssetDatabase.ImportAsset(SheetTexturePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(SheetTexturePath);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 8;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SheetTexturePath);
    }

    // A ridged material: the picture repeats every 8 ridges (0.6 m), so a face 'length' metres wide gets length / 0.6 repeats.
    static Material Sheet(Texture2D sheet, string name, Color32 color, float length)
    {
        Material material = SiteBuilderUtil.GetMaterial(Prefix, name, color, 0.1f);
        material.mainTexture = sheet;
        material.mainTextureScale = new Vector2(length / (RidgePitch * 8f), 1f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Mats MakeMaterials(Texture2D sheet)
    {
        float longLength = 2f * (HalfLength + WallThickness);
        float endLength = 2f * (HalfWidth + WallThickness);
        var m = new Mats
        {
            wallGreenLong = Sheet(sheet, "WallGreenLong", new Color32(40, 112, 90, 255), longLength),
            wallGreenEnd = Sheet(sheet, "WallGreenEnd", new Color32(40, 112, 90, 255), endLength),
            wallGreyLong = Sheet(sheet, "WallGreyLong", new Color32(150, 156, 160, 255), longLength),
            wallGreyEnd = Sheet(sheet, "WallGreyEnd", new Color32(150, 156, 160, 255), endLength),
            gableOut = SiteBuilderUtil.GetMaterial(Prefix, "GableGreen", new Color32(40, 112, 90, 255), 0.1f),
            gableIn = SiteBuilderUtil.GetMaterial(Prefix, "GableGrey", new Color32(150, 156, 160, 255), 0.1f),
            concrete = SiteBuilderUtil.GetMaterial(Prefix, "Concrete", new Color32(176, 172, 162, 255), 0.02f),
            vent = SiteBuilderUtil.GetMaterial(Prefix, "VentMesh", new Color32(196, 204, 192, 255), 0.2f),
            roof = SiteBuilderUtil.GetMaterial(Prefix, "RoofGrey", new Color32(132, 138, 144, 255), 0.02f),
            steel = SiteBuilderUtil.GetMaterial(Prefix, "Steel", new Color32(56, 62, 70, 255), 0.3f),
            floor = SiteBuilderUtil.GetMaterial(Prefix, "Floor", new Color32(150, 146, 138, 255), 0.05f),
            courtOrange = SiteBuilderUtil.GetMaterial(Prefix, "CourtOrange", new Color32(206, 128, 100, 255), 0.05f),
            courtGreen = SiteBuilderUtil.GetMaterial(Prefix, "CourtGreen", new Color32(70, 132, 114, 255), 0.05f),
            line = SiteBuilderUtil.GetMaterial(Prefix, "CourtLine", new Color32(242, 240, 232, 255), 0.05f),
            netWhite = SiteBuilderUtil.GetMaterial(Prefix, "NetWhite", new Color32(236, 236, 232, 255), 0.1f),
            window = SiteBuilderUtil.GetMaterial(Prefix, "WindowPale", new Color32(224, 214, 184, 255), 0.1f, new Color32(120, 114, 92, 255)),
            lamp = SiteBuilderUtil.GetMaterial(Prefix, "Lamp", new Color32(255, 255, 240, 255), 0.1f, new Color32(255, 255, 230, 255)),
            doorGreen = SiteBuilderUtil.GetMaterial(Prefix, "DoorGreen", new Color32(48, 110, 92, 255), 0.2f),
            doorMark = SiteBuilderUtil.GetMaterial(Prefix, "DoorMark", new Color32(226, 224, 214, 255), 0.05f),
            post = SiteBuilderUtil.GetMaterial(Prefix, "PostGrey", new Color32(130, 134, 136, 255), 0.3f),
            leanOrange = Sheet(sheet, "LeanOrange", new Color32(214, 112, 86, 255), 2.5f),
            leanOrangeSide = Sheet(sheet, "LeanOrangeSide", new Color32(214, 112, 86, 255), 4.4f),
            leanDark = SiteBuilderUtil.GetMaterial(Prefix, "LeanDoor", new Color32(176, 82, 62, 255), 0.1f),
            leanRed = SiteBuilderUtil.GetMaterial(Prefix, "LeanRed", new Color32(150, 44, 44, 255), 0.1f),
            leanRoof = SiteBuilderUtil.GetMaterial(Prefix, "LeanRoof", new Color32(120, 126, 132, 255), 0.2f),
            cabinWhite = SiteBuilderUtil.GetMaterial(Prefix, "CabinWhite", new Color32(232, 234, 232, 255), 0.05f),
            dark = SiteBuilderUtil.GetMaterial(Prefix, "DoorDark", new Color32(46, 52, 56, 255), 0.3f),
            pot = SiteBuilderUtil.GetMaterial(Prefix, "Pot", new Color32(180, 90, 60, 255), 0.1f),
            plant = SiteBuilderUtil.GetMaterial(Prefix, "PotPlant", new Color32(60, 120, 50, 255), 0.05f),
            goalFrame = SiteBuilderUtil.GetMaterial(Prefix, "GoalWhite", new Color32(238, 238, 234, 255), 0.2f),
            goalNet = SiteBuilderUtil.GetMaterial(Prefix, "GoalNet", new Color32(200, 204, 204, 255), 0.1f),
            rust = SiteBuilderUtil.GetMaterial(Prefix, "Container", new Color32(150, 80, 56, 255), 0.1f),
        };
        return m;
    }

    // ------------------------------------------------------------------------------------
    // Floor, court, net
    // ------------------------------------------------------------------------------------

    static void BuildFloorAndCourt(Transform model, Mats m)
    {
        Transform floor = SiteBuilderUtil.Group(model, "FloorAndCourt");

        // The concrete floor, a little proud of the lawn.
        SiteBuilderUtil.Part(floor, "Floor", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(2f * HalfLength, 0.04f, 2f * HalfWidth), m.floor, false);

        // The court: everything orange first, then the two big green areas on top, leaving the orange service strip near the
        // net, the alleys and the back margins, like the worn court in the photos.
        float top = 0.045f;
        SiteBuilderUtil.Part(floor, "CourtBase", PrimitiveType.Cube, new Vector3(0f, top, 0f),
            new Vector3(2f * CourtHalfLength, 0.01f, 2f * CourtHalfWidth), m.courtOrange, false);
        foreach (float side in new[] { -1f, 1f })
        {
            float xMin = ShortServiceX;
            float xMax = CourtHalfLength - 0.7f;
            SiteBuilderUtil.Part(floor, "CourtGreen", PrimitiveType.Cube, new Vector3(side * (xMin + xMax) / 2f, top + 0.003f, 0f),
                new Vector3(xMax - xMin, 0.01f, 2f * SinglesHalfWidth), m.courtGreen, false);
        }

        // White lines.
        float y = top + 0.012f;
        // The outer boundary.
        Line(floor, m, 0f, y, CourtHalfWidth, 2f * CourtHalfLength, LineWidth);
        Line(floor, m, 0f, y, -CourtHalfWidth, 2f * CourtHalfLength, LineWidth);
        foreach (float side in new[] { -1f, 1f })
        {
            LineAcross(floor, m, side * CourtHalfLength, y, 2f * CourtHalfWidth);                    // back lines
            LineAcross(floor, m, side * ShortServiceX, y, 2f * CourtHalfWidth);                      // short service lines
            LineAcross(floor, m, side * LongServiceX, y, 2f * CourtHalfWidth);                       // doubles long service lines
            // The centre line, from the short service line to the back line.
            Line(floor, m, side * (ShortServiceX + CourtHalfLength) / 2f, y, 0f, CourtHalfLength - ShortServiceX, LineWidth);
        }
        // The singles side lines, full length.
        Line(floor, m, 0f, y, SinglesHalfWidth, 2f * CourtHalfLength, LineWidth);
        Line(floor, m, 0f, y, -SinglesHalfWidth, 2f * CourtHalfLength, LineWidth);

        // The net: two thin posts, a white tape on top and a few strands of net below. Posts have no collider (so nothing snags).
        Transform net = SiteBuilderUtil.Group(model, "Net");
        foreach (float z in new[] { -3.35f, 3.35f })
        {
            SiteBuilderUtil.Part(net, "NetPost", PrimitiveType.Cylinder, new Vector3(0f, 0.78f, z), new Vector3(0.08f, 0.78f, 0.08f), m.steel, false);
            SiteBuilderUtil.Part(net, "NetFoot", PrimitiveType.Cube, new Vector3(0f, 0.05f, z), new Vector3(0.5f, 0.08f, 0.4f), m.concrete, false);
        }
        SiteBuilderUtil.Part(net, "NetTape", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(0.02f, 0.08f, 6.7f), m.netWhite, false);
        for (int i = 0; i < 5; i++)
        {
            SiteBuilderUtil.Part(net, "NetStrand_" + (i + 1), PrimitiveType.Cube, new Vector3(0f, 1.36f - i * 0.15f, 0f), new Vector3(0.012f, 0.012f, 6.7f), m.netWhite, false);
        }
    }

    // A court line running along x, centred at (x, z).
    static void Line(Transform parent, Mats m, float x, float y, float z, float length, float width)
    {
        SiteBuilderUtil.Part(parent, "Line", PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(length, 0.004f, width), m.line, false);
    }

    // A court line running across the court (along z), at x.
    static void LineAcross(Transform parent, Mats m, float x, float y, float length)
    {
        SiteBuilderUtil.Part(parent, "Line", PrimitiveType.Cube, new Vector3(x, y, 0f), new Vector3(LineWidth, 0.004f, length), m.line, false);
    }

    // ------------------------------------------------------------------------------------
    // Walls
    // ------------------------------------------------------------------------------------

    // The two long walls: a green outer sheet (with the collider), a grey inner sheet, a low concrete wall inside, a pale concrete
    // base outside and a pale mesh vent strip on both faces.
    static Transform BuildSideWalls(Transform model, Mats m)
    {
        Transform walls = SiteBuilderUtil.Group(model, "SideWalls");
        float length = 2f * (HalfLength + WallThickness);
        foreach (float s in new[] { -1f, 1f })
        {
            SiteBuilderUtil.Part(walls, "WallOuter", PrimitiveType.Cube, new Vector3(0f, WallHeight / 2f, s * (HalfWidth + WallThickness / 2f)),
                new Vector3(length, WallHeight, WallThickness), m.wallGreenLong, true);
            SiteBuilderUtil.Part(walls, "WallInner", PrimitiveType.Cube, new Vector3(0f, (BaseHeight + WallHeight) / 2f, s * (HalfWidth - 0.01f)),
                new Vector3(2f * HalfLength, WallHeight - BaseHeight, 0.02f), m.wallGreyLong, false);
            SiteBuilderUtil.Part(walls, "BaseInside", PrimitiveType.Cube, new Vector3(0f, BaseHeight / 2f, s * (HalfWidth - 0.05f)),
                new Vector3(2f * HalfLength, BaseHeight, 0.06f), m.concrete, false);
            SiteBuilderUtil.Part(walls, "BaseOutside", PrimitiveType.Cube, new Vector3(0f, 0.45f, s * (HalfWidth + WallThickness + 0.03f)),
                new Vector3(length + 0.12f, 0.9f, 0.06f), m.concrete, false);
            // The vent strip: a band of pale mesh at 1.3 .. 1.8 m, on the outside and the inside.
            SiteBuilderUtil.Part(walls, "VentOutside", PrimitiveType.Cube, new Vector3(0f, 1.55f, s * (HalfWidth + WallThickness + 0.01f)),
                new Vector3(2f * HalfLength - 1f, 0.5f, 0.02f), m.vent, false);
            SiteBuilderUtil.Part(walls, "VentInside", PrimitiveType.Cube, new Vector3(0f, 1.55f, s * (HalfWidth - 0.03f)),
                new Vector3(2f * HalfLength - 1f, 0.5f, 0.02f), m.vent, false);
        }
        return walls;
    }

    // The two arched end walls: the north one is solid, the south one has the door opening. Each is a green wall up to where the
    // arch springs, plus a flat fill under the arch (green outside, grey inside), with pale windows high up.
    static Transform BuildEndWalls(Transform model, Mats m)
    {
        Transform ends = SiteBuilderUtil.Group(model, "EndWalls");
        float outerHalf = HalfWidth + WallThickness;

        // ---- North end (x = -HalfLength - ...): solid.
        float nx = -(HalfLength + WallThickness / 2f);
        SiteBuilderUtil.Part(ends, "NorthWall", PrimitiveType.Cube, new Vector3(nx, WallHeight / 2f, 0f),
            new Vector3(WallThickness, WallHeight, 2f * outerHalf), m.wallGreenEnd, true);
        SiteBuilderUtil.Part(ends, "NorthInner", PrimitiveType.Cube, new Vector3(-(HalfLength - 0.01f), (BaseHeight + WallHeight) / 2f, 0f),
            new Vector3(0.02f, WallHeight - BaseHeight, 2f * HalfWidth), m.wallGreyEnd, false);
        SiteBuilderUtil.Part(ends, "NorthBaseInside", PrimitiveType.Cube, new Vector3(-(HalfLength - 0.05f), BaseHeight / 2f, 0f),
            new Vector3(0.06f, BaseHeight, 2f * HalfWidth), m.concrete, false);
        SiteBuilderUtil.Part(ends, "NorthBaseOutside", PrimitiveType.Cube, new Vector3(-(HalfLength + WallThickness + 0.03f), 0.45f, 0f),
            new Vector3(0.06f, 0.9f, 2f * outerHalf + 0.12f), m.concrete, false);
        BuildGableFill(ends, -1f, "NorthGable", m);
        foreach (float z in new[] { -1.5f, 1.5f })
        {
            SiteBuilderUtil.Part(ends, "NorthWindow", PrimitiveType.Cube, new Vector3(nx, WallHeight + 1.25f, z), new Vector3(0.3f, 1.5f, 1.2f), m.window, false);
        }

        // ---- South end (x = +HalfLength + ...): a door opening at z DoorZMin .. DoorZMax.
        float sx = HalfLength + WallThickness / 2f;
        float leftWidth = DoorZMin + outerHalf;                  // from -outerHalf to DoorZMin
        float rightWidth = outerHalf - DoorZMax;                 // from DoorZMax to +outerHalf
        SiteBuilderUtil.Part(ends, "SouthWallLeft", PrimitiveType.Cube, new Vector3(sx, WallHeight / 2f, (-outerHalf + DoorZMin) / 2f),
            new Vector3(WallThickness, WallHeight, leftWidth), m.wallGreenEnd, true);
        SiteBuilderUtil.Part(ends, "SouthWallRight", PrimitiveType.Cube, new Vector3(sx, WallHeight / 2f, (DoorZMax + outerHalf) / 2f),
            new Vector3(WallThickness, WallHeight, rightWidth), m.wallGreenEnd, true);
        SiteBuilderUtil.Part(ends, "SouthLintel", PrimitiveType.Cube, new Vector3(sx, (DoorHeight + WallHeight) / 2f, (DoorZMin + DoorZMax) / 2f),
            new Vector3(WallThickness, WallHeight - DoorHeight, DoorZMax - DoorZMin), m.wallGreenEnd, true);
        // Grey inside faces and the concrete base outside, with the door gap left open.
        float innerLeft = DoorZMin + HalfWidth;
        float innerRight = HalfWidth - DoorZMax;
        SiteBuilderUtil.Part(ends, "SouthInnerLeft", PrimitiveType.Cube, new Vector3(HalfLength - 0.01f, (BaseHeight + WallHeight) / 2f, (-HalfWidth + DoorZMin) / 2f),
            new Vector3(0.02f, WallHeight - BaseHeight, innerLeft), m.wallGreyEnd, false);
        SiteBuilderUtil.Part(ends, "SouthInnerRight", PrimitiveType.Cube, new Vector3(HalfLength - 0.01f, (BaseHeight + WallHeight) / 2f, (DoorZMax + HalfWidth) / 2f),
            new Vector3(0.02f, WallHeight - BaseHeight, innerRight), m.wallGreyEnd, false);
        SiteBuilderUtil.Part(ends, "SouthBaseLeft", PrimitiveType.Cube, new Vector3(HalfLength + WallThickness + 0.03f, 0.45f, (-outerHalf + DoorZMin) / 2f),
            new Vector3(0.06f, 0.9f, leftWidth + 0.06f), m.concrete, false);
        SiteBuilderUtil.Part(ends, "SouthBaseRight", PrimitiveType.Cube, new Vector3(HalfLength + WallThickness + 0.03f, 0.45f, (DoorZMax + outerHalf) / 2f),
            new Vector3(0.06f, 0.9f, rightWidth + 0.06f), m.concrete, false);
        BuildGableFill(ends, 1f, "SouthGable", m);
        // Three pale windows high up on the front, like the photo (a wide low one at the left, two tall ones in the middle).
        SiteBuilderUtil.Part(ends, "SouthWindow_1", PrimitiveType.Cube, new Vector3(sx, WallHeight + 1.0f, -1.9f), new Vector3(0.3f, 1.3f, 1.3f), m.window, false);
        SiteBuilderUtil.Part(ends, "SouthWindow_2", PrimitiveType.Cube, new Vector3(sx, WallHeight + 1.9f, 0.4f), new Vector3(0.3f, 2.0f, 1.0f), m.window, false);
        SiteBuilderUtil.Part(ends, "SouthWindow_3", PrimitiveType.Cube, new Vector3(sx, WallHeight + 1.9f, 2.0f), new Vector3(0.3f, 2.0f, 0.9f), m.window, false);

        // The door leaf, swung open against the front wall (it hinges on the left, outside).
        SiteBuilderUtil.Part(ends, "DoorLeaf", PrimitiveType.Cube, new Vector3(HalfLength + WallThickness + 0.9f, DoorHeight / 2f - 0.05f, DoorZMin - 0.06f),
            new Vector3(1.6f, DoorHeight - 0.1f, 0.05f), m.doorGreen, false);
        SiteBuilderUtil.Part(ends, "DoorMark_1", PrimitiveType.Cube, new Vector3(HalfLength + WallThickness + 1.25f, 1.7f, DoorZMin - 0.09f), new Vector3(0.3f, 0.25f, 0.02f), m.doorMark, false);
        SiteBuilderUtil.Part(ends, "DoorMark_2", PrimitiveType.Cube, new Vector3(HalfLength + WallThickness + 1.45f, 1.55f, DoorZMin - 0.09f), new Vector3(0.12f, 0.3f, 0.02f), m.doorMark, false);
        return ends;
    }

    // The flat fill under the arch at one end (g = -1 north, +1 south): a fan of triangles from the middle of the wall's top edge out to
    // the arch. Two sub-meshes: the faces looking out of the hall (green) and the faces looking in (grey). Saved as a mesh asset.
    static void BuildGableFill(Transform parent, float g, string name, Mats m)
    {
        float archHalf = HalfWidth + WallThickness;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var outside = new System.Collections.Generic.List<int>();
        var inside = new System.Collections.Generic.List<int>();
        Vector3 outward = new Vector3(g, 0f, 0f);

        Vector3 centre = Vector3.zero;
        for (int i = 0; i < ArchSegments; i++)
        {
            float t0 = Mathf.PI * i / ArchSegments;
            float t1 = Mathf.PI * (i + 1) / ArchSegments;
            Vector3 a = new Vector3(0f, Rise * Mathf.Sin(t0), -archHalf * Mathf.Cos(t0));
            Vector3 b = new Vector3(0f, Rise * Mathf.Sin(t1), -archHalf * Mathf.Cos(t1));
            // Make the first triangle face outward, and add its reverse for the inside.
            if (Vector3.Dot(Vector3.Cross(a - centre, b - centre), outward) < 0f)
            {
                Vector3 swap = a; a = b; b = swap;
            }
            int s = vertices.Count;
            vertices.AddRange(new[] { centre, a, b, centre, b, a });
            outside.AddRange(new[] { s, s + 1, s + 2 });
            inside.AddRange(new[] { s + 3, s + 4, s + 5 });
        }

        var mesh = new Mesh { name = "EXT14_" + name };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(outside, 0);
        mesh.SetTriangles(inside, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = "Assets/Meshes/Sites/EXT14_" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(g * (HalfLength + WallThickness / 2f), WallHeight, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { m.gableOut, m.gableIn };
    }

    // ------------------------------------------------------------------------------------
    // Roof, ribs, lights
    // ------------------------------------------------------------------------------------

    static Transform BuildRoofAndRibs(Transform model, Mats m)
    {
        // The roof shell: a half-ellipse across z, pulled along x, wider and longer than the walls so it overhangs. Both sides drawn.
        float roofHalfWidth = HalfWidth + WallThickness + 0.3f;
        float roofRise = Rise + 0.2f;
        float roofHalfLength = HalfLength + WallThickness + 0.6f;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var indices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < ArchSegments; i++)
        {
            float t0 = Mathf.PI * i / ArchSegments;
            float t1 = Mathf.PI * (i + 1) / ArchSegments;
            Vector3 a0 = new Vector3(-roofHalfLength, roofRise * Mathf.Sin(t0), -roofHalfWidth * Mathf.Cos(t0));
            Vector3 a1 = new Vector3(-roofHalfLength, roofRise * Mathf.Sin(t1), -roofHalfWidth * Mathf.Cos(t1));
            Vector3 b0 = new Vector3(roofHalfLength, a0.y, a0.z);
            Vector3 b1 = new Vector3(roofHalfLength, a1.y, a1.z);
            AddDoubleTriangle(vertices, indices, a0, a1, b1);
            AddDoubleTriangle(vertices, indices, a0, b1, b0);
        }
        var mesh = new Mesh { name = "EXT14_Hall_Roof" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        const string path = "Assets/Meshes/Sites/EXT14_Hall_Roof.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        Transform roofGroup = SiteBuilderUtil.Group(model, "Roof");
        var go = new GameObject("BarrelRoof");
        go.transform.SetParent(roofGroup, false);
        go.transform.localPosition = new Vector3(0f, WallHeight, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m.roof;

        // Steel arch ribs just under the roof, every 3 m, and long purlins running along the hall.
        Transform ribs = SiteBuilderUtil.Group(model, "Ribs");
        float ribHalf = HalfWidth - 0.02f;
        float ribRise = Rise - 0.1f;
        for (float x = -HalfLength + 1f; x <= HalfLength - 1f + 0.01f; x += 3f)
        {
            for (int i = 0; i < ArchSegments; i++)
            {
                float t0 = Mathf.PI * i / ArchSegments;
                float t1 = Mathf.PI * (i + 1) / ArchSegments;
                Vector3 p0 = new Vector3(x, WallHeight + ribRise * Mathf.Sin(t0), -ribHalf * Mathf.Cos(t0));
                Vector3 p1 = new Vector3(x, WallHeight + ribRise * Mathf.Sin(t1), -ribHalf * Mathf.Cos(t1));
                Vector3 d = p1 - p0;
                GameObject bar = SiteBuilderUtil.Part(ribs, "Rib", PrimitiveType.Cube, (p0 + p1) / 2f, new Vector3(0.12f, 0.14f, d.magnitude + 0.03f), m.steel, false);
                bar.transform.localRotation = Quaternion.LookRotation(d.normalized, Vector3.right);
            }
        }
        foreach (float t in new[] { 0.14f, 0.28f, 0.42f, 0.58f, 0.72f, 0.86f })
        {
            float a = Mathf.PI * t;
            SiteBuilderUtil.Part(ribs, "Purlin", PrimitiveType.Cube,
                new Vector3(0f, WallHeight + ribRise * Mathf.Sin(a), -ribHalf * Mathf.Cos(a)), new Vector3(2f * HalfLength, 0.08f, 0.08f), m.steel, false);
        }
        return roofGroup;
    }

    static void AddDoubleTriangle(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<int> indices, Vector3 a, Vector3 b, Vector3 c)
    {
        int s = vertices.Count;
        vertices.AddRange(new[] { a, b, c, a, c, b });
        indices.AddRange(new[] { s, s + 1, s + 2, s + 3, s + 4, s + 5 });
    }

    // Six bright ceiling lamps hanging near the walls (the glow makes the inside look lit).
    static void BuildLamps(Transform model, Mats m)
    {
        Transform lamps = SiteBuilderUtil.Group(model, "Lamps");
        foreach (float x in new[] { -6f, 0f, 6f })
        {
            foreach (float z in new[] { -HalfWidth + 0.4f, HalfWidth - 0.4f })
            {
                SiteBuilderUtil.Part(lamps, "Lamp", PrimitiveType.Cube, new Vector3(x, WallHeight + 0.5f, z), new Vector3(0.3f, 0.3f, 0.3f), m.lamp, false);
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // Lean-to, cabin, goals, container
    // ------------------------------------------------------------------------------------

    // The orange corrugated lean-to against the east long wall, near the south end: orange walls, two doors and a red band on the
    // front, and a grey roof that slopes away from the hall. (+z in this frame is world east.)
    static void BuildLeanTo(Transform model, Mats m)
    {
        Transform lean = SiteBuilderUtil.Group(model, "LeanTo");
        float zWall = HalfWidth + WallThickness;
        float centreX = HalfLength - 1.9f;
        float centreZ = zWall + 1.25f;
        SiteBuilderUtil.Part(lean, "Walls", PrimitiveType.Cube, new Vector3(centreX, 1.2f, centreZ), new Vector3(4.4f, 2.4f, 2.5f), m.leanOrange, true);
        float frontX = centreX + 2.2f + 0.015f;
        for (int i = 0; i < 2; i++)
        {
            SiteBuilderUtil.Part(lean, "Door_" + (i + 1), PrimitiveType.Cube, new Vector3(frontX, 1.0f, centreZ - 0.6f + i * 1.2f), new Vector3(0.03f, 1.9f, 1.05f), m.leanDark, false);
        }
        SiteBuilderUtil.Part(lean, "RedBand", PrimitiveType.Cube, new Vector3(frontX + 0.005f, 2.25f, centreZ), new Vector3(0.03f, 0.3f, 2.5f), m.leanRed, false);
        GameObject roof = SiteBuilderUtil.Part(lean, "Roof", PrimitiveType.Cube, new Vector3(centreX, 2.55f, centreZ), new Vector3(4.9f, 0.08f, 3.0f), m.leanRoof, false);
        roof.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
    }

    // The small white cabin with a window, a door, a flat roof and two potted plants. In the Extras group (which turns with the root).
    static void BuildCabin(Transform extras, Mats m)
    {
        Transform cabin = SiteBuilderUtil.Group(extras, "Cabin");
        cabin.localPosition = ToLocal(CabinWorldX, CabinWorldZ);
        // The root is turned 90 degrees, so no extra turn is needed: the cabin's long side runs along world z, its front (door) faces world west.
        SiteBuilderUtil.Part(cabin, "Walls", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(5f, 2.8f, 3.2f), m.cabinWhite, true);
        SiteBuilderUtil.Part(cabin, "Roof", PrimitiveType.Cube, new Vector3(0f, 2.9f, 0f), new Vector3(5.4f, 0.2f, 3.6f), m.cabinWhite, false);
        SiteBuilderUtil.Part(cabin, "Window", PrimitiveType.Cube, new Vector3(-0.8f, 1.6f, -1.63f), new Vector3(0.9f, 1.0f, 0.05f), m.dark, false);
        SiteBuilderUtil.Part(cabin, "Door", PrimitiveType.Cube, new Vector3(1.4f, 1.1f, -1.63f), new Vector3(0.9f, 2.1f, 0.05f), m.dark, false);
        foreach (float x in new[] { -0.2f, 2.0f })
        {
            SiteBuilderUtil.Part(cabin, "Pot", PrimitiveType.Cylinder, new Vector3(x, 0.2f, -2.2f), new Vector3(0.4f, 0.2f, 0.4f), m.pot, false);
            SiteBuilderUtil.Part(cabin, "Plant", PrimitiveType.Sphere, new Vector3(x, 0.6f, -2.2f), new Vector3(0.5f, 0.5f, 0.5f), m.plant, false);
        }

        // The rust container (placed by the team). Its length (world z) is the local x size, its width (world x) the local z size.
        SiteBuilderUtil.Part(extras, "Container", PrimitiveType.Cube, ToLocal(ContainerWorldX, ContainerWorldZ) + new Vector3(0f, 1.3f, 0f),
            new Vector3(ContainerLength, 2.6f, 2.3f), m.rust, true);
    }

    // Two white football goals west of the cabin: a frame of two posts and a bar, with a net panel behind (east).
    static void BuildGoals(Transform extras, Mats m)
    {
        Transform goals = SiteBuilderUtil.Group(extras, "FootballGoals");
        for (int g = 0; g < 2; g++)
        {
            Transform goal = SiteBuilderUtil.Group(goals, "Goal_" + (g + 1));
            goal.localPosition = ToLocal(GoalWorldX, GoalWorldZs[g]);
            foreach (float x in new[] { -1.5f, 1.5f })
            {
                SiteBuilderUtil.Part(goal, "Post", PrimitiveType.Cylinder, new Vector3(x, 1.0f, 0f), new Vector3(0.08f, 1.0f, 0.08f), m.goalFrame, false);
            }
            SiteBuilderUtil.Part(goal, "Bar", PrimitiveType.Cube, new Vector3(0f, 2.0f, 0f), new Vector3(3.1f, 0.08f, 0.08f), m.goalFrame, false);
            SiteBuilderUtil.Part(goal, "Net", PrimitiveType.Cube, new Vector3(0f, 1.0f, 1.0f), new Vector3(3f, 2f, 0.02f), m.goalNet, false);
        }
    }
}
