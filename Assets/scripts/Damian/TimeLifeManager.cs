using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TimeLifeManager : MonoBehaviour
{
    [Header("Configuración de Tiempo")]
    [SerializeField] private float baseTime = 90f;
    [SerializeField] private float currentTime;
    [SerializeField] private bool isTimerActive = false;

    [Header("Pérdida de Tiempo")]
    [SerializeField] private float fallDamagePerMeter = 2f;
    [SerializeField] private float minFallHeight = 3f;
    [SerializeField] private float enemyDamage = 10f;

    [Header("Colores del Timer")]
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;

    [Header("Umbrales de Color")]
    [SerializeField] private float yellowThreshold = 0.6f; // >= 60% verde
    [SerializeField] private float redThreshold = 0.3f; // < 30% rojo

    [Header("Eventos")]
    public UnityEvent OnTimerStart;
    public UnityEvent OnTimeLoss;
    public UnityEvent OnTimeGain;
    public UnityEvent OnGameOver;
    public UnityEvent OnCriticalTime;

    // Evento con payload: tiempo actual (float)
    public UnityEvent<float> OnTimerUpdated;

    // Propiedades públicas
    public float CurrentTime => currentTime;
    public float BaseTime => baseTime;
    public bool IsTimerActive => isTimerActive;
    public float TimePercentage => baseTime <= 0 ? 0f : currentTime / baseTime;

    // Singleton
    public static TimeLifeManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentTime = baseTime;

        OnTimerStart = OnTimerStart ?? new UnityEvent();
        OnTimeLoss = OnTimeLoss ?? new UnityEvent();
        OnTimeGain = OnTimeGain ?? new UnityEvent();
        OnGameOver = OnGameOver ?? new UnityEvent();
        OnCriticalTime = OnCriticalTime ?? new UnityEvent();
        OnTimerUpdated = OnTimerUpdated ?? new UnityEvent<float>();
    }

    void Update()
    {
        if (isTimerActive)
            UpdateTimer();
    }

    void UpdateTimer()
    {
        float prev = currentTime;
        currentTime -= Time.deltaTime;

        if (currentTime <= 10f && prev > 10f)
        {
            OnCriticalTime?.Invoke();
        }

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            GameOver();
        }

        // Emitir actualización para UI cada frame
        OnTimerUpdated?.Invoke(currentTime);
    }

    // StartTimer() activa el conteo. Si ya está activo, no reinicia por defecto.
    public void StartTimer(bool resetIfAlreadyActive = false)
    {
        if (isTimerActive && !resetIfAlreadyActive)
            return;

        if (resetIfAlreadyActive)
            currentTime = baseTime;

        isTimerActive = true;
        OnTimerStart?.Invoke();
        Debug.Log("¡Timer iniciado! Tiempo restante: " + currentTime + "s");
    }

    public void StopTimer()
    {
        isTimerActive = false;
    }

    public void LoseTime(float timeToLose, Vector3 damagePosition = default)
    {
        if (timeToLose <= 0) return;

        currentTime = Mathf.Max(0, currentTime - timeToLose);
        OnTimeLoss?.Invoke();
        OnTimerUpdated?.Invoke(currentTime);

        Debug.Log($"Tiempo perdido: {timeToLose}s. Tiempo restante: {currentTime}s");

        if (currentTime <= 0f)
            GameOver();
    }

    public void GainTime(float timeToGain)
    {
        if (timeToGain <= 0) return;

        currentTime = Mathf.Min(baseTime, currentTime + timeToGain);
        OnTimeGain?.Invoke();
        OnTimerUpdated?.Invoke(currentTime);

        Debug.Log($"Tiempo ganado: {timeToGain}s. Tiempo restante: {currentTime}s");
    }

    public void ProcessFallDamage(float fallDistance)
    {
        if (fallDistance >= minFallHeight)
        {
            float damage = (fallDistance - minFallHeight) * fallDamagePerMeter;
            LoseTime(damage);
        }
    }

    public void ProcessEnemyDamage(Vector3 damagePosition)
    {
        LoseTime(enemyDamage, damagePosition);
    }

    public Color GetTimerColor()
    {
        float percentage = TimePercentage;
        if (percentage >= yellowThreshold)
            return greenColor;
        else if (percentage >= redThreshold)
            return yellowColor;
        else
            return redColor;
    }

    void GameOver()
    {
        isTimerActive = false;
        OnGameOver?.Invoke();
        Debug.Log("GAME OVER - Desconexión");
    }

    public void ResetTimer()
    {
        currentTime = baseTime;
        isTimerActive = false;
        OnTimerUpdated?.Invoke(currentTime);
    }

    public void FreezeTime(float duration)
    {
        StartCoroutine(FreezeTimerCoroutine(duration));
    }

    IEnumerator FreezeTimerCoroutine(float duration)
    {
        bool wasActive = isTimerActive;
        isTimerActive = false;

        yield return new WaitForSeconds(duration);

        isTimerActive = wasActive;
        Debug.Log($"Tiempo congelado por {duration}s");
    }
}