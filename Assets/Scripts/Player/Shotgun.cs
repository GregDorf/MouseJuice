using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private ParticleSystem smoke;
    [SerializeField] private ParticleSystem blanks;

    [Header("Shotgun Damage Settings")]
    [SerializeField] private int shotgunDamage = 15;
    [SerializeField] private LayerMask enemyLayer;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Halo halo;
    [SerializeField] private Collider2D triggerCollider;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        halo = GetComponentInChildren<Halo>();

        // Получаем коллайдер самого дробовика
        if (triggerCollider == null)
        {
            triggerCollider = GetComponentInChildren<Collider2D>();
            if (triggerCollider == null) Debug.LogWarning($"{name}: Collider2D not found on Shotgun!");
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: Shotgun Collider2D should have 'Is Trigger' enabled!");
        }
    }

    void Update()
    {
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;

        Vector2 direction = mouse - transform.position;

        if (direction.x > 0)
        {
            spriteRenderer.flipY = false;
        }
        else
        {
            spriteRenderer.flipY = true;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void HideShotgun()
    {
        spriteRenderer.enabled = false;
    }

    public void ShowShotgun()
    {
        spriteRenderer.enabled = true;
    }

    public bool Fire()
    {
        animator.SetTrigger("Shot");
        halo.TriggerFlash();
        smoke.Play();

        // Сканируем зону поражения, наносим урон и возвращаем true, если задели хотя бы одного врага
        return ScanAndDamageEnemies();
    }

    private bool ScanAndDamageEnemies()
    {
        if (triggerCollider == null) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemyLayer;
        filter.useTriggers = true; // Учитываем коллайдеры-триггеры врагов, если они есть

        List<Collider2D> results = new List<Collider2D>();
        int hitCount = triggerCollider.OverlapCollider(filter, results);
        bool hitAny = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (results[i] != null)
            {
                // Используем GetComponentInParent на случай, если хитбокс врага находится на дочернем объекте
                EnemyHP enemyHP = results[i].GetComponentInParent<EnemyHP>();
                if (enemyHP != null)
                {
                    enemyHP.TakeDamage(shotgunDamage);
                    hitAny = true;
                }
            }
        }

        return hitAny;
    }

    public void Reload()
    {
        animator.SetTrigger("Reload");
        blanks.Play();
    }
}