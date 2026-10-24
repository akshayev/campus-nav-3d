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
