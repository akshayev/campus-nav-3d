using UnityEngine;

// Sits on the shed's EXT03_Model object. The camera follows the avatar from above and behind, so
// the shed's overhead parts (the roof, and the fascia band and side beams hanging from its edges)
// would hide the avatar in two situations:
//   1. the avatar is UNDER the roof, or
//   2. the avatar is just outside, but the camera is still above the shed, so these parts are
//      between the camera and the avatar. (The fascia and beams matter as much as the roof: seen
//      from above and behind they cover an avatar standing about 2 m outside the shed.)
// This script switches them all OFF in both cases and back ON when neither applies:
//   - Case 1: the Player is inside the shed's footprint plus a small margin.
//   - Case 2: the straight line from the Main Camera to the Player passes through the bounding box
//     of those overhead parts.
//
// It is deliberately cheap: once per frame it does a few comparisons and one box test (no memory
// is allocated), and it only touches the renderers on the frame the answer changes.
//
// Anti-flicker: the roof hides IMMEDIATELY (the avatar must never be hidden), but it only comes back
// after nothing has wanted it hidden for a short time (showDelaySeconds), so it can't blink on and off
// when the camera line brushes the edge of the box.
//
// The footprint size is filled in by the builder script (Assets/Editor/SiteBuilders/EXT03_BikeShed.cs, or
// EXT04_CarShed.cs for the car shed canopy) from its size constants, so resizing the shed there also
// resizes this zone. The overhead
// parts' bounding box is measured once in Start(), so the shed is assumed not to move while the game runs.
public class ShedRoofFader : MonoBehaviour
{
    [Tooltip("The 'Roof' group whose renderers are switched off while the roof is in the way.")]
    [SerializeField] private Transform roofGroup;

    [Tooltip("Other groups that hang from the roof (the builder fills in Fascia and Beams). They are switched off and on together with the roof. Empty = roof only.")]
    [SerializeField] private Transform[] alsoHide;

    [Tooltip("Half of the shed's footprint (x = half the length, y = half the depth), in metres. Set by the builder script.")]
    [SerializeField] private Vector2 footprintHalfSize = new Vector2(10.5f, 4f);

    [Tooltip("Where the footprint's centre is, in metres from this object (x, then z). (0, 0) = centred on this object, which is right for the bike shed. A shed that is not centred on its object (the car shed canopy) gets its offset from the builder script.")]
    [SerializeField] private Vector2 footprintCenter = Vector2.zero;

    [Tooltip("Extra metres around the footprint where the roof is already hidden, so it vanishes just before the avatar walks in.")]
    [SerializeField] private float margin = 1f;

    [Tooltip("Optional: the Player. Leave empty and it is found automatically by the 'Player' tag.")]
    [SerializeField] private Transform player;

    [Tooltip("Optional: the camera to keep clear of the roof. Leave empty to use the scene's Main Camera.")]
    [SerializeField] private Camera viewCamera;

    [Tooltip("The overhead parts' bounding box is made this much taller (metres, split top and bottom) before the camera line is tested against it.")]
    [SerializeField] private float boundsHeightPadding = 0.4f;

    [Tooltip("The roof only comes back after nothing has wanted it hidden for this many seconds (stops flicker).")]
    [SerializeField] private float showDelaySeconds = 0.3f;

    private Renderer[] roofRenderers;   // roof + the 'alsoHide' groups: everything this script switches
    private Bounds roofBounds;
    private bool roofVisible = true;
    private float wantedVisibleSince = -1f;

    private void Start()
    {
        if (roofGroup != null)
        {
            var found = new System.Collections.Generic.List<Renderer>(roofGroup.GetComponentsInChildren<Renderer>());
            if (alsoHide != null)
            {
                foreach (Transform group in alsoHide)
                {
                    if (group != null)
                    {
                        found.AddRange(group.GetComponentsInChildren<Renderer>());
                    }
                }
            }

            if (found.Count > 0)
            {
                roofRenderers = found.ToArray();
                roofBounds = roofRenderers[0].bounds;
                for (int i = 1; i < roofRenderers.Length; i++)
                {
                    roofBounds.Encapsulate(roofRenderers[i].bounds);
                }
                roofBounds.Expand(new Vector3(0f, boundsHeightPadding, 0f));
            }
        }

        FindPlayerIfNeeded();
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            return;
        }

        GameObject found = GameObject.FindWithTag("Player");
        if (found != null)
        {
            player = found.transform;
        }
    }

    private void Update()
    {
        if (roofRenderers == null)
        {
            return;
        }

        if (player == null)
        {
            // The Player normally exists from the first frame; this just keeps trying if it does not.
            FindPlayerIfNeeded();
            return;
        }

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        bool shouldHide = PlayerIsUnderShed() || CameraLineBlockedByRoof();

        if (shouldHide)
        {
            wantedVisibleSince = -1f;
            if (roofVisible)
            {
                SetRoofVisible(false);
            }
            return;
        }

        // Nothing wants the roof hidden. Bring it back only once that has held for a moment.
        if (roofVisible)
        {
            return;
        }

        if (wantedVisibleSince < 0f)
        {
            wantedVisibleSince = Time.time;
        }
        else if (Time.time - wantedVisibleSince >= showDelaySeconds)
        {
            SetRoofVisible(true);
        }
    }

    // Case 1: is the Player inside the footprint (plus margin)? Done in the shed's own space, so it
    // still works if the shed is ever moved or turned in the scene.
    private bool PlayerIsUnderShed()
    {
        Vector3 local = transform.InverseTransformPoint(player.position);
        return Mathf.Abs(local.x - footprintCenter.x) <= footprintHalfSize.x + margin
            && Mathf.Abs(local.z - footprintCenter.y) <= footprintHalfSize.y + margin;
    }

    // Case 2: does the segment from the camera to the Player pass through the overhead parts' box?
    private bool CameraLineBlockedByRoof()
    {
        if (viewCamera == null)
        {
            return false;
        }

        Vector3 from = viewCamera.transform.position;
        Vector3 toPlayer = player.position - from;
        float distance = toPlayer.magnitude;
        if (distance < 0.01f)
        {
            return false;
        }

        // A camera that is itself inside the box is certainly looking through the roof.
        if (roofBounds.Contains(from))
        {
            return true;
        }

        // IntersectRay gives the distance along the ray to the box; the roof only matters if that is
        // before the Player (a box behind the Player is not in the way).
        float hitDistance;
        return roofBounds.IntersectRay(new Ray(from, toPlayer / distance), out hitDistance) && hitDistance <= distance;
    }

    private void SetRoofVisible(bool visible)
    {
        roofVisible = visible;
        for (int i = 0; i < roofRenderers.Length; i++)
        {
            roofRenderers[i].enabled = visible;
        }
    }
}
