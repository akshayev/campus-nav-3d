using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared helpers for the exterior "site builder" scripts (EXT03_BikeShed, EXT04_CarShed, ...).
//
// Every site builder does the same boring things: open Exterior.unity, find its site object in the
// scene, delete the old model, make materials, make named groups and shapes, save, and tidy up. That
// code lives here once, so each builder only has to say WHAT its site looks like.
//
// This file lives in an "Editor" folder, so it is never part of the game build.
public static class SiteBuilderUtil
{
    public const string ScenePath = "Assets/Scenes/Exterior.unity";
    public const string MaterialFolder = "Assets/Materials/Sites";

    // Runs a builder's body. A mistake (missing object, bad constant) is logged as an error and, in
    // batch mode, makes Unity exit with code 1 so a script or CI run can see that it failed.
    public static void Run(string builderName, System.Action body)
    {
        try
        {
            body();
        }
        catch (System.Exception e)
        {
            Debug.LogError(builderName + ": build FAILED: " + e);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    // Opens Exterior.unity. In the Editor it first offers to save anything unsaved (batch mode has
    // nothing to save). Returns false if the person clicked Cancel, in which case the builder should stop.
    public static bool TryOpenExteriorScene(out Scene scene)
    {
        scene = default;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return false;
        }

        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    // Finds the site's top-level object in the scene. It has the old name (for example POI_BikeShed) the
    // first time a builder runs and the new tagged name (EXT03_BikeShed) after that. The object is renamed
    // to the new name. Its position, trigger and poiId are never touched.
    public static GameObject FindAndRenameRoot(Scene scene, string newName, string oldName)
    {
        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == newName || candidate.name == oldName)
            {
                if (root != null)
                {
                    throw new System.Exception("Found more than one site root (" + newName + " / " + oldName + "). Fix the scene by hand first.");
                }
                root = candidate;
            }
        }
        if (root == null)
        {
            throw new System.Exception("Could not find '" + oldName + "' or '" + newName + "' at the top level of Exterior.unity.");
        }
        root.name = newName;
        return root;
    }

    // Deletes the root's direct children with the given names (the old placeholder "Cube" and any model
    // from a previous run), so a re-run starts clean.
    public static void DeleteChildren(GameObject root, params string[] names)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (System.Array.IndexOf(names, child.name) >= 0)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    // Makes the empty "Model" object under the root. The root sits at some height in the scene, so the
    // model is lowered by that height: local y = 0 inside the model is the ground (world y = 0).
    public static Transform CreateModel(GameObject root, string modelName)
    {
        var model = new GameObject(modelName).transform;
        model.SetParent(root.transform, false);
        model.localPosition = new Vector3(0f, -root.transform.position.y, 0f);
        return model;
    }

    // An empty named object used to group parts (Roof, Poles, ...).
    public static Transform Group(Transform parent, string name)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    // Makes one named shape. Every primitive comes with a collider; we remove it unless asked to keep it.
    public static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        return part;
    }

    // Same as Part, then turns the shape (rotation in degrees around x, y, z).
    public static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles, Material material, bool keepCollider)
    {
        GameObject part = Part(parent, name, type, localPosition, localScale, material, keepCollider);
        part.transform.localRotation = Quaternion.Euler(localEulerAngles);
        return part;
    }

    public static void EnsureFolder(string parent, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folderName))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    // Loads Assets/Materials/Sites/<prefix>_<name>.mat (for example EXT03_RoofTop.mat), or creates it.
    // Same Standard shader as the project's other materials, no textures. An existing material is updated
    // in place (same file, same GUID), so re-running never churns the .meta files.
    public static Material GetMaterial(string prefix, string name, Color32 color, float smoothness)
    {
        string path = MaterialFolder + "/" + prefix + "_" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Same as above, plus a constant glow (emission) added on top of the lighting. Used for surfaces that the
    // scene's light never reaches, such as a wall facing away from the sun, which would otherwise render grey.
    public static Material GetMaterial(string prefix, string name, Color32 color, float smoothness, Color32 emission)
    {
        Material material = GetMaterial(prefix, name, color, smoothness);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
        return material;
    }

    // Saves the scene and the assets, then removes the "groundFloorPlacements" block.
    //
    // Why the last step: FloorTransitionManager has a TEMPORARY list called groundFloorPlacements (see
    // docs/PHASE_LOG.md). Every time Unity saves Exterior.unity it writes that whole list into the scene
    // file, which adds about 40 lines of unrelated noise to every site-builder commit. The list's default
    // values come from the script, so the scene works the same without them; this removes them from the
    // saved file again. When the list is deleted from FloorTransitionManager one day, this step simply
    // finds nothing to remove.
    public static void SaveSceneAndAssets(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string fullPath = Path.GetFullPath(ScenePath);
        string text = File.ReadAllText(fullPath);
        string cleaned = Regex.Replace(text, @"^  groundFloorPlacements:\r?\n(?:(?:  - |    ).*\r?\n)*", "", RegexOptions.Multiline);
        if (cleaned != text)
        {
            File.WriteAllText(fullPath, cleaned);
            Debug.Log("SiteBuilderUtil: removed the groundFloorPlacements block from the saved scene file.");
            if (!Application.isBatchMode)
            {
                // The open scene is now older than the file on disk; reload it so Unity does not ask.
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }
    }

    // The number of objects under the site root, including the root itself.
    public static int CountObjects(GameObject root)
    {
        return root.GetComponentsInChildren<Transform>(true).Length;
    }
}
