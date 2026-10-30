using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A test for the main entrance: can an avatar walk in from the plaza and reach the Floor1 trigger? It does NOT run the game. It opens
// Exterior.unity and sweeps a capsule shaped like the avatar's CharacterController (radius 0.3 m, height 1.8 m) from the plaza toward the
// building along several lines across the porch, and reports where the capsule is stopped by a solid (non-trigger) collider, and whether
// the trigger StairsEntrance_ToFloor1 (a trigger box) is reached before that.
//
//   Unity -batchmode -quit -projectPath ./UnityProject -executeMethod EntranceCheck.Run -logFile entrancecheck.log
//   (look for lines starting with "ENTRANCECHECK" in entrancecheck.log)
//
// Editor only; it never saves the scene.
public static class EntranceCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene(SiteBuilderUtil.ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        GameObject triggerObject = GameObject.Find("StairsEntrance_ToFloor1");
        var triggerBox = triggerObject != null ? triggerObject.GetComponent<BoxCollider>() : null;
        Debug.Log("ENTRANCECHECK trigger object found: " + (triggerBox != null) + (triggerBox != null ? ", world bounds " + triggerBox.bounds : ""));

        foreach (float x in new[] { 17.6f, 18.2f, 19.0f, 20.0f, 21.0f, 21.9f, 22.4f })
        {
            Vector3 bottom = new Vector3(x, 0.3f, -4f);
            Vector3 top = new Vector3(x, 1.5f, -4f);
            RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, 0.3f, Vector3.forward, 8f, ~0, QueryTriggerInteraction.Collide);
            float stopAt = float.MaxValue;
            string stopper = "nothing";
            float triggerAt = float.MaxValue;
            foreach (RaycastHit h in hits)
            {
                if (h.collider.isTrigger)
                {
                    if (h.collider == triggerBox)
                    {
                        triggerAt = Mathf.Min(triggerAt, -4f + h.distance);
                    }
                    continue;
                }
                if (h.distance > 0f && -4f + h.distance < stopAt)
                {
                    stopAt = -4f + h.distance;
                    stopper = h.collider.transform.parent != null ? h.collider.transform.parent.name + "/" + h.collider.name : h.collider.name;
                }
            }
            bool reaches = triggerAt < stopAt;
            Debug.Log("ENTRANCECHECK walking north at x " + x + ": stopped at z " + (stopAt == float.MaxValue ? "(not stopped)" : stopAt.ToString("0.00")) + " by " + stopper
                + "; trigger reached at z " + (triggerAt == float.MaxValue ? "(never)" : triggerAt.ToString("0.00")) + " -> " + (reaches ? "ENTERS" : "does not enter"));
        }
    }
}
