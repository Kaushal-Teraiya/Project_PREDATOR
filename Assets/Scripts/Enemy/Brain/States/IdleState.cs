using UnityEngine;
public class IdleState : IEnemyState
{
    private EnemyBrain brain;
    private float idleTimer;
    private float idleDuration;
    public IdleState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        idleTimer = 0f;
        idleDuration = Random.Range(1.5f, 3.5f);
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
        Debug.Log("[idleState] Idle State started.");
    }
    public void Tick()
    {
        var Enemy = brain.enemyMovement;
        Enemy.RequestAnimation(new AnimationIntent(AnimationType.Idle , 10));
        idleTimer += Time.deltaTime;
        // if (idleTimer >= idleDuration)
        // {
        //     Debug.Log("[IdleState] Entering WanderState");
        //     brain.SwitchState(brain.WanderState);
        // }
    }
    public void OnExit()
    {

    }
}

