using UnityEngine;

// Sits on the shed's EXT03_Model object. The camera follows the avatar from above, so a solid
// roof would hide the avatar the moment it walks under the shed. This script solves that by
// switching the roof OFF while the Player is inside the shed's footprint (plus a small margin)
// and back ON when the Player is outside again.
//
// It is deliberately cheap: once per frame it does one position comparison, and it only touches
// the roof's renderers on the frame the answer actually changes (not every frame).
//
// The footprint size is filled in by the builder script (Assets/Editor/SiteBuilders/EXT03_BikeShed.cs)
// from its Length and Depth constants, so resizing the shed there also resizes this zone.
public class ShedRoofFader : MonoBehaviour
{
    [Tooltip("The 'Roof' group whose renderers are switched off while the Player is under the shed.")]
    [SerializeField] private Transform roofGroup;

    [Tooltip("Half of the shed's footprint (x = half the length, y = half the depth), in metres. Set by the builder script.")]
    [SerializeField] private Vector2 footprintHalfSize = new Vector2(10.5f, 4f);

    [Tooltip("Extra metres around the footprint where the roof is already hidden, so it vanishes just before the avatar walks in.")]
    [SerializeField] private float margin = 1f;

    [Tooltip("Optional: the Player. Leave empty and it is found automatically by the 'Player' tag.")]
    [SerializeField] private Transform player;

    private Renderer[] roofRenderers;
    private bool roofVisible = true;

    private void Start()
    {
        if (roofGroup != null)
        {
            roofRenderers = roofGroup.GetComponentsInChildren<Renderer>();
        }
        FindPlayerIfNeeded();
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

        // Express the Player's position in the shed's own space, so the check still works
        // if the shed is ever moved or turned in the scene.
        Vector3 local = transform.InverseTransformPoint(player.position);
        bool inside = Mathf.Abs(local.x) <= footprintHalfSize.x + margin
                   && Mathf.Abs(local.z) <= footprintHalfSize.y + margin;

        bool shouldBeVisible = !inside;
        if (shouldBeVisible == roofVisible)
        {
            return;
        }

        roofVisible = shouldBeVisible;
        foreach (Renderer roofRenderer in roofRenderers)
        {
            roofRenderer.enabled = roofVisible;
        }
    }
}
