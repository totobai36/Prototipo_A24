using UnityEngine;
using System.Collections;

public class LevelTransformationManager : MonoBehaviour
{
    [Header("Elementos a Activar")]
    [SerializeField] private GameObject[] trapsToActivate;
    [SerializeField] private GameObject[] lasersToActivate;
    [SerializeField] private GameObject[] movingPlatforms;
    [SerializeField] private GameObject[] collapsiblePlatforms;

    [Header("Elementos a Desactivar")]
    [SerializeField] private GameObject[] safeElementsToDisable;
    [SerializeField] private GameObject[] lightsToTurnOff;

    [Header("Configuración de Transformación")]
    [SerializeField] private float transformationDelay = 0.5f;
    [SerializeField] private bool useSequentialActivation = true;
    [SerializeField] private float sequentialDelay = 0.2f;

    [Header("Efectos")]
    [SerializeField] private ParticleSystem transformationEffect;
    [SerializeField] private AudioClip transformationSound;
    [SerializeField] private float screenShakeIntensity = 5f;
    [SerializeField] private float screenShakeDuration = 2f;

    public void TransformLevel()
    {
        StartCoroutine(TransformLevelCoroutine());
        Debug.Log("¡INICIANDO TRANSFORMACIÓN DEL NIVEL!");
    }

    IEnumerator TransformLevelCoroutine()
    {
        PlayTransformationEffects();
        yield return new WaitForSeconds(transformationDelay);

        if (useSequentialActivation)
        {
            yield return StartCoroutine(SequentialTransformation());
        }
        else
        {
            InstantTransformation();
        }

        Debug.Log("Transformación del nivel completada");
    }

    IEnumerator SequentialTransformation()
    {
        yield return StartCoroutine(SequentialToggle(safeElementsToDisable, false));
        yield return StartCoroutine(SequentialToggle(trapsToActivate, true));
        yield return StartCoroutine(SequentialToggle(lasersToActivate, true));
        yield return StartCoroutine(SequentialToggle(movingPlatforms, true));
        yield return StartCoroutine(SequentialToggle(collapsiblePlatforms, true));
    }

    IEnumerator SequentialToggle(GameObject[] objects, bool activeState)
    {
        if (objects == null) yield break;
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(activeState);
                if (transformationEffect != null)
                    Instantiate(transformationEffect, obj.transform.position, Quaternion.identity);

                yield return new WaitForSeconds(sequentialDelay);
            }
        }
    }

    void InstantTransformation()
    {
        ToggleObjects(safeElementsToDisable, false);
        ToggleObjects(lightsToTurnOff, false);
        ToggleObjects(trapsToActivate, true);
        ToggleObjects(lasersToActivate, true);
        ToggleObjects(movingPlatforms, true);
        ToggleObjects(collapsiblePlatforms, true);
    }

    void ToggleObjects(GameObject[] objects, bool activeState)
    {
        if (objects == null) return;
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(activeState);
        }
    }

    void PlayTransformationEffects()
    {
        if (transformationEffect != null)
            transformationEffect.Play();

        if (transformationSound != null)
            AudioSource.PlayClipAtPoint(transformationSound, transform.position);

        CameraShake cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
        if (cameraShake != null)
            cameraShake.Shake(screenShakeIntensity, screenShakeDuration);
    }
}