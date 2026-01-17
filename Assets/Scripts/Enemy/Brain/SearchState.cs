

public class SearchState : IEnemyState
{
    private EnemyBrain brain;
    public SearchState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter() { }
    public void Tick() { }
    public void Exit() { }
}

