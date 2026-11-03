public class EnemyIdleState : IEnemyState
{
    private readonly Enemy enemy;

    public EnemyIdleState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.movement?.Stop();
    }

    public void Update()
    {
        if (!enemy.PlayerDetected)
            return;

        enemy.AnimationSetTrig("Shock", 0.7f, () =>
        {
            enemy.ChangeState(enemy.ChaseState);
        });
    }

    public void Exit() { }
}
