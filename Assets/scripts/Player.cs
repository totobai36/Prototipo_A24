using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    // ==========================
    // MOVIMIENTO
    // ==========================
    [Header("Movimiento")]
    [SerializeField] private float velocidadMovimiento = 6.0f;
    [SerializeField] private float velocidadSprint = 10.0f;
    [SerializeField] private float aceleracion = 8f; // Reducido para menos exageración
    [SerializeField] private float friccion = 15f;

    [Header("Cámara relativa")]
    [SerializeField] private bool useCameraRelative = true;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool rotateTowardsMove = true;
    [SerializeField] private float turnSpeed = 180f; // Reducido para rotación más suave

    private float x; // Horizontal (A/D)
    private float y; // Vertical   (W/S)

    // ==========================
    // ESCALERAS - SISTEMA HÍBRIDO DEL PRIMER SCRIPT
    // ==========================
    [Header("Escaleras y Rampas")]
    [SerializeField] private float stepHeight = 0.4f;
    [SerializeField] private float stepForce = 15f; // Mucho más suave que 300
    [SerializeField] private float rampForce = 8f;  // Mucho más suave que 200
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float slopeLimit = 45f;
    [SerializeField] private bool debugSteps = false; // Desactivado por defecto

    // ==========================
    // SALTO / SUELO
    // ==========================
    [Header("Salto")]
    [SerializeField] private float fuerzaDeSalto = 8f;
    public bool EnSuelo; // Para compatibilidad con Pies.cs

    // ==========================
    // WALL RUN - SISTEMA DEL SEGUNDO SCRIPT
    // ==========================
    [Header("WallRun")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallRunDuration = 3f;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallStickForce = 50f;
    [SerializeField] private LayerMask wallLayer = -1;
    [SerializeField] private float wallCheckDistance = 1.2f;
    [SerializeField] private float wallRunGravity = 2f;
    [SerializeField] private float wallRunCooldown = 0.5f;

    // Estados internos del Wall Run
    private bool isWallRunning = false;
    private Vector3 wallNormal;
    private Vector3 wallForward;
    private float wallRunTimer = 0f;
    private float wallRunCooldownTimer = 0f;
    private bool wallLeft = false;
    private bool wallRight = false;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    // Propiedad pública para compatibilidad
    public bool IsWallRunning => isWallRunning;

    // ==========================
    // COMPONENTES
    // ==========================
    private Rigidbody rb;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (anim) anim.applyRootMotion = false;
        rb.freezeRotation = true;
        
        // Configurar layers
        if (groundLayer.value == -1 || groundLayer.value == 0) 
        {
            groundLayer = ~0; // Incluir todo por defecto
        }
        
        // Autoasignar cámara
        if (!cameraTransform && Camera.main) 
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Input
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        // Cooldown wallrun
        if (wallRunCooldownTimer > 0f) wallRunCooldownTimer -= Time.deltaTime;

        // Wall Run - usando la lógica del segundo script
        CheckForWalls();
        HandleWallRun();

        // Salto desde suelo (no en wallrun)
        if (EnSuelo && Input.GetKeyDown(KeyCode.Space) && !isWallRunning)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * fuerzaDeSalto, ForceMode.Impulse);
        }

        // Salto variable
        if (rb.linearVelocity.y > 0f && Input.GetKeyUp(KeyCode.Space) && !isWallRunning)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z); 
        }

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        // Si está en wall run, solo manejar wall run
        if (isWallRunning)
        {
            WallRunMovement();
            return;
        }

        // Movimiento normal y escaleras
        if (EnSuelo)
        {
            HandleStepsAndRamps();
        }
        
        HandleMovement();
        ApplyFriction();
        LimitHorizontalVelocity();
    }

    // ==========================
    // MOVIMIENTO MEJORADO (menos exagerado)
    // ==========================
    void HandleMovement()
    {
        bool isMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f;
        if (!isMoving) return;

        // Calcular dirección
        Vector3 wishDir;
        if (useCameraRelative && cameraTransform)
        {
            Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
            Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
            wishDir = camRight * x + camFwd * y;
        }
        else
        {
            wishDir = transform.right * x + transform.forward * y;
        }

        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        // Sprint
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && EnSuelo && y > 0.1f;
        float currentMaxSpeed = isSprinting ? velocidadSprint : velocidadMovimiento;

        // Aplicar fuerza de movimiento (más suave)
        Vector3 targetVelocity = wishDir * currentMaxSpeed;
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityError = targetVelocity - currentHorizontalVel;
        
        // Fuerza más suave
        rb.AddForce(velocityError * aceleracion, ForceMode.Acceleration);

        // Rotación más suave
        if (rotateTowardsMove && wishDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
            float rotationSpeed = turnSpeed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed);
        }
    }

    void ApplyFriction()
    {
        if (EnSuelo && Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.1f)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.magnitude > 0.1f)
            {
                rb.AddForce(-horizontalVel * friccion, ForceMode.Acceleration);
            }
        }
    }

    void LimitHorizontalVelocity()
    {
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && EnSuelo && y > 0.1f;
        float maxSpeed = isSprinting ? velocidadSprint : velocidadMovimiento;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed + 0.5f) // Margen más amplio
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    // ==========================
    // ESCALERAS - SISTEMA SUAVIZADO DEL PRIMER SCRIPT
    // ==========================
    void HandleStepsAndRamps()
    {
        if (Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.1f) return;

        Vector3 moveDir = GetMovementDirection();
        if (moveDir.sqrMagnitude < 0.001f) return;

        CapsuleCollider capCollider = GetComponent<CapsuleCollider>();
        if (capCollider == null) return;

        float radius = capCollider.radius;
        Vector3 center = transform.position + capCollider.center;

        // Detectar obstáculos adelante
        Vector3 rayStart = center;
        float rayDistance = radius + 0.3f; // Reducido para menos agresividad

        if (Physics.Raycast(rayStart, moveDir, out RaycastHit hit, rayDistance, groundLayer))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (debugSteps)
            {
                Debug.DrawRay(rayStart, moveDir * rayDistance, Color.red, 0.1f);
            }

            if (angle > 1f && angle < slopeLimit) // Rampa
            {
                HandleRamp(hit.normal, moveDir);
            }
            else if (angle >= slopeLimit) // Escalón
            {
                HandleStep(hit.point, moveDir);
            }
        }
    }

    Vector3 GetMovementDirection()
    {
        if (useCameraRelative && cameraTransform)
        {
            Vector3 camFwd = cameraTransform.forward; 
            camFwd.y = 0f; 
            camFwd.Normalize();
            Vector3 camRight = cameraTransform.right; 
            camRight.y = 0f; 
            camRight.Normalize();
            return (camRight * x + camFwd * y).normalized;
        }
        else
        {
            return (transform.right * x + transform.forward * y).normalized;
        }
    }

    void HandleRamp(Vector3 normal, Vector3 moveDir)
    {
        Vector3 rampDirection = Vector3.ProjectOnPlane(moveDir, normal).normalized;
        Vector3 rampForceVector = rampDirection * rampForce;
        rb.AddForce(rampForceVector, ForceMode.Acceleration); // Cambiado a Acceleration para más suavidad

        if (debugSteps)
        {
            Debug.DrawRay(transform.position, rampDirection * 2f, Color.green, 0.1f);
        }
    }

    void HandleStep(Vector3 hitPoint, Vector3 moveDir)
    {
        Vector3 stepCheckStart = hitPoint + Vector3.up * stepHeight + moveDir * 0.2f;
        
        if (!Physics.Raycast(stepCheckStart, Vector3.down, 0.2f, groundLayer))
        {
            if (Physics.Raycast(stepCheckStart, Vector3.down, stepHeight + 0.3f, groundLayer))
            {
                // Fuerza mucho más suave para escalones
                Vector3 stepForceVector = Vector3.up * stepForce;
                rb.AddForce(stepForceVector, ForceMode.Acceleration);

                if (debugSteps)
                {
                    Debug.DrawRay(transform.position, Vector3.up * 2f, Color.yellow, 0.1f);
                }
            }
        }
    }

    // ==========================
    // WALL RUN - LÓGICA DEL SEGUNDO SCRIPT
    // ==========================
    void CheckForWalls()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        wallLeft = Physics.Raycast(rayStart, -transform.right, out leftWallHit, wallCheckDistance, wallLayer);
        wallRight = Physics.Raycast(rayStart, transform.right, out rightWallHit, wallCheckDistance, wallLayer);
        
        if (isWallRunning && !wallLeft && !wallRight)
        {
            StopWallRun();
        }
    }

    void HandleWallRun()
    {
        bool canWallRun = CanStartWallRun();

        if (canWallRun && !isWallRunning)
        {
            StartWallRun();
        }
        else if (isWallRunning)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                WallJump();
            }
            else if (ShouldStopWallRun())
            {
                StopWallRun();
            }
        }
    }

    bool CanStartWallRun()
    {
        return (wallLeft || wallRight) &&
               !EnSuelo && 
               rb.linearVelocity.y < 1f &&
               wallRunCooldownTimer <= 0f;
    }

    bool ShouldStopWallRun()
    {
        return Input.GetKey(KeyCode.S) || 
               Input.GetKey(KeyCode.DownArrow) ||
               (!wallLeft && !wallRight) ||
               wallRunTimer <= 0f ||
               EnSuelo;
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunDuration;

        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

        Vector3 refDir;
        if (useCameraRelative && cameraTransform)
        {
            refDir = cameraTransform.forward; refDir.y = 0f; refDir.Normalize();
        }
        else
        {
            refDir = transform.forward;
        }
        if (Vector3.Dot(wallForward, refDir) < 0f) wallForward = -wallForward;

        // Cancelar velocidad vertical negativa
        Vector3 vel = rb.linearVelocity;
        if (vel.y < 0f) vel.y = 0f;
        rb.linearVelocity = vel;
        
        // Pequeño impulso hacia arriba
        rb.AddForce(Vector3.up * 3f, ForceMode.Impulse);
    }

    void WallRunMovement()
    {
        wallRunTimer -= Time.deltaTime;

        // Movimiento hacia adelante automático
        Vector3 targetVelocity = wallForward * wallRunSpeed;
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityError = targetVelocity - currentHorizontalVel;
        
        rb.AddForce(velocityError * 10f, ForceMode.Force);
        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Force);
        
        // Contrarrestar gravedad
        float gravityCancellation = -Physics.gravity.y - wallRunGravity;
        rb.AddForce(Vector3.up * gravityCancellation, ForceMode.Acceleration);

        // Rotar hacia la dirección del wall run
        if (wallForward.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(wallForward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    void WallJump()
    {
        StopWallRun();

        Vector3 jumpDirection = (wallNormal + Vector3.up + wallForward * 0.5f).normalized;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(jumpDirection * wallJumpForce, ForceMode.Impulse);

        wallRunCooldownTimer = wallRunCooldown;
    }

    void StopWallRun()
    {
        if (isWallRunning)
        {
            isWallRunning = false;
            wallRunCooldownTimer = 0.2f;

            // Conservar algo de velocidad hacia adelante
            Vector3 v = rb.linearVelocity;
            float forwardSpeed = Vector3.Dot(v, wallForward);
            Vector3 newPlanarVelocity = wallForward * Mathf.Max(forwardSpeed * 0.7f, 3f);
            
            rb.linearVelocity = new Vector3(newPlanarVelocity.x, v.y, newPlanarVelocity.z);
        }
    }

    // ==========================
    // ANIMACIÓN - ARREGLADA PARA CORRER Y SALTAR
    // ==========================
    void UpdateAnimations()
    {
        if (!anim) return;
        
        // MÉTODO 1: Usar input directo (como el primer script)
        float animVelX = x;
        float animVelY = y;
        
        // Ajustar para sprint - si está corriendo, aumentar el valor Y
        if (Input.GetKey(KeyCode.LeftShift) && EnSuelo && y > 0.1f)
        {
            animVelY = y * 2f; // Duplicar velocidad de animación para sprint
        }
        
        // MÉTODO 2: Alternativamente, usar velocidad real pero normalizada
        // (descomenta estas líneas si prefieres este método)
        /*
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 localVel = transform.InverseTransformDirection(flatVel);
        
        float maxSpeed = Input.GetKey(KeyCode.LeftShift) ? velocidadSprint : velocidadMovimiento;
        animVelX = Mathf.Clamp(localVel.x / maxSpeed, -1f, 1f);
        animVelY = Mathf.Clamp(localVel.z / maxSpeed, -1f, 1f);
        */
        
        // Aplicar valores con suavizado
        float smoothTime = EnSuelo ? 0.1f : 0.3f;
        
        anim.SetFloat("VelX", animVelX, smoothTime, Time.deltaTime);
        anim.SetFloat("VelY", animVelY, smoothTime, Time.deltaTime);
        anim.SetBool("Suelo", EnSuelo);
        anim.SetBool("Wall", isWallRunning);
        
        // Parámetro adicional para sprint (si tu animator lo tiene)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && EnSuelo && y > 0.1f;
        anim.SetBool("Sprint", isSprinting);
        
        // Parámetro para velocidad de salto (si tu animator lo tiene)
        anim.SetFloat("JumpVelocity", rb.linearVelocity.y);
    }

    // ==========================
    // DEBUG VISUAL
    // ==========================
    void OnDrawGizmosSelected()
    {
        if (!debugSteps) return;
        
        // Wall run rays
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Gizmos.color = wallLeft ? Color.red : Color.white;
        Gizmos.DrawRay(rayStart, -transform.right * wallCheckDistance);
        
        Gizmos.color = wallRight ? Color.red : Color.white;
        Gizmos.DrawRay(rayStart, transform.right * wallCheckDistance);
        
        // Step detection
        Vector3 moveDir = GetMovementDirection();
        if (moveDir.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, moveDir * 1f);
        }
    }
}