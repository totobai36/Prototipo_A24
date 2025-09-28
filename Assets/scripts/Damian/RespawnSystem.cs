using UnityEngine;

public class RespawnSystem : MonoBehaviour
{
    [Header("Configuración de Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private float timePenalty = 15f;
    
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private FallDetector fallDetector;
    
    [Header("Configuración Simple")]
    [SerializeField] private float heightOffset = 0.0f; // CERO - directamente en el suelo
    
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
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Buscar jugador
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
                Debug.Log($"Jugador encontrado: {player.name}");
            }
        }
        
        // Buscar FallDetector
        if (fallDetector == null && player != null)
        {
            fallDetector = player.GetComponent<FallDetector>();
        }
        
        // Establecer posición inicial
        if (player != null)
        {
            lastSafePosition = player.position;
            hasSafePosition = true;
            Debug.Log($"Posición inicial establecida: {lastSafePosition}");
        }
    }
    
    void Update()
    {
        if (player == null || isRespawning) return;
        
        // Actualizar posición segura cuando está en el suelo
        if (fallDetector != null && fallDetector.IsCurrentlyGrounded() && !fallDetector.IsCurrentlyFalling())
        {
            lastSafePosition = player.position;
            hasSafePosition = true;
        }
    }
    
    public void TriggerRespawn()
    {
        if (isRespawning) 
        {
            Debug.Log("Respawn ya en proceso - ignorando");
            return;
        }
        
        Debug.Log("=== INICIANDO RESPAWN SIMPLE ===");
        StartCoroutine(RespawnCoroutine());
    }
    
    System.Collections.IEnumerator RespawnCoroutine()
    {
        isRespawning = true;
        
        if (!hasSafePosition)
        {
            lastSafePosition = Vector3.zero;
        }
        
        // FORZAR heightOffset a 0 si es muy grande
        if (heightOffset > 1f)
        {
            heightOffset = 0.0f;
            Debug.LogWarning("HeightOffset era demasiado grande, forzado a 0");
        }
        
        // Calcular punto de respawn
        Vector3 respawnPosition = lastSafePosition + Vector3.up * heightOffset;
        
        Debug.Log($"RESPAWN SIMPLE:");
        Debug.Log($"- HeightOffset usado: {heightOffset}");
        Debug.Log($"- Posición actual jugador: {player.position}");
        Debug.Log($"- Última posición segura: {lastSafePosition}");
        Debug.Log($"- Punto de respawn: {respawnPosition}");
        
        // Detener jugador
        var rigidbody = player.GetComponent<Rigidbody>();
        var playerController = player.GetComponent<CharacterController>();
        
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }
        
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // TELEPORTAR
        player.position = respawnPosition;
        Debug.Log($"Jugador teleportado a: {player.position}");
        
        // Esperar
        yield return new WaitForSeconds(respawnDelay);
        
        // Penalización
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.LoseTime(timePenalty);
            Debug.Log($"Penalización: -{timePenalty}s");
        }
        
        // Reactivar
        if (rigidbody != null)
        {
            rigidbody.isKinematic = false;
        }
        
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Resetear flag
        if (fallDetector != null)
        {
            fallDetector.ResetRespawnFlag();
        }
        
        Debug.Log("=== RESPAWN COMPLETADO ===");
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
            Gizmos.DrawWireSphere(lastSafePosition + Vector3.up * heightOffset, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(lastSafePosition, Vector3.one * 0.3f);
        }
    }
}