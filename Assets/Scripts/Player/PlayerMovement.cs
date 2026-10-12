using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // переменные, видимые в редакторе
    [Header("Base Movement")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float jump_force = 10f;
    [SerializeField] private int jump_count = 2;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.9f); // Смещение точки проверки под ноги
    [SerializeField] private float groundCheckRadius = 0.2f;                    // Радиус круга проверки земли

    [Header("Charge Settings")]
    public int charge_count = 2;
    [SerializeField] private float charge_distance = 5f;          // Изначальная дальность рывка без врагов
    [SerializeField] private float charge_duration = 0.2f;          // Изначальное время рывка без врагов
    [SerializeField] private float charge_min_corner = 0.5f;
    [SerializeField] private float charge_cooldown = 10f;
    [SerializeField] private AnimationCurve charge_curve;

    [Header("Enemy Hit Settings (Абсолютные значения при попадании шотганом)")]
    [SerializeField] private float enemyHitDistance = 8f;          // Дальность рывка, если шотган попал по врагу
    [SerializeField] private float enemyHitDuration = 0.35f;       // Длительность рывка, если шотган попал по врагу

    [Header("Physics Layers")]
    [SerializeField] private LayerMask charge_layers;              // Стены и препятствия, об которые рывок ломается
    [SerializeField] private LayerMask ground_layer;               // Единый слой для пола, стен и потолка (Tilemap)
    [SerializeField] private LayerMask enemy_layer;

    [Header("Shotgun")]
    [SerializeField] private GameObject shotgun;
    private Shotgun shotgun_script;

    // компоненты, необходимые для работы скрипта
    private PlayerHP hp;
    private PlayerSoundManager soundManager;
    private PlayerInaction inactive;
    private new Rigidbody2D rigidbody;
    private SpriteRenderer sprite_renderer;
    private Animator animator;
    private TrailRenderer trail_renderer;
    private int jump_counter;
    [HideInInspector] public int charge_counter;
    private float chargeCooldownTimer;
    private float gravity_scale;
    internal bool isWalled = false;
    private ObservableBool isWalledOnce = new ObservableBool(false);
    internal bool isGrounded = false;
    internal bool isWalledLeft = false;
    internal bool isWalledRight = false;
    internal bool isWallJumping; // Флаг: сейчас совершается прыжок от стены
    private readonly float wallJumpLockTime = 0.5f; // Сколько секунд длится блокировка
    private TrailFade trail_fade;
    public bool player_incharge;

    //private float uncontrol_time;


    private void Start()
    {
        // игрок отошел
        inactive = GetComponent<PlayerInaction>();
        // определение компонентов
        rigidbody = GetComponent<Rigidbody2D>();
        // компоненты в дочерних объектах
        sprite_renderer = GetComponentInChildren<SpriteRenderer>();
        // аниматор
        animator = GetComponentInChildren<Animator>();
        // след
        trail_renderer = GetComponentInChildren<TrailRenderer>();
        trail_renderer.emitting = false;
        // запоминаем значение гравитации
        gravity_scale = rigidbody.gravityScale;

        trail_fade = trail_renderer.GetComponent<TrailFade>();

        soundManager = GetComponent<PlayerSoundManager>();

        shotgun_script = shotgun.GetComponent<Shotgun>();

        hp = GetComponent<PlayerHP>();

        // задаем изначальное количество прыжков на старте
        jump_counter = 0;
        //uncontrol_time = 0;
        player_incharge = false;

        chargeCooldownTimer = charge_cooldown;

        isWalledOnce.OnChangedToTrue += soundManager.PlaySlide;
    }

    // основной цикл программы
    private void Update()
    {
        // ИСПРАВЛЕНИЕ: Проверяем землю через OverlapCircle в самом начале кадра.
        // Это исключает баги с единым Tilemap, когда игрок съезжает со стены вплотную к полу.
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle((Vector2)transform.position + groundCheckOffset, groundCheckRadius, ground_layer);

        // Если только что приземлились
        if (!wasGrounded && isGrounded)
        {
            jump_counter = jump_count;
            soundManager.PlayLanding();
        }

        SlideOnTheWall(out RaycastHit2D left_hit, out RaycastHit2D right_hit);
        Jump(left_hit, right_hit);
        Move();

        ChargeCooldown();
        if (charge_counter > 0)
        {
            Charge();
        }
        if (charge_counter > charge_count)
        {
            charge_counter = charge_count;
        }

        animator.SetBool("isWalled", isWalled);
        animator.SetBool("isGrounded", isGrounded);

        //if (uncontrol_time <= 0)
        //{
        //    rigidbody.gravityScale = gravity_scale;

        //    Move();
        //    Charge();
        //    Jump();
        //}
        //else
        //{
        //    uncontrol_time -= Time.deltaTime;
        //}
    }

    // метод движения вдоль оси X (горизонтально)
    private void Move()
    {
        // Блокируем горизонтальный ввод во время прыжка от стены и во время рывка
        if (isWallJumping || player_incharge) return;

        float direction = Input.GetAxis("Horizontal");

        // Если висим на левой стене и давим влево -> обнуляем ввод
        if (isWalledLeft && direction < 0) direction = 0;

        // Если висим на правой стене и давим вправо -> обнуляем ввод
        if (isWalledRight && direction > 0) direction = 0;

        if (!isWalled)
        {
            // Повороты спрайта (ориентируемся на реальное направление движения или курсор)
            if (direction > 0) sprite_renderer.flipX = false;
            else if (direction < 0) sprite_renderer.flipX = true;
            else if (direction == 0)
            {
                Vector2 start = transform.position;
                Vector2 mouse_pos = MousePosToWorld();
                sprite_renderer.flipX = (start.x - mouse_pos.x) >= 0;
            }

            // Движение (теперь переменная direction "чистая" и не даст идти в стену)
            if (direction != 0 && (isGrounded || !isWalled))
            {
                rigidbody.velocity = new Vector2(speed * direction, rigidbody.velocity.y);
                animator.SetBool("isRunning", true);
                if (isGrounded) soundManager.PlaySteps();
                inactive.PlayerFirstActivity();
            }
            else
            {
                animator.SetBool("isRunning", false);
            }
        }
    }

    // метод прыжка
    private void Jump(RaycastHit2D left_hit, RaycastHit2D right_hit)
    {
        bool isWallJump = isWalled && (left_hit.collider != null || right_hit.collider != null);

        if (Input.GetKeyDown(KeyCode.Space) && (jump_counter > 0 || isWallJump))
        {
            jump_counter--;
            animator.SetTrigger("jump");
            soundManager.PlayJump();
            inactive.PlayerFirstActivity();

            rigidbody.velocity = new Vector2(rigidbody.velocity.x, 0);

            if (isWallJump)
            {
                isWallJumping = true;
                float wallDir = (left_hit.collider != null) ? 1 : -1;
                rigidbody.AddForce(new Vector2(wallDir * 5f, jump_force), ForceMode2D.Impulse);

                // Запускаем таймер сброса
                StartCoroutine(ResetWallJump());
            }
            else
            {
                rigidbody.AddForce(Vector2.up * jump_force, ForceMode2D.Impulse);
            }
        }

        animator.SetFloat("jumpPeakVelocity", rigidbody.velocity.y);
    }

    private System.Collections.IEnumerator ResetWallJump()
    {
        yield return new WaitForSeconds(wallJumpLockTime);
        isWallJumping = false;
    }

    // метод рывка
    //private void Charge()
    //{
    //    if (Input.GetKeyDown(KeyCode.F))
    //    {
    //        // положение игрока и мыши
    //        Vector2 start = transform.position;
    //        Vector2 end = MousePosToWorld();

    //        // направление рывка
    //        Vector3 dir = (end - start).normalized;

    //        rigidbody.gravityScale = 0f;
    //        rigidbody.velocity = Vector2.zero;
    //        rigidbody.AddForce(dir * charge_force, ForceMode2D.Impulse);

    //        // тратим счетчик прыжка
    //        jump_counter--;
    //        uncontrol_time = charge_uncontrol_time;
    //    }
    //}

    /* 
     * ЗАМЕНЕНО: Старые методы OnCollisionEnter2D и OnCollisionExit2D закомментированы, 
     * так как при едином Tilemap коллайдере они вызывают баги при переходе со стены на пол. 
     * Вместо них теперь используется OverlapCircle в Update().
     */
    /*
    // обработка нахождения на земле
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((ground_layer.value & (1 << collision.gameObject.layer)) != 0)
        {
            // Проверяем точки контакта: ищем среди них ту, которая смотрит вверх (пол)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) // Нормаль направлена вверх -> это опора (пол)
                {
                    jump_counter = jump_count;
                    animator.SetBool("isGrounded", true);
                    soundManager.PlayLanding();
                    isGrounded = true;
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if ((ground_layer.value & (1 << collision.gameObject.layer)) != 0)
        {
            // Сбрасываем флаг земли при уходе с поверхности
            animator.SetBool("isGrounded", false);
            isGrounded = false;
        }
    }
    */

    // метод отслеживания позиции мыши и перевода ее координат в мировые
    private Vector2 MousePosToWorld()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    // метод рывка
    private void Charge()
    {
        if (Input.GetMouseButtonDown(0) && !hp.is_invulnerable)
        {
            rigidbody.velocity = Vector2.zero;
            rigidbody.gravityScale = 0f;
            charge_counter--;

            animator.SetBool("dashEnd", false);
            animator.SetTrigger("dash");
            inactive.PlayerFirstActivity();

            // Стреляем из шотгана и узнаем, задел ли выстрел врага для расширения рывка
            bool shotgunHitEnemy = shotgun_script.Fire();

            soundManager.PlayFire();

            trail_renderer.emitting = true;

            // старт корутины рывка с передачей результата выстрела
            StartCoroutine(ChargeCoroutine(shotgunHitEnemy));
        }
    }

    private void ChargeCooldown()
    {
        // Если заряды полные, ничего не делаем
        if (charge_counter > 0) return;

        // Уменьшаем таймер на время, прошедшее с прошлого кадра
        chargeCooldownTimer -= Time.deltaTime;

        // Если таймер дошел до нуля или меньше
        if (chargeCooldownTimer <= 0f)
        {
            charge_counter = charge_count;

            // Сбрасываем таймер на исходное время кулдауна
            chargeCooldownTimer = charge_cooldown;

            shotgun_script.Reload();
            soundManager.PlayReload();
        }
    }

    // корутина рывка
    private IEnumerator ChargeCoroutine(bool shotgunHitEnemy)
    {
        Vector2 start = transform.position;
        Vector2 mouse_pos = MousePosToWorld();
        Vector2 dir = (start - mouse_pos).normalized;

        // Если шотган при выстреле задел врага, сразу берем увеличенные значения дальности и времени!
        float currentMaxDistance = shotgunHitEnemy ? enemyHitDistance : charge_distance;
        float currentDuration = shotgunHitEnemy ? enemyHitDuration : charge_duration;

        rigidbody.gravityScale = 0f;
        rigidbody.velocity = Vector2.zero;

        player_incharge = true;

        float t = 0f;
        Vector2 currentPos = start;

        RaycastHit2D hitInfo = default;
        bool hitDetected = false;

        // Коллекция для отслеживания уже задетых врагов в текущем рывке (чтобы наносить урон/триггерить анимацию ровно 1 раз)
        HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();

        while (t < currentDuration)
        {
            float normalized = t / currentDuration;
            float curveValue = charge_curve.Evaluate(normalized);

            float stepDistance = currentMaxDistance * curveValue;
            Vector2 targetPos = start + dir * stepDistance;

            float frameStep = Vector2.Distance(currentPos, targetPos);
            float castDistance = Mathf.Max(frameStep, 0.3f);

            // 1. Проверяем попадание по врагам во время полета (вызывает анимацию/реакцию врага через RegisterPlayerHit)
            RaycastHit2D enemyHit = Physics2D.CircleCast(
                currentPos,
                0.3f,
                dir,
                castDistance,
                enemy_layer
            );

            if (enemyHit.collider != null && enemyHit.collider.gameObject != gameObject)
            {
                EnemyHP enemyHP = enemyHit.collider.GetComponentInParent<EnemyHP>();
                if (enemyHP != null && !hitEnemies.Contains(enemyHit.collider))
                {
                    hitEnemies.Add(enemyHit.collider);
                    enemyHP.RegisterPlayerHit(player_incharge, baseDamage: 0, chargedBonusDamage: 1);
                }
            }

            // 2. Проверяем препятствия и стены (charge_layers)
            RaycastHit2D wallHit = Physics2D.CircleCast(
                currentPos,
                0.25f,
                dir,
                castDistance,
                charge_layers
            );

            if (wallHit.collider != null && wallHit.collider.gameObject != gameObject)
            {
                // Убеждаемся, что нормаль препятствия не является полом под ногами при взлете
                if (Vector2.Dot(dir, wallHit.normal) < -0.2f)
                {
                    hitInfo = wallHit;
                    hitDetected = true;

                    targetPos = wallHit.point - (dir * charge_min_corner);
                    rigidbody.MovePosition(targetPos);
                    break;
                }
            }

            rigidbody.MovePosition(targetPos);
            currentPos = targetPos;

            t += Time.deltaTime;
            yield return null;
        }

        rigidbody.gravityScale = gravity_scale;

        // обработка столкновения со стеной
        if (hitDetected)
        {
            Vector2 reflect = Vector2.Reflect(dir, hitInfo.normal);
            rigidbody.velocity = Vector2.zero;
            rigidbody.AddForce(reflect * jump_force, ForceMode2D.Impulse);
        }
        else
        {
            rigidbody.AddForce(dir * currentMaxDistance, ForceMode2D.Impulse);
        }

        animator.SetBool("dashEnd", true);
        trail_fade.StartFade();

        player_incharge = false;
    }

    // зацепление за стену
    private void SlideOnTheWall(out RaycastHit2D raycast_left, out RaycastHit2D raycast_right)
    {
        // Ищем стены слева и справа по единому слою ground_layer
        raycast_left = Physics2D.Raycast(transform.position, Vector2.left, 0.7f, ground_layer);
        raycast_right = Physics2D.Raycast(transform.position, Vector2.right, 0.7f, ground_layer);

        isWalledLeft = (raycast_left.collider != null);
        isWalledRight = (raycast_right.collider != null);

        bool touchingWall = (isWalledLeft || isWalledRight);

        // ИСПРАВЛЕНИЕ: Добавляем && !isGrounded. 
        // Если игрок на земле, зацепление за стену блокируется принудительно!
        isWalled = touchingWall && !isGrounded;

        isWalledOnce.Value = isWalled;

        if (isWalled)
        {
            sprite_renderer.flipX = isWalledRight;
            jump_counter = jump_count - 1;
        }

        animator.SetBool("isWalled", isWalled);
    }

    // Визуализация точки проверки земли в редакторе Unity (Gizmos)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + groundCheckOffset, groundCheckRadius);
    }
}

public class ObservableBool
{
    private bool _value;

    // Событие, которое срабатывает ТОЛЬКО при переходе из false в true
    public event Action OnChangedToTrue;

    public bool Value
    {
        get => _value;
        set
        {
            // Если старое значение было false, а новое true
            if (!_value && value)
            {
                OnChangedToTrue?.Invoke(); // Вызываем триггер
            }
            _value = value;
        }
    }

    public ObservableBool(bool initialValue = false)
    {
        _value = initialValue;
    }
}

/* TODO:
 * 1) заменить все предустановленные клавиши на настройки Input у Unity
 * 2) вписать сюда анимации игрока и триггеры для их проигрывания
 * 3) вынести теги в настройки, а также другие захардкоженные переменные, связанные с bounce
 */