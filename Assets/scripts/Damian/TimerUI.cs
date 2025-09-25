using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Slider timeSlider; // configurado entre 0..1 o 0..baseTime
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image fillImage; // para cambiar color

    [Header("Opciones")]
    [SerializeField] private bool sliderUsesPercentage = true; // si true, Slider.value será 0..1

    void Start() // Se ejecuta después de TODOS los Awakes() (donde se inicializa TimeLifeManager.Instance)
{
    if (TimeLifeManager.Instance != null)
    {
        TimeLifeManager.Instance.OnTimerUpdated.AddListener(UpdateTimerUI);
        
        // **IMPORTANTE:** Llamar a la función inmediatamente para mostrar el tiempo
        // en el primer frame y evitar el "New Text" inicial.
        UpdateTimerUI(TimeLifeManager.Instance.CurrentTime); 
    }
    else
    {
        Debug.LogError("TimerUI: No se pudo encontrar TimeLifeManager.Instance para suscribirse.");
    }
}

    void OnDisable()
    {
        if (TimeLifeManager.Instance != null)
            TimeLifeManager.Instance.OnTimerUpdated.RemoveListener(UpdateTimerUI);
    }

    public void UpdateTimerUI(float time)
    {
        if (TimeLifeManager.Instance == null) return;

        float baseTime = TimeLifeManager.Instance.BaseTime;
        float percentage = baseTime <= 0 ? 0f : time / baseTime;

        if (timeText != null)
        {
            // Mostrar como MM:SS
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (timeSlider != null)
        {
            if (sliderUsesPercentage)
                timeSlider.value = percentage;
            else
                timeSlider.value = time;
        }

        if (fillImage != null)
        {
            fillImage.color = TimeLifeManager.Instance.GetTimerColor();
        }
    }
}