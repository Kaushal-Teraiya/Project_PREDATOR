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
        var dir = brain.transform.position - brain.player.transform.position;
        dir.Normalize();
        var offset = brain.attackDistance - 0.5f;
        var desiredPoint = brain.player.transform.position + dir * offset;

        if (brain.HasVision())
        {
            if (chaseTimer > 0f)
            {
                Debug.Log($"[CHASE] Vision reacquired. Timer reset from {chaseTimer:F2}");
            }
            chaseTimer = 0f;
            Enemy.MoveTo(desiredPoint);
            Enemy.RotationIntent(EnemyMovement.RotationPriority.Vision, brain.chaseTargetPosition);
        }
        else
        {
            Enemy.MoveTo(brain.chaseTargetPosition);

            if (brain.HasReachedThePosition(brain.chaseTargetPosition))
            {
                brain.EndChase(brain.chaseTargetPosition);
            }
        }

        var distanceToPlayer = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        var directionToPlayer = brain.player.transform.position - brain.transform.position;
        var dot = Vector3.Dot(brain.transform.forward.normalized, directionToPlayer.normalized);

        if (!brain.IsInState(brain.AttackState) && brain.HasVision() && distanceToPlayer <= brain.attackDistance && !brain.IsInCooldown() && dot > 0.5f)
        {
            Debug.Log("[Chase] Entering Attack.");
            brain.SwitchState(brain.AttackState);
        }
    }

    public void OnExit()
    {
        brain.SetCurrentlyChasing(false);
    }
}
