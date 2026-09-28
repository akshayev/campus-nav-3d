using UnityEngine;
using UnityEngine.SceneManagement;

// Lives in the AvatarSelect scene. Each avatar button calls SelectAvatar with its own number
// (Male = 0, Female = 1 — set in the button's OnClick list in the Inspector).
//
// We remember the choice with PlayerPrefs: Unity's built-in "save a small value on this
// device" feature. It survives scene changes (and even closing the game), which is exactly
// what we need to carry the choice from this scene into Exterior.
public class AvatarSelectorController : MonoBehaviour
{
    // The PlayerPrefs "slot name". AvatarSpawner reads the same constant, so the two scripts
    // can never disagree because of a typo.
    public const string SelectedAvatarKey = "SelectedAvatar";

    [Tooltip("Scene to load after an avatar is picked. Must be listed in File > Build Profiles.")]
    [SerializeField] private string nextSceneName = "Exterior";

    public void SelectAvatar(int avatarIndex)
    {
        PlayerPrefs.SetInt(SelectedAvatarKey, avatarIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nextSceneName);
    }
}
