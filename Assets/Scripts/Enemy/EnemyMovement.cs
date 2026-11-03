using UnityEngine;
using Pathfinding;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private float speedRandomizationRange = 1f;
    [SerializeField] private float waypointReachDistance = 0.3f;

    [Header("Obstacle & Wall Check")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float wallCheckDistance = 0.6f;

    [Header("Animation Settings")]
    [SerializeField] private float runAnimationStartSpeed = 0.15f;
    [SerializeField] private float runAnimationStopSpeed = 0.07f;

    [Header("Path Settings")]
    [SerializeField] private float destinationUpdateThreshold = 0.5f;
    [SerializeField] private float stuckTimeout = 0.7f;

    private Seeker seeker;
    private Rigidbody2D rb;
    private Enemy enemy;
    private EnemySound sound;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private Path currentPath;
    private int currentWaypoint;

    private bool pathPending;
    private bool hasDestination;

    private Vector3 currentDestination;
    private Vector3 requestedDestination;

    private int pathRequestVersion;

    private float actualSpeed;
    private float stuckTimer;

    private Vector3 lastPosition;

    private bool isWalkingSoundPlaying;
    private bool isRunAnimationPlaying;

    public bool HasReachedDestination { get; private set; } = true;

    public float HorizontalVelocity =>
        rb != null ? rb.velocity.x : 0f;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();

        sound = GetComponentInParent<EnemySound>();

        if (sound == null)
            sound = GetComponent<EnemySound>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        int enemyLayer = gameObject.layer;

        Physics2D.IgnoreLayerCollision(
            enemyLayer,
            enemyLayer,
            true);

        float randomOffset = Random.Range(
            -speedRandomizationRange,
            speedRandomizationRange);

        actualSpeed = Mathf.Max(
            1f,
            baseMoveSpeed + randomOffset);

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!hasDestination ||
            HasReachedDestination ||
            currentPath == null ||
            currentPath.error)
        {
            StopMovementSound();
            return;
        }

        ExecuteMovement();
        UpdateRunAnimation();

        if (!HasReachedDestination &&
            !isWalkingSoundPlaying &&
            sound != null &&
            IsMovingHorizontally())
        {
            isWalkingSoundPlaying = true;
            sound.PlayWalk();
        }
    }

    private void FixedUpdate()
    {
        UpdateRunAnimation();
    }

    public void MoveTo(Vector3 destination)
    {
        if (seeker == null || AstarPath.active == null)
            return;

        float thresholdSqr =
            destinationUpdateThreshold *
            destinationUpdateThreshold;

        if (hasDestination &&
            (currentDestination - destination).sqrMagnitude <
            thresholdSqr)
        {
            return;
        }

        currentDestination = destination;
        requestedDestination = destination;

        hasDestination = true;
        HasReachedDestination = false;

        stuckTimer = 0f;

        if (!pathPending)
            RequestPath();
    }

    private void RequestPath()
    {
        if (seeker == null ||
            AstarPath.active == null ||
            !hasDestination)
        {
            return;
        }

        pathPending = true;

        int requestVersion = pathRequestVersion;
        Vector3 destinationSnapshot = requestedDestination;

        seeker.StartPath(
            transform.position,
            destinationSnapshot,
            path => OnPathComplete(
                path,
                destinationSnapshot,
                requestVersion));
    }

    private void OnPathComplete(
        Path path,
        Vector3 destinationSnapshot,
        int requestVersion)
    {
        if (requestVersion != pathRequestVersion)
            return;

        pathPending = false;

        if (!hasDestination)
            return;

        float thresholdSqr =
            destinationUpdateThreshold *
            destinationUpdateThreshold;

        if ((requestedDestination - destinationSnapshot).sqrMagnitude >=
            thresholdSqr)
        {
            RequestPath();
            return;
        }

        if (path == null || path.error)
        {
            currentPath = null;
            hasDestination = false;
            HasReachedDestination = true;

            StopMovementSound();
            return;
        }

        currentPath = path;
        currentWaypoint = 0;

        stuckTimer = 0f;
        lastPosition = transform.position;
    }

    // Вызывается только EnemyPositionManager.
    public void SetAssignedSlot(Vector3 slotPosition)
    {
        MoveTo(slotPosition);
    }

    private void ExecuteMovement()
    {
        if (currentPath == null ||
            currentPath.vectorPath == null ||
            currentWaypoint >= currentPath.vectorPath.Count)
        {
            FinishMovement();
            return;
        }

        float distanceToDestination = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(currentDestination.x, transform.position.y));

        if (distanceToDestination <= waypointReachDistance)
        {
            FinishMovement();
            return;
        }

        Vector3 waypoint =
            currentPath.vectorPath[currentWaypoint];

        waypoint.y = transform.position.y;
        waypoint.z = transform.position.z;

        Vector3 direction = waypoint - transform.position;
        direction.z = 0f;

        if (direction.sqrMagnitude <=
            waypointReachDistance * waypointReachDistance)
        {
            currentWaypoint++;
            stuckTimer = 0f;
            return;
        }

        direction.Normalize();

        float moveDirectionX = direction.x;

        if (Mathf.Abs(moveDirectionX) > 0.001f)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                new Vector2(Mathf.Sign(moveDirectionX), 0f),
                wallCheckDistance,
                obstacleLayer);

            if (hit.collider != null)
                moveDirectionX = 0f;
        }

        if ((transform.position - lastPosition).sqrMagnitude < 0.0001f)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTimeout)
            {
                currentPath = null;
                hasDestination = false;
                HasReachedDestination = true;

                StopMovementSound();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        float targetVelocityX = moveDirectionX * actualSpeed;

        if (rb != null)
        {
            float smoothedX = Mathf.MoveTowards(
                rb.velocity.x,
                targetVelocityX,
                actualSpeed * 15f * Time.deltaTime);

            rb.velocity = new Vector2(
                smoothedX,
                rb.velocity.y);
        }
        else
        {
            transform.position +=
                new Vector3(moveDirectionX, 0f, 0f) *
                actualSpeed *
                Time.deltaTime;
        }
    }

    private void UpdateRunAnimation()
    {
        if (enemy == null)
            return;

        float horizontalSpeed =
            rb != null ? Mathf.Abs(rb.velocity.x) : 0f;

        if (!isRunAnimationPlaying &&
            horizontalSpeed >= runAnimationStartSpeed)
        {
            isRunAnimationPlaying = true;
            enemy.AnimationSetBool("isRun", true);
        }
        else if (isRunAnimationPlaying &&
                 horizontalSpeed <= runAnimationStopSpeed)
        {
            isRunAnimationPlaying = false;
            enemy.AnimationSetBool("isRun", false);
        }
    }

    private bool IsMovingHorizontally()
    {
        return rb != null &&
               Mathf.Abs(rb.velocity.x) >= runAnimationStartSpeed;
    }

    public void Stop()
    {
        pathRequestVersion++;

        hasDestination = false;
        pathPending = false;

        currentPath = null;
        currentWaypoint = 0;

        HasReachedDestination = true;
        stuckTimer = 0f;

        StopMovementSound();

        if (rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    private void FinishMovement()
    {
        hasDestination = false;
        currentPath = null;
        currentWaypoint = 0;

        HasReachedDestination = true;

        StopMovementSound();

        if (rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    private void StopMovementSound()
    {
        stuckTimer = 0f;

        if (isRunAnimationPlaying)
        {
            isRunAnimationPlaying = false;

            if (enemy != null)
                enemy.AnimationSetBool("isRun", false);
        }

        if (isWalkingSoundPlaying)
        {
            isWalkingSoundPlaying = false;

            if (sound != null)
                sound.StopWalk();
        }
    }

    // Управление поворотом теперь принадлежит менеджеру.
    public void FaceTarget(Vector3 targetPosition)
    {
        if (spriteRenderer == null)
            return;

        float directionX =
            targetPosition.x - transform.position.x;

        FaceDirection(directionX);
    }

    public void FaceDirection(float directionX)
    {
        if (spriteRenderer == null ||
            Mathf.Abs(directionX) <= 0.05f)
        {
            return;
        }

        spriteRenderer.flipX = directionX < 0f;
    }
}