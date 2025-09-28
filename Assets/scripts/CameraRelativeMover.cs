using UnityEngine;

/// Movimiento WASD relativo a la c�mara en plano XZ (tercera persona).
/// - W: avanza hacia donde mira la c�mara (en el plano)
/// - A/D: strafe relativo a la c�mara
/// - Opcional: alinear rotaci�n del jugador con la direcci�n de movimiento
/// - Usa Rigidbody.MovePosition (suave y compatible con f�sica)
[RequireComponent(typeof(Rigidbody))]
public class CameraRelativeMover : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la c�mara que define la referencia (Main Camera).")]
    public Transform cameraTransform;

    [Header("Movimiento")]
    [Range(0.5f, 20f)] public float moveSpeed = 6f;
    [Range(0f, 30f)] public float acceleration = 25f; // acelera a la velocidad objetivo
    [Range(0f, 30f)] public float deceleration = 25f; // frena cuando no hay input
    public bool sprintEnabled = true;
    [Range(1f, 2f)] public float sprintMultiplier = 1.4f;

    [Header("Rotaci�n del jugador")]
    public bool rotateTowardsMove = true;
    [Range(60f, 720f)] public float turnSpeed = 540f; // grados/seg

    [Header("Input")]
    public string horizontalAxis = "Horizontal"; // A/D
    public string verticalAxis = "Vertical";   // W/S
    public string sprintButton = "Fire3";      // por defecto LeftShift en Input Manager

    [Header("Opcionales")]
    [Tooltip("Si es true, bloquea y oculta el cursor (�til con c�maras orbit).")]
    public bool lockCursor = false;

    Rigidbody rb;
    Vector3 planarVelocity; // velocidad acumulada en XZ

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // evitar vuelcos f�sicos
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Reset()
    {
        // Intenta autocompletar la c�mara principal
        var cam = Camera.main;
        if (cam) cameraTransform = cam.transform;
    }

    void FixedUpdate()
    {
        if (!cameraTransform)
        {
            var cam = Camera.main;
            if (cam) cameraTransform = cam.transform;
            if (!cameraTransform) return;
        }

        // 1) Leer input
        float x = Input.GetAxis(horizontalAxis);
        float y = Input.GetAxis(verticalAxis);
        bool sprint = sprintEnabled && Input.GetButton(sprintButton);

        // 2) Ejes relativos a c�mara (proyectados en plano horizontal)
        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        // Direcci�n deseada
        Vector3 wishDir = camRight * x + camFwd * y;
        float wishMag = Mathf.Clamp01(wishDir.magnitude); // 0..1
        if (wishMag > 0f) wishDir /= wishMag; // normaliza si hay input

        float targetSpeed = moveSpeed * (sprint ? sprintMultiplier : 1f);
        Vector3 targetVel = wishDir * (targetSpeed * wishMag); // velocidad objetivo en XZ

        // 3) Separar componentes: mantenemos Y del rigidbody (gravedad/saltos)
        Vector3 vel = rb.linearVelocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);

        // 4) Acelerar/frenar hacia targetVel (suave)
        Vector3 desiredDelta = targetVel - velXZ;
        float rate = (targetVel.sqrMagnitude > 0.0001f) ? acceleration : deceleration;
        Vector3 change = Vector3.ClampMagnitude(desiredDelta, rate * Time.fixedDeltaTime);
        planarVelocity = velXZ + change;

        rb.MovePosition(rb.position + planarVelocity * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(planarVelocity.x, vel.y, planarVelocity.z); // conserva Y

        // 5) Rotar el jugador hacia la direcci�n de movimiento (opcional)
        if (rotateTowardsMove && planarVelocity.sqrMagnitude > 0.0004f)
        {
            Vector3 lookDir = planarVelocity; lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
