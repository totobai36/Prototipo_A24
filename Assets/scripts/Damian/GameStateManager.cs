using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    public enum GameState { Exploration, CountdownActive, GameOver, Victory }
    [SerializeField] private GameState currentState = GameState.Exploration;

    // =======================================================
    // FIX CLAVE: Agregar la definición del evento de cambio de estado
    // =======================================================
    [HideInInspector] public UnityEvent<GameState> OnGameStateChanged = new UnityEvent<GameState>();

    [Header("Referencias")]
    [SerializeField] private Switch mainSwitch;

    [Header("Escenas")]
    [SerializeField] private string gameOverSceneName = "Derrota"; 
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
        
        // Inicializar el estado y notificar
        SetGameState(currentState);
    }
    
    // =======================================================
    // NUEVO: Método para cambiar el estado y disparar el evento
    // =======================================================
    private void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        // NOTIFICACIÓN: Esto permite a ExtractionPoint (y otros scripts) reaccionar
        OnGameStateChanged?.Invoke(currentState); 
        
        Debug.Log($"[GameStateManager] Estado de juego cambiado a: {currentState}");
    }

    private void OnSwitchActivated()
    {
        if (currentState == GameState.Exploration)
        {
            Debug.Log("Switch activado. Iniciando Cuenta Regresiva.");
            SetGameState(GameState.CountdownActive);
            
            TriggerLevelTransformation();
            ChangeMusicToIntense();
        }
    }

    // Lógica para OnCountdownEnd (Llamada por TimeLifeManager)
    public void OnCountdownEnd()
    {
        if (currentState == GameState.CountdownActive)
        {
            Debug.Log("El tiempo ha terminado. Game Over.");
            OnGameOver(); // Llama a la lógica de Game Over
        }
    }

    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    public void OnGameOver()
    {
        if (currentState != GameState.Victory) 
        {
            SetGameState(GameState.GameOver);
            Debug.Log("Estado cambiado a: Game Over. Cargando escena...");
            LoadScene(gameOverSceneName);
        }
    }

    public void OnVictory()
    {
        SetGameState(GameState.Victory);
        Debug.Log("¡VICTORIA! Cargando escena...");
        LoadScene(victorySceneName);
    }
    
    void TriggerLevelTransformation()
    {
        // Lógica de transformación del nivel...
    }

    void ChangeMusicToIntense()
    {
        // Lógica para cambiar la música...
    }
    
    void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"Nombre de escena vacío. Por favor, asigna la escena en el Inspector.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}