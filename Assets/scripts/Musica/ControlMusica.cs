using UnityEngine;
using UnityEngine.Audio;

public class ControlMusica : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    public void ControldeMusica (float sliderMusica)
    {
        audioMixer.SetFloat("VolumenMusica", Mathf.Log10(sliderMusica) * 20);
    }
}
