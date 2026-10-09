using UnityEngine;

// Sits on an invisible trigger Collider, same idea as POITriggerVolume from Phase 7. Place one
// at the bottom of a staircase in the Exterior scene (direction = EnterFloor) and a matching one
// at the top of the stairs inside the floor scene (direction = ExitFloor), each pointing at a
// spawnPoint Transform marking where the avatar should appear after the transition.
[RequireComponent(typeof(Collider))]
public class StairsTriggerVolume : MonoBehaviour
{
    public enum Direction { EnterFloor, ExitFloor }

    [Tooltip("EnterFloor: walking in loads the target floor. ExitFloor: walking in unloads the current floor and returns outside.")]
    [SerializeField] private Direction direction = Direction.EnterFloor;

    [Tooltip("Only used when Direction is EnterFloor — must match a floorId in Firestore (or the placeholder, e.g. 'FLOOR_1').")]
    [SerializeField] private string floorId = "FLOOR_1";

    [Tooltip("Where the avatar should appear after this transition completes.")]
    [SerializeField] private Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning($"StairsTriggerVolume on '{name}': no spawnPoint assigned, cannot transition.");
            return;
        }

        if (direction == Direction.EnterFloor)
        {
            FloorTransitionManager.Instance.EnterFloor(floorId, spawnPoint.position);
        }
        else
        {
            FloorTransitionManager.Instance.ExitFloor(spawnPoint.position);
        }
    }
}
