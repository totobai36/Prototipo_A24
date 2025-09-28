using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    // ==========================
    // MOVIMIENTO
    // ==========================
    [Header("Movimiento")]
    [SerializeField] private float velocidadMovimiento = 6.0f;

    [Header("Cámara relativa")]
    [SerializeField] private bool useCameraRelative = true;      // ON: W sigue la cámara
    [SerializeField] private Transform cameraTransform;          // arrastrá Main Camera (si no, usa Camera.main)
    [SerializeField] private bool rotateTowardsMove = true;      // ON: el player mira hacia donde se mueve
    [SerializeField] private float turnSpeed = 540f;             // grados/seg

    private float x; // Horizontal (A/D)
    private float y; // Vertical   (W/S)

    // ==========================
    // SALTO / SUELO
    // ==========================
    [Header("Salto")]
    [SerializeField] private float fuerzaDeSalto = 4f;
    public bool Saltar; // true = en suelo (se setea desde Pies.cs)

    // ==========================
    // COMPONENTES
    // ==========================
    private Rigidbody rb;
    private Animator anim;

    // ==========================
    // WALL RUN
    // ==========================
    [Header("WallRun")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallRunDuration = 3f;
    [SerializeField] private float wallJumpForce = 8f;
    [SerializeField] private float wallJumpSideForce = 5f;
    [SerializeField] private LayerMask wallLayer = -1;
    [SerializeField] private float wallCheckDistance = 1.2f;
    [SerializeField] private float wallRunGravity = 2f; // Gravedad reducida durante wallrun

    private bool isWallRunning = false;
    private Vector3 wallNormal;
    private Vector3 wallForward;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft = false;
    private bool wallRight = false;
    private float wallRunTimer = 0f;

    private float wallRunCooldown = 0.5f;
    private float wallRunCooldownTimer = 0f;

    public bool EnSuelo { get; internal set; }

    // ==========================
    // CICLO DE VIDA
    // ==========================
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // 🔹 FIX 1: evitar empujes de animaciones y restos de velocidad al iniciar
        if (anim) anim.applyRootMotion = false;
        rb.linearVelocity = Vector3.zero;
        rb.freezeRotation = true; // evitar vuelcos por físicas
    }

    void Update()
    {
        // Autoasigna la Main Camera si no se configuró
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        // Input crudo
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        // Cooldown wallrun
        if (wallRunCooldownTimer > 0f) wallRunCooldownTimer -= Time.deltaTime;

        // Detección de paredes y wallrun
        CheckForWalls();
        HandleWallRun();

        // Salto desde suelo (no en wallrun)
        if (Saltar && Input.GetKeyDown(KeyCode.Space) && !isWallRunning)
        {
            rb.AddForce(Vector3.up * fuerzaDeSalto, ForceMode.Impulse);
        }

        // Animaciones
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        // 🔹 FIX 2: si no hay input y no estás en wallrun, no muevas
        if (!isWallRunning && Mathf.Abs(x) < 0.001f && Mathf.Abs(y) < 0.001f)
        {
            // 🔹 FIX 3: frena deriva en suelo
            if (Saltar)
            {
                Vector3 v = rb.linearVelocity;
                v.x = Mathf.MoveTowards(v.x, 0f, 40f * Time.fixedDeltaTime);
                v.z = Mathf.MoveTowards(v.z, 0f, 40f * Time.fixedDeltaTime);
                rb.linearVelocity = v;
            }
            return;
        }

        if (!isWallRunning)
        {
            // --- MOVIMIENTO EN PLANO (con o sin cámara-relativa) ---
            Vector3 wishDir;

            if (useCameraRelative && cameraTransform)
            {
                // Ejes relativos a cámara proyectados en XZ
                Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
                Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
                wishDir = camRight * x + camFwd * y;
            }
            else
            {
                // Modo clásico (relativo al propio transform)
                wishDir = transform.right * x + transform.forward * y;
            }

            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            // Desplazamiento físico suave
            Vector3 delta = wishDir * velocidadMovimiento * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + delta);

            // Rotar hacia la dirección de movimiento (opcional)
            if (rotateTowardsMove && wishDir.sqrMagnitude > 0.0004f)
            {
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            }
        }
        // Si está en wallrun, la velocidad se gestiona en WallRunMovement()

        // 🔹 FIX 3 (también al final por seguridad): anti-deriva en suelo
        if (!isWallRunning && Saltar && Mathf.Abs(x) < 0.001f && Mathf.Abs(y) < 0.001f)
        {
            Vector3 v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, 0f, 40f * Time.fixedDeltaTime);
            v.z = Mathf.MoveTowards(v.z, 0f, 40f * Time.fixedDeltaTime);
            rb.linearVelocity = v;
        }
    }

    // ==========================
    // WALL RUN
    // ==========================
    void CheckForWalls()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        wallLeft = Physics.Raycast(rayStart, -transform.right, out leftWallHit, wallCheckDistance, wallLayer);
        wallRight = Physics.Raycast(rayStart, transform.right, out rightWallHit, wallCheckDistance, wallLayer);
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
        return (wallLeft || wallRight) &&
               y > 0.1f &&              // intención de avanzar
               !Saltar &&
               wallRunCooldownTimer <= 0f &&
               rb.linearVelocity.y < 1f;
    }

    bool ShouldStopWallRun()
    {
        return y <= 0f ||
               (!wallLeft && !wallRight) ||
               wallRunTimer <= 0f ||
               Saltar;
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunDuration;

        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        // Tangente de pared
        wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

        // Elegir sentido coherente con movimiento/cámara
        Vector3 refDir;
        if (useCameraRelative && cameraTransform)
        {
            refDir = cameraTransform.forward; refDir.y = 0f; refDir.Normalize();
        }
        else
        {
            Vector3 velPlanar = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            refDir = velPlanar.sqrMagnitude > 0.1f ? velPlanar.normalized : transform.forward;
        }
        if (Vector3.Dot(wallForward, refDir) < 0f) wallForward = -wallForward;

        // Suavizar caída vertical inicial
        Vector3 vel = rb.linearVelocity;
        if (vel.y < 0f) vel.y = Mathf.Max(vel.y * 0.1f, -2f);
        rb.linearVelocity = vel;
    }

    void WallRunMovement()
    {
        wallRunTimer -= Time.deltaTime;

        // Velocidad objetivo a lo largo de la pared
        Vector3 targetVelocity = wallForward * wallRunSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        // Interpola hacia la velocidad objetivo
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);

        // Mantenerse "pegado" y aplicar gravedad reducida
        rb.AddForce(-wallNormal * 200f * Time.deltaTime, ForceMode.Force);
        rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Acceleration);
    }

    void WallJump()
    {
        StopWallRun();

        // Salto alejándose de la pared y hacia arriba
        Vector3 wallJumpDirection = (wallNormal * 2f + Vector3.up).normalized;

        rb.linearVelocity = Vector3.zero; // reset
        rb.AddForce(wallJumpDirection * wallJumpForce, ForceMode.Impulse);
        rb.AddForce(wallForward * wallJumpSideForce * 0.5f, ForceMode.Impulse);

        wallRunCooldownTimer = wallRunCooldown;
    }

    void StopWallRun()
    {
        if (isWallRunning)
        {
            isWallRunning = false;
            wallRunCooldownTimer = 0.2f;

            // Al salir, limpia planar para que no “arrastre” hacia atrás
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
        }
    }

    // ==========================
    // ANIMACIÓN
    // ==========================
    void UpdateAnimations()
    {
        if (!anim) return;
        anim.SetFloat("VelX", x);
        anim.SetFloat("VelY", y);
        anim.SetBool("Suelo", Saltar);
        anim.SetBool("Wall", isWallRunning); // si lo usás
    }

    // ==========================
    // API para otros scripts
    // ==========================
    public bool IsWallRunning() => isWallRunning;
}
