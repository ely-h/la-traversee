using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public void TriggerShake(float duration = 0.15f, float magnitude = 0.3f)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null; 
        }

        transform.localPosition = originalPos;
    }
}