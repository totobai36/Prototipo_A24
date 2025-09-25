using UnityEngine;

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
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Suscribirse al switch (Principio de Inversión de Dependencia)
        if (mainSwitch != null)
        {
            mainSwitch.OnSwitchActivated.AddListener(OnSwitchActivated);
        }
        
        // Suscribirse a eventos del timer
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnGameOver.AddListener(OnGameOver);
        }
    }
    
   public void OnSwitchActivated() // ← Debe ser PUBLIC
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
            TimeLifeManager.Instance.StartTimer();
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
        // Notificar al sistema de música
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
