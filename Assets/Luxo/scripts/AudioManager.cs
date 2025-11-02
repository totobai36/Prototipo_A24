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

    [Header("Music Sources (Crossfade / Scheduling)")]
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

    [Header("Clips por estado (opcional)")]
    public AudioClip musicaExploration;
    public AudioClip musicaCountdown;

    // ==== NUEVO: Secciones musicales ====
    [Header("Secciones musicales (asignar en Inspector)")]
    [Tooltip("Suena una sola vez y luego engancha con Intro2 en loop")]
    [SerializeField] private AudioClip intro1;       // one-shot
    [SerializeField] private AudioClip intro2Loop;   // loop perfecto

    [Tooltip("Secuencia de entrada a gameplay: A1 -> A2 -> BLoop1")]
    [SerializeField] private AudioClip a1Clip;
    [SerializeField] private AudioClip a2Clip;
    [SerializeField] private AudioClip bLoop1Clip;   // loop perfecto
    [SerializeField] private AudioClip bLoop2Clip;   // loop perfecto (variante)

    [Header("Scheduling (sample-perfect)")]
    [SerializeField] private bool useScheduling = true; // dejar ON para evitar clicks

    private AudioSource _activeMusic;    // El que suena ahora
    private AudioSource _idleMusic;      // El que entra con crossfade / scheduling
    private Coroutine _fadeRoutine;

    private Dictionary<string, AudioClip> _sceneMusic;

    // ==== NUEVO: estado de secuenciador de loops ====
    private double _scheduledLoopStartDSP = 0.0;
    private AudioClip _currentLoopClip;
    private AudioClip _pendingNextLoop;
    private bool _loopScheduled;

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

    private void Update()
    {
        // ==== NUEVO: cambio de BLoop en el borde de loop ====
        if (!_loopScheduled || _pendingNextLoop == null || _currentLoopClip == null) return;

        double loopLen = ClipDur(_currentLoopClip);
        double now = AudioSettings.dspTime;

        // ¿estamos llegando al final de la vuelta actual?
        if (now + 0.05f < _scheduledLoopStartDSP + loopLen) return;

        double nextStart = _scheduledLoopStartDSP + loopLen;

        _idleMusic.clip = _pendingNextLoop;
        _idleMusic.loop = true;
        _idleMusic.volume = 1f;
        _idleMusic.PlayScheduled(nextStart);

        SwapActiveIdle();

        _currentLoopClip = _pendingNextLoop;
        _pendingNextLoop = null;
        _scheduledLoopStartDSP = nextStart;
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
                PlayVictory(0.8f);
                PlaySFXVictory();
                break;

            case GameStateManager.GameState.GameOver:
                TransitionToSnapshot("Lose", 0.5f);
                PlayDefeat(0.8f);
                PlaySFXDefeat();
                break;
        }
    }

    // ------------------ Música (crossfade genérico) ------------------
    public void PlayMusic(AudioClip clip, float fadeTime = 1f, bool loop = true)
    {
        if (!clip) return;
        if (_activeMusic.clip == clip && _activeMusic.isPlaying) return;

        _idleMusic.clip = clip;
        _idleMusic.loop = loop;
        _idleMusic.Play();

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Crossfade(_activeMusic, _idleMusic, fadeTime));

        SwapActiveIdle();
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

        if (from) { from.volume = 0f; from.Stop(); }
        if (to) to.volume = 1f;
    }

    private void SwapActiveIdle()
    {
        var temp = _activeMusic;
        _activeMusic = _idleMusic;
        _idleMusic = temp;
    }

    private static double ClipDur(AudioClip c)
    {
        if (!c) return 0.0;
        return (double)c.samples / c.frequency;
    }

    // ------------------ NUEVO: Intro1 -> loop Intro2 ------------------
    public void PlayIntroThenLoopSegment(AudioClip introOnce, AudioClip loopForever)
    {
        if (!introOnce || !loopForever)
        {
            Debug.LogWarning("PlayIntroThenLoopSegment: faltan clips.");
            return;
        }

        if (!useScheduling)
        {
            PlayMusic(introOnce, 0.1f, false);
            StartCoroutine(_PlayIntroThenLoopFallback(loopForever));
            return;
        }

        double t0 = AudioSettings.dspTime + 0.12;

        _activeMusic.clip = introOnce;
        _activeMusic.loop = false;
        _activeMusic.volume = 1f;
        _activeMusic.PlayScheduled(t0);

        double introDur = ClipDur(introOnce);

        _idleMusic.clip = loopForever;
        _idleMusic.loop = true;
        _idleMusic.volume = 1f;
        _idleMusic.PlayScheduled(t0 + introDur);

        StartCoroutine(Crossfade(_activeMusic, _idleMusic, 0.03f));

        SwapActiveIdle();

        // Reset estado de loop-switch
        _currentLoopClip = loopForever;
        _pendingNextLoop = null;
        _scheduledLoopStartDSP = t0 + introDur;
        _loopScheduled = true;

        TransitionToSnapshot("Calm", 0.4f);
    }

    private IEnumerator _PlayIntroThenLoopFallback(AudioClip loopForever)
    {
        yield return new WaitForSeconds((float)ClipDur(_activeMusic.clip) - 0.05f);
        PlayMusic(loopForever, 0.08f, true);

        _currentLoopClip = loopForever;
        _pendingNextLoop = null;
        _scheduledLoopStartDSP = AudioSettings.dspTime;
        _loopScheduled = true;
    }

    // ------------------ NUEVO: A1 -> A2 -> BLoop1 ------------------
    public void PlaySequenceIntroA1A2B1()
    {
        _pendingNextLoop = null;
        _loopScheduled = false;

        if (!useScheduling || !a1Clip || !a2Clip || !bLoop1Clip)
        {
            // Fallback por crossfades si faltan clips
            if (a1Clip) PlayMusic(a1Clip, 0.08f, false);
            if (a2Clip) StartCoroutine(_seqFallbackThenLoop(a2Clip, bLoop1Clip));
            TransitionToSnapshot("Gameplay", 0.5f);
            return;
        }

        double t0 = AudioSettings.dspTime + 0.15f;

        // A1 en activa
        _activeMusic.clip = a1Clip;
        _activeMusic.loop = false;
        _activeMusic.volume = 1f;
        _activeMusic.PlayScheduled(t0);

        double a1Dur = ClipDur(a1Clip);
        double a2Start = t0 + a1Dur;

        // A2 en idle
        _idleMusic.clip = a2Clip;
        _idleMusic.loop = false;
        _idleMusic.volume = 1f;
        _idleMusic.PlayScheduled(a2Start);

        SwapActiveIdle();

        double a2Dur = ClipDur(a2Clip);
        double bStart = a2Start + a2Dur;

        _idleMusic.clip = bLoop1Clip;
        _idleMusic.loop = true;
        _idleMusic.volume = 1f;
        _idleMusic.PlayScheduled(bStart);

        SwapActiveIdle();

        _currentLoopClip = bLoop1Clip;
        _scheduledLoopStartDSP = bStart;
        _loopScheduled = true;

        TransitionToSnapshot("Gameplay", 0.5f);
    }

    private IEnumerator _seqFallbackThenLoop(AudioClip a2, AudioClip loop)
    {
        if (a2)
        {
            yield return new WaitForSeconds((float)ClipDur(_activeMusic.clip) - 0.05f);
            PlayMusic(a2, 0.08f, false);
        }
        if (loop)
        {
            yield return new WaitForSeconds((float)ClipDur(a2) - 0.05f);
            PlayMusic(loop, 0.08f, true);
        }
        _currentLoopClip = loop;
        _loopScheduled = true;
        _scheduledLoopStartDSP = AudioSettings.dspTime;
    }

    // ------------------ NUEVO: solicitar cambio BLoop (1<->2) ------------------
    /// <summary>
    /// Pide cambiar de loop a la variante indicada (1 o 2). El cambio ocurre en el próximo borde de loop.
    /// </summary>
    public void RequestBLoopVariant(int variant)
    {
        AudioClip wanted = (variant == 2) ? bLoop2Clip : bLoop1Clip;
        if (wanted == null || !_loopScheduled) return;
        if (_currentLoopClip == wanted) return;
        _pendingNextLoop = wanted;
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
