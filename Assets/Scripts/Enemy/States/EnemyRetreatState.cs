using UnityEngine;

public class EnemyRetreatState : IEnemyState
{
    private readonly Enemy enemy;

    private float retreatTimeout;

    private const float MaxRetreatDuration = 2f;

    public EnemyRetreatState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        retreatTimeout = MaxRetreatDuration;
    }

    public void Update()
    {
        retreatTimeout -= Time.deltaTime;

        if (enemy.movement.HasReachedDestination ||
            retreatTimeout <= 0f)
        {
            if (enemy.weapon != null)
                enemy.weapon.ResetCombatCycle();

            enemy.ChangeState(enemy.ChaseState);
        }
    }

    public void Exit() { }
}