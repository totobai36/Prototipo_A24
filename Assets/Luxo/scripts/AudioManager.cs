using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips comunes")]
    public AudioClip victorySFX;
    public AudioClip defeatSFX;
    public AudioClip levelMusic;

    [Header("Fuentes (si no las asignas, se crean solas)")]
    public AudioSource musicSource; // loop
    public AudioSource sfxSource;   // one shots

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Asegurar fuentes
        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();

        // Config por defecto
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f; // 2D
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;   // 2D
    }

    // ---------- SFX ----------
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        float prevPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = prevPitch;
    }

    public void PlayVictory(float volume = 1f) { PlaySFX(victorySFX, volume); }
    public void PlayDefeat(float volume = 1f) { PlaySFX(defeatSFX, volume); }

    // ---------- Música ----------
    public void PlayMusic(AudioClip clip = null, float volume = 0.6f, bool loop = true)
    {
        if (clip) levelMusic = clip;
        if (!levelMusic) return;
        musicSource.loop = loop;
        musicSource.clip = levelMusic;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();
}
