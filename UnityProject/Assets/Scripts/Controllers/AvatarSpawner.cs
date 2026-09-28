using UnityEngine;

// Put this on the Player object in Exterior. When the scene starts it:
//   1. reads which avatar was picked on the AvatarSelect screen,
//   2. creates that character model as a CHILD of Player, so it moves and turns with it,
//   3. hides the grey capsule, and
//   4. hands the model's Animator to AvatarMovementController so Idle/Walk/Run play.
//
// Only the visuals change. Player keeps its CharacterController, AvatarMovementController
// and the joystick/keyboard input path exactly as before.
[RequireComponent(typeof(AvatarMovementController))]
public class AvatarSpawner : MonoBehaviour
{
    [Tooltip("Character prefabs in the same order as the AvatarSelect buttons: 0 = Male, 1 = Female.")]
    [SerializeField] private GameObject[] avatarPrefabs;

    private void Start()
    {
        // Defaults to 0 (Male) if nothing was chosen — e.g. when you press Play directly in
        // Exterior while testing, without going through AvatarSelect first.
        int index = PlayerPrefs.GetInt(AvatarSelectorController.SelectedAvatarKey, 0);
        index = Mathf.Clamp(index, 0, avatarPrefabs.Length - 1);

        GameObject avatar = Instantiate(avatarPrefabs[index], transform);

        // Player's pivot is the middle of the capsule, but Mixamo models have their pivot at
        // the feet. Shift the model down so its feet sit at the bottom of the capsule.
        CharacterController body = GetComponent<CharacterController>();
        avatar.transform.localPosition = new Vector3(0f, body.center.y - body.height / 2f, 0f);
        avatar.transform.localRotation = Quaternion.identity;

        // The capsule stays as the (invisible) physics body; we just stop drawing it.
        GetComponent<MeshRenderer>().enabled = false;

        Animator animator = avatar.GetComponent<Animator>();
        // Our script moves the character. If the animation were also allowed to move it
        // ("root motion"), the two would fight — so make sure that's off.
        animator.applyRootMotion = false;
        GetComponent<AvatarMovementController>().SetAnimator(animator);
    }
}
