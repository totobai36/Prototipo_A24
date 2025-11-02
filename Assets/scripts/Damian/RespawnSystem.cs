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
            
            // Suscribirse para reasignar referencias
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
    
    // Este método se ejecuta CADA VEZ que se carga una escena.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Limpia las referencias y flags antiguas
        player = null; 
        fallDetector = null;
        isRespawning = false;
        hasSafePosition = false; 

        // ⭐️ Iniciar la Coroutine de espera para evitar condiciones de carrera
        StartCoroutine(WaitForAndAssignPlayer());
    }

    private IEnumerator WaitForAndAssignPlayer()
    {
        // Esperar un frame (o hasta que el Player aparezca)
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
            
            // ⭐️ ÚLTIMO CAMBIO CLAVE (Línea 86 aprox): Usar GetComponentInChildren para máxima seguridad.
            // Esto funcionará sin importar si está en la raíz o en un objeto hijo.
            fallDetector = player.GetComponentInChildren<FallDetector>();

            if (fallDetector != null)
            {
                // Inicializar la posición segura
                SetSafePosition(player.position); 
                fallDetector.ResetRespawnFlag(); 
                Debug.Log($"[RespawnSystem] Referencias reasignadas con éxito después de {attempts} intentos.");
            }
            else
            {
                Debug.LogError("[RespawnSystem] ERROR CRÍTICO: FallDetector no encontrado en el objeto 'Player' ni en sus hijos.");
            }
        }
        else
        {
            Debug.LogError("[RespawnSystem] El objeto 'Player' con Tag no fue encontrado después de múltiples intentos.");
        }
    }
    
    public void Respawn()
    {
        // Comprobación de nulidad para evitar que la caída infinita siga gastando tiempo
        if (isRespawning || !hasSafePosition || player == null || fallDetector == null) 
        {
             // Debug.LogWarning("[RespawnSystem] Respawn abortado: Faltan referencias o ya está en curso.");
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
        // Usamos GetComponent porque estos componentes sí deberían estar en la raíz del objeto 'player'
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