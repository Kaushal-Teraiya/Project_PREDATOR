public class IdleState : IEnemyState
{
    private EnemyBrain brain;
    public IdleState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter() { }
    public void Tick() { }
    public void OnExit() { }
}

