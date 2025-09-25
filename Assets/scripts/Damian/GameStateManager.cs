using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    public enum GameState { Exploration, CountdownActive, GameOver, Victory }
    [SerializeField] private GameState currentState = GameState.Exploration;

    [Header("Referencias")]
    [SerializeField] private Switch mainSwitch;

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
        Debug.Log("Suscripción al Switch realizada con éxito."); // Agrega este log para confirmar
    }
    else
    {
        Debug.LogError("ERROR: La referencia 'mainSwitch' está vacía en el GameStateManager.");
    }

    // TimeLifeManager: asume que su Awake() ya asignó la Instance
    if (TimeLifeManager.Instance != null)
        TimeLifeManager.Instance.OnGameOver.AddListener(OnGameOver);
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
        Debug.Log("Estado cambiado a: Game Over");
    }

    public void OnVictory()
    {
        currentState = GameState.Victory;
        Debug.Log("¡VICTORIA!");
    }

    public GameState GetCurrentState() => currentState;
}
