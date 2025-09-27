using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadMovimiento = 5.0f;
    [SerializeField] private float velocidadRotacion = 200.0f;
    private Animator anim;
    [SerializeField] private float x, y;

    [Header("Salto")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float fuerzaDeSalto = 4f;
    public bool Saltar;
    [SerializeField] private float timer = 4f;

    [Header("WallRun")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallRunDuration = 3f;
    [SerializeField] private float wallJumpForce = 8f;
    [SerializeField] private float wallJumpSideForce = 5f;
    [SerializeField] private LayerMask wallLayer = -1;
    [SerializeField] private float wallCheckDistance = 1.2f;
    [SerializeField] private float minWallRunHeight = 1f;
    [SerializeField] private float wallRunGravity = 2f; // Gravedad reducida durante wallrun
    [SerializeField] private float minVelocityForWallRun = 2f;

    private bool isWallRunning = false;
    private Vector3 wallNormal;
    private Vector3 wallForward;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft = false;
    private bool wallRight = false;
    private float wallRunTimer = 0f;

    // Variables adicionales para mejorar el wallrun
    private bool wasWallRunning = false;
    private float wallRunCooldown = 0.5f;
    private float wallRunCooldownTimer = 0f;

    void Start()
    {
        Saltar = false;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Asegurar que el Rigidbody tenga la configuración correcta
        rb.freezeRotation = true; // Evitar rotaciones no deseadas
    }

    void FixedUpdate()
    {
        // Movimiento normal solo cuando no estamos en wallrun
        if (!isWallRunning)
        {
            // Rotación del personaje
            transform.Rotate(0, x * Time.fixedDeltaTime * velocidadRotacion, 0);

            // Movimiento hacia adelante/atrás
            Vector3 movement = transform.forward * y * velocidadMovimiento * Time.fixedDeltaTime;
            transform.position += movement;
        }
    }

    void Update()
    {
        // Input del jugador
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        // Actualizar cooldown
        if (wallRunCooldownTimer > 0)
            wallRunCooldownTimer -= Time.deltaTime;

        // Verificar paredes
        CheckForWalls();

        // Manejar wallrun
        HandleWallRun();

        // Sistema de salto mejorado
        HandleJumping();

        // Actualizar animaciones
        UpdateAnimations();
    }

    void CheckForWalls()
    {
        // Verificar pared izquierda - desde el centro del personaje
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        wallLeft = Physics.Raycast(rayStart, -transform.right, out leftWallHit, wallCheckDistance, wallLayer);

        // Verificar pared derecha
        wallRight = Physics.Raycast(rayStart, transform.right, out rightWallHit, wallCheckDistance, wallLayer);

        // Debug visual mejorado
        Debug.DrawRay(rayStart, -transform.right * wallCheckDistance, wallLeft ? Color.green : Color.red);
        Debug.DrawRay(rayStart, transform.right * wallCheckDistance, wallRight ? Color.green : Color.red);
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
            else
            {
                WallRunMovement();
            }
        }
    }

    bool CanStartWallRun()
    {
        // Calcular velocidad horizontal (sin Y)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        return (wallLeft || wallRight) &&
               y > 0.1f && // Debe moverse hacia adelante
               !Saltar && // No debe estar en el suelo
               wallRunCooldownTimer <= 0f && // Cooldown terminado
               rb.linearVelocity.y < 1f; // No debe estar saltando hacia arriba muy rápido
    }

    bool ShouldStopWallRun()
    {
        return y <= 0 || // No se mueve hacia adelante
               (!wallLeft && !wallRight) || // No hay pared
               wallRunTimer <= 0 || // Se acabó el tiempo
               Saltar; // Tocó el suelo
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunDuration;

        // Determinar la normal de la pared y dirección hacia adelante
        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        // Calcular dirección hacia adelante a lo largo de la pared
        wallForward = Vector3.Cross(wallNormal, Vector3.up);

        // Determinar la mejor dirección basada en la velocidad actual del jugador
        Vector3 playerMovementDirection = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized;

        // Si el jugador se mueve, usar esa dirección; si no, usar la dirección hacia adelante del transform
        Vector3 referenceDirection = playerMovementDirection.magnitude > 0.1f ? playerMovementDirection : transform.forward;

        // Asegurar que la dirección hacia adelante apunte correctamente
        if (Vector3.Dot(wallForward, referenceDirection) < 0)
            wallForward = -wallForward;

        // Reducir velocidad vertical si está cayendo
        Vector3 vel = rb.linearVelocity;
        if (vel.y < 0)
        {
            vel.y = Mathf.Max(vel.y * 0.1f, -2f); // Reducir velocidad de caída
            rb.linearVelocity = vel;
        }

        Debug.Log("Wall Run Started - Wall: " + (wallLeft ? "Left" : "Right"));
    }

    void WallRunMovement()
    {
        wallRunTimer -= Time.deltaTime;

        // Calcular velocidad objetivo
        Vector3 targetVelocity = wallForward * wallRunSpeed;

        // Mantener algo de velocidad Y, pero aplicar gravedad reducida
        targetVelocity.y = rb.linearVelocity.y;

        // Aplicar movimiento
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);

        // Aplicar fuerza hacia la pared para mantener contacto
        rb.AddForce(-wallNormal * 200f * Time.deltaTime, ForceMode.Force);

        // Aplicar gravedad reducida
        rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Acceleration);
    }

    void WallJump()
    {
        StopWallRun();

        // Calcular dirección del salto (alejándose de la pared y hacia arriba)
        Vector3 wallJumpDirection = (wallNormal * 2f + Vector3.up).normalized;

        // Resetear velocidad antes del salto
        rb.linearVelocity = Vector3.zero;

        // Aplicar fuerzas del salto
        rb.AddForce(wallJumpDirection * wallJumpForce, ForceMode.Impulse);
        rb.AddForce(wallForward * wallJumpSideForce * 0.5f, ForceMode.Impulse);

        // Iniciar cooldown
        wallRunCooldownTimer = wallRunCooldown;

        Debug.Log("Wall Jump Executed");
    }

    void StopWallRun()
    {
        if (isWallRunning)
        {
            isWallRunning = false;
            wasWallRunning = true;

            // Pequeño cooldown para evitar re-enganche inmediato
            wallRunCooldownTimer = 0.2f;

            Debug.Log("Wall Run Stopped");
        }
    }

    void HandleJumping()
    {
        if (Saltar && Input.GetKeyDown(KeyCode.Space) && !isWallRunning)
        {
            rb.AddForce(new Vector3(0, fuerzaDeSalto, 0), ForceMode.Impulse);
        }
    }

    void UpdateAnimations()
    {
        if (!isWallRunning)
        {
            anim.SetFloat("VelX", x);
            anim.SetFloat("VelY", y);
            anim.SetBool("Suelo", Saltar);
        }


        /*
        anim.SetBool("WallRun", isWallRunning);
        anim.SetBool("WallRunLeft", isWallRunning && wallLeft);
        anim.SetBool("WallRunRight", isWallRunning && wallRight);
        */
    }

    void EstoyCayendo()
    {
        anim.SetBool("Suelo", false);
        anim.SetBool("Salto", false);
    }

    public bool IsWallRunning()
    {
        return isWallRunning;
    }
}
