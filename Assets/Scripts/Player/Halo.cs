using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Halo : MonoBehaviour
{
    [Header("Halo")]
    [SerializeField] private Sprite haloSprite;
    [SerializeField] private Color flashColor = Color.yellow;
    [SerializeField] private float haloSize = 3f;
    [SerializeField] private float duration = 0.2f;

    [Header("2D Light")]
    [SerializeField] private float lightRadius = 5f;
    [SerializeField] private float lightIntensity = 2f;

    public void TriggerFlash()
    {
        StartCoroutine(SpawnHaloCoroutine());
        StartCoroutine(SpawnLightCoroutine());
    }

    private IEnumerator SpawnHaloCoroutine()
    {
        GameObject haloObject = new GameObject("FlashHalo");
        haloObject.transform.position = transform.position;
        haloObject.transform.localScale = Vector3.zero;

        SpriteRenderer renderer = haloObject.AddComponent<SpriteRenderer>();
        renderer.sprite = haloSprite;
        renderer.color = flashColor;
        renderer.sortingOrder = 10;

        Vector3 targetScale = Vector3.one * haloSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            haloObject.transform.localScale = Vector3.Lerp(
                Vector3.zero,
                targetScale,
                progress
            );

            Color color = renderer.color;
            color.a = 1f - progress;
            renderer.color = color;

            yield return null;
        }

        Destroy(haloObject);
    }

    private IEnumerator SpawnLightCoroutine()
    {
        GameObject lightObject = new GameObject("FlashLight");
        lightObject.transform.position = transform.position;

        Light2D light = lightObject.AddComponent<Light2D>();

        light.lightType = Light2D.LightType.Point;
        light.color = flashColor;
        light.pointLightOuterRadius = lightRadius;
        light.intensity = lightIntensity;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            light.intensity = Mathf.Lerp(
                lightIntensity,
                0f,
                progress
            );

            yield return null;
        }

        Destroy(lightObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = flashColor;
        Gizmos.DrawWireSphere(
            transform.position,
            lightRadius
        );
    }
}