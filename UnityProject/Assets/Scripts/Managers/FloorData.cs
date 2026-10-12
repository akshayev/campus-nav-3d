using System;
using UnityEngine;

// Plain data for one floor, matching the SDD's `floors` Firestore collection (Section 3.2).
//
// Same reasoning as POIData: a plain [Serializable] class, not a ScriptableObject, since this
// maps 1:1 onto a Firestore document and there's no need for per-floor Editor asset files yet.
[Serializable]
public class FloorData
{
    public string floorId;

    // 1-5, matches the building's physical floor number.
    public int floorNumber;

    [Tooltip("Shown on the floor indicator badge, e.g. \"Ground Floor\", \"IT Department Floor\".")]
    public string label;

    [Tooltip("True only for the one floor the team has committed to making fully detailed/walkable (see knowledge base Section 3).")]
    public bool isGuaranteed;

    [Tooltip("Name of the Unity scene to additively load for this floor, e.g. \"Floor1\". " +
        "The SDD docx calls this field 'navMeshSceneRef' from an earlier draft that assumed " +
        "NavMesh — the project later decided against NavMesh (CharacterController + colliders " +
        "instead, see knowledge base Section 4), so this is just a scene name, nothing more.")]
    public string sceneName;
}
