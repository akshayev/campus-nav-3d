using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

// Fetches the exteriorPOIs collection from Firestore over plain HTTPS — Firestore's own REST
// API — using Unity's built-in UnityWebRequest, instead of the official Firebase Unity SDK.
//
// Why not the SDK: it doesn't support WebGL (it's built on native code, which can't compile
// through to WebAssembly), and WebGL is this project's primary target per CLAUDE.md. The REST
// API is just normal HTTPS + JSON, so it works the same on every platform Unity can build for.
//
// If anything goes wrong here — no internet, Firestore down, a bad response — this script
// just logs a warning and leaves POIManager's hardcoded POI list exactly as it was. It only
// ever REPLACES that list on a confirmed, successful, non-empty fetch (see ReplacePOIList in
// POIManager.cs). That's what makes the hardcoded list a real offline fallback, not just a
// placeholder that breaks the moment Firebase is involved.
public class FirebaseDataManager : MonoBehaviour
{
    // Same singleton pattern as POIManager/PlayerMovementInput, so FloorTransitionManager
    // can call FetchRoomsForFloor() later without needing its own reference wired in the
    // Inspector — it just calls FirebaseDataManager.Instance.
    public static FirebaseDataManager Instance { get; private set; }

    [Tooltip("Firebase Console > Project settings > General > Your apps > Web app > projectId.")]
    [SerializeField] private string projectId = "cucek-campus-nav";

    [Tooltip("Firebase Console > Project settings > General > Your apps > Web app > apiKey. " +
        "This is NOT a secret — Firebase web/WebGL API keys are meant to be public. Security " +
        "comes from the Firestore rules (public read, no write), not from hiding this.")]
    [SerializeField] private string apiKey = "AIzaSyB1ArD-FIipXwo6uG5KB4Sm9zIlido183A";

    [Tooltip("Firestore collection name holding the exterior POI documents.")]
    [SerializeField] private string collectionName = "exteriorPOIs";

    // Filled by FetchFloors() on Start, same "replace on confirmed non-empty success" safety
    // as the POI list. Floor1 is a placeholder entry until the team's walkthrough/sketch of
    // the real floors exists (see knowledge base Section 3) — nothing here is final room data.
    public List<FloorData> CachedFloors { get; private set; } = new List<FloorData>
    {
        new FloorData { floorId = "FLOOR_1", floorNumber = 1, label = "Floor 1 (placeholder)", isGuaranteed = true, sceneName = "Floor1" },
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(FetchPOIs());
        StartCoroutine(FetchFloors());
    }

    private IEnumerator FetchPOIs()
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/{collectionName}?key={apiKey}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"FirebaseDataManager: fetch failed ({request.error}). Keeping hardcoded POI list.");
            yield break;
        }

        List<POIData> fetched;
        try
        {
            fetched = ParseFirestoreResponse(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FirebaseDataManager: could not parse Firestore response ({e.Message}). Keeping hardcoded POI list.");
            yield break;
        }

        if (fetched.Count == 0)
        {
            Debug.LogWarning("FirebaseDataManager: Firestore returned zero POIs (empty or missing collection). Keeping hardcoded POI list.");
            yield break;
        }

        if (POIManager.Instance != null)
        {
            POIManager.Instance.ReplacePOIList(fetched);
            Debug.Log($"FirebaseDataManager: loaded {fetched.Count} POI(s) from Firestore.");
        }
    }

    // Firestore's REST API wraps every field value in a type tag, e.g. a string field looks
    // like {"stringValue": "Main Building"} rather than just "Main Building" — that's what
    // makes this parsing look more involved than reading a plain JSON object.
    private static List<POIData> ParseFirestoreResponse(string json)
    {
        List<POIData> result = new List<POIData>();

        JObject root = JObject.Parse(json);
        JArray documents = root["documents"] as JArray;
        if (documents == null)
        {
            // Firestore returns a response with no "documents" key at all when the
            // collection is empty — that's not an error, just nothing to read yet.
            return result;
        }

        foreach (JToken doc in documents)
        {
            JToken fields = doc["fields"];
            string id = (string)fields?["id"]?["stringValue"];
            string displayName = (string)fields?["displayName"]?["stringValue"];
            string description = (string)fields?["description"]?["stringValue"];

            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            result.Add(new POIData
            {
                id = id,
                displayName = displayName,
                description = description,
            });
        }

        return result;
    }

    private IEnumerator FetchFloors()
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/floors?key={apiKey}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"FirebaseDataManager: floors fetch failed ({request.error}). Keeping placeholder floor list.");
            yield break;
        }

        List<FloorData> fetched;
        try
        {
            fetched = ParseFloorsResponse(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FirebaseDataManager: could not parse floors response ({e.Message}). Keeping placeholder floor list.");
            yield break;
        }

        if (fetched.Count == 0)
        {
            Debug.LogWarning("FirebaseDataManager: Firestore returned zero floors. Keeping placeholder floor list.");
            yield break;
        }

        CachedFloors = fetched;
        Debug.Log($"FirebaseDataManager: loaded {fetched.Count} floor(s) from Firestore.");
    }

    private static List<FloorData> ParseFloorsResponse(string json)
    {
        List<FloorData> result = new List<FloorData>();

        JObject root = JObject.Parse(json);
        JArray documents = root["documents"] as JArray;
        if (documents == null)
        {
            return result;
        }

        foreach (JToken doc in documents)
        {
            JToken fields = doc["fields"];
            string floorId = (string)fields?["floorId"]?["stringValue"];
            if (string.IsNullOrEmpty(floorId))
            {
                continue;
            }

            result.Add(new FloorData
            {
                floorId = floorId,
                // Firestore's REST API sends whole numbers as a STRING inside integerValue
                // (e.g. "1"), not a JSON number — int.Parse converts that back to a real int.
                floorNumber = int.TryParse((string)fields?["floorNumber"]?["integerValue"], out int n) ? n : 0,
                label = (string)fields?["label"]?["stringValue"],
                isGuaranteed = (bool?)fields?["isGuaranteed"]?["booleanValue"] ?? false,
                sceneName = (string)fields?["sceneName"]?["stringValue"],
            });
        }

        return result;
    }

    // Called on demand by FloorTransitionManager when the avatar enters a stairs/lift trigger
    // for a specific floor — unlike POIs/floors, we don't want to fetch every floor's rooms up
    // front, only the one the player is about to walk into (SDD 5.3: "fetches that floor's
    // /rooms ... conserve memory/bandwidth").
    public void FetchRoomsForFloor(string floorId, Action<List<RoomData>> onComplete)
    {
        StartCoroutine(FetchRoomsCoroutine(floorId, onComplete));
    }

    private IEnumerator FetchRoomsCoroutine(string floorId, Action<List<RoomData>> onComplete)
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/floors/{floorId}/rooms?key={apiKey}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        List<RoomData> fetched = new List<RoomData>();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"FirebaseDataManager: rooms fetch for '{floorId}' failed ({request.error}). No rooms will show on this floor.");
        }
        else
        {
            try
            {
                fetched = ParseRoomsResponse(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"FirebaseDataManager: could not parse rooms response for '{floorId}' ({e.Message}). No rooms will show on this floor.");
                fetched = new List<RoomData>();
            }
        }

        onComplete?.Invoke(fetched);
    }

    private static List<RoomData> ParseRoomsResponse(string json)
    {
        List<RoomData> result = new List<RoomData>();

        JObject root = JObject.Parse(json);
        JArray documents = root["documents"] as JArray;
        if (documents == null)
        {
            return result;
        }

        foreach (JToken doc in documents)
        {
            JToken fields = doc["fields"];
            string roomId = (string)fields?["roomId"]?["stringValue"];
            if (string.IsNullOrEmpty(roomId))
            {
                continue;
            }

            JToken positionFields = fields?["position"]?["mapValue"]?["fields"];
            float px = ParseFloatField(positionFields?["x"]);
            float py = ParseFloatField(positionFields?["y"]);
            float pz = ParseFloatField(positionFields?["z"]);

            float radius = ParseFloatField(fields?["poiRadius"]);

            result.Add(new RoomData
            {
                roomId = roomId,
                displayName = (string)fields?["name"]?["stringValue"],
                description = (string)fields?["description"]?["stringValue"],
                position = new Vector3(px, py, pz),
                poiRadius = radius > 0f ? radius : 2f,
            });
        }

        return result;
    }

    // Firestore can send a number as either doubleValue (a real JSON number) or integerValue
    // (a quoted string) depending on how it was typed in the Console — this reads whichever is
    // present so position/poiRadius parse correctly either way.
    private static float ParseFloatField(JToken fieldWrapper)
    {
        if (fieldWrapper == null)
        {
            return 0f;
        }

        if (fieldWrapper["doubleValue"] != null)
        {
            return (float)fieldWrapper["doubleValue"];
        }

        if (fieldWrapper["integerValue"] != null && float.TryParse((string)fieldWrapper["integerValue"], out float iv))
        {
            return iv;
        }

        return 0f;
    }
}
