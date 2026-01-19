using UnityEngine;
public class IdleState : IEnemyState
{
    private EnemyBrain brain;
    public IdleState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        Debug.Log("idle starts");
    }
    public void Tick() { }
    public void OnExit()
    {
        Debug.Log("idle exits");
    }
}

