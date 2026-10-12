using UnityEngine;

// The main building as seen from OUTSIDE is a solid pink block with a red roof. That same block
// would hide everything once you walk in (the camera would be looking at a wall), so this script
// swaps between two looks:
//   - Outside:  the solid shell (pink walls, windows, red roof). Its walls are solid, so you
//               can't walk through them, only in through the red door marker.
//   - Inside:   a see-through "cutaway" box, so you can still see the floor and the avatar.
//
// FloorTransitionManager calls SetInside(true) when you go into the building and
// SetInside(false) when you come back out. This is a stand-in until the real Blender model
// arrives; when it does, this script can swap that model's walls the same way.
public class BuildingShell : MonoBehaviour
{
    [Tooltip("The solid pink building (walls, roof). Shown outside, hidden inside. Its collider goes with it, so the avatar isn't pushed around by walls while standing on a floor.")]
    [SerializeField] private GameObject outsideShell;

    [Tooltip("The see-through box shown while you're inside the building.")]
    [SerializeField] private GameObject insideCutaway;

    [Tooltip("Optional: the camera, told to use its close indoor view while the avatar is inside the building.")]
    [SerializeField] private CampusCameraController cameraController;

    private void Awake()
    {
        SetInside(false);
    }

    public void SetInside(bool inside)
    {
        if (outsideShell != null) outsideShell.SetActive(!inside);
        if (insideCutaway != null) insideCutaway.SetActive(inside);
        if (cameraController != null) cameraController.SetIndoor(inside);
    }
}
