using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the EXT10 west yard in Exterior.unity: the wide brick ("interlock") paved yard between the west end
// of the main building and the canteen kiosk, with rain puddles, as in the reference photo taken beside the
// basketball goal. It joins the front plaza (EXT05) at its south end and runs north to the kiosk's front.
//
// How to use it:
//   - In the Unity Editor:  menu  Campus > Build EXT10 West Yard
//   - From the command line (Unity closed):
//       Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT10_WestYard.Build -logFile ext10.log
//
// It is RE-RUNNABLE: every run deletes the old EXT10_WestYard object and builds a fresh one.
//
// What it touches: it ADDS one new top-level object, EXT10_WestYard, to Exterior.unity. Nothing that already
// exists is changed. The paving is visual only (no collider): the avatar still walks on the existing GroundPlane.
// It needs the brick picture made by EXT05 (Assets/Textures/Sites/EXT05_Interlock.png), so build EXT05 first.
//
// Layout (world metres):
//
//      x = -30.5 .. -12.8   between the canteen's roof edge (x -30.9) and the main building's west wall (x -13)
//      z = -12.6 .. 21.5    from the far edge of the front plaza up to the kiosk's front wall
//
// Plus a band, x -57 .. -30.3, z -12.6 .. 3.4: the plaza's row carried west past the stage.
//
// The slab is 4 cm thick, a little higher than the other paving (3 cm), so where they overlap the yard shows
// on top instead of flickering against them.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class EXT10_WestYard
{
    const string RootName = "EXT10_WestYard";
    const string Prefix = "EXT10";

    const float MinX = -30.5f;
    const float MaxX = -12.8f;           // runs 0.2 m into the front plaza (EXT05), so there is no grass gap between them
    const float MinZ = -12.6f;
    const float MaxZ = 21.5f;

    // The band: the plaza's row (same z range) carried on west, past the stage (x -61.4 .. -49.4) so the stage
    // faces paving. It overlaps the yard's west edge a little so there is no gap.
    const float BandMinX = -57f;           // stops at the MCA building's veranda (x -58)
    const float BandMaxX = -30.3f;
    const float BandMinZ = -12.6f;
    const float BandMaxZ = 3.4f;
    const float Thickness = 0.04f;
    const float TileSize = 0.8f;

    // Puddles: x, z, radius along x, radius along z. The big one lies where the photo shows it, in the middle
    // of the yard, with two smaller ones near the building.
    static readonly float[][] Puddles =
    {
        new[] { -22f,  9f,  6.5f, 3.2f },
        new[] { -16.5f, 15f, 2.6f, 1.6f },
        new[] { -26f,  -1f, 3.2f, 1.8f },
    };

    [MenuItem("Campus/Build EXT10 West Yard")]
    public static void Build()
    {
        SiteBuilderUtil.Run("EXT10_WestYard", BuildInternal);
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

        float width = MaxX - MinX;
        float depth = MaxZ - MinZ;

        Material brick = SiteBuilderUtil.GetMaterial(Prefix, "YardBrick", new Color32(255, 255, 255, 255), 0.1f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sites/EXT05_Interlock.png");
        if (texture != null)
        {
            brick.mainTexture = texture;
            brick.mainTextureScale = new Vector2(width / TileSize, depth / TileSize);
        }
        else
        {
            brick.color = new Color32(124, 104, 94, 255); // run Campus > Build EXT05 Front Yard first for the brick picture
        }

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        SiteBuilderUtil.Part(root.transform, "Paving", PrimitiveType.Cube,
            new Vector3((MinX + MaxX) / 2f, Thickness / 2f, (MinZ + MaxZ) / 2f),
            new Vector3(width, Thickness, depth), brick, false);

        // The band west of the yard (its own material, so the brick pattern keeps its size).
        float bandWidth = BandMaxX - BandMinX;
        float bandDepth = BandMaxZ - BandMinZ;
        Material bandBrick = SiteBuilderUtil.GetMaterial(Prefix, "BandBrick", new Color32(255, 255, 255, 255), 0.1f);
        if (texture != null)
        {
            bandBrick.mainTexture = texture;
            bandBrick.mainTextureScale = new Vector2(bandWidth / TileSize, bandDepth / TileSize);
        }
        else
        {
            bandBrick.color = new Color32(124, 104, 94, 255);
        }
        SiteBuilderUtil.Part(root.transform, "StageBand", PrimitiveType.Cube,
            new Vector3((BandMinX + BandMaxX) / 2f, Thickness / 2f, (BandMinZ + BandMaxZ) / 2f),
            new Vector3(bandWidth, Thickness, bandDepth), bandBrick, false);
        // (The puddles on the yard and the band were removed: the team does not want the blue patches on the interlock.)

        SiteBuilderUtil.SaveSceneAndAssets(scene);
        Debug.Log("EXT10_WestYard: built " + SiteBuilderUtil.CountObjects(root) + " objects (" + width + " m x " + depth + " m).");
    }
}
