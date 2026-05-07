using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] AudioClip musicMenu;
    [SerializeField] AudioClip musicGameplay;

    [Header("SFX")]
    [SerializeField] AudioClip sfxButton;
    [SerializeField] AudioClip sfxGameOver;
    [SerializeField] AudioClip sfxTrick;

    [Header("Settings")]
    [SerializeField][Range(0f, 1f)] float musicVolume = 0.5f;
    [SerializeField][Range(0f, 1f)] float sfxVolume = 1f;

    AudioSource musicSource;
    AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Asume escena 0 = menú, cualquier otra = gameplay
        if (scene.buildIndex == 0)
            PlayMusic(musicMenu);
        else
            PlayMusic(musicGameplay);
    }

    void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    // ── API pública ──────────────────────────────────────────────────────────

    public void PlayButton() => sfxSource.PlayOneShot(sfxButton, sfxVolume);
    public void PlayGameOver() => sfxSource.PlayOneShot(sfxGameOver, sfxVolume);
    public void PlayTrick() => sfxSource.PlayOneShot(sfxTrick, sfxVolume);
}