using System.Collections;
using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [Header("Health & Armor Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int maxArmor = 0;
    [SerializeField] private int currentHealth;
    [SerializeField] private int currentArmor;

    private Enemy enemy;
    [SerializeField] private GameObject enemyCorpse;

    private EnemySound sound;

    private bool isDead = false;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentArmor = maxArmor;

        enemy = GetComponent<Enemy>();
        sound = GetComponent<EnemySound>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        if (currentArmor > 0)
        {
            if (currentArmor >= damage)
            {
                currentArmor -= damage;
                damage = 0;
            }
            else
            {
                damage -= currentArmor;
                currentArmor = 0;
            }
        }

        if (damage > 0)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            // Проигрываем звук получения урона при любом нанесении урона
            if (sound != null)
            {
                sound.PlayHit();
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void RegisterPlayerHit(bool isCharged, int baseDamage, int chargedBonusDamage)
    {
        if (isDead) return;

        int totalDamage = baseDamage;

        if (isCharged)
        {
            totalDamage += chargedBonusDamage;
            enemy.AnimationSetTrig("Push", 0.5f);
            sound.PlayPush();
        }

        TakeDamage(totalDamage);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (enemy != null)
        {
            enemy.AnimationSetTrig("Death", 5f);
            sound.PlayDeath();
            enemy.enabled = false;
            if (enemy.movement != null) enemy.movement.Stop();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(0.7f);

        if (enemyCorpse != null)
        {
            GameObject corpse = Instantiate(enemyCorpse, transform.position, transform.rotation);

            SpriteRenderer enemySprite = GetComponentInChildren<SpriteRenderer>();
            SpriteRenderer corpseSprite = corpse.GetComponentInChildren<SpriteRenderer>();

            if (enemySprite != null && corpseSprite != null)
            {
                corpseSprite.flipX = enemySprite.flipX;
            }
        }

        Destroy(gameObject);
    }

    public int CurrentHealth => currentHealth;
    public int CurrentArmor => currentArmor;
}