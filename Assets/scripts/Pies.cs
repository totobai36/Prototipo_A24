using UnityEngine;

public class Pies : MonoBehaviour
{
    private PlayerMov playerMovScript;
    private PlayerWallRun wallRunScript; 
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = -1;
    
    void Awake()
    {
        // Busca el script PlayerMov en el objeto padre
        playerMovScript = GetComponentInParent<PlayerMov>();
        
        // Busca el script PlayerWallRun en el objeto padre
        wallRunScript = GetComponentInParent<PlayerWallRun>();

        if (playerMovScript == null)
        {
            Debug.LogError("Pies.cs: ¡No se encontró el script PlayerMov.cs en el padre!");
        }
        
        // FIX: Configurar groundLayer si no está configurado
        if (groundLayer.value == -1 || groundLayer.value == 0)
        {
            groundLayer = ~0; // Incluir todas las capas por defecto
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // FIX: Verificar que el objeto está en la capa correcta
        if (!IsInLayerMask(other.gameObject, groundLayer))
            return;

        // Solo marcar como en suelo si NO estamos en wallrun
        bool isRunning = wallRunScript != null && wallRunScript.IsWallRunning;

        if (playerMovScript != null && !isRunning)
        {
            playerMovScript.EnSuelo = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // FIX: Verificar que el objeto está en la capa correcta
        if (!IsInLayerMask(other.gameObject, groundLayer))
            return;

        if (playerMovScript != null)
        {
            playerMovScript.EnSuelo = false;
        }
    }
    
    // FIX: Método helper para verificar capas
    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) != 0;
    }
    
    // Método para debug - mostrar el área del trigger
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = playerMovScript != null && playerMovScript.EnSuelo ? Color.green : Color.red;
            
            if (col is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
        }
    }
}