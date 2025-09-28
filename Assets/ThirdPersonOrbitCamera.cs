using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;            // Debe ser el Player

    [Header("Orbit")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 9f;
    public float mouseXSensitivity = 180f;  // grados/seg
    public float mouseYSensitivity = 120f;
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Follow")]
    public float followLerp = 12f;      // suavizado de seguimiento
    public bool snapOnStart = true;     // coloca la cámara de golpe al inicio

    [Header("Misc")]
    public bool lockCursor = true;

    float yaw;      // rot Y
    float pitch;    // rot X
    bool initialized;

    void Awake()
    {
        // Seguridad: evitar self-target
        if (target == transform)
        {
            Debug.LogError("[ThirdPersonOrbitCamera] 'target' no puede ser la propia cámara. Asigna el Player.");
            target = null;
        }
    }

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (!target)
        {
            // Si no hay target intenta encontrar al Player por tag
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        if (target)
        {
            // Calcula yaw/pitch iniciales a partir de la posición relativa
            Vector3 rel = (transform.position - target.position);
            if (rel.sqrMagnitude < 0.01f) rel = Quaternion.Euler(0, 30, 0) * Vector3.back * distance;

            distance = Mathf.Clamp(rel.magnitude, minDistance, maxDistance);

            Vector3 dir = rel.normalized;
            pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            if (snapOnStart)
            {
                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 desiredPos = target.position + rot * (Vector3.back * distance);
                transform.SetPositionAndRotation(desiredPos, rot);
                initialized = true;
            }
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // Seguridad: evitar self-target en runtime
        if (target == transform)
        {
            Debug.LogError("[ThirdPersonOrbitCamera] 'target' está apuntando a la cámara. Corrígelo en el Inspector.");
            return;
        }

        // Input mouse
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        yaw += mx * mouseXSensitivity * Time.deltaTime;
        pitch -= my * mouseYSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            distance = Mathf.Clamp(distance - scroll * 5f, minDistance, maxDistance);

        // Posición/rotación deseada
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + rot * (Vector3.back * distance);

        // Primer frame sin suavizado si no se inicializó
        if (!initialized)
        {
            transform.SetPositionAndRotation(desiredPos, rot);
            initialized = true;
            return;
        }

        // Suavizado estable (no se “fuga”)
        float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
    }
}


