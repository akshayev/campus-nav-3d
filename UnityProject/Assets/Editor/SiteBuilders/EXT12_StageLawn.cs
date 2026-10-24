using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT12 stage lawn in Exterior.unity, copied from the reference photo of the open lawn in front of the
// stage: a straight row of small flowering plants across the grass, a patch of bare earth with a small red flag
// and a mound of soil, and a tall lattice cell tower in the distance.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT12 Stage Lawn
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT12_StageLawn.Build -logFile ext12.log
//
// It is RE-RUNNABLE: every run deletes the old EXT12_StageLawn object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT12_StageLawn, to Exterior.unity. Nothing that already
// exists is changed. The plants are small copies of the EXT05 bush (so build EXT05 first). Nothing stands on brick:
// the row is on the lawn between the stage (z -15.3) and the paving band (z -12.6), and the rest is further south
// and west.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT12_StageLawn
{
    const string RootName = "EXT12_StageLawn";
    const string Prefix = "EXT12";

    // The row of small plants: from x RowMinX to RowMaxX at z RowZ, one every RowSpacing metres, alternating red and yellow.
    const float RowMinX = -47f;
    const float RowMaxX = -20f;
    const float RowZ = -14f;
    const float RowSpacing = 3.4f;
    const float PlantScale = 0.6f;       // the plants in the photo are small (about 1 m)

    // Bare-earth patch with the flag and the soil mound (the middle of the patch).
    const float PatchX = -38f;
    const float PatchZ = -28f;

    // Cell tower, far behind the long block (the boundary wall is at z -81).
    const float TowerX = -66f;
    const float TowerZ = -68f;
    const float TowerHeight = 30f;

    [MenuItem("Campus/Build EXT12 Stage Lawn")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT12_StageLawn", BuildInternal);
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

        Material leaves = SiteBuilderUtil.GetMaterial(Prefix, "BushGreen", new Color32(60, 118, 52, 255), 0.05f);
        Material red = SiteBuilderUtil.GetMaterial(Prefix, "FlowerRed", new Color32(212, 44, 36, 255), 0.1f);
        Material yellow = SiteBuilderUtil.GetMaterial(Prefix, "FlowerYellow", new Color32(236, 200, 40, 255), 0.1f);
        Material earth = SiteBuilderUtil.GetMaterial(Prefix, "BareEarth", new Color32(150, 100, 70, 255), 0.02f);
        Material soil = SiteBuilderUtil.GetMaterial(Prefix, "SoilMound", new Color32(70, 52, 40, 255), 0.02f);
        Material flagRed = SiteBuilderUtil.GetMaterial(Prefix, "FlagRed", new Color32(210, 40, 44, 255), 0.1f);
        Material pole = SiteBuilderUtil.GetMaterial(Prefix, "FlagPole", new Color32(40, 40, 44, 255), 0.2f);
        Material steel = SiteBuilderUtil.GetMaterial(Prefix, "TowerSteel", new Color32(150, 154, 158, 255), 0.3f);

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // The row of small plants.
        Transform row = SiteBuilderUtil.Group(root.transform, "PlantRow");
        int count = Mathf.RoundToInt((RowMaxX - RowMinX) / RowSpacing) + 1;
        for (int i = 0; i < count; i++)
        {
            float x = RowMinX + i * RowSpacing;
            string name = "Plant_" + (i + 1);
            EXT05_FrontInterlock.BuildFlowerBush(row, name, x, RowZ, leaves, i % 2 == 0 ? red : yellow);
            row.Find(name).localScale = new Vector3(PlantScale, PlantScale, PlantScale);
        }

        // Bare earth, a mound of soil and the small red flag on a thin pole.
        Transform patch = SiteBuilderUtil.Group(root.transform, "BarePatch");
        SiteBuilderUtil.Part(patch, "Earth_1", PrimitiveType.Cylinder, new Vector3(PatchX, 0.02f, PatchZ), new Vector3(9f, 0.006f, 4.4f), earth, false);
        SiteBuilderUtil.Part(patch, "Earth_2", PrimitiveType.Cylinder, new Vector3(PatchX + 5f, 0.02f, PatchZ - 1.2f), new Vector3(6f, 0.006f, 3.2f), earth, false);
        SiteBuilderUtil.Part(patch, "Mound", PrimitiveType.Sphere, new Vector3(PatchX + 3f, 0.12f, PatchZ - 0.5f), new Vector3(1.8f, 0.4f, 1.4f), soil, false);
        SiteBuilderUtil.Part(patch, "FlagPole", PrimitiveType.Cylinder, new Vector3(PatchX + 1f, 0.8f, PatchZ + 0.3f), new Vector3(0.04f, 0.8f, 0.04f), pole, false);
        SiteBuilderUtil.Part(patch, "Flag", PrimitiveType.Cube, new Vector3(PatchX + 1.25f, 1.45f, PatchZ + 0.3f), new Vector3(0.5f, 0.3f, 0.02f), flagRed, false);

        BuildCellTower(root.transform, steel);

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT12_StageLawn: built " + SiteBuilderUtil.CountObjects(root) + " objects.");
    }

    // A tall lattice mast: three thin legs leaning in toward the top, with horizontal rings every 3 m and a small
    // box of antennas near the top. Visual only (no colliders).
    static void BuildCellTower(Transform parent, Material steel)
    {
        Transform tower = SiteBuilderUtil.Group(parent, "CellTower");
        tower.localPosition = new Vector3(TowerX, 0f, TowerZ);
        const float baseRadius = 1.6f;
        const float topRadius = 0.35f;
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(Mathf.Cos(angle) * baseRadius, 0f, Mathf.Sin(angle) * baseRadius);
            Vector3 p1 = new Vector3(Mathf.Cos(angle) * topRadius, TowerHeight, Mathf.Sin(angle) * topRadius);
            Vector3 direction = p1 - p0;
            GameObject leg = SiteBuilderUtil.Part(tower, "Leg_" + (i + 1), PrimitiveType.Cylinder, (p0 + p1) / 2f,
                new Vector3(0.12f, direction.magnitude / 2f, 0.12f), steel, false);
            leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
        }
        for (float y = 3f; y < TowerHeight; y += 3f)
        {
            float r = Mathf.Lerp(baseRadius, topRadius, y / TowerHeight);
            for (int i = 0; i < 3; i++)
            {
                float a0 = i * 120f * Mathf.Deg2Rad;
                float a1 = (i + 1) * 120f * Mathf.Deg2Rad;
                Vector3 s = new Vector3(Mathf.Cos(a0) * r, y, Mathf.Sin(a0) * r);
                Vector3 t = new Vector3(Mathf.Cos(a1) * r, y, Mathf.Sin(a1) * r);
                Vector3 d = t - s;
                GameObject bar = SiteBuilderUtil.Part(tower, "Ring_" + y + "_" + (i + 1), PrimitiveType.Cube, (s + t) / 2f,
                    new Vector3(0.06f, 0.06f, d.magnitude), steel, false);
                bar.transform.localRotation = Quaternion.LookRotation(d);
            }
        }
        SiteBuilderUtil.Part(tower, "Antennas", PrimitiveType.Cube, new Vector3(0f, TowerHeight - 3f, 0f), new Vector3(1.2f, 2.6f, 0.9f), steel, false);
    }
}
