using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class EnemyPositionManager : MonoBehaviour
{
    public static EnemyPositionManager Instance { get; private set; }

    [Header("Formation Settings")]
    [SerializeField] private float updateInterval = 0.35f;
    [SerializeField] private float slotSpacing = 0.45f;
    [SerializeField] private float baseCombatDistance = 1.5f;

    [Header("Combat Rotation")]
    [Min(1)]
    [SerializeField] private int activeFightersCount = 3;

    [Header("Retreat Settings")]
    [SerializeField] private float retreatExtraDistance = 0.5f;
    [SerializeField] private float retreatSearchStep = 1f;
    [SerializeField] private int retreatSearchAttempts = 4;

    private float timer;
    private Transform player;

    private readonly List<Enemy> activeEnemies = new();
    private readonly HashSet<Enemy> activeSet = new();

    private readonly List<Enemy> leftGroup = new();
    private readonly List<Enemy> rightGroup = new();

    // Постоянная очередь бойцов.
    // После отхода боец перемещается в конец списка.
    private readonly List<Enemy> combatOrder = new();

    // -1 = слева, 1 = справа.
    private readonly Dictionary<Enemy, int> assignedSides = new();

    private readonly Dictionary<Enemy, float> slotDistances = new();
    private readonly Dictionary<Enemy, Vector3> assignedSlots = new();

    private readonly Dictionary<Enemy, IEnemyState> previousStates = new();
    private readonly HashSet<Enemy> searchCommandsIssued = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindPlayer();
    }

    private void OnValidate()
    {
        activeFightersCount = Mathf.Max(1, activeFightersCount);
        updateInterval = Mathf.Max(0.05f, updateInterval);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        UpdateFormationAssignments();
        ProcessEnemies();

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            ApplyFormationSlots();
        }
    }

    private void FindPlayer()
    {
        GameObject obj =
            GameObject.FindGameObjectWithTag("Player");

        player = obj != null ? obj.transform : null;
    }

    private bool IsCombatState(Enemy enemy)
    {
        IEnemyState state = enemy.CurrentState;

        return state == enemy.ChaseState ||
               state == enemy.ShootState ||
               state == enemy.RetreatState ||
               state == enemy.ReturnState;
    }

    private void UpdateFormationAssignments()
    {
        activeEnemies.Clear();
        activeSet.Clear();

        leftGroup.Clear();
        rightGroup.Clear();

        Vector3 playerPosition = player.position;

        foreach (Enemy enemy in Enemy.AllEnemies)
        {
            if (enemy == null ||
                !enemy.isActiveAndEnabled ||
                enemy.movement == null ||
                !enemy.movement.isActiveAndEnabled)
            {
                continue;
            }

            if (!IsCombatState(enemy))
                continue;

            EnemyHP hp = enemy.GetComponent<EnemyHP>();

            if (hp != null && hp.IsDead)
                continue;

            activeEnemies.Add(enemy);
            activeSet.Add(enemy);

            // Новые враги добавляются в конец очереди.
            if (!combatOrder.Contains(enemy))
                combatOrder.Add(enemy);

            if (!assignedSides.ContainsKey(enemy))
            {
                float difference =
                    enemy.transform.position.x - playerPosition.x;

                int side;

                if (Mathf.Abs(difference) > 0.1f)
                {
                    side = difference < 0f ? -1 : 1;
                }
                else
                {
                    side = enemy.GetInstanceID() % 2 == 0
                        ? -1
                        : 1;
                }

                assignedSides[enemy] = side;
            }

            if (assignedSides[enemy] < 0)
                leftGroup.Add(enemy);
            else
                rightGroup.Add(enemy);
        }

        RemoveInactiveAssignments();

        leftGroup.Sort(CompareEnemiesByOrder);
        rightGroup.Sort(CompareEnemiesByOrder);

        AssignGroupSlots(leftGroup, playerPosition, -1);
        AssignGroupSlots(rightGroup, playerPosition, 1);
    }

    private int CompareEnemiesByOrder(Enemy a, Enemy b)
    {
        return combatOrder.IndexOf(a).CompareTo(
            combatOrder.IndexOf(b));
    }

    private void RemoveInactiveAssignments()
    {
        List<Enemy> staleEnemies = new();

        foreach (Enemy enemy in assignedSides.Keys)
        {
            if (enemy == null || !activeSet.Contains(enemy))
                staleEnemies.Add(enemy);
        }

        foreach (Enemy enemy in staleEnemies)
        {
            assignedSides.Remove(enemy);
            slotDistances.Remove(enemy);
            assignedSlots.Remove(enemy);
            previousStates.Remove(enemy);
            searchCommandsIssued.Remove(enemy);
            combatOrder.Remove(enemy);
        }

        // Дополнительная очистка очереди от уничтоженных объектов.
        combatOrder.RemoveAll(enemy => enemy == null);
    }

    private void AssignGroupSlots(
        List<Enemy> group,
        Vector3 playerPosition,
        int side)
    {
        float nextDistance = baseCombatDistance;
        float previousHalfWidth = 0f;

        for (int i = 0; i < group.Count; i++)
        {
            Enemy enemy = group[i];

            float currentHalfWidth =
                GetHalfWidth(enemy.movement);

            if (i == 0)
            {
                nextDistance += currentHalfWidth;
            }
            else
            {
                nextDistance += previousHalfWidth +
                                currentHalfWidth +
                                slotSpacing;
            }

            Vector3 slotPosition = new Vector3(
                playerPosition.x + side * nextDistance,
                enemy.transform.position.y,
                enemy.transform.position.z);

            slotDistances[enemy] = nextDistance;
            assignedSlots[enemy] = slotPosition;

            previousHalfWidth = currentHalfWidth;
        }
    }

    private void ApplyFormationSlots()
    {
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy == null ||
                enemy.movement == null ||
                !assignedSlots.TryGetValue(
                    enemy,
                    out Vector3 slotPosition))
            {
                continue;
            }

            if (enemy.CurrentState == enemy.ChaseState &&
                enemy.PlayerDetected)
            {
                enemy.movement.SetAssignedSlot(slotPosition);
            }
            else if (enemy.CurrentState == enemy.ReturnState)
            {
                enemy.movement.MoveTo(slotPosition);
            }
        }
    }

    private void ProcessEnemies()
    {
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy == null || enemy.movement == null)
                continue;

            EnemyMovement movement = enemy.movement;
            IEnemyState currentState = enemy.CurrentState;

            if (currentState == null)
                continue;

            if (!previousStates.TryGetValue(
                    enemy,
                    out IEnemyState previousState) ||
                previousState != currentState)
            {
                HandleStateEnter(enemy, currentState);
                previousStates[enemy] = currentState;
            }

            if (currentState == enemy.ChaseState)
            {
                if (enemy.PlayerDetected)
                {
                    if (searchCommandsIssued.Remove(enemy))
                    {
                        movement.Stop();

                        if (assignedSlots.TryGetValue(
                                enemy,
                                out Vector3 slot))
                        {
                            movement.SetAssignedSlot(slot);
                        }
                    }
                }
                else if (enemy.ChaseState.SearchingLastPosition &&
                         searchCommandsIssued.Add(enemy))
                {
                    movement.MoveTo(
                        enemy.LastKnownPlayerPosition);
                }
            }
            else
            {
                searchCommandsIssued.Remove(enemy);
            }

            if (currentState == enemy.ShootState)
            {
                if (enemy.PlayerDetected)
                    movement.FaceTarget(enemy.PlayerPosition);
            }
            else if (Mathf.Abs(movement.HorizontalVelocity) > 0.05f)
            {
                movement.FaceDirection(
                    movement.HorizontalVelocity);
            }
        }
    }

    private void HandleStateEnter(
        Enemy enemy,
        IEnemyState newState)
    {
        EnemyMovement movement = enemy.movement;

        movement.Stop();

        if (newState == enemy.RetreatState)
        {
            // Сначала рассчитываем точку с учётом прежнего слота.
            Vector3 retreatPoint = FindRetreatPoint(enemy);

            // Затем отправляем бойца в конец очереди.
            MoveEnemyToQueueEnd(enemy);

            movement.MoveTo(retreatPoint);
        }
        else if (newState == enemy.ReturnState)
        {
            if (assignedSlots.TryGetValue(
                    enemy,
                    out Vector3 slotPosition))
            {
                movement.MoveTo(slotPosition);
            }
        }
        else if (newState == enemy.ChaseState &&
                 enemy.PlayerDetected)
        {
            if (assignedSlots.TryGetValue(
                    enemy,
                    out Vector3 slotPosition))
            {
                movement.SetAssignedSlot(slotPosition);
            }
        }
    }

    private void MoveEnemyToQueueEnd(Enemy enemy)
    {
        if (enemy == null)
            return;

        combatOrder.Remove(enemy);
        combatOrder.Add(enemy);
    }

    // Проверяет, входит ли враг в число активных бойцов.
    // Считаются только обнаружившие игрока враги в Chase/Shoot.
    public bool CanEnterShoot(Enemy enemy)
    {
        if (enemy == null)
            return false;

        int activeRank = 0;
        int limit = Mathf.Max(1, activeFightersCount);

        foreach (Enemy candidate in combatOrder)
        {
            if (candidate == null || !candidate.PlayerDetected)
                continue;

            IEnemyState state = candidate.CurrentState;

            if (state != candidate.ChaseState &&
                state != candidate.ShootState)
            {
                continue;
            }

            if (candidate == enemy)
                return activeRank < limit;

            activeRank++;
        }

        return false;
    }

    private Vector3 FindRetreatPoint(Enemy enemy)
    {
        Vector3 currentPosition = enemy.transform.position;

        if (AstarPath.active == null)
            return currentPosition;

        int side = assignedSides.TryGetValue(
            enemy,
            out int assignedSide)
                ? assignedSide
                : 1;

        float firstSlotDistance =
            baseCombatDistance + GetHalfWidth(enemy.movement);

        float ownSlotDistance =
            slotDistances.TryGetValue(
                enemy,
                out float distance)
                    ? distance
                    : firstSlotDistance;

        float slotOffset = Mathf.Max(
            0f,
            ownSlotDistance - firstSlotDistance);

        float retreatBaseDistance =
            Mathf.Lerp(
                enemy.RetreatDistance,
                enemy.AttackDistance,
                0.5f) + retreatExtraDistance;

        float desiredDistance =
            retreatBaseDistance + slotOffset;

        float currentDistance = Mathf.Abs(
            currentPosition.x - enemy.PlayerPosition.x);

        Vector3 bestPoint = currentPosition;
        float bestScore = float.PositiveInfinity;

        int attempts = Mathf.Max(1, retreatSearchAttempts);

        for (int i = 0; i < attempts; i++)
        {
            float searchDistance =
                desiredDistance + i * retreatSearchStep;

            Vector3 targetPosition = new Vector3(
                enemy.PlayerPosition.x + side * searchDistance,
                currentPosition.y,
                currentPosition.z);

            NNInfo nodeInfo = AstarPath.active.GetNearest(
                targetPosition,
                NNConstraint.Default);

            if (nodeInfo.node == null ||
                !nodeInfo.node.Walkable)
            {
                continue;
            }

            Vector3 candidate = (Vector3)nodeInfo.position;

            float candidateDistance = Mathf.Abs(
                candidate.x - enemy.PlayerPosition.x);

            if (candidateDistance <= currentDistance + 0.25f)
                continue;

            if (candidateDistance <= enemy.RetreatDistance + 0.5f)
                continue;

            float score = Mathf.Abs(
                candidateDistance - desiredDistance);

            if (score < bestScore)
            {
                bestScore = score;
                bestPoint = candidate;
            }
        }

        return bestPoint;
    }

    private float GetHalfWidth(Enemy enemy)
    {
        return GetHalfWidth(enemy.movement);
    }

    private float GetHalfWidth(EnemyMovement movement)
    {
        Collider2D col =
            movement.GetComponent<Collider2D>();

        if (col == null)
            col = movement.GetComponentInChildren<Collider2D>();

        if (col == null)
            return 0.5f;

        return Mathf.Max(0.25f, col.bounds.extents.x);
    }
}