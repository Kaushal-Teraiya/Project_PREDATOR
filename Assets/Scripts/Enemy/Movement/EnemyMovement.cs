using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Movement API + shared state + coordination.
/// EnemyMovement owns Unity components, shared movement/rotation state, shared config, and
/// the public API other systems (EnemyBrain, etc.) call. The actual movement algorithms live
/// in GroundMovement and SurfaceTraversalMovement — this class dispatches to them based on
/// currentMovementSurface and otherwise stays out of their way.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    //==============================Variables===============================//
    #region Base Movement Tunables

    [Header("Base Movement")]
    [SerializeField] private float enemySpeed;
    [SerializeField] private float rotationSpeed = 360f;
    public float RotationSpeed => rotationSpeed;
    [SerializeField] private float rotationThreshold = 2f;
    public float RotationThreshold => rotationThreshold;

    public float MovementSpeed => enemySpeed;

    #endregion Base Movement Tunables

    #region Core Components
    private NavMeshAgent agent;
    private ProximitySensor proximitySensor;
    private AvoidanceSteering avoidance;
    private Animator animator;
    public Animator _Animator => animator;
    #endregion

    #region Movement Logic
    private GroundMovement groundMovement;
    private SurfaceTraversalMovement surfaceTraversalMovement;
    #endregion

    #region State Enums

    public enum MovementMode
    {
        Idle,
        Investigate,
        Chase,
        Search,
        Wander,
        Reposition,
        Reaction
    }

    public enum MovementSurface
    {
        None,
        Ground,
        Wall,
        Ceiling,
        GenericSurface
    }

    public enum RotationPriority
    {
        None,
        State,
        Proximity,
        Vision
    }

    public enum TraversalPhase
    {
        None,
        Mounting,
        Traversing,
        Transitioning,
        Leaping
    }

    public enum TraversalContext
    {
        None,
        Inside,
        Outside
    }

    public enum CrawlIntent
    {
        None,
        DesiredDirection,
        Random,
        SurfaceUp,
        SurfaceDown,
        SurfaceLeft,
        SurfaceRight
    }

    #endregion State Enums

    #region Core Movement States

    // These two are inspector-configured seeds only — the moment Awake() constructs
    // surfaceTraversalMovement, the *runtime* copies live there (see SetCrawlIntent /
    // SetSurfaceTransitionAllowed). Kept here, with the same field names, purely so existing
    // serialized prefab/scene values keep working.
    [SerializeField] private CrawlIntent crawlIntent;
    [SerializeField] private bool isTransitionAllowed;

    private MovementSurface currentMovementSurface;
    public MovementSurface CurrentMovementSurface => currentMovementSurface;
    public MovementSurface detectedGeometry => surfaceTraversalMovement.DetectedGeometry;
    private MovementMode currentMovementMode;
    private RotationPriority currentRotationPriority;
    public RotationPriority CurrentRotationPriority => currentRotationPriority;
    private Vector3 currentRotationTargetPosition;
    public Vector3 CurrentRotationTargetPosition => currentRotationTargetPosition;
    public Vector3 lastMovementDir => groundMovement.LastMovementDir;
    private float currentSpeed;
    public float CurrentSpeed => currentSpeed;
    private bool canRotate;
    public bool CanRotate => canRotate;

    #endregion Core Movement States

    #region Animation Data

    public AnimationIntent currentAnimationIntent;
    private AnimationIntent lastAppliedAnimationIntent;

    #endregion

    #region Navmesh Data

    [Header("NavMesh Data")]
    [SerializeField] private float repathInterval = 0.5f;
    public float RepathInterval => repathInterval;
    [SerializeField] private float acceleration = 10f;

    #endregion

    #region Leap Data

    [Header("Leap Data")]
    private bool isLeaping;
    private bool hasCapturedArcStart;
    private float delayTimer;
    private float arcTimer;
    private float delayDuration;
    private float arcDuration;
    private float arcHeight;
    private Vector3 leapDirection;
    private Quaternion leapTargetRotation;
    private bool blendRotationDuringLeap;
    private Quaternion leapStartRotation;
    private Vector3 arcStartPosition;
    private Vector3 arcEndPosition;

    #endregion

    #region Traversal State

    public event Action OnTransitionComplete;
    public event Action OnEdgeDetected;
    public event Action OnLeapEnded;
    public bool IsTransitionAllowed;

    public Vector3 currentSurfaceNormal { get; private set; }

    #endregion Traversal State

    #region Traversal Tunables

    [Header("Traversal Layers")]

    [SerializeField] private LayerMask TraversableSurfaceMask;

    [Header("Traversal Detection")]

    [SerializeField] private float offsetForNewSurface;
    [SerializeField] private float sphereCenterOffsetFromEye;
    [SerializeField] private float eyeOffset;
    [SerializeField] private float frontWallDetectionDistance;

    [Header("Traversal Transition")]

    [SerializeField] private float surfaceTransitionThreshold;
    [SerializeField] private float unlockAfterTime;
    public float UnlockAfterTime => unlockAfterTime;
    [SerializeField] private float surfaceOffset;

    [Header("Traversal Targeting")]

    [SerializeField] private int numberOfAttempts;

    [Header("Sphere Traversal")]

    [SerializeField] private Transform sphereCenter;
    public Transform SphereCenter => sphereCenter;
    [SerializeField] private float Sphere_targetReachDistance;
    [SerializeField] private float targetDistance;
    [SerializeField] private float edgeResetDelay = 1f;

    public Transform Transform => transform;
    public LayerMask _TraversableSurfaceMask => TraversableSurfaceMask;
    public float SurfaceOffset => surfaceOffset;
    public float SurfaceTransitionThreshold => surfaceTransitionThreshold;
    public float FrontWallDetectionDistance => frontWallDetectionDistance;
    public float EyeOffset => eyeOffset;
    public float OffsetForNewSurface => offsetForNewSurface;
    public float SphereCenterOffsetFromEye => sphereCenterOffsetFromEye;
    public int NumberOfAttempts => numberOfAttempts;
    public float SurfaceRoamDistance => surfaceTraversalMovement.SurfaceCrawlAbility.roamDistance;
    public float EdgeResetDelay => edgeResetDelay;
    public float SurfaceCrawlSpeed => surfaceTraversalMovement.SurfaceCrawlAbility.crawlSpeed;
    public float SurfaceTurnSpeed => surfaceTraversalMovement.SurfaceCrawlAbility.turnSpeed;
    public TraversalPhase CurrentTraversalPhase => surfaceTraversalMovement.CurrentPhase;
    public SurfaceCrawlAbility surfaceCrawlAbility => surfaceTraversalMovement.SurfaceCrawlAbility;


    private IMovementLogic currentMovementLogic;

    #endregion

    //===================================Functions==================================//
    #region Unity Lifecycle
    void Awake()
    {
        proximitySensor = GetComponentInChildren<ProximitySensor>();
        avoidance = GetComponent<AvoidanceSteering>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        canRotate = true;

        groundMovement = new GroundMovement(this, transform, agent, avoidance);
        surfaceTraversalMovement = new SurfaceTraversalMovement(this, transform, crawlIntent, isTransitionAllowed);
    }

    private void Start()
    {
        SetCurrentMovementSurface(MovementSurface.Ground);
    }

    private void Update()
    {
        if (surfaceTraversalMovement.IsMounting())
        {
            surfaceTraversalMovement.AlignToSurface();
            return;
        }

        surfaceTraversalMovement.TickEdgeTimer();

        UpdateSurfaceNormal();
        ResetAnimationIntent();
        ResolveSpeed();
        Rotate();
        ExecuteLeap();
        currentSpeed = Mathf.MoveTowards(currentSpeed, enemySpeed, acceleration * Time.deltaTime);
        agent.nextPosition = transform.position;
    }
    #endregion Unity Lifecycle

    #region Movement API

    public void MoveTo(Vector3 destination)
    {
        if (IsCurrentTraversalPhase(TraversalPhase.Mounting))
            return;

        currentMovementLogic.MoveTo(destination);
    }

    public void Rotate()
    {
        if (!canRotate)
        {
            return;
        }

        /* 24th May 2026 THIS is place where this FUCKASS BUG ate my whole week !!  Bug: on transition to ceiling the zombie would flip onto the outer surface of the ceiling 
        FIX: the flip was happening due to the fallback because when we set Current Movement surface to ceiling there was no ceiling related gate so it fellback on Ground rotation!!!!!
        FUCK YOU!!!!!!!!I solved it !! HUHUHUHUAHAHHAHAHA 😤🤣😈*/
        if (IsCurrentMovementSurface(MovementSurface.Wall) || IsCurrentMovementSurface(MovementSurface.Ceiling))
        {
            surfaceTraversalMovement.Rotate();
            return;
        }

        if (IsCurrentMovementSurface(MovementSurface.GenericSurface))
        {
            return;
        }

        if (IsCurrentMovementSurface(MovementSurface.Ground))
        {
            groundMovement.Rotate();
        }
    }

    public void Stop()
    {
        SetMovementMode(MovementMode.Idle);
        RequestAnimation(new AnimationIntent(AnimationType.Idle, 60));
    }
    public void SetMovementMode(MovementMode mode)
    {
        currentMovementMode = mode;
    }

    public void SetRotationPermission(bool allowRotate)
    {
        canRotate = allowRotate;
    }

    public void RotationIntent(RotationPriority priority, Vector3 position)
    {
        if (priority < currentRotationPriority)
        {
            return;
        }

        currentRotationTargetPosition = position;
        currentRotationPriority = priority;
    }

    public void ClearRotationIntent()
    {
        currentRotationPriority = RotationPriority.None;
    }
    #endregion Movement API

    #region Shared Surface Data

    private void UpdateSurfaceNormal()
    {
        var origin = transform.position + transform.up * 1f;
        var direction = -transform.up;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 3f, TraversableSurfaceMask))
        {
            currentSurfaceNormal = hit.normal;
        }
    }

    public void SetCurrentSurfaceNormal(Vector3 currentSurfaceNormal)
    {
        this.currentSurfaceNormal = currentSurfaceNormal;
    }

    #endregion Shared Surface Data

    #region Speed & Mode

    private void ResolveSpeed()
    {
        switch (currentMovementMode)
        {
            case MovementMode.Idle:
                enemySpeed = 0f;
                break;

            case MovementMode.Investigate:
                enemySpeed = 2f;
                break;

            case MovementMode.Search:
                enemySpeed = 4f;
                break;

            case MovementMode.Chase:
                enemySpeed = 7f;
                break;

            case MovementMode.Wander:
                enemySpeed = 2f;
                break;
            case MovementMode.Reposition:
                enemySpeed = 10f;
                break;
            case MovementMode.Reaction:
                enemySpeed = 0f;
                break;

            default:
                enemySpeed = 3f;
                break;
        }
    }

    #endregion Speed & Mode

    #region Getters

    public Vector3 GetMovementDirection()
    {
        return lastMovementDir;
    }

    public void SetDesiredSurfaceDirection(Vector3 destination)
    {
        surfaceTraversalMovement.SetDesiredDirection(destination);
    }

    public CrawlIntent GetContinuationDirection(Vector3 toPlayer)
    {
        return surfaceTraversalMovement.GetContinuationDirection(toPlayer);
    }

    #endregion

    #region Surface Mounting & Transitions

    public void BeginSurfaceMount(SurfaceInfo surfaceInfo, SurfaceCrawlAbility surfaceCrawlAbility, Vector3 projectionVector)
    {
        surfaceTraversalMovement.BeginSurfaceMount(surfaceInfo, surfaceCrawlAbility, projectionVector);
    }

    public void InvokeUnlockTransition(float delay)
    {
        surfaceTraversalMovement.InvokeUnlockTransition(delay);
    }

    #endregion Surface Mounting & Transitions

    #region Avoidance & Proximity
    public bool IsAvoiding()
    {
        return avoidance.IsAvoiding();
    }

    public void DisableProximity()
    {
        proximitySensor.enabled = false;
        avoidance.enabled = false;
    }

    public void EnableProximity()
    {
        proximitySensor.enabled = true;
        avoidance.enabled = true;
    }

    #endregion Avoidance & Proximity

    #region Animation Control System
    public void ResetAnimationIntent()
    {
        currentAnimationIntent.animationType = AnimationType.None;
        currentAnimationIntent.priority = -1;
    }

    public void RequestAnimation(AnimationIntent intent)
    {
        if (intent.priority > currentAnimationIntent.priority)
        {
            currentAnimationIntent = intent;
        }
    }

    public void ApplyAnimationIntent()
    {
        if (currentAnimationIntent.animationType == lastAppliedAnimationIntent.animationType) return;

        lastAppliedAnimationIntent = currentAnimationIntent;

        switch (currentAnimationIntent.animationType)
        {
            case AnimationType.Idle:
                Animator_SetFloat("Speed", 0f);
                break;
            case AnimationType.Walk:
                Animator_SetFloat("Speed", 0.5f);
                break;
            case AnimationType.Run:
                Animator_SetFloat("Speed", 7f);
                break;
            case AnimationType.Attack:
                Animator_SetTrigger("Attack");
                break;
            case AnimationType.Death:
                Animator_SetBool("IsDead", true);
                break;
            case AnimationType.Reposition:
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("ArmsBackRun"))
                    Animator_SetTrigger("EnterReposition");
                break;
            case AnimationType.SurfaceCrawl:
                Animator_SetTrigger("WallCrawl");
                break;
            case AnimationType.CrawlJump:
                Animator_SetTrigger("CrawlJump");
                break;
            case AnimationType.HitReaction:
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("HitReaction_"))
                    Animator_SetTrigger("Hit");
                ResetAnimationIntent();
                break;
            default:
                Animator_SetFloat("Speed", 0f);
                break;
        }
    }

    #endregion Animation Control System

    #region Leap
    public void StartLeap(Vector3 targetPosition, float delayDuration, float arcDuration, float arcHeight, Quaternion? targetRotation = null)
    {

        hasCapturedArcStart = false;
        DisableProximity();
        isLeaping = true;
        delayTimer = 0f;
        arcTimer = 0f;
        arcEndPosition = targetPosition;
        this.delayDuration = delayDuration;
        this.arcDuration = arcDuration;
        this.arcHeight = arcHeight;
        leapDirection = (targetPosition - transform.position).normalized;
        leapTargetRotation = targetRotation ?? transform.rotation;
        blendRotationDuringLeap = targetRotation.HasValue;
        leapStartRotation = transform.rotation;
        RotationIntent(RotationPriority.State, targetPosition);
    }

    private void ExecuteLeap()
    {
        if (!isLeaping) return;

        if (delayTimer < delayDuration)
        {
            delayTimer += Time.deltaTime;
            return;
        }

        if (!hasCapturedArcStart)
        {
            arcStartPosition = transform.position;
            hasCapturedArcStart = true;
        }
        arcTimer += Time.deltaTime;
        var t = Mathf.Clamp01(arcTimer / arcDuration);

        if (blendRotationDuringLeap)
        {
            transform.rotation = Quaternion.Slerp(
                leapStartRotation,
                leapTargetRotation,
                t);
        }

        Vector3 horizontal = Vector3.Lerp(arcStartPosition, arcEndPosition, t);
        var height = arcHeight * 4 * t * (1 - t);
        horizontal.y += height;
        transform.position = horizontal;
        
        if (arcTimer >= arcDuration)
        {
            RotationIntent(RotationPriority.State, transform.position + leapDirection);
            isLeaping = false;
            arcTimer = 0f;
            delayTimer = 0f;
            SetCurrentMovementSurface(MovementSurface.Ground);
            SetAgentEnabled(true);

            //This is crucial it fixes the stale reposition target when leap happens between different ground heights.
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, agent.areaMask))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }

            OnLeapEnded?.Invoke();
        }
    }
    #endregion Leap

    #region Boolean Setter Functions

    public void ResetCrawlIntent()
    {
        surfaceTraversalMovement.ResetCrawlIntent();
    }
    public bool IsMounting()
    {
        return surfaceTraversalMovement.IsMounting();
    }

    public bool IsCurrentTraversalPhase(TraversalPhase traversalPhase)
    {
        return surfaceTraversalMovement.IsCurrentPhase(traversalPhase);
    }

    public bool IsCurrentTraversalContext(TraversalContext traversalContext)
    {
        return surfaceTraversalMovement.IsCurrentContext(traversalContext);
    }

    public void SetCurrentMovementSurface(MovementSurface surface)
    {
        currentMovementSurface = surface;
        switch (surface)
        {
            case MovementSurface.Ground:
                currentMovementLogic = groundMovement;
                break;

            case MovementSurface.Wall:
            case MovementSurface.Ceiling:
            case MovementSurface.GenericSurface:
                currentMovementLogic = surfaceTraversalMovement;
                break;

            default:
                currentMovementLogic = groundMovement;
                break;
        }
    }
    public void SetDetectedGeometry(MovementSurface geometry)
    {
        surfaceTraversalMovement.SetDetectedGeometry(geometry);
    }

    public void SetCrawlIntent(CrawlIntent intent)
    {
        surfaceTraversalMovement.SetCrawlIntent(intent);
    }

    public bool IsCurrentMovementSurface(MovementSurface movementSurface)
    {
        return currentMovementSurface == movementSurface;
    }

    public bool IsDetectedGeometry(MovementSurface geometry)
    {
        return surfaceTraversalMovement.IsDetectedGeometry(geometry);
    }

    public void SetPlayerAvoidance(bool _value)
    {
        avoidance.SetPlayerAvoidance(_value);
    }

    public void Animator_SetFloat(string floatName, float speed)
    {
        animator.SetFloat(floatName, speed);
    }

    public void Animator_SetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    public void Animator_ResetTrigger(string triggerName)
    {
        animator.ResetTrigger(triggerName);
    }

    public void Animator_SetBool(string boolName, bool value)
    {
        animator.SetBool(boolName, value);
    }

    public void SetSurfaceTransitionAllowed(bool _isTransitionAllowed)
    {
        surfaceTraversalMovement.SetTransitionAllowed(_isTransitionAllowed);
    }

    public void SetTraversalPhase(TraversalPhase phase)
    {
        surfaceTraversalMovement.SetPhase(phase);
    }

    public void SetAgentEnabled(bool enableAgent)
    {
        agent.enabled = enableAgent;
    }

    public void DisableNavmeshAgent()
    {
        agent.enabled = false;
    }
    public bool CanTransitionTo(MovementSurface targetSurface)
    {
        return surfaceTraversalMovement.CanTransitionTo(targetSurface);
    }

    public void InvokeAfter(float delay, Action callback)
    {
        StartCoroutine(InvokeAfterCoroutine(delay, callback));
    }

    private IEnumerator InvokeAfterCoroutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    // Called by SurfaceTraversalMovement — events can only be raised from the declaring class.
    public void NotifyTransitionComplete() => OnTransitionComplete?.Invoke();
    public void NotifyEdgeDetected() => OnEdgeDetected?.Invoke();

    #endregion Boolean Setter Functions

    #region GizmosDebug
    private void OnDrawGizmosSelected()
    {
        // surfaceTraversalMovement is only constructed in Awake(), which doesn't run in edit
        // mode until the object has actually been through Play — guard against that.
        surfaceTraversalMovement?.DrawGizmosSelected();
    }

    private void OnDrawGizmos()
    {
        surfaceTraversalMovement?.DrawGizmos();
    }

    #endregion GizmosDebug
}