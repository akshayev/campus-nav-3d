using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lives in the Exterior scene (which is never unloaded) and handles moving the avatar between
// Exterior and whichever floor is currently loaded on top of it, per the SDD's additive-scene
// design (Section 5.1/5.3): only one floor sub-scene is ever loaded at a time, kept separate
// from Exterior so WebGL build size/memory stay reasonable.
//
// Supports stacking floor-to-floor (e.g. Exterior -> Floor1 -> Floor2), not just
// Exterior <-> one floor. The key idea: instead of requiring someone to hand-place a spawn
// point for "where do I land when I come back down" (which would need a Transform reference
// pointing INTO a scene that's unloaded the moment you're not standing in it — impossible to
// wire reliably in the Editor), we just remember where the player was STANDING right before
// they went up, and teleport them back there when they come down. A stack of these
// "come back here" records is exactly what a Stack<T> is for: the floor you most recently
// entered from is the first one you return to.
public class FloorTransitionManager : MonoBehaviour
{
    public static FloorTransitionManager Instance { get; private set; }

    [Tooltip("The avatar's Transform — this script moves it to the right spot when entering/exiting a floor.")]
    [SerializeField] private Transform player;

    // Where the building sits in the Exterior scene's world space, and how tall one storey is.
    // Floor scenes are additive, so they share the Exterior's world coordinates: a floor's
    // ground, walls and stairs are built at (buildingOrigin + local position + floor height),
    // which puts them physically INSIDE the building rather than in a separate space. Room
    // positions in Firestore are stored relative to buildingOrigin (so real measurements can
    // be entered without knowing where the building is in Unity). Placeholder values: they match
    // the entrance trigger in Exterior; update both when the real model is placed.
    [Tooltip("World position of the building's front door (ground level), which is where room positions are measured from: x = left/right of the door, z = distance in from the door. Matches StairsEntrance_ToFloor1 in Exterior.")]
    [SerializeField] private Vector3 buildingOrigin = new Vector3(20f, 0f, 4f);

    [Tooltip("Height of one storey in metres. Floor N's ground sits at (N-1) * this above the building origin.")]
    [SerializeField] private float storeyHeight = 4f;

    [Tooltip("Optional: the floor indicator badge text (Step 6). Safe to leave empty for now.")]
    [SerializeField] private TMPro.TextMeshProUGUI floorIndicatorText;

    // One of these is pushed every time EnterFloor loads a new floor on top of whatever was
    // there before (Exterior, or another floor). sceneName/floorId are null when the level
    // being left is Exterior itself (Exterior has no scene to reload — it's never unloaded).
    private class ReturnPoint
    {
        public string sceneName;
        public string floorId;
        public Vector3 position;
    }

    private readonly Stack<ReturnPoint> returnStack = new Stack<ReturnPoint>();

    // Name of the currently-loaded floor scene, or null if we're outside (no floor loaded).
    private string currentFloorScene;

    // True while a transition coroutine is running, plus a short cooldown afterwards. Without
    // this, coming back down puts the player right where they climbed from, i.e. standing inside
    // the stairs trigger of the freshly reloaded floor, which would fire instantly and bounce
    // them straight back up.
    private bool transitioning;
    private float ignoreTriggersUntil;
    private const float TransitionCooldownSeconds = 1.5f;

    private bool CanTransition()
    {
        return !transitioning && Time.time >= ignoreTriggersUntil;
    }

    // floorId of the currently-loaded floor, or null if we're outside. Needed so ExitFloor can
    // re-fetch the PREVIOUS floor's rooms when returning to it (see UnloadAndReturnRoutine).
    private string currentFloorId;

    // The ids of room popups we added for the current floor, so we can clean them out of
    // POIManager's list again when the player leaves (see ClearRoomPOIs below).
    private List<string> currentRoomIds = new List<string>();

    // The runtime-generated trigger-volume GameObjects for the current floor's rooms (see
    // SpawnRoomTriggers), destroyed again when the player leaves the floor.
    private List<GameObject> currentRoomTriggers = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Moves the player instantly. A CharacterController keeps its own copy of the position and
    // snaps the transform back to it on the next Move() call, so just writing
    // player.position is silently undone. Switching the controller off for the write fixes it.
    private void TeleportPlayer(Vector3 position)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.position = position;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private IEnumerator RunTransition(IEnumerator routine)
    {
        transitioning = true;
        yield return routine;
        transitioning = false;
        ignoreTriggersUntil = Time.time + TransitionCooldownSeconds;
    }

    public void EnterFloor(string floorId, Vector3 spawnPosition)
    {
        if (!CanTransition())
        {
            return;
        }

        FloorData floor = FirebaseDataManager.Instance.CachedFloors.FirstOrDefault(f => f.floorId == floorId);
        if (floor == null || string.IsNullOrEmpty(floor.sceneName))
        {
            Debug.LogWarning($"FloorTransitionManager: no floor data (or no scene name) found for floorId '{floorId}'.");
            return;
        }

        StartCoroutine(RunTransition(EnterFloorRoutine(floor, spawnPosition)));
    }

    private IEnumerator EnterFloorRoutine(FloorData floor, Vector3 spawnPosition)
    {
        // Remember where to put the player when they come back down from this floor — right
        // where they're standing right now, on whatever level that is (Exterior or a floor).
        returnStack.Push(new ReturnPoint
        {
            sceneName = currentFloorScene,
            floorId = currentFloorId,
            position = player != null ? player.position : Vector3.zero,
        });

        yield return UnloadCurrentFloorRooms();

        if (currentFloorScene != null)
        {
            yield return SceneManager.UnloadSceneAsync(currentFloorScene);
        }

        yield return SceneManager.LoadSceneAsync(floor.sceneName, LoadSceneMode.Additive);

        currentFloorScene = floor.sceneName;
        currentFloorId = floor.floorId;

        if (player != null)
        {
            TeleportPlayer(spawnPosition);
        }

        if (floorIndicatorText != null)
        {
            floorIndicatorText.text = floor.label;
        }

        Debug.Log($"FloorTransitionManager: entered '{floor.label}' (scene '{floor.sceneName}'), avatar moved to {spawnPosition}.");

        FirebaseDataManager.Instance.FetchRoomsForFloor(floor.floorId, rooms =>
        {
            currentRoomIds = rooms.Select(r => r.roomId).ToList();
            if (POIManager.Instance != null)
            {
                POIManager.Instance.AddRoomPOIs(rooms);
            }
            SpawnRoomTriggers(rooms, floor);
            Debug.Log($"FloorTransitionManager: loaded {rooms.Count} room(s) for '{floor.floorId}'.");
        });
    }

    // Builds one invisible trigger Box Collider per room, positioned/sized from the room's
    // Firestore data (position, poiRadius), with a RoomTriggerVolume component watching it.
    // This is what makes a room popup actually appear when the Player walks near it — without
    // this, AddRoomPOIs only registers the room's text, nothing in the scene detects the Player.
    //
    // These coordinates are placeholders (no real floor-plan measurements exist yet, see
    // /reference) — correct room shapes/positions are a later task once real measurements exist.
    private void SpawnRoomTriggers(List<RoomData> rooms, FloorData floor)
    {
        // Room positions are local to the building, so shift them to world space and up to
        // this floor's height.
        Vector3 floorOrigin = buildingOrigin + Vector3.up * ((floor != null ? floor.floorNumber - 1 : 0) * storeyHeight);

        foreach (RoomData room in rooms)
        {
            GameObject trigger = new GameObject($"RoomTrigger_{room.roomId}");
            trigger.transform.position = floorOrigin + room.position;

            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            float size = room.poiRadius * 2f;
            collider.size = new Vector3(size, size, size);

            RoomTriggerVolume volume = trigger.AddComponent<RoomTriggerVolume>();
            volume.roomId = room.roomId;

            currentRoomTriggers.Add(trigger);
        }
    }

    // Removes the current floor's room popups from POIManager and destroys their trigger
    // GameObjects. Called before leaving a floor in either direction (going up to a new floor,
    // or going back down) — always a clean slate, never left-over triggers from a floor that's
    // no longer loaded.
    private IEnumerator UnloadCurrentFloorRooms()
    {
        if (POIManager.Instance != null)
        {
            POIManager.Instance.ClearRoomPOIs(currentRoomIds);
        }
        currentRoomIds.Clear();

        foreach (GameObject trigger in currentRoomTriggers)
        {
            if (trigger != null)
            {
                Destroy(trigger);
            }
        }
        currentRoomTriggers.Clear();

        yield return null;
    }

    // Called by a StairsTriggerVolume with Direction = ExitFloor. Always goes back to
    // wherever the player came FROM — Exterior if this was the first floor they entered,
    // or the previous floor if they'd climbed further (e.g. Floor2 -> Floor1).
    public void ExitFloor()
    {
        if (!CanTransition())
        {
            return;
        }

        if (currentFloorScene == null)
        {
            // Already outside — ignore a stray "exit" trigger.
            return;
        }

        if (returnStack.Count == 0)
        {
            Debug.LogWarning("FloorTransitionManager: ExitFloor called with an empty return stack — nothing to return to.");
            return;
        }

        StartCoroutine(RunTransition(ExitFloorRoutine()));
    }

    private IEnumerator ExitFloorRoutine()
    {
        ReturnPoint target = returnStack.Pop();

        yield return UnloadCurrentFloorRooms();

        string sceneToUnload = currentFloorScene;
        yield return SceneManager.UnloadSceneAsync(sceneToUnload);
        currentFloorScene = null;
        currentFloorId = null;

        if (target.sceneName == null)
        {
            // Returning to Exterior — already loaded, nothing to load.
            if (player != null)
            {
                TeleportPlayer(target.position);
            }

            if (floorIndicatorText != null)
            {
                floorIndicatorText.text = "Outside";
            }

            Debug.Log($"FloorTransitionManager: exited '{sceneToUnload}', avatar moved to {target.position} (Exterior).");
            yield break;
        }

        // Returning to a previous FLOOR (e.g. came down from Floor2 to Floor1) — reload it.
        yield return SceneManager.LoadSceneAsync(target.sceneName, LoadSceneMode.Additive);
        currentFloorScene = target.sceneName;
        currentFloorId = target.floorId;

        if (player != null)
        {
            TeleportPlayer(target.position);
        }

        FloorData floor = FirebaseDataManager.Instance.CachedFloors.FirstOrDefault(f => f.floorId == target.floorId);
        if (floorIndicatorText != null)
        {
            floorIndicatorText.text = floor != null ? floor.label : target.sceneName;
        }

        Debug.Log($"FloorTransitionManager: exited '{sceneToUnload}', returned to '{target.sceneName}', avatar moved to {target.position}.");

        FirebaseDataManager.Instance.FetchRoomsForFloor(target.floorId, rooms =>
        {
            currentRoomIds = rooms.Select(r => r.roomId).ToList();
            if (POIManager.Instance != null)
            {
                POIManager.Instance.AddRoomPOIs(rooms);
            }
            SpawnRoomTriggers(rooms, floor);
            Debug.Log($"FloorTransitionManager: re-loaded {rooms.Count} room(s) for '{target.floorId}'.");
        });
    }
}
