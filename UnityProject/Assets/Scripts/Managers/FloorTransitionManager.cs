using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lives in the Exterior scene (which is never unloaded) and handles moving the avatar between
// Exterior and whichever floor is currently loaded on top of it, per the SDD's additive-scene
// design (Section 5.1/5.3): only one floor sub-scene is ever loaded at a time, kept separate
// from Exterior so WebGL build size/memory stay reasonable.
public class FloorTransitionManager : MonoBehaviour
{
    public static FloorTransitionManager Instance { get; private set; }

    [Tooltip("The avatar's Transform — this script moves it to the right spot when entering/exiting a floor.")]
    [SerializeField] private Transform player;

    [Tooltip("Optional: the floor indicator badge text (Step 6). Safe to leave empty for now.")]
    [SerializeField] private TMPro.TextMeshProUGUI floorIndicatorText;

    // Name of the currently-loaded floor scene, or null if we're outside (no floor loaded).
    // Used as a simple guard so walking into a stairs trigger twice in a row doesn't try to
    // load the same scene twice.
    private string currentFloorScene;

    // The ids of room popups we added for the current floor, so we can clean them out of
    // POIManager's list again when the player leaves (see ClearRoomPOIs below).
    private List<string> currentRoomIds = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void EnterFloor(string floorId, Vector3 spawnPosition)
    {
        if (currentFloorScene != null)
        {
            // Already on a floor — ignore a second "enter" trigger instead of stacking loads.
            return;
        }

        FloorData floor = FirebaseDataManager.Instance.CachedFloors.FirstOrDefault(f => f.floorId == floorId);
        if (floor == null || string.IsNullOrEmpty(floor.sceneName))
        {
            Debug.LogWarning($"FloorTransitionManager: no floor data (or no scene name) found for floorId '{floorId}'.");
            return;
        }

        StartCoroutine(LoadFloorRoutine(floor, spawnPosition));
    }

    private IEnumerator LoadFloorRoutine(FloorData floor, Vector3 spawnPosition)
    {
        yield return SceneManager.LoadSceneAsync(floor.sceneName, LoadSceneMode.Additive);

        currentFloorScene = floor.sceneName;

        if (player != null)
        {
            player.position = spawnPosition;
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
            Debug.Log($"FloorTransitionManager: loaded {rooms.Count} room(s) for '{floor.floorId}'.");
        });
    }

    public void ExitFloor(Vector3 spawnPosition)
    {
        if (currentFloorScene == null)
        {
            // Already outside — ignore a stray "exit" trigger.
            return;
        }

        StartCoroutine(UnloadFloorRoutine(spawnPosition));
    }

    private IEnumerator UnloadFloorRoutine(Vector3 spawnPosition)
    {
        string sceneToUnload = currentFloorScene;
        currentFloorScene = null;

        if (POIManager.Instance != null)
        {
            POIManager.Instance.ClearRoomPOIs(currentRoomIds);
        }
        currentRoomIds.Clear();

        yield return SceneManager.UnloadSceneAsync(sceneToUnload);

        if (player != null)
        {
            player.position = spawnPosition;
        }

        if (floorIndicatorText != null)
        {
            floorIndicatorText.text = "Outside";
        }

        Debug.Log($"FloorTransitionManager: exited '{sceneToUnload}', avatar moved to {spawnPosition}.");
    }
}
