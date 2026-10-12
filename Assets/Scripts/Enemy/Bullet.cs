using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float destroyDelay = 0.25f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    private int damage;
    private bool isDead = false;

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

        // Автоматически разворачиваем пулю в сторону её полета при старте
        if (rb != null && rb.velocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // Запускаем таймер времени жизни
        StartCoroutine(LifetimeRoutine());
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if ((collisionLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            if (collision.CompareTag("Player"))
            {
                collision.GetComponent<PlayerHP>()?.Damage(damage, collision.transform);
            }

            StartCoroutine(DieRoutine());
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (isDead) yield break;
        isDead = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("end");
        }

        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }
}