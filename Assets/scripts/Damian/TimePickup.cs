using UnityEngine;

public class TimePickup : MonoBehaviour
{
    [Header("Configuración de Tiempo a Añadir")]
    [SerializeField] private float timeToAdd = 10f;

    [Header("Configuración del Pickup")]
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private ParticleSystem pickupEffect;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el jugador tocó el objeto
        if (other.CompareTag("Player"))
        {
            // Sumar tiempo
            if (TimeLifeManager.Instance != null)
            {
                TimeLifeManager.Instance.GainTime(timeToAdd);
            }

            // Reproducir sonido
            if (pickupSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }

            // Efecto visual
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            // Destruir o desactivar el objeto
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}


