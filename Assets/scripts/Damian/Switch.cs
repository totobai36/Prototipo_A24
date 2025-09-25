using UnityEngine;
using UnityEngine.Events;

public class Switch : MonoBehaviour
{
    [Header("Estado")]
    [SerializeField] private bool isActivated = false;
    
    [Header("Efectos Visuales del Switch")]
    [SerializeField] private GameObject visualFeedback;
    [SerializeField] private Animator switchAnimator;
    [SerializeField] private ParticleSystem activationEffect;
    
    [Header("Eventos - Principio Open/Closed")]
    public UnityEvent OnSwitchActivated; // Otros sistemas se suscriben a esto
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateSwitch();
        }
    }
    
    void ActivateSwitch()
    {
        isActivated = true;
        
        // Solo maneja efectos visuales propios del switch
        PlaySwitchEffects();
        
        // Notifica a otros sistemas (Principio de Inversión de Dependencia)
        OnSwitchActivated?.Invoke();
        
        Debug.Log("Switch activado - notificando a otros sistemas");
    }
    
    void PlaySwitchEffects()
    {
        if (visualFeedback != null)
            visualFeedback.SetActive(true);
        
        if (switchAnimator != null)
            switchAnimator.SetTrigger("Activate");
        
        if (activationEffect != null)
            activationEffect.Play();
    }
}
