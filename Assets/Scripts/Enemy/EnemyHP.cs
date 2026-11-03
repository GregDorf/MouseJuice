using System.Collections;
using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [Header("Health & Armor Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int maxArmor = 0;
    [SerializeField] private int currentHealth;
    [SerializeField] private int currentArmor;

    [SerializeField] private GameObject enemyCorpse;

    private Enemy enemy;
    private EnemySound sound;

    private bool isDead;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentArmor = maxArmor;

        enemy = GetComponent<Enemy>();
        sound = GetComponent<EnemySound>();

        if (sound == null)
            sound = GetComponentInChildren<EnemySound>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        if (currentArmor > 0)
        {
            int absorbed = Mathf.Min(currentArmor, damage);
            currentArmor -= absorbed;
            damage -= absorbed;
        }

        if (damage > 0)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (sound != null)
                sound.PlayHit();
        }

        if (currentHealth <= 0)
            Die();
    }

    public void RegisterPlayerHit(bool isCharged, int baseDamage, int chargedBonusDamage)
    {
        if (isDead)
            return;

        int totalDamage = baseDamage;

        if (isCharged)
        {
            totalDamage += chargedBonusDamage;

            if (enemy != null)
                enemy.AnimationSetTrig("Push", 0.5f);

            if (sound != null)
                sound.PlayPush();
        }

        TakeDamage(totalDamage);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (enemy != null)
        {
            enemy.AnimationSetTrig("Death", 5f);

            if (sound != null)
                sound.PlayDeath();

            enemy.enabled = false;

            if (enemy.movement != null)
                enemy.movement.Stop();
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
            col.enabled = false;

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(0.7f);

        if (enemyCorpse != null)
        {
            GameObject corpse = Instantiate(
                enemyCorpse,
                transform.position,
                transform.rotation);

            SpriteRenderer sourceSprite = GetComponentInChildren<SpriteRenderer>();
            SpriteRenderer corpseSprite = corpse.GetComponentInChildren<SpriteRenderer>();

            if (sourceSprite != null && corpseSprite != null)
                corpseSprite.flipX = sourceSprite.flipX;
        }

        Destroy(gameObject);
    }

    public int CurrentHealth => currentHealth;
    public int CurrentArmor => currentArmor;
}
