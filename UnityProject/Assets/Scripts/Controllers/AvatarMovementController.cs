using UnityEngine;

// Moves the avatar around using whatever direction PlayerMovementInput is currently reporting.
// This script doesn't know or care if that direction came from the joystick or the keyboard —
// that's the whole point of having a shared input source.
//
// There are two ways to steer (pick one with "Control Mode" in the Inspector):
//
//   TurnAndWalk (default): LEFT/RIGHT on the joystick (or A/D) TURNS the avatar on the spot, and
//     UP/DOWN (or W/S) walks it forward/backward along the way it is facing. The camera
//     (CampusCameraController) swings round to stay behind the avatar, so turning left turns the
//     camera left too, and the same for every other direction.
//
//   CameraRelative: pushing "up" moves the avatar in the direction the camera is looking, not
//     along a fixed world axis. The camera never turns in this mode.
//
// Requires a CharacterController component (Unity adds one automatically if it's missing,
// because of the [RequireComponent] line below). CharacterController is Unity's standard
// "move a capsule around the world without fighting physics" component — very common in
// beginner tutorials, which is why we're using it here instead of Rigidbody forces.
[RequireComponent(typeof(CharacterController))]
public class AvatarMovementController : MonoBehaviour
{
    public enum ControlMode { TurnAndWalk, CameraRelative }

    [Tooltip("TurnAndWalk: left/right turns the avatar (and the camera follows round behind it), up/down walks. " +
        "CameraRelative: the direction pushed is the direction walked, with a camera that never turns.")]
    [SerializeField] private ControlMode controlMode = ControlMode.TurnAndWalk;

    [Tooltip("Normal walking speed in units (metres) per second. Keep it under 2.5: the Animator " +
        "switches to the Running animation above that.")]
    [SerializeField] private float walkSpeed = 2f;

    [Tooltip("Running speed, used while Shift is held or the on-screen Run button is switched on. " +
        "Must be above 2.5 so the Animator plays the Running animation.")]
    [SerializeField] private float runSpeed = 4f;

    [Header("Animation")]
    [Tooltip("The speed (m/s) the Walking animation looks right at. The animation plays faster/slower " +
        "than normal so the feet match how fast the avatar really moves. If the feet seem to slide, " +
        "nudge this: lower = feet step faster, higher = slower.")]
    [SerializeField] private float walkAnimationReferenceSpeed = 1.6f;

    [Tooltip("Same idea for the Running animation (m/s).")]
    [SerializeField] private float runAnimationReferenceSpeed = 4f;

    [Tooltip("TurnAndWalk only: how fast the stepping animation plays while turning on the spot (1 = normal walk speed).")]
    [SerializeField] private float turnInPlaceAnimationRate = 0.8f;

    [Tooltip("Optional: the text on the on-screen Run button, updated to say On/Off.")]
    [SerializeField] private TMPro.TextMeshProUGUI runButtonLabel;

    [Tooltip("TurnAndWalk only: how fast the avatar turns at full left/right, in degrees per second.")]
    [SerializeField] private float turnSpeed = 120f;

    [Tooltip("TurnAndWalk only: walking backward is slower than forward by this factor (1 = same speed).")]
    [SerializeField] private float backwardSpeedFactor = 0.6f;

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

    // True while the on-screen Run button is switched on (Shift does the same on a keyboard).
    public bool RunToggledOn { get; private set; }

    private bool IsRunning
    {
        get { return RunToggledOn || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
    }

    // Wired to the on-screen Run button's OnClick (touch screens have no Shift key).
    public void ToggleRun()
    {
        RunToggledOn = !RunToggledOn;
        UpdateRunLabel();
    }

    private void UpdateRunLabel()
    {
        if (runButtonLabel != null)
        {
            runButtonLabel.text = RunToggledOn ? "Run: On" : "Run: Off";
        }
    }

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
        UpdateRunLabel();
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

        Vector3 moveDirection;
        if (controlMode == ControlMode.TurnAndWalk)
        {
            // Sideways input turns the avatar (positive x = turn right = clockwise from above).
            transform.Rotate(0f, input.x * turnSpeed * Time.deltaTime, 0f);

            // Forward/back input walks along the way the avatar now faces.
            float forwardInput = input.y;
            if (forwardInput < 0f)
            {
                forwardInput *= backwardSpeedFactor;
            }
            moveDirection = transform.forward * forwardInput;
        }
        else
        {
            moveDirection = GetCameraRelativeDirection(input);
        }

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

        float topSpeed = IsRunning ? runSpeed : walkSpeed;
        Vector3 velocity = moveDirection * topSpeed;
        CurrentSpeed = velocity.magnitude;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Face the direction we're moving, so the placeholder capsule (and later the real
        // avatar model) visibly turns instead of always facing the same way.
        // (In TurnAndWalk the avatar is already turned by the input above, and facing the walking
        // direction would flip it round when walking backward, so this only applies to CameraRelative.)
        if (controlMode == ControlMode.CameraRelative && moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.forward = moveDirection;
        }

        // "Speed" must match the parameter name in AvatarAnimator exactly (case-sensitive).
        // The Animator picks Idle / Walking / Running from it (Walking above 0.1, Running above 2.5).
        if (animator != null)
        {
            float animatorSpeedValue = CurrentSpeed;
            float playbackRate = 1f;   // 1 = the clip's own speed

            bool turningOnTheSpot = controlMode == ControlMode.TurnAndWalk
                && CurrentSpeed < 0.1f && Mathf.Abs(input.x) > 0.1f;

            if (turningOnTheSpot)
            {
                // No "turn" animation exists, so step gently in place while turning, not freeze.
                animatorSpeedValue = 0.5f;
                playbackRate = turnInPlaceAnimationRate;
            }
            else if (CurrentSpeed > 0.1f)
            {
                // Play the clip as fast as the avatar really moves, so the feet don't slide.
                float referenceSpeed = CurrentSpeed > 2.5f ? runAnimationReferenceSpeed : walkAnimationReferenceSpeed;
                playbackRate = CurrentSpeed / referenceSpeed;

                // Walking backward just plays the normal walk cycle at the (slower) backward pace.
                // (Playing it in reverse looks better but Unity can't blend animations at a
                // negative playback speed: the avatar would freeze in a half-blended pose.)
            }

            animator.SetFloat("Speed", animatorSpeedValue);
            animator.speed = playbackRate;
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
