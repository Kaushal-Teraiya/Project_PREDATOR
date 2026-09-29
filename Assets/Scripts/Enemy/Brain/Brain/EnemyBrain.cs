using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.AI;


public class EnemyBrain : MonoBehaviour
{
    [Header("Sensors")]
    private SoundSensor soundSensor;
    private VisionSensor visionSensor;

    // [Header("Archetype")]

    // public Archetype enemyArchetype;
    [SerializeField] private EnemyMovement.MovementSurface preferredMovementSurface;
    // private bool canClimbWalls;
    // private bool canLunge;

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
    private CombatState combatState;
    public IEnemyState RepositionState => repositionState;
    public IEnemyState WanderState => wanderState;
    public IEnemyState AttackState => attackState;
    public IEnemyState ChaseState => chaseState;
    public IEnemyState IdleState => idleState;
    public IEnemyState BufferState => bufferState;
    public IEnemyState CombatState => combatState;

    [Header("Object & Script References")]
    private EnemyMovement enemyMovement;
    public EnemyMovement EnemyMovement => enemyMovement;
    private Vector3 currentInvestigationCenter;
    private float currentInvestigationRadius;
    public GameObject player { get; private set; }
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    public EnemyHealth _enemyHealth => enemyHealth;
    [SerializeField] private WayPointManager wayPointManager;
    public WayPointManager _WayPointManager => wayPointManager;
    private PlayerMovement playerMovement;


    [Header("SearchPoint Data")]
    [SerializeField] private LayerMask searchPointLayer;
    [SerializeField] private float searchPointFactorPercent = 0.5f;

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
    [SerializeField] private float chaseTargetArrivalRadius;
    public float ChaseTargetArrivalRadius => chaseTargetArrivalRadius;
    public bool isEndingChase { get; private set; }
    private float lastChaseTime;
    private Vector3 investigationForward;
    private bool postChase;
    public bool shouldPauseOnChaseStart { get; private set; }
    public bool hasSeenPlayerFirstTime = true;
    public IPursuitBehaviour pursuitBehaviour { get; private set; }

    [Header("AttackState Data")]
    [SerializeField] private float attackDistance; //creats a spacioing between Enemy and Player.
    private float attackRegisterDistance; //Defines at which distance the performed attack is registered.
    //[SerializeField] private float midRangeAttackRegisterDistance, closeRangeAttackRegisterDistance, farRangeAttackRegisterDistance;
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
    public float BandDistanceTolerance => bandDistanceTolerance;
    public float AttackCooldown => attackCooldown;
    public float MidBandCompression => midBandCompression;
    public float FarBandCompression => farBandCompression;
    public enum CombatBand
    {
        None,
        Far,
        Mid,
        Close
    }
    [SerializeField] private CombatBand currentCombatBand = CombatBand.None;
    [SerializeField] private float bandDistanceTolerance = 0.5f;

    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float lungeAttackPredictionDistance = 1.5f;
    private ICombatStyle currentCombatStyle;
    public ICombatStyle CurrentCombatStyle => currentCombatStyle;
    public enum CombatStyleType
    {
        AttackReposition,
        Berserk,
        Circling
    }

    [SerializeField] private CombatStyleType combatStyleType;

    [Header("Navmesh Data")]
    [SerializeField] private float distanceForSampling = 5f;
    public float DistanceForSampling => distanceForSampling;

    [Header("WanderState Data")]
    [SerializeField] private float wayPointCollectionRadius = 10f;
    public float _WayPointCollectionRadius => wayPointCollectionRadius;

    // [Header("Suspicion Data")]
    // private float suspicion;
    // [SerializeField] private float suspicionDecayRate = 20f;
    // [SerializeField] private float chaseThreshold = 100f;
    // [SerializeField] private float investigationThreshold = 30f;
    // public float Suspicion => suspicion;

    [Header("Proximity Data")]
    [SerializeField] private float proximityRadius = 10f;
    [SerializeField] private LayerMask proximityObstacleMask;

    [Header("Vision")]
    [SerializeField] private float visionGraceDuration = 3f;
    public VisionSensor.visibilityResult previousResult { get; private set; }
    public VisionSensor.visibilityResult currentResult { get; private set; }
    public Vector3 snapShotPosition { get; private set; }

    [Header("RepositionState Data")]
    [SerializeField] private LayerMask obstacleMaskForReposition;
    [SerializeField] private float midBandCompression = 0.5f;
    [SerializeField] private float farBandCompression = 0.75f;
    [SerializeField] private float repositionArrivalRadius = 0.5f;
    [SerializeField] private float sliceHalfAngle;
    [SerializeField] private float repositionTimeout = 5f;
    private bool isCommitedToReposition;
    public float Debug_movementAngle;
    public bool Debug_FrontSemiCircle, Debug_BackSemiCircle;
    public float RepositionTimeout => repositionTimeout;
    public bool IsCommitedToReposition => isCommitedToReposition;
    public float RepositionArrivalRadius => repositionArrivalRadius;
    public LayerMask ObstacleMaskForReposition => obstacleMaskForReposition;
    public PlayerMovement PlayerMovement => playerMovement;
    public float ProximityRadius => proximityRadius;
    public LayerMask ProximityObstacleMask => proximityObstacleMask;
    public float VisionGraceDuration => visionGraceDuration;
    public SurfaceCrawlAbility SurfaceCrawlAbility => GetAbility<SurfaceCrawlAbility>();
    public float PlayerVicinityThreshold => playerVicinityThreshold;
    public LayerMask ClearanceMask => clearanceMask;
    public float ProgressCheckInterval => progressCheckInterval;
    public float MinimumProgressDistance => minimumProgressDistance;
    public float MaxReachPlayerDuration => maxReachPlayerDuration;
    public float CollapseThreshold => collapseThreshold;
    public Vector3 CurrentInvestigationCenter => currentInvestigationCenter;
    public float CurrentInvestigationRadius => currentInvestigationRadius;
    public Vector3 InvestigationForward => investigationForward;
    public float SearchPointFactor => searchPointFactorPercent;
    public LayerMask SearchPointLayer => searchPointLayer;
    public bool PostChase => postChase;
    public bool IsSearchComplete => searchController.IsSearchComplete;
    public bool DrawBandGizmos => drawBandGizmos;
    public int CircleSegments => circleSegments;
    public AttackTypes MidAttackAsset => midAttacksAsset;
    public float SliceHalfAngle => sliceHalfAngle;
    public float SearchPointFactorPercent => searchPointFactorPercent;

    [Header("Combat Band Gizmos")]
    [SerializeField] private bool drawBandGizmos = true;

    [UnityEngine.Range(8, 128)]
    [SerializeField] private int circleSegments = 48;
    public bool _DrawBandGizmos => drawBandGizmos;
    public int _CircleSegments => circleSegments;

    public Vector3 Debug_RepositionTarget;

    [Header("Ragdoll")]
    public RagdollController ragdollController { get; private set; }
    public Vector3 LastHitDirection { get; private set; }
    public float LastHitForce { get; private set; }

    [Header("Archetypes")]
    [SerializeField] private EnemyArchetype enemyArchetype;

    private SurfaceTraversalController surfaceTraversalController;
    private CombatController combatController;
    private SearchController searchController;
    private PerceptionController perceptionController;
    private EnemyAnimationController animationController;
    public CombatController _CombatController => combatController;
    public SearchController SearchController => searchController;
    public PerceptionController PerceptionController => perceptionController;

    [Header("Surface Traversal Tuning")]
    [SerializeField] private float playerVicinityThreshold;
    [SerializeField] private LayerMask clearanceMask;
    [SerializeField] private float progressCheckInterval = 1f;
    [SerializeField] private float minimumProgressDistance = 0.25f;
    [SerializeField] private float maxReachPlayerDuration = 10f;
    [SerializeField] private float collapseThreshold;


    // [Header("MovementData")]
    // [SerializeField] private EnemyMovement.MovementSurface preferredMovementSurface;

    //============================================Functions============================================//

    #region Unity Lifecycle
    void Awake()
    {
        soundSensor = GetComponent<SoundSensor>();
        visionSensor = GetComponentInChildren<VisionSensor>();
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHealth = GetComponent<EnemyHealth>();
        surfaceTraversalController = new SurfaceTraversalController(this, enemyMovement);
        ragdollController = GetComponent<RagdollController>();
        animationController = GetComponent<EnemyAnimationController>();

        AssignPursuitBehavior();
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
        combatState = new CombatState(this);

        player = GameObject.FindGameObjectWithTag("Player");
        playerMovement = player.transform.GetComponent<PlayerMovement>();
        playerHealth = player.GetComponent<PlayerHealth>();

        currentCombatStyle = CombatStyleFactory.Create(combatStyleType, this);
        searchController = new SearchController(this);
        combatController = new CombatController(this, playerMovement);
        perceptionController = new PerceptionController(this, soundSensor, visionSensor);
        perceptionController.Initialize();

        playerHealth.playerDead += HandleEnemyStateOnPlayerDeath;
        enemyMovement.OnLeapEnded += HandleLeapEnd;
        enemyMovement.OnTransitionComplete += surfaceTraversalController.HandleTransitionComplete;
        enemyMovement.OnEdgeDetected += surfaceTraversalController.HandleEdgeDetected;

        SwitchState(idleState);
        // preferredMovementSurface = EnemyMovement.MovementSurface.Ceiling;
        //SetPreferredMovementSurface(EnemyMovement.MovementSurface.Ground);
    }

    void Update()
    {
        // Animator_SetFloat("Speed", enemyMovement.MovementSpeed);
        // enemyMovement.ResetAnimationIntent();
        CheckPerception();
        CheckStateChange();
        //EvaluateWall();
        surfaceTraversalController.Tick();
        currentState.Tick();
        enemyMovement.ApplyAnimationIntent();
    }

    #endregion

    #region Initializers
    private void InitializeInvestigateState(Vector3 lastKnownPosition, float radius)
    {
        currentInvestigationCenter = lastKnownPosition;
        currentInvestigationRadius = radius;
        investigateState.SetAreaCenter_AreaRadius(currentInvestigationCenter, currentInvestigationRadius);

        surfaceTraversalController.InitializeInvestigate();
    }

    public void InitializeSearch()
    {
        searchController.InitializeSearch();
        surfaceTraversalController.InitializeSearch();
    }

    public void InitializeChase()
    {
        chaseTargetPosition = lastConfirmedPosition;
        surfaceTraversalController.InitializeChase();
    }

    private void AssignPursuitBehavior()
    {
        if (GetAbility<SurfaceCrawlAbility>() != null)
        {
            pursuitBehaviour = new CrawlerPursuitBehaviour();
        }
        else
        {
            pursuitBehaviour = new GroundPursuitBehaviour();
        }
    }

    public void InitializeWanderState()
    {
        surfaceTraversalController.InitializeWander();
    }

    #endregion

    #region Getter , Collector & Helper Functions
    public T GetAbility<T>() where T : EnemyAbility
    {
        foreach (var ability in enemyArchetype.enemyAbilities)
        {
            if (ability is T matchedAbility)
            {
                return matchedAbility;
            }
        }
        return null;
    }

    public void SetHitImpact(Vector3 direction, float force)
    {
        LastHitDirection = direction;
        LastHitForce = force;
    }

    #endregion

    #region State Decesions
    public void SwitchState(IEnemyState newState)
    {
        enemyMovement.SetRotationPermission(true);
        enemyMovement.ClearRotationIntent();
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();
    }
    private void CheckStateChange()
    {
        // if (true)
        // {
        //     return;
        // }

        if (IsInState(deadState))
        {
            return;
        }

        if (playerHealth.playerisDead && IsInCombat())
        {
            enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
            //enemyMovement.Stop();
            enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Idle, 70)); //temporary idle , later we want multiple different behviour of zombies on player death
            return;

        }
        var distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);


        if (IsInState(repositionState) && distanceToPlayer > farAttacksAsset.maxRange * 1.5f && !isCommitedToReposition)
        {
            //MAKE SURE TO CHECK VISIBILITY RESULT BEFORE TRANSITIONING TO ANY STATE BECAUSE WE HAVE TO TAKE DARKNESS IN ACCOUNT AS WELL.
            SwitchState(chaseState);
        }
        if (IsInState(repositionState))
        {
            return;
        }

        if (IsInState(combatState))
        {
            return;
        }

        var directionToPlayer = player.transform.position - transform.position;
        var dot = Vector3.Dot(transform.forward.normalized, directionToPlayer.normalized);

        if (IsInState(chaseState) && HasVision() && _CombatController.IsInCombatBand() && !_CombatController.IsInCooldown() && dot > AttackDotThreshold)
        {
            Debug.Log("[Chase] Entering Attack.");
            // Debug.Log("[EnemeyBrain] Dot for Attack is: " + dot);
            enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, player.transform.position);
            //Enemy.RequestAnimation(new AnimationIntent(AnimationType.Attack, 50));
            SwitchState(combatState);
        }

        //INVESTIGATION TRIGGERED BY VISION
        if (!IsInState(chaseState) && !IsInState(attackState) && IsCenterVision())
        {
            if (CheckVisibilityResult(VisionSensor.visibilityResult.Chase))
            {
                currentCombatStyle.PrepareInitialDecision();
              
                //currentCombatStyle.Enter();
                //                Debug.Log("[EnemyBrain] current combat band is " + GetCurrentCombatBand());
                SwitchState(chaseState);
                return;
            }
        }

        if (!IsInState(chaseState) && !IsInState(attackState) && !IsInState(investigateState) && IsCenterVision())
        {
            if (CheckVisibilityResult(VisionSensor.visibilityResult.Investigate))
            {
                InitializeInvestigateState(snapShotPosition, 10f);
                //overrideGoal = Goal.FreeMove;
                SwitchState(investigateState);
                return;
            }
        }

        if (Time.time - lastStimulusTime > stimulusMemoryDuration && lastStimulusPosition != Vector3.zero)
        {
            perceptionController.ClearStimulus();
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
        perceptionController.Tick();

        lastStimulusPosition = perceptionController.LastStimulusPosition;
        lastStimulusTime = perceptionController.LastStimulusTime;

        if (!isEndingChase && currentlyChasing)
        {
            Debug.Log("[EnemyBrain] Trying to set chase end == true");
            SetChaseEnd(true);
        }

        if (perceptionController.HasVision() && perceptionController.IsCenterVision())
        {
            lastConfirmedPosition = perceptionController.LastConfirmedPosition;
            lastConfirmedSeenTime = perceptionController.LastConfirmedSeenTime;

            if (currentlyChasing)
            {
                chaseTargetPosition = lastConfirmedPosition;
                lastChaseTime = Time.time;
            }

            if (hasSeenPlayerFirstTime)
            {
                hasSeenPlayerFirstTime = false;
                PauseBeforeChase();
            }
        }
    }
    #endregion

    #region State Control Functions

    public void NotifySearchPointReleased(SearchPoint point)
    {
        searchController.NotifySearchPointReleased(point);
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

    private void HandleEnemyStateOnPlayerDeath(bool state)
    {
        SwitchState(idleState);
    }

    #endregion

    #region Combat

    public void SelectNextBand()
    {
        _CombatController.SelectNextBand();
    }

    public void SelectCloseOrMidBand()
    {
        _CombatController.SelectCloseOrMidBand();
    }

    public CombatBand GetCurrentCombatBand()
    {
        return currentCombatBand;
    }

    public void OverrideAttackAnimation(AnimationClip clip)
    {
        animationController.OverrideAttackAnimation(clip);
    }

    public Vector2 GetEffectiveBandRange(AttackTypes attackProfile)
    {
        return _CombatController.GetEffectiveBandRange(attackProfile);
    }

    public Vector3 PredictPlayerPosition(float predictionTime, float maxPredictionDistance)
    {
        return _CombatController.PredictPlayerPosition(predictionTime, maxPredictionDistance);
    }

    public float ChooseBandSlice()
    {
        return _CombatController.ChooseBandSlice();
    }

    public SearchPoint GetCurrentSearchPoint()
    {
        return searchController.GetCurrentSearchPoint();
    }

    public void IncrementSearchIndex()
    {
        searchController.IncrementSearchIndex();
    }

    #endregion

    #region State Queries & Control

    public bool IsInCombat()
    {
        if (currentAttackProfile == null)
            return false;
        return IsInState(attackState) || IsInState(combatState);
    }

    private void HandleLeapEnd()
    {
        // if (GetAbility<SurfaceCrawlAbility>() != null)
        // {
        //     overrideGoal = Goal.Middle;
        // }
    }

    private bool IsInCombatBand()
    {
        return _CombatController.IsInCombatBand();
    }

    public bool CanClaim(SearchPoint point)
    {
        return searchController.CanClaim(point);
    }

    public bool HasReachedThePosition(Vector3 lastConfirmedPosition, float _arrivalRadius)
    {
        Vector3 toTarget = lastConfirmedPosition - transform.position;
        toTarget.y = 0f;

        return toTarget.sqrMagnitude <= _arrivalRadius * _arrivalRadius;
    }
    public bool WasRecentlyChasing()
    {
        return Time.time - lastConfirmedSeenTime <= visionGraceDuration;
    }
    public void SetCurrentlyChasing(bool _isChasing)
    {
        currentlyChasing = _isChasing;

        if (_isChasing)
        {
            lastChaseTime = Time.time;
        }
    }

    public void SetAttackRegisterDistance(AttackTypes currentProfile)
    {
        _CombatController.SetAttackRegisterDistance(currentProfile);
    }

    public bool HasVision()
    {
        return perceptionController.HasVision();
    }

    public bool IsCenterVision()
    {
        return perceptionController.IsCenterVision();
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
        return _CombatController.IsInCooldown();
    }

    public bool IsInState(IEnemyState state)
    {
        return currentState == state;
    }

    public void NotifyAttackEnded()
    {
        _CombatController.NotifyAttackEnded();
    }

    public void HandleDeath()
    {
        SwitchState(deadState);
    }

    public void DisablePerception()
    {
        perceptionController.DisablePerception();
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

    private void PauseBeforeChase()
    {
        SetChasePause(true);
    }

    public void SetChasePause(bool permission)
    {
        shouldPauseOnChaseStart = permission;
    }

    public void SetChaseEnd(bool _isEndingChase)
    {
        isEndingChase = _isEndingChase;
    }

    public bool CheckVisibilityResult(VisionSensor.visibilityResult expectedResult)
    {
        return perceptionController.CheckVisibilityResult(expectedResult);
    }

    public void SetIsCommitedToReposition(bool _isCommited)
    {
        isCommitedToReposition = _isCommited;
    }

    internal void SetCombatBand(CombatBand band, AttackTypes profile)
    {
        currentCombatBand = band;
        currentAttackProfile = profile;
        SetAttackRegisterDistanceInternal(profile.attackRegisterDistance);
    }

    internal AttackTypes GetCurrentAttackProfile()
    {
        return currentAttackProfile;
    }

    internal void SetAttackRegisterDistanceInternal(float distance)
    {
        attackRegisterDistance = distance;
    }

    internal float GetLastAttackEndTime()
    {
        return lastAttackEndTime;
    }

    internal void SetLastAttackEndTime(float time)
    {
        lastAttackEndTime = time;
    }

    public float GetPredictionDistance()
    {
        return lungeAttackPredictionDistance;
    }


    private void SetSurfaceTransitionAllowed(bool allowed)
    {
        enemyMovement.SetSurfaceTransitionAllowed(allowed);
    }

    #endregion

}