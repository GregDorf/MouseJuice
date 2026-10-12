public class EnemyShootState : IEnemyState
{
    private readonly Enemy enemy;
    private readonly EnemyWeapon weapon;

    public EnemyShootState(Enemy enemy)
    {
        this.enemy = enemy;
        this.weapon = enemy.weapon;
    }

    public void Enter()
    {
        enemy.movement.Stop();
        enemy.movement.FaceTarget(enemy.PlayerPosition);

        if (weapon != null)
        {
            weapon.Fire();
        }
    }

    public void Update()
    {
        // Обязательно держим направление лицом к игроку во время стрельбы и кулдауна
        enemy.movement.FaceTarget(enemy.PlayerPosition);

        float distance = enemy.DistanceToPlayer;

        // 1. Если игрок подошел слишком близко — мгновенно уходим в отступление
        if (distance <= enemy.RetreatDistance)
        {
            enemy.ChangeState(enemy.RetreatState);
            return;
        }

        // 2. Если игрок убежал дальше дистанции атаки или потерян — переходим в погоню
        if (distance > enemy.AttackDistance || !enemy.PlayerDetected)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        // 3. Если серия выстрелов завершена и прошел кулдаун — запускаем новую серию
        if (weapon != null && weapon.CanShoot)
        {
            weapon.Fire();
        }
    }

    public void Exit() { }
}