using UnityEngine;
using UnityEngine.AI;

public class AttackState : IEnemyState
{
    private EnemyBrain brain;
    private AnimationClip currentAnimationClip;
    private double tolerance;
    public AttackState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        var Enemy = brain.EnemyMovement;
        brain.SetAttackRegisterDistance(brain.CurrentAttackProfile);
        Enemy.Stop();
        Enemy.RotationIntent(EnemyMovement.RotationPriority.State, brain.player.transform.position);
        //Enemy.SetMovementMode(EnemyMovement.MovementMode.Idle);
        Enemy.DisableProximity();
        float distance = Vector3.Distance(brain.player.transform.position, brain.transform.position);

        if (brain.CurrentAttackProfile == null)
        {
            Debug.Log("[AttackState] CURRENT ATTACK PROFILE IS NULL");
        }

        var normalizedDirection = (brain.player.transform.position - brain.transform.position).normalized;
        var maxDistance = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        var rayOrigin = brain.transform.position + Vector3.up * 1.5f;

        if (Enemy.IsCurrentMovememntSurface(EnemyMovement.MovementSurface.Ground))
        {
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, normalizedDirection, maxDistance, brain.ObstacleMaskForReposition);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.root == brain.transform)
                {
                    continue;
                }

                if (hit.collider.transform.root == brain.player.transform)
                {
                    break;
                }

                Debug.Log("[AttackState] Hit Collider Name on attack start Raycast: " + hit.collider.name);
                brain.SwitchState(brain.RepositionState);
                return;
            }

            var band = brain.GetEffectiveBandRange(brain.CurrentAttackProfile);
            tolerance = (float)(band.x - band.y) * 0.25;

            if ((distance < brain.CurrentAttackProfile.minRange - tolerance || distance > brain.CurrentAttackProfile.maxRange + tolerance) && !brain.IsCommitedToReposition)
            {
                brain.SwitchState(brain.RepositionState);
                return;
            }
        }
        SetupAnimation();
        // Surface attack
        if (!Enemy.IsCurrentMovememntSurface(EnemyMovement.MovementSurface.Ground))
        {
            SurfaceLeapAttack();
        }
        else if (brain.CurrentAttackProfile.usesLeap)
        {
            GroundLeapAttack();
        }
        // brain.ApplyRootMotion(true);
        // Enemy.SyncHipsTracker();
        Enemy.Animator_SetBool("canExitAttack", false);
        Enemy.ResetAnimationIntent();

        Enemy.RequestAnimation(new AnimationIntent(AnimationType.Attack, 100));
        //Debug.Log("[Attack] In Attack..");
    }
    public void Tick()
    {
        var Enemy = brain.EnemyMovement;
        Enemy.RequestAnimation(new AnimationIntent(AnimationType.Attack, 100));

    }
    public void OnExit()
    {
        brain.EnemyMovement.SetRotationPermission(true);
        //brain.ApplyRootMotion(false);
        brain.EnemyMovement.EnableProximity();
        brain.EnemyMovement.Animator_ResetTrigger("Attack");
        brain.EnemyMovement.Animator_SetBool("canExitAttack", true);
    }

    public void HandleAttackHit()
    {
        if (TargetInRange(brain.player))
        {
            IDamageable damagable = brain.player.GetComponent<IDamageable>();
            if (damagable != null)
            {
                HitResult hitResult = new HitResult
                {
                    hitObject = brain.player,
                    hitDirection = brain.transform.forward,
                    hitForce = 0f,
                    hitNormal = Vector3.zero,
                    success = true

                };
                damagable.TakeDamage(hitResult, brain.AttackDamage);
            }
            //            Debug.Log("[Attack] Damage Applied.");
        }
    }

    public void HandleAttackEnd()
    {
        brain.NotifyAttackEnded();
        brain.SetIsCommitedToReposition(false);
        // BandPositionCoordinator.Instance.ReleaseBand(brain.GetCurrentCombatBand());
        // brain.SwitchState(brain.BufferState);
        float distance = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        var value = Random.value;
        if (value < 0.5f)
        {
            if (distance <= brain.AttackRegisterDistance)
            {
                StartNewAttack();
            }
            else
            {
                //MAKE SURE TO CHECK VISIBILITY RESULT BEFORE TRANSITIONING TO ANY STATE BECAUSE WE HAVE TO TAKE DARKNESS IN ACCOUNT AS WELL.
                brain.SwitchState(brain.ChaseState);
            }
        }
        else
        {
            if (distance > brain.FarAttackAsset.maxRange * 1.5f)
            {
                //MAKE SURE TO CHECK VISIBILITY RESULT BEFORE TRANSITIONING TO ANY STATE BECAUSE WE HAVE TO TAKE DARKNESS IN ACCOUNT AS WELL.
                brain.SwitchState(brain.ChaseState);
            }
            else
            {
                brain.SwitchState(brain.RepositionState);
            }

        }

    }

    private void StartNewAttack()
    {
        brain.EnemyMovement.Animator_ResetTrigger("Attack");
        SetupAnimation();
        if (brain.GetCurrentCombatBand() == EnemyBrain.CombatBand.Far)
        {
            brain.SwitchState(brain.RepositionState);
        }
        brain.EnemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Attack, 30));
    }

    private void SetupAnimation()
    {
        // if (!brain.EnemyMovement.IsCurrentMovememntSurface(EnemyMovement.MovementSurface.Ground))
        // {
        //     var leapClip = brain.FarAttackAsset.attackAnimations;
        //     if (leapClip.Count == 0)
        //     {
        //         Debug.Log("[AttackState]NO CLIPS FOUND!");
        //     }
        //     currentAnimationClip = leapClip[Random.Range(0, leapClip.Count)];
        //     brain.OverrideAttackAnimation(currentAnimationClip);
        //     return;
        // }
        var clips = brain.CurrentAttackProfile.attackAnimations;
        if (clips.Count == 0)
        {
            Debug.Log("[AttackState]NO CLIPS FOUND!");
        }
        currentAnimationClip = clips[Random.Range(0, clips.Count)];
        brain.OverrideAttackAnimation(currentAnimationClip);


        //  Debug.Log("current Animation clip name :" + currentAnimationClip.name);
        // Debug.Log("current attack profile :" + brain.CurrentAttackProfile.name);
    }

    private bool TargetInRange(GameObject target)
    {
        var distance = Vector3.Distance(brain.transform.position, target.transform.position);
        return distance <= brain.AttackRegisterDistance;
    }

    private void GroundLeapAttack()
    {
        var predictionTime = brain.CurrentAttackProfile.leapDelay + brain.CurrentAttackProfile.leapDuration;
        var predictionDistance = brain.GetPredictionDistance();
        var predictedPosition = brain.PredictPlayerPosition(predictionTime, predictionDistance);
        var direction = (predictedPosition - brain.transform.position).normalized;
        //Later use 50% chances of acting dumb by changing predicted position to simple player.transform.position
        var targetPosition = predictedPosition - direction * (brain.CloseAttackAsset.minRange - 1.2f);
        NavMeshHit navHit;

        if (NavMesh.SamplePosition(targetPosition, out navHit, 2f, NavMesh.AllAreas))
        {
            targetPosition = navHit.position;
        }
        brain.EnemyMovement.StartLeap(targetPosition, brain.CurrentAttackProfile.leapDelay, brain.CurrentAttackProfile.leapDuration, brain.CurrentAttackProfile.leapArcHeight);
    }

    private void SurfaceLeapAttack()
    {
        Vector3 direction = (brain.player.transform.position - brain.transform.position).normalized;
        direction.y = 0;
        Vector3 targetPosition = brain.player.transform.position - direction * 3f;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(targetPosition, out navHit, 2f, NavMesh.AllAreas))
        {
            targetPosition = navHit.position;
        }

        Quaternion targetRotation = Quaternion.LookRotation(brain.transform.forward, Vector3.up);

        brain.EnemyMovement.StartLeap(
            targetPosition,
            0f, // or brain.CurrentAttackProfile.leapDelay
            1f, // or brain.CurrentAttackProfile.leapDuration
            3f, targetRotation);

        //brain.EnemyMovement.SetCurrentMovementSurface(EnemyMovement.MovementSurface.Ground);
    }
}
