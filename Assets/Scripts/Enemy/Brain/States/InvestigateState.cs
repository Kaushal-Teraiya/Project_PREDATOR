
public class InvestigateState : IEnemyState
{
    private EnemyBrain brain;
    public InvestigateState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter() { }
    public void Tick() { }
    public void OnExit() { }
}

