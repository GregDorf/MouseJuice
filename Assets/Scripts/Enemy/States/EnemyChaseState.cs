using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly Enemy enemy;

    public bool SearchingLastPosition { get; private set; }

    public EnemyChaseState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        SearchingLastPosition = false;
    }

    public void Update()
    {
        if (enemy.PlayerDetected)
        {
            SearchingLastPosition = false;

            float distance = enemy.DistanceToPlayer;

            if (distance <= enemy.RetreatDistance)
            {
                enemy.ChangeState(enemy.RetreatState);
                return;
            }

            if (distance <= enemy.AttackDistance)
            {
                EnemyPositionManager manager =
                    EnemyPositionManager.Instance;

                if (manager == null ||
                    manager.CanEnterShoot(enemy))
                {
                    enemy.ChangeState(enemy.ShootState);
                }

                return;
            }

            return;
        }

        SearchingLastPosition = true;

        if (enemy.movement.HasReachedDestination)
            enemy.ChangeState(enemy.ReturnState);
    }

    public void Exit() { }
}