using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TimeLifeManager : MonoBehaviour
{
    public static TimeLifeManager Instance { get; private set; }

    [Header("Configuración de Tiempo")]
    [SerializeField] private float baseTime = 90f;
    [SerializeField] private float currentTime;
    [SerializeField] private bool isTimerActive = false;
    // NUEVO: Tiempo de espera entre Game Over (Ragdoll) y la carga de escena.
    [SerializeField] private float gameOverDelay = 1.0f; 

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
    public float TimePercentage => baseTime > 0 ? currentTime / baseTime : 0f;
    public bool IsTimerActive => isTimerActive;
    
    // Flag para evitar múltiples llamadas a la corrutina de Game Over
    private bool isGameOverProcessing = false; 


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentTime = baseTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartTimer()
    {
        if (!isTimerActive)
        {
            isTimerActive = true;
            OnTimerStart?.Invoke();
            Debug.Log("Temporizador iniciado.");
        }
    }

    public void StopTimer()
    {
        if (isTimerActive)
        {
            isTimerActive = false;
            Debug.Log("Temporizador detenido.");
        }
    }

    void Update()
    {
        if (isTimerActive)
        {
            currentTime -= Time.deltaTime;
            OnTimerUpdated?.Invoke(currentTime);
            
            // Lógica para el umbral de tiempo crítico
            if (currentTime <= baseTime * redThreshold && currentTime > 0)
            {
                OnCriticalTime?.Invoke();
            }

            if (currentTime <= 0)
            {
                currentTime = 0; // CORRECCIÓN CLAVE: Asegura que el tiempo nunca es negativo
                GameOver();
            }
        }
    }

    public void LoseTime(float timeToLose, Vector3? damagePosition = null)
    {
        if (!isTimerActive || isGameOverProcessing) return;

        currentTime -= timeToLose;
        OnTimeLoss?.Invoke();
        OnTimerUpdated?.Invoke(currentTime);

        if (currentTime <= 0)
        {
            currentTime = 0;
            GameOver();
        }
        
        Debug.Log($"Tiempo perdido: {timeToLose}s. Tiempo restante: {currentTime}s");
    }

    public void GainTime(float timeToGain)
    {
        if (!isTimerActive || isGameOverProcessing) return;

        currentTime += timeToGain;
        if (currentTime > baseTime)
        {
            currentTime = baseTime;
        }

        OnTimeGain?.Invoke();
        OnTimerUpdated?.Invoke(currentTime);

        Debug.Log($"Tiempo ganado: {timeToGain}s. Tiempo restante: {currentTime}s");
    }

    // --- NUEVA LÓGICA DE GAME OVER CON DELAY ---
    void GameOver()
    {
        // 1. Prevenir ejecuciones duplicadas.
        if (isGameOverProcessing) return; 
        
        isTimerActive = false;
        isGameOverProcessing = true; // Activar el flag de proceso

        // 2. Notifica a los suscriptores (ej: CharacterDeathHandler para el Ragdoll)
        // El ragdoll/animación se activa INMEDIATAMENTE.
        OnGameOver?.Invoke(); 
        
        Debug.Log("GAME OVER: Tiempo agotado. Activando animación y esperando para cargar la escena...");

        // 3. Inicia la Coroutine que ESPERARÁ y luego cargará la escena de derrota.
        StartCoroutine(ProcessGameOverWithDelay(gameOverDelay)); // Usa la variable de 1.0s
    }

    private IEnumerator ProcessGameOverWithDelay(float delay)
    {
        // Espera el delay (1 segundo) para que se vea la animación inicial
        yield return new WaitForSeconds(delay);

        // Notificar al GameStateManager para la carga de escena de Derrota
        if (GameStateManager.Instance != null)
        {
            // Nota: Se asume que GameStateManager.Instance.OnGameOver() es público
            // y contiene la llamada a SceneManager.LoadScene("Derrota").
            GameStateManager.Instance.OnGameOver();
        }
        else
        {
            Debug.LogError("GameStateManager.Instance no encontrado. La escena de derrota no se puede cargar.");
        }
        
        // El objeto TimeLifeManager será destruido con la carga de la escena.
    }
    // --- FIN NUEVA LÓGICA DE GAME OVER CON DELAY ---


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
    }
    
    // Métodos de daño (dejados para referencia)
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
}