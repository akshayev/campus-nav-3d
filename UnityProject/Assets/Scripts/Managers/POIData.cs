using System;
using UnityEngine;

// Plain data for one Point of Interest: an id, a display name, and a description.
//
// We used a plain [Serializable] class here instead of a ScriptableObject
// (Unity's other common way to hold reusable data) because:
//   - POIManager owns one hardcoded LIST of these right now (six entries) —
//     a List<POIData> shows up neatly as one block in the Inspector, where
//     six separate ScriptableObject ASSET FILES would be six extra files to
//     create and wire up by hand for no benefit yet.
//   - When Firestore data arrives (a later phase), each document naturally
//     maps to one POIData — Firebase's JSON deserializer builds a C# object
//     exactly like this one, so no redesign is needed, just a different
//     place the List<POIData> gets filled from (Firestore instead of the
//     Inspector).
// If POIs later need their own custom icon/audio clip/Editor tooling,
// ScriptableObjects would start to make more sense — not needed yet.
[Serializable]
public class POIData
{
    public string id;
    public string displayName;
    [TextArea(2, 5)] public string description;
}
