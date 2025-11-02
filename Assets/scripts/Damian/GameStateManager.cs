using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    public enum GameState { Exploration, CountdownActive, GameOver, Victory }
    [SerializeField] private GameState currentState = GameState.Exploration;

    [HideInInspector] public UnityEvent<GameState> OnGameStateChanged = new UnityEvent<GameState>();

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
            
            // Suscripción al evento de carga de escena es CRÍTICA
            SceneManager.sceneLoaded -= OnSceneLoaded; 
            SceneManager.sceneLoaded += OnSceneLoaded; 
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // ❌ ELIMINAMOS EL MÉTODO Start() y su lógica de suscripción.

    // Este método se ejecuta CADA VEZ que se carga una escena.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Resetear el estado al cargar la escena de juego
        if (scene.name != gameOverSceneName && scene.name != victorySceneName)
        {
            SetGameState(GameState.Exploration); 
            SubscribeToSwitch(); // Vuelve a buscar y suscribirse al nuevo Switch
        }
    }
    
    // Método para buscar y suscribirse al Switch de la escena
    private void SubscribeToSwitch()
    {
        // ✅ SOLUCIÓN AL ERROR: La búsqueda dinámica con FindObjectOfType
        Switch mainSwitchInScene = FindAnyObjectByType<Switch>(); 

        if (mainSwitchInScene != null)
        {
            // Limpiar suscripciones anteriores para evitar duplicados.
            mainSwitchInScene.OnSwitchActivated.RemoveListener(OnSwitchActivated); 
            mainSwitchInScene.OnSwitchActivated.AddListener(OnSwitchActivated);
            Debug.Log("Suscripción al Switch realizada con éxito."); 
        }
        else
        {
            // Ahora es un Warning (advertencia) y no un Error, ya que la lógica persistente sigue viva
            Debug.LogWarning("Switch principal no encontrado en la escena. Asegúrese de que esté presente y que su orden de ejecución permita que los managers se inicien primero.");
        }
    }

    private void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnGameStateChanged?.Invoke(currentState); 
        
        Debug.Log($"[GameStateManager] Estado de juego cambiado a: {currentState}");
    }

    private void OnSwitchActivated()
    {
        if (currentState == GameState.Exploration)
        {
            Debug.Log("Switch activado. Iniciando Cuenta Regresiva.");
            SetGameState(GameState.CountdownActive);
            
            // ✅ INICIAMOS EL TIMER AQUÍ
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.StartTimer(); 
            }
            
            TriggerLevelTransformation();
            ChangeMusicToIntense();
        }
    }

    public void OnCountdownEnd()
    {
        if (currentState == GameState.CountdownActive)
        {
            Debug.Log("El tiempo ha terminado. Game Over.");
            OnGameOver(); 
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