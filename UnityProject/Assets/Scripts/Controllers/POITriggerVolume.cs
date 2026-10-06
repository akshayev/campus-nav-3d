using UnityEngine;

// Put this on an empty GameObject with a Box Collider (Is Trigger ticked).
// It doesn't know what a POI actually IS — no name, no description — it only
// knows its own poiId and watches for the Player walking in or out. POIManager
// (a separate script, one per scene) owns the actual POI data and decides what
// to show. Splitting it this way means every POI in the scene can share the
// exact same script; only the poiId field differs per object.
[RequireComponent(typeof(Collider))]
public class POITriggerVolume : MonoBehaviour
{
    [Tooltip("Must match an id in POIManager's list exactly (case-sensitive).")]
    [SerializeField] private string poiId;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (POIManager.Instance != null)
        {
            POIManager.Instance.ShowPOI(poiId);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (POIManager.Instance != null)
        {
            POIManager.Instance.HidePOI(poiId);
        }
    }
}
