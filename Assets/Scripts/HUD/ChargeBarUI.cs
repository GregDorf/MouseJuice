using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChargeBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fillTransform;
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Settings")]
    // ћассив высот дл€ каждого состо€ни€ зар€да (от 0 до max)
    // –азмер массива в инспекторе должен быть равен (charge_count + 1)
    [SerializeField] private float[] heights;

    [Header("Flash")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.15f;

    private Material originalMaterial;
    private int lastChargeCount;

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();

        originalMaterial = fillImage.material;
        lastChargeCount = playerMovement.charge_counter;

        UpdateBarHeight(playerMovement.charge_counter);
    }

    private void Update()
    {
        int currentCharge = playerMovement.charge_counter;

        // ќбновл€ем высоту, если зар€д изменилс€
        if (currentCharge != lastChargeCount)
        {
            UpdateBarHeight(currentCharge);

            StopAllCoroutines();
            StartCoroutine(Flash());

            lastChargeCount = currentCharge;
        }
    }

    private void UpdateBarHeight(int charge)
    {
        // ѕроверка на выход за границы массива
        if (charge >= 0 && charge < heights.Length)
        {
            Vector2 size = fillTransform.sizeDelta;
            size.y = heights[charge];
            fillTransform.sizeDelta = size;
        }
    }

    private IEnumerator Flash()
    {
        fillImage.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        fillImage.material = originalMaterial;
    }
}