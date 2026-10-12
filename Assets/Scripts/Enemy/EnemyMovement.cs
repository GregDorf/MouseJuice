using UnityEngine;
using Pathfinding;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waypointDistance = 0.3f;

    [Header("Retreat Settings")]
    [SerializeField] private int retreatCandidates = 16;
    [SerializeField] private float retreatSearchRadius = 2f;

    [Header("Separation Settings")]
    [SerializeField] private float separationRadius = 0.8f; // Радиус, на котором враги начинают отталкиваться
    [SerializeField] private float separationWeight = 1.2f; // Сила отталкивания

    private Seeker seeker;
    private Rigidbody2D rb;
    private Enemy enemy;
    private EnemySound sound;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Path currentPath;
    private int currentWaypoint;
    private bool pathPending;
    private Vector3 lastRequestedDestination;
    private bool hasDestination;

    // Переменные для отслеживания застревания по прогрессу пути (не по физическим толчкам)
    private float lastDistanceToWaypoint = float.MaxValue;
    private float stuckTimer = 0f;
    private const float StuckCheckThreshold = 0.5f; // Время в секундах, если не приближаемся к цели — считаем, что застряли

    public bool HasReachedDestination { get; private set; } = true;

    private void Awake()
    {
        sound = GetComponent<EnemySound>();
        enemy = GetComponent<Enemy>();
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Автоматически отключаем столкновения между объектами слоя "Enemy"
        int enemyLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }

    private void Update()
    {
        if (hasDestination && currentPath != null && !currentPath.error)
        {
            ExecuteMovement();
            sound.PlayWalk();
        }
        else
        {
            stuckTimer = 0f;
            if (enemy != null) enemy.AnimationSetBool("isRun", false);
            sound.StopWalk();
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (seeker == null || AstarPath.active == null) return;

        if (hasDestination && Vector3.SqrMagnitude(lastRequestedDestination - destination) < 0.25f)
        {
            return;
        }

        lastRequestedDestination = destination;
        hasDestination = true;
        HasReachedDestination = false;
        stuckTimer = 0f;
        lastDistanceToWaypoint = float.MaxValue;

        if (pathPending) return;

        pathPending = true;
        seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    private void ExecuteMovement()
    {
        if (currentPath.vectorPath == null || currentWaypoint >= currentPath.vectorPath.Count)
        {
            FinishMovement();
            return;
        }

        Vector3 waypoint = currentPath.vectorPath[currentWaypoint];
        Vector3 direction = waypoint - transform.position;
        direction.z = 0f;

        float distanceToWaypoint = direction.sqrMagnitude;

        if (distanceToWaypoint <= waypointDistance * waypointDistance)
        {
            currentWaypoint++;
            lastDistanceToWaypoint = float.MaxValue;
            stuckTimer = 0f;
            return;
        }

        if (Mathf.Abs(distanceToWaypoint - lastDistanceToWaypoint) < 0.001f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= StuckCheckThreshold)
            {
                Stop();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastDistanceToWaypoint = distanceToWaypoint;
        }

        direction.Normalize();

        // Считаем сепарацию
        Vector3 separation = CalculateSeparation();

        // Итоговый вектор движения (путь + мягкое отталкивание)
        Vector3 finalDirection = (direction + separation * separationWeight);
        finalDirection.z = 0f;
        finalDirection.Normalize();

        if (rb != null)
        {
            rb.velocity = new Vector2(finalDirection.x * moveSpeed, rb.velocity.y);
        }
        else
        {
            transform.position += finalDirection * moveSpeed * Time.deltaTime;
        }

        // ГЛАВНОЕ ИЗМЕНЕНИЕ: flipX ориентируем по основному пути (direction), 
        // а не по итоговому вектору с сепарацией. Это уберет мерцание спрайта!
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }

        if (enemy != null) enemy.AnimationSetBool("isRun", true);
    }

    // Метод для расчета отталкивания от соседних врагов
    private Vector3 CalculateSeparation()
    {
        Vector3 separationDir = Vector3.zero;

        // Ищем всех врагов в радиусе separationRadius (используем коллайдеры)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        int count = 0;

        foreach (var col in colliders)
        {
            // Проверяем, что это другой враг (не мы сами и тот же слой)
            if (col.gameObject != gameObject && col.gameObject.layer == gameObject.layer)
            {
                Vector3 toOther = transform.position - col.transform.position;
                float dist = toOther.magnitude;

                if (dist < separationRadius && dist > 0.0001f)
                {
                    // Чем ближе враг, тем сильнее он отталкивает
                    separationDir += toOther.normalized / dist;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            separationDir /= count;
            separationDir.z = 0f;
        }

        return separationDir;
    }

    public void Stop()
    {
        hasDestination = false;
        pathPending = false;
        currentPath = null;
        currentWaypoint = 0;
        HasReachedDestination = true;
        stuckTimer = 0f;

        if (enemy != null) enemy.AnimationSetBool("isRun", false);

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    private void FinishMovement()
    {
        HasReachedDestination = true;
        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        if (enemy != null) enemy.AnimationSetBool("isRun", false);
    }

    private void OnPathComplete(Path path)
    {
        pathPending = false;
        if (path.error)
        {
            currentPath = null;
            HasReachedDestination = true;
            return;
        }

        currentPath = path;
        currentWaypoint = 0;
        lastDistanceToWaypoint = float.MaxValue;
        stuckTimer = 0f;
    }

    public void FaceTarget(Vector3 targetPos)
    {
        if (spriteRenderer == null) return;
        float directionX = targetPos.x - transform.position.x;
        if (Mathf.Abs(directionX) > 0.05f)
        {
            spriteRenderer.flipX = directionX < 0f;
        }
    }

    public Vector3 FindRetreatPoint()
    {
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent == null || AstarPath.active == null) return transform.position;

        Vector3 playerPosition = enemyComponent.PlayerPosition;
        float desiredDistance = (enemyComponent.AttackDistance + enemyComponent.RetreatDistance) / 2f;
        Vector3 retreatDirection = (transform.position - playerPosition);
        retreatDirection.z = 0f;
        if (retreatDirection.sqrMagnitude < 0.001f) retreatDirection = Vector3.right;
        retreatDirection.Normalize();

        Vector3 desiredPosition = playerPosition + retreatDirection * desiredDistance;
        NNInfo nodeInfo = AstarPath.active.GetNearest(transform.position, NNConstraint.Default);
        if (nodeInfo.node == null) return transform.position;

        uint enemyArea = nodeInfo.node.Area;
        Vector3 bestPoint = transform.position;
        float bestScore = float.MaxValue;

        TryRetreatCandidate(desiredPosition, enemyArea, ref bestPoint, ref bestScore);

        for (int i = 0; i < retreatCandidates; i++)
        {
            float angle = 360f / retreatCandidates * i;
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * retreatDirection;
            TryRetreatCandidate(desiredPosition + dir * retreatSearchRadius, enemyArea, ref bestPoint, ref bestScore);
        }

        return bestPoint;
    }

    private void TryRetreatCandidate(Vector3 candidatePosition, uint enemyArea, ref Vector3 bestPoint, ref float bestScore)
    {
        NNInfo nodeInfo = AstarPath.active.GetNearest(candidatePosition, NNConstraint.Default);
        if (nodeInfo.node == null || !nodeInfo.node.Walkable || nodeInfo.node.Area != enemyArea) return;

        Vector3 point = (Vector3)nodeInfo.position;
        float score = Vector3.SqrMagnitude(point - candidatePosition);
        if (score < bestScore)
        {
            bestScore = score;
            bestPoint = point;
        }
    }
}