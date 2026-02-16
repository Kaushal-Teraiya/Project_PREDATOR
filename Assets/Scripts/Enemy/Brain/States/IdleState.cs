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
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
        Debug.Log("[idleState] Idle State started.");
    }
    public void Tick() {}
    public void OnExit()
    {
        
    }
}

