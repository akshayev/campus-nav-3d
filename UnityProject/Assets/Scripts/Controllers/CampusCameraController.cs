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

    [Tooltip("Where the camera sits relative to the target, in world-space units (X = left/right, Y = up, Z = forward/back).")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);

    [Tooltip("How many seconds it roughly takes the camera to catch up. Smaller = snappier, larger = smoother/laggier.")]
    [SerializeField] private float followSmoothTime = 0.15f;

    [Tooltip("Which layers count as solid obstacles the camera should not clip through.")]
    [SerializeField] private LayerMask collisionMask = 1; // "Default" layer only, by default

    [Tooltip("Gap kept between the camera and any obstacle it detects, so it doesn't sit exactly on the surface.")]
    [SerializeField] private float collisionBuffer = 0.3f;

    // Internal state SmoothDamp uses to track velocity between frames. Don't touch this from other scripts.
    private Vector3 followVelocity;

    // LateUpdate runs after every other Update this frame, so the camera reacts to where the
    // avatar just moved to, instead of one frame behind it.
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        smoothedPosition = ResolveCameraCollision(target.position, smoothedPosition);

        transform.position = smoothedPosition;

        // Look slightly above the target's feet (roughly chest height) rather than straight
        // at the ground pivot, so the framing feels natural.
        transform.LookAt(target.position + Vector3.up * 1f);
    }

    // If something solid sits between the target and where the camera wants to be, move the
    // camera to just in front of that obstacle instead of letting it pass through it.
    private Vector3 ResolveCameraCollision(Vector3 fromTarget, Vector3 desiredCameraPosition)
    {
        Vector3 direction = desiredCameraPosition - fromTarget;
        float desiredDistance = direction.magnitude;

        if (desiredDistance < 0.001f)
        {
            return desiredCameraPosition;
        }

        if (Physics.Raycast(fromTarget, direction.normalized, out RaycastHit hit, desiredDistance, collisionMask))
        {
            float safeDistance = Mathf.Max(hit.distance - collisionBuffer, 0.1f);
            return fromTarget + direction.normalized * safeDistance;
        }

        return desiredCameraPosition;
    }
}
