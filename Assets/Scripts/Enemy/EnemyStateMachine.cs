public class EnemyStateMachine
{
    private IEnemyState currentState;

    public void Initialize(IEnemyState startingState)
    {
        currentState = startingState;
        currentState?.Enter();
    }

    public void ChangeState(IEnemyState newState)
    {
        if (newState == null || newState == currentState)
            return;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}