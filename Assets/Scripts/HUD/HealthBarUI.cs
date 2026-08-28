using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform fill;
    [SerializeField] private PlayerHP playerHP;

    [Header("Top")]
    [SerializeField] private Image topImage;
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.3f;

    [Header("Bar Settings")]
    [SerializeField] private float minHeight = 35f;
    [SerializeField] private float maxHeight = 390f;

    private float maxHP;
    private float lastHP;

    private Material originalTopMaterial;
    private Material originalFillMaterial; // Сохраняем оригинал для Fill
    private Coroutine flashRoutine;

    private void Awake()
    {
        playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();
    }

    private void Start()
    {
        maxHP = playerHP.hit_points;
        lastHP = playerHP.current_hp;

        // Сохраняем оригинальные материалы
        originalTopMaterial = topImage.material;
        originalFillMaterial = fillImage.material;

        UpdateBar();
    }

    private void Update()
    {
        float currentHP = playerHP.current_hp;

        if (currentHP < lastHP)
        {
            OnHPChanged();
        }

        lastHP = currentHP;
        UpdateBar();
    }

    private void UpdateBar()
    {
        float hp01 = Mathf.Clamp01(playerHP.current_hp / maxHP);
        float height = Mathf.Lerp(minHeight, maxHeight, hp01);

        Vector2 size = fill.sizeDelta;
        size.y = height;
        fill.sizeDelta = size;
    }

    private void OnHPChanged()
    {
        if (flashRoutine == null)
        {
            flashRoutine = StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        if (flashMaterial == null)
        {
            flashRoutine = null;
            yield break;
        }

        // Сохраняем текущие цвета, чтобы потом вернуть их обратно
        Color originalTopColor = topImage.color;
        Color originalFillColor = fillImage.color;

        // Ставим белый цвет для обоих элементов, чтобы вспышка была чистой
        topImage.color = Color.white;
        fillImage.color = Color.white;

        // Включаем материал
        topImage.material = flashMaterial;
        fillImage.material = flashMaterial;

        yield return new WaitForSeconds(flashDuration);

        // Возвращаем материалы
        topImage.material = originalTopMaterial;
        fillImage.material = originalFillMaterial;

        // Возвращаем оригинальные цвета
        topImage.color = originalTopColor;
        fillImage.color = originalFillColor;

        flashRoutine = null;
    }
}