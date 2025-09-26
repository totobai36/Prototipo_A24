using UnityEngine;

public class ExtractionPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Validar que sea el jugador
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 2. Validar que la instancia del GameStateManager exista
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("ExtractionPoint: No se encontró GameStateManager.");
            return;
        }

        // 3. ¡VALIDACIÓN CLAVE! Solo ganar si el temporizador está activo.
        if (GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.CountdownActive)
        {
            // Ejecutar lógica de Victoria
            Debug.Log("CONDICIÓN DE VICTORIA CUMPLIDA: Countdown activo.");
            
            // Detener el timer (si existe)
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.StopTimer();
            }

            // Notificar al GameStateManager para cargar la escena de Victoria
            GameStateManager.Instance.OnVictory(); 
            
            // Opcional: Desactivar el Player
            other.gameObject.SetActive(false); 
        }
        else
        {
            // Si el estado es Exploration, no pasa nada y se ignora la colisión.
            Debug.Log("Punto de Extracción ignorado. La cuenta regresiva aún no ha comenzado (estado: " + GameStateManager.Instance.GetCurrentState() + ").");
        }
    }
}