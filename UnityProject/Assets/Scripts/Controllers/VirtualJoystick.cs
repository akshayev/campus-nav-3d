using UnityEngine;
using UnityEngine.EventSystems;

// Put this on the joystick BACKGROUND image (a UI Image with a Canvas as an ancestor).
// It reads finger/mouse drags and turns them into a direction the rest of the game can use.
//
// How it works: while the player is dragging inside this circle, we track how far the
// "handle" (the little knob) has moved away from the center, clamp that distance to
// handleRange, and store the result as Direction — a vector whose length is 0 (not
// moving) to 1 (fully pushed to the edge).
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("The background circle's own RectTransform. Drag the same object this script is on.")]
    [SerializeField] private RectTransform background;

    [Tooltip("The small knob that visually slides around inside the background.")]
    [SerializeField] private RectTransform handle;

    [Tooltip("How far the handle can move from the center, in UI pixels.")]
    [SerializeField] private float handleRange = 60f;

    // Other scripts read this every frame. (0,0) means "not being touched right now".
    public Vector2 Direction { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Treat a fresh tap/click the same as an immediate drag to that point.
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out localPoint);

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        handle.anchoredPosition = clamped;

        Direction = clamped / handleRange; // now in the range 0..1
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }
}
