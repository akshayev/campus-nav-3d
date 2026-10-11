using UnityEngine;

// Sits on an invisible trigger Collider, same idea as POITriggerVolume from Phase 7. Place one
// at the bottom of a staircase (direction = EnterFloor, pointing at a spawnPoint marking where
// the avatar lands on the new floor) and a matching one at the top of the stairs inside that
// floor scene (direction = ExitFloor) to come back down.
//
// ExitFloor does NOT need a spawnPoint: FloorTransitionManager remembers where the player was
// standing right before they went up, and returns them to exactly that spot. This matters once
// floors stack (Exterior -> Floor1 -> Floor2): a spawnPoint for "go back down" would need a
// Transform reference pointing INTO a scene that's unloaded whenever you're not standing in it,
// which Unity can't resolve — so we sidestep the whole problem by not needing one.
[RequireComponent(typeof(Collider))]
public class StairsTriggerVolume : MonoBehaviour
{
    public enum Direction { EnterFloor, ExitFloor }

    [Tooltip("EnterFloor: walking in loads the target floor. ExitFloor: walking in returns to wherever the player came from.")]
    [SerializeField] private Direction direction = Direction.EnterFloor;

    [Tooltip("Only used when Direction is EnterFloor — must match a floorId in Firestore (e.g. 'FLOOR_1', 'FLOOR_2').")]
    [SerializeField] private string floorId = "FLOOR_1";

    [Tooltip("Only used when Direction is EnterFloor — where the avatar should appear on the new floor.")]
    [SerializeField] private Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (direction == Direction.EnterFloor)
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning($"StairsTriggerVolume on '{name}': no spawnPoint assigned, cannot transition.");
                return;
            }

            FloorTransitionManager.Instance.EnterFloor(floorId, spawnPoint.position);
        }
        else
        {
            FloorTransitionManager.Instance.ExitFloor();
        }
    }
}
