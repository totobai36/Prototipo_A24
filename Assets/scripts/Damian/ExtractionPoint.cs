using UnityEngine;

public class ExtractionPoint : MonoBehaviour
{
    // =======================================================
    // NUEVAS PROPIEDADES PARA CONTROL VISUAL
    // =======================================================
    [Header("Efectos Visuales")]
    [Tooltip("El componente Renderer de la malla del punto de extracción (ej: MeshRenderer).")]
    [SerializeField] private Renderer extractionRenderer; 
    
    [Tooltip("Material para el estado de Exploración (ROJO).")]
    [SerializeField] private Material inactiveMaterial; 
    
    [Tooltip("Material para el estado de Cuenta Regresiva Activa (VERDE).")]
    [SerializeField] private Material activeMaterial;   
    // =======================================================

    void Awake()
    {
        // 1. Obtener la referencia al Renderer si no está asignada en el Inspector.
        if (extractionRenderer == null)
        {
            extractionRenderer = GetComponent<Renderer>();
        }
        
        // 2. Establecer el estado visual inicial (Rojo por defecto)
        if (extractionRenderer != null && inactiveMaterial != null)
        {
            extractionRenderer.material = inactiveMaterial;
        }
    }
    
    void Start()
    {
        // Suscribirse al evento de cambio de estado del juego
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged.AddListener(HandleGameStateChange);
            
            // Forzar una comprobación inicial por si Start se llama tarde
            HandleGameStateChange(GameStateManager.Instance.GetCurrentState());
            
            Debug.Log("ExtractionPoint: Suscrito al evento de cambio de estado.");
        }
        else
        {
            Debug.LogError("ExtractionPoint: No se encontró GameStateManager.Instance. El cambio de color no funcionará.");
        }
    }
    
    void OnDestroy()
    {
        // Limpieza de la suscripción
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged.RemoveListener(HandleGameStateChange);
        }
    }

    /// <summary>
    /// Maneja el cambio de estado del juego y actualiza el material del punto.
    /// </summary>
    private void HandleGameStateChange(GameStateManager.GameState newState)
    {
        Debug.Log($"ExtractionPoint: Estado de juego cambiado a {newState}.");

        if (extractionRenderer == null) return;

        // Cambiar el material según el estado del juego
        if (newState == GameStateManager.GameState.CountdownActive)
        {
            if (activeMaterial != null)
            {
                extractionRenderer.material = activeMaterial;
                Debug.Log("ExtractionPoint: ¡Cambiado a material ACTIVO (Verde)!");
            }
        }
        else // Vuelve a rojo si es Exploration, GameOver o cualquier otro estado.
        {
             if (inactiveMaterial != null)
            {
                extractionRenderer.material = inactiveMaterial;
                Debug.Log("ExtractionPoint: Vuelto a material INACTIVO (Rojo).");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Validar que sea el jugador
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 2. Validar que la instancia del GameStateManager exista
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("ExtractionPoint: No se encontró GameStateManager.");
            return;
        }

        // 3. ¡VALIDACIÓN CLAVE! Solo ganar si el temporizador está activo (Verde).
        if (GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.CountdownActive)
        {
            // Ejecutar lógica de Victoria
            Debug.Log("CONDICIÓN DE VICTORIA CUMPLIDA: Countdown activo.");
            
            // Detener el timer (si existe)
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.StopTimer();
            }

            // Notificar al GameStateManager para cargar la escena de Victoria
            GameStateManager.Instance.OnVictory(); 
            
            // Opcional: Desactivar el Player
            other.gameObject.SetActive(false); 
        }
        else
        {
            // Si el estado es Exploration (Rojo), no pasa nada y se ignora la colisión.
            Debug.Log("Punto de Extracción ignorado. La cuenta regresiva aún no ha comenzado (estado: " + GameStateManager.Instance.GetCurrentState() + ").");
        }
    }
}