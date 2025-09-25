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
    [SerializeField] private float deathHeight = -50f; // Altura de muerte instantánea
    
    private Rigidbody playerRigidbody;
    
    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            Debug.LogError("FallDetector necesita un Rigidbody en el jugador");
        }
    }
    
    void Update()
    {
        CheckGrounded();
        CheckDeathHeight();
    }
    
    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask);
        
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
        Debug.Log("Iniciando caída desde altura: " + fallStartHeight);
    }
    
    void Landing()
    {
        isFalling = false;
        float fallDistance = fallStartHeight - transform.position.y;
        
        if (enableFallDamage && TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.ProcessFallDamage(fallDistance);
        }
        
        Debug.Log($"Aterrizaje - Distancia de caída: {fallDistance}m");
    }
    
    void CheckDeathHeight()
    {
        if (transform.position.y < deathHeight)
        {
            // Muerte instantánea por caída al vacío
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.LoseTime(TimeLifeManager.Instance.CurrentTime);
            }
            Debug.Log("¡Caída mortal!");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
    
    // Métodos públicos para otros sistemas
    public bool IsCurrentlyGrounded() => isGrounded;
    public bool IsCurrentlyFalling() => isFalling;
    public float GetCurrentFallDistance() => isFalling ? fallStartHeight - transform.position.y : 0f;
}