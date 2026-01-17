
public class InvestigateState : IEnemyState
{
    private EnemyBrain brain;
    public bool hasReachedDestination { get; private set; }
    public InvestigateState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void OnEnter() { }
    public void Tick() { }
    public void OnExit() { }
}

