using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SearchState : IEnemyState
{
    private EnemyBrain brain;
    private float searchDuration;
    private float scanDuration = 10f;
    private float observeDuration = 3f;
    private float observeStartTime;
    private float scanStartTime;
    private Vector3 LookAt;
    private bool allowMove;
    private bool allowRotate;



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

            if (distance <= currentTarget.arrivalRadius)// if enmies are in the search point radius
            {

                if (currentTarget.TryClaim(brain)) // only the claimer executes animation
                {
                    LogPhase($"CLAIMED {currentTarget.name} → Scanning");
                    brain.enemyMovement.Stop();
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

                    LogPhase($"FAILED claim → Spreading around {currentTarget.name}");
                    float minRadius = currentTarget.searchPointRadius * 0.6f;
                    float maxRadius = currentTarget.searchPointRadius * 0.9f;
                    float minSpacing = 1.2f;
                    int maxAttempts = 6;

                    float baseAngle = GetObserverAngle();
                    Vector3 chosenPos = brain.transform.position;

                    for (int i = 0; i < maxAttempts; i++)
                    {
                        float angleOffset = i * 35f * Mathf.Deg2Rad;
                        float angle = baseAngle + angleOffset;

                        float radius = Random.Range(minRadius, maxRadius);

                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                        Vector3 candidate = currentTarget.transform.position + offset;
                        if (!IsPositionBlocked(candidate, minSpacing))
                        {
                            chosenPos = candidate;
                            break;
                        }
                    }
                    targetPosition = chosenPos;
                    SetLookAt(targetPosition);
                    currentPhase = SearchPhase.Spreading;

                }

            }
        }


        if (currentPhase == SearchPhase.Spreading)
        {
            allowMove = allowRotate = true;
            var direction = targetPosition - brain.transform.position;

            float settleRadius = 0.3f;
            if (direction.sqrMagnitude <= settleRadius * settleRadius)
            {
                LogPhase($"Reached offset → Observing {currentTarget.name}");

                brain.enemyMovement.Stop();
                currentPhase = SearchPhase.Observing;
                observeStartTime = Time.time;
                bool isCurious = Random.value < 1f;
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


            }
        }

        if (currentPhase == SearchPhase.Scanning)
        {
            allowMove = allowRotate = false;

            //scan
            if (Time.time - scanStartTime >= scanDuration)
            {
                LogPhase($"Scan complete → Releasing {currentTarget.name}");
                currentTarget.Release(brain);
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
            {
                return;
            }

            bool observeTimeElapsed = (Time.time - observeStartTime) >= observeDuration;


            if (observeTimeElapsed)
            {
                //Remaining.........
                bool isStillInterested = Random.value < 0.5f; //if its interested and scan is complete and observe time is not elapsed then move towards the same search point else lose interest
                if (!isStillInterested)
                {
                    LogPhase($"Observation complete → Moving to next point");
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
        }

        if (allowMove)
        {
            brain.enemyMovement.MoveTo(targetPosition);
        }
        if (allowRotate)
        {
            brain.enemyMovement.RotateTowards(LookAt - brain.transform.position);
        }


    }
    public void OnExit()
    {
        LogPhase("EXIT SearchState");
    }

    void LogPhase(string message)
    {
        Debug.Log($"[Search][{brain.name}] {message}");
    }

    float GetObserverAngle()
    {
        int hash = Mathf.Abs(brain.GetInstanceID());
        return (hash % 360) * Mathf.Deg2Rad;
    }

    private void SetLookAt(Vector3 _LookAt)
    {
        LookAt = _LookAt;
    }

    bool IsPositionBlocked(Vector3 position, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                return true;
            }
        }
        return false;
    }




}

