using System.Collections.Generic;
using UnityEngine;

// One of these lives in the Exterior scene. Every POITriggerVolume talks to it
// through POIManager.Instance — same singleton pattern as PlayerMovementInput,
// so it's consistent with a pattern this team has already seen.
//
// Right now the six POIs below are hardcoded in the Inspector. Later, a
// FirebaseDataManager can replace FillHardcodedData() with a Firestore read
// that fills the same pois list — nothing else in this script, or in
// POITriggerVolume, needs to change.
public class POIManager : MonoBehaviour
{
    public static POIManager Instance { get; private set; }

    [Tooltip("All POIs this scene knows about. Each POITriggerVolume's poiId must match one 'id' here.")]
    [SerializeField] private List<POIData> pois = new List<POIData>();

    [Tooltip("The popup panel's title text.")]
    [SerializeField] private TMPro.TextMeshProUGUI popupTitle;

    [Tooltip("The popup panel's description text.")]
    [SerializeField] private TMPro.TextMeshProUGUI popupDescription;

    [Tooltip("The whole popup panel GameObject — shown on enter, hidden on exit.")]
    [SerializeField] private GameObject popupPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (pois.Count == 0)
        {
            FillHardcodedData();
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    // Six POIs, filled in code so there's always a known-good fallback even if
    // the Inspector list is accidentally left empty. If you DO fill the list
    // in the Inspector instead, this method is simply skipped (see Awake).
    private void FillHardcodedData()
    {
        pois.Add(new POIData { id = "main_building", displayName = "Main Building", description = "The main B.Tech building — five storeys, home to most departments." });
        pois.Add(new POIData { id = "mca_block", displayName = "MCA Block", description = "The MCA department block." });
        pois.Add(new POIData { id = "bike_shed", displayName = "Bike Shed", description = "Covered parking for bikes." });
        pois.Add(new POIData { id = "car_shed", displayName = "Car Shed", description = "Covered parking for cars." });
        pois.Add(new POIData { id = "canteen", displayName = "Canteen", description = "Campus canteen." });
        pois.Add(new POIData { id = "badminton_court", displayName = "Indoor Badminton Court", description = "Indoor badminton court." });
    }

    public void ShowPOI(string poiId)
    {
        POIData data = pois.Find(p => p.id == poiId);
        if (data == null)
        {
            Debug.LogWarning($"POIManager: no POI data found for id '{poiId}'. Check the poiId on the trigger volume matches a POIData id exactly.");
            return;
        }

        if (popupTitle != null) popupTitle.text = data.displayName;
        if (popupDescription != null) popupDescription.text = data.description;
        if (popupPanel != null) popupPanel.SetActive(true);
    }

    public void HidePOI(string poiId)
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    // Called by FirebaseDataManager once a Firestore fetch succeeds. We only ever REPLACE
    // the list on a confirmed, non-empty result — never clear it — so a failed or slow
    // fetch simply leaves the hardcoded data in place instead of breaking the POI popups.
    public void ReplacePOIList(List<POIData> newPois)
    {
        if (newPois == null || newPois.Count == 0)
        {
            return;
        }
        pois = newPois;
    }
}
