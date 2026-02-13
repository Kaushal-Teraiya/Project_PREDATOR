using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private float InvestigateAreaRadius;
    [SerializeField] private float proximityRadius;
    [SerializeField] private float maxRadiusForHearing = 8f;
    [SerializeField] private LayerMask searchPointLayer;
    [SerializeField] private float SearchPointFactorPercent = 0.5f;
    [SerializeField] private float arrivalRadius = 0.5f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float visionGraceDuration = 0.2f;

    private SoundSensor soundSensor;
    private VisionSensor visionSensor;
    private EnemyMovement Movement_Enemy;
    public EnemyMovement enemyMovement => Movement_Enemy;
    private IEnemyState currentState;
    private IdleState idleState;
    private InvestigateState investigateState;
    private SearchState searchState;
    private ChaseState chaseState;
    private Vector3 currentInvestigationCenter;
    private float currentInvestigationRadius;
    public GameObject player { get; private set; }
    private List<SearchPoint> availableSearchPoints = new List<SearchPoint>();
    private List<SearchPoint> selectedSearchPoints = new List<SearchPoint>();
    public int currentSearchIndex { get; private set; }
    private SearchPoint lastReleasedPoint;
    private float lastReleaseTime;
    public bool IsSearchComplete
    {
        get
        {
            return currentSearchIndex >= selectedSearchPoints.Count;
        }
    }
    public Vector3 lastConfirmedPosition { get; private set; }
    public float lastConfirmedSeenTime { get; private set; }
    private bool currentlyChasing;
    public float chaseTimeLimit { get; private set; } = 2f;
    public Vector3 chaseTargetPosition { get; private set; }
    private float lastChaseTime;
    private Vector3 investigationForward;
    private bool postChase;

    void Awake()
    {
        soundSensor = GetComponent<SoundSensor>();
        visionSensor = GetComponentInChildren<VisionSensor>();
        Movement_Enemy = GetComponent<EnemyMovement>();
    }
    void Start()
    {
        idleState = new IdleState(this);
        searchState = new SearchState(this);
        investigateState = new InvestigateState(this);
        chaseState = new ChaseState(this);
        SwitchState(idleState);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        CheckPerception();
        CheckStateChange();
        currentState?.Tick();
    }

    private void SwitchState(IEnemyState newState)
    {
        enemyMovement.SetRotationPermission(true);
        enemyMovement.ClearRotationIntent();
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();

    }

    private void InitializeInvestigateState(Vector3 lastKnownPosition, float radius)
    {
        currentInvestigationCenter = lastKnownPosition;
        currentInvestigationRadius = radius;
        // currentInvestigationCenter = soundSensor.LastHeardPosition;
        // currentInvestigationRadius = InvestigateAreaRadius;
        investigateState.SetAreaCenter_AreaRadius(currentInvestigationCenter, currentInvestigationRadius);
    }

    public void InitializeSearch()
    {
        CollectNearbySearchPoints();
        ShuffleSearchPoints();

        var pickN = availableSearchPoints.Count * SearchPointFactorPercent;
        int roundUp = (int)Mathf.Ceil((float)pickN);
        int finalN = Mathf.Clamp(roundUp, 2, 8);
        finalN = (int)MathF.Min(finalN, availableSearchPoints.Count);

        for (int i = 0; i < finalN; i++)
        {
            selectedSearchPoints.Add(availableSearchPoints[i]);
        }

        foreach (var searchPoint in selectedSearchPoints)
        {
            searchPoint.GenerateSlots();
        }

        if (postChase)
        {
            for (int i = selectedSearchPoints.Count - 1; i >= 0; i--)
            {
                Vector3 toPoint = selectedSearchPoints[i].transform.position - currentInvestigationCenter;
                toPoint.y = 0f;
                if (Vector3.Dot(investigationForward.normalized, toPoint.normalized) <= 0f)
                {
                    selectedSearchPoints.RemoveAt(i);
                }

            }
        }
        currentSearchIndex = 0;
        Debug.Log($"Selected Search Points Count: {selectedSearchPoints.Count}");
    }

    private void CollectNearbySearchPoints()
    {
        selectedSearchPoints.Clear();
        availableSearchPoints.Clear();
        Collider[] points = Physics.OverlapSphere(currentInvestigationCenter, currentInvestigationRadius, searchPointLayer);
        foreach (var point in points)
        {
            if (point != null)
            {
                var searchPoint = point.gameObject.GetComponent<SearchPoint>();
                if (searchPoint == null)
                {
                    Debug.Log("null search point");
                    continue;

                }
                availableSearchPoints.Add(searchPoint);
            }
        }

    }

    public SearchPoint GetCurrentSearchPoint()
    {
        if (currentSearchIndex >= 0 && currentSearchIndex < selectedSearchPoints.Count)
        {
            return selectedSearchPoints[currentSearchIndex];
        }
        else
        {
            return null;
        }
    }

    public void IncrementSearchIndex()
    {
        currentSearchIndex++;
    }

    private void ShuffleSearchPoints()
    {
        for (int currentIndex = availableSearchPoints.Count - 1; currentIndex > 0; currentIndex--)
        {
            int randomIndex = UnityEngine.Random.Range(0, currentIndex + 1);
            var temp = availableSearchPoints[currentIndex];
            availableSearchPoints[currentIndex] = availableSearchPoints[randomIndex];
            availableSearchPoints[randomIndex] = temp;
        }
    }

    private void CheckStateChange()
    {
        if (currentState != chaseState && visionSensor.HasLineOfSight)
        {
            SwitchState(chaseState);
            return;
        }

        if (currentState == idleState && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= maxRadiusForHearing)
        {
            InitializeInvestigateState(soundSensor.LastHeardPosition, InvestigateAreaRadius);
            SwitchState(investigateState);
        }

        if ((currentState == investigateState || currentState == searchState) && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= maxRadiusForHearing)
        {
            float distance = Vector3.Distance(soundSensor.LastHeardPosition, currentInvestigationCenter);
            if (distance > currentInvestigationRadius)
            {
                InitializeInvestigateState(soundSensor.LastHeardPosition, InvestigateAreaRadius);
                SwitchState(investigateState);
            }
        }

        if (currentState == investigateState && investigateState.hasReachedDestination)
        {
            SwitchState(searchState);
        }

        if (currentState == searchState && IsSearchComplete)
        {
            SwitchState(idleState);
        }

    }

    private void CheckPerception()
    {
        if (visionSensor != null && HasVision())
        {
            lastConfirmedPosition = visionSensor.LastSeenPosition;
            lastConfirmedSeenTime = visionSensor.LastSeenTime;
            if (currentlyChasing)
            {
                chaseTargetPosition = lastConfirmedPosition;
                lastChaseTime = Time.time;
            }

        }

        Vector3 origin = visionSensor.transform.position;
        Vector3 direction = player.transform.position - visionSensor.transform.position;
        float distance = direction.magnitude;
        bool recentlyChasing = currentlyChasing || Time.time - lastChaseTime <= visionGraceDuration;
        RaycastHit hit;

        if (distance < proximityRadius && !HasVision()) //this means even if we are not in enemy's vision it can still sense us if we are near them
        {
            if (!Physics.Raycast(origin, direction.normalized, out hit, distance, obstacleMask))
            {
                enemyMovement.RotationIntent(EnemyMovement.RotationPriority.Proximity, origin + direction.normalized);
            }
            //later we can add closest player for multiplayer here
            // Debug.Log(hit.collider.gameObject.layer);
        }
    }

    public void NotifySearchPointReleased(SearchPoint point)
    {
        lastReleasedPoint = point;
        lastReleaseTime = Time.time;
    }


    public bool CanClaim(SearchPoint point)
    {
        if (point == lastReleasedPoint && Time.time - lastReleaseTime < 0.3f)
        {
            return false;
        }

        return true;
    }


    void OnDrawGizmos()
    {
        if (currentInvestigationRadius <= 0f)
            return;

        Gizmos.color = UnityEngine.Color.orange;


        DrawCircle(
            currentInvestigationCenter,
            currentInvestigationRadius,
            40
        );

        if (currentInvestigationRadius > 0f)
        {
            Gizmos.color = UnityEngine.Color.orange;
            DrawCircle(currentInvestigationCenter, currentInvestigationRadius, 40);
        }

        //to visualize last chase target position 
        if (chaseTargetPosition != Vector3.zero)
        {
            Gizmos.color = UnityEngine.Color.red;

            // Sphere at last chase position
            Gizmos.DrawSphere(chaseTargetPosition, 0.25f);

            // Line from zombie to chase target
            Gizmos.DrawLine(transform.position, chaseTargetPosition);
        }

        Gizmos.color = new UnityEngine.Color(0f, 1f, 1f, 0.8f); // bright cyan

        DrawCircle(transform.position, proximityRadius, 40);

        // Optional vertical line for clarity
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.up * 2f
        );
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    public bool HasReachedThePosition(Vector3 lastConfirmedPosition)
    {
        Vector3 toTarget = lastConfirmedPosition - transform.position;
        toTarget.y = 0f;

        return toTarget.sqrMagnitude <= arrivalRadius * arrivalRadius;
    }

    public void SetCurrentlyChasing(bool _isChasing)
    {
        currentlyChasing = _isChasing;

        if (_isChasing)
        {
            lastChaseTime = Time.time;
        }
    }

    public void EndChase(Vector3 lastChasePosition)
    {
        SetCurrentlyChasing(false);
        SetInvestigationForward(enemyMovement.transform.forward);
        InitializeInvestigateState(lastChasePosition, InvestigateAreaRadius);
        postChase = true;
        SwitchState(investigateState);
    }


    public bool HasVision()
    {
        return visionSensor.HasLineOfSight;
    }

    public void InitializeChase()
    {
        chaseTargetPosition = lastConfirmedPosition;
    }

    private void SetInvestigationForward(Vector3 forward)
    {
        investigationForward = forward;
    }

    public void SetPostChase(bool _postChase)
    {
        postChase = _postChase;
    }


}