using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyBrain brain;
    public ChaseState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    private float chaseTimer;


    public void OnEnter()
    {
        Debug.Log("Entered Chase");
        brain.SetCurrentlyChasing(true);
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Chase);
        chaseTimer = 0f;
        brain.InitializeChase();
        //enemy speed change intent will be set here and movement mode emum will set the speeds inside the enemyMovement 
    }
    public void Tick()
    {
        var Enemy = brain.enemyMovement;
        if (brain.HasVision())
        {
            if (chaseTimer > 0f)
            {
                Debug.Log($"[CHASE] Vision reacquired. Timer reset from {chaseTimer:F2}");
            }
            chaseTimer = 0f;
            Enemy.MoveTo(brain.chaseTargetPosition);
            Enemy.RotationIntent(EnemyMovement.RotationPriority.Vision, brain.chaseTargetPosition);
        }
        else
        {
            Enemy.MoveTo(brain.chaseTargetPosition);
            // if (chaseTimer > brain.chaseTimeLimit && !brain.HasVision())
            // {
            //     Debug.Log("[CHASE] Timer exceeded. Ending chase.");
            //     brain.EndChase(brain.chaseTargetPosition);
            // }

            if (brain.HasReachedThePosition(brain.chaseTargetPosition))
            {
                brain.EndChase(brain.chaseTargetPosition);
            }
        }
    }

    public void OnExit()
    {
        brain.SetCurrentlyChasing(false);
    }
}
