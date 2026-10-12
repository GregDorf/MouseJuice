using UnityEngine;

public class EnemyRetreatState : IEnemyState
{
    private readonly Enemy enemy;
    private Vector3 retreatPoint;
    private float retreatTimeout;

    public EnemyRetreatState(Enemy enemy) => this.enemy = enemy;

    public void Enter()
    {
        retreatPoint = enemy.movement.FindRetreatPoint();
        retreatTimeout = 3f;
        enemy.movement.MoveTo(retreatPoint);
    }

    public void Update()
    {
        retreatTimeout -= Time.deltaTime;

        if (enemy.movement.HasReachedDestination || retreatTimeout <= 0f)
        {
            // Достигли точки отступления — передаем управление в Chase (он сам решит стрелять или преследовать)
            enemy.ChangeState(enemy.ChaseState);
        }
    }

    public void Exit()
    {
        enemy.movement.Stop();
    }
}