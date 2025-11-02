using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;             // Singleton

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;   // Master/Music/SFX
    [SerializeField] private string musicVolParam = "MusicVolume";
    [SerializeField] private string sfxVolParam = "SFXVolume";

    [Header("Music Sources (Crossfade)")]
    [SerializeField] private AudioSource musicA;
    [SerializeField] private AudioSource musicB;
    [SerializeField] private float defaultFadeTime = 1.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;  // o una pool si querés
    [SerializeField] private AudioClip sfxVictory;   // opcional: SFX corto de victoria
    [SerializeField] private AudioClip sfxDefeat;    // opcional: SFX corto de derrota

    [Header("Clips por escena (por defecto)")]
    public AudioClip musicaInicio;
    public AudioClip musicaTuto;
    public AudioClip musicaLevel1;
    public AudioClip musicaVictoria;
    public AudioClip musicaDerrota;

    // (Opcional) Clips por estado de juego si querés forzar música en estados
    [Header("Clips por estado (opcional)")]
    public AudioClip musicaExploration;
    public AudioClip musicaCountdown;

    private AudioSource _activeMusic;    // El que suena ahora
    private AudioSource _idleMusic;      // El que entra con crossfade
    private Coroutine _fadeRoutine;

    private Dictionary<string, AudioClip> _sceneMusic;

    // ------------------ Lifecycle ------------------
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _activeMusic = musicA;
            _idleMusic = musicB;

            _sceneMusic = new Dictionary<string, AudioClip>()
            {
                { "Inicio",     musicaInicio  },
                { "Level Tuto", musicaTuto    },
                { "Level 1",    musicaLevel1  },
                { "Victoria",   musicaVictoria},
                { "Derrota",    musicaDerrota }
            };

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Suscribirse al cambio de estado del GameStateManager (si existe)
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameStateChanged.AddListener(HandleGameStateChanged);
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameStateChanged.RemoveListener(HandleGameStateChanged);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ------------------ Scene -> música/snapshot ------------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1) Música por escena (si hay default configurado)
        if (_sceneMusic != null && _sceneMusic.TryGetValue(scene.name, out var clip) && clip != null)
            PlayMusic(clip, defaultFadeTime);

        // 2) Snapshot por escena (opcional)
        switch (scene.name)
        {
            case "Inicio":
                TransitionToSnapshot("Calm", 0.8f);
                break;
            case "Level Tuto":
            case "Level 1":
                TransitionToSnapshot("Gameplay", 0.8f);
                break;
            case "Victoria":
                TransitionToSnapshot("Win", 0.8f);
                break;
            case "Derrota":
                TransitionToSnapshot("Lose", 0.8f);
                break;
        }
    }

    // ------------------ GameState -> música/snapshot ------------------
    private void HandleGameStateChanged(GameStateManager.GameState state)
    {
        // Cambiar snapshot según estado
        switch (state)
        {
            case GameStateManager.GameState.Exploration:
                TransitionToSnapshot("Calm", 0.5f);
                if (musicaExploration) PlayMusic(musicaExploration, 0.8f, true);
                break;

            case GameStateManager.GameState.CountdownActive:
                TransitionToSnapshot("Gameplay", 0.5f);
                if (musicaCountdown) PlayMusic(musicaCountdown, 0.8f, true);
                break;

            case GameStateManager.GameState.Victory:
                TransitionToSnapshot("Win", 0.5f);
                PlayVictory(0.8f); // música de victoria (si está asignada)
                PlaySFXVictory();  // SFX corto (opcional)
                break;

            case GameStateManager.GameState.GameOver:
                TransitionToSnapshot("Lose", 0.5f);
                PlayDefeat(0.8f);  // música de derrota (si está asignada)
                PlaySFXDefeat();   // SFX corto (opcional)
                break;
        }
    }

    // ------------------ Música ------------------
    public void PlayMusic(AudioClip clip, float fadeTime = 1f, bool loop = true)
    {
        if (!clip) return;

        if (_activeMusic.clip == clip && _activeMusic.isPlaying) return;

        _idleMusic.clip = clip;
        _idleMusic.loop = loop;
        _idleMusic.Play();

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Crossfade(_activeMusic, _idleMusic, fadeTime));

        var temp = _activeMusic;
        _activeMusic = _idleMusic;
        _idleMusic = temp;
    }

    public void StopMusic(float fadeTime = 0.5f)
    {
        if (_activeMusic == null) return;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Crossfade(_activeMusic, null, fadeTime));
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float time)
    {
        float t = 0f;
        float startFrom = from ? from.volume : 0f;
        float startTo = to ? to.volume : 0f;

        if (to) to.volume = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            if (from) from.volume = Mathf.Lerp(startFrom, 0f, k);
            if (to) to.volume = Mathf.Lerp(startTo, 1f, k);
            yield return null;
        }

        if (from)
        {
            from.volume = 0f;
            from.Stop();
        }
        if (to) to.volume = 1f;
    }

    // ------------------ Wrappers para GameStateManager ------------------
    public void PlayVictory(float fadeTime = 1f)
    {
        if (musicaVictoria) PlayMusic(musicaVictoria, fadeTime, false);
    }

    public void PlayDefeat(float fadeTime = 1f)
    {
        if (musicaDerrota) PlayMusic(musicaDerrota, fadeTime, false);
    }

    // ------------------ SFX ------------------
    public void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (!clip || !sfxSource) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXVictory() { if (sfxVictory) PlaySFX(sfxVictory); }
    public void PlaySFXDefeat() { if (sfxDefeat) PlaySFX(sfxDefeat); }

    // ------------------ Volúmenes (0..1) -> dB ------------------
    public void SetMusicVolume01(float v01)
    {
        if (!mainMixer) return;
        mainMixer.SetFloat(musicVolParam, ToDecibels(v01));
    }

    public void SetSFXVolume01(float v01)
    {
        if (!mainMixer) return;
        mainMixer.SetFloat(sfxVolParam, ToDecibels(v01));
    }

    private float ToDecibels(float v01)
    {
        v01 = Mathf.Clamp(v01, 0.0001f, 1f);
        return Mathf.Log10(v01) * 20f;
    }

    // ------------------ Snapshots ------------------
    public void TransitionToSnapshot(string snapshotName, float time)
    {
        if (!mainMixer) return;
        var snap = mainMixer.FindSnapshot(snapshotName);
        if (snap != null) snap.TransitionTo(time);
    }
}
