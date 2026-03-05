using System.Collections.Generic;
using UnityEngine;

public class WanderState : IEnemyState
{
    private EnemyBrain brain;
    private float arrivalRadius = 1.5f;
    private bool isWaiting;
    private float waitDuration;
    private float waitTimer;
    private WayPoint currentTarget;
    private WayPoint lastTarget;
    private WayPointManager wayPointManager;
    //private List<WayPoint> wayPointsList = new List<WayPoint>();

    public WanderState(EnemyBrain brain)
    {
        this.brain = brain;
        wayPointManager = brain._WayPointManager;
    }
    public void OnEnter()
    {
        Debug.Log("[WanderState] Entered Wander State.");
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Wander);

        PickNewTarget();

    }
    public void Tick()
    {

        if (currentTarget == null)
        {
            PickNewTarget();
            return;
        }

        if (brain.Suspicion > 0f)
        {
            brain.enemyMovement.MoveTo(brain.lastConfirmedPosition);
            brain.enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State,brain.lastConfirmedPosition);
            return;
        }

        if (!isWaiting && currentTarget != null && currentTarget.IsClaimedByOther(brain))
        {
            Debug.Log($"{brain.name} abandoned {currentTarget.name}");
            currentTarget = null;
            return;
        }

        if (isWaiting)   //pause for a while then increment index
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitDuration)
            {
                isWaiting = false;
                waitTimer = 0f;

                lastTarget = currentTarget;
                currentTarget.ReleaseWayPoint(brain);
                currentTarget = null;

                PickNewTarget();
            }

            return;
        }

        if (currentTarget != null)
        {
            brain.enemyMovement.MoveTo(currentTarget.transform.position);
        }

        if (HasReachedWaypoint(currentTarget))
        {
            if (currentTarget.TryClaimWayPoint(brain))
            {
                isWaiting = true;
                waitTimer = 0f;
                waitDuration = Random.Range(1f, 2f);
                Debug.Log($"{brain.name} claimed {currentTarget.name}");
            }
            else
            {
                currentTarget = null;
            }
        }

    }
    public void OnExit()
    {
        if (currentTarget != null)
        {
            currentTarget.ReleaseWayPoint(brain);
            currentTarget = null;
        }
    }

    private void PickNewTarget()
    {
        if (wayPointManager == null)
        {
            return;
        }
        var wayPointList = wayPointManager.GetFreeInRadius(brain.transform.position, brain._WayPointCollectionRadius);

        if (lastTarget != null)
        {
            wayPointList.Remove(lastTarget);
        }

        if (wayPointList.Count == 0)
        {
            Debug.Log($"{brain.name} found NO free waypoints.");
            brain.SwitchState(brain.IdleState);
            return;
        }

        var randomIndex = Random.Range(0, wayPointList.Count);
        currentTarget = wayPointList[randomIndex];
        Debug.Log($"{brain.name} picked {currentTarget.name}");
        brain.enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, currentTarget.transform.position);
        lastTarget = null;
    }

    private bool HasReachedWaypoint(WayPoint _wayPoint)
    {
        Vector3 toTarget = _wayPoint.transform.position - brain.transform.position;
        toTarget.y = 0f;
        //  Debug.Log("[WanderState] WayPoint Reached.");
        bool status = toTarget.sqrMagnitude <= arrivalRadius * arrivalRadius;
        //        Debug.Log("[WanderState] Status :" + status);
        return status;
    }

}


