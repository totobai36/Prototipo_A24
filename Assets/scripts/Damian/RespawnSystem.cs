using UnityEngine;
using System.Collections;
using DiasGames.Components; // Necesario para acceder a MovementComponent, Ragdoll y otras interfaces

public class RespawnSystem : MonoBehaviour
{
    [Header("Configuración de Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private float timePenalty = 15f;
    
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private FallDetector fallDetector; // Referencia al script que me permitiste modificar
    
    [Header("Configuración Simple")]
    [SerializeField] private float heightOffset = 0.0f; // Offset vertical para la posición segura
    
    private bool isRespawning = false;
    private Vector3 lastSafePosition;
    private bool hasSafePosition = false;
    
    public static RespawnSystem Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Buscar jugador por Tag (debe tener la Tag "Player")
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
            }
        }
        
        // Buscar FallDetector en el jugador (si no está asignado)
        if (fallDetector == null && player != null)
        {
            fallDetector = player.GetComponent<FallDetector>();
        }

        // Inicializar la posición segura
        if (player != null)
        {
            SetSafePosition(player.position);
        }
    }

    // Método PÚBLICO para iniciar el Respawn (llamado por FallDetector)
    public void Respawn()
    {
        if (isRespawning || !hasSafePosition) return;

        // Llamar a la corrutina de Respawn
        StartCoroutine(PerformRespawn(lastSafePosition));
    }

    // Actualiza la última posición donde el personaje estaba seguro (ej. al tocar el suelo)
    public void SetSafePosition(Vector3 position)
    {
        lastSafePosition = position + Vector3.up * heightOffset;
        hasSafePosition = true;
    }

    private IEnumerator PerformRespawn(Vector3 respawnPosition)
    {
        isRespawning = true;
        
        // --- 1. Obtener Componentes del Personaje ---
        MovementComponent movementComponent = player.GetComponent<MovementComponent>(); // Componente Intocable
        CharacterController charController = player.GetComponent<CharacterController>();  // Componente de Unity (asumido)
        Ragdoll ragdoll = player.GetComponent<Ragdoll>(); // Componente Intocable

        // --- 2. Desactivar Movimiento y Control ---
        // Deshabilitamos el CharacterController y el script de lógica de movimiento
        // para un teleport limpio y para detener la gravedad/física del jugador.
        if (movementComponent != null)
        {
            movementComponent.enabled = false; 
        }
        if (charController != null)
        {
            charController.enabled = false;
        }
        
        // Si por alguna razón el Ragdoll está activo, lo desactivamos 
        // (asumiendo que tu Ragdoll.cs tiene un método público para hacerlo, 
        // aunque tu snippet solo muestra `ActivateRagdoll()`).
        // Si tu Ragdoll no tiene `DeactivateRagdoll()`, Unity lo maneja al reactivar el CharacterController.
        
        // --- 3. TELEPORTAR ---
        player.position = respawnPosition;
        
        // --- 4. Esperar y Penalizar ---
        yield return new WaitForSeconds(respawnDelay);
        
        // Aplicar Penalización de tiempo
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.LoseTime(timePenalty);
        }
        
        // --- 5. Reactivar Componentes ---
        if (charController != null)
        {
            charController.enabled = true;
        }
        if (movementComponent != null)
        {
            movementComponent.enabled = true; // Reactiva la lógica del MovementComponent
        }
        
        // --- 6. Finalizar Respawn ---
        if (fallDetector != null)
        {
            // Llama al método que agregamos a FallDetector para que pueda volver a detectar caídas.
            fallDetector.ResetRespawnFlag(); 
        }
        
        isRespawning = false;
    }
    
    public bool IsRespawning()
    {
        return isRespawning;
    }
    
    void OnDrawGizmosSelected()
    {
        if (hasSafePosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastSafePosition, 0.5f);
        }
    }
}