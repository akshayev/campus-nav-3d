using UnityEngine;

// Third-person follow camera, per SDD 5.2.
//
// Two ideas make this feel good instead of janky:
//
// 1. SMOOTH FOLLOW: instead of snapping the camera straight to "target position + offset"
//    every frame (which looks robotic and jittery), we ease toward it using
//    Vector3.SmoothDamp. Think of it like the camera being attached to the avatar with a
//    soft spring instead of a rigid rod — fast when far behind, gentle as it catches up,
//    no bouncing past the target.
//
// 2. COLLISION AWARENESS: every frame we cast a ray from the avatar toward where the
//    camera WANTS to sit. If the ground (or later, a wall/slope) is in the way, we pull the
//    camera in front of it instead of letting it clip through the geometry.
//
// The avatar is on its own "Player" layer specifically so this script's raycast can't
// accidentally treat the avatar's own body as an obstacle.
public class CampusCameraController : MonoBehaviour
{
    [Tooltip("The object the camera follows. Drag the avatar here.")]
    [SerializeField] private Transform target;

    [Tooltip("Where the camera sits relative to the target (X = left/right, Y = up, Z = forward/back). " +
        "With 'Rotate With Target' on, this is measured from the way the avatar faces, so -8 means 8 units BEHIND it.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);

    [Tooltip("On: the camera swings round to stay behind the avatar when it turns (turn left = camera turns left). " +
        "Off: the camera keeps looking the same compass direction.")]
    [SerializeField] private bool rotateWithTarget = true;

    [Tooltip("How many seconds the camera takes to swing round behind the avatar after it turns. Smaller = snappier.")]
    [SerializeField] private float turnFollowSmoothTime = 0.25f;

    [Tooltip("How many seconds it roughly takes the camera to catch up. Smaller = snappier, larger = smoother/laggier.")]
    [SerializeField] private float followSmoothTime = 0.15f;

    [Tooltip("Which layers count as solid obstacles the camera should not clip through.")]
    [SerializeField] private LayerMask collisionMask = 1; // "Default" layer only, by default

    [Tooltip("Gap kept between the camera and any obstacle it detects, so it doesn't sit exactly on the surface.")]
    [SerializeField] private float collisionBuffer = 0.3f;

    [Header("Indoor view")]
    [Tooltip("Where the camera sits INSIDE the building: low and close behind the avatar, so you can see the ceiling and walls. " +
        "Used automatically when you go into the building (unless the overhead view is switched on).")]
    [SerializeField] private Vector3 indoorOffset = new Vector3(0f, 1.5f, -3.2f);

    [Tooltip("Indoors the camera must not pass through walls or furniture. Those are on the 'Ignore Raycast' layer (layer 2), " +
        "so this mask adds it to the normal 'Default' layer.")]
    [SerializeField] private LayerMask indoorCollisionMask = (1 << 0) | (1 << 2);

    [Tooltip("How many seconds the camera takes to swoop between the outdoor and indoor positions.")]
    [SerializeField] private float offsetBlendTime = 0.5f;

    [Tooltip("Optional: the text on the on-screen View button, updated to say Close/Overhead.")]
    [SerializeField] private TMPro.TextMeshProUGUI viewButtonLabel;

    private bool isIndoor;
    private bool overheadViewOn;
    private Vector3 currentOffset;
    private Vector3 offsetVelocity;
    private bool offsetInitialised;

    // Called by BuildingShell when the avatar goes into / comes out of the building.
    public void SetIndoor(bool indoor)
    {
        isIndoor = indoor;
    }

    // Wired to the on-screen View button: switches between the close indoor camera and the
    // high overhead one (handy for seeing a whole floor at once, but the ceiling can't be seen from above).
    public void ToggleOverheadView()
    {
        overheadViewOn = !overheadViewOn;
        UpdateViewLabel();
    }

    private void UpdateViewLabel()
    {
        if (viewButtonLabel != null)
        {
            viewButtonLabel.text = overheadViewOn ? "View: Overhead" : "View: Close";
        }
    }

    private void Awake()
    {
        UpdateViewLabel();
    }

    // Internal state SmoothDamp uses to track velocity between frames. Don't touch this from other scripts.
    private Vector3 followVelocity;

    // The direction (left/right angle) the camera is currently placed behind, and its turning speed.
    private float currentYaw;
    private float yawVelocity;
    private bool yawInitialised;

    // LateUpdate runs after every other Update this frame, so the camera reacts to where the
    // avatar just moved to, instead of one frame behind it.
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Which way the camera should be "behind". Smoothed so a sharp turn becomes a gentle swing
        // instead of a snap (SmoothDampAngle handles the wrap-around from 359 to 0 degrees).
        if (rotateWithTarget)
        {
            if (!yawInitialised)
            {
                currentYaw = target.eulerAngles.y;
                yawInitialised = true;
            }
            currentYaw = Mathf.SmoothDampAngle(currentYaw, target.eulerAngles.y, ref yawVelocity, turnFollowSmoothTime);
        }
        else
        {
            currentYaw = 0f;
        }

        // Close indoor camera when inside the building, the normal high one otherwise. The offset is
        // blended so going through a door is a smooth swoop, not a jump.
        bool useIndoorView = isIndoor && !overheadViewOn;
        Vector3 wantedOffset = useIndoorView ? indoorOffset : offset;
        if (!offsetInitialised)
        {
            currentOffset = wantedOffset;
            offsetInitialised = true;
        }
        currentOffset = Vector3.SmoothDamp(currentOffset, wantedOffset, ref offsetVelocity, offsetBlendTime);

        Vector3 desiredPosition = target.position + Quaternion.Euler(0f, currentYaw, 0f) * currentOffset;

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        smoothedPosition = ResolveCameraCollision(target.position, smoothedPosition, useIndoorView ? indoorCollisionMask : collisionMask);

        transform.position = smoothedPosition;

        // Look slightly above the target's feet (roughly chest height) rather than straight
        // at the ground pivot, so the framing feels natural.
        transform.LookAt(target.position + Vector3.up * 1f);
    }

    // If something solid sits between the target and where the camera wants to be, move the
    // camera to just in front of that obstacle instead of letting it pass through it.
    // Trigger colliders (stairs, room and POI volumes) are invisible and walk-through, so they
    // are ignored: otherwise a trigger box between us and the camera pulls the camera in close.
    private Vector3 ResolveCameraCollision(Vector3 fromTarget, Vector3 desiredCameraPosition, LayerMask mask)
    {
        Vector3 direction = desiredCameraPosition - fromTarget;
        float desiredDistance = direction.magnitude;

        if (desiredDistance < 0.001f)
        {
            return desiredCameraPosition;
        }

        if (Physics.Raycast(fromTarget, direction.normalized, out RaycastHit hit, desiredDistance, mask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(hit.distance - collisionBuffer, 0.1f);
            return fromTarget + direction.normalized * safeDistance;
        }

        return desiredCameraPosition;
    }
}
