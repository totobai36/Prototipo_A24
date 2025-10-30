using UnityEngine;
using DiasGames.Components; // Para Ragdoll 
using DiasGames.AbilitySystem.Core; // Para AbilitySystemController

public class CharacterDeathHandler : MonoBehaviour
{
    private Ragdoll _ragdoll;
    private AbilitySystemController _abilitySystem;
    private bool isDead = false; // Flag para evitar llamadas duplicadas

    void Awake()
    {
        _ragdoll = GetComponent<Ragdoll>();
        _abilitySystem = GetComponent<AbilitySystemController>();
        
        // Asegúrate de que TimeLifeManager exista en la escena
        TimeLifeManager timeManager = FindFirstObjectByType<TimeLifeManager>();
        if (timeManager != null)
        {
            // Suscripción a la condición de muerte por TIEMPO AGOTADO
            timeManager.OnGameOver.AddListener(HandleDeath);
        }
    }

    // Método que se llama cuando el personaje muere (por tiempo, o si se llama directamente)
    private void HandleDeath()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Manejo de Muerte Activado: Llamando a Ragdoll y deteniendo el personaje.");
        
        // 1. Desactivar el cerebro de habilidades para que no intente moverse más (Sistema Intocable)
        if (_abilitySystem != null)
        {
            _abilitySystem.enabled = false;
        }
        
        // 2. Activar el Ragdoll del sistema intocable
        if (_ragdoll != null)
        {
            _ragdoll.ActivateRagdoll();
        }
        
        // --- CORRECCIÓN LÍNEA 49 ---
        // La llamada a 'GameStateManager.Instance.OnGameOver()' se elimina.
        // GameState Manager ya es notificado por TimeLifeManager.
    }
}