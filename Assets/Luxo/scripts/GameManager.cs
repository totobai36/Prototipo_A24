using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-20)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas clave (por nombre)")]
    public string mainMenuScene = "MainMenu";
    public string victoryScene = "Victory";
    public string defeatScene = "Defeat";
    public string levelScene = "Level";

    [Header("Transiciones")]
    [Tooltip("Retraso antes de cambiar de escena para que alcance a sonar el SFX")]
    public float transitionDelay = 0.75f;

    [Header("Música por escena (opcional)")]
    public List<SceneMusicEntry> musicByScene = new List<SceneMusicEntry>();

    [Serializable]
    public class SceneMusicEntry
    {
        public string sceneName;
        public AudioClip music;
        public bool loop = true;
        [Range(0f, 1f)] public float volume = 0.6f;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // --- Cambiar música automáticamente al cargar cada escena ---
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var am = AudioManager.Instance;
        if (!am) return;

        // Busca configuración específica para esta escena
        foreach (var entry in musicByScene)
        {
            if (!string.IsNullOrEmpty(entry.sceneName) && scene.name == entry.sceneName)
            {
                am.PlayMusic(entry.music, entry.volume, entry.loop);
                return;
            }
        }

        // Si no hay entrada específica, podés dejar la música anterior o parar:
        // am.StopMusic();
    }

    // ---------- API pública para tu juego ----------
    public void OnWin()
    {
        // SFX victoria
        AudioManager.Instance?.PlayVictory(1f);
        // Ir a la escena de victoria (o al siguiente nivel si preferís)
        StartCoroutine(LoadSceneAfter(victoryScene, transitionDelay));
    }

    public void OnLose()
    {
        // SFX derrota
        AudioManager.Instance?.PlayDefeat(1f);
        StartCoroutine(LoadSceneAfter(defeatScene, transitionDelay));
    }

    public void RestartLevel()
    {
        string current = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadSceneAfter(current, 0f));
    }

    public void LoadMainMenu() => StartCoroutine(LoadSceneAfter(mainMenuScene, 0f));
    public void LoadScene(string sceneName) => StartCoroutine(LoadSceneAfter(sceneName, 0f));

    public void LoadNextByBuildIndex()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            StartCoroutine(LoadSceneAfter(SceneUtility.GetScenePathByBuildIndex(next), 0f));
    }

    IEnumerator LoadSceneAfter(string sceneNameOrPath, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // Si te pasan una ruta (Assets/…/Scene.unity), convertir a nombre
        string sceneName = sceneNameOrPath;
        if (sceneNameOrPath.EndsWith(".unity"))
        {
            int slash = sceneNameOrPath.LastIndexOf('/');
            sceneName = sceneNameOrPath.Substring(slash + 1).Replace(".unity", "");
        }

        SceneManager.LoadScene(sceneName);
    }
}
