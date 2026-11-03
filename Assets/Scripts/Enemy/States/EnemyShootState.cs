using UnityEngine;

public class EnemyShootState : IEnemyState
{
    private readonly Enemy enemy;
    private readonly EnemyWeapon weapon;

    public EnemyShootState(Enemy enemy)
    {
        this.enemy = enemy;
        weapon = enemy.weapon;
    }

    public void Enter()
    {
        // Позиционированием и поворотом занимается менеджер.
    }

    public void Update()
    {
        if (!enemy.PlayerDetected)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        EnemyPositionManager manager =
            EnemyPositionManager.Instance;

        if (manager != null &&
            !manager.CanEnterShoot(enemy))
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        float distance = enemy.DistanceToPlayer;

        if (distance <= enemy.RetreatDistance)
        {
            enemy.ChangeState(enemy.RetreatState);
            return;
        }

        if (distance > enemy.AttackDistance)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        if (weapon == null)
            return;

        if (weapon.BurstsCompleted)
        {
            enemy.ChangeState(enemy.RetreatState);
            return;
        }

        if (weapon.CanShoot)
            weapon.Fire();
    }

    public void Exit() { }
}