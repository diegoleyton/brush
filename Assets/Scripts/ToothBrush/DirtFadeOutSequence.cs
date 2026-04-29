using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DirtFadeOutSequence : PausableAnim
{
    [SerializeField] private Image[] images;
    [SerializeField] private float fadeDuration = 3f;

    static int[][] groups_ = { new int[] { 0, 2 }, new int[] { 5, 3 }, new int[] { 1, 4 } };
    int currentGroup_ = 0;

    public void Run(float duration)
    {
        StartCoroutine(FadeGroup(duration));
    }

    private IEnumerator FadeGroup(float duration)
    {
        var indices = groups_[currentGroup_];
        currentGroup_++;
        float waitTime = duration / indices.Length;

        foreach (int i in indices)
        {
            if(IsPaused) {
                yield return 0;
                continue;
            }

            if (images[i] != null)
            {
                StartCoroutine(FadeOut(images[i], fadeDuration));
            }
            yield return StartCoroutine(Wait(waitTime)); // espera antes de la siguiente imagen
        }
    }

    private IEnumerator FadeOut(Image img, float duration)
    {
        Color startColor = img.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float t = 0;
        while (t < duration)
        {
            if(IsPaused) {
                yield return 0;
                continue;
            }
            t += Time.deltaTime;
            img.color = Color.Lerp(startColor, endColor, t / duration);
            yield return null;
        }

        img.color = endColor;

        Destroy(img.gameObject);
    }

    private IEnumerator Wait(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            if(IsPaused) {
                yield return 0;
                continue;
            }
            t += Time.deltaTime;
            yield return 0;
        }
    }
}