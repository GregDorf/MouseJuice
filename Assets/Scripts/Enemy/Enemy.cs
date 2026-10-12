using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Distances & Detection")]
    [SerializeField] private float aggroDistance = 10f;
    [SerializeField] private float attackDistance = 7f;
    [SerializeField] private float retreatDistance = 4f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField, Tooltip("Интервал проверки видимости через стены для оптимизации FPS")]
    private float detectionInterval = 0.1f;

    [Header("Scripts")]
    public EnemyMovement movement;
    public EnemyWeapon weapon;

    [Header("Debug")]
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 playerPosition;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Action Pause")]
    private bool isPaused = false; // Флаг паузы (враг замер во время анимации)
    public bool IsPaused => isPaused;

    private Transform player;
    private EnemyStateMachine stateMachine;

    private EnemyIdleState idleState;
    private EnemyChaseState chaseState;
    private EnemyShootState shootState;
    private EnemyRetreatState retreatState;
    private EnemyReturnState returnState;

    [SerializeField] private bool playerDetected = false;
    [SerializeField] private float distanceToPlayer;
    private float detectionTimer;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        weapon = GetComponentInChildren<EnemyWeapon>();
        startPosition = transform.position;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError($"{name}: Player with tag 'Player' was not found.");
        }

        stateMachine = new EnemyStateMachine();

        idleState = new EnemyIdleState(this);
        chaseState = new EnemyChaseState(this);
        shootState = new EnemyShootState(this);
        retreatState = new EnemyRetreatState(this);
        returnState = new EnemyReturnState(this);
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        // Если враг находится на паузе после анимации, пропускаем всю логику движения и состояний
        if (isPaused) return;

        if (player != null)
        {
            // Позиция и дистанция обновляются каждый кадр для максимальной отзывчивости движения
            playerPosition = player.position;
            distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
        }

        // Оптимизация: тяжелый Raycast (проверка преград) выполняется по таймеру
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionInterval;
            UpdateLineOfSight();
        }

        stateMachine.Update();
    }

    private void UpdateLineOfSight()
    {
        if (player == null)
        {
            playerDetected = false;
            return;
        }

        float currentMaxDetection = playerDetected ? aggroDistance * 1.5f : aggroDistance;

        if (distanceToPlayer > currentMaxDetection)
        {
            playerDetected = false;
            return;
        }

        Vector2 directionToPlayer = (Vector2)(playerPosition - transform.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, obstacleLayer);

        playerDetected = (hit.collider == null);
    }

    public void ChangeState(IEnemyState newState)
    {
        stateMachine.ChangeState(newState);
    }

    // Метод для триггер-анимаций с паузой и колбэком по завершении
    public void AnimationSetTrig(string triggerName, float delayTime, System.Action onComplete = null)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
        StartCoroutine(ActionDelayRoutine(delayTime, onComplete));
    }

    // Корутина для создания паузы
    private System.Collections.IEnumerator ActionDelayRoutine(float delay, System.Action onComplete)
    {
        isPaused = true;

        if (movement != null) movement.Stop();

        yield return new WaitForSeconds(delay);

        isPaused = false;

        // Вызываем действие (например, смену состояния) после окончания паузы
        onComplete?.Invoke();
    }

    // Простой метод для постоянных состояний (бег, ходьба) без корутин и пауз
    public void AnimationSetBool(string boolName, bool value)
    {
        if (animator != null && animator.GetBool(boolName) != value)
        {
            animator.SetBool(boolName, value);
        }
    }

    // Геттеры
    public Vector3 StartPosition => startPosition;
    public float AttackDistance => attackDistance;
    public float RetreatDistance => retreatDistance;
    public EnemyIdleState IdleState => idleState;
    public EnemyChaseState ChaseState => chaseState;
    public EnemyShootState ShootState => shootState;
    public EnemyRetreatState RetreatState => retreatState;
    public EnemyReturnState ReturnState => returnState;

    public bool PlayerDetected => playerDetected;
    public Vector3 PlayerPosition => playerPosition;
    public float DistanceToPlayer => distanceToPlayer;
}