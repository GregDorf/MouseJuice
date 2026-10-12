using DG.Tweening;
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
    [SerializeField, Tooltip("Количество выстрелов в одной серии")]
    private int shotsPerBurst = 3;
    [SerializeField, Tooltip("Задержка между выстрелами внутри серии")]
    private float timeBetweenShots = 0.1f;
    [SerializeField, Tooltip("Кулдаун после завершения серии выстрелов")]
    private float cooldownTime = 1.5f;

    [Header("Halo")]
    [SerializeField] private Halo halo;
    [SerializeField] private Transform transform_halo;

    private Enemy enemy;
    private SpriteRenderer enemySprite;
    private SpriteRenderer weaponSprite;
    private EnemySound sound;
    private bool lastFacingLeft = false;

    private bool isShooting = false;
    private bool isCoolingDown = false;

    // Главное свойство: оружие может стрелять, только если оно не занято серией и не на кулдауне
    public bool CanShoot => !isShooting && !isCoolingDown;

    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        sound = GetComponentInParent<EnemySound>();
        if (enemy != null)
        {
            enemySprite = enemy.GetComponentInChildren<SpriteRenderer>();
        }

        weaponSprite = GetComponent<SpriteRenderer>();

        if (firePoint == null)
        {
            firePoint = transform;
        }

        // Устанавливаем начальное положение Halo при старте
        UpdateHaloPosition(enemySprite != null && enemySprite.flipX);
    }

    void Update()
    {
        if (enemy == null || enemySprite == null) return;

        bool isFacingLeft = enemySprite.flipX;

        // Обновляем позицию и поворот оружия при смене направления
        if (isFacingLeft != lastFacingLeft)
        {
            lastFacingLeft = isFacingLeft;
            float currentXOffset = isFacingLeft ? -Mathf.Abs(weaponXOffset) : Mathf.Abs(weaponXOffset);
            transform.localPosition = new Vector3(currentXOffset, transform.localPosition.y, transform.localPosition.z);

            if (weaponSprite != null)
            {
                weaponSprite.flipX = isFacingLeft;
            }

            UpdateHaloPosition(isFacingLeft);
        }
    }

    private void UpdateHaloPosition(bool isFacingLeft)
    {
        if (transform_halo == null) return;

        float haloX = isFacingLeft ? -2.5f : 2.5f;
        transform_halo.localPosition = new Vector3(haloX, transform_halo.localPosition.y, transform_halo.localPosition.z);
    }

    // Метод запуска атаки. Теперь запускает серию из 3 выстрелов.
    public void Fire()
    {
        if (!CanShoot) return;

        StartCoroutine(ShootBurstRoutine());
    }

    private IEnumerator ShootBurstRoutine()
    {
        isShooting = true;

        for (int i = 0; i < shotsPerBurst; i++)
        {
            PerformSingleShot();

            // Если это не последний выстрел в серии, ждем паузу
            if (i < shotsPerBurst - 1)
            {
                yield return new WaitForSeconds(timeBetweenShots);
            }
        }

        isShooting = false;

        // После окончания всей серии включаем кулдаун
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
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        bool isFacingLeft = enemySprite != null && enemySprite.flipX;
        Vector2 shootDirection = isFacingLeft ? Vector2.left : Vector2.right;

        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        if (rbBullet != null)
        {
            rbBullet.velocity = shootDirection * bulletSpeed;
        }

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetDamage(bulletDamage);
        }

        enemy.AnimationSetTrig("Fire", 0);
        sound.PlayShot();
        if (halo != null) halo.TriggerFlash();
    }
}