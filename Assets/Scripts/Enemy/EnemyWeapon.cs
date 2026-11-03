using System.Collections;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Weapon Settings")]
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private float weaponXOffset = 0.5f;

    [Header("Burst & Cooldown Settings")]
    [SerializeField, Min(1)] private int shotsPerBurst = 3;
    [SerializeField, Min(0f)] private float timeBetweenShots = 0.1f;
    [SerializeField, Min(0f)] private float cooldownTime = 1.5f;

    [Header("Combat Cycle")]
    [SerializeField, Min(1)] private int burstsBeforeRetreat = 3;

    [Header("Halo / Light")]
    [SerializeField] private Halo halo;
    [SerializeField] private Transform transform_halo;

    private Enemy enemy;
    private SpriteRenderer enemySprite;
    private SpriteRenderer weaponSprite;
    private EnemySound sound;

    private bool lastFacingLeft;
    private bool isShooting;
    private bool isCoolingDown;

    private int completedBursts;

    public bool CanShoot => !isShooting && !isCoolingDown;
    public bool IsFiring => isShooting;
    public bool IsCoolingDown => isCoolingDown;

    public int BurstsBeforeRetreat =>
        Mathf.Max(1, burstsBeforeRetreat);

    public int CompletedBursts => completedBursts;

    public bool BurstsCompleted =>
        completedBursts >= BurstsBeforeRetreat;

    private void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        sound = GetComponentInParent<EnemySound>();

        if (enemy != null)
            enemySprite = enemy.GetComponentInChildren<SpriteRenderer>();

        weaponSprite = GetComponent<SpriteRenderer>();

        if (firePoint == null)
            firePoint = transform;

        bool facingLeft = enemySprite != null && enemySprite.flipX;

        lastFacingLeft = facingLeft;
        ApplyFacingDirection(facingLeft);
    }

    private void Update()
    {
        if (enemySprite == null || isShooting)
            return;

        bool facingLeft = enemySprite.flipX;

        if (facingLeft != lastFacingLeft)
        {
            lastFacingLeft = facingLeft;
            ApplyFacingDirection(facingLeft);
        }
    }

    private void ApplyFacingDirection(bool facingLeft)
    {
        float xOffset = facingLeft
            ? -Mathf.Abs(weaponXOffset)
            : Mathf.Abs(weaponXOffset);

        transform.localPosition = new Vector3(
            xOffset,
            transform.localPosition.y,
            transform.localPosition.z);

        if (weaponSprite != null)
            weaponSprite.flipX = facingLeft;

        UpdateHaloPosition(facingLeft);
    }

    private void UpdateHaloPosition(bool facingLeft)
    {
        if (transform_halo == null)
            return;

        float haloOffset = 2.5f;
        float haloX = facingLeft ? -haloOffset : haloOffset;

        transform_halo.localPosition = new Vector3(
            haloX,
            transform_halo.localPosition.y,
            transform_halo.localPosition.z);

        SpriteRenderer haloSprite =
            transform_halo.GetComponent<SpriteRenderer>();

        if (haloSprite != null)
            haloSprite.flipX = facingLeft;
    }

    public void Fire()
    {
        if (!CanShoot || BurstsCompleted)
            return;

        bool facingLeft = enemySprite != null && enemySprite.flipX;
        UpdateHaloPosition(facingLeft);

        StartCoroutine(ShootBurstRoutine());
    }

    private IEnumerator ShootBurstRoutine()
    {
        isShooting = true;

        int count = Mathf.Max(1, shotsPerBurst);

        for (int i = 0; i < count; i++)
        {
            bool facingLeft = enemySprite != null && enemySprite.flipX;
            UpdateHaloPosition(facingLeft);

            PerformSingleShot();

            if (i < count - 1)
                yield return new WaitForSeconds(timeBetweenShots);
        }

        isShooting = false;
        completedBursts++;

        if (!BurstsCompleted)
            StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;

        yield return new WaitForSeconds(cooldownTime);

        isCoolingDown = false;
    }

    private void PerformSingleShot()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity);

        bool facingLeft = enemySprite != null && enemySprite.flipX;

        Vector2 shootDirection = facingLeft
            ? Vector2.left
            : Vector2.right;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();

        if (bulletRb != null)
            bulletRb.velocity = shootDirection * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
            bulletScript.SetDamage(bulletDamage);

        if (enemy != null)
            enemy.AnimationSetTrig("Fire", 0f);

        if (sound != null)
            sound.PlayShot();

        if (halo != null)
            halo.TriggerFlash();
    }

    public void ResetCombatCycle()
    {
        completedBursts = 0;
    }
}