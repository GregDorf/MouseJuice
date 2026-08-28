using DG.Tweening;
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

    [Header("ChargeSettings")]
    //[SerializeField] private float charge_force = 15f;
    //[SerializeField] private float charge_uncontrol_time = 0.2f;
    [SerializeField] private float charge_speed = 5f;
    [SerializeField] private float charge_duration = 0.2f;
    [SerializeField] private float charge_min_corner = 0.5f;
    public int charge_count = 3;
    [SerializeField] private float charge_cooldown = 5f;
    [SerializeField] private AnimationCurve charge_curve;

    [Header("Physics Layers")]
    [SerializeField] private LayerMask charge_layers;
    [SerializeField] private LayerMask ground_layer;
    [SerializeField] private LayerMask wall_layer;
    [SerializeField] private LayerMask enemyLayer;

    // компоненты, необходимые для работы скрипта
    private PlayerInaction inactive;
    private new Rigidbody2D rigidbody;
    private SpriteRenderer sprite_renderer;
    private Animator animator;
    private TrailRenderer trail_renderer;
    private int jump_counter;
    public int charge_counter;
    private float nextChargeTime = 0f;
    private float gravity_scale;
    internal bool isWalled = false;
    internal bool isGrounded = false;
    internal bool isWalledLeft = false;
    internal bool isWalledRight = false;
    internal bool isWallJumping; // Флаг: сейчас совершается прыжок от стены
    private readonly float wallJumpLockTime = 0.5f; // Сколько секунд длится блокировка
    private TrailFade trail_fade;
    internal bool player_incharge;

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

        // задаем изначальное количество прыжков на старте
        jump_counter = 0;
        //uncontrol_time = 0;
        player_incharge = false;

        charge_counter = charge_count - 1;
    }

    // основной цикл программы
    private void Update()
    {
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
        if (isWallJumping) return;

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

    // обработка нахождения на земле
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((ground_layer.value & (1 << collision.gameObject.layer)) != 0)
        {
            jump_counter = jump_count;
            animator.SetBool("isGrounded", true);
            isGrounded = true;
        }
    }
    // обработка нахождения не на земле
    private void OnCollisionExit2D(Collision2D collision)
    {
        if ((ground_layer.value & (1 << collision.gameObject.layer)) != 0)
        {
            animator.SetBool("isGrounded", false);
            isGrounded = false;
        }
    }

    // метод отслеживания позиции мыши и перевода ее координат в мировые
    private Vector2 MousePosToWorld()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    // метод рывка
    private void Charge()
    {
        if (Input.GetMouseButtonDown(0))
        {
            rigidbody.velocity = Vector2.zero;
            rigidbody.gravityScale = 0f;
            charge_counter--;

            animator.SetBool("dashEnd", false);
            animator.SetTrigger("dash");
            inactive.PlayerFirstActivity();

            trail_renderer.emitting = true;

            // старт корутины рывка
            StartCoroutine(ChargeCoroutine());
        }
    }

    private void ChargeCooldown()
    {
        if (charge_counter >= charge_count) return;

        if (Time.time >= nextChargeTime)
        {
            charge_counter = charge_count;
            nextChargeTime = Time.time + charge_cooldown;
        }
    }

    // корутина рывка
    private IEnumerator ChargeCoroutine()
    {
        Vector2 start = transform.position;
        Vector2 mouse_pos = MousePosToWorld();
        Vector2 dir = (mouse_pos - start).normalized;

        float maxDistance = charge_speed;

        rigidbody.gravityScale = 0f;
        rigidbody.velocity = Vector2.zero;

        player_incharge = true;

        float t = 0f;
        Vector2 currentPos = start;

        RaycastHit2D hitInfo = default;
        bool hitDetected = false;

        while (t < charge_duration)
        {
            float normalized = t / charge_duration;
            float curveValue = charge_curve.Evaluate(normalized);

            float stepDistance = maxDistance * curveValue;

            Vector2 targetPos = start + dir * stepDistance;

            // проверка по пути КАЖДЫЙ кадр
            RaycastHit2D hit = Physics2D.CircleCast(
                currentPos,
                0.4f,
                dir,
                Vector2.Distance(currentPos, targetPos),
                charge_layers | enemyLayer
            );

            if (hit.collider != null && hit.distance > 0.01f)
            {
                hitInfo = hit;
                hitDetected = true;

                // ставим точку строго в место столкновения
                targetPos = hit.point - (dir * charge_min_corner);

                rigidbody.MovePosition(targetPos);
                break;
            }

            rigidbody.MovePosition(targetPos);

            currentPos = targetPos;

            t += Time.deltaTime;
            yield return null;
        }

        rigidbody.gravityScale = gravity_scale;

        // обработка столкновения
        if (hitDetected)
        {
            int layer = hitInfo.collider.gameObject.layer;

            bool enemyHit = (enemyLayer.value & (1 << layer)) != 0;

            if (enemyHit)
            {
                rigidbody.velocity = Vector2.zero;
                rigidbody.AddForce(Vector2.up * jump_force, ForceMode2D.Impulse);
            }
            else
            {
                Vector2 reflect = Vector2.Reflect(dir, hitInfo.normal);
                rigidbody.velocity = Vector2.zero;
                rigidbody.AddForce(reflect * jump_force, ForceMode2D.Impulse);
            }
        }
        else
        {
            rigidbody.AddForce(dir * charge_speed, ForceMode2D.Impulse);
        }

        animator.SetBool("dashEnd", true);
        trail_fade.StartFade();

        player_incharge = false;
    }

    // зацепление за стену
    private void SlideOnTheWall(out RaycastHit2D raycast_left, out RaycastHit2D raycast_right)
    {
        raycast_left = Physics2D.Raycast(transform.position, Vector2.left, 0.7f, wall_layer);
        raycast_right = Physics2D.Raycast(transform.position, Vector2.right, 0.7f, wall_layer);

        // Запоминаем конкретные стороны
        isWalledLeft = (raycast_left.collider != null);
        isWalledRight = (raycast_right.collider != null);

        // Общий статус для анимации и прыжка
        bool touchingWall = (isWalledLeft || isWalledRight);
        isWalled = touchingWall && !isGrounded;

        if (isWalled)
        {
            // Поворот спрайта (если правая стена - смотрим вправо и наоборот)
            sprite_renderer.flipX = isWalledRight;
            jump_counter = jump_count - 1;
        }

        animator.SetBool("isWalled", isWalled);
    }
}

/* TODO:
 * 1) заменить все предустановленные клавиши на настройки Input у Unity
 * 2) вписать сюда анимации игрока и триггеры для их проигрывания
 * 3) вынести теги в настройки, а также другие захардкоженные переменные, связанные с bounce
 */