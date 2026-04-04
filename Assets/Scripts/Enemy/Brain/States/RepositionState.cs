using System;
using UnityEngine;
using UnityEngine.AI;

public class RepositionState : IEnemyState
{
    private EnemyBrain brain;
    private Vector3 desiredPosition;
    private bool foundValidPosition;
    private Vector3 target;
    private Vector3 offsetDirection;
    private float offsetRadius;
    private bool hasAlmostReached;
    private Vector3 lastTargetPosition;
    private float repositionTimer;

    public RepositionState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        brain.enemyMovement.Animator_SetBool("canExitReposition", false);
        brain.SelectNextBand();
        // if (!BandPositionCoordinator.Instance.TryReserveBand(brain.GetCurrentCombatBand()))
        // {
        //     brain.SwitchState(brain.RepositionState);
        // }
        //        Debug.Log("Reposition ENTER");

        brain.SetIsCommitedToReposition(true);
        SetFoundValidPosition(false);
        //   brain.enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Reposition, 100));
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Reposition);

        var band = brain.GetEffectiveBandRange(brain.CurrentAttackProfile);
        float minRange = band.x;
        float maxRange = band.y;


        float angleInDegrees = brain.ChooseBandSlice();
        float angle = angleInDegrees * Mathf.Deg2Rad;
        float radius = UnityEngine.Random.Range(minRange, maxRange);
        offsetRadius = radius;
        offsetDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        var offset = offsetDirection * offsetRadius;
        target = brain.player.transform.position + offset;

        // float angle = Random.Range(0f, Mathf.PI * 2f);
        // float radius = Random.Range(minRange, maxRange);
        // Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        // var offset = direction * radius;
        // target = brain.player.transform.position + offset;
    }
    public void Tick()
    {
        repositionTimer += Time.deltaTime;
        //brain.enemyMovement.ResetAnimationIntent();
        hasAlmostReached = false;
        brain.enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Reposition, 500));

        var movingCenter = brain.player.transform.position;
        target = movingCenter + offsetDirection * offsetRadius;

        NavMeshHit navHit;

        if (NavMesh.SamplePosition(target, out navHit, brain.DistanceForSampling, NavMesh.AllAreas))
        {
            desiredPosition = navHit.position;
            lastTargetPosition = desiredPosition;
        }

        brain.Debug_RepositionTarget = desiredPosition;
        brain.enemyMovement.MoveTo(desiredPosition);
        brain.enemyMovement.ClearRotationIntent();

        if (brain.HasReachedThePosition(desiredPosition, brain.RepositionArrivalRadius))
        {
            hasAlmostReached = true;
            //brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
            brain.enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, brain.player.transform.position);
            brain.SwitchState(brain.AttackState);
        }

        if (repositionTimer > brain.RepositionTimeout)
        {
            brain.SwitchState(brain.BufferState);
        }

    }
    public void OnExit()
    {
        brain.enemyMovement.Animator_ResetTrigger("EnterReposition");
        brain.enemyMovement.ClearRotationIntent();
        brain.enemyMovement.ResetAnimationIntent();
        brain.enemyMovement.Animator_SetBool("EnterReposition", false);
        brain.enemyMovement.Animator_SetBool("canExitReposition", true);
        repositionTimer = 0f;
        if (hasAlmostReached)
        {
            desiredPosition = lastTargetPosition;
            brain.enemyMovement.MoveTo(desiredPosition);
        }
        //isCommitedToReposition bool is set inside the attackState HandleAttackEnd() function.
    }

    private void SetFoundValidPosition(bool _found)
    {
        foundValidPosition = _found;
    }
}
