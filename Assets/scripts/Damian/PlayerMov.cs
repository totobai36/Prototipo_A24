using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMov : MonoBehaviour
{
    // ==========================
    // MOVIMIENTO GENERAL
    // ==========================
    [Header("Movimiento General")]
    [SerializeField] private float velocidadMaxima = 6.0f;
    [SerializeField] private float velocidadSprint = 10.0f; 
    [SerializeField] private float aceleracionTerrestre = 150f; 
    [SerializeField] private float aceleracionAerea = 50f;
    [SerializeField] private float friccionTerrestre = 10f;
    
    // ==========================
    // ESCALERAS / PASOS - SISTEMA COMPLETAMENTE NUEVO
    // ==========================
    [Header("Subida de Escaleras y Rampas")]
    [SerializeField] private float stepHeight = 0.4f; 
    [SerializeField] private float stepForce = 300f; // Fuerza para subir
    [SerializeField] private float rampForce = 200f; // Fuerza adicional en rampas
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float slopeLimit = 45f; // Ángulo máximo de rampas
    [SerializeField] private bool debugSteps = true;
    
    // ==========================
    // CÁMARA & ROTACIÓN - ARREGLADO PARA CINEMACHINE
    // ==========================
    [Header("Cámara & Rotación")]
    [SerializeField] private bool useCameraRelative = true;    
    [SerializeField] private Transform cameraTransform;         
    [SerializeField] private bool rotateTowardsMove = false; // DESACTIVADO por defecto para Cinemachine
    [SerializeField] private float turnSpeed = 150f; // Reducido para rotación más suave
    [SerializeField] private bool useInstantTurn = true; // NUEVO: rotación instantánea con movimiento
    [SerializeField] private float rotationSmoothness = 10f; // NUEVO: suavidad de rotación

    // Propiedad pública para que el script de WallRun acceda a la velocidad de giro
    public float TurnSpeed => turnSpeed; 

    private float inputX;
    private float inputY;

    // ==========================
    // SALTO / SUELO / GRAVEDAD
    // ==========================
    [Header("Salto")]
    [SerializeField] private float fuerzaDeSalto = 8.0f; 
    [SerializeField] private float multiplicadorGravedadCaida = 2.5f;
    // La variable EnSuelo debe ser pública para que WallRun pueda leerla
    public bool EnSuelo; 

    // ==========================
    // REFERENCIA AL WALL RUN 
    // ==========================
    private PlayerWallRun wallRunController;

    // ==========================
    // COMPONENTES
    // ==========================
    private Rigidbody rb;
    private Animator anim;

    // ==========================
    // CICLO DE VIDA
    // ==========================
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        // Obtiene la referencia al nuevo script Wall Run
        wallRunController = GetComponent<PlayerWallRun>(); 

        if (anim) anim.applyRootMotion = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;

        // FIX: Configurar groundLayer correctamente
        if (groundLayer.value == -1 || groundLayer.value == 0) 
        {
            groundLayer = ~0; // Todo excepto nada (incluye Default layer)
        }
        
        // Buscar cámara automáticamente si no está asignada
        if (!cameraTransform && Camera.main) 
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");

        // --- SALTO GENERAL (TIERRA) ---
        if (EnSuelo && Input.GetKeyDown(KeyCode.Space) && (wallRunController == null || !wallRunController.IsWallRunning))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * fuerzaDeSalto, ForceMode.Impulse);
        }
        
        // --- SALTO VARIABLE ---
        if (rb.linearVelocity.y > 0f && Input.GetKeyUp(KeyCode.Space) && (wallRunController == null || !wallRunController.IsWallRunning))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z); 
        }

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        // Si el Wall Run está activo, detenemos la lógica de movimiento base
        if (wallRunController != null && wallRunController.IsWallRunning)
        {
            return;
        }

        if (EnSuelo)
        {
            HandleStepsAndRamps();
        }
        
        HandleMovement();
        HandleFallControl();
        LimitHorizontalVelocity();
    }

    // ==========================
    // MANEJO DE MOVIMIENTO - ARREGLADO PARA CINEMACHINE
    // ==========================
    void HandleMovement()
    {
        Vector3 wishDir;
        
        bool isMoving = inputX != 0 || inputY != 0;
        // FIX: Mejorar la lógica del sprint
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && EnSuelo && isMoving && inputY >= 0f;
        float currentMaxSpeed = isSprinting ? velocidadSprint : velocidadMaxima; 
        
        float currentAccel = EnSuelo ? aceleracionTerrestre : aceleracionAerea;
        
        // --- Cálculo de la Dirección Deseada (ARREGLADO) ---
        if (useCameraRelative && cameraTransform)
        {
            Vector3 camFwd = cameraTransform.forward; 
            camFwd.y = 0f; 
            camFwd.Normalize();
            Vector3 camRight = cameraTransform.right; 
            camRight.y = 0f; 
            camRight.Normalize();
            wishDir = camRight * inputX + camFwd * inputY;
        }
        else
        {
            wishDir = transform.right * inputX + transform.forward * inputY;
        }

        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
        
        // --- Aplicar Fricción ---
        if (EnSuelo && wishDir.sqrMagnitude < 0.001f)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.sqrMagnitude > 0.01f)
            {
                rb.AddForce(-horizontalVel * friccionTerrestre, ForceMode.Acceleration);
            }
        }

        // --- Aplicar Fuerza ---
        if (isMoving)
        {
            Vector3 targetVelocity = wishDir * currentMaxSpeed;
            Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 velocityError = targetVelocity - currentHorizontalVel;
            
            Vector3 force = velocityError * currentAccel;
            rb.AddForce(force * Time.fixedDeltaTime, ForceMode.Force);
        }
        
        // --- ROTACIÓN DEL JUGADOR (OPTIMIZADA PARA CINEMACHINE) ---
        if (isMoving && wishDir.sqrMagnitude > 0.1f)
        {
            if (useInstantTurn)
            {
                // Rotación instantánea hacia donde se mueve, pero solo si es un cambio significativo
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                float angleDifference = Quaternion.Angle(transform.rotation, targetRot);
                
                if (angleDifference > 5f) // Solo rotar si el cambio es mayor a 5 grados
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothness * Time.fixedDeltaTime);
                }
            }
            else if (rotateTowardsMove)
            {
                // Rotación suave hacia donde se mueve
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            }
            // Si ambos están desactivados, no rota el jugador (deja que Cinemachine maneje todo)
        }
    }

    void HandleFallControl()
    {
        if (!EnSuelo && rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (multiplicadorGravedadCaida - 1f), ForceMode.Acceleration);
        }
    }
    
    void LimitHorizontalVelocity()
    {
        // FIX: Mejorar la limitación de velocidad
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && EnSuelo && (inputX != 0 || inputY != 0) && inputY >= 0f;
        float maxSpeed = isSprinting ? velocidadSprint : velocidadMaxima;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed + 0.1f) // Pequeño margen para evitar oscilaciones
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }
    
    // ==========================
    // SISTEMA NUEVO DE ESCALERAS Y RAMPAS
    // ==========================
    void HandleStepsAndRamps()
    {
        if (inputX == 0 && inputY == 0) return;

        // Obtener dirección de movimiento
        Vector3 moveDir = GetMovementDirection();
        if (moveDir.sqrMagnitude < 0.001f) return;

        CapsuleCollider capCollider = GetComponent<CapsuleCollider>();
        if (capCollider == null) return;

        float radius = capCollider.radius;
        float height = capCollider.height;
        Vector3 center = transform.position + capCollider.center;

        // 1. DETECTAR OBSTÁCULOS ADELANTE
        Vector3 rayStart = center;
        Vector3 rayDirection = moveDir;
        float rayDistance = radius + 0.5f;

        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayDistance, groundLayer))
        {
            if (debugSteps)
            {
                Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.red, 0.1f);
                Debug.Log($"Hit: {hit.collider.name}, Normal: {hit.normal}, Angle: {Vector3.Angle(hit.normal, Vector3.up)}");
            }

            float angle = Vector3.Angle(hit.normal, Vector3.up);

            // 2. DETERMINAR TIPO DE SUPERFICIE
            if (angle > 1f && angle < slopeLimit) // Es una rampa
            {
                HandleRamp(hit.normal, moveDir);
            }
            else if (angle >= slopeLimit) // Es una pared/escalón
            {
                HandleStep(hit.point, moveDir, capCollider);
            }
        }

        // 3. VERIFICAR SUPERFICIE ACTUAL (para rampas que ya estamos subiendo)
        Vector3 groundCheck = center - Vector3.up * (height / 2f + 0.1f);
        if (Physics.Raycast(groundCheck, Vector3.down, out RaycastHit groundHit, 0.3f, groundLayer))
        {
            float groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (groundAngle > 1f && groundAngle < slopeLimit)
            {
                HandleRamp(groundHit.normal, moveDir);
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
            return (camRight * inputX + camFwd * inputY).normalized;
        }
        else
        {
            return (transform.right * inputX + transform.forward * inputY).normalized;
        }
    }

    void HandleRamp(Vector3 normal, Vector3 moveDir)
    {
        // Calcular dirección de subida en la rampa
        Vector3 rampDirection = Vector3.ProjectOnPlane(moveDir, normal).normalized;
        
        // Aplicar fuerza adicional para subir la rampa
        Vector3 rampForceVector = rampDirection * rampForce;
        rb.AddForce(rampForceVector * Time.fixedDeltaTime, ForceMode.Force);

        if (debugSteps)
        {
            Debug.DrawRay(transform.position, rampDirection * 2f, Color.green, 0.1f);
            Debug.DrawRay(transform.position, normal * 2f, Color.blue, 0.1f);
        }
    }

    void HandleStep(Vector3 hitPoint, Vector3 moveDir, CapsuleCollider capCollider)
    {
        // Verificar si podemos subir el escalón
        Vector3 stepCheckStart = hitPoint + Vector3.up * stepHeight + moveDir * 0.3f;
        
        // Verificar espacio libre arriba del escalón
        if (!Physics.Raycast(stepCheckStart, Vector3.down, 0.2f, groundLayer))
        {
            // Verificar que hay piso donde aterrizar
            if (Physics.Raycast(stepCheckStart, Vector3.down, stepHeight + 0.5f, groundLayer))
            {
                // Aplicar fuerza hacia arriba para subir el escalón
                Vector3 stepForceVector = (Vector3.up * 0.7f + moveDir * 0.3f) * stepForce;
                rb.AddForce(stepForceVector * Time.fixedDeltaTime, ForceMode.Force);

                if (debugSteps)
                {
                    Debug.DrawRay(transform.position, Vector3.up * 2f, Color.yellow, 0.1f);
                    Debug.Log("Aplicando fuerza de escalón");
                }
            }
        }
    }
    
    // ==========================
    // ANIMACIÓN
    // ==========================
    void UpdateAnimations()
    {
        if (!anim) return;
        
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 localVel = transform.InverseTransformDirection(flatVel);
        
        float targetVelY = localVel.z;
        float targetVelX = localVel.x;
        
        float smoothTime = EnSuelo ? 0.1f : 0.5f;

        anim.SetFloat("VelY", targetVelY, smoothTime, Time.deltaTime);
        anim.SetFloat("VelX", targetVelX, smoothTime, Time.deltaTime);
        
        anim.SetBool("Suelo", EnSuelo);
        
        if (wallRunController != null)
        {
            anim.SetBool("Wall", wallRunController.IsWallRunning);
        }
    }
}