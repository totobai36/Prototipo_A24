using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Asumo que este script es 'VD' pero lo nombro 'Menu'

public class VD : MonoBehaviour
{
    // Las escenas ya no se cargan aquí, se pide al GameStateManager.
    
    private void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Accept();
        }
        else if (SceneManager.GetActiveScene().name == "Derrota" && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Usar la función de volver al menú, que es más robusta
            Reboot(); 
        }
    }

    // Cambiado para usar el GameStateManager
    public void Reboot()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ReturnToMainMenu();
        }
        // Si no existe, no hace nada (lo cual es seguro).
    }

    // Cambiado para usar el GameStateManager
    public void Accept()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ReturnToMainMenu();
        }
    }

    // ⭐️ CORRECCIÓN CLAVE: El botón 'Play' inicia el flujo de juego.
    public void Play()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameSequence(); // Va al Tutorial
        }
        else
        {
            Debug.LogError("No se encontró GameStateManager. No se puede iniciar el juego.");
        }
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("Saliendo de la aplicación...");
    }
}