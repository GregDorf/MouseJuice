using UnityEngine;

public class EnemyReturnState : IEnemyState
{
    private readonly Enemy enemy;

    public EnemyReturnState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        if (enemy.PlayerDetected)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        if (enemy.movement.HasReachedDestination)
        {
            enemy.ChangeState(enemy.IdleState);
        }
    }

    public void Exit() { }
}