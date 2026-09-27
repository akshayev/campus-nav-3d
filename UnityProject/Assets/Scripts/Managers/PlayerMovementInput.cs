using UnityEngine;

// The single shared source of "which way does the player want to move".
//
// AvatarMovementController (and anything else that needs movement input later) only ever
// reads PlayerMovementInput.Instance.MoveDirection. It never talks to the joystick or the
// keyboard directly. That's what lets us test with a keyboard in the Editor and switch to
// touch on a phone with zero changes anywhere else.
//
// Priority rule: if the on-screen joystick is actively being dragged, it wins. Otherwise we
// fall back to WASD / arrow keys. This means desktop WebGL and the Editor "just work" even
// though there's no touchscreen, exactly as CLAUDE.md asks for.
public class PlayerMovementInput : MonoBehaviour
{
    public static PlayerMovementInput Instance { get; private set; }

    [Tooltip("Drag the JoystickBackground object here (the one with the VirtualJoystick script).")]
    [SerializeField] private VirtualJoystick joystick;

    // How big the joystick's input has to be before we trust it over the keyboard.
    // Prevents tiny drag jitter from overriding keyboard input.
    private const float JoystickActiveThreshold = 0.05f;

    public Vector2 MoveDirection { get; private set; }

    private void Awake()
    {
        // Simple singleton: if one already exists, this is a duplicate, so remove it.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        MoveDirection = ReadMoveInput();
    }

    // The ONE place where touch, mouse (the joystick handles both) and keyboard converge.
    // Add a new input device later (e.g. a gamepad) by changing only this method.
    private Vector2 ReadMoveInput()
    {
        Vector2 joystickDirection = joystick != null ? joystick.Direction : Vector2.zero;
        if (joystickDirection.sqrMagnitude >= JoystickActiveThreshold * JoystickActiveThreshold)
        {
            return joystickDirection;
        }

        Vector2 keyboardDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
        return Vector2.ClampMagnitude(keyboardDirection, 1f);
    }
}
