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
    [Header("Tunable Parameters")]
    [SerializeField] private float InvestigateAreaRadius;
    [SerializeField] private float proximityRadius;
    [SerializeField] private float maxRadiusForHearing = 8f;
    [SerializeField] private LayerMask searchPointLayer;
    [SerializeField] private float SearchPointFactorPercent = 0.5f;
    [SerializeField] private float arrivalRadius = 0.5f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float visionGraceDuration = 0.2f;
    [SerializeField] private float attackCooldown = 1.5f;


    [Header("Sensors")]
    private SoundSensor soundSensor;
    private VisionSensor visionSensor;

    [Header("States")]
    private IEnemyState currentState;
    private IdleState idleState;
    private InvestigateState investigateState;
    private SearchState searchState;
    private ChaseState chaseState;
    private AttackState attackState;
    private DeadState deadState;
    private WanderState wanderState;
    public IEnemyState WanderState => wanderState;
    public IEnemyState AttackState => attackState;
    public IEnemyState ChaseState => chaseState;
    public IEnemyState IdleState => idleState;

    [Header("Object & Script References")]
    private EnemyMovement Movement_Enemy;
    public EnemyMovement enemyMovement => Movement_Enemy;
    private Vector3 currentInvestigationCenter;
    private float currentInvestigationRadius;
    public GameObject player { get; private set; }
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    public EnemyHealth _enemyHealth => enemyHealth;
    [SerializeField] private WayPointManager wayPointManager;
    public WayPointManager _WayPointManager => wayPointManager;
    private Animator animator;


    [Header("SearchPoint Data")]
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

    [Header("ChaseState Data")]
    public Vector3 lastConfirmedPosition { get; private set; }
    public float lastConfirmedSeenTime { get; private set; }
    private bool currentlyChasing;
    public Vector3 chaseTargetPosition { get; private set; }
    private float lastChaseTime;
    private Vector3 investigationForward;
    private bool postChase;

    [Header("AttackState Data")]
    public float attackDistance { get; private set; } = 4f;
    private float lastAttackEndTime;
    public int attackDamage { get; private set; } = 50;

    [Header("WanderState Data")]
    [SerializeField] private float wayPointCollectionRadius = 10f;
    public float _WayPointCollectionRadius => wayPointCollectionRadius;

    [Header("Animation Data")]
    [SerializeField] private List<AnimationClip> attackVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> idleVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> walkVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> runVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> deathVariants = new List<AnimationClip>();
    private AnimatorOverrideController animatorOverrideController;
    private RuntimeAnimatorController baseController;

    [Header("Suspicion Data")]
    [SerializeField] private float suspicion;
    [SerializeField] private float suspicionIncreaseRate = 40f;
    [SerializeField] private float suspicionDecayRate = 20f;
    [SerializeField] private float chaseThreshold = 100f;
    public float Suspicion => suspicion;

    void Awake()
    {
        soundSensor = GetComponent<SoundSensor>();
        visionSensor = GetComponentInChildren<VisionSensor>();
        Movement_Enemy = GetComponent<EnemyMovement>();
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        baseController = animator.runtimeAnimatorController;

        animatorOverrideController = new AnimatorOverrideController
        {
            runtimeAnimatorController = baseController
        };

        AnimationClip walkVariant = walkVariants[UnityEngine.Random.Range(0, walkVariants.Count)];
        AnimationClip idleVariant = idleVariants[UnityEngine.Random.Range(0, idleVariants.Count)];
        AnimationClip runVariant = runVariants[UnityEngine.Random.Range(0, runVariants.Count)];
        AnimationClip attackVariant = attackVariants[UnityEngine.Random.Range(0, attackVariants.Count)];
        AnimationClip deathVariant = deathVariants[UnityEngine.Random.Range(0, deathVariants.Count)];

        animatorOverrideController["Walk_"] = walkVariant;
        animatorOverrideController["Idle_"] = idleVariant;
        animatorOverrideController["Run_"] = runVariant;
        animatorOverrideController["Attack_"] = attackVariant;
        animatorOverrideController["Death_"] = deathVariant;

        animator.runtimeAnimatorController = animatorOverrideController;

    }
    void Start()
    {
        idleState = new IdleState(this);
        searchState = new SearchState(this);
        investigateState = new InvestigateState(this);
        chaseState = new ChaseState(this);
        attackState = new AttackState(this);
        deadState = new DeadState(this);
        wanderState = new WanderState(this);
        SwitchState(idleState);
        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // Animator_SetFloat("Speed", enemyMovement.MovementSpeed);
        UpdateSuspicion();
        CheckPerception();
        CheckStateChange();
        currentState?.Tick();
        enemyMovement.ApplyAnimationIntent();
    }

    public void SwitchState(IEnemyState newState)
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

    public void InitializeChase()
    {
        chaseTargetPosition = lastConfirmedPosition;
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
        if (IsInState(deadState))
        {
            return;
        }

        if (playerHealth.playerisDead && IsInState(attackState))
        {
            enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
            enemyMovement.Stop();
            enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Idle, 70)); //temporary idle , later we want multiple different behviour of zombies on player death
            return;

        }

        // if (IsInState(wanderState) && HasVision())
        // {
        //     SwitchState(chaseState);
        // }

        // if (!IsInState(chaseState) && !IsInState(attackState) && HasVision())
        // {
        //     SwitchState(chaseState);
        //     return;
        // }



        if (!IsInState(chaseState) && !IsInState(attackState))
        {
            if (suspicion >= chaseThreshold)
            {
                lastConfirmedPosition = player.transform.position;
                SwitchState(chaseState);
                return;
            }
        }

        if ((IsInState(idleState) || IsInState(wanderState)) && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= maxRadiusForHearing)
        {
            InitializeInvestigateState(soundSensor.LastHeardPosition, InvestigateAreaRadius);
            SwitchState(investigateState);
        }

        if ((IsInState(investigateState) || IsInState(searchState)) && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= maxRadiusForHearing)
        {
            float distance = Vector3.Distance(soundSensor.LastHeardPosition, currentInvestigationCenter);
            if (distance > currentInvestigationRadius)
            {
                InitializeInvestigateState(soundSensor.LastHeardPosition, InvestigateAreaRadius);
                SwitchState(investigateState);
            }
        }

        if (IsInState(investigateState) && investigateState.hasReachedDestination)
        {
            SwitchState(searchState);
        }

        if (IsInState(searchState) && IsSearchComplete)
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

    private void UpdateSuspicion()
    {
        if (IsInState(deadState) || IsInState(chaseState) || IsInState(attackState))
        {
            return;
        }

        if (HasVision())
        {
            suspicion += suspicionIncreaseRate * Time.deltaTime;
        }
        else
        {
            suspicion -= suspicionDecayRate * Time.deltaTime;
        }

        suspicion = Mathf.Clamp(suspicion, 0, chaseThreshold);
    }

    public bool HasVision()
    {
        return visionSensor.HasLineOfSight;
    }

    private void SetInvestigationForward(Vector3 forward)
    {
        investigationForward = forward;
    }

    public void SetPostChase(bool _postChase)
    {
        postChase = _postChase;
    }

    public bool IsInCooldown()
    {
        return Time.time - lastAttackEndTime < attackCooldown;
    }

    public bool IsInState(IEnemyState state)
    {
        return currentState == state;
    }

    public void NotifyAttackEnded()
    {
        lastAttackEndTime = Time.time;
    }

    public void HandleDeath()
    {
        SwitchState(deadState);
    }

    public void DisablePerception()
    {
        visionSensor.enabled = false;
        soundSensor.enabled = false;
    }

    public string GetCurrentState()
    {
        return currentState.GetType().Name;
    }



    public bool IsDead()
    {
        return enemyHealth.EnemyisDead;
    }

    public void HandleAttackHit()
    {
        if (IsInState(attackState))
        {
            attackState.HandleAttackHit();
        }
    }

    public void HandleAttackEnd()
    {
        if (IsInState(attackState))
        {
            attackState.HandleAttackEnd();
        }
    }

    public string GetSearchPhase()
    {
        if (IsInState(searchState))
        {
            return searchState.GetPhase();
        }

        return " ";
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



}