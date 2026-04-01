using UnityEngine;
using UnityEngine.AI;

public class ChaseState : IEnemyState
{
    private EnemyBrain brain;
    private float chasePauseTime = 2f;
    private Vector3 lockedChasePoint;
    public ChaseState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    // private float chaseTimer;


    public void OnEnter()
    {
        brain.enemyMovement.SetPlayerAvoidance(false);
        //        Debug.Log("Entered Chase");
        brain.SetCurrentlyChasing(true);
        //  lockedChasePoint = brain.chaseTargetPosition;
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Chase);
        if (brain.shouldPauseOnChase)
        {
            chasePauseTime = 2f;
            brain.SetChasePause(false);
        }
        else
        {
            chasePauseTime = 0f;
        }
        brain.InitializeChase();
        //enemy speed change intent will be set here and movement mode emum will set the speeds inside the enemyMovement 
    }
    public void Tick()
    {
        var Enemy = brain.enemyMovement;
        chasePauseTime -= Time.deltaTime;

        if (chasePauseTime > 0)
        {
            Enemy.RotationIntent(EnemyMovement.RotationPriority.State, brain.player.transform.position);
            return;
        }
        var dir = brain.transform.position - brain.player.transform.position;
        dir.Normalize();
        var offset = brain.AttackDistance - brain.AttackOffset;
        var desiredPoint = brain.player.transform.position + dir * offset;

        var distanceToPlayer = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        var directionToPlayer = brain.player.transform.position - brain.transform.position;
        var dot = Vector3.Dot(brain.transform.forward.normalized, directionToPlayer.normalized);

        if (brain.CheckVisibilityResult(VisionSensor.visibilityResult.Chase))
        {
            Enemy.MoveTo(desiredPoint);
            Enemy.ClearRotationIntent();
        }
        else if (brain.WasRecentlyChasing() && !brain.CheckVisibilityResult(VisionSensor.visibilityResult.None))//visibility is not none => was the second condition
        {
            Enemy.MoveTo(brain.chaseTargetPosition);
            Enemy.ClearRotationIntent();
        }
        else if (brain.isEndingChase)
        {
            Enemy.MoveTo(brain.chaseTargetPosition);
            Enemy.ClearRotationIntent();
            if (brain.HasReachedThePosition(brain.chaseTargetPosition, brain.ChaseTargetArrivalRadius))
            {
                brain.EndChase(brain.chaseTargetPosition);
            }
        }
     
        Enemy.RequestAnimation(new AnimationIntent(AnimationType.Run, 40));
    }

    public void OnExit()
    {
        brain.SetCurrentlyChasing(false);
        brain.enemyMovement.SetPlayerAvoidance(true);
    }
}
