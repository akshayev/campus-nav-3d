using UnityEngine;

// Same job as POITriggerVolume (watch for the Player walking in/out, tell POIManager to
// show/hide the popup) but for rooms. The difference is POITriggerVolume sits on a GameObject
// hand-placed in the Editor with poiId set in the Inspector; this one is added by
// FloorTransitionManager at runtime onto a Box Collider it generates from Firestore room data
// (position + poiRadius), since there's no real building geometry to hand-place room triggers
// on yet. roomId is public (not [SerializeField]) so FloorTransitionManager can set it in code
// right after AddComponent.
[RequireComponent(typeof(Collider))]
public class RoomTriggerVolume : MonoBehaviour
{
    public string roomId;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (POIManager.Instance != null)
        {
            POIManager.Instance.ShowPOI(roomId);
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
            POIManager.Instance.HidePOI(roomId);
        }
    }
}
