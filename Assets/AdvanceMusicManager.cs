using UnityEngine;
using System; // Necesario para [Serializable]

// Esta clase es serializada, lo que la hace visible y configurable en el Inspector.
[Serializable]
public class MusicTrack
{
    public string trackName;       // Nombre para identificar la pista (ej: "Menu", "Level 1")
    public AudioClip introClip;    // El segmento de inicio (A1) - Se reproduce una vez.
    public AudioClip mainLoopClip; // El segmento principal (A2) - Se repite.
}

[RequireComponent(typeof(AudioSource))]
public class AdvancedMusicManager : MonoBehaviour
{
    private static AdvancedMusicManager instance = null;

    [Header("Configuración de Pistas")]
    // Aquí puedes definir cuántas pistas quieras en el Inspector.
    public MusicTrack[] availableTracks;

    private AudioSource audioSource;
    private MusicTrack currentTrack;

    void Awake()
    {
        // Implementación del Singleton (evita duplicados y persistencia)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();

        // Iniciar la primera pista si está disponible
        if (availableTracks.Length > 0)
        {
            PlayTrack(availableTracks[0].trackName);
        }
    }

    void Update()
    {
        // Lógica de Transición (Intro -> Loop)
        if (currentTrack != null && currentTrack.introClip != null && currentTrack.mainLoopClip != null)
        {
            // Verificamos si estamos reproduciendo la Intro Y estamos cerca del final
            if (audioSource.clip == currentTrack.introClip && !audioSource.loop && !audioSource.isPlaying)
            {
                // La Intro ha terminado, cambiamos al Loop.
                audioSource.clip = currentTrack.mainLoopClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    /// <summary>
    /// Cambia a una nueva pista de música por su nombre.
    /// </summary>
    public void PlayTrack(string trackName)
    {
        MusicTrack newTrack = Array.Find(availableTracks, track => track.trackName == trackName);

        if (newTrack == null)
        {
            Debug.LogWarning("Pista de música no encontrada: " + trackName);
            return;
        }

        if (newTrack == currentTrack) return; // Ya estamos en esta pista.

        audioSource.Stop();
        currentTrack = newTrack;

        // Decidir si reproducir Intro o ir directamente al Loop
        if (currentTrack.introClip != null)
        {
            audioSource.clip = currentTrack.introClip;
            audioSource.loop = false; // La intro no se repite
        }
        else
        {
            // Si no hay Intro, vamos directamente al Loop.
            audioSource.clip = currentTrack.mainLoopClip;
            audioSource.loop = true;
        }

        audioSource.Play();
    }
}