using UnityEngine;
using DiasGames.AbilitySystem.Core; // Necesario para encontrar AbilitySystemController

// Ejecuta ANTES que la mayoría (y antes que el asset)
[DefaultExecutionOrder(-200)]
public class FallDetector : MonoBehaviour
{
    [Header("Detección de Caídas")]
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private bool isGrounded;
    [SerializeField] private float fallStartHeight;
    [SerializeField] private bool isFalling;

    [Header("Configuración de Daño / Derrota")]
    [SerializeField] private bool enableFallDamage = true;
    [SerializeField] private float deathHeight = -50f;          // Vacío: por debajo de esto es derrota directa
    [SerializeField] private float maxSafeFallDistance = 15f;   // Si la distancia de caída >= a esto: derrota directa

    [Header("Sistema de Respawn (opcional)")]
    [SerializeField] private bool enableRespawn = true;

    private Rigidbody playerRigidbody;
    private AbilitySystemController abilityController; // para cortar el asset al morir

    // Evita disparar derrota/respawn múltiples veces
    private bool defeatTriggered = false;

    void Awake()
    {
        abilityController = GetComponent<AbilitySystemController>();
    }

    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null)
            Debug.LogError("FallDetector necesita un Rigidbody en el jugador");

        // Inicializar la última posición segura
        if (RespawnSystem.Instance != null)
            RespawnSystem.Instance.SetSafePosition(transform.position);
    }

    // Usamos FixedUpdate para competir en la misma fase que el asset (evita NRE por llegar tarde)
    void FixedUpdate()
    {
        CheckGrounded();
        CheckDeathHeight(); // vacío -> derrota directa
    }

    void CheckGrounded()
    {
        bool nowGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask);

        // Aterrizaje
        if (nowGrounded && !isGrounded)
        {
            isGrounded = true;

            if (isFalling && enableFallDamage)
            {
                float totalFallDistance = fallStartHeight - transform.position.y;

                // 🔴 Derrota directa por caída letal (gran altura)
                if (!defeatTriggered && totalFallDistance >= maxSafeFallDistance)
                {
                    defeatTriggered = true;
                    Debug.Log($"Caída letal ({totalFallDistance:F1} m) → Derrota directa");

                    // Cortamos inmediatamente el controller del asset para evitar DieAbility.UpdateAbility
                    if (abilityController) abilityController.enabled = false;

                    GoToDefeat();
                    isFalling = false;
                    return; // no seguimos con daño ni safe position
                }

                // Si no fue letal, aplicá daño por tiempo como siempre
                if (TimeLifeManager.Instance != null)
                    TimeLifeManager.Instance.ProcessFallDamage(totalFallDistance);
            }

            isFalling = false;

            // Actualizar posición segura si tenés respawn system
            if (RespawnSystem.Instance != null && enableRespawn)
                RespawnSystem.Instance.SetSafePosition(transform.position);
        }
        // Comienzo de caída
        else if (!nowGrounded && isGrounded)
        {
            isGrounded = false;
            isFalling = true;
            fallStartHeight = transform.position.y;
        }
    }

    void CheckDeathHeight()
    {
        if (!defeatTriggered && transform.position.y < deathHeight)
        {
            defeatTriggered = true;
            Debug.Log("Caída al vacío → Derrota directa");

            // Cortar asset antes del cambio de escena
            if (abilityController) abilityController.enabled = false;

            GoToDefeat();
        }
    }

    // Llama a la pantalla de derrota usando tu GameStateManager; si no existe, carga la escena directamente.
    private void GoToDefeat()
    {
        // Opcional: suavizar físicas/controles antes de cambiar de escena
        if (playerRigidbody) playerRigidbody.linearVelocity = Vector3.zero;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnGameOver();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Derrota");
    }

    void OnDrawGizmosSelected()
    {
        // Ray de chequeo de suelo
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);

        // Línea de “muerte” por vacío
        Gizmos.color = Color.red;
        Vector3 deathLine = new Vector3(transform.position.x, deathHeight, transform.position.z);
        Gizmos.DrawLine(deathLine - Vector3.right * 5f, deathLine + Vector3.right * 5f);
        Gizmos.DrawLine(deathLine - Vector3.forward * 5f, deathLine + Vector3.forward * 5f);
    }

    // Helpers públicos
    public bool IsCurrentlyGrounded() => isGrounded;
    public bool IsCurrentlyFalling() => isFalling;
    public float GetCurrentFallDistance() => isFalling ? (fallStartHeight - transform.position.y) : 0f;

    // Compatibilidad con RespawnSystem antiguo
    public void ResetDefeatFlag() => defeatTriggered = false;
    public void ResetRespawnFlag() { ResetDefeatFlag(); Debug.Log("ResetRespawnFlag() → alias de ResetDefeatFlag()"); }
}
