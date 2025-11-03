using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    // =======================================================
    // ESTADOS DE JUEGO
    // =======================================================
    public enum GameState { Exploration, CountdownActive, GameOver, Victory }
    [SerializeField] private GameState currentState = GameState.Exploration;

    [HideInInspector] public UnityEvent<GameState> OnGameStateChanged = new UnityEvent<GameState>();

    // =======================================================
    // CONFIGURACIÓN DE ESCENAS Y FLUJO
    // =======================================================
    [Header("Flujo de Escenas")]
    [SerializeField] private string mainMenuScene = "Inicio"; 
    // ⚠️ Dejamos el nombre como lo tienes configurado, asumiendo que es correcto.
    [SerializeField] private string tutorialScene = "Level Tuto"; 
    [SerializeField] private string level1Scene = "Level 1"; 
    [SerializeField] private string gameOverSceneName = "Derrota"; 
    [SerializeField] private string victorySceneName = "Victoria"; 

    [Header("Transiciones")]
    [Tooltip("Tiempo de espera antes de cambiar de escena (para reproducir SFX o animación)")]
    [SerializeField] private float transitionDelay = 1.0f;

    [Header("Referencias Opcionales")]
    // ⚠️ mainSwitch ahora es buscado en código al cargar la escena, pero se mantiene para debug.
    [SerializeField] private Switch mainSwitch; 
    [SerializeField] private AudioClip musicaExploracion;
    [SerializeField] private AudioClip musicaIntensa;
    [SerializeField] private AudioClip musicaVictoria;
    [SerializeField] private AudioClip musicaDerrota;
    
    // =======================================================
    // INSTANCIA Y AWAKE
    // =======================================================
    public static GameStateManager Instance { get; private set; }

    private bool isTransitioning = false; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // ⭐️ CLAVE 1: Suscribirse al evento de carga de escena de Unity para re-inicializar.
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
        // Limpieza al destruir el objeto
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (mainSwitch != null)
        {
            mainSwitch.OnSwitchActivated.RemoveListener(OnSwitchActivated);
        }
    }

    // El método Start() ya no es necesario, toda la inicialización va en OnSceneLoaded.
    
    // ⭐️ CLAVE 2: Lógica que corre CADA VEZ que se carga una escena.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Limpiar referencia y suscripción anterior.
        if (mainSwitch != null)
        {
            mainSwitch.OnSwitchActivated.RemoveListener(OnSwitchActivated);
            mainSwitch = null; 
        }

        // 2. Intentar encontrar el Switch en la nueva escena (si existe).
        if (scene.name == tutorialScene || scene.name == level1Scene)
        {
            mainSwitch = FindAnyObjectByType<Switch>();

            if (mainSwitch != null)
            {
                // 3. Suscribirse al nuevo Switch.
                mainSwitch.OnSwitchActivated.AddListener(OnSwitchActivated);
                Debug.Log($"[GameStateManager] Referencia de Switch reasignada para la escena: {scene.name}.");
            }
            
            // 4. Resetear el estado y el Timer del nivel
            SetGameState(GameState.Exploration);
            
            if (TimeLifeManager.Instance != null)
            {
                // ⭐️ CLAVE 3: Asegurar que el temporizador esté en estado inicial (detenido y tiempo máximo).
                TimeLifeManager.Instance.ResetTimer();
                Debug.Log("[GameStateManager] TimeLifeManager reseteado para nuevo nivel.");
            }
        }
    }
    
    // =======================================================
    // MÉTODOS DE FLUJO PÚBLICOS
    // =======================================================

    // LLamado por Menu.cs. Inicia el juego yendo al Tutorial
    public void StartGameSequence()
    {
        // SetGameState(GameState.Exploration); // Esto lo hace OnSceneLoaded
        LoadScene(tutorialScene);
    }

    // LLamado por ExtractionPoint.cs en el Tutorial. Pasa al Level 1.
    public void LoadNextLevel()
    {
        // SetGameState(GameState.Exploration); // Esto lo hace OnSceneLoaded
        LoadScene(level1Scene);
    }
    
    // LLamado por pantallas de Game Over/Victory. Vuelve al Menú.
    public void ReturnToMainMenu()
    {
        SetGameState(GameState.Exploration); 
        LoadScene(mainMenuScene);
    }

    // =======================================================
    // LÓGICA DE ACTIVACIÓN
    // =======================================================

    private void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }

    // Lógica que se dispara cuando el Switch del nivel es activado
    public void OnSwitchActivated()
    {
        if (currentState == GameState.Exploration)
        {
            SetGameState(GameState.CountdownActive);
            
            // Esta línea ahora debería funcionar porque OnSceneLoaded ya reseteó el Timer
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.StartTimer(); 
                Debug.Log("[GameStateManager] Temporizador iniciado por activación del Switch.");
            }
            else
            {
                Debug.LogError("[GameStateManager] TimeLifeManager.Instance es NULL. El temporizador no pudo iniciar.");
            }
            
            TriggerLevelTransformation();
            ChangeMusic(musicaIntensa, true);
        }
    }

    // Lógica para OnCountdownEnd (Llamada por TimeLifeManager)
    public void OnCountdownEnd()
    {
        if (currentState == GameState.CountdownActive)
        {
            Debug.Log("[GameStateManager] El tiempo ha terminado. Game Over.");
            OnGameOver(); 
        }
    }
    
    public void OnGameOver()
    {
        if (currentState != GameState.Victory && currentState != GameState.GameOver) 
        {
            SetGameState(GameState.GameOver);
            LoadScene(gameOverSceneName);
        }
    }

    public void OnVictory()
    {
        if (currentState != GameState.Victory && currentState != GameState.GameOver)
        {
            SetGameState(GameState.Victory);
            LoadScene(victorySceneName);
        }
    }

    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    // =======================================================
    // LÓGICA DE CARGA DE ESCENA (INTERNA)
    // =======================================================
    
    void LoadScene(string sceneName, float delay = -1f)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[GameStateManager] Ya hay una transición en curso.");
            return;
        }

        float actualDelay = (delay >= 0) ? delay : transitionDelay; 
        StartCoroutine(LoadSceneAfter(sceneName, actualDelay));
    }

    private IEnumerator LoadSceneAfter(string sceneName, float delay)
    {
        isTransitioning = true;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[GameStateManager] Nombre de escena vacío. No se puede cargar.");
            isTransitioning = false;
            yield break;
        }

        // SceneManager.sceneLoaded se encargará de re-inicializar el Switch/Timer
        SceneManager.LoadScene(sceneName); 
        
        isTransitioning = false;
    }
    
    void TriggerLevelTransformation()
    {
        Debug.Log("[GameStateManager] Transformando el nivel...");
    }

    void ChangeMusic(AudioClip clip, bool loop = true)
    {
        // Lógica de AudioManager
    }
}