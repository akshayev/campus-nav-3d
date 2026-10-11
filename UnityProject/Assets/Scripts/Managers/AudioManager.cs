using UnityEngine;

// Plays every sound in the game from one place (SDD 5.2). One of these lives in the Exterior
// scene, and other scripts talk to it through AudioManager.Instance, the same singleton
// pattern as POIManager and FloorTransitionManager.
//
// What it does:
//   - Footsteps: while the avatar is walking on the ground, one step sound every few metres.
//   - UI click: when a popup opens (called by POIManager).
//   - Floor change chime: when you use the stairs (called by FloorTransitionManager).
//   - Ambience: a quiet looping background sound.
//   - Mute: the "Sound" button on the screen switches everything off/on, and remembers the
//     choice next time (PlayerPrefs, same idea as the avatar choice).
//
// The four clips below are simple generated placeholders in Assets/Audio. To use better ones,
// drag a different clip onto the same slot in the Inspector, no code changes needed.
//
// WebGL note: browsers block sound until the player has clicked or touched the page once.
// Unity handles this by itself: sound simply starts after the first click/tap.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sounds (drag from Assets/Audio)")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField] private AudioClip floorChangeClip;
    [SerializeField] private AudioClip doorClip;
    [SerializeField] private AudioClip ambienceClip;

    [Header("Volumes (0 = silent, 1 = full)")]
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.15f;

    [Header("Footsteps")]
    [Tooltip("Metres the avatar travels between footsteps. Bigger = slower steps.")]
    [SerializeField] private float stepDistance = 0.8f;

    [Tooltip("The avatar to listen to. Leave empty and it is found automatically by the 'Player' tag.")]
    [SerializeField] private AvatarMovementController avatar;

    [Header("Mute button (optional)")]
    [Tooltip("The text on the on-screen Sound button, updated to say On/Off.")]
    [SerializeField] private TMPro.TextMeshProUGUI muteButtonLabel;

    private const string MutedPrefKey = "audioMuted";

    private AudioSource effectsSource;
    private AudioSource ambienceSource;
    private CharacterController avatarBody;
    private float nextStepTime;

    public bool IsMuted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Two speakers, made here in code so there is nothing extra to set up in the Editor:
        // one for short effects, one for the looping background sound.
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f; // 0 = plain 2D sound, not placed in the 3D world

        ambienceSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.playOnAwake = false;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.loop = true;
        ambienceSource.volume = ambienceVolume;
        ambienceSource.clip = ambienceClip;
    }

    private void Start()
    {
        if (avatar == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                avatar = player.GetComponent<AvatarMovementController>();
            }
        }
        if (avatar != null)
        {
            avatarBody = avatar.GetComponent<CharacterController>();
        }

        IsMuted = PlayerPrefs.GetInt(MutedPrefKey, 0) == 1;
        ApplyMute();

        if (ambienceClip != null)
        {
            ambienceSource.Play();
        }
    }

    private void Update()
    {
        if (footstepClip == null || avatar == null || IsMuted)
        {
            return;
        }

        // Only step while actually moving along the ground (not standing still, not falling).
        bool walking = avatar.CurrentSpeed > 0.1f && avatarBody != null && avatarBody.isGrounded;
        if (!walking)
        {
            // So the first step after stopping happens right away instead of after a delay.
            nextStepTime = 0f;
            return;
        }

        if (Time.time >= nextStepTime)
        {
            // Slight random pitch/volume so the steps do not sound like one repeated sample.
            effectsSource.pitch = Random.Range(0.9f, 1.1f);
            effectsSource.PlayOneShot(footstepClip, footstepVolume * Random.Range(0.85f, 1f));

            // Faster walking = shorter gap between steps (time = distance / speed).
            nextStepTime = Time.time + stepDistance / avatar.CurrentSpeed;
        }
    }

    // Called by POIManager when a room/building popup opens.
    public void PlayUiClick()
    {
        PlayEffect(uiClickClip, uiVolume);
    }

    // Called by FloorTransitionManager when the player goes up or down a floor.
    public void PlayFloorChange()
    {
        PlayEffect(floorChangeClip, uiVolume);
    }

    // Called by DoorController when a door starts to open.
    public void PlayDoor()
    {
        PlayEffect(doorClip, uiVolume);
    }

    // Wired to the on-screen Sound button's OnClick.
    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        PlayerPrefs.SetInt(MutedPrefKey, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMute();

        // Confirm the unmute with a click (a muted click would be silent anyway).
        PlayUiClick();
    }

    private void PlayEffect(AudioClip clip, float volume)
    {
        if (clip == null || IsMuted)
        {
            return;
        }

        effectsSource.pitch = 1f;
        effectsSource.PlayOneShot(clip, volume);
    }

    // AudioListener.volume is the master volume for the whole game: 0 silences everything.
    private void ApplyMute()
    {
        AudioListener.volume = IsMuted ? 0f : 1f;

        if (muteButtonLabel != null)
        {
            muteButtonLabel.text = IsMuted ? "Sound: Off" : "Sound: On";
        }
    }
}
