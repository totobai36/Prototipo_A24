using UnityEngine;

public class ExtractionPoint : MonoBehaviour
{
    // =======================================================
    // ENUM PARA SELECCIONAR LA ACCIÓN
    // =======================================================
    public enum ExtractionAction 
    { 
        LoadNextLevel, // Llama a LoadNextLevel() -> Lleva a Level 1
        OnVictory      // Llama a OnVictory() -> Lleva a la pantalla de Victoria
    }

    [Header("Configuración de Extracción")]
    [Tooltip("Define la acción a realizar al completar la extracción (pasar de nivel o ganar el juego).")]
    [SerializeField] private ExtractionAction actionOnSuccess = ExtractionAction.OnVictory;


    // =======================================================
    // PROPIEDADES VISUALES
    // =======================================================
    [Header("Efectos Visuales")]
    [Tooltip("El componente Renderer de la malla del punto de extracción (ej: MeshRenderer).")]
    [SerializeField] private Renderer extractionRenderer; 
    
    [Tooltip("Material para el estado de Exploración (ROJO).")]
    [SerializeField] private Material inactiveMaterial; 
    
    [Tooltip("Material para el estado de Cuenta Regresiva Activa (VERDE).")]
    [SerializeField] private Material activeMaterial;   

    void Awake()
    {
        if (extractionRenderer == null)
        {
            extractionRenderer = GetComponent<Renderer>();
        }
        
        if (extractionRenderer != null && inactiveMaterial != null)
        {
            extractionRenderer.material = inactiveMaterial;
        }
    }
    
    void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged.AddListener(HandleGameStateChange);
        }
        else
        {
            Debug.LogError("ExtractionPoint: No se encontró GameStateManager.");
        }
    }

    private void HandleGameStateChange(GameStateManager.GameState newState)
    {
        if (extractionRenderer != null)
        {
            if (newState == GameStateManager.GameState.CountdownActive && activeMaterial != null)
            {
                extractionRenderer.material = activeMaterial;
            }
            else if (newState == GameStateManager.GameState.Exploration && inactiveMaterial != null)
            {
                extractionRenderer.material = inactiveMaterial;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (GameStateManager.Instance == null)
        {
            return;
        }

        // VALIDACIÓN CLAVE: Solo permitir la extracción si el temporizador está activo
        if (GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.CountdownActive)
        {
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.StopTimer();
            }

            // ⭐️ Llama al método según la configuración del Inspector.
            switch (actionOnSuccess)
            {
                case ExtractionAction.LoadNextLevel:
                    Debug.Log("Tutorial completado. Llamando a LoadNextLevel.");
                    GameStateManager.Instance.LoadNextLevel(); 
                    break;
                case ExtractionAction.OnVictory:
                    Debug.Log("Nivel completado. Llamando a OnVictory.");
                    GameStateManager.Instance.OnVictory();
                    break;
            }
            
            // Opcional: Desactivar el Player
            other.gameObject.SetActive(false); 
        }
        else
        {
            Debug.Log("Punto de Extracción ignorado. La cuenta regresiva aún no ha comenzado.");
        }
    }
}