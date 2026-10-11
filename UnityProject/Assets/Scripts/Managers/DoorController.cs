using UnityEngine;

// A door that swings open when the player walks up to it and closes again when they leave.
//
// How it is built (done for you by the interior builder in each floor scene):
//   DoorRoot   - this script + a big invisible trigger box around the doorway. It never moves.
//   Hinge      - an empty object at the door's hinge edge. This is what actually turns.
//   Leaf       - the visible door panel (with a solid collider), a child of Hinge.
//
// The trigger is bigger than the door, so the door starts opening a step or two before the
// avatar reaches it, and stays open while the avatar is in the doorway. It never closes on
// top of the player.
//
// Same trigger pattern as POITriggerVolume: it only reacts to the object tagged "Player".
[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Tooltip("The object that turns. Put it at the door's hinge edge, with the door panel as its child.")]
    [SerializeField] private Transform hinge;

    [Tooltip("How far the door swings, in degrees. Negative swings one way, positive the other.")]
    [SerializeField] private float openAngle = -90f;

    [Tooltip("How fast the door turns, in degrees per second.")]
    [SerializeField] private float turnSpeed = 240f;

    private int playersNearby;
    private float currentAngle;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playersNearby++;
        if (playersNearby == 1 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playersNearby = Mathf.Max(0, playersNearby - 1);
    }

    private void Update()
    {
        if (hinge == null)
        {
            return;
        }

        float target = playersNearby > 0 ? openAngle : 0f;
        currentAngle = Mathf.MoveTowards(currentAngle, target, turnSpeed * Time.deltaTime);
        hinge.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}
