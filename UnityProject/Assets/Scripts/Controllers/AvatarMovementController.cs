using UnityEngine;

// Moves the avatar around using whatever direction PlayerMovementInput is currently reporting.
// This script doesn't know or care if that direction came from the joystick or the keyboard —
// that's the whole point of having a shared input source.
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

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector2 input = PlayerMovementInput.Instance != null
            ? PlayerMovementInput.Instance.MoveDirection
            : Vector2.zero;

        // Screen-space X/Y from the input becomes world-space X/Z (ground plane movement).
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
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
        }

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Face the direction we're moving, so the placeholder capsule (and later the real
        // avatar model) visibly turns instead of always facing the same way.
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.forward = moveDirection;
        }
    }
}
