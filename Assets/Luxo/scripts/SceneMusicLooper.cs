using UnityEngine;

/// <summary>
/// Control de música por ESCENA (sin singleton).
/// - Modo IntroThenLoop: Intro1 (una vez) -> Intro2 (loop).
/// - Modo A1A2ThenLoop: A1 -> A2 -> B1 (loop), con cambio B1<->B2 en el borde.
/// Requiere 2 AudioSources asignados: musicA y musicB (2D, PlayOnAwake OFF, Loop OFF).
/// </summary>
public class SceneMusicLooper : MonoBehaviour
{
    public enum Mode { IntroThenLoop, A1A2ThenLoop }

    [Header("Modo de esta escena")]
    [SerializeField] private Mode mode = Mode.IntroThenLoop;

    [Header("Fuentes (en este mismo GameObject)")]
    [SerializeField] private AudioSource musicA;   // 2D, PlayOnAwake OFF, Loop OFF
    [SerializeField] private AudioSource musicB;   // 2D, PlayOnAwake OFF, Loop OFF

    [Header("Clips - IntroThenLoop")]
    [SerializeField] private AudioClip intro1;       // one-shot
    [SerializeField] private AudioClip intro2Loop;   // loop perfecto (Loop marcado en import)

    [Header("Clips - A1A2ThenLoop")]
    [SerializeField] private AudioClip a1Clip;
    [SerializeField] private AudioClip a2Clip;
    [SerializeField] private AudioClip bLoop1Clip;   // loop perfecto
    [SerializeField] private AudioClip bLoop2Clip;   // loop perfecto (variante)

    [Header("Opciones")]
    [Tooltip("Usar PlayScheduled para encadenado sample-perfect")]
    [SerializeField] private bool useScheduling = true;
    [Tooltip("Pequeño crossfade de seguridad al encadenar (segundos)")]
    [SerializeField] private float microFade = 0.03f;

    // Estado interno
    private AudioSource _active;
    private AudioSource _idle;
    private AudioClip _currentLoop;
    private AudioClip _pendingLoop;
    private double _scheduledLoopStartDSP;
    private bool _loopScheduled;

    private void Reset()
    {
        // ayuda en editor al añadir el script
        var sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            musicA = sources[0];
            musicB = sources[1];
        }
    }

    private void Awake()
    {
        if (!musicA || !musicB)
            Debug.LogWarning("[SceneMusicLooper] Asigna dos AudioSources 2D (PlayOnAwake OFF, Loop OFF).");

        _active = musicA;
        _idle = musicB;
    }

    private void Start()
    {
        switch (mode)
        {
            case Mode.IntroThenLoop: Play_IntroThenLoop(); break;
            case Mode.A1A2ThenLoop: Play_A1A2ThenLoop(); break;
        }
    }

    private void Update()
    {
        // Cambio B1<->B2 cuantizado
        if (!_loopScheduled || _pendingLoop == null || _currentLoop == null) return;

        double loopLen = ClipDur(_currentLoop);
        double now = AudioSettings.dspTime;

        if (now + 0.05f < _scheduledLoopStartDSP + loopLen) return;

        double nextStart = _scheduledLoopStartDSP + loopLen;

        _idle.clip = _pendingLoop;
        _idle.loop = true;
        _idle.volume = 1f;
        if (useScheduling) _idle.PlayScheduled(nextStart);
        else _idle.Play(); // fallback (no sample-perfect)

        SwapSources();

        _currentLoop = _pendingLoop;
        _pendingLoop = null;
        _scheduledLoopStartDSP = nextStart;
    }

    // ----------------- Modos -----------------

    private void Play_IntroThenLoop()
    {
        if (!intro2Loop)
        {
            Debug.LogWarning("[SceneMusicLooper] Falta Intro2Loop.");
            return;
        }

        if (!intro1 || !useScheduling)
        {
            // Fallback simple: si no hay Intro1 o no se usa scheduling, loopea Intro2.
            _active.clip = intro2Loop;
            _active.loop = true;
            _active.Play();
            _currentLoop = intro2Loop;
            _loopScheduled = true;
            _scheduledLoopStartDSP = AudioSettings.dspTime;
            return;
        }

        double t0 = AudioSettings.dspTime + 0.12;

        // Intro1 (once)
        _active.clip = intro1;
        _active.loop = false;
        _active.volume = 1f;
        _active.PlayScheduled(t0);

        // Intro2 (loop)
        double introDur = ClipDur(intro1);
        _idle.clip = intro2Loop;
        _idle.loop = true;
        _idle.volume = 1f;
        _idle.PlayScheduled(t0 + introDur);

        if (microFade > 0f) StartCoroutine(Crossfade(_active, _idle, microFade));
        SwapSources();

        _currentLoop = intro2Loop;
        _pendingLoop = null;
        _scheduledLoopStartDSP = t0 + introDur;
        _loopScheduled = true;
    }

    private void Play_A1A2ThenLoop()
    {
        if (!a1Clip || !a2Clip || !bLoop1Clip)
        {
            Debug.LogWarning("[SceneMusicLooper] Faltan clips A1/A2/B1.");
            return;
        }

        if (!useScheduling)
        {
            // Fallback por crossfades “a tiempo”
            StartCoroutine(SeqFallbackThenLoop());
            return;
        }

        double t0 = AudioSettings.dspTime + 0.15;

        // A1
        _active.clip = a1Clip;
        _active.loop = false;
        _active.volume = 1f;
        _active.PlayScheduled(t0);

        double a1Dur = ClipDur(a1Clip);
        double a2Start = t0 + a1Dur;

        // A2
        _idle.clip = a2Clip;
        _idle.loop = false;
        _idle.volume = 1f;
        _idle.PlayScheduled(a2Start);
        SwapSources();

        double a2Dur = ClipDur(a2Clip);
        double bStart = a2Start + a2Dur;

        // B1 (loop)
        _idle.clip = bLoop1Clip;
        _idle.loop = true;
        _idle.volume = 1f;
        _idle.PlayScheduled(bStart);
        SwapSources();

        _currentLoop = bLoop1Clip;
        _scheduledLoopStartDSP = bStart;
        _loopScheduled = true;
    }

    // ----------------- API pública minimal -----------------

    /// <summary>
    /// Pide cambiar de loop (B1/B2). Se aplica en el próximo borde del loop actual.
    /// </summary>
    public void RequestLoopVariant(int variant) // 1 o 2
    {
        AudioClip wanted = (variant == 2) ? bLoop2Clip : bLoop1Clip;
        if (!_loopScheduled || wanted == null || _currentLoop == wanted) return;
        _pendingLoop = wanted;
    }

    // ----------------- Utils -----------------

    private static double ClipDur(AudioClip c) => !c ? 0.0 : (double)c.samples / c.frequency;

    private void SwapSources()
    {
        var tmp = _active;
        _active = _idle;
        _idle = tmp;
    }

    private System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to, float time)
    {
        float t = 0f;
        float vFrom0 = from ? from.volume : 0f;
        float vTo0 = to ? to.volume : 0f;
        if (to) to.volume = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            if (from) from.volume = Mathf.Lerp(vFrom0, 0f, k);
            if (to) to.volume = Mathf.Lerp(vTo0, 1f, k);
            yield return null;
        }

        if (from) { from.volume = 0f; from.Stop(); }
        if (to) to.volume = 1f;
    }

    // ----------------- Fallback simple si no se usa PlayScheduled -----------------
    private System.Collections.IEnumerator SeqFallbackThenLoop()
    {
        if (a1Clip)
        {
            _active.clip = a1Clip;
            _active.loop = false;
            _active.Play();
            yield return new WaitForSeconds(a1Clip.length);
        }

        if (a2Clip)
        {
            _active.clip = a2Clip;
            _active.loop = false;
            _active.Play();
            yield return new WaitForSeconds(a2Clip.length);
        }

        if (bLoop1Clip)
        {
            _active.clip = bLoop1Clip;
            _active.loop = true;
            _active.Play();
            _currentLoop = bLoop1Clip;
            _loopScheduled = true;
            _scheduledLoopStartDSP = AudioSettings.dspTime;
        }
    }
}
