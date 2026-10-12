using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    // настройки неуязвимости после получения урона
    [Header("Damage Settings")]
    [SerializeField] private float invulnerability_time = 0.4f;
    [SerializeField] private float knockback_force = 8f;
    [SerializeField] private float transparency = 0.5f;
    [SerializeField] private LayerMask enemy_layer;
    [SerializeField] private LayerMask enemy_attack_layer;

    [Header("Death Settings")]
    [SerializeField] private GameObject playerCorpse; // Префаб трупа игрока

    [Header("HP Settings")]
    // объявляем все необходимые на старте переменные, хп, броня, количество возрождений, время нахождения в зоне газа
    public int hit_points = 100;
    [SerializeField] private int armor_points = 0;
    [SerializeField] private int num_of_ressurections = 3;
    [SerializeField] private float max_time_in_gas = 10f;
    [SerializeField] private float gas_time_speed = 3f;
    [SerializeField] private string gas_tag = "Gas";

    // локальные переменные для отслеживания состояний переменных выше
    [HideInInspector] public int current_hp;
    private int current_ressurections;
    private float current_gas_time;

    [HideInInspector] public bool is_invulnerable = false;

    private PlayerSoundManager soundManager;
    private new Rigidbody2D rigidbody;
    private SpriteRenderer sprite_renderer;
    private Shotgun shotgun;
    private Collider2D player_collider;
    private PlayerMovement movement;
    private Animator playerAnimator;
    private PlayerRespawner respawner;
    [HideInInspector] public bool isDead = false;


    // определяем наши локальные переменные, инициализированные выше
    private void Start()
    {
        respawner = GameObject.FindWithTag("Respawner").GetComponent<PlayerRespawner>();

        playerAnimator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
        player_collider = GetComponentInChildren<Collider2D>();
        sprite_renderer = GetComponentInChildren<SpriteRenderer>();
        shotgun = GetComponentInChildren<Shotgun>();
        movement = GetComponent<PlayerMovement>();
        soundManager = GetComponent<PlayerSoundManager>();

        current_hp = hit_points;
        current_ressurections = num_of_ressurections;
        current_gas_time = 0;
    }

    // основной цикл программы
    private void Update()
    {
        // включаем столкновения с врагами только во время рывка
        Physics2D.IgnoreLayerCollision(gameObject.layer, Mathf.RoundToInt(Mathf.Log(enemy_layer.value, 2)), !movement.player_incharge);

        // 1. Защита от отрицательных значений
        if (current_hp < 0) current_hp = 0;

        // 2. Логика смерти
        if (current_hp <= 0 && !isDead)
        {
            StartCoroutine(HandleDeath());
        }

        // потихоньку опустошаем шкалу газа
        if (current_gas_time > 0)
        {
            current_gas_time -= Time.deltaTime;
        }
    }

    private IEnumerator HandleDeath()
    {
        isDead = true;
        current_hp = 0;
        current_ressurections--;

        shotgun.HideShotgun();

        // ПРОИГРЫВАЕМ АНИМАЦИЮ В ЛЮБОМ СЛУЧАЕ
        if (playerAnimator != null)
        {
            soundManager.PlayDeath();
            playerAnimator.SetTrigger("death");
        }

        // Считаем время анимации: 25 кадров / 18 fps = 1.38 секунд
        float animDuration = 25f / 18f;
        yield return new WaitForSeconds(animDuration);

        // --- СПАВНИМ ТРУП ИГРОКА ПО ОКОНЧАНИИ АНИМАЦИИ ---
        if (playerCorpse != null)
        {
            GameObject corpse = Instantiate(playerCorpse, transform.position, transform.rotation);

            // Копируем параметр flipX текущего спрайта игрока на труп
            SpriteRenderer corpseSprite = corpse.GetComponentInChildren<SpriteRenderer>();
            if (sprite_renderer != null && corpseSprite != null)
            {
                corpseSprite.flipX = sprite_renderer.flipX;
            }
        }

        if (current_ressurections < 0)
        {
            // Окончательная смерть
            gameObject.SetActive(false);
        }
        else
        {
            // 1. Отключаем игрока ПОСЛЕ анимации
            gameObject.SetActive(false);

            // 2. Ищем респавнер и запускаем таймер
            var respawner = GameObject.FindWithTag("Respawner")?.GetComponent<PlayerRespawner>();
            if (respawner != null)
                respawner.StartRespawnSequence();
        }
    }

    // Новый публичный метод, который вызовет Respawner при активации игрока
    public void ResetHP()
    {
        shotgun.ShowShotgun();
        current_hp = hit_points; // Восстанавливаем HP только когда персонаж возвращается
        isDead = false;
    }

    // метод нанесения урона
    public void Damage(int _damage, Transform attacker = null)
    {
        // Если игрок уже неуязвим или делает рывок, игнорируем урон
        if (is_invulnerable || movement.player_incharge)
        {
            return;
        }

        int damageDealt = Mathf.Max(0, _damage - armor_points);
        int hpAfterDamage = current_hp - damageDealt;

        // Сначала вычитаем здоровье
        current_hp = hpAfterDamage;

        // ПРОВЕРКА: Если урон НЕ смертельный, запускаем защиту
        if (current_hp > 0 && attacker != null)
        {
            StartCoroutine(DamageProtection(attacker));
        }

        if (!isDead)
        {
            soundManager.PlayHit();
            playerAnimator.SetTrigger("hurt");
        }
    }

    // метод временной неуязвимости после получения урона
    private IEnumerator DamageProtection(Transform attacker)
    {
        is_invulnerable = true;

        // делаем игрока полупрозрачным
        Color color = sprite_renderer.color;
        color.a = transparency;
        sprite_renderer.color = color;

        // Временно скрываем дробовик при получении урона
        shotgun.HideShotgun();

        Collider2D enemy_collider = null;

        // временно отключаем столкновения с данным врагом (если он есть)
        if (attacker != null && attacker.TryGetComponent<Collider2D>(out enemy_collider))
        {
            Physics2D.IgnoreCollision(player_collider, enemy_collider, true);
        }

        // отталкиваем игрока
        if (attacker != null)
        {
            Vector2 bounce_vec = (transform.position - attacker.position).normalized;
            rigidbody.velocity = Vector2.zero;
            rigidbody.AddForce(bounce_vec * knockback_force, ForceMode2D.Impulse);
        }

        // ждем окончания неуязвимости
        yield return new WaitForSeconds(invulnerability_time);

        // возвращаем столкновения с врагом, если он всё еще существует
        if (enemy_collider != null)
        {
            Physics2D.IgnoreCollision(player_collider, enemy_collider, false);
        }

        // ВАЖНО: Всегда возвращаем видимость дробовика по окончании таймера
        shotgun.ShowShotgun();

        // возвращаем непрозрачность
        color.a = 1f;
        sprite_renderer.color = color;

        is_invulnerable = false;
    }

    // метод заполнения шкалы газа, пока игрок находится в зоне газа
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(gas_tag))
        {
            // добавляем шкалу газа
            current_gas_time += gas_time_speed * Time.deltaTime;

            // если она превысила макс значение, наносим урон
            if (current_gas_time >= max_time_in_gas)
            {
                Damage(collision.GetComponent<Damager>().TakeDamage());
                current_gas_time = 0;
            }
        }
    }

    // обработка столкновения с противником
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if ((enemy_attack_layer.value & (1 << collider.gameObject.layer)) != 0)
        {
            if (collider.gameObject.TryGetComponent<Damager>(out var damager))
            {
                Damage(damager.TakeDamage(), collider.transform);
            }
        }
    }
}