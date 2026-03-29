using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class EnemyBrain : MonoBehaviour
{
    [Header("General Parameters")]
    [SerializeField] private float arrivalRadius;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float visionGraceDuration = 3f;

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
    private BufferState bufferState;
    private RepositionState repositionState;
    public IEnemyState RepositionState => repositionState;
    public IEnemyState WanderState => wanderState;
    public IEnemyState AttackState => attackState;
    public IEnemyState ChaseState => chaseState;
    public IEnemyState IdleState => idleState;
    public IEnemyState BufferState => bufferState;

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
    [SerializeField] private LayerMask searchPointLayer;
    [SerializeField] private float SearchPointFactorPercent = 0.5f;

    [Header("InvestigateState Data")]
    [SerializeField] private float InvestigateAreaRadius;
    public Vector3 lastStimulusPosition { get; private set; }
    [SerializeField] private float stimulusMemoryDuration = 5f;
    private float lastStimulusTime;

    [Header("ChaseState Data")]
    public Vector3 lastConfirmedPosition { get; private set; }
    public float lastConfirmedSeenTime { get; private set; }
    private bool currentlyChasing;
    public Vector3 chaseTargetPosition { get; private set; }

    public bool isEndingChase { get; private set; }
    private float lastChaseTime;
    private Vector3 investigationForward;
    private bool postChase;
    public bool shouldPauseOnChase { get; private set; }
    public bool hasSeenFirstTime = true;

    [Header("AttackState Data")]
    [SerializeField] private float attackDistance = 4f;
    [SerializeField] private float attackRegisterDistance;
    public float AttackRegisterDistance => attackRegisterDistance;
    public float AttackDistance => attackDistance;
    [SerializeField] private float attackOffset;
    public float AttackOffset => attackOffset;
    private float lastAttackEndTime;
    [SerializeField] private int attackDamage = 20;
    public int AttackDamage => attackDamage;
    [SerializeField] private float attackDotThreshold = 0.5f;
    public float AttackDotThreshold => attackDotThreshold;
    [SerializeField] private AttackTypes closeAttacksAsset;
    [SerializeField] private AttackTypes midAttacksAsset;
    [SerializeField] private AttackTypes farAttacksAsset;
    private AttackTypes currentAttackProfile;
    public AttackTypes CurrentAttackProfile => currentAttackProfile;
    public AttackTypes CloseAttackAsset => closeAttacksAsset;
    public AttackTypes FarAttackAsset => farAttacksAsset;
    public enum CombatBand
    {
        Far,
        Mid,
        Close
    }
    [SerializeField] private CombatBand currentCombatBand;

    [SerializeField] private float farBandAttackDistance;
    [SerializeField] private float midBandAttackDistance;
    [SerializeField] private float closeBandAttackDistance;
    [SerializeField] private float bandDistanceTolerance = 0.5f;

    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Navmesh Data")]
    [SerializeField] private float distanceForSampling = 5f;
    public float DistanceForSampling => distanceForSampling;

    [Header("WanderState Data")]
    [SerializeField] private float wayPointCollectionRadius = 10f;
    public float _WayPointCollectionRadius => wayPointCollectionRadius;

    [Header("Animation Data")]
    //[SerializeField] private List<AnimationClip> attackVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> idleVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> walkVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> runVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> deathVariants = new List<AnimationClip>();
    public AnimatorOverrideController animatorOverrideController;
    private RuntimeAnimatorController baseController;

    // [Header("Suspicion Data")]
    // private float suspicion;
    // [SerializeField] private float suspicionDecayRate = 20f;
    // [SerializeField] private float chaseThreshold = 100f;
    // [SerializeField] private float investigationThreshold = 30f;
    // public float Suspicion => suspicion;

    [Header("Proximity Data")]
    [SerializeField] private float proximityRadius = 10f;

    [Header("Vision")]
    public VisionSensor.visibilityResult previousResult { get; private set; }
    public VisionSensor.visibilityResult currentResult { get; private set; }
    private Vector3 snapShotPosition;

    [Header("Combat Band Gizmos")]
    [SerializeField] private bool drawBandGizmos = true;

    [Range(8, 128)]
    [SerializeField] private int circleSegments = 48;

    public Vector3 Debug_RepositionTarget;

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
        //   AnimationClip attackVariant = attackVariants[UnityEngine.Random.Range(0, attackVariants.Count)];
        AnimationClip deathVariant = deathVariants[UnityEngine.Random.Range(0, deathVariants.Count)];

        animatorOverrideController["Walk_"] = walkVariant;
        animatorOverrideController["Idle_"] = idleVariant;
        animatorOverrideController["Run_"] = runVariant;
        // animatorOverrideController["Attack_"] = attackVariant;
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
        bufferState = new BufferState(this);
        repositionState = new RepositionState(this);
        SwitchState(idleState);
        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.playerDead += HandleEnemyStateOnPlayerDeath;
        soundSensor.OnSoundHeard += HandleSoundStimulus;
        visionSensor.OnPeripheralGlimpse += HandlePeripheralStimulus;
    }

    void Update()
    {
        // Animator_SetFloat("Speed", enemyMovement.MovementSpeed);
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

        var pickNsearchPoints = availableSearchPoints.Count * SearchPointFactorPercent;
        int roundUp = (int)Mathf.Ceil((float)pickNsearchPoints);
        int finalNsearchPoints = Mathf.Clamp(roundUp, 2, 8);
        finalNsearchPoints = (int)MathF.Min(finalNsearchPoints, availableSearchPoints.Count);

        for (int i = 0; i < finalNsearchPoints; i++)
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
        var distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);


        if (IsInState(repositionState) && distanceToPlayer > farAttacksAsset.maxRange)
        {
            //MAKE SURE TO CHECK VISIBILITY RESULT BEFORE TRANSITIONING TO ANY STATE BECAUSE WE HAVE TO TAKE DARKNESS IN ACCOUNT AS WELL.
            SwitchState(chaseState);
        }
        if (IsInState(repositionState))
        {
            return;
        }

        var directionToPlayer = player.transform.position - transform.position;
        var dot = Vector3.Dot(transform.forward.normalized, directionToPlayer.normalized);

        if (!IsInState(AttackState) && IsInState(chaseState) && HasVision() && IsInCombatBand() && !IsInCooldown() && dot > AttackDotThreshold)
        {
            Debug.Log("[Chase] Entering Attack.");
            // Debug.Log("[EnemeyBrain] Dot for Attack is: " + dot);
            enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, player.transform.position);
            //Enemy.RequestAnimation(new AnimationIntent(AnimationType.Attack, 50));
            SwitchState(AttackState);
        }

        //INVESTIGATION TRIGGERED BY VISION
        if (!IsInState(chaseState) && !IsInState(attackState) && IsCenterVision())
        {
            if (CheckVisibilityResult(VisionSensor.visibilityResult.Chase))
            {
                SelectNextBand();
                Debug.Log("[EnemyBrain] current combat band is " + GetCurrentCombatBand());
                SwitchState(chaseState);
                return;
            }
        }

        if (!IsInState(chaseState) && !IsInState(attackState) && !IsInState(investigateState) && IsCenterVision())
        {
            if (CheckVisibilityResult(VisionSensor.visibilityResult.Investigate))
            {
                InitializeInvestigateState(snapShotPosition, 10f);
                SwitchState(investigateState);
                return;
            }
        }


        if (Time.time - lastStimulusTime > stimulusMemoryDuration)
        {
            lastStimulusPosition = Vector3.zero;
            // suspicion = 0f;
        }

        //THIS WHOLE THING WILL BE GONE AND WE WILL INSTEAD HANDLE INVESTIGATION TRANSITION VIA STAGES OF SUSPICION (Check notepad for concept). 7th march 2026
        //19th march 2026 LOL I LITERALLY DIDN'T DO THAT , REALISED THE GRADUAL SUSPICION INCREMENT IS MESSY & NOT IT.. HAD TO HANDLE BOTH SOUND AND VISION SEPARATELY , FUNNY THING IS THE WHOLE SUSPCION SYSTEM IS GONE..
        //---
        // if ((IsInState(idleState) || IsInState(wanderState)) && soundSensor.HasValidSound() && soundSensor.lastHeardValue > investigationThreshold) //&& soundSensor.LastHeardRadius >= maxRadiusForHearing)
        // {
        //     InitializeInvestigateState(soundSensor.LastHeardPosition, InvestigateAreaRadius);
        //     SwitchState(investigateState);
        // }

        //INVESTIGATION TRIGGERED BY SOUND
        if (lastStimulusPosition != Vector3.zero)
        {
            if (IsInState(investigateState) || IsInState(searchState)) // && soundSensor.HasValidSound()) //&& soundSensor.LastHeardRadius >= maxRadiusForHearing)
            {
                float distance = Vector3.Distance(lastStimulusPosition, currentInvestigationCenter);
                if (distance > currentInvestigationRadius)
                {
                    InitializeInvestigateState(lastStimulusPosition, InvestigateAreaRadius);
                    SwitchState(investigateState);
                }
            }

            if (IsInState(idleState) || IsInState(wanderState))
            {
                InitializeInvestigateState(lastStimulusPosition, InvestigateAreaRadius);
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
        if (visionSensor != null)
        {
            var oldResult = previousResult;
            currentResult = visionSensor.VisibilityResult;
            //            Debug.Log("[EnemyBrain] current Result: " + currentResult);
            if (!isEndingChase && oldResult == VisionSensor.visibilityResult.Chase && currentResult == VisionSensor.visibilityResult.None && currentlyChasing)
            {
                SetChaseEnd(true);
            }

            if (HasVision() && IsCenterVision())
            {
                lastConfirmedPosition = visionSensor.LastSeenPosition;
                lastConfirmedSeenTime = visionSensor.LastSeenTime;

                if (currentlyChasing)
                {
                    chaseTargetPosition = lastConfirmedPosition;
                    lastChaseTime = Time.time;
                }

                if (hasSeenFirstTime)
                {
                    hasSeenFirstTime = false;
                    SurpriseModafaka();
                }
            }

            //Only take 1 snapshot of playerposition for investigation on transition from none type of visibility into investigate  visibility
            if (currentResult == VisionSensor.visibilityResult.Investigate && previousResult == VisionSensor.visibilityResult.None)
            {
                snapShotPosition = visionSensor.LastSeenPosition;
            }

            previousResult = currentResult;
            //            Debug.Log("[EnemyBrain] previous Result: " + previousResult);
        }



        Vector3 origin = visionSensor.transform.position;
        Vector3 direction = player.transform.position - visionSensor.transform.position;
        float distance = direction.magnitude;
        //  bool recentlyChasing = currentlyChasing || Time.time - lastChaseTime <= visionGraceDuration;
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

    public bool WasRecentlyChasing()
    {
        return Time.time - lastConfirmedSeenTime <= visionGraceDuration;
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
        chaseTargetPosition = lastChasePosition;
        InitializeInvestigateState(lastChasePosition, InvestigateAreaRadius);
        postChase = true;
        SetChasePause(true);
        SwitchState(investigateState);
    }

    private void HandleSoundStimulus(Vector3 position)
    {
        lastStimulusPosition = position;
        lastStimulusTime = Time.time;
    }
    private void HandlePeripheralStimulus(Vector3 position)
    {
        lastStimulusPosition = position;
        lastStimulusTime = Time.time;
        enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, position);
    }

    private void HandleEnemyStateOnPlayerDeath(bool state)
    {
        SwitchState(idleState);
    }

    public void SelectNextBand()
    {
        var value = UnityEngine.Random.value;

        if (value < 0.33)
        {
            currentCombatBand = CombatBand.Close;
            currentAttackProfile = closeAttacksAsset;
        }
        else if (value < 0.66)
        {
            currentCombatBand = CombatBand.Mid;
            currentAttackProfile = midAttacksAsset;
        }
        else
        {
            currentCombatBand = CombatBand.Far;
            currentAttackProfile = farAttacksAsset;
        }
    }

    public CombatBand GetCurrentCombatBand()
    {
        return currentCombatBand;
    }

    public void OverrideAttackAnimation(AnimationClip clip)
    {
        animatorOverrideController["Attack_"] = clip;
        animator.runtimeAnimatorController = animatorOverrideController;
        animator.Play("Attack", 0, 0f);
    }

    private bool IsInCombatBand()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float desiredBandDistance = GetDesiredBandDistance(GetCurrentCombatBand()) / 2;
        float positioning = Mathf.Abs(distanceToPlayer - desiredBandDistance);
        Debug.Log("[EnemyBrain] Distanceto Player " + distanceToPlayer);
        return positioning <= desiredBandDistance + bandDistanceTolerance;
    }

    private float GetDesiredBandDistance(CombatBand combatBand)
    {
        if (combatBand == CombatBand.Far)
        {
            return farBandAttackDistance;
        }
        else if (combatBand == CombatBand.Mid)
        {
            return midBandAttackDistance;
        }
        else
        {
            return closeBandAttackDistance;
        }
    }

    public void ApplyRootMotion(bool _applyRootMotion)
    {
        animator.applyRootMotion = _applyRootMotion;
    }

    public bool HasVision()
    {
        return visionSensor.HasLineOfSight;
    }

    public bool IsCenterVision()
    {
        return visionSensor.AngleFactor > 0.5f;
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

    private void SurpriseModafaka()
    {
        SetChasePause(true);
    }

    public void SetChasePause(bool permission)
    {
        shouldPauseOnChase = permission;
    }

    public void SetChaseEnd(bool _isEndingChase)
    {
        isEndingChase = _isEndingChase;
    }

    public bool CheckVisibilityResult(VisionSensor.visibilityResult expectedResult)
    {
        return visionSensor != null && visionSensor.VisibilityResult == expectedResult;
    }

    void OnDrawGizmos()
    {
        DrawCircle(transform.position, proximityRadius, 40);
        if (currentInvestigationRadius > 0f)
        {
            Gizmos.color = UnityEngine.Color.orange;
            DrawCircle(currentInvestigationCenter, currentInvestigationRadius, 40);
        }

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
            if (chaseTargetPosition == transform.position)
            {
                Gizmos.color = UnityEngine.Color.green;
            }
            else
            {
                Gizmos.color = UnityEngine.Color.red;

            }

            // Sphere at last chase position
            Gizmos.DrawSphere(chaseTargetPosition, 0.25f);

            // Line from zombie to chase target
            Gizmos.DrawLine(transform.position, chaseTargetPosition);
        }

        Gizmos.color = UnityEngine.Color.brown; // bright cyan



        // Optional vertical line for clarity
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.up * 2f
        );

        if (!drawBandGizmos || player == null)
            return;

        DrawBand(closeAttacksAsset, UnityEngine.Color.green);
        DrawBand(midAttacksAsset, UnityEngine.Color.yellow);
        DrawBand(farAttacksAsset, Color.red);

        DrawRepositionTarget();
    }

    void DrawBand(AttackTypes profile, Color color)
    {
        if (profile == null)
            return;

        Vector3 center = player.transform.position;

        Gizmos.color = color;

        DrawCirclev2(center, profile.minRange);
        DrawCirclev2(center, profile.maxRange);
    }

    void DrawCirclev2(Vector3 center, float radius)
    {
        float step = Mathf.PI * 2f / circleSegments;

        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;

        for (int i = 1; i <= circleSegments; i++)
        {
            float angle = step * i;

            Vector3 nextPoint =
                center +
                new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

            Gizmos.DrawLine(prevPoint, nextPoint);

            prevPoint = nextPoint;
        }
    }
    void DrawRepositionTarget()
    {
        if (Debug_RepositionTarget == Vector3.zero)
            return;

        Gizmos.color = UnityEngine.Color.cyan;

        Gizmos.DrawSphere(Debug_RepositionTarget, 0.2f);
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
