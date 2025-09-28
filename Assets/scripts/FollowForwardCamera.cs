using UnityEngine;

/// Cámara tercera persona que sigue al player detrás/arriba.
/// Opcional: sólo reposiciona cuando el player se mueve hacia delante.
public class FollowForwardCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                  // Asignar el Player (no la cámara)

    [Header("Offset detrás del player")]
    public float distance = 5f;               // qué tan atrás
    public float height = 2f;               // qué tan arriba

    [Header("Suavizados")]
    [Range(1f, 30f)] public float followLerp = 12f; // posición
    [Range(1f, 30f)] public float lookLerp = 16f; // rotación

    [Header("Sólo seguir al avanzar")]
    public bool onlyWhenMovingForward = false; // si true, sigue sólo si va hacia delante
    [Range(0f, 2f)] public float forwardDotThreshold = 0.2f; // 0= cualquier avance leve

    // cálculo de velocidad del target (para detectar avance)
    Vector3 lastTargetPos;
    bool initialized;

    void LateUpdate()
    {
        if (!target) return;

        // Velocidad del target en el plano XZ
        Vector3 targetPos = target.position;
        Vector3 targetVel = initialized ? (targetPos - lastTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f)
                                        : Vector3.zero;
        Vector3 targetVelXZ = new Vector3(targetVel.x, 0f, targetVel.z);

        // ¿Se mueve hacia delante? (producto punto entre forward del player y su vel)
        bool movingForward = targetVelXZ.sqrMagnitude > 0.0004f &&
                             Vector3.Dot(target.forward, targetVelXZ.normalized) > forwardDotThreshold;

        // Punto ideal de la cámara: detrás y arriba del player
        Vector3 desiredPos = target.position - target.forward * distance + Vector3.up * height;

        // Si sólo seguimos al avanzar y no está avanzando, quedarnos donde estamos (pero mirar al player)
        Vector3 newPos = transform.position;
        if (!onlyWhenMovingForward || movingForward || !initialized)
        {
            float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
            newPos = Vector3.Lerp(transform.position, desiredPos, t);
        }

        // Mirar al player con suavizado
        Quaternion desiredRot = Quaternion.LookRotation((target.position - newPos).normalized, Vector3.up);
        float r = 1f - Mathf.Exp(-lookLerp * Time.deltaTime);
        Quaternion newRot = Quaternion.Slerp(transform.rotation, desiredRot, r);

        // Aplicar
        transform.SetPositionAndRotation(newPos, newRot);

        // Guardar estado
        lastTargetPos = targetPos;
        initialized = true;
    }

    void OnValidate()
    {
        distance = Mathf.Max(0.1f, distance);
        height = Mathf.Max(0f, height);
    }
}
