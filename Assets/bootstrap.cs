using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private GameObject audioManagerPrefab;
    [SerializeField] private GameObject gameManagerPrefab;

    private void Awake()
    {
        // Crear sistemas globales si no existen
        if (AudioManager.Instance == null && audioManagerPrefab != null)
            Instantiate(audioManagerPrefab);

        if (GameStateManager.Instance == null && gameManagerPrefab != null)
            Instantiate(gameManagerPrefab);

        // Cargar la primera escena jugable
        SceneManager.LoadScene("Inicio");
    }
}
