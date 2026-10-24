using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT04 car shed "blockout" model (simple shapes, no textures) in Exterior.unity:
// a lean-to steel canopy attached to the front of a single-storey classroom block, with a paved
// floor, a gravel strip, a brick path and a few parked cars.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT04 CarShed
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT04_CarShed.Build -logFile ext04.log
//
// It is RE-RUNNABLE: every run deletes the old model and builds a fresh one from the constants
// below, so you can change a number, run it again, and never get duplicates. To resize the car
// shed, change the constants, run it, then commit the changed scene and assets.
//
// What it touches: only the car shed object in Exterior (named POI_CarShed the first time, then
// EXT04_CarShed). Its position, its POITriggerVolume and its trigger collider (poiId car_shed) are
// left exactly as they are. The steps shared with the other site builders (opening the scene, making
// materials, groups and shapes, saving) are in SiteBuilderUtil.cs.
//
// Layout. Units are metres, the long axis is x, and everything is measured from the shed object's
// centre (the model is lowered so y = 0 is the ground):
//
//      z = +7.5   outer edge of the canopy (low edge, poles, gravel strip and brick path beyond)
//      z = +1.5   the block's front wall; the canopy is attached here (high edge)
//      z = -7.5   back of the classroom block
//
// The open front of the canopy faces +z (toward the main building). This file lives in an "Editor"
// folder, so it is never part of the game build.
public static class EXT04_CarShed
{
    // ------------------------------------------------------------------------------------
    // CONSTANTS - everything about the car shed's size and look is set here (metres).
    // ------------------------------------------------------------------------------------
    const float Length = 47f;            // length of both the canopy and the block, along x
    const float CanopyDepth = 6f;        // canopy depth, from the block's front wall out to the outer edge (z)
    const float BlockDepth = 9f;         // classroom block depth, from its back wall to its front wall (z)
    const float WallHeight = 3.6f;       // height of the block's walls (the eave)
    const float CanopyHighEdge = 3.3f;   // canopy roof height at the wall
    const float CanopyLowEdge = 2.7f;    // canopy roof height at the outer edge (the roof falls away from the building)
    const float RoofThickness = 0.08f;   // thickness of the canopy roof sheet (dark top layer + light underside layer)
    const float PoleSpacing = 5f;        // poles stand about this far apart (rounded to whole parking bays, see BayWidth)
    const float PoleRadius = 0.05f;      // 0.05 = 0.10 m diameter, thin steel poles
    const int PurlinCount = 4;           // steel purlins running along x under the roof (the first and last sit on the two pole rows)
    const float RafterSpacing = 2.5f;    // sloping steel rafters across the canopy, about this far apart along x
    const float BayWidth = 2.6f;         // width of one parking bay along x
    static readonly int[] CarBays = { 2, 4, 5, 9, 13, 16 }; // which bays have a car in them (0 = the westmost bay); the other bays stay empty
    const float FloorBandWidth = 1.2f;   // width of each paver band across the floor
    const float FloorMargin = 0.5f;      // floor sticks out this far beyond the canopy at the ends and the front
    const float GravelWidth = 2.5f;      // gravel strip along the outer edge of the floor
    const float PathWidth = 2.5f;        // brick path width (x)
    const float PathLength = 14f;        // brick path length (z), starting where the gravel strip ends

    // Extra settings (not in the original brief, kept here so nothing is hidden in the code below).
    const float FloorThickness = 0.02f;      // paver / path slab thickness; visual only (no collider)
    const float GravelThickness = 0.01f;     // gravel is a little lower than the pavers
    const float WallPoleSetback = 1.3f;      // the inner pole row stands this far out from the block's wall
    const float OuterPoleSetback = 0.2f;     // the outer pole row stands this far in from the canopy's outer edge
    const float RafterWidth = 0.08f;         // sloping rafter: width (x) ...
    const float RafterHeight = 0.14f;        // ... and height
    const float PurlinWidth = 0.08f;         // purlin: width (z) ...
    const float PurlinHeight = 0.10f;        // ... and height
    const float BracketLength = 1.2f;        // short beam on top of each inner pole, along x
    const float BracketHeight = 0.12f;       // its height ...
    const float BracketThickness = 0.10f;    // ... and thickness
    const float PlinthHeight = 1.0f;         // maroon band at the foot of the block's front wall
    const float PlinthProud = 0.05f;         // how far the plinth sticks out of the wall
    const float BlockRoofHeight = 0.15f;     // thickness of the block's shallow roof slab
    const float BlockRoofOverhang = 0.3f;    // the block's roof sticks out this far on every side
    const float WindowWidth = 1.4f;          // window frame size ...
    const float WindowHeight = 1.1f;         // ... height ...
    const float WindowSill = 1.0f;           // ... and height of its bottom edge above the ground
    const float WindowSpacing = 4.5f;        // windows are about this far apart (rounded so they fit the length evenly)
    static readonly int[] ShutterGaps = { 3, 6, 8 }; // a roller shutter in these gaps between windows (1 = between window 1 and 2)
    const float ShutterWidth = 2.2f;         // roller shutter size
    const float ShutterHeight = 2.4f;
    const float CarLength = 4.0f;            // car size (z) ...
    const float CarWidth = 1.7f;             // ... and (x)
    const float CarGroundClearance = 0.18f;  // gap under the car body
    const float CarBodyHeight = 0.75f;
    const float CarCabinHeight = 0.55f;
    const float CarCabinLength = 2.2f;       // the darker cabin box on top of the body
    const float CarNoseGap = 0.5f;           // cars park nose-in toward the wall, this far from it
    const float FadeMargin = 1.0f;           // the canopy roof hides this far outside its footprint (see ShedRoofFader)

    // Colours. The numbers in the comments are the colour we WANT TO SEE in the game (read off the reference
    // photos). The scene's lighting changes what is on screen, so the values below are tuned from in-game
    // screenshots, not copied from the photos:
    //   - Surfaces that face up (floor, roof, car tops) come out about (+31, +46, +60) brighter and bluer than
    //     their colour, so those colours are LOWER than the colour we want to see.
    //   - Surfaces that face the front (+z) get almost no sun and come out grey (about 35% of their colour),
    //     so the front wall, plinth and window frames also have an Emission glow (the *Glow colours) that
    //     brings them back to cream, maroon and white.
    static readonly Color32 RoofTopColor = new Color32(28, 32, 34, 255);        // want: dark blue-grey sheet metal (80,92,108)
    static readonly Color32 RoofUnderColor = new Color32(225, 220, 205, 255);   // want: off-white underside (hidden by the roof fader when you stand under it)
    static readonly Color32 SteelColor = new Color32(64, 66, 68, 255);          // want: blue-grey steel (95,112,128)
    static readonly Color32 BlockWallColor = new Color32(200, 185, 155, 255);   // want: cream (230,215,185)
    static readonly Color32 BlockWallGlow = new Color32(140, 120, 90, 255);
    static readonly Color32 PlinthColor = new Color32(100, 40, 35, 255);        // want: maroon (140,60,50)
    static readonly Color32 PlinthGlow = new Color32(85, 16, 0, 255);
    static readonly Color32 BlockRoofColor = new Color32(0, 0, 0, 255);         // want: shallow dark roof (35,40,50)
    static readonly Color32 WindowFrameColor = new Color32(240, 240, 235, 255); // want: white frames
    static readonly Color32 WindowFrameGlow = new Color32(136, 126, 113, 255);
    static readonly Color32 GlassColor = new Color32(70, 85, 95, 255);          // window glass and car windows
    static readonly Color32 ShutterColor = new Color32(70, 72, 76, 255);        // darker roller shutters
    static readonly Color32 PaverBaseColor = new Color32(109, 84, 60, 255);     // want: weathered pavers (140,130,120)
    static readonly Color32 PaverRedColor = new Color32(129, 49, 20, 255);      // want: red-brown accent bands (160,95,80)
    static readonly Color32 PaverDarkColor = new Color32(64, 46, 32, 255);      // want: dark accent bands (95,92,92)
    static readonly Color32 GravelColor = new Color32(94, 76, 58, 255);         // want: gravel (125,122,118)
    static readonly Color32 BrickColor = new Color32(119, 39, 5, 255);          // want: herringbone brick path (150,85,65)
    static readonly Color32 CarWhiteColor = new Color32(215, 200, 185, 255);    // want: white
    static readonly Color32 CarBlackColor = new Color32(0, 0, 0, 255);          // want: black (cannot go darker than the wash)
    static readonly Color32 CarBrownColor = new Color32(64, 16, 0, 255);        // want: brown (95,62,48)
    static readonly Color32 CarBlueColor = new Color32(79, 94, 100, 255);       // want: grey-blue (110,140,160)
    static readonly Color32 CarGreyColor = new Color32(39, 26, 16, 255);        // want: dark grey (70,72,76)

    // The paver band pattern repeats along x: 0 = base colour, 1 = red-brown, 2 = dark.
    static readonly int[] BandPattern = { 0, 0, 1, 0, 0, 2, 0, 1, 0, 2, 0, 0 };

    // The colour of each car, in the same order as CarBays (0 = white, 1 = black, 2 = brown, 3 = grey-blue, 4 = dark grey).
    static readonly int[] CarColourIndex = { 0, 1, 2, 3, 0, 4 };

    // ------------------------------------------------------------------------------------
    // What things are called.
    // ------------------------------------------------------------------------------------
    const string RootName = "EXT04_CarShed";
    const string OldRootName = "POI_CarShed";
    const string ModelName = "EXT04_Model";

    // The paved, gravel and path surfaces are flat slabs lying on the ground.
    const float Matte = 0.0f;
    const float RoofSmoothness = 0.1f;
    const float CarSmoothness = 0.05f;

    // Poles and cars are solid for the avatar, but the follow camera must not treat them as walls: its single
    // ray would hit a thin pole or a car and pull the camera right up against the avatar, which then vanishes.
    // Layer 2 is "Ignore Raycast" (the same layer the interior walls and furniture use): the avatar still
    // collides with them, the camera ray goes past.
    const int CameraPassThroughLayer = 2;

    [MenuItem("Campus/Build EXT04 CarShed")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT04_CarShed", BuildInternal);
    }

    static void BuildInternal()
    {
        // (Copied into variables first so the compiler does not warn about "unreachable code".)
        float length = Length;
        float canopyDepth = CanopyDepth;
        float drop = CanopyHighEdge - CanopyLowEdge;
        int purlins = PurlinCount;
        if (length <= 0f || canopyDepth <= 0f || BlockDepth <= 0f || drop <= 0f || purlins < 2 || BayWidth <= 0f)
        {
            throw new System.Exception("Check the constants: Length, CanopyDepth, BlockDepth and BayWidth must be above 0, CanopyHighEdge must be higher than CanopyLowEdge, and PurlinCount must be at least 2.");
        }

        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        // 1. Find the car shed object: it is called POI_CarShed the first time, EXT04_CarShed after that.
        GameObject root = SiteBuilderUtil.FindAndRenameRoot(scene, RootName, OldRootName);

        // 2. Delete the old placeholder cube and any previous model, so a re-run starts clean.
        SiteBuilderUtil.DeleteChildren(root, "Cube", ModelName);

        // 3. Materials (saved as assets, no textures).
        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        Materials m = new Materials
        {
            roofTop = Mat("RoofTop", RoofTopColor, RoofSmoothness),
            roofUnder = Mat("RoofUnder", RoofUnderColor, RoofSmoothness),
            steel = Mat("Steel", SteelColor, 0.3f),
            blockWall = Mat("BlockWall", BlockWallColor, Matte, BlockWallGlow),
            plinth = Mat("Plinth", PlinthColor, Matte, PlinthGlow),
            blockRoof = Mat("BlockRoof", BlockRoofColor, Matte),
            windowFrame = Mat("WindowFrame", WindowFrameColor, 0.1f, WindowFrameGlow),
            glass = Mat("Glass", GlassColor, 0.3f),
            shutter = Mat("Shutter", ShutterColor, 0.1f),
            paverBase = Mat("PaverBase", PaverBaseColor, Matte),
            paverRed = Mat("PaverRed", PaverRedColor, Matte),
            paverDark = Mat("PaverDark", PaverDarkColor, Matte),
            gravel = Mat("Gravel", GravelColor, Matte),
            brick = Mat("Brick", BrickColor, Matte),
            carWhite = Mat("CarWhite", CarWhiteColor, CarSmoothness),
            carBlack = Mat("CarBlack", CarBlackColor, CarSmoothness),
            carBrown = Mat("CarBrown", CarBrownColor, CarSmoothness),
            carBlue = Mat("CarBlue", CarBlueColor, CarSmoothness),
            carGrey = Mat("CarGrey", CarGreyColor, CarSmoothness),
            tyre = Mat("Tyre", new Color32(30, 30, 32, 255), 0.05f),
            headlight = Mat("Headlight", new Color32(235, 235, 220, 255), 0.4f),
            taillight = Mat("Taillight", new Color32(190, 30, 30, 255), 0.3f),
        };

        // 4. The model. The root sits at y = 1 in the scene, so the model is lowered by the root's height:
        //    that puts the ground at local y = 0.
        Transform model = SiteBuilderUtil.CreateModel(root, ModelName);

        // Where things stand along z (see the layout at the top of this file).
        float totalDepth = CanopyDepth + BlockDepth;
        float backZ = -totalDepth / 2f;          // back wall of the block
        float wallZ = backZ + BlockDepth;        // the block's front wall: the canopy is attached here
        float outerZ = wallZ + CanopyDepth;      // the canopy's outer (low) edge

        Transform roofGroup = BuildCanopy(model, m, wallZ, outerZ);
        BuildBlock(model, m, backZ, wallZ);
        BuildFloor(model, m, wallZ, outerZ);
        BuildCars(model, m, wallZ);

        // 5. The roof fader: hides ONLY the canopy roof while the Player is under the canopy or the roof is
        //    in the camera's way. (The block's roof is not touched, and neither are the beams under the roof.)
        var fader = model.gameObject.AddComponent<ShedRoofFader>();
        var serialized = new SerializedObject(fader);
        serialized.FindProperty("roofGroup").objectReferenceValue = roofGroup;
        serialized.FindProperty("footprintHalfSize").vector2Value = new Vector2(Length / 2f, CanopyDepth / 2f);
        // The canopy is not centred on the model's origin: its centre is half a canopy depth out from the wall.
        serialized.FindProperty("footprintCenter").vector2Value = new Vector2(0f, wallZ + CanopyDepth / 2f);
        serialized.FindProperty("margin").floatValue = FadeMargin;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 6. Save everything. (Count first: in the Editor the scene is reloaded after the save.)
        int objectCount = SiteBuilderUtil.CountObjects(root);
        SiteBuilderUtil.SaveSceneAndAssets(scene);

        Debug.Log("EXT04_CarShed: built OK. '" + RootName + "' now has " + objectCount + " objects (root + children). Scene saved.");
    }

    // All the materials in one bundle, so the build steps below can take a single argument.
    class Materials
    {
        public Material roofTop, roofUnder, steel, blockWall, plinth, blockRoof, windowFrame, glass, shutter;
        public Material paverBase, paverRed, paverDark, gravel, brick;
        public Material carWhite, carBlack, carBrown, carBlue, carGrey, tyre, headlight, taillight;
    }

    // ------------------------------------------------------------------------------------
    // Canopy: sloping roof, steel beams under it, and poles (the poles have colliders).
    // ------------------------------------------------------------------------------------
    static Transform BuildCanopy(Transform model, Materials m, float wallZ, float outerZ)
    {
        Transform canopy = SiteBuilderUtil.Group(model, "Canopy");

        // The roof falls away from the building: high at the wall, low at the outer edge. It is a flat
        // slab tilted about the x axis. 'angle' is the tilt, 'slabLength' the length of the sloping sheet,
        // 'normal' the direction pointing straight out of its top surface.
        float drop = CanopyHighEdge - CanopyLowEdge;
        float angle = Mathf.Atan2(drop, CanopyDepth);
        float slabLength = Mathf.Sqrt(CanopyDepth * CanopyDepth + drop * drop);
        Vector3 tilt = new Vector3(angle * Mathf.Rad2Deg, 0f, 0f);
        Vector3 normal = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 midTop = new Vector3(0f, (CanopyHighEdge + CanopyLowEdge) / 2f, wallZ + CanopyDepth / 2f); // middle of the roof's top surface

        // Roof: two thin layers, dark blue-grey on top and off-white underneath. Visual only (no collider).
        Transform roof = SiteBuilderUtil.Group(canopy, "Roof");
        float layer = RoofThickness / 2f;
        SiteBuilderUtil.Part(roof, "Roof_Top", PrimitiveType.Cube, midTop - normal * (layer / 2f),
            new Vector3(Length, layer, slabLength), tilt, m.roofTop, false);
        SiteBuilderUtil.Part(roof, "Roof_Under", PrimitiveType.Cube, midTop - normal * (layer * 1.5f),
            new Vector3(Length, layer, slabLength), tilt, m.roofUnder, false);

        // Height (straight down from the roof's top surface) of the underside of things hanging below it.
        float cosAngle = Mathf.Cos(angle);
        System.Func<float, float> topY = z => CanopyHighEdge - drop * (z - wallZ) / CanopyDepth;
        float rafterDrop = (RoofThickness + RafterHeight) / cosAngle; // top surface -> underside of the rafters

        // Beams: sloping rafters straight under the roof, then purlins running along x under the rafters.
        Transform beams = SiteBuilderUtil.Group(canopy, "Beams");
        int rafters = Mathf.Max(2, Mathf.FloorToInt(Length / RafterSpacing) + 1);
        for (int i = 0; i < rafters; i++)
        {
            float x = Mathf.Lerp(-Length / 2f, Length / 2f, (float)i / (rafters - 1));
            Vector3 center = new Vector3(x, midTop.y, midTop.z) - normal * (RoofThickness + RafterHeight / 2f);
            SiteBuilderUtil.Part(beams, "Rafter_" + (i + 1).ToString("00"), PrimitiveType.Cube, center,
                new Vector3(RafterWidth, RafterHeight, slabLength), tilt, m.steel, false);
        }

        float wallPoleZ = wallZ + WallPoleSetback;
        float outerPoleZ = outerZ - OuterPoleSetback;
        for (int i = 0; i < PurlinCount; i++)
        {
            float z = Mathf.Lerp(wallPoleZ, outerPoleZ, (float)i / (PurlinCount - 1));
            float y = topY(z) - rafterDrop - PurlinHeight / 2f;
            SiteBuilderUtil.Part(beams, "Purlin_" + (i + 1).ToString("00"), PrimitiveType.Cube,
                new Vector3(0f, y, z), new Vector3(Length, PurlinHeight, PurlinWidth), m.steel, false);
        }

        // Poles: two rows (one near the wall, one at the outer edge), standing on the lines between parking
        // bays so they never touch a parked car. The inner row has a short bracket beam on top. Every pole has
        // a collider, so the avatar cannot walk through it.
        Transform poles = SiteBuilderUtil.Group(canopy, "Poles");
        int bays = BayCount();
        int step = Mathf.Max(1, Mathf.RoundToInt(PoleSpacing / BayWidth));
        float firstLine = -bays * BayWidth / 2f;
        int poleNumber = 0;
        for (int line = 0; line <= bays; line += step)
        {
            poleNumber++;
            float x = firstLine + line * BayWidth;
            string number = poleNumber.ToString("00");

            // Outer row: the pole reaches up to the underside of the purlin.
            float outerTop = topY(outerPoleZ) - rafterDrop - PurlinHeight;
            float outerHeight = outerTop - FloorThickness;
            SiteBuilderUtil.Part(poles, "Pole_Outer_" + number, PrimitiveType.Cylinder,
                new Vector3(x, FloorThickness + outerHeight / 2f, outerPoleZ),
                new Vector3(PoleRadius * 2f, outerHeight / 2f, PoleRadius * 2f), m.steel, true).layer = CameraPassThroughLayer;

            // Inner row: the pole stops a bracket's height below the purlin; the bracket fills the gap.
            float wallPurlinBottom = topY(wallPoleZ) - rafterDrop - PurlinHeight;
            float wallTop = wallPurlinBottom - BracketHeight;
            float wallHeight = wallTop - FloorThickness;
            SiteBuilderUtil.Part(poles, "Pole_Wall_" + number, PrimitiveType.Cylinder,
                new Vector3(x, FloorThickness + wallHeight / 2f, wallPoleZ),
                new Vector3(PoleRadius * 2f, wallHeight / 2f, PoleRadius * 2f), m.steel, true).layer = CameraPassThroughLayer;
            SiteBuilderUtil.Part(poles, "Bracket_Wall_" + number, PrimitiveType.Cube,
                new Vector3(x, wallTop + BracketHeight / 2f, wallPoleZ),
                new Vector3(BracketLength, BracketHeight, BracketThickness), m.steel, false);
        }

        return roof;
    }

    // ------------------------------------------------------------------------------------
    // Block: the single-storey classroom block behind the canopy. One box collider for the whole block,
    // so the avatar cannot walk into it. Windows, shutters and the plinth are on the front (+z) face only.
    // ------------------------------------------------------------------------------------
    static void BuildBlock(Transform model, Materials m, float backZ, float wallZ)
    {
        Transform block = SiteBuilderUtil.Group(model, "Block");
        float midZ = (backZ + wallZ) / 2f;

        Transform walls = SiteBuilderUtil.Group(block, "Walls");
        SiteBuilderUtil.Part(walls, "Wall_Main", PrimitiveType.Cube, new Vector3(0f, WallHeight / 2f, midZ),
            new Vector3(Length, WallHeight, BlockDepth), m.blockWall, false);

        Transform plinth = SiteBuilderUtil.Group(block, "Plinth");
        SiteBuilderUtil.Part(plinth, "Plinth_Front", PrimitiveType.Cube,
            new Vector3(0f, PlinthHeight / 2f, wallZ + PlinthProud / 2f),
            new Vector3(Length, PlinthHeight, PlinthProud), m.plinth, false);

        // Windows: white frame with a darker pane, evenly spaced along the front wall.
        Transform windows = SiteBuilderUtil.Group(block, "Windows");
        int windowCount = Mathf.Max(1, Mathf.RoundToInt(Length / WindowSpacing));
        float spacing = Length / windowCount;
        for (int i = 0; i < windowCount; i++)
        {
            float x = -Length / 2f + spacing * (i + 0.5f);
            string number = (i + 1).ToString("00");
            SiteBuilderUtil.Part(windows, "Window_" + number + "_Frame", PrimitiveType.Cube,
                new Vector3(x, WindowSill + WindowHeight / 2f, wallZ + 0.03f),
                new Vector3(WindowWidth, WindowHeight, 0.06f), m.windowFrame, false);
            SiteBuilderUtil.Part(windows, "Window_" + number + "_Glass", PrimitiveType.Cube,
                new Vector3(x, WindowSill + WindowHeight / 2f, wallZ + 0.04f),
                new Vector3(WindowWidth - 0.2f, WindowHeight - 0.2f, 0.07f), m.glass, false);
        }

        // Roller shutters: darker rectangles in the gaps between two windows.
        Transform shutters = SiteBuilderUtil.Group(block, "Shutters");
        for (int i = 0; i < ShutterGaps.Length; i++)
        {
            float x = -Length / 2f + spacing * ShutterGaps[i];
            SiteBuilderUtil.Part(shutters, "Shutter_" + (i + 1).ToString("00"), PrimitiveType.Cube,
                new Vector3(x, ShutterHeight / 2f, wallZ + 0.03f),
                new Vector3(ShutterWidth, ShutterHeight, 0.06f), m.shutter, false);
        }

        // The block's own shallow dark roof. It is NOT part of the roof fader.
        Transform blockRoof = SiteBuilderUtil.Group(block, "Roof");
        SiteBuilderUtil.Part(blockRoof, "BlockRoof_Slab", PrimitiveType.Cube,
            new Vector3(0f, WallHeight + BlockRoofHeight / 2f, midZ),
            new Vector3(Length + 2f * BlockRoofOverhang, BlockRoofHeight, BlockDepth + 2f * BlockRoofOverhang), m.blockRoof, false);

        // The single collider for the whole block (walls and roof).
        var collider = block.gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(Length, WallHeight + BlockRoofHeight, BlockDepth);
        collider.center = new Vector3(0f, (WallHeight + BlockRoofHeight) / 2f, midZ);
    }

    // ------------------------------------------------------------------------------------
    // Floor: paver bands under the canopy, a gravel strip along the outer edge, and a brick path leading
    // out from the gravel strip toward +z. All visual only (no colliders): the avatar walks on the ground.
    // ------------------------------------------------------------------------------------
    static void BuildFloor(Transform model, Materials m, float wallZ, float outerZ)
    {
        Transform floor = SiteBuilderUtil.Group(model, "Floor");

        // Pavers: bands across the canopy (each band is BandWidth wide along x), coloured by the repeating pattern.
        Transform bands = SiteBuilderUtil.Group(floor, "Bands");
        float floorLength = Length + 2f * FloorMargin;
        float floorFrontZ = outerZ + FloorMargin;
        float floorDepth = floorFrontZ - wallZ;
        float floorCenterZ = (wallZ + floorFrontZ) / 2f;
        int bandCount = Mathf.CeilToInt(floorLength / FloorBandWidth);
        Material[] paverMaterials = { m.paverBase, m.paverRed, m.paverDark };
        for (int i = 0; i < bandCount; i++)
        {
            float xStart = -floorLength / 2f + i * FloorBandWidth;
            float xEnd = Mathf.Min(xStart + FloorBandWidth, floorLength / 2f);
            Material material = paverMaterials[BandPattern[i % BandPattern.Length]];
            SiteBuilderUtil.Part(bands, "Band_" + (i + 1).ToString("00"), PrimitiveType.Cube,
                new Vector3((xStart + xEnd) / 2f, FloorThickness / 2f, floorCenterZ),
                new Vector3(xEnd - xStart, FloorThickness, floorDepth), material, false);
        }

        // Gravel strip along the outer edge.
        Transform gravel = SiteBuilderUtil.Group(floor, "Gravel");
        float gravelEndZ = floorFrontZ + GravelWidth;
        SiteBuilderUtil.Part(gravel, "Gravel_Strip", PrimitiveType.Cube,
            new Vector3(0f, GravelThickness / 2f, (floorFrontZ + gravelEndZ) / 2f),
            new Vector3(floorLength, GravelThickness, GravelWidth), m.gravel, false);

        // Brick path, centred on the shed, leading away from the gravel strip toward +z (the main building).
        Transform path = SiteBuilderUtil.Group(floor, "Path");
        SiteBuilderUtil.Part(path, "Path_Brick", PrimitiveType.Cube,
            new Vector3(0f, FloorThickness / 2f, gravelEndZ + PathLength / 2f),
            new Vector3(PathWidth, FloorThickness, PathLength), m.brick, false);
    }

    // ------------------------------------------------------------------------------------
    // Cars: simple low-poly cars (body, glass cabin, roof, wheels, lights, bumpers) parked nose-in toward the wall, one per occupied bay.
    // Each car has one box collider on its root.
    // ------------------------------------------------------------------------------------
    static void BuildCars(Transform model, Materials m, float wallZ)
    {
        Transform cars = SiteBuilderUtil.Group(model, "Cars");
        int bays = BayCount();
        float firstLine = -bays * BayWidth / 2f;
        Material[] colours = { m.carWhite, m.carBlack, m.carBrown, m.carBlue, m.carGrey };
        float centerZ = wallZ + CarNoseGap + CarLength / 2f;
        float bodyBottom = FloorThickness + CarGroundClearance;
        float totalHeight = CarGroundClearance + CarBodyHeight + CarCabinHeight;

        for (int i = 0; i < CarBays.Length; i++)
        {
            int bay = CarBays[i];
            if (bay < 0 || bay >= bays)
            {
                throw new System.Exception("CarBays contains bay " + bay + " but there are only " + bays + " bays (0 to " + (bays - 1) + ").");
            }

            float x = firstLine + (bay + 0.5f) * BayWidth;
            Transform car = SiteBuilderUtil.Group(cars, "Car_" + (i + 1).ToString("00"));
            car.localPosition = new Vector3(x, 0f, centerZ);
            car.gameObject.layer = CameraPassThroughLayer;

            Material paint = colours[CarColourIndex[i]];
            const float lowerBody = 0.6f;     // height of the lower body (above the ground clearance)
            const float roofThickness = 0.07f;
            float cabinHeight = CarBodyHeight + CarCabinHeight - lowerBody - roofThickness;
            float wheelRadius = 0.32f;
            float halfLength = CarLength / 2f;

            // Lower body, with a slightly narrower bonnet end and boot end (a car is not one box).
            SiteBuilderUtil.Part(car, "Body", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + lowerBody / 2f, 0f), new Vector3(CarWidth, lowerBody, CarLength), paint, false);
            // Glass cabin toward the rear (+z) and a coloured roof on top, so it reads as a car with windows.
            float cabinZ = 0.25f;
            SiteBuilderUtil.Part(car, "Cabin", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + lowerBody + cabinHeight / 2f, cabinZ), new Vector3(CarWidth - 0.2f, cabinHeight, CarCabinLength - 0.3f), m.glass, false);
            SiteBuilderUtil.Part(car, "Roof", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + lowerBody + cabinHeight + roofThickness / 2f, cabinZ), new Vector3(CarWidth - 0.1f, roofThickness, CarCabinLength - 0.1f), paint, false);
            // Slanted windscreen and rear window: glass panels leaning from the cabin down to the bonnet and boot.
            SiteBuilderUtil.Part(car, "Windscreen", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + lowerBody + cabinHeight / 2f, cabinZ - CarCabinLength / 2f + 0.05f),
                new Vector3(CarWidth - 0.2f, 0.05f, cabinHeight * 1.25f), new Vector3(-48f, 0f, 0f), m.glass, false);
            SiteBuilderUtil.Part(car, "RearWindow", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + lowerBody + cabinHeight / 2f, cabinZ + CarCabinLength / 2f - 0.05f),
                new Vector3(CarWidth - 0.2f, 0.05f, cabinHeight * 1.2f), new Vector3(52f, 0f, 0f), m.glass, false);

            // Bumpers, headlights (nose, -z) and taillights (+z).
            SiteBuilderUtil.Part(car, "BumperFront", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + 0.12f, -halfLength - 0.03f), new Vector3(CarWidth + 0.06f, 0.2f, 0.12f), m.carBlack, false);
            SiteBuilderUtil.Part(car, "BumperRear", PrimitiveType.Cube,
                new Vector3(0f, bodyBottom + 0.12f, halfLength + 0.03f), new Vector3(CarWidth + 0.06f, 0.2f, 0.12f), m.carBlack, false);
            foreach (float side in new[] { -1f, 1f })
            {
                SiteBuilderUtil.Part(car, "Headlight", PrimitiveType.Cube,
                    new Vector3(side * (CarWidth / 2f - 0.25f), bodyBottom + lowerBody - 0.12f, -halfLength - 0.01f), new Vector3(0.32f, 0.14f, 0.05f), m.headlight, false);
                SiteBuilderUtil.Part(car, "Taillight", PrimitiveType.Cube,
                    new Vector3(side * (CarWidth / 2f - 0.25f), bodyBottom + lowerBody - 0.12f, halfLength + 0.01f), new Vector3(0.32f, 0.14f, 0.05f), m.taillight, false);
            }

            // Four wheels: dark cylinders turned on their side, poking out a little past the body.
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float wz in new[] { -halfLength + 0.8f, halfLength - 0.8f })
                {
                    SiteBuilderUtil.Part(car, "Wheel", PrimitiveType.Cylinder,
                        new Vector3(side * (CarWidth / 2f - 0.04f), FloorThickness + wheelRadius, wz),
                        new Vector3(wheelRadius * 2f, 0.14f, wheelRadius * 2f), new Vector3(0f, 0f, 90f), m.tyre, false);
                }
            }

            var collider = car.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(CarWidth, totalHeight, CarLength);
            collider.center = new Vector3(0f, FloorThickness + totalHeight / 2f, 0f);
        }
    }

    // ------------------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------------------

    // How many parking bays fit along the length.
    static int BayCount()
    {
        return Mathf.Max(1, Mathf.FloorToInt(Length / BayWidth));
    }

    // Loads Assets/Materials/Sites/EXT04_<name>.mat, or creates it (see SiteBuilderUtil.GetMaterial).
    static Material Mat(string name, Color32 color, float smoothness)
    {
        return SiteBuilderUtil.GetMaterial("EXT04", name, color, smoothness);
    }

    // The same, with an Emission glow (see the colour notes at the top).
    static Material Mat(string name, Color32 color, float smoothness, Color32 glow)
    {
        return SiteBuilderUtil.GetMaterial("EXT04", name, color, smoothness, glow);
    }
}
