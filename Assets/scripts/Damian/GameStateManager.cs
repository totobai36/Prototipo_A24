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
    // CONFIGURACIÓN GENERAL
    // =======================================================
    [Header("Escenas")]
    [SerializeField] private string mainMenuScene = "Inicio";
    [SerializeField] private string tutorialScene = "Level Tuto";
    [SerializeField] private string level1Scene = "Level 1";
    [SerializeField] private string gameOverSceneName = "Derrota";
    [SerializeField] private string victorySceneName = "Victoria";

    [Header("Transiciones")]
    [Tooltip("Tiempo de espera antes de cambiar de escena (para reproducir SFX o animación)")]
    [SerializeField] private float transitionDelay = 1.0f;

    [Header("Referencias opcionales")]
    [SerializeField] private Switch mainSwitch; // si lo usas en niveles con switch
    [SerializeField] private AudioClip musicaExploracion;
    [SerializeField] private AudioClip musicaCountdown;
    [SerializeField] private AudioClip musicaVictoria;
    [SerializeField] private AudioClip musicaDerrota;

    public static GameStateManager Instance { get; private set; }

    // =======================================================
    // CICLO DE VIDA
    // =======================================================
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
        // Suscripción al switch si existe
        if (mainSwitch != null)
        {
            mainSwitch.OnSwitchActivated.AddListener(OnSwitchActivated);
            Debug.Log("[GameStateManager] Suscripción al Switch realizada con éxito.");
        }

        // Inicializar estado
        SetGameState(currentState);
    }

    // =======================================================
    // CAMBIO DE ESTADO
    // =======================================================
    private void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);

        Debug.Log($"[GameStateManager] Estado cambiado a: {currentState}");
    }

    public GameState GetCurrentState() => currentState;

    // =======================================================
    // EVENTOS PRINCIPALES DE JUEGO
    // =======================================================
    public void OnSwitchActivated()
    {
        if (currentState == GameState.Exploration)
        {
            Debug.Log("[GameStateManager] Switch activado. Iniciando cuenta regresiva...");
            SetGameState(GameState.CountdownActive);
            TriggerLevelTransformation();
            ChangeMusic(musicaCountdown, true);
        }
    }

    // Llamado por TimeLifeManager cuando se acaba el tiempo
    public void OnCountdownEnd()
    {
        if (currentState == GameState.CountdownActive)
        {
            Debug.Log("[GameStateManager] Tiempo agotado. Game Over.");
            OnGameOver();
        }
    }

    public void OnGameOver()
    {
        if (currentState == GameState.Victory) return;

        SetGameState(GameState.GameOver);
        Debug.Log("[GameStateManager] Game Over. Cargando escena de derrota...");
        StartCoroutine(LoadSceneAfter(gameOverSceneName, transitionDelay));

        // Audio
        AudioManager.Instance?.PlayDefeat(1f);
    }

    public void OnVictory()
    {
        SetGameState(GameState.Victory);
        Debug.Log("[GameStateManager] ¡Victoria! Cargando escena...");
        StartCoroutine(LoadSceneAfter(victorySceneName, transitionDelay));

        AudioManager.Instance?.PlayVictory(1f);
    }

    // =======================================================
    // CARGA DE ESCENAS
    // =======================================================
    public void LoadMainMenu() => StartCoroutine(LoadSceneAfter(mainMenuScene, 0f));
    public void LoadTutorial() => StartCoroutine(LoadSceneAfter(tutorialScene, 0f));
    public void LoadLevel1() => StartCoroutine(LoadSceneAfter(level1Scene, 0f));

    private IEnumerator LoadSceneAfter(string sceneName, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[GameStateManager] Nombre de escena vacío. No se puede cargar.");
            yield break;
        }

        SceneManager.LoadScene(sceneName);

        // Cambiar música automáticamente por escena
        var am = AudioManager.Instance;
        if (am != null)
        {
            if (sceneName == victorySceneName && musicaVictoria != null)
                am.PlayMusic(musicaVictoria, 1f, false);
            else if (sceneName == gameOverSceneName && musicaDerrota != null)
                am.PlayMusic(musicaDerrota, 1f, false);
            else if (sceneName == mainMenuScene && musicaExploracion != null)
                am.PlayMusic(musicaExploracion, 1f, true);
        }
    }

    // =======================================================
    // AUXILIARES
    // =======================================================
    void TriggerLevelTransformation()
    {
        // Ejemplo: animaciones, cambio de materiales, etc.
        Debug.Log("[GameStateManager] Transformando el nivel...");
    }

    void ChangeMusic(AudioClip clip, bool loop = true)
    {
        if (AudioManager.Instance && clip != null)
            AudioManager.Instance.PlayMusic(clip, 1f, loop);
    }
}
