using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource explorationMusicSource;
    [SerializeField] private AudioSource countdownMusicSource;
    [SerializeField] private AudioSource gameOverMusicSource;
    [SerializeField] private AudioSource victoryMusicSource;
    
    [Header("Configuración")]
    [SerializeField] private float fadeTime = 2f;
    [SerializeField] private float countdownVolume = 0.8f;
    [SerializeField] private float explorationVolume = 0.6f;
    
    void Start()
    {
        // Iniciar con música de exploración
        PlayExplorationMusic();
    }
    
    void PlayExplorationMusic()
    {
        if (explorationMusicSource != null)
        {
            explorationMusicSource.volume = explorationVolume;
            explorationMusicSource.Play();
        }
    }
    
    public void SwitchToCountdownMusic()
    {
        StartCoroutine(CrossFadeMusic(explorationMusicSource, countdownMusicSource, countdownVolume));
        Debug.Log("Cambiando a música de countdown");
    }
    
    public void SwitchToGameOverMusic()
    {
        StartCoroutine(FadeOutAllAndPlayNew(gameOverMusicSource));
    }
    
    public void SwitchToVictoryMusic()
    {
        StartCoroutine(FadeOutAllAndPlayNew(victoryMusicSource));
    }
    
    IEnumerator CrossFadeMusic(AudioSource fromSource, AudioSource toSource, float targetVolume)
    {
        if (fromSource == null || toSource == null) yield break;
        
        // Preparar nueva música
        toSource.volume = 0f;
        toSource.Play();
        
        float elapsed = 0f;
        float startVolumeFrom = fromSource.volume;
        
        // Crossfade
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            
            fromSource.volume = Mathf.Lerp(startVolumeFrom, 0f, t);
            toSource.volume = Mathf.Lerp(0f, targetVolume, t);
            
            yield return null;
        }
        
        // Finalizar
        fromSource.Stop();
        fromSource.volume = startVolumeFrom;
        toSource.volume = targetVolume;
    }
    
    IEnumerator FadeOutAllAndPlayNew(AudioSource newSource)
    {
        AudioSource[] allSources = { explorationMusicSource, countdownMusicSource, gameOverMusicSource, victoryMusicSource };
        
        // Fade out todas las fuentes activas
        float elapsed = 0f;
        float[] startVolumes = new float[allSources.Length];
        
        for (int i = 0; i < allSources.Length; i++)
        {
            if (allSources[i] != null && allSources[i].isPlaying)
                startVolumes[i] = allSources[i].volume;
        }
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            
            for (int i = 0; i < allSources.Length; i++)
            {
                if (allSources[i] != null && allSources[i].isPlaying)
                {
                    allSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
                }
            }
            
            yield return null;
        }
        
        // Parar todas y reproducir la nueva
        for (int i = 0; i < allSources.Length; i++)
        {
            if (allSources[i] != null)
            {
                allSources[i].Stop();
                allSources[i].volume = startVolumes[i];
            }
        }
        
        if (newSource != null)
        {
            newSource.Play();
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        AudioSource[] allSources = { explorationMusicSource, countdownMusicSource, gameOverMusicSource, victoryMusicSource };
        
        foreach (AudioSource source in allSources)
        {
            if (source != null)
                source.volume *= volume;
        }
    }
}