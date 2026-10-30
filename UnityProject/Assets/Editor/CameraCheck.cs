using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A test for the "avatar disappears inside a building" bug. It does NOT run the game: it opens Exterior.unity, puts an imaginary avatar
// on a grid of spots in and around the badminton hall, the bike shed and the car shed, and for each spot and each of 8 directions it
// does what CampusCameraController does every frame:
//   1. works out where the follow camera wants to sit (offset 0,5,-8 behind the avatar),
//   2. fires the camera's collision ray from the avatar to that spot (Default layer only, triggers ignored),
// and counts how many spots the ray is BLOCKED (the camera would be pulled in right next to the avatar). It also lists WHAT blocked it.
//
//   Unity -batchmode -quit -projectPath ./UnityProject -executeMethod CameraCheck.Run -logFile camcheck.log
//   (look for lines starting with "CAMCHECK" in camcheck.log)
//
// It lives in an "Editor" folder, so it is never part of the game build, and it never saves the scene.
public static class CameraCheck
{
    struct Area
    {
        public string name;
        public float minX, maxX, minZ, maxZ;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(SiteBuilderUtil.ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        // Hall (x 54.8 .. 63.2, z 21 .. 40.4), bike shed (x 43 .. 76, z -51 .. -43), car shed (canopy + block).
        var areas = new[]
        {
            new Area { name = "BadmintonHall", minX = 55.2f, maxX = 62.8f, minZ = 21.6f, maxZ = 40.0f },
            new Area { name = "BikeShed",      minX = 44f,   maxX = 75f,   minZ = -50.5f, maxZ = -43.5f },
            new Area { name = "CarShed",       minX = -8f,   maxX = 37f,   minZ = -57f,   maxZ = -42f },
        };

        foreach (Area area in areas)
        {
            Check(area);
        }
    }

    static void Check(Area area)
    {
        int samples = 0;
        int blocked = 0;
        var blockers = new Dictionary<string, int>();
        var minRatio = 1f;
        int viewBlocked = 0;
        var viewBlockers = new Dictionary<string, int>();

        for (float x = area.minX; x <= area.maxX; x += 1.5f)
        {
            for (float z = area.minZ; z <= area.maxZ; z += 1.5f)
            {
                var pivot = new Vector3(x, 1f, z);
                // Skip spots that are inside a solid collider (the avatar could not stand there).
                if (Physics.CheckSphere(pivot, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                for (int yaw = 0; yaw < 360; yaw += 45)
                {
                    samples++;
                    Vector3 desired = pivot + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 5f, -8f);
                    Vector3 direction = desired - pivot;
                    float distance = direction.magnitude;

                    // Second test: what solid thing lies on the straight line from the camera to the avatar (every layer)? Parts that a roof
                    // fader hides (hall walls and roof, the car shed block, the bike shed mural wall) do not count: they vanish when in the way.
                    foreach (RaycastHit h in Physics.RaycastAll(desired, -direction.normalized, distance - 0.2f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        string p = PathOf(h.collider.transform);
                        if (p.Contains("/SideWalls") || p.Contains("/EndWalls") || p.Contains("EXT04_Model/Block") || p.Contains("MuralWall"))
                        {
                            continue;
                        }
                        viewBlocked++;
                        string vk = p + " (layer " + h.collider.gameObject.layer + ")";
                        viewBlockers[vk] = viewBlockers.TryGetValue(vk, out int vn) ? vn + 1 : 1;
                        break;
                    }
                    if (Physics.Raycast(pivot, direction.normalized, out RaycastHit hit, distance, 1, QueryTriggerInteraction.Ignore))
                    {
                        blocked++;
                        string key = PathOf(hit.collider.transform) + " (layer " + hit.collider.gameObject.layer + ")";
                        blockers[key] = blockers.TryGetValue(key, out int n) ? n + 1 : 1;
                        minRatio = Mathf.Min(minRatio, Mathf.Max(hit.distance - 0.3f, 0.1f) / distance);
                    }
                }
            }
        }

        var report = new StringBuilder();
        report.Append("CAMCHECK ").Append(area.name).Append(": ").Append(blocked).Append(" of ").Append(samples)
            .Append(" camera rays are blocked (").Append(samples == 0 ? 0 : Mathf.RoundToInt(100f * blocked / samples)).Append("%); closest the camera is pulled in to ")
            .Append(Mathf.RoundToInt(minRatio * 100f)).Append("% of its distance.");
        foreach (var pair in blockers.OrderByDescending(p => p.Value).Take(8))
        {
            report.Append("\nCAMCHECK     ").Append(pair.Value).Append(" x ").Append(pair.Key);
        }
        report.Append("\nCAMCHECK   view line blocked by something the faders do NOT hide: ").Append(viewBlocked).Append(" of ").Append(samples);
        foreach (var pair in viewBlockers.OrderByDescending(p => p.Value).Take(8))
        {
            report.Append("\nCAMCHECK       ").Append(pair.Value).Append(" x ").Append(pair.Key);
        }
        Debug.Log(report.ToString());
    }

    // "EXT14_BadmintonHall/Model/SideWalls/WallOuter" without numbers in the names, so repeated parts group together.
    static string PathOf(Transform t)
    {
        var parts = new List<string>();
        for (Transform c = t; c != null; c = c.parent)
        {
            parts.Add(System.Text.RegularExpressions.Regex.Replace(c.name, @"_?\d+$", ""));
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
