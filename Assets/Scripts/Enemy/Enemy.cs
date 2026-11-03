using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static List<Enemy> AllEnemies { get; } = new List<Enemy>();

    [Header("Distances & Detection")]
    [SerializeField] private float aggroDistance = 10f;
    [SerializeField] private float attackDistance = 7f;
    [SerializeField] private float retreatDistance = 4f;

    [Header("Distance Randomization Ranges")]
    [SerializeField] private float attackDistRandomRange = 1.5f;
    [SerializeField] private float retreatDistRandomRange = 1.0f;

    [Header("Layers & Optimization")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionInterval = 0.1f;

    [Header("Scripts")]
    public EnemyMovement movement;
    public EnemyWeapon weapon;

    [Header("Debug")]
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 playerPosition;
    [SerializeField] private float distanceToPlayer;
    [SerializeField] private bool playerDetected;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    private bool isPaused;
    private int activePauseCount;

    private Transform player;
    private EnemyStateMachine stateMachine;

    private EnemyIdleState idleState;
    private EnemyChaseState chaseState;
    private EnemyShootState shootState;
    private EnemyRetreatState retreatState;
    private EnemyReturnState returnState;

    private float actualAttackDistance;
    private float actualRetreatDistance;
    private float detectionTimer;

    private Vector3 lastKnownPlayerPosition;

    public bool IsPaused => isPaused;
    public bool PlayerDetected => playerDetected;

    public Vector3 PlayerPosition => playerPosition;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;

    public float DistanceToPlayer => distanceToPlayer;
    public Vector3 StartPosition => startPosition;

    public float AttackDistance => actualAttackDistance;
    public float RetreatDistance => actualRetreatDistance;

    public IEnemyState CurrentState => stateMachine?.CurrentState;

    public EnemyIdleState IdleState => idleState;
    public EnemyChaseState ChaseState => chaseState;
    public EnemyShootState ShootState => shootState;
    public EnemyRetreatState RetreatState => retreatState;
    public EnemyReturnState ReturnState => returnState;

    private void Awake()
    {
        if (!AllEnemies.Contains(this))
            AllEnemies.Add(this);

        movement = movement != null
            ? movement
            : GetComponent<EnemyMovement>();

        weapon = weapon != null
            ? weapon
            : GetComponentInChildren<EnemyWeapon>();

        startPosition = transform.position;

        float attackOffset = Random.Range(
            -attackDistRandomRange,
            attackDistRandomRange);

        actualAttackDistance = Mathf.Max(
            2f,
            attackDistance + attackOffset);

        float retreatOffset = Random.Range(
            -retreatDistRandomRange,
            retreatDistRandomRange);

        actualRetreatDistance = Mathf.Clamp(
            retreatDistance + retreatOffset,
            1f,
            Mathf.Max(1.1f, actualAttackDistance - 1f));

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        FindPlayer();

        stateMachine = new EnemyStateMachine();

        idleState = new EnemyIdleState(this);
        chaseState = new EnemyChaseState(this);
        shootState = new EnemyShootState(this);
        retreatState = new EnemyRetreatState(this);
        returnState = new EnemyReturnState(this);
    }

    private void OnDestroy()
    {
        AllEnemies.Remove(this);
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        if (isPaused)
            return;

        if (player == null)
            FindPlayer();

        if (player != null)
        {
            playerPosition = player.position;

            distanceToPlayer = Vector2.Distance(
                transform.position,
                playerPosition);
        }
        else
        {
            distanceToPlayer = float.PositiveInfinity;
            playerDetected = false;
        }

        detectionTimer -= Time.deltaTime;

        if (detectionTimer <= 0f)
        {
            detectionTimer = Mathf.Max(0.02f, detectionInterval);
            UpdateLineOfSight();
        }

        if (playerDetected)
            lastKnownPlayerPosition = playerPosition;

        stateMachine?.Update();
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        player = playerObject != null
            ? playerObject.transform
            : null;
    }

    private void UpdateLineOfSight()
    {
        if (player == null)
        {
            playerDetected = false;
            return;
        }

        float maxDetectionDistance = playerDetected
            ? aggroDistance * 1.5f
            : aggroDistance;

        if (distanceToPlayer > maxDetectionDistance)
        {
            playerDetected = false;
            return;
        }

        Vector2 origin = transform.position;
        Vector2 target = player.position;

        Vector2 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            playerDetected = true;
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction / distance,
            distance,
            obstacleLayer);

        playerDetected =
            hit.collider == null ||
            hit.transform == player ||
            hit.transform.IsChildOf(player);
    }

    public void ChangeState(IEnemyState newState)
    {
        stateMachine?.ChangeState(newState);
    }

    public void AnimationSetTrig(
        string triggerName,
        float delayTime,
        System.Action onComplete = null)
    {
        if (animator != null)
            animator.SetTrigger(triggerName);

        if (delayTime <= 0f)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(
            ActionDelayRoutine(delayTime, onComplete));
    }

    private IEnumerator ActionDelayRoutine(
        float delay,
        System.Action onComplete)
    {
        activePauseCount++;
        isPaused = true;

        if (movement != null)
            movement.Stop();

        yield return new WaitForSeconds(delay);

        activePauseCount = Mathf.Max(
            0,
            activePauseCount - 1);

        isPaused = activePauseCount > 0;

        onComplete?.Invoke();
    }

    public void AnimationSetBool(string boolName, bool value)
    {
        if (animator != null &&
            animator.GetBool(boolName) != value)
        {
            animator.SetBool(boolName, value);
        }
    }
}