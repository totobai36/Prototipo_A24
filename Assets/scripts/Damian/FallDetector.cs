using UnityEngine;

public class FallDetector : MonoBehaviour
{
    [Header("Detección de Caídas")]
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private bool isGrounded;
    [SerializeField] private float fallStartHeight;
    [SerializeField] private bool isFalling;
    
    [Header("Configuración de Daño")]
    [SerializeField] private bool enableFallDamage = true;
    [SerializeField] private float deathHeight = -50f;
    
    [Header("Sistema de Respawn")]
    [SerializeField] private bool enableRespawn = true;
    [SerializeField] private float maxSafeFallDistance = 15f;
    
    private Rigidbody playerRigidbody;
    private bool hasTriggeredRespawn = false;
    
    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            Debug.LogError("FallDetector necesita un Rigidbody en el jugador");
        }
    }

    [System.Obsolete]
    void Update()
    {
        CheckGrounded();
        
        // SOLO verificar death height si no estamos en proceso de respawn
        if (!RespawnSystem.Instance || !RespawnSystem.Instance.IsRespawning())
        {
            CheckDeathHeight();
        }
    }

    [System.Obsolete]
    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask);
        
        // RESETEAR FLAG solo cuando esté completamente en el suelo Y no cayendo
        if (isGrounded && !isFalling)
        {
            hasTriggeredRespawn = false;
            //Debug.Log("Flag de respawn reseteado - jugador en suelo seguro");
        }
        
        // Comenzar a caer
        if (wasGrounded && !isGrounded && !isFalling)
        {
            StartFalling();
        }
        
        // Aterrizar
        if (!wasGrounded && isGrounded && isFalling)
        {
            Landing();
        }
    }
    
    void StartFalling()
    {
        isFalling = true;
        fallStartHeight = transform.position.y;
        Debug.Log($"Iniciando caída desde altura: {fallStartHeight}");
    }

    [System.Obsolete]
    void Landing()
    {
        isFalling = false;
        float fallDistance = fallStartHeight - transform.position.y;
        
        Debug.Log($"Aterrizaje - Distancia de caída: {fallDistance}m");
        
        // Verificar si debe activar respawn por caída mortal
        float potentialDamage = CalculateFallDamage(fallDistance);
        bool wouldCauseDeath = TimeLifeManager.Instance != null && 
                              potentialDamage >= TimeLifeManager.Instance.CurrentTime;
        
        if (enableRespawn && RespawnSystem.Instance != null && 
            fallDistance > maxSafeFallDistance && wouldCauseDeath && !hasTriggeredRespawn)
        {
            Debug.Log($"CAÍDA MORTAL POR DISTANCIA: {fallDistance}m (>{maxSafeFallDistance}m) causaría {potentialDamage}s de daño");
            hasTriggeredRespawn = true;
            RespawnSystem.Instance.TriggerRespawn();
        }
        else if (enableFallDamage && TimeLifeManager.Instance != null)
        {
            // Caída normal - aplicar daño
            TimeLifeManager.Instance.ProcessFallDamage(fallDistance);
        }
    }
    
    float CalculateFallDamage(float fallDistance)
    {
        if (fallDistance < 3f) return 0f;
        return (fallDistance - 3f) * 2f;
    }

    [System.Obsolete]
    void CheckDeathHeight()
    {
        if (transform.position.y < deathHeight && !hasTriggeredRespawn)
        {
            Debug.Log($"CAÍDA MORTAL POR ALTURA: Y={transform.position.y} < {deathHeight}");
            hasTriggeredRespawn = true;
            
            if (enableRespawn && RespawnSystem.Instance != null)
            {
                Debug.Log("Activando respawn por caída al vacío");
                RespawnSystem.Instance.TriggerRespawn();
            }
            else
            {
                Debug.LogError($"No se puede activar respawn: enableRespawn={enableRespawn}, RespawnSystem.Instance={RespawnSystem.Instance != null}");
                
                // Muerte instantánea
                if (TimeLifeManager.Instance != null)
                {
                    TimeLifeManager.Instance.LoseTime(TimeLifeManager.Instance.CurrentTime);
                }
                Debug.Log("Muerte instantánea por caída al vacío");
            }
        }
    }
    
    // NUEVO MÉTODO para resetear el flag externamente
    public void ResetRespawnFlag()
    {
        hasTriggeredRespawn = false;
        Debug.Log("Flag de respawn reseteado externamente");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
        
        // Línea de muerte
        Gizmos.color = Color.red;
        Vector3 deathLine = new Vector3(transform.position.x, deathHeight, transform.position.z);
        Gizmos.DrawLine(deathLine - Vector3.right * 5f, deathLine + Vector3.right * 5f);
        Gizmos.DrawLine(deathLine - Vector3.forward * 5f, deathLine + Vector3.forward * 5f);
    }
    
    // Métodos públicos
    public bool IsCurrentlyGrounded() => isGrounded;
    public bool IsCurrentlyFalling() => isFalling;
    public float GetCurrentFallDistance() => isFalling ? fallStartHeight - transform.position.y : 0f;

    [System.Obsolete]
    public void ForceRespawn()
    {
        if (enableRespawn && RespawnSystem.Instance != null && !hasTriggeredRespawn)
        {
            hasTriggeredRespawn = true;
            RespawnSystem.Instance.TriggerRespawn();
        }
    }
}