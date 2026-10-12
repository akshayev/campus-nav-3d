using System;
using UnityEngine;

// Plain data for one room, matching the SDD's `floors/{floorId}/rooms` Firestore subcollection
// (Section 3.3). One of these exists per room on whichever floor is currently loaded.
[Serializable]
public class RoomData
{
    public string roomId;
    public string displayName;
    [TextArea(2, 5)] public string description;

    [Tooltip("Position relative to the building origin (FloorTransitionManager.buildingOrigin, the front door) and the floor's own height, so y is normally 0. Where the room's trigger volume should sit.")]
    public Vector3 position;

    [Tooltip("Proximity radius (Unity units) at which the room's popup should trigger — same idea as POITriggerVolume's collider size, kept here so it can come from Firestore per room.")]
    public float poiRadius = 2f;
}
