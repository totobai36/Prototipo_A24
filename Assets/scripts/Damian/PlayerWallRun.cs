using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerMov))]
public class PlayerWallRun : MonoBehaviour
{
    [Header("Referencias")]
    private Rigidbody rb;
    private PlayerMov playerMov;
    private Animator anim;

    [Header("WallRun Settings")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallRunDuration = 3f;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallStickForce = 50f;
    [SerializeField] private LayerMask wallLayer = -1;
    [SerializeField] private float wallCheckDistance = 1.2f;
    [SerializeField] private float wallRunGravity = 2f;
    [SerializeField] private float wallRunCooldown = 0.5f;

    // Estados internos
    private bool isWallRunning = false;
    private Vector3 wallNormal;
    private Vector3 wallForward;
    private float wallRunTimer = 0f;
    private float wallRunCooldownTimer = 0f;

    private bool wallLeft = false;
    private bool wallRight = false;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    
    // Propiedad pública para que otros scripts accedan al estado
    public bool IsWallRunning => isWallRunning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMov = GetComponent<PlayerMov>();
        anim = GetComponent<Animator>();

        if (playerMov == null)
            Debug.LogError("PlayerMov script no encontrado. Asegúrate de que esté en el mismo GameObject.");

        // FIX: Configurar wallLayer si no está configurado
        if (wallLayer.value == -1 || wallLayer.value == 0)
        {
            wallLayer = ~0; // Incluir todas las capas por defecto
        }
    }

    void Update()
    {
        if (wallRunCooldownTimer > 0f) 
            wallRunCooldownTimer -= Time.deltaTime;

        CheckForWalls();

        if (CanStartWallRun() && !isWallRunning)
        {
            StartWallRun();
        }
        else if (isWallRunning)
        {
            HandleWallRunState();
        }
    }

    void FixedUpdate()
    {
        if (isWallRunning)
        {
            WallRunMovement();
        }
    }

    // ===================================
    // LÓGICA DE DETECCIÓN Y ESTADO
    // ===================================

    private void CheckForWalls()
    {
        // FIX: Mejorar la detección de paredes con múltiples rayos
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        
        // Rayos principales
        wallLeft = Physics.Raycast(rayStart, -transform.right, out leftWallHit, wallCheckDistance, wallLayer);
        wallRight = Physics.Raycast(rayStart, transform.right, out rightWallHit, wallCheckDistance, wallLayer);
        
        // Rayos adicionales para mejor detección
        if (!wallLeft)
        {
            Vector3 leftDiagForward = (-transform.right + transform.forward * 0.5f).normalized;
            wallLeft = Physics.Raycast(rayStart, leftDiagForward, out leftWallHit, wallCheckDistance * 0.8f, wallLayer);
        }
        
        if (!wallRight)
        {
            Vector3 rightDiagForward = (transform.right + transform.forward * 0.5f).normalized;
            wallRight = Physics.Raycast(rayStart, rightDiagForward, out rightWallHit, wallCheckDistance * 0.8f, wallLayer);
        }
        
        // Debug visual (opcional - quitar en build final)
        Debug.DrawRay(rayStart, -transform.right * wallCheckDistance, wallLeft ? Color.red : Color.white);
        Debug.DrawRay(rayStart, transform.right * wallCheckDistance, wallRight ? Color.red : Color.white);
        
        if (isWallRunning && !wallLeft && !wallRight)
        {
            StopWallRun();
        }
    }

    private bool CanStartWallRun()
    {
        // FIX: Hacer wallrun más automático - solo necesita estar en el aire y moverse hacia adelante
        float forwardInput = Input.GetAxisRaw("Vertical");
        
        return (wallLeft || wallRight) &&
               !playerMov.EnSuelo && 
               forwardInput >= 0f &&  // Permitir wallrun sin input hacia adelante
               wallRunCooldownTimer <= 0f &&
               rb.linearVelocity.y <= 1f; // No empezar wallrun si está subiendo muy rápido
    }

    private bool ShouldStopWallRun()
    {
        float downInput = Input.GetAxisRaw("Vertical");
        
        // FIX: Mejorar condiciones para parar wallrun
        return downInput < -0.5f ||  // Solo parar si se presiona fuertemente hacia abajo
               wallRunTimer <= 0f ||
               playerMov.EnSuelo ||
               rb.linearVelocity.y < -10f; // Parar si cae muy rápido
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunDuration;
        
        // Determinar dirección de la pared
        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        
        // FIX: Calcular wallForward correctamente para ambos lados
        if (wallRight) // Pared a la derecha
        {
            wallForward = Vector3.Cross(Vector3.up, wallNormal).normalized;
        }
        else // Pared a la izquierda
        {
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;
        }
        
        // Verificar que siempre vamos hacia adelante relativo al jugador
        if (Vector3.Dot(wallForward, transform.forward) < 0f) 
            wallForward = -wallForward;

        // Resetear velocidad vertical y dar un pequeño impulso
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * 2f, ForceMode.Impulse); 
        
        if (anim) anim.SetBool("Wall", true);
        
        Debug.Log("WallRun iniciado: " + (wallRight ? "Derecha" : "Izquierda") + " - Dirección: " + wallForward);
    }

    private void HandleWallRunState()
    {
        if (ShouldStopWallRun())
        {
            StopWallRun();
            return;
        }

        // FIX: Saltar de la pared con espacio
        if (Input.GetKeyDown(KeyCode.Space))
        {
            WallJump();
        }
    }

    // ===================================
    // LÓGICA DE FÍSICA
    // ===================================

    private void WallRunMovement()
    {
        wallRunTimer -= Time.deltaTime;

        // FIX: Movimiento automático hacia adelante en la pared
        Vector3 targetVelocity = wallForward * wallRunSpeed;
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityError = targetVelocity - currentHorizontalVel;
        
        // Aplicar fuerza para mantener velocidad en la pared
        rb.AddForce(velocityError * 20f * Time.deltaTime, ForceMode.VelocityChange);

        // Fuerza para pegarse a la pared
        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Force);
        
        // FIX: Mejor control de gravedad - reducir gradualmente
        float gravityReduction = Mathf.Lerp(1f, wallRunGravity / -Physics.gravity.y, wallRunTimer / wallRunDuration);
        float gravityCancellation = -Physics.gravity.y * (1f - gravityReduction);
        rb.AddForce(Vector3.up * gravityCancellation, ForceMode.Acceleration); 
        
        // FIX: Rotación más suave hacia la pared
        if (playerMov.TurnSpeed > 0)
        {
            Quaternion targetRot = Quaternion.LookRotation(wallForward, Vector3.up);
            float rotationSpeed = playerMov.TurnSpeed * 0.5f; // Rotación más lenta en wallrun
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void WallJump()
    {
        StopWallRun(true); 
        
        // FIX: Mejor dirección de salto de pared
        Vector3 jumpDirection = (wallNormal * 0.7f + Vector3.up * 0.8f + wallForward * 0.3f).normalized;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(jumpDirection * wallJumpForce, ForceMode.Impulse);

        wallRunCooldownTimer = wallRunCooldown;
        
        Debug.Log("Wall Jump ejecutado");
    }

    private void StopWallRun(bool isWallJump = false)
    {
        if (isWallRunning)
        {
            isWallRunning = false;
            
            if (!isWallJump) 
                wallRunCooldownTimer = 0.2f; 
                
            if (anim) 
                anim.SetBool("Wall", false);

            // FIX: Mantener momentum al salir del wallrun
            Vector3 v = rb.linearVelocity;
            float forwardSpeed = Vector3.Dot(v, wallForward);
            
            if (forwardSpeed > 0) // Solo mantener velocidad si vamos hacia adelante
            {
                Vector3 newPlanarVelocity = wallForward * forwardSpeed; 
                rb.linearVelocity = new Vector3(newPlanarVelocity.x, v.y, newPlanarVelocity.z);
            }
            
            Debug.Log("WallRun terminado");
        }
    }
    
    // Método para debug - llamar desde el Inspector o desde código
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            
            Gizmos.DrawWireSphere(rayStart, 0.1f);
            Gizmos.DrawRay(rayStart, -transform.right * wallCheckDistance);
            Gizmos.DrawRay(rayStart, transform.right * wallCheckDistance);
            
            if (isWallRunning)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, wallNormal * 2f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, wallForward * 2f);
            }
        }
    }
}