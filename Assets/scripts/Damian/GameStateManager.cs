using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    public enum GameState { Exploration, CountdownActive, GameOver, Victory }
    [SerializeField] private GameState currentState = GameState.Exploration;

    [Header("Referencias")]
    [SerializeField] private Switch mainSwitch;

[Header("Escenas")]
// Cambia "Scenes/Derrota" por solo "Derrota"
[SerializeField] private string gameOverSceneName = "Derrota"; 
// Cambia "Scenes/Victoria" por solo "Victoria"
[SerializeField] private string victorySceneName = "Victoria"; 
    public static GameStateManager Instance { get; private set; }

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
        }
  
    }

void Start()
{
    // Suscripción al Switch
    if (mainSwitch != null)
    {
        mainSwitch.OnSwitchActivated.AddListener(OnSwitchActivated);
        Debug.Log("Suscripción al Switch realizada con éxito."); 
    }
    else
    {
        Debug.LogError("ERROR: La referencia 'mainSwitch' está vacía en el GameStateManager.");
    }
    
    // =======================================================
    // NUEVA SUSCRIPCIÓN: Escucha el GAME OVER del TimeLifeManager
    // =======================================================
    TimeLifeManager timeManager = FindFirstObjectByType<TimeLifeManager>();
    if (timeManager != null)
    {
        timeManager.OnGameOver.AddListener(OnGameOver); 
        Debug.Log("Suscripción al evento OnGameOver de TimeLifeManager.");
    }
    else
    {
        Debug.LogError("TimeLifeManager no encontrado para la suscripción de OnGameOver.");
    }
}

void OnDestroy() // Usamos OnDestroy en lugar de OnDisable para Singletons con DontDestroyOnLoad
{
    if (mainSwitch != null)
        mainSwitch.OnSwitchActivated.RemoveListener(OnSwitchActivated);

    if (TimeLifeManager.Instance != null)
        TimeLifeManager.Instance.OnGameOver.RemoveListener(OnGameOver);
}
    // Este método se llama cuando el switch invoca su evento
    public void OnSwitchActivated()
    {
        if (currentState == GameState.Exploration)
        {
            StartCountdownPhase();
        }
    }

    void StartCountdownPhase()
    {
        currentState = GameState.CountdownActive;

        // Coordinar el inicio de todos los sistemas
        StartTimer();
        TriggerLevelTransformation();
        ChangeMusicToIntense();

        Debug.Log("¡FASE DE COUNTDOWN INICIADA!");
    }

    void StartTimer()
    {
        if (TimeLifeManager.Instance != null)
        {
            // No forzamos reset aquí: StartTimer() solo activa el conteo si no está activo
            TimeLifeManager.Instance.StartTimer();
        }
        else
        {
            Debug.LogWarning("TimeLifeManager no encontrado en la escena.");
        }
    }

    void TriggerLevelTransformation()
    {
        // Notificar al sistema de transformación del nivel
        LevelTransformationManager levelTransform = FindFirstObjectByType<LevelTransformationManager>();
        if (levelTransform != null)
        {
            levelTransform.TransformLevel();
        }
    }

    void ChangeMusicToIntense()
    {
        MusicManager musicManager = FindFirstObjectByType<MusicManager>();
        if (musicManager != null)
        {
            musicManager.SwitchToCountdownMusic();
        }
    }
 void OnGameOver()
    {
        currentState = GameState.GameOver;
        Debug.Log("Estado cambiado a: Game Over. Cargando escena...");
        LoadScene(gameOverSceneName);
    }

    public void OnVictory()
    {
        currentState = GameState.Victory;
        Debug.Log("¡VICTORIA! Cargando escena...");
        LoadScene(victorySceneName);
    }

    void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"Nombre de escena vacío. Por favor, asigna la escena en el Inspector.");
            return;
        }

        // Importante: Unity debe tener todas las escenas añadidas en File -> Build Settings.
        SceneManager.LoadScene(sceneName);
    }
    public GameState GetCurrentState() => currentState;
}
