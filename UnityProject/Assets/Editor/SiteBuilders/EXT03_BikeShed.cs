using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT03 bike shed "blockout" model (simple shapes, no textures) in Exterior.unity.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT03 BikeShed
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT03_BikeShed.Build -logFile ext03.log
//
// It is RE-RUNNABLE: every run deletes the old model and builds a fresh one from the constants
// below, so you can change a number, run it again, and never get duplicates. To resize the shed,
// change the constants, run it, then commit the changed scene and assets.
//
// What it touches: only the shed object in Exterior (named POI_BikeShed the first time, then
// EXT03_BikeShed). Its position, its POITriggerVolume and its trigger collider (poiId bike_shed)
// are left exactly as they are. This file lives in an "Editor" folder, so it is never part of the
// game build.
//
// The steps every site builder shares (opening the scene, finding the site object, making materials,
// groups and shapes, saving) are in SiteBuilderUtil.cs; this file only says what the bike shed looks like.
public static class EXT03_BikeShed
{
    // ------------------------------------------------------------------------------------
    // CONSTANTS - everything about the shed's size and look is set here (metres, long axis = x).
    // ------------------------------------------------------------------------------------
    const float Length = 21f;          // shed length along x (the long side)
    const float Depth = 8f;            // shed width along z (the short side, roof spans this)
    const float EaveHeight = 3.0f;     // height of the roof's two long edges above the ground
    const float CrownRise = 1.2f;      // how much higher the middle of the roof is than the edges (crown = 3.0 + 1.2 = 4.2 m)
    const float RoofThickness = 0.08f; // thickness of the roof sheet
    const int RoofSegments = 10;       // the curved roof is built from this many flat slices across its width
    const int PolesPerSide = 6;        // round steel poles along EACH long edge (2 = corner poles only)
    const float PoleRadius = 0.09f;    // 0.09 = 0.18 m diameter
    const float FasciaHeight = 0.6f;   // dark band hanging down from each long roof edge
    const float FloorMargin = 0.5f;    // floor sticks out this far beyond the roof on every side
    const float FloorBandWidth = 1.2f; // width of each red / black paver band across the floor
    const bool BackWall = false;       // true = add the low mural wall along the back long edge (-z side)

    // Extra settings (not in the original brief, kept here so nothing is hidden in the code below).
    const float FloorThickness = 0.02f;  // floor slab thickness; it is visual only (no collider)
    const float BeamHeight = 0.30f;      // steel side beam: height ...
    const float BeamThickness = 0.20f;   // ... and thickness
    const float EdgeInset = 0.30f;       // poles and beams sit this far inside the roof's long edge
    const float LineWidth = 0.10f;       // yellow bay line width
    const float BayDepth = 2.0f;         // yellow bay lines run this far in from each long edge
    const float WallHeight = 1.6f;       // mural wall height (only used if BackWall = true)
    const float WallThickness = 0.12f;   // mural wall thickness (only used if BackWall = true)
    const float FadeMargin = 1.0f;       // roof hides this far outside the footprint (see ShedRoofFader)

    // Colours read off the reference photos (0-255 RGB).
    // Colour tune: the scene's bright lighting adds roughly (+38, +50, +60) to every colour on screen, so
    // these shed colours are DARKER (and more saturated) than the colours you want to see in-game. They
    // were chosen so the in-game result matches the reference photos (floor red ~190,88,62; charcoal
    // pavers ~88,80,70; roof top ~118,132,140). Old values: roof top 150,165,180; floor red 170,62,45;
    // floor black 55,56,60; smoothness roof 0.35, floors 0.15.
    static readonly Color32 RoofTopColor = new Color32(86, 92, 84, 255);
    static readonly Color32 RoofUnderColor = new Color32(225, 220, 205, 255);
    static readonly Color32 FasciaColor = new Color32(70, 72, 78, 255);
    static readonly Color32 SteelColor = new Color32(150, 155, 160, 255);
    static readonly Color32 FloorRedColor = new Color32(152, 34, 4, 255);
    static readonly Color32 FloorBlackColor = new Color32(46, 27, 12, 255);
    static readonly Color32 LineColor = new Color32(230, 200, 90, 255);
    static readonly Color32 WallColor = new Color32(205, 205, 210, 255); // plain stand-in for the painted mural

    // How shiny the main surfaces are (0 = matte). Matte surfaces do not mirror the bright sky.
    const float RoofTopSmoothness = 0.10f;
    const float FloorSmoothness = 0.0f;

    // ------------------------------------------------------------------------------------
    // Where things are saved / what things are called.
    // ------------------------------------------------------------------------------------
    const string MeshFolder = "Assets/Models/Generated";
    const string RoofMeshPath = MeshFolder + "/EXT03_RoofVault.asset";
    const string RootName = "EXT03_BikeShed";
    const string OldRootName = "POI_BikeShed";
    const string ModelName = "EXT03_Model";

    [MenuItem("Campus/Build EXT03 BikeShed")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT03_BikeShed", BuildInternal);
    }

    static void BuildInternal()
    {
        // (Copied into variables first so the compiler does not warn about "unreachable code".)
        int poles = PolesPerSide;
        int segments = RoofSegments;
        float rise = CrownRise;
        float depth = Depth;
        float length = Length;
        bool wallOn = BackWall;
        if (poles < 2 || segments < 2 || rise <= 0f || depth <= 0f || length <= 0f)
        {
            throw new System.Exception("Check the constants: PolesPerSide and RoofSegments must be at least 2, and Length, Depth and CrownRise must be above 0.");
        }

        Scene scene;
        if (!SiteBuilderUtil.TryOpenExteriorScene(out scene))
        {
            return;
        }

        // 1. Find the shed object: it is called POI_BikeShed the first time, EXT03_BikeShed after that.
        GameObject root = SiteBuilderUtil.FindAndRenameRoot(scene, RootName, OldRootName);

        // 2. Delete the old placeholder cube and any previous model, so a re-run starts clean.
        SiteBuilderUtil.DeleteChildren(root, "Cube", ModelName);

        // 3. Materials and the generated roof mesh (saved as assets).
        SiteBuilderUtil.EnsureFolder("Assets/Materials", "Sites");
        SiteBuilderUtil.EnsureFolder("Assets/Models", "Generated");

        Material roofTop = GetMaterial("RoofTop", RoofTopColor, RoofTopSmoothness);
        Material roofUnder = GetMaterial("RoofUnder", RoofUnderColor, 0.1f);
        Material fascia = GetMaterial("Fascia", FasciaColor, 0.2f);
        Material steel = GetMaterial("Steel", SteelColor, 0.3f);
        Material floorRed = GetMaterial("FloorRed", FloorRedColor, FloorSmoothness);
        Material floorBlack = GetMaterial("FloorBlack", FloorBlackColor, FloorSmoothness);
        Material lines = GetMaterial("Lines", LineColor, 0.1f);
        Material wall = wallOn ? GetMaterial("MuralWall", WallColor, 0.1f) : null;

        Mesh roofMesh = BuildRoofMesh();

        // 4. The model. The root sits at y = 1 in the scene, so the model is lowered by the root's
        //    height: that puts the floor's underside at world y = 0 (the ground).
        Transform model = SiteBuilderUtil.CreateModel(root, ModelName);

        float floorTop = FloorThickness;

        // Roof: one generated mesh with two materials (light blue-grey on top, cream underneath).
        Transform roofGroup = SiteBuilderUtil.Group(model, "Roof");
        var roofObject = new GameObject("Roof_Vault");
        roofObject.transform.SetParent(roofGroup, false);
        roofObject.AddComponent<MeshFilter>().sharedMesh = roofMesh;
        roofObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { roofTop, roofUnder };

        // Poles: the only parts with colliders, so the avatar cannot walk through them.
        Transform poleGroup = SiteBuilderUtil.Group(model, "Poles");
        float edgeZ = Depth / 2f - EdgeInset;
        float beamCenterY = EaveHeight - 0.45f;
        float poleHeight = beamCenterY - floorTop;
        float poleInset = 0.25f;
        for (int side = 0; side < 2; side++)
        {
            float z = side == 0 ? edgeZ : -edgeZ;
            string sideName = side == 0 ? "Front" : "Back";
            for (int i = 0; i < PolesPerSide; i++)
            {
                float t = PolesPerSide == 1 ? 0.5f : (float)i / (PolesPerSide - 1);
                float x = Mathf.Lerp(-Length / 2f + poleInset, Length / 2f - poleInset, t);
                // Unity's Cylinder is 2 m tall and 1 m wide, so scale y by height / 2 and x/z by the diameter.
                SiteBuilderUtil.Part(poleGroup, "Pole_" + sideName + "_" + (i + 1).ToString("00"), PrimitiveType.Cylinder,
                    new Vector3(x, floorTop + poleHeight / 2f, z),
                    new Vector3(PoleRadius * 2f, poleHeight / 2f, PoleRadius * 2f), steel, true);
            }
        }

        // Beams: light grey steel running along each long edge, just under the roof.
        Transform beamGroup = SiteBuilderUtil.Group(model, "Beams");
        SiteBuilderUtil.Part(beamGroup, "Beam_Front", PrimitiveType.Cube, new Vector3(0f, beamCenterY, edgeZ), new Vector3(Length, BeamHeight, BeamThickness), steel, false);
        SiteBuilderUtil.Part(beamGroup, "Beam_Back", PrimitiveType.Cube, new Vector3(0f, beamCenterY, -edgeZ), new Vector3(Length, BeamHeight, BeamThickness), steel, false);

        // Fascia: the dark band along both long roof edges, hanging down from the eave.
        Transform fasciaGroup = SiteBuilderUtil.Group(model, "Fascia");
        float fasciaY = EaveHeight - FasciaHeight / 2f + 0.05f;
        SiteBuilderUtil.Part(fasciaGroup, "Fascia_Front", PrimitiveType.Cube, new Vector3(0f, fasciaY, Depth / 2f), new Vector3(Length, FasciaHeight, 0.08f), fascia, false);
        SiteBuilderUtil.Part(fasciaGroup, "Fascia_Back", PrimitiveType.Cube, new Vector3(0f, fasciaY, -Depth / 2f), new Vector3(Length, FasciaHeight, 0.08f), fascia, false);

        // Floor: separate paver bands across the width (alternating black / red) so no two
        // surfaces overlap. Visual only: the avatar walks on the ground plane underneath.
        Transform floorGroup = SiteBuilderUtil.Group(model, "Floor");
        float floorLength = Length + 2f * FloorMargin;
        float floorWidth = Depth + 2f * FloorMargin;
        int bandCount = Mathf.CeilToInt(floorWidth / FloorBandWidth);
        for (int i = 0; i < bandCount; i++)
        {
            float zStart = -floorWidth / 2f + i * FloorBandWidth;
            float zEnd = Mathf.Min(zStart + FloorBandWidth, floorWidth / 2f);
            SiteBuilderUtil.Part(floorGroup, "Floor_Band_" + (i + 1).ToString("00"), PrimitiveType.Cube,
                new Vector3(0f, FloorThickness / 2f, (zStart + zEnd) / 2f),
                new Vector3(floorLength, FloorThickness, zEnd - zStart),
                i % 2 == 0 ? floorBlack : floorRed, false);
        }

        // Yellow lines: two bay lines along the length (one parking row along each long edge,
        // an aisle in the middle) and one arrow near each end of the aisle, pointing along +x.
        Transform lineGroup = SiteBuilderUtil.Group(model, "Lines");
        float lineY = floorTop + 0.005f;
        float lineZ = Depth / 2f - BayDepth;
        SiteBuilderUtil.Part(lineGroup, "Line_Bay_Front", PrimitiveType.Cube, new Vector3(0f, lineY, lineZ), new Vector3(Length - 1f, 0.01f, LineWidth), lines, false);
        SiteBuilderUtil.Part(lineGroup, "Line_Bay_Back", PrimitiveType.Cube, new Vector3(0f, lineY, -lineZ), new Vector3(Length - 1f, 0.01f, LineWidth), lines, false);
        BuildArrow(lineGroup, "Arrow_West", -Length / 2f + 2.5f, lineY, lines);
        BuildArrow(lineGroup, "Arrow_East", Length / 2f - 2.5f, lineY, lines);

        // Optional low mural wall along the back long edge (-z). Like everything except the poles,
        // it has no collider.
        if (wallOn)
        {
            Transform wallGroup = SiteBuilderUtil.Group(model, "Wall");
            SiteBuilderUtil.Part(wallGroup, "Wall_Back", PrimitiveType.Cube,
                new Vector3(0f, WallHeight / 2f, -(Depth / 2f - 0.1f)),
                new Vector3(Length, WallHeight, WallThickness), wall, false);
        }

        // 5. The roof fader: hides the Roof (plus Fascia and Beams) while the Player is under the shed or the roof is in the camera's way.
        var fader = model.gameObject.AddComponent<ShedRoofFader>();
        var serialized = new SerializedObject(fader);
        serialized.FindProperty("roofGroup").objectReferenceValue = roofGroup;
        // The fascia band and side beams hang from the roof edge and block the camera's view just as the
        // roof does, so they are hidden and shown together with it.
        SerializedProperty alsoHide = serialized.FindProperty("alsoHide");
        alsoHide.arraySize = 2;
        alsoHide.GetArrayElementAtIndex(0).objectReferenceValue = fasciaGroup;
        alsoHide.GetArrayElementAtIndex(1).objectReferenceValue = beamGroup;
        serialized.FindProperty("footprintHalfSize").vector2Value = new Vector2(Length / 2f, Depth / 2f);
        serialized.FindProperty("margin").floatValue = FadeMargin;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 6. Save everything. (Count first: in the Editor the scene is reloaded after the save.)
        int objectCount = SiteBuilderUtil.CountObjects(root);
        SiteBuilderUtil.SaveSceneAndAssets(scene);

        Debug.Log("EXT03_BikeShed: built OK. '" + RootName + "' now has " + objectCount + " objects (root + children). Scene saved.");
    }

    // ------------------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------------------

    // A painted arrow on the aisle floor pointing along +x: a shaft plus two angled head bars.
    static void BuildArrow(Transform parent, string name, float xCenter, float y, Material material)
    {
        const float shaftLength = 1.4f;
        const float headLength = 0.6f;
        const float headAngle = 35f;
        float tipX = xCenter + shaftLength / 2f;

        SiteBuilderUtil.Part(parent, name + "_Shaft", PrimitiveType.Cube, new Vector3(xCenter, y, 0f), new Vector3(shaftLength, 0.01f, LineWidth), material, false);

        float a = headAngle * Mathf.Deg2Rad;
        float dx = -Mathf.Cos(a) * headLength / 2f;
        float dz = Mathf.Sin(a) * headLength / 2f;
        GameObject left = SiteBuilderUtil.Part(parent, name + "_HeadLeft", PrimitiveType.Cube, new Vector3(tipX + dx, y, dz), new Vector3(headLength, 0.01f, LineWidth), material, false);
        left.transform.localRotation = Quaternion.Euler(0f, headAngle, 0f);
        GameObject right = SiteBuilderUtil.Part(parent, name + "_HeadRight", PrimitiveType.Cube, new Vector3(tipX + dx, y, -dz), new Vector3(headLength, 0.01f, LineWidth), material, false);
        right.transform.localRotation = Quaternion.Euler(0f, -headAngle, 0f);
    }

    // Loads Assets/Materials/Sites/EXT03_<name>.mat, or creates it (see SiteBuilderUtil.GetMaterial).
    static Material GetMaterial(string name, Color32 color, float smoothness)
    {
        return SiteBuilderUtil.GetMaterial("EXT03", name, color, smoothness);
    }

    // ------------------------------------------------------------------------------------
    // The roof: a curved (faceted) sheet built as one mesh with two sub-meshes:
    //   sub-mesh 0 = top surface plus the thin edges   (light blue-grey material)
    //   sub-mesh 1 = underside                          (cream material)
    // The curve is a circle arc through the two eaves (height EaveHeight, at z = +-Depth/2) and
    // the crown (height EaveHeight + CrownRise, at z = 0), cut into RoofSegments flat slices.
    // ------------------------------------------------------------------------------------
    static Mesh BuildRoofMesh()
    {
        float halfDepth = Depth / 2f;
        float radius = (CrownRise * CrownRise + halfDepth * halfDepth) / (2f * CrownRise);
        float centerY = EaveHeight + CrownRise - radius;
        float maxAngle = Mathf.Asin(halfDepth / radius);
        float x0 = -Length / 2f;
        float x1 = Length / 2f;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var topTriangles = new List<int>();
        var underTriangles = new List<int>();

        // Point on the arc: angle 0 = crown, +-maxAngle = the eaves. 'r' picks outer or inner surface.
        System.Func<float, float, float, Vector3> point = (angle, r, x) =>
            new Vector3(x, centerY + r * Mathf.Cos(angle), r * Mathf.Sin(angle));
        // Direction pointing straight out of the arc's centre (the surface's normal) at an angle.
        System.Func<float, Vector3> outward = angle => new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));

        float innerRadius = radius - RoofThickness;

        for (int i = 0; i < RoofSegments; i++)
        {
            float a0 = Mathf.Lerp(-maxAngle, maxAngle, (float)i / RoofSegments);
            float a1 = Mathf.Lerp(-maxAngle, maxAngle, (float)(i + 1) / RoofSegments);
            Vector3 out0 = outward((a0 + a1) / 2f);

            // Top surface, then underside (facing the opposite way).
            AddQuad(vertices, normals, topTriangles, point(a0, radius, x0), point(a0, radius, x1), point(a1, radius, x1), point(a1, radius, x0), out0);
            AddQuad(vertices, normals, underTriangles, point(a0, innerRadius, x0), point(a0, innerRadius, x1), point(a1, innerRadius, x1), point(a1, innerRadius, x0), -out0);

            // The two open ends of the roof: close the thickness.
            AddQuad(vertices, normals, topTriangles, point(a0, radius, x0), point(a1, radius, x0), point(a1, innerRadius, x0), point(a0, innerRadius, x0), Vector3.left);
            AddQuad(vertices, normals, topTriangles, point(a0, radius, x1), point(a1, radius, x1), point(a1, innerRadius, x1), point(a0, innerRadius, x1), Vector3.right);
        }

        // The two long eave edges: close the thickness there too.
        foreach (float sign in new[] { -1f, 1f })
        {
            float a = sign * maxAngle;
            Vector3 along = new Vector3(0f, -Mathf.Sin(a), Mathf.Cos(a)) * sign; // points away from the crown along the roof
            AddQuad(vertices, normals, topTriangles, point(a, radius, x0), point(a, radius, x1), point(a, innerRadius, x1), point(a, innerRadius, x0), along);
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RoofMeshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "EXT03_RoofVault" };
            AssetDatabase.CreateAsset(mesh, RoofMeshPath);
        }
        else
        {
            mesh.Clear(); // refill the existing asset (same file, same GUID)
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(topTriangles, 0);
        mesh.SetTriangles(underTriangles, 1);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    // Adds one flat four-cornered face. 'facing' says which way the visible side should point; the
    // corners are flipped if needed so the face is drawn from that side. Each face has its own
    // vertices, which gives the flat, faceted look.
    static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        if (Vector3.Dot(normal, facing) < 0f)
        {
            // Wrong way round: reverse the corner order.
            Vector3 swap = b;
            b = d;
            d = swap;
            normal = -normal;
        }

        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        for (int i = 0; i < 4; i++)
        {
            normals.Add(normal);
        }
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
