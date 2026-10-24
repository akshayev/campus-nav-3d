using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Takes a few screenshots of the Exterior scene from fixed camera spots, so the EXT05 front yard can be
// checked without pressing Play. It opens the scene but never saves it.
//
//   Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EXT05_Preview.Capture -logFile preview.log
//
// The pictures go to the folder in the EXT05_PREVIEW_DIR environment variable (default: ./preview).
public static class EXT05_Preview
{
    struct View
    {
        public string name;
        public Vector3 from;
        public Vector3 lookAt;
    }

    public static void Capture()
    {
        string dir = System.Environment.GetEnvironmentVariable("EXT05_PREVIEW_DIR");
        if (string.IsNullOrEmpty(dir))
        {
            dir = "preview";
        }
        Directory.CreateDirectory(dir);

        EditorSceneManager.OpenScene(SiteBuilderUtil.ScenePath, OpenSceneMode.Single);

        var views = new[]
        {
            new View { name = "1_front_overview", from = new Vector3(20f, 14f, -40f), lookAt = new Vector3(20f, 3f, 0f) },
            new View { name = "2_from_west",      from = new Vector3(-50f, 5f, -22f),  lookAt = new Vector3(15f, 3f, -4f) },
            new View { name = "3_avatar_view",    from = new Vector3(0f, 3f, -12f),    lookAt = new Vector3(18f, 3f, 0f) },
            new View { name = "4_gazebo_close",   from = new Vector3(-24f, 4f, -20f),  lookAt = new Vector3(-24f, 2f, -4f) },
            new View { name = "5_stage",          from = new Vector3(-45f, 2.5f, -6f),  lookAt = new Vector3(-55.4f, 3f, -19.3f) },
            new View { name = "9_porch_left",     from = new Vector3(8f, 2.2f, -14f),  lookAt = new Vector3(-7f, 2.5f, 1f) },
            new View { name = "10_entry_right",   from = new Vector3(44f, 2.2f, -16f),  lookAt = new Vector3(24f, 2.5f, 2f) },
            new View { name = "11_entry_wide",    from = new Vector3(-18f, 3f, -24f),   lookAt = new Vector3(22f, 6f, 6f) },
            new View { name = "7_canteen_front",  from = new Vector3(-34f, 1.8f, 5.5f), lookAt = new Vector3(-46f, 2.5f, 23.8f) },
            new View { name = "8_canteen_side",   from = new Vector3(-12f, 6f, 4f),  lookAt = new Vector3(-41f, 2f, 28f) },
            new View { name = "12_mca_front",     from = new Vector3(-44f, 2f, 28f),   lookAt = new Vector3(-62f, 2.2f, 28f) },
            new View { name = "13_mca_wide",      from = new Vector3(-36f, 9f, 2f),    lookAt = new Vector3(-58f, 2f, 30f) },
            new View { name = "14_mca_end",       from = new Vector3(-50f, 2.5f, 3f),  lookAt = new Vector3(-60f, 2.5f, 20f) },
            new View { name = "15_stage_blocks",  from = new Vector3(-62f, 2.5f, -2f),  lookAt = new Vector3(-50f, 3f, -30f) },
            new View { name = "20_tower",         from = new Vector3(-28f, 2f, -18f),   lookAt = new Vector3(-33.7f, 6f, -28.9f) },
            new View { name = "16_court",         from = new Vector3(10f, 2f, -12f),   lookAt = new Vector3(28f, 1.5f, -31f) },
            new View { name = "17_path",          from = new Vector3(-48f, 1.8f, -19f), lookAt = new Vector3(-48f, 2.2f, -46f) },
            new View { name = "18_yard_wide",     from = new Vector3(-6f, 14f, -8f),   lookAt = new Vector3(-20f, 0f, -48f) },
            new View { name = "19_row",           from = new Vector3(6f, 4f, -30f),    lookAt = new Vector3(-28f, 2f, -50f) },
            new View { name = "21_west_yard",     from = new Vector3(-31f, 2f, 0f),    lookAt = new Vector3(-20f, 2.5f, 20f) },
            new View { name = "22_west_yard_high", from = new Vector3(-8f, 20f, -14f),  lookAt = new Vector3(-22f, 0f, 10f) },
            new View { name = "23_band",         from = new Vector3(-24f, 2.5f, -2f),  lookAt = new Vector3(-58f, 2f, -10f) },
            new View { name = "24_band_high",    from = new Vector3(-40f, 24f, 16f),   lookAt = new Vector3(-40f, 0f, -8f) },
            new View { name = "25_west_end",     from = new Vector3(-24f, 2.2f, -3f),  lookAt = new Vector3(-72f, 2.5f, -4f) },
            new View { name = "26_west_end_wide", from = new Vector3(-30f, 2.2f, 9f),   lookAt = new Vector3(-70f, 2.5f, -2f) },
            new View { name = "27_west_end_high", from = new Vector3(-30f, 22f, 14f),   lookAt = new Vector3(-58f, 0f, -4f) },
            new View { name = "28_canteen_tree", from = new Vector3(-38f, 1.8f, -2f),  lookAt = new Vector3(-44f, 6f, 22f) },
            new View { name = "29_tree_wide",    from = new Vector3(-20f, 3f, 4f),    lookAt = new Vector3(-45f, 5f, 20f) },
            new View { name = "30_mca_end",     from = new Vector3(-40f, 1.8f, 3f),   lookAt = new Vector3(-58f, 2.5f, 16f) },
            new View { name = "31_mca_from_pave", from = new Vector3(-24f, 4f, 2f),    lookAt = new Vector3(-58f, 2f, 18f) },
            new View { name = "32_photo_view",   from = new Vector3(-37f, 1.8f, 2f),   lookAt = new Vector3(-58f, 2.5f, 14f) },
            new View { name = "33_canteen_front", from = new Vector3(-41f, 2f, 3f),    lookAt = new Vector3(-41f, 3f, 24f) },
            new View { name = "34_row",          from = new Vector3(-34f, 3f, 6f),    lookAt = new Vector3(-58f, 2.5f, 40f) },
            new View { name = "35_row_high",     from = new Vector3(-30f, 26f, 10f),  lookAt = new Vector3(-52f, 0f, 44f) },
            new View { name = "36_canteen_new",  from = new Vector3(-41f, 2.2f, 26f), lookAt = new Vector3(-41f, 3f, 44f) },
            new View { name = "37_one_building", from = new Vector3(-40f, 4f, 2f),   lookAt = new Vector3(-58f, 2.5f, 36f) },
            new View { name = "38_one_high",     from = new Vector3(-28f, 30f, 8f),   lookAt = new Vector3(-52f, 0f, 38f) },
            new View { name = "39_like_photo",  from = new Vector3(-44f, 1.8f, 5f),  lookAt = new Vector3(-60f, 2.8f, 22f) },
            new View { name = "40_mca_from_paving", from = new Vector3(-40f, 4.5f, -2f), lookAt = new Vector3(-58f, 2.5f, 20f) },
            new View { name = "41_row_from_pave", from = new Vector3(-40f, 4.5f, -10f), lookAt = new Vector3(-60f, 2.5f, 12f) },
            new View { name = "42_row_high",     from = new Vector3(-30f, 34f, -6f),   lookAt = new Vector3(-56f, 0f, 14f) },
            new View { name = "43_tree_photo",  from = new Vector3(-50f, 1.8f, -4f),  lookAt = new Vector3(-48f, 6f, 12f) },
            new View { name = "44_tree_wide",   from = new Vector3(-36f, 3f, -2f),    lookAt = new Vector3(-48f, 6f, 18f) },
            new View { name = "45_entry_path", from = new Vector3(20f, 1.9f, -22f), lookAt = new Vector3(20f, 3f, 3f) },
            new View { name = "46_entry_high", from = new Vector3(20f, 22f, -26f), lookAt = new Vector3(20f, 0f, -2f) },
            new View { name = "47_path_view",   from = new Vector3(20f, 1.9f, -12f),  lookAt = new Vector3(20f, 3f, -40f) },
            new View { name = "48_plaza_edge",  from = new Vector3(48f, 1.9f, -6f),   lookAt = new Vector3(0f, 3f, -12f) },
            new View { name = "49_front_clear", from = new Vector3(20f, 14f, -30f),   lookAt = new Vector3(20f, 0f, -6f) },
            new View { name = "50_stage_lawn",    from = new Vector3(-52f, 1.9f, -2f),  lookAt = new Vector3(-34f, 2.5f, -30f) },
            new View { name = "51_lawn_high",     from = new Vector3(-34f, 30f, 6f),    lookAt = new Vector3(-36f, 0f, -22f) },
            new View { name = "52_cars_close", from = new Vector3(14f, 1.8f, -34f), lookAt = new Vector3(14f, 1.0f, -50f) },
            new View { name = "53_cars_side",  from = new Vector3(-12f, 3.5f, -38f), lookAt = new Vector3(14f, 0.8f, -50f) },
            new View { name = "54_court_photo", from = new Vector3(14f, 1.8f, -17f),  lookAt = new Vector3(28f, 2.0f, -30f) },
            new View { name = "55_court_high",  from = new Vector3(28f, 26f, -10f),   lookAt = new Vector3(28f, 0f, -29f) },
            new View { name = "56_court_photo2", from = new Vector3(8f, 1.8f, -17f),   lookAt = new Vector3(36f, 2.0f, -30f) },
            new View { name = "57_court_high2",  from = new Vector3(34f, 26f, -8f),    lookAt = new Vector3(34f, 0f, -28f) },
            new View { name = "58_courts",      from = new Vector3(-2f, 1.8f, -16f),  lookAt = new Vector3(30f, 2.0f, -30f) },
            new View { name = "59_courts_high", from = new Vector3(30f, 28f, -6f),    lookAt = new Vector3(30f, 0f, -28f) },
            new View { name = "6_stage_and_sheds", from = new Vector3(-50f, 8f, -4f),  lookAt = new Vector3(-10f, 2f, -28f) },
        };

        var cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.farClipPlane = 500f;
        camera.clearFlags = CameraClearFlags.Skybox;

        foreach (View view in views)
        {
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            camera.transform.position = view.from;
            camera.transform.LookAt(view.lookAt);
            camera.Render();

            RenderTexture.active = target;
            var picture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            picture.Apply();
            File.WriteAllBytes(Path.Combine(dir, view.name + ".png"), picture.EncodeToPNG());

            RenderTexture.active = null;
            Object.DestroyImmediate(picture);
            Object.DestroyImmediate(target);
        }

        Object.DestroyImmediate(cameraObject);
        Debug.Log("EXT05_Preview: wrote " + views.Length + " pictures to " + Path.GetFullPath(dir));
    }
}

