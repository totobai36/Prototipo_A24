using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPosition;
    private bool isShaking = false;
    
    void Start()
    {
        originalPosition = transform.localPosition;
    }
    
    public void Shake(float intensity, float duration)
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }
    }
    
    IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);
            
            transform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPosition;
        isShaking = false;
    }
}