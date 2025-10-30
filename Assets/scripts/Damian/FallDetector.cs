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
        
        // Inicializar la última posición segura al comienzo
        if (RespawnSystem.Instance != null)
        {
            RespawnSystem.Instance.SetSafePosition(transform.position);
        }
    }

    void Update()
    {
        CheckGrounded();
        
        // Solo verificar death height si el RespawnSystem no está en proceso de respawn
        if (RespawnSystem.Instance == null || !RespawnSystem.Instance.IsRespawning())
        {
            CheckDeathHeight(); 
        }
    }

    void CheckGrounded()
    {
        // Usamos una simple Raycast para este ejemplo:
        bool nowGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask);

        if (nowGrounded && !isGrounded)
        {
            isGrounded = true;
            if (isFalling && enableFallDamage)
            {
                float totalFallDistance = fallStartHeight - transform.position.y;
                if (TimeLifeManager.Instance != null)
                {
                    TimeLifeManager.Instance.ProcessFallDamage(totalFallDistance);
                }
            }
            isFalling = false;
            // Al aterrizar, actualizamos la posición segura
            if (RespawnSystem.Instance != null)
            {
                RespawnSystem.Instance.SetSafePosition(transform.position);
            }
        }
        else if (!nowGrounded && isGrounded)
        {
            isGrounded = false;
            isFalling = true;
            fallStartHeight = transform.position.y;
        }
    }

    void CheckDeathHeight()
    {
        if (transform.position.y < deathHeight)
        {
            // --- CORRECCIÓN LÍNEA 95 ---
            if (enableRespawn && RespawnSystem.Instance != null && !hasTriggeredRespawn)
            {
                // Aseguramos la llamada al método público correcto: Respawn()
                RespawnSystem.Instance.Respawn(); 
                hasTriggeredRespawn = true; 
            }
            else if (!enableRespawn && TimeLifeManager.Instance != null)
            {
                // Muerte instantánea si no hay respawn
                TimeLifeManager.Instance.LoseTime(TimeLifeManager.Instance.CurrentTime);
            }
            Debug.Log("Muerte instantánea por caída al vacío");
        }
    }
    
    // --- CORRECCIÓN LÍNEA 159 ---
    // Este método es necesario para que RespawnSystem restablezca la lógica de caída.
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
}