using UnityEngine;
using System.Collections;
using DiasGames.Components; 
using UnityEngine.SceneManagement; 

public class RespawnSystem : MonoBehaviour
{
    [Header("Configuración de Respawn")]
    [SerializeField] private float respawnDelay = 0.1f;
    [SerializeField] private float timePenalty = 15f;
    
    [Header("Referencias")]
    // ⚠️ Dejar estos campos sin asignar en el Inspector
    [SerializeField] private Transform player;
    [SerializeField] private FallDetector fallDetector; 
    
    [Header("Configuración Simple")]
    [SerializeField] private float heightOffset = 0.0f; 
    
    private bool isRespawning = false;
    private Vector3 lastSafePosition;
    private bool hasSafePosition = false;
    
    public static RespawnSystem Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // ⭐️ CLAVE: Suscribirse al evento de carga de escena para reasignar el Player
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

    // ❌ ELIMINAMOS EL MÉTODO Start(). La lógica ahora está aquí:
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Limpia las referencias y flags antiguas
        player = null; 
        fallDetector = null;
        isRespawning = false;
        hasSafePosition = false; 

        // 2. Iniciar la Coroutine de espera para evitar condiciones de carrera
        StartCoroutine(WaitForAndAssignPlayer());
    }

    private IEnumerator WaitForAndAssignPlayer()
    {
        // Esperar un máximo de 10 frames para que el Player aparezca
        int maxAttempts = 10;
        int attempts = 0;
        GameObject playerGO = null;

        while (playerGO == null && attempts < maxAttempts)
        {
            playerGO = GameObject.FindGameObjectWithTag("Player");
            attempts++;
            yield return null; // Esperar al siguiente frame
        }

        if (playerGO != null)
        {
            player = playerGO.transform;
            
            // ⭐️ USO ROBUSTO: Buscar en el objeto principal y en todos los hijos
            fallDetector = player.GetComponentInChildren<FallDetector>();

            if (fallDetector != null)
            {
                // Inicializar la posición segura
                SetSafePosition(player.position); 
                fallDetector.ResetRespawnFlag(); 
                Debug.Log($"[RespawnSystem] Referencias reasignadas con éxito después de {attempts} intentos. ¡Listo para respawnear!");
            }
            else
            {
                // Si el error persiste aquí, revisa la ortografía de "FallDetector" o la jerarquía.
                Debug.LogError("[RespawnSystem] ERROR CRÍTICO: FallDetector no encontrado en el objeto 'Player' ni en sus hijos.");
            }
        }
        else
        {
            Debug.LogError("[RespawnSystem] El objeto 'Player' con Tag no fue encontrado después de múltiples intentos. Respawn inactivo.");
        }
    }
    
    public void Respawn()
    {
        // Comprobación de nulidad para evitar la caída infinita
        if (isRespawning || !hasSafePosition || player == null || fallDetector == null) 
        {
             return; 
        }

        StartCoroutine(PerformRespawn(lastSafePosition));
    }

    public void SetSafePosition(Vector3 position)
    {
        lastSafePosition = position + Vector3.up * heightOffset;
        hasSafePosition = true;
    }

    private IEnumerator PerformRespawn(Vector3 respawnPosition)
    {
        if (player == null || fallDetector == null)
        {
             yield break;
        }

        isRespawning = true;
        
        // --- 1. Obtener Componentes del Personaje ---
        MovementComponent movementComponent = player.GetComponent<MovementComponent>(); 
        CharacterController charController = player.GetComponent<CharacterController>();  

        // --- 2. Desactivar Movimiento y Control ---
        if (movementComponent != null) movementComponent.enabled = false; 
        if (charController != null) charController.enabled = false;
        
        // --- 3. TELEPORTAR ---
        player.position = respawnPosition;
        
        // --- 4. Esperar y Penalizar ---
        yield return new WaitForSeconds(respawnDelay);
        
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.LoseTime(timePenalty);
        }
        
        // --- 5. Reactivar Componentes ---
        if (charController != null) charController.enabled = true;
        if (movementComponent != null) movementComponent.enabled = true; 
        
        // --- 6. Finalizar Respawn ---
        // Solo llamamos a ResetRespawnFlag si la referencia es válida
        if(fallDetector != null)
        {
            fallDetector.ResetRespawnFlag(); 
        }
        
        isRespawning = false;
    }
    
    public bool IsRespawning()
    {
        return isRespawning;
    }
    
    void OnDrawGizmosSelected()
    {
        if (hasSafePosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastSafePosition, 0.5f);
        }
    }
}