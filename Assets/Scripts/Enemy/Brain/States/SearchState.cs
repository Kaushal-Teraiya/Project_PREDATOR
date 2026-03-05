using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SearchState : IEnemyState
{
    private EnemyBrain brain;
    private float searchDuration;
    private float scanDuration = 3f;
    private float observeDuration = 5f;
    private float observeStartTime;
    private float scanStartTime;
    private Vector3 LookAt;
    private bool allowMove;
    private bool allowRotate;
    private bool willRetryIfFreed;
    private int observerSlotIndex = -1;



    //private Vector3 personalOffset;
    private enum SearchPhase
    {
        Moving,
        Scanning,
        Spreading,
        Observing
    }

    private SearchPhase currentPhase;
    private SearchPoint currentTarget;
    private Vector3 targetPosition;
    public SearchState(EnemyBrain brain)
    {
        this.brain = brain;

    }
    public void OnEnter()
    {
        brain.enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Search);
        currentTarget = null;
        targetPosition = Vector3.positiveInfinity;
        brain.InitializeSearch();
        if (currentTarget == null)
        {
            currentTarget = brain.GetCurrentSearchPoint();
            if (currentTarget == null)
            {
                return;
            }
            LogPhase($"Moving to SearchPoint: {currentTarget.name}");
            targetPosition = currentTarget.transform.position;
        }
        SetLookAt(targetPosition);
        currentPhase = SearchPhase.Moving;
    }
    public void Tick()
    {

        var Enemy = brain.enemyMovement;
        allowMove = allowRotate = false;

        if (currentPhase == SearchPhase.Moving)
        {
            allowMove = allowRotate = true;

            if (currentTarget == null)
            {
                currentTarget = brain.GetCurrentSearchPoint();
                if (currentTarget == null)
                {
                    return;
                }
            }

            float distance = Vector3.Distance(brain.transform.position, targetPosition);
            // float threshold = 0.2f;

            if (distance <= currentTarget.arrivalRadius || distance<= 1.5f)// if enmies are in the search point radius
            {
                if (currentTarget.TryClaimSearchPoint(brain)) // only the claimer executes animation
                {
                    LogPhase($"CLAIMED {currentTarget.name} → Scanning");
                    Enemy.Stop();
                    //snap to achor play animation and other stuff
                    currentPhase = SearchPhase.Scanning;
                    scanStartTime = Time.time;
                    targetPosition = currentTarget.transform.position;
                    SetLookAt(targetPosition);
                    return;
                }
                else
                {
                    if (!currentTarget.IsOccupied && currentTarget.IsCenterBlockedFor(brain))
                    {
                        // Do nothing this frame, retry claim next Tick
                        return;
                    }

                    int slotIndex = currentTarget.TryClaimObserverSlot();
                    if (slotIndex == -1)
                    {
                        currentTarget = null;
                        brain.IncrementSearchIndex();
                        currentPhase = SearchPhase.Moving;
                        return;
                    }

                    targetPosition = currentTarget.GetObserverPosition(slotIndex);
                    observerSlotIndex = slotIndex;
                    SetLookAt(targetPosition);
                    currentPhase = SearchPhase.Spreading;
                }

            }
            else
            {
                Debug.Log("[SearchState] MNot able to claim because  of arrival radius issue !!!");
            }
        }


        if (currentPhase == SearchPhase.Spreading)
        {
            allowMove = allowRotate = true;

            var direction = targetPosition - brain.transform.position;

            float settleRadius = 0.8f;
            if (direction.sqrMagnitude <= settleRadius * settleRadius)
            {
                LogPhase($"Reached offset → Observing {currentTarget.name}");

                Enemy.Stop();
                currentPhase = SearchPhase.Observing;
                observeStartTime = Time.time;
                bool isCurious = Random.value < 0.5f;
                if (isCurious)
                {
                    SetLookAt(currentTarget.transform.position);
                }
                else
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    var randomDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float lookDistance = 3f;
                    SetLookAt(brain.transform.position + randomDir * lookDistance);

                }

                willRetryIfFreed = Random.value < 0.1f;


            }
        }

        if (currentPhase == SearchPhase.Scanning)
        {
            allowMove = allowRotate = false;

            //scan
            if (Time.time - scanStartTime >= scanDuration)
            {
                LogPhase($"Scan complete → Releasing {currentTarget.name}");
                currentTarget.ReleaseSearchPoint(brain);
                brain.NotifySearchPointReleased(currentTarget);
                currentTarget = null;
                brain.IncrementSearchIndex();
                currentPhase = SearchPhase.Moving;

                currentTarget = brain.GetCurrentSearchPoint();
                if (currentTarget == null)
                {
                    return;
                }
                LogPhase($"Moving to SearchPoint: {currentTarget.name}");
                targetPosition = currentTarget.transform.position;
                SetLookAt(targetPosition);
                return;
            }
        }

        if (currentPhase == SearchPhase.Observing)
        {
            allowMove = false;
            allowRotate = true;

            if (currentTarget == null)
                return;

            bool observeTimeElapsed = (Time.time - observeStartTime) >= observeDuration;
            bool pointIsFree = !currentTarget.IsOccupied;

            //Early freed point
            if (pointIsFree && !observeTimeElapsed && willRetryIfFreed)
            {

                // immediately try to reclaim
                targetPosition = currentTarget.transform.position;
                SetLookAt(targetPosition);
                currentTarget.ReleaseObserverSlot(observerSlotIndex);
                observerSlotIndex = -1;
                currentPhase = SearchPhase.Moving;
                return;

            }

            //normal lose interest
            if (observeTimeElapsed)
            {
                LogPhase($"Observation complete → Moving to next point");
                currentTarget.ReleaseObserverSlot(observerSlotIndex);
                observerSlotIndex = -1;
                currentTarget = null;
                brain.IncrementSearchIndex();
                currentTarget = brain.GetCurrentSearchPoint();
                if (currentTarget == null)
                    return;

                targetPosition = currentTarget.transform.position;
                SetLookAt(targetPosition);
                currentPhase = SearchPhase.Moving;
                return;
            }
        }


        if (allowMove)
        {
            Enemy.MoveTo(targetPosition);
        }

        if (allowRotate)
        {
            Enemy.RotationIntent(EnemyMovement.RotationPriority.State, LookAt);
        }

        Enemy.SetRotationPermission(allowRotate);

        // if (allowRotate)
        // {
        //     if (brain.enemyMovement.IsAvoiding())
        //     {
        //         //do nothin..
        //     }
        //     else
        //     {
        //        // brain.enemyMovement.RotateTowards(LookAt - brain.transform.position);
        //     }
        // }
    }
    public void OnExit()
    {
        LogPhase("EXIT SearchState");
        if (currentTarget != null && observerSlotIndex != -1)
        {
            currentTarget.ReleaseObserverSlot(observerSlotIndex);
            observerSlotIndex = -1;
        }
        brain.SetPostChase(false);
        
    }

    void LogPhase(string message)
    {
//        Debug.Log($"[Search][{brain.name}] {message}");
    }

    private void SetLookAt(Vector3 _LookAt)
    {
        LookAt = _LookAt;
    }

    public string GetPhase()
    {
        return currentPhase.ToString();
    }

}
