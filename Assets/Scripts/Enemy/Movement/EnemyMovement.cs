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
        Random,
        SurfaceUp,
        SurfaceDown,
        SurfaceLeft,
        SurfaceRight
    }

    #endregion State Enums

    #region Core Movement States

    [SerializeField] private CrawlIntent crawlIntent;
    private TraversalContext currentTraversalContext;
    private TraversalPhase currentTraversalPhase;
    private MovementSurface currentMovementSurface;
    public MovementSurface CurrentMovementSurface => currentMovementSurface;
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

    private SurfaceInfo currentSurface;
    private SurfaceInfo detectedSurface;
    private Vector3 targetPosition;

    private Quaternion targetRotation;
    private SurfaceCrawlAbility surfaceCrawlAbility;

    private Vector3 crawlTarget;
    private Vector3 crawlTargetSphere;

    private bool isChangingSurface;
    private bool isTransitionLocked;
    [SerializeField] private bool isTransitionAllowed;

    private Vector3 currentSphereNormal;
    private Vector3 currentSurfaceNormal;
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

    [SerializeField] private LayerMask TraversableSurfaceMask;


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
            AlignToSurface();
            return;
        }

        UpdateSurfaceNormal();
        Debug.Log("currentSurfaceNormal : " + currentSurfaceNormal);
        ResetAnimationIntent();
        ResolveSpeed();
        //  Debug.Log("BEFORE ROT: " + transform.rotation.eulerAngles);
        ApplyRotation(); //  Debug.Log("AFTER ROT: " + transform.rotation.eulerAngles);
        ExecuteLunge();
        Debug.Log("CURRENT MOVEMENT SURFACE === " + currentMovementSurface);
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
            default:
                HandleSurfaceTraversal(destination);
                break;
                // case MovementSurface.Wall:
                // case MovementSurface.Ceiling:
                //     HandleSurfaceTraversal(destination);
                //break;
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
    private void ProbeNewSurface()
    {
        if (isTransitionLocked)
        {
            return;
        }

        if (!isTransitionAllowed)
        {
            return;
        }

        Debug.Log("DETECT NEW SURFACE CALLED");

        Vector3 eyePosition = transform.position + transform.forward * offsetForNewSurface;

        debugEyePosition = eyePosition;

        Vector3 probeOrigin = eyePosition + (-transform.up * sphereCenterOffsetFromEye);

        debugProbeOrigin = probeOrigin;

        Vector3 rayDirection = -transform.forward;

        debugRayDirection = rayDirection;

        debugDidHit = false;

        if (Physics.Raycast(probeOrigin, rayDirection, out RaycastHit hit, 5f, surfaceCrawlAbility.TraversableSurfaceMask))
        {
            debugDidHit = true;
            isChangingSurface = true;
            debugHitPoint = hit.point;
            debugHitNormal = hit.normal;

            float dotProduct = Vector3.Dot(currentSurface.surfaceHitNormal, hit.normal);

            Debug.Log($"DOT : {dotProduct}");

            if (dotProduct < surfaceTransitionThreshold)
            {
                isTransitionLocked = true;
                RequestAnimation(new AnimationIntent(AnimationType.CrawlJump, 300));

                SurfaceInfo newSurface;
                newSurface.surfaceHitNormal = hit.normal;
                newSurface.surfaceHitPoint = hit.point;
                newSurface.hitCollider = hit.collider;
                newSurface.surfaceTag = hit.collider.tag;

                Debug.Log("reached Probe function");
                SetDetectedGeometry(DetermineMovementSurface(hit));

                ComputeSurfaceAlignment(newSurface);
                Debug.Log("THIS SHIT IS CAUSING THE ISSUE");
                isChangingSurface = false;
                PickNewCrawlTarget(numberOfAttempts, surfaceCrawlAbility.roamDistance);
                Invoke(nameof(UnlockTransition), unlockAfterTime);
                return;
            }
        }

        else
        {
            PickNewCrawlTarget(numberOfAttempts, surfaceCrawlAbility.roamDistance * 0.5f);
        }
    }
    private Vector3 GetProjectableVector(Vector3 surfaceNormal)
    {
        /*This function calculates which world axis is pointing into the current surface that the enemy AI is On ,
        it returns the opposite of that world axis which can be used to project onto the new plane for perfect / correct body alignment on the new surface 
        after transition*/

        if (IsCurrentTraversalContext(TraversalContext.Inside))
        {
            Vector3 projectable = surfaceNormal;  //That's it 

            Debug.Log($"PROJECTABLE VECTOR : {projectable}");

            return projectable;
        }

        if (IsCurrentTraversalContext(TraversalContext.Outside))
        {
            Vector3 projectable = -surfaceNormal;
            return projectable;
        }

        return Vector3.down;

        /* 30th May 2026 With Heavy Heart I had to comment this section out because it is a bit too lengthy solution
         to find a projectable vector for new surface , Now we simply use surface Normals which are also defined
         using vector3.xyz 🥲. 
         */
        //  {//jkhk} //
        //{//} //  Vector3[] worldAxes =
        //     {
        //     Vector3.right,
        //     Vector3.left,
        //     Vector3.forward,
        //     Vector3.back,
        //     Vector3.up,
        //     Vector3.down
        // };
        // Vector3 inwardAxis = Vector3.right;

        // float lowestDot = Mathf.Infinity;

        // foreach (var axis in worldAxes)
        // {
        //     float dot = Vector3.Dot(surfaceNormal, axis);

        //     Debug.Log($"Axis : {axis} | Dot : {dot}");

        //     // MOST NEGATIVE DOT
        //     // axis points MOST INTO wall
        //     if (dot < lowestDot)
        //     {
        //         lowestDot = dot;
        //         inwardAxis = axis;
        //     }
        // }

        // Debug.Log($"INWARD AXIS : {inwardAxis}");

        // Return opposite of inward axis
        // Vector3 projectable = -inwardAxis;

    }

    private void UpdateSurfaceNormal()
    {
        var origin = transform.position + transform.up * 1f;
        var direction = -transform.up;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 3f, TraversableSurfaceMask))
        {
            currentSurfaceNormal = hit.normal;
            Debug.Log("This is the current Surface normal" + currentSurfaceNormal);
        }
    }
    #endregion Surface Detection & Traversal Context

    #region Surface Alignment & Orientation

    private void ComputeSurfaceAlignment(SurfaceInfo detectedSurface)
    {
        Vector3 alignmentVector = GetProjectableVector(currentSurface.surfaceHitNormal);

        // storedWallPoint = surfacePoint;
        // storedWallNormal = surfaceNormal;
        // currentSurface.wallHitPoint = surfacePoint;
        // currentSurface.wallHitNormal = surfaceNormal;
        // currentSurface.SurfaceTag = "xyz";

        currentSurface = detectedSurface;
        var forward = Vector3.ProjectOnPlane(alignmentVector, currentSurface.surfaceHitNormal).normalized;
        targetRotation = Quaternion.LookRotation(forward, currentSurface.surfaceHitNormal);
        Debug.Log("wall to wall trnastion");
        // targetRotation *= Quaternion.Euler(0, -90f, 90f); //Very Important
        targetPosition = currentSurface.surfaceHitPoint + currentSurface.surfaceHitNormal * surfaceCrawlAbility.surfaceOffset;
        Debug.Log("TARGET POSITION = " + targetPosition + "TARGET ROTATION = " + targetRotation.eulerAngles);

        //isMounting = true;
        currentTraversalPhase = TraversalPhase.Mounting;
        agent.enabled = false;
        Debug.Log(currentSurface.surfaceHitPoint);
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
        /* 24th May 2026 THIS is place where this FUCKASS BUG ate my whole week !!  Bug: on transition to ceiling the zombie would flip onto the outer surface of the ceiling 
        FIX: the flip was happening due to the fallback because when we set Current Movement surface to ceiling there was no ceiling related gate so it fellback on Ground rotation!!!!!
        FUCK YOU!!!!!!!!I solved it !! HUHUHUHUAHAHHAHAHA 😤🤣😈*/
        if (IsCurrentMovememntSurface(MovementSurface.Wall) || IsCurrentMovememntSurface(MovementSurface.Ceiling))
        {
            RotationOnWall();
            return;
        }

        if (IsCurrentMovememntSurface(MovementSurface.GenericSurface))
        {
            // WallRotation();
            return;
        }

        if (IsCurrentMovememntSurface(MovementSurface.Ground))
        {
            GroundRotation();
        }
        // normal ground

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
        Debug.Log("Agent status : " + agent.enabled);
        Debug.Log(currentMovementSurface + " from ground movement fucntiin and context is " + currentTraversalContext);
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

    private void HandleSurfaceTraversal(Vector3 destination)
    {
        if (!IsCurrentTraversalPhase(TraversalPhase.Traversing))
        {
            return;
        }

        Debug.Log(
    $"SURFACE={currentMovementSurface} | CONTEXT={currentTraversalContext}"
      );

        Vector3 directionToCrawlTarget = crawlTarget - transform.position;
        CheckSurfaceContinuity();
        EvaluateNewSurfaceTransition(directionToCrawlTarget);
        UpdateCrawlTarget(directionToCrawlTarget);
        ExecuteSurfaceTraversal(directionToCrawlTarget);
    }

    private void CheckSurfaceContinuity()
    {
        var origin = transform.position + transform.forward * eyeOffset + currentSurfaceNormal * 1f;
        debugSurfaceCheckOrigin = origin;
        debugSurfaceCheckDirection = -currentSurfaceNormal * 3f;


        debugSurfaceCheckHit = Physics.Raycast(origin, -currentSurfaceNormal, 3f, surfaceCrawlAbility.TraversableSurfaceMask);

        if (!debugSurfaceCheckHit && isTransitionAllowed)
        {
            Debug.Log("The point is unreachable because the wall is not continuous.");
            //PickNewCrawlTarget(numberOfAttempts, wallCrawlAbility.roamDistance);
            //crawlTarget = transform.position;

            Debug.Log("we aint moving");
            //crawlTarget = Vector3.zero;
            UpdateTraversalContext(false);
            ProbeNewSurface();
        }

    }

    private void EvaluateNewSurfaceTransition(Vector3 directionToCrawlTarget)
    {
        if (!isTransitionAllowed)
        {
            return;
        }

        if (!IsCurrentTraversalPhase(TraversalPhase.Transitioning))
        {
            //29th may 2026 
            // To Disable surface transition and let the crawler stay on the current surface we will disable forward raycast that checks surface ahead for transition//
            /*This Function is esentially for checking walls for TraversalContext.Inside ,
            To Apply the same Transition Gate for TraversalContext.Outside we must gate Probing
            inside DetectNewSurface() Function as well.*/

            var startPoint = transform.position + transform.up * frontWallDetectionDistance;
            if (Physics.Raycast(startPoint, directionToCrawlTarget.normalized, out RaycastHit hit, 5f, surfaceCrawlAbility.TraversableSurfaceMask))
            {
                detectedSurface.surfaceHitNormal = hit.normal;
                detectedSurface.surfaceHitPoint = hit.point;
                detectedSurface.hitCollider = hit.collider;
                detectedSurface.surfaceTag = hit.collider.tag;
                Debug.Log("Reacheddd");
                SetDetectedGeometry(DetermineMovementSurface(hit));
                //isTransitioningSurface = true;
                currentTraversalPhase = TraversalPhase.Transitioning;
                RequestAnimation(new AnimationIntent(AnimationType.CrawlJump, 300));
                UpdateTraversalContext(true);
                ComputeSurfaceAlignment(detectedSurface);
            }
        }
    }

    private MovementSurface DetermineMovementSurface(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Sphere"))
        {
            return MovementSurface.GenericSurface;
        }
        else
        {
            float upVectorDot = Vector3.Dot(hit.normal, Vector3.up);
            if (upVectorDot > 0.7f)
            {
                Debug.Log("grounndddddd");
                return MovementSurface.Ground;
            }
            else if (upVectorDot < -0.7f)
            {
                Debug.Log("ceilinggggggggg");
                return MovementSurface.Ceiling;
            }
            else
            {
                Debug.Log("wallllllllllllll");
                return MovementSurface.Wall;
            }
        }


    }
    private void StickToSurface()
    {
        Vector3 origin = transform.position + currentSurfaceNormal;

        if (Physics.Raycast(origin, -currentSurfaceNormal, out RaycastHit hit, 3f, surfaceCrawlAbility.TraversableSurfaceMask))
        {
            transform.position = hit.point + hit.normal * surfaceCrawlAbility.surfaceOffset;
        }
    }

    private void UpdateCrawlTarget(Vector3 directionToCrawlTarget)
    {
        if (directionToCrawlTarget.magnitude < 0.2f)
        {
            PickNewCrawlTarget(numberOfAttempts, surfaceCrawlAbility.roamDistance);
            return;
        }
    }

    private void ExecuteSurfaceTraversal(Vector3 directionToCrawlTarget)
    {
        directionToCrawlTarget.Normalize();

        if (!isChangingSurface)
        {
            Debug.Log("we're moving again");
            transform.position += directionToCrawlTarget * surfaceCrawlAbility.crawlSpeed * Time.deltaTime;
            Vector3 forward = Vector3.ProjectOnPlane(directionToCrawlTarget, currentSurfaceNormal).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(forward, currentSurfaceNormal);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                surfaceCrawlAbility.turnSpeed * Time.deltaTime
            );

            StickToSurface();
        }
    }

    #endregion Traversal Movement

    #region Getters
    public Vector3 GetMovementDirection()
    {
        return lastMovementDir;
    }

    private Vector3 GetCrawlDirection(CrawlIntent crawlIntent)
    {
        switch (crawlIntent)
        {
            case CrawlIntent.None:
                return transform.position;
            case CrawlIntent.SurfaceUp:
                return GetSurfaceUpDirection();
            case CrawlIntent.SurfaceDown:
                return GetSurfaceDownDirection();
            case CrawlIntent.SurfaceLeft:
                return GetSurfaceLeftDirection();
            case CrawlIntent.SurfaceRight:
                return GetSurfaceRightDirection();
            case CrawlIntent.Random:
            default:
                return GetRandomDirection();

        }
    }

    private Vector3 GetSurfaceLeftDirection()
    {
        return -GetSurfaceRightDirection();
    }
    private Vector3 GetSurfaceRightDirection()
    {
        Vector3 surfaceUpDir = Vector3.ProjectOnPlane(Vector3.up, currentSurfaceNormal).normalized;
        Vector3 surfaceRight = Vector3.Cross(currentSurfaceNormal, surfaceUpDir).normalized;
        return surfaceRight;
    }
    private Vector3 GetSurfaceDownDirection()
    {
        Vector3 surfaceDownDir = Vector3.ProjectOnPlane(Vector3.down, currentSurfaceNormal).normalized;
        return surfaceDownDir;
    }
    private Vector3 GetSurfaceUpDirection()
    {
        Debug.Log("SURFACE UP CALLED");
        // Debug.Log("Is on vertical surface , wall normal is " + currentSurface.wallHitNormal);
        Vector3 surfaceUpDir = Vector3.ProjectOnPlane(Vector3.up, currentSurfaceNormal).normalized;
        return surfaceUpDir;
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
        randomDir = Vector3.ProjectOnPlane(randomDir, currentSurfaceNormal).normalized;
        return randomDir;
    }

    #endregion

    #region Surface Mounting & Transitions

    public void BeginSurfaceMount(SurfaceInfo surfaceInfo, SurfaceCrawlAbility surfaceCrawlAbility, Vector3 projectionVector)
    {
        if (IsCurrentTraversalPhase(TraversalPhase.Mounting))
        {
            return;
        }

        Debug.Log(currentTraversalPhase);
        this.surfaceCrawlAbility = surfaceCrawlAbility;
        currentSurface = surfaceInfo;

        Vector3 normal = surfaceInfo.surfaceHitNormal;
        Vector3 forward = Vector3.ProjectOnPlane(projectionVector, normal).normalized;

        if (forward.sqrMagnitude < 0.001f)
        {
            Debug.Log("FALLBACKKKKKKKKKKKKKKKKKKKEIE");
            // forward = Vector3.Cross(transform.right, currentSurfaceNormal).normalized;
            forward = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        }

        targetRotation = Quaternion.LookRotation(forward, normal);
        float offsetDirection = IsCurrentTraversalContext(TraversalContext.Inside) ? 1f : -1f;
        targetPosition = surfaceInfo.surfaceHitPoint + normal * surfaceCrawlAbility.surfaceOffset * offsetDirection;
        currentTraversalPhase = TraversalPhase.Mounting;
        agent.enabled = false;

    }

    private void AlignToSurface()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            surfaceCrawlAbility.rotationSpeed * Time.deltaTime
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            surfaceCrawlAbility.positionSpeed * Time.deltaTime
        );

        float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);
        float positionDifference = Vector3.Distance(transform.position, targetPosition);

        if (rotationDifference < 20f && positionDifference < 0.05f)
        {
            transform.rotation = targetRotation;
            transform.position = targetPosition;
            InitializeTraversal();
        }
    }

    private void InitializeTraversal()
    {
        currentTraversalPhase = TraversalPhase.Traversing;
        Debug.Log($"CURRENT = {currentMovementSurface} | DETECTED = {detectedGeometry} | INTENT = {crawlIntent}");
        SetCurrentMovementSurface(detectedGeometry);
        Debug.Log($"CURRENT = {currentMovementSurface}");

        if (IsDetectedGeometry(MovementSurface.Wall) || IsDetectedGeometry(MovementSurface.GenericSurface))
        {
            currentSurfaceNormal = currentSurface.surfaceHitNormal;
            Debug.Log("Forced Normal : " + currentSurfaceNormal);
            SetCrawlIntent(CrawlIntent.Random);
            Debug.Log("Init Target Normal: " + currentSurfaceNormal);
            PickNewCrawlTarget(numberOfAttempts, surfaceCrawlAbility.roamDistance);
        }
        else if (IsDetectedGeometry(MovementSurface.Ceiling) || IsDetectedGeometry(MovementSurface.Ground))
        {
            currentSurfaceNormal = currentSurface.surfaceHitNormal;
            SetCrawlIntent(CrawlIntent.Random);
            PickNewCrawlTarget(numberOfAttempts, surfaceCrawlAbility.roamDistance);
            if (IsCurrentMovememntSurface(MovementSurface.Ground))
            {
                agent.enabled = true;
            }
        }
        ResetAnimationIntent();
        RequestAnimation(new AnimationIntent(AnimationType.SurfaceCrawl, 70));
        //isCrawling = true;
        Debug.Log("Mount Complete :: Wall Mode");
    }
    private void UnlockTransition()
    {
        isTransitionLocked = false;
    }


    #endregion Surface Mounting & Transitions

    #region Traversal Targets

    private void PickNewCrawlTarget(int remainingAttempts, float currentRoamDistance)
    {

        if (remainingAttempts <= 0)
        {
            Debug.Log("NO TARGET FOUND!");
            //SWITCH SURFACE to ground
            return;
        }


        for (int i = 0; i < numberOfAttempts; i++)
        {
            Vector3 crawlDir = GetCrawlDirection(crawlIntent);
            Vector3 rawTarget = transform.position + crawlDir * currentRoamDistance;

            debugRawTarget = rawTarget;
            Debug.Log("Normal used" + currentSurfaceNormal);
            Vector3 origin = rawTarget + currentSurfaceNormal * 1f; // adding wall normal to do safe raycast check
            debugRayOrigin = origin;

            if (Physics.Raycast(origin, -currentSurfaceNormal, out RaycastHit hit, 3f, surfaceCrawlAbility.TraversableSurfaceMask))
            {
                crawlTarget = hit.point + hit.normal * surfaceCrawlAbility.surfaceOffset;
                debugHitPoint = crawlTarget;
                return;
            }
        }

        float shrunkRoamDistance = currentRoamDistance * 0.5f;
        Debug.Log("new roam distaance" + shrunkRoamDistance);
        // Debug.Log("Retrying with smaller roamDistance");

        PickNewCrawlTarget(remainingAttempts - 1, shrunkRoamDistance);
    }

    // private void PickNewCrawlTarget_Wall(int remainingAttempts, float currentRoamDistance)
    // {
    //     if (remainingAttempts <= 0)
    //     {
    //         Debug.Log("NO TARGET FOUND!");
    //         //SWITCH SURFACE to ground
    //         return;
    //     }

    //     bool foundCrawlTarget = false;

    //     for (int i = 0; i < numberOfAttempts; i++)
    //     {
    //         Vector3 crawlDir = GetCrawlDirection(crawlIntent);
    //         Vector3 rawTarget = transform.position + crawlDir * currentRoamDistance;

    //         debugRawTarget = rawTarget;

    //         Vector3 origin = rawTarget + currentSurface.surfaceHitNormal * 1f; // adding wall normal to do safe raycast check
    //         debugRayOrigin = origin;

    //         if (Physics.Raycast(origin, -currentSurface.surfaceHitNormal, out RaycastHit hit, 3f , surfaceCrawlAbility.TraversableSurfaceMask))
    //         {
    //             crawlTarget = hit.point + currentSurface.surfaceHitNormal * surfaceCrawlAbility.surfaceOffset;
    //             debugHitPoint = crawlTarget;
    //             foundCrawlTarget = true;
    //             break;
    //         }
    //     }

    //     if (foundCrawlTarget)
    //     {
    //         return;
    //     }

    //     float shrunkRoamDistance = currentRoamDistance * 0.5f;
    //     Debug.Log("new roam distaance" + shrunkRoamDistance);
    //     // Debug.Log("Retrying with smaller roamDistance");

    //     PickNewCrawlTarget_Wall(remainingAttempts - 1, shrunkRoamDistance);
    // }

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
            case AnimationType.SurfaceCrawl:
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

    private bool IsHorizontalSurface()
    {
        float dot = Mathf.Abs(Vector3.Dot(currentSurface.surfaceHitNormal, Vector3.up));
        return dot > 0.9f;
    }

    private bool IsCurrentSurfaceTag(string surfaceTag)
    {
        return currentSurface.surfaceTag == surfaceTag;
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

    public void SetCrawlIntent(CrawlIntent crawlIntent)
    {
        this.crawlIntent = crawlIntent;
        PickNewCrawlTarget(numberOfAttempts, surfaceCrawlAbility.roamDistance);
    }

    public bool IsCurrentMovememntSurface(MovementSurface movementSurface)
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

    public void SetSurfaceTransitionAllowed(bool _isTransitionAllowed)
    {
        isTransitionAllowed = _isTransitionAllowed;
    }

    public void DisableNavmeshAgent()
    {
        agent.enabled = false;
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