using UnityEngine;
public class AttackRepositionStyle : ICombatStyle
{
    private EnemyBrain brain;

    public AttackRepositionStyle(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        Debug.Log("[AttackReposition Style ENTER]");
    }

    public void ExecuteOneShotDecesion()
    {
        if (NeedToReposition())
        {
            brain.SwitchState(brain.RepositionState);
            return;
        }

        brain.SwitchState(brain.AttackState);
    }

    private bool NeedToReposition()
    {
        var enemy = brain.EnemyMovement;

        if (!enemy.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ground))
        {
            return false;
        }

        float distance = Vector3.Distance(
            brain.transform.position,
            brain.player.transform.position);

        Vector3 normalizedDirection =
            (brain.player.transform.position - brain.transform.position).normalized;

        float maxDistance = distance;
        Vector3 rayOrigin = brain.transform.position + Vector3.up * 1.5f;

        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            normalizedDirection,
            maxDistance,
            brain.ObstacleMaskForReposition,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == brain.transform)
                continue;

            if (hit.collider.transform.root == brain.player.transform)
                break;

            Debug.Log("[AttackRepositionStyle] Obstacle: " + hit.collider.name);
            return true;
        }

        var band = brain.GetEffectiveBandRange(brain.CurrentAttackProfile);
        float tolerance = (band.y - band.x) * 0.25f;

        if ((distance < brain.CurrentAttackProfile.minRange - tolerance ||
             distance > brain.CurrentAttackProfile.maxRange + tolerance) &&
            !brain.IsCommitedToReposition)
        {
            return true;
        }

        return false;
    }

    public void HandleAttackEnd()
    {
        Debug.Log("Attack Reposition Style");
        brain.SetIsCommitedToReposition(false);
        //brain.SelectNextBand();
        float distance = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        var value = Random.value;
        if (value < 0.5f)
        {
            if (distance <= brain.AttackRegisterDistance)
            {
                //StartNewAttack();
                brain.SwitchState(brain.AttackState);
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

    public void Exit()
    {
        Debug.Log("[AttackReposition Style EXIT]");
    }

    public void Tick()
    {
        Debug.Log("[AttackReposition Style TICK]");
    }

}
