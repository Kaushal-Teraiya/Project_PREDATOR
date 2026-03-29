using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.AI;

public class RepositionState : IEnemyState
{
    private EnemyBrain brain;
    private Vector3 desiredPosition;
    private bool foundValidPosition;
    private Vector3 target;
    public RepositionState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        SetFoundValidPosition(false);
        brain.enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Reposition, 100));
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Reposition);
        float minRange = brain.CurrentAttackProfile.minRange;
        float maxRange = brain.CurrentAttackProfile.maxRange;

        // float angle = Random.Range(0f, Mathf.PI * 2f);
        // float radius = Random.Range(minRange, maxRange);
        // Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        // var offset = direction * radius;
        // target = brain.player.transform.position + offset;
        for (int i = 0; i < 5; i++)
        {

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minRange, maxRange);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var offset = direction * radius;
            target = brain.player.transform.position + offset;
            NavMeshHit navHit;

            if (NavMesh.SamplePosition(target, out navHit, brain.DistanceForSampling, NavMesh.AllAreas))
            {
                desiredPosition = navHit.position;
                SetFoundValidPosition(true);
                break;
            }
        }

        if (!foundValidPosition)
        {
            brain.SwitchState(brain.ChaseState);
            return;
        }
       // desiredPosition = target;
        brain.Debug_RepositionTarget = desiredPosition;
    }
    public void Tick()
    {
        brain.enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Reposition, 100));
        brain.enemyMovement.MoveTo(desiredPosition);
        brain.enemyMovement.ClearRotationIntent();
        if (brain.HasReachedThePosition(desiredPosition))
        {
            brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
            brain.enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, brain.player.transform.position);
            brain.SwitchState(brain.AttackState);
        }
    }
    public void OnExit()
    {
        brain.enemyMovement.ClearRotationIntent();
        brain.enemyMovement.ResetAnimationIntent();
        brain.enemyMovement.Animator_SetBool("EnterReposition", false);
    }

    private void SetFoundValidPosition(bool _found)
    {
        foundValidPosition = _found;
    }
}
