using UnityEngine;

/// C�mara tercera persona que sigue al player detr�s/arriba.
/// Opcional: s�lo reposiciona cuando el player se mueve hacia delante.
public class FollowForwardCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                  // Asignar el Player (no la c�mara)

    [Header("Offset detr�s del player")]
    public float distance = 5f;               // qu� tan atr�s
    public float height = 2f;               // qu� tan arriba

    [Header("Suavizados")]
    [Range(1f, 30f)] public float followLerp = 12f; // posici�n
    [Range(1f, 30f)] public float lookLerp = 16f; // rotaci�n

    [Header("S�lo seguir al avanzar")]
    public bool onlyWhenMovingForward = false; // si true, sigue s�lo si va hacia delante
    [Range(0f, 2f)] public float forwardDotThreshold = 0.2f; // 0= cualquier avance leve

    // c�lculo de velocidad del target (para detectar avance)
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

        // �Se mueve hacia delante? (producto punto entre forward del player y su vel)
        bool movingForward = targetVelXZ.sqrMagnitude > 0.0004f &&
                             Vector3.Dot(target.forward, targetVelXZ.normalized) > forwardDotThreshold;

        // Punto ideal de la c�mara: detr�s y arriba del player
        Vector3 desiredPos = target.position - target.forward * distance + Vector3.up * height;

        // Si s�lo seguimos al avanzar y no est� avanzando, quedarnos donde estamos (pero mirar al player)
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
