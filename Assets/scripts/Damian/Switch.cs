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
    
    // =======================================================
    // NUEVAS PROPIEDADES DE COLOR
    // =======================================================
    [Header("Configuración de Color")]
    [SerializeField] private Color inactiveColor = Color.green; // Color inicial
    [SerializeField] private Color activatedColor = Color.red;  // Color al activarse

    private Renderer visualRenderer;
    // =======================================================

    [Header("Eventos - Principio Open/Closed")]
    public UnityEvent OnSwitchActivated;

    void Awake()
    {
        if (OnSwitchActivated == null)
            OnSwitchActivated = new UnityEvent();

        // =======================================================
        // NUEVA LÓGICA AWAKE: Obtener el Renderer y establecer color inicial
        // =======================================================
        if (visualFeedback != null)
        {
            visualRenderer = visualFeedback.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                // Asigna el color inicial (verde)
                visualRenderer.material.color = inactiveColor;
            }
            else
            {
                Debug.LogWarning("Switch: El GameObject visualFeedback no tiene un componente Renderer.");
            }
        }
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
        
        // =======================================================
        // NUEVA LÓGICA: Cambiar el color a rojo al activarse
        // =======================================================
        if (visualRenderer != null)
        {
            visualRenderer.material.color = activatedColor;
        }
        
        OnSwitchActivated?.Invoke();
        Debug.Log("Switch activado - notificando a otros sistemas");
        
        if (TimeLifeManager.Instance != null)
        {
            // NOTA: Si el GameStateManager está suscrito a OnSwitchActivated,
            // él inicia el timer. Si no, lo iniciamos aquí.
            TimeLifeManager.Instance.StartTimer(); 
        }
    }
    
    // ... (El resto del método PlaySwitchEffects no se modifica)
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