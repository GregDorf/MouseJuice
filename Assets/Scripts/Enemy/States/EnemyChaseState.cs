using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly Enemy enemy;
    private Vector3 lastTargetPosition;
    private bool isSearchingLastPosition = false;
    private const float RebuildPathThreshold = 0.5f; // Уменьшено для более точного преследования

    public EnemyChaseState(Enemy enemy) => this.enemy = enemy;

    public void Enter()
    {
        isSearchingLastPosition = false;
        if (enemy.PlayerDetected)
        {
            lastTargetPosition = enemy.PlayerPosition;
            enemy.movement.MoveTo(lastTargetPosition);
        }
    }

    public void Update()
    {
        if (enemy.PlayerDetected)
        {
            isSearchingLastPosition = false;
            float distance = enemy.DistanceToPlayer;

            // Строгая проверка дистанций для мгновенного переключения состояний
            if (distance <= enemy.RetreatDistance)
            {
                enemy.ChangeState(enemy.RetreatState);
                return;
            }
            else if (distance <= enemy.AttackDistance)
            {
                enemy.ChangeState(enemy.ShootState);
                return;
            }

            // Движение к игроку
            bool hasReached = enemy.movement.HasReachedDestination;
            bool playerMoved = Vector3.SqrMagnitude(enemy.PlayerPosition - lastTargetPosition) > RebuildPathThreshold * RebuildPathThreshold;

            if (hasReached || playerMoved)
            {
                lastTargetPosition = enemy.PlayerPosition;
                enemy.movement.MoveTo(lastTargetPosition);
            }
        }
        else
        {
            if (!isSearchingLastPosition)
            {
                isSearchingLastPosition = true;
                lastTargetPosition = enemy.PlayerPosition;
                enemy.movement.MoveTo(lastTargetPosition);
            }

            if (enemy.movement.HasReachedDestination)
            {
                enemy.ChangeState(enemy.ReturnState);
            }
        }
    }

    public void Exit() { }
}