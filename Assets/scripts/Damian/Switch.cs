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
    public UnityEvent OnSwitchActivated;

    void Awake()
{
    // Esto es correcto, asegura que el evento exista antes de ser accedido.
    if (OnSwitchActivated == null)
        OnSwitchActivated = new UnityEvent();
}

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
    PlaySwitchEffects();

    OnSwitchActivated?.Invoke();
    Debug.Log("Switch activado - notificando a otros sistemas");
    
    // =======================================================
    // NUEVA LÍNEA: Inicia el contador de tiempo al activarse
    // =======================================================
    if (TimeLifeManager.Instance != null)
    {
        TimeLifeManager.Instance.StartTimer(); 
    }
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