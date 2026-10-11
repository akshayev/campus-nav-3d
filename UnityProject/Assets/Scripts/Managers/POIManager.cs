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

    [Tooltip("Optional photo shown on the right of the popup. Safe to leave empty (popups just have no photo).")]
    [SerializeField] private UnityEngine.UI.Image popupPhoto;

    // Photos live in Assets/Resources/RoomPhotos/ and are matched purely by file name: the photo
    // for the room/POI with id "room_classroom2" is "room_classroom2.jpeg". No photo with that
    // name = the popup looks exactly as it did before. (Firebase Storage isn't on the free Spark
    // plan, so photos ship inside the build instead of being downloaded.)
    private const string PhotoFolder = "RoomPhotos/";

    // Rooms of the same kind look alike (every classroom, every faculty room), so one shared photo
    // covers them all: if a room has no photo of its own and its id contains one of these words,
    // it uses that shared photo. A photo named exactly after the room id always wins.
    private static readonly KeyValuePair<string, string>[] SharedPhotos =
    {
        new KeyValuePair<string, string>("classroom", "shared_classroom"),
        new KeyValuePair<string, string>("faculty", "shared_faculty"),
    };

    // The photo is scaled to fit inside this box (keeping its own shape, so a portrait room photo
    // and a landscape building photo both look right); the text gets whatever width is left.
    private const float PhotoMaxWidth = 260f;
    private const float PhotoMaxHeight = 240f;
    private const float PanelWidth = 800f;
    private const float PanelPadding = 20f;

    // More than one photo for the same id is a small gallery: "main_building", "main_building_2",
    // "main_building_3", ... (numbering stops at the first missing one). It advances by itself
    // every few seconds, and tapping the photo jumps to the next one.
    private const float GallerySecondsPerPhoto = 3f;
    private readonly List<Sprite> galleryPhotos = new List<Sprite>();
    private int galleryIndex;
    private float nextGalleryAdvanceTime;

    private Vector2 titleSize, titlePos, descriptionSize, descriptionPos;

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

        // Remember the text boxes' original layout so we can restore it when a popup has no photo.
        if (popupTitle != null)
        {
            RectTransform r = popupTitle.rectTransform;
            titleSize = r.sizeDelta;
            titlePos = r.anchoredPosition;
        }
        if (popupDescription != null)
        {
            RectTransform r = popupDescription.rectTransform;
            descriptionSize = r.sizeDelta;
            descriptionPos = r.anchoredPosition;
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    // Finds the photo(s) for this id and shows the first one; moves the text aside to make room,
    // or hides the photo slot and puts the text back if there is none.
    private void ApplyPhoto(string poiId)
    {
        galleryPhotos.Clear();
        galleryIndex = 0;

        Sprite first = Resources.Load<Sprite>(PhotoFolder + poiId);
        if (first == null)
        {
            foreach (KeyValuePair<string, string> shared in SharedPhotos)
            {
                if (poiId.Contains(shared.Key))
                {
                    first = Resources.Load<Sprite>(PhotoFolder + shared.Value);
                    break;
                }
            }
        }

        if (first != null)
        {
            galleryPhotos.Add(first);

            // Extra gallery photos: <id>_2, <id>_3, ... (only for an exact-id photo).
            for (int n = 2; ; n++)
            {
                Sprite extra = Resources.Load<Sprite>(PhotoFolder + poiId + "_" + n);
                if (extra == null)
                {
                    break;
                }
                galleryPhotos.Add(extra);
            }
        }

        ShowGalleryPhoto();
    }

    // Puts the current gallery photo in the slot (or hides the slot) and lays the text out
    // around it. Photos can differ in shape, so this reruns on every photo change.
    private void ShowGalleryPhoto()
    {
        Sprite photo = galleryPhotos.Count > 0 ? galleryPhotos[galleryIndex] : null;
        bool hasPhoto = photo != null && popupPhoto != null;
        nextGalleryAdvanceTime = Time.unscaledTime + GallerySecondsPerPhoto;

        float photoWidth = 0f;
        if (popupPhoto != null)
        {
            popupPhoto.sprite = photo;
            popupPhoto.preserveAspect = true;
            popupPhoto.gameObject.SetActive(hasPhoto);

            if (hasPhoto)
            {
                float aspect = photo.rect.width / photo.rect.height;
                photoWidth = Mathf.Min(PhotoMaxWidth, PhotoMaxHeight * aspect);
                popupPhoto.rectTransform.sizeDelta = new Vector2(photoWidth, photoWidth / aspect);
            }
        }

        // Text fills the panel to the left of the photo (or the whole panel when there is none).
        float textWidth = hasPhoto ? PanelWidth - photoWidth - PanelPadding * 3f : 0f;
        float textCenterX = -PanelWidth / 2f + PanelPadding + textWidth / 2f;

        if (popupTitle != null)
        {
            RectTransform r = popupTitle.rectTransform;
            r.sizeDelta = hasPhoto ? new Vector2(textWidth, titleSize.y) : titleSize;
            r.anchoredPosition = hasPhoto ? new Vector2(textCenterX, titlePos.y) : titlePos;
        }
        if (popupDescription != null)
        {
            RectTransform r = popupDescription.rectTransform;
            r.sizeDelta = hasPhoto ? new Vector2(textWidth, descriptionSize.y) : descriptionSize;
            r.anchoredPosition = hasPhoto ? new Vector2(textCenterX, descriptionPos.y) : descriptionPos;
        }
    }

    private void Update()
    {
        if (galleryPhotos.Count > 1 && popupPanel != null && popupPanel.activeInHierarchy
            && Time.unscaledTime >= nextGalleryAdvanceTime)
        {
            NextPhoto();
        }
    }

    // Also wired to the photo's Button, so tapping the photo goes to the next one.
    public void NextPhoto()
    {
        if (galleryPhotos.Count < 2)
        {
            return;
        }

        galleryIndex = (galleryIndex + 1) % galleryPhotos.Count;
        ShowGalleryPhoto();
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
        ApplyPhoto(poiId);
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

    // Called by FloorTransitionManager after it loads a floor and fetches that floor's rooms.
    // Rooms are ADDED on top of the existing (exterior) POI list, not a replacement, so a room
    // trigger and an exterior POI trigger can both show a popup through the same panel.
    public void AddRoomPOIs(List<RoomData> rooms)
    {
        if (rooms == null)
        {
            return;
        }

        foreach (RoomData room in rooms)
        {
            pois.Add(new POIData
            {
                id = room.roomId,
                displayName = room.displayName,
                description = room.description,
            });
        }
    }

    // Called by FloorTransitionManager when the player leaves a floor, so a room from a floor
    // that's no longer loaded can't be found again by a stale id (its trigger volume is gone
    // too, destroyed along with the floor scene, but this keeps the list itself clean).
    public void ClearRoomPOIs(List<string> roomIds)
    {
        if (roomIds == null || roomIds.Count == 0)
        {
            return;
        }

        pois.RemoveAll(p => roomIds.Contains(p.id));
    }
}
