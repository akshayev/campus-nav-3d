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
    [Tooltip("Firebase Console > Project settings > General > Your apps > Web app > projectId.")]
    [SerializeField] private string projectId = "cucek-campus-nav";

    [Tooltip("Firebase Console > Project settings > General > Your apps > Web app > apiKey. " +
        "This is NOT a secret — Firebase web/WebGL API keys are meant to be public. Security " +
        "comes from the Firestore rules (public read, no write), not from hiding this.")]
    [SerializeField] private string apiKey = "AIzaSyB1ArD-FIipXwo6uG5KB4Sm9zIlido183A";

    [Tooltip("Firestore collection name holding the exterior POI documents.")]
    [SerializeField] private string collectionName = "exteriorPOIs";

    private void Start()
    {
        StartCoroutine(FetchPOIs());
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
}
