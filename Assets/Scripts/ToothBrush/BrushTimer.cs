using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BrushTimer : PausableAnim
{
    [SerializeField] Image target_;

    public void StartTimer(float duration)
    {
        StartCoroutine(UpdateBar(duration));
    }

    private IEnumerator UpdateBar(float duration)
    {
        if (target_ == null || duration <= 0f)
            yield break;

        // Save initial scale
        float startScale = 1f;
        float endScale = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (IsPaused)
            {
                yield return 0;
                continue;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Lerp X scale from start to 0
            target_.fillAmount = Mathf.Lerp(startScale, endScale, t);

            yield return null;
        }

        // Ensure it ends exactly at 0
        target_.fillAmount = endScale;
    }
}