//using Mono.Cecil.Cil;
using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EnemyMovement : MonoBehaviour
{
    //==============================Variables===============================//
    #region Base Movement Tunables

    [Header("Base Movement")]
    [SerializeField] private float enemySpeed;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationThreshold = 2f;
    [SerializeField] private float separationWeight = 0.5f;

    public float MovementSpeed => enemySpeed;

    #endregion Base Movement Tunables

    #region Core Components
    private NavMeshAgent agent;
    private ProximitySensor proximitySensor;
    private AvoidanceSteering avoidance;
    private Animator animator;

    #endregion

    #region State Enums

    public enum MovementMode
    {
        Idle,
        Investigate,
        Chase,
        Search,
        Wander,
        Reposition
    }

    public enum MovementSurface
    {
        Ground,
        Wall,
        Ceiling,
        Sphere
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
        MovingToGeometry,
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

    #endregion State Enums

    #region Core Movement States

    private TraversalContext currentTraversalContext;
    private TraversalPhase currentTraversalPhase;
    private MovementSurface currentMovementSurface;
    private MovementSurface detectedGeometry;
    private MovementSurface currentGeometryIdentity;
    private MovementMode currentMovementMode;
    private RotationPriority currentRotationPriority;
    private Vector3 currentRotationTargetPosition;
    private Vector3 lastMovementDir;
    private float currentSpeed;
    private bool canRotate;

    #endregion Core Movement States

    #region Animation Data

    public AnimationIntent currentAnimationIntent;
    private AnimationIntent lastAppliedAnimationIntent;

    #endregion

    #region Navmesh Data

    [Header("NavMesh Data")]

    private NavMeshPath currentPath;
    private int currentCornerIndex;
    private float repathTimer;
    [SerializeField] private float repathInterval = 0.5f;
    [SerializeField] private float acceleration = 10f;

    #endregion

    #region Leap Data

    [Header("Lunge Data")]
    private bool isLunging;
    //private bool isRunning;
    private bool hasCapturedArcStart;
    private float delayTimer;
    private float arcTimer;
    private float delayDuration;
    private float arcDuration;
    private float arcHeight;
    private Vector3 lungeDirection;
    private Vector3 arcStartPosition;
    private Vector3 arcEndPosition;

    #endregion


    #region Traversal State

    private WallInfo currentSurface;
    private WallInfo detectedSurface;
    private Vector3 targetPosition;

    private Quaternion targetRotation;
    private WallCrawlAbility wallCrawlAbility;

    private Vector3 crawlTarget;
    private Vector3 crawlTargetSphere;

    private bool isChangingSurface;
    private bool isTransitionLocked;

    private Vector3 currentSphereNormal;
    private float sphereRadius;

    #endregion Traversal State

    #region  Debug Gizmos

    private Vector3 debugEyePosition;
    private Vector3 debugProbeOrigin;
    private Vector3 debugRayDirection;
    private Vector3 debugHitPointnew;
    private Vector3 debugHitNormal;
    private bool debugDidHit;
    private Vector3 debugSurfaceCheckOrigin;
    private Vector3 debugSurfaceCheckDirection;
    private bool debugSurfaceCheckHit;
    private Vector3 debugRawTarget;
    private Vector3 debugRayOrigin;
    private Vector3 debugHitPoint;

    #endregion

    #region Traversal Tunables

    [Header("Traversal Layers")]

    [SerializeField] private LayerMask WallSurfaceMask;
    [SerializeField] private LayerMask CeilingSurfaceMask;


    [Header("Traversal Detection")]

    [SerializeField] private float offsetForNewSurface;
    [SerializeField] private float sphereCenterOffsetFromEye;
    [SerializeField] private float eyeOffset;
    [SerializeField] private float frontWallDetectionDistance;


    [Header("Traversal Transition")]

    [SerializeField] private float surfaceTransitionThreshold;
    [SerializeField] private float unlockAfterTime;
    [SerializeField] private float surfaceOffset;


    [Header("Traversal Targeting")]

    [SerializeField] private int numberOfAttempts;


    [Header("Sphere Traversal")]

    [SerializeField] private Transform sphereCenter;
    [SerializeField] private float Sphere_targetReachDistance;
    [SerializeField] private float targetDistance;

    #endregion

    #region Old Variables
    //public bool isSphere { get; private set; }
    // private bool isInsideSurface;
    // private bool isOutsideSurface;
    // [SerializeField] private int numberOftries;
    // [SerializeField] private float shrinkCoeff;
    //[SerializeField] private float overlapRadius;
    //private bool foundCrawlTarget;
    //[SerializeField] private LayerMask OnWallMask;
    // private Vector3 detectedSurfaceNormal;
    // private Vector3 detectedSurfacePoint;
    // private Collider detectedSurfaceCollider;
    // private bool isTransitioningSurface;
    // DEBUG VARIABLES
    //private bool isMounting;
    // private Vector3 storedWallPoint;
    // private Vector3 storedWallNormal;
    //private bool isCrawling;

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
        currentPath = new NavMeshPath();
        canRotate = true;
    }

    private void Start()
    {
        SetCurrentMovementSurface(MovementSurface.Ground);
    }

    private void Update()
    {
        // Debug.Log("Current Traversal Phase ::" + currentTraversalPhase);
        //Debug.Log(currentTraversalPhase);
        if (IsCurrentTraversalPhase(TraversalPhase.Mounting))
        {
            SmoothMount();
            return;
        }

        ResetAnimationIntent();
        ResolveSpeed();
        //  Debug.Log("BEFORE ROT: " + transform.rotation.eulerAngles);
        ApplyRotation(); //  Debug.Log("AFTER ROT: " + transform.rotation.eulerAngles);
        ExecuteLunge();

        currentSpeed = Mathf.MoveTowards(currentSpeed, enemySpeed, acceleration * Time.deltaTime);
        agent.nextPosition = transform.position;
        // Debug.Log("FINAL ROT: " + transform.rotation.eulerAngles);
        // Debug.Log("[FINAL ROTATION] " + transform.rotation.eulerAngles);
        // Debug.Log("[SURFACE] Current: " + currentMovementSurface);
        // Debug.Log("[EnemyMovement] Enemy Speed is " + enemySpeed);
    }
    #endregion Unity Lifecycle

    #region Movement API

    public void MoveTo(Vector3 destination)
    {
        if (IsCurrentTraversalPhase(TraversalPhase.Mounting))
            return;

        //Debug.Log(currentTraversalPhase);
        //  Debug.Log(currentMovementSurface);
        switch (currentMovementSurface)
        {
            case MovementSurface.Ground:
                HandleGroundMovement(destination);
                break;
            case MovementSurface.Wall:
                HandleWallMovement(destination);
                break;
            case MovementSurface.Sphere:
                HandleSphereMovement(destination);
                break;
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
        //        Debug.Log($"Trying to set rotation intent: {priority}, Current: {currentRotationPriority}");

        if (priority < currentRotationPriority)
        {
            return;
        }

        currentRotationTargetPosition = position;
        currentRotationPriority = priority;
        //        Debug.Log($"Rotation intent set to {priority}");
    }

    public void ClearRotationIntent()
    {
        currentRotationPriority = RotationPriority.None;
    }
    #endregion Movement API

    #region Surface Detection & Traversal Context
    private void UpdateTraversalContext(bool wallAhead)
    {
        currentTraversalContext = wallAhead ? TraversalContext.Inside : TraversalContext.Outside;
    }
    private void DetectNewSurface()
    {
        if (isTransitionLocked)
        {
            return;
        }
        Vector3 eyePosition =
            transform.position +
            transform.forward * offsetForNewSurface;

        debugEyePosition = eyePosition;

        Vector3 probeOrigin =
            eyePosition +
            (-transform.up * sphereCenterOffsetFromEye);

        debugProbeOrigin = probeOrigin;

        Vector3 rayDirection = -transform.forward;

        debugRayDirection = rayDirection;

        debugDidHit = false;

        if (Physics.Raycast(
            probeOrigin,
            rayDirection,
            out RaycastHit hit,
            5f,
            WallSurfaceMask))
        {
            debugDidHit = true;
            isChangingSurface = true;
            debugHitPoint = hit.point;
            debugHitNormal = hit.normal;

            float dotProduct =
                Vector3.Dot(
                    currentSurface.wallHitNormal,
                    hit.normal
                );

            Debug.Log($"DOT : {dotProduct}");

            if (dotProduct < surfaceTransitionThreshold)
            {
                isTransitionLocked = true;
                RequestAnimation(new AnimationIntent(AnimationType.CrawlJump, 300));
                ComputeSurfaceAlignment(
                    hit.normal,
                    hit.point
                );
                isChangingSurface = false;
                PickNewCrawlTarget_Wall(numberOfAttempts, wallCrawlAbility.roamDistance);
                Invoke(nameof(UnlockTransition), unlockAfterTime);
                return;
            }
        }

        else
        {
            PickNewCrawlTarget_Wall(numberOfAttempts, wallCrawlAbility.roamDistance * 0.5f);
        }
    }
    Vector3 GetProjectableVector(Vector3 surfaceNormal)
    {
        /*This function calculates which world axis is pointing into the current surface that the enemy AI is On ,
        it returns the opposite of that world axis which can be used to project onto the new plane for perfect / correct body alignment on the new surface 
        after transition*/

        Vector3[] worldAxes =
        {
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back,
        Vector3.up,
        Vector3.down
    };

        if (IsCurrentTraversalContext(TraversalContext.Inside))
        {
            Vector3 inwardAxis = Vector3.right;

            float lowestDot = Mathf.Infinity;

            foreach (var axis in worldAxes)
            {
                float dot = Vector3.Dot(surfaceNormal, axis);

                Debug.Log($"Axis : {axis} | Dot : {dot}");

                // MOST NEGATIVE DOT
                // axis points MOST INTO wall
                if (dot < lowestDot)
                {
                    lowestDot = dot;
                    inwardAxis = axis;
                }
            }

            Debug.Log($"INWARD AXIS : {inwardAxis}");

            // Return opposite of inward axis
            Vector3 projectable = -inwardAxis;

            Debug.Log($"PROJECTABLE VECTOR : {projectable}");

            return projectable;
        }

        if (IsCurrentTraversalContext(TraversalContext.Outside))
        {
            Vector3 projectable = -surfaceNormal;
            return projectable;
        }

        return Vector3.down;

    }
    #endregion Surface Detection & Traversal Context

    #region Surface Alignment & Orientation

    private void ComputeSurfaceAlignment(Vector3 surfaceNormal, Vector3 surfacePoint)
    {
        Vector3 alignmentVector = GetProjectableVector(currentSurface.wallHitNormal);

        // storedWallPoint = surfacePoint;
        // storedWallNormal = surfaceNormal;
        currentSurface.wallHitPoint = surfacePoint;
        currentSurface.wallHitNormal = surfaceNormal;

        var forward = Vector3.ProjectOnPlane(alignmentVector, surfaceNormal).normalized;
        targetRotation = Quaternion.LookRotation(forward, surfaceNormal);
        Debug.Log("wall to wall trnastion");
        // targetRotation *= Quaternion.Euler(0, -90f, 90f); //Very Important
        targetPosition = currentSurface.wallHitPoint + currentSurface.wallHitNormal * wallCrawlAbility.wallOffset;
        //isMounting = true;
        currentTraversalPhase = TraversalPhase.Mounting;
        agent.enabled = false;
        Debug.Log(currentSurface.wallHitPoint);
    }

    private bool RotateTowardsIntent()
    {
        if (!canRotate)
        {
            Debug.Log("Rotation blocked by canRotate");
            return false;
        }

        if (currentRotationPriority == RotationPriority.None)
        {
            return false;
        }

        var direction = currentRotationTargetPosition - transform.position;
        direction.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        // if (direction.sqrMagnitude < 0.0001f)
        //     return true;

        //        Debug.Log($"Intent Dir Magnitude: {direction.magnitude}");
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        bool rotationComplete = angle <= rotationThreshold;
        //        Debug.Log($"Angle to target: {Quaternion.Angle(transform.rotation, targetRotation)}");

        // if (rotationComplete)
        // {
        //     currentRotationPriority = RotationPriority.None;
        //     return true;
        // }
        return false;
    }

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

            default:
                enemySpeed = 3f;
                break;

        }
    }

    private void ApplyRotation()
    {
        if (!canRotate)
        {
            Debug.Log("Cant rotate");
            return;
        }

        if (IsCurrentSurface(MovementSurface.Wall))
        {
            RotationOnWall();
            return;
        }

        if (IsCurrentSurface(MovementSurface.Sphere))
        {
            // WallRotation();
            return;
        }
        // normal ground
        GroundRotation();
    }

    private void RotationOnWall()
    {

    }

    private void GroundRotation()
    {
        Quaternion beforeRotation = transform.rotation;
        if (avoidance != null && avoidance.IsAvoiding())
        {
            if (lastMovementDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lastMovementDir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
            UpdateRotationAnimation(beforeRotation);
            return;
        }


        if (currentRotationPriority != RotationPriority.None)
        {
            RotateTowardsIntent();
            UpdateRotationAnimation(beforeRotation);
            return;
        }

        if (lastMovementDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lastMovementDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        }
        UpdateRotationAnimation(beforeRotation);
        ClearRotationIntent();
    }


    #endregion Surface Alignment & Orientation

    #region Traversal Movement

    private void HandleGroundMovement(Vector3 destination)
    {
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);
        if (!agent.enabled)
            return;
        repathTimer -= Time.deltaTime;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        if (repathTimer <= 0f)
        {
            if (agent.CalculatePath(destination, currentPath))
            {
                if (currentPath.status == NavMeshPathStatus.PathComplete)
                {
                    currentCornerIndex = 0;
                }
            }
            repathTimer = repathInterval;
        }

        if (currentPath.corners.Length > 0)
        {
            if (currentCornerIndex >= currentPath.corners.Length)
            {
                return;
            }

            destination = currentPath.corners[currentCornerIndex];

            if (Vector3.Distance(transform.position, destination) < 0.3f)
            {
                currentCornerIndex++;
            }
        }

        // Direction.y = 0f;A
        Vector3 Direction = destination - transform.position;
        Vector3 normalizedDirection = Direction.normalized;

        if (avoidance != null)
        {
            avoidance.SetRotationPermission(canRotate);
        }

        Vector3 movementDir;

        if (avoidance != null && avoidance.HasOverrideDirection(out Vector3 overrideDir))
        {
            movementDir = overrideDir;

        }
        else
        {
            movementDir = normalizedDirection;
        }

        movementDir.Normalize();

        if (proximitySensor != null)
        {
            Vector3 separationVector = proximitySensor.GetSeparationDirection();
            separationVector.y = 0f;
            movementDir += separationVector * separationWeight;
        }

        if (movementDir.sqrMagnitude > 0.0001f)
        {
            lastMovementDir = movementDir.normalized;
        }

        movementDir = movementDir.normalized;

        transform.position += movementDir * currentSpeed * Time.deltaTime;
        NavMeshHit groundHit;
        if (NavMesh.SamplePosition(transform.position, out groundHit, 1f, NavMesh.AllAreas))
        {
            transform.position = groundHit.position;
        }

        Debug.DrawRay(transform.position, movementDir * 2f, Color.cyan);

    }

    private void HandleWallMovement(Vector3 destination)
    {
        if (!IsCurrentTraversalPhase(TraversalPhase.Traversing))
        {
            return;
        }

        Vector3 directionToCrawlTarget = crawlTarget - transform.position;

        CheckSurfaceContinuity();
        EvaluateNewSurfaceTransition(directionToCrawlTarget);
        UpdateCrawlTarget(directionToCrawlTarget);
        ExecuteWallTraversal(directionToCrawlTarget);
    }

    private void HandleSphereMovement(Vector3 destination)
    {
        if (!IsCurrentTraversalPhase(TraversalPhase.Traversing))
        {
            return;
        }

        Vector3 moveDir = crawlTargetSphere - transform.position;

        UpdateSphereTarget(moveDir);
        ExecuteSphereTraversal(moveDir);
        RotateOnSphere(moveDir);

    }

    private void RotateOnSphere(Vector3 moveDir)
    {
        Quaternion surfaceAlignment = Quaternion.FromToRotation(transform.up, currentSphereNormal);

        transform.rotation = surfaceAlignment * transform.rotation;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, currentSphereNormal).normalized;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDir,
                currentSphereNormal
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                wallCrawlAbility.turnSpeed * Time.deltaTime
            );
    }

    private void UpdateSphereTarget(Vector3 moveDir)
    {
        currentSphereNormal = (transform.position - sphereCenter.position).normalized;

        if (moveDir.magnitude < Sphere_targetReachDistance)
        {
            PickNewCrawlTarget_Sphere();
            return;
        }
    }

    private void ExecuteSphereTraversal(Vector3 moveDir)
    {
        moveDir.Normalize();

        moveDir = Vector3.ProjectOnPlane(moveDir, currentSphereNormal).normalized;

        if (moveDir.sqrMagnitude < 0.001f)
        {
            PickNewCrawlTarget_Sphere();
            return;
        }

        Vector3 newPosition = transform.position + moveDir * wallCrawlAbility.crawlSpeed * Time.deltaTime;

        Vector3 directionFromCenter = (newPosition - sphereCenter.position).normalized;

        transform.position = sphereCenter.position + directionFromCenter * sphereRadius;

        currentSphereNormal = (transform.position - sphereCenter.position).normalized;
    }

    private void CheckSurfaceContinuity()
    {
        var origin = transform.position + transform.forward * eyeOffset + transform.up * 1f;
        debugSurfaceCheckOrigin = origin;
        debugSurfaceCheckDirection = -currentSurface.wallHitNormal * 3f;

        debugSurfaceCheckHit = Physics.Raycast(origin, -currentSurface.wallHitNormal, 3f, WallSurfaceMask);

        if (!debugSurfaceCheckHit && !IsCurrentSurface(MovementSurface.Sphere))
        {
            Debug.Log("The point is unreachable because the wall is not continuous.");
            //PickNewCrawlTarget(numberOfAttempts, wallCrawlAbility.roamDistance);
            //crawlTarget = transform.position;

            Debug.Log("we aint moving");
            //crawlTarget = Vector3.zero;
            UpdateTraversalContext(false);
            DetectNewSurface();
        }

    }

    private void EvaluateNewSurfaceTransition(Vector3 directionToCrawlTarget)
    {

        if (IsCurrentSurface(MovementSurface.Wall))
        {
            if (!IsCurrentTraversalPhase(TraversalPhase.Transitioning))
            {
                var startPoint = transform.position + transform.up * frontWallDetectionDistance;
                if (Physics.Raycast(startPoint, directionToCrawlTarget.normalized, out RaycastHit hit, 5f, WallSurfaceMask))
                {
                    detectedSurface.wallHitNormal = hit.normal;
                    detectedSurface.wallHitPoint = hit.point;
                    detectedSurface.hitCollider = hit.collider;

                    //isTransitioningSurface = true;
                    currentTraversalPhase = TraversalPhase.Transitioning;
                    RequestAnimation(new AnimationIntent(AnimationType.CrawlJump, 300));
                    UpdateTraversalContext(true);
                    ComputeSurfaceAlignment(detectedSurface.wallHitNormal, detectedSurface.wallHitPoint);
                }
            }
        }
    }

    private void UpdateCrawlTarget(Vector3 directionToCrawlTarget)
    {
        if (directionToCrawlTarget.magnitude < 0.2f)
        {
            PickNewCrawlTarget_Wall(numberOfAttempts, wallCrawlAbility.roamDistance);
            return;
        }
    }

    private void ExecuteWallTraversal(Vector3 directionToCrawlTarget)
    {
        directionToCrawlTarget.Normalize();

        if (!isChangingSurface)
        {
            Debug.Log("we're moving again");
            transform.position += directionToCrawlTarget * wallCrawlAbility.crawlSpeed * Time.deltaTime;
            Vector3 forward = Vector3.ProjectOnPlane(directionToCrawlTarget, currentSurface.wallHitNormal).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(forward, currentSurface.wallHitNormal);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                wallCrawlAbility.turnSpeed * Time.deltaTime
            );
        }
    }

    public Vector3 GetMovementDirection()
    {
        return lastMovementDir;
    }

    #endregion Traversal Movement

    #region Surface Mounting & Transitions
    public void BeginWallMount(WallInfo wallInfo, WallCrawlAbility wallCrawlAbility)
    {
        if (IsCurrentTraversalPhase(TraversalPhase.Mounting))
        {
            return;
        }

        currentGeometryIdentity = MovementSurface.Wall;
        Debug.Log(currentTraversalPhase);
        this.wallCrawlAbility = wallCrawlAbility;
        currentSurface = wallInfo;

        Vector3 forward = transform.forward;
        forward = Vector3.ProjectOnPlane(Vector3.up, currentSurface.wallHitNormal).normalized;
        if (forward.sqrMagnitude < 0.001f)
        {
            Debug.Log("FALLBACKKKKKKKKKKKKKKKKKKKEIE");
            forward = Vector3.Cross(transform.right, currentSurface.wallHitNormal).normalized;
        }

        targetRotation = Quaternion.LookRotation(forward, currentSurface.wallHitNormal);

        targetPosition = currentSurface.wallHitPoint + currentSurface.wallHitNormal * wallCrawlAbility.wallOffset;
        //isMounting = true;
        currentTraversalPhase = TraversalPhase.Mounting;
        agent.enabled = false;
        Debug.Log(currentSurface.wallHitPoint);
    }

    public void BeginSphereMount(WallInfo wallInfo, WallCrawlAbility wallCrawlAbility)
    {
        if (IsCurrentTraversalPhase(TraversalPhase.Mounting))
        {
            return;
        }

        Debug.Log("Hit Point: " + wallInfo.wallHitPoint);
        Debug.Log("Normal: " + wallInfo.wallHitNormal);

        currentGeometryIdentity = MovementSurface.Sphere;

        this.wallCrawlAbility = wallCrawlAbility;

        currentSphereNormal = wallInfo.wallHitNormal;
        currentSurface = wallInfo;

        targetPosition =
            wallInfo.wallHitPoint -
            currentSphereNormal * surfaceOffset;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                Vector3.up,
                currentSphereNormal
            ).normalized;

        targetRotation =
            Quaternion.LookRotation(
                forward,
                currentSphereNormal
            );
        // SetCurrentMovementSurface(MovementSurface.Sphere);

        //isCrawling = true;
        Debug.Log("Target Position: " + targetPosition);
        currentTraversalPhase = TraversalPhase.Mounting;
        agent.enabled = false;
    }

    private void SmoothMount()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            wallCrawlAbility.rotationSpeed * Time.deltaTime
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            wallCrawlAbility.positionSpeed * Time.deltaTime
        );

        float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);
        float positionDifference = Vector3.Distance(transform.position, targetPosition);

        if (rotationDifference < 20f && positionDifference < 0.05f)
        {
            transform.rotation = targetRotation;
            transform.position = targetPosition;
            //isMounting = false;
            // isTransitioningSurface = false;
            InitializeTraversal();
        }
    }

    private void InitializeTraversal()
    {
        currentTraversalPhase = TraversalPhase.Traversing;
        SetCurrentMovementSurface(detectedGeometry);

        if (IsDetectedGeometry(MovementSurface.Wall))
        {
            PickNewCrawlTarget_Wall(numberOfAttempts, wallCrawlAbility.roamDistance);
        }
        else if (IsDetectedGeometry(MovementSurface.Sphere))
        {
            currentSphereNormal = (transform.position - sphereCenter.position).normalized;

            sphereRadius =
                Vector3.Distance(
                    transform.position,
                    sphereCenter.position
                );

            PickNewCrawlTarget_Sphere();
        }
        ResetAnimationIntent();
        RequestAnimation(new AnimationIntent(AnimationType.WallCrawl, 70));
        //isCrawling = true;
        Debug.Log("Mount Complete :: Wall Mode");
    }
    private void UnlockTransition()
    {
        isTransitionLocked = false;
    }


    #endregion Surface Mounting & Transitions

    #region Traversal Targets
    private void PickNewCrawlTarget_Wall(int remainingAttempts, float currentRoamDistance)
    {
        if (remainingAttempts <= 0)
        {
            Debug.Log("NO TARGET FOUND!");
            //SWITCH SURFACE to ground
            return;
        }

        bool foundCrawlTarget = false;

        for (int i = 0; i < numberOfAttempts; i++)
        {
            Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
            randomDir = Vector3.ProjectOnPlane(randomDir, currentSurface.wallHitNormal).normalized;
            Vector3 rawTarget = transform.position + randomDir * currentRoamDistance;
            debugRawTarget = rawTarget;

            Vector3 origin = rawTarget + currentSurface.wallHitNormal * 1f; // adding wall normal to do safe raycast check
            debugRayOrigin = origin;

            if (Physics.Raycast(origin, -currentSurface.wallHitNormal, out RaycastHit hit, 3f))
            {
                crawlTarget = hit.point + currentSurface.wallHitNormal * wallCrawlAbility.wallOffset;
                debugHitPoint = crawlTarget;
                foundCrawlTarget = true;
                break;
            }
        }

        if (foundCrawlTarget)
        {
            return;
        }

        float shrunkRoamDistance = currentRoamDistance * 0.5f;
        Debug.Log("new roam distaance" + shrunkRoamDistance);
        // Debug.Log("Retrying with smaller roamDistance");

        PickNewCrawlTarget_Wall(remainingAttempts - 1, shrunkRoamDistance);
    }

    void PickNewCrawlTarget_Sphere()
    {
        Vector3 randomSurfaceDirection =
            UnityEngine.Random.onUnitSphere;

        crawlTargetSphere =
            sphereCenter.position +
            randomSurfaceDirection * sphereRadius;

        Debug.Log("SphereCenter: " + sphereCenter.position);
        Debug.Log("SphereRadius: " + sphereRadius);
        Debug.Log("Target: " + crawlTargetSphere);
    }
    #endregion Traversal Targets

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
                    animator.SetTrigger("EnterReposition");
                break;
            case AnimationType.WallCrawl:
                Animator_SetTrigger("WallCrawl");
                break;
            case AnimationType.CrawlJump:
                Animator_SetTrigger("CrawlJump");
                break;
            default:
                Animator_SetFloat("Speed", 0f);
                break;

        }
    }

    private void UpdateRotationAnimation(Quaternion previousRotation)
    {
        float angleDelta = Quaternion.Angle(previousRotation, transform.rotation);
        if (angleDelta < 0.2f)
        {
            Animator_SetFloat("TurnAmount", 0f);
            return;
        }

        float signedAngle = Vector3.SignedAngle(previousRotation * Vector3.forward, transform.forward, Vector3.up);
        float normalizedTurn = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
        Animator_SetFloat("TurnAmount", normalizedTurn);
    }

    #endregion Animation Control System

    #region Leap
    public void StartLunge(Vector3 targetPosition, float delayDuration, float arcDuration, float arcHeight)
    {
        hasCapturedArcStart = false;
        DisableProximity();
        isLunging = true;
        delayTimer = 0f;
        arcTimer = 0f;
        arcEndPosition = targetPosition;
        this.delayDuration = delayDuration;
        this.arcDuration = arcDuration;
        this.arcHeight = arcHeight;
        lungeDirection = (targetPosition - transform.position).normalized;
        RotationIntent(RotationPriority.State, targetPosition);
    }

    private void ExecuteLunge()
    {
        if (!isLunging) return;

        if (delayTimer < delayDuration)
        {
            //  isRunning = true;
            delayTimer += Time.deltaTime;
            //  transform.position += lungeDirection * currentSpeed * Time.deltaTime;
            return;
        }

        if (!hasCapturedArcStart)
        {
            arcStartPosition = transform.position;
            hasCapturedArcStart = true;
            //  isRunning = false;
        }

        arcTimer += Time.deltaTime;
        var t = Mathf.Clamp01(arcTimer / arcDuration);
        Vector3 horizontal = Vector3.Lerp(arcStartPosition, arcEndPosition, t);
        var height = arcHeight * 4 * t * (1 - t);
        horizontal.y += height;
        transform.position = horizontal;
        if (arcTimer >= arcDuration)
        {
            RotationIntent(RotationPriority.State, transform.position + lungeDirection);
            isLunging = false;
            arcTimer = 0f;
            delayTimer = 0f;
            // EnableProximity();
        }
    }
    #endregion Leap

    #region Boolean Setter Functions
    public void SetIsSphere(bool isSphere)
    {
        //this.isSphere = isSphere;
    }

    public bool IsMounting()
    {
        return IsCurrentTraversalPhase(TraversalPhase.Mounting);
    }

    public bool IsCurrentTraversalPhase(TraversalPhase traversalPhase)
    {
        return currentTraversalPhase == traversalPhase;
    }

    public bool IsCurrentTraversalContext(TraversalContext traversalContext)
    {
        return currentTraversalContext == traversalContext;
    }

    public bool isCurrentGeometryIdentity(MovementSurface geometryIdentity)
    {
        return currentGeometryIdentity == geometryIdentity;
    }

    public void SetCurrentMovementSurface(MovementSurface surface)
    {
        currentMovementSurface = surface;
    }
    public void SetDetectedGeometry(MovementSurface detectedGeometry)
    {
        this.detectedGeometry = detectedGeometry;
    }

    public bool IsCurrentSurface(MovementSurface movementSurface)
    {
        return currentMovementSurface == movementSurface;
    }

    public bool IsDetectedGeometry(MovementSurface detectedGeometry)
    {
        return this.detectedGeometry == detectedGeometry;
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
    #endregion Boolean Setter Functions

    #region GizmosDebug
    private void OnDrawGizmosSelected()
    {
        // 🔵 Raw target (random point on wall plane)
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(debugRawTarget, 0.12f);

        // 🟡 Raycast origin (offset outward from wall)
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(debugRayOrigin, 0.12f);

        // 🔴 Ray toward wall
        Gizmos.color = Color.red;
        Gizmos.DrawLine(debugRayOrigin, debugHitPoint);

        // 🟢 Final wall hit point
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(debugHitPoint, 0.15f);

        // 🟣 Final crawl target
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(crawlTarget, 0.15f);

        Gizmos.color = debugSurfaceCheckHit ? Color.pink : Color.hotPink;

        Gizmos.DrawLine(
            debugSurfaceCheckOrigin,
            debugSurfaceCheckOrigin + debugSurfaceCheckDirection
        );

        Gizmos.DrawSphere(debugSurfaceCheckOrigin, 0.1f);
    }

    private void OnDrawGizmos()
    {
        // EYE POSITION
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(debugEyePosition, 0.08f);

        // PROBE ORIGIN
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(debugProbeOrigin, 0.1f);

        // RAY
        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            debugProbeOrigin,
            debugRayDirection * 5f
        );

        // HIT VISUALIZATION
        if (debugDidHit)
        {
            // HIT POINT
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(debugHitPoint, 0.12f);

            // HIT NORMAL
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(
                debugHitPoint,
                debugHitNormal * 2f
            );
        }
    }
    #endregion GizmosDebug
}