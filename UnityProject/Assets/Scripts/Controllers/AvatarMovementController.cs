using UnityEngine;

// Moves the avatar around using whatever direction PlayerMovementInput is currently reporting.
// This script doesn't know or care if that direction came from the joystick or the keyboard —
// that's the whole point of having a shared input source.
//
// Movement is CAMERA-RELATIVE: pushing "up" on the joystick (or W) moves the avatar in the
// direction the camera is looking, not along a fixed world axis. That's what players expect
// from a third-person game, and it keeps controls intuitive if the camera ever rotates.
//
// Requires a CharacterController component (Unity adds one automatically if it's missing,
// because of the [RequireComponent] line below). CharacterController is Unity's standard
// "move a capsule around the world without fighting physics" component — very common in
// beginner tutorials, which is why we're using it here instead of Rigidbody forces.
[RequireComponent(typeof(CharacterController))]
public class AvatarMovementController : MonoBehaviour
{
    [Tooltip("Movement speed in units per second.")]
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("How strongly gravity pulls the avatar down. Keeps it stuck to the ground plane.")]
    [SerializeField] private float gravity = -9.81f;

    [Tooltip("Fastest the avatar is allowed to fall, in units per second. Without this cap, " +
        "falling off an edge (there's no boundary/NavMesh yet) lets fall speed grow forever, " +
        "which can make a single big physics step behave unpredictably.")]
    [SerializeField] private float maxFallSpeed = 20f;

    [Tooltip("The camera whose facing direction defines 'forward'. Leave empty to use the scene's Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    // Horizontal speed this frame, in units per second (0 when standing still).
    // A later phase will feed this into the Animator to blend idle → walk → run.
    public float CurrentSpeed { get; private set; }

    private CharacterController controller;
    private float verticalVelocity;

    // The avatar model's Animator. Empty until AvatarSpawner creates the model and calls
    // SetAnimator — until then (e.g. with the plain capsule) we simply skip animation.
    private Animator animator;

    // A one-time snapshot of the camera's facing, taken at Awake. See the comment in Awake
    // for why we don't just read cameraTransform.forward/right fresh every frame.
    private Vector3 cameraForward;
    private Vector3 cameraRight;

    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // CampusCameraController re-aims the camera at us (LookAt) every frame, and its
        // position eases toward us with a slight delay (SmoothDamp). If we read the camera's
        // live rotation here, we'd get a feedback loop: we turn to face where the camera is
        // looking, which makes the camera re-aim at our new facing, which changes where we
        // turn next, and so on — in practice this makes movement drift and eventually stop
        // responding correctly. Our camera has a fixed offset and never orbits under player
        // control, so its starting direction is the only one that should ever matter — we
        // capture it once, here, before anything has had a chance to move.
        cameraForward = FlattenAndNormalize(cameraTransform.forward);
        cameraRight = FlattenAndNormalize(cameraTransform.right);
    }

    private static Vector3 FlattenAndNormalize(Vector3 vector)
    {
        vector.y = 0f;
        return vector.normalized;
    }

    private void Update()
    {
        Vector2 input = PlayerMovementInput.Instance != null
            ? PlayerMovementInput.Instance.MoveDirection
            : Vector2.zero;

        Vector3 moveDirection = GetCameraRelativeDirection(input);

        // CharacterController needs us to apply gravity ourselves. When grounded we push
        // gently downward (not zero) so isGrounded stays true instead of flickering.
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
        }

        Vector3 velocity = moveDirection * moveSpeed;
        CurrentSpeed = velocity.magnitude;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Face the direction we're moving, so the placeholder capsule (and later the real
        // avatar model) visibly turns instead of always facing the same way.
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.forward = moveDirection;
        }

        // "Speed" must match the parameter name in AvatarAnimator exactly (case-sensitive).
        if (animator != null)
        {
            animator.SetFloat("Speed", CurrentSpeed);
        }
    }

    // Turns a 2D stick direction (x = right, y = forward) into a flat 3D direction, using the
    // camera's ORIGINAL facing (captured once in Awake — see the comment there).
    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 direction = cameraForward * input.y + cameraRight * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }
}
