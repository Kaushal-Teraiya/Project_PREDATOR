

public class SearchState : IEnemyState
{
    private EnemyBrain brain;
    private float searchDuration;
    public bool isSearchComplete { get; private set; }
    public SearchState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter() { }
    public void Tick() { }
    public void OnExit() { }
}

