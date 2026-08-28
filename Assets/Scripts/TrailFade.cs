using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TrailFade : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.3f;

    private TrailRenderer trail;
    private Gradient originalGradient;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        originalGradient = trail.colorGradient;
    }

    public void StartFade()
    {
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        float time = 0f;

        GradientColorKey[] colors = originalGradient.colorKeys;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);

            Gradient newGradient = new();

            GradientAlphaKey[] alphas = new GradientAlphaKey[]
            {
                new(alpha, 0),
                new(0, 1)
            };

            newGradient.SetKeys(colors, alphas);
            trail.colorGradient = newGradient;

            yield return null;
        }

        trail.emitting = false;

        // возвращаем оригинальный градиент
        trail.colorGradient = originalGradient;
    }
}

/* TODO:
 */