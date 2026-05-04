//using Mono.Cecil.Cil;
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
    [Header("Tunable Parameters")]
    [SerializeField] private float enemySpeed;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationThreshold = 2f;
    [SerializeField] private float separationWeight = 0.5f;
    [SerializeField] private LayerMask WallSurfaceMask;
    [SerializeField] private LayerMask CeilingSurfaceMask;
    public float MovementSpeed => enemySpeed;

    private ProximitySensor proximitySensor;
    private bool canRotate;
    private AvoidanceSteering avoidance;
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
        Ceiling
    }

    public enum RotationPriority
    {
        None,
        State,
        Proximity,
        Vision
    }


    private MovementSurface currentMovementSurface;
    private MovementMode currentMovementMode;
    private RotationPriority currentRotationPriority;
    private Vector3 currentRotationTargetPosition;
    private Vector3 lastMovementDir;
    private float currentSpeed;
    private Animator animator;
    public AnimationIntent currentAnimationIntent;

    [Header("NavMesh Data")]
    private NavMeshAgent agent;
    private NavMeshPath currentPath;
    private int currentCornerIndex;
    private float repathTimer;
    [SerializeField] private float repathInterval = 0.5f;
    [SerializeField] private float acceleration = 10f;

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
    private AnimationIntent lastAppliedAnimationIntent;
    private bool allowSurfaceTraversal;
    private bool wallDetected;
    private bool ceilingDetected;
    private RaycastHit lastHitWall;
    private Vector3 currentWallNormal;

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
        currentMovementSurface = MovementSurface.Ground;
    }

    private void Update()
    {
        ResetAnimationIntent();
        ResolveSpeed();
        ApplyRotation();
        ExecuteLunge();
        Debug.Log("[FINAL ROTATION] " + transform.rotation.eulerAngles);
        Debug.Log("[SURFACE] Current: " + currentMovementSurface);
        // Debug.Log("[EnemyMovement] Enemy Speed is " + enemySpeed);
        currentSpeed = Mathf.MoveTowards(currentSpeed, enemySpeed, acceleration * Time.deltaTime);
        agent.nextPosition = transform.position;
    }

    public void MoveTo(Vector3 destination)
    {
        switch (currentMovementSurface)
        {
            case MovementSurface.Ground:
                HandleGroundMovement(destination);
                break;
            case MovementSurface.Wall:
                HandleWallMovement(destination);
                break;
        }

    }

    private void HandleWallMovement(Vector3 destination)
    {

        Vector3 direction = destination - transform.position;
        Vector3 movementDir = direction.normalized;
        Debug.Log("[WallMovement] movementDir: " + movementDir);
        Debug.Log("[WallMovement] currentWallNormal: " + currentWallNormal);
        Debug.DrawRay(transform.position, currentWallNormal * 50f, Color.yellow);   // wall normal
        Debug.DrawRay(transform.position + Vector3.up * 3f, movementDir * 50f, Color.whiteSmoke);       // movement
        movementDir = Vector3.ProjectOnPlane(movementDir, currentWallNormal).normalized;

        if (movementDir.sqrMagnitude > 0.0001f)
        {
            lastMovementDir = movementDir;
        }

        transform.position += movementDir * currentSpeed * Time.deltaTime;
        Debug.DrawRay(transform.position, movementDir, Color.black);
    }

    private void HandleGroundMovement(Vector3 destination)
    {
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);

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

    public bool IsAvoiding()
    {
        return avoidance.IsAvoiding();
    }
    public void SetRotationPermission(bool allowRotate)
    {
        canRotate = allowRotate;
    }

    public void SetMovementMode(MovementMode mode)
    {
        currentMovementMode = mode;
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
    public void Stop()
    {
        enemySpeed = 0f;
    }

    private void ApplyRotation()
    {
        //        Debug.Log($"ApplyRotation - Current Priority: {currentRotationPriority}");
        if (!canRotate)
        {
            Debug.Log("[Rotation] BLOCKED: canRotate = false");
            return;
        }

        if (IsCurrentSurface(MovementSurface.Wall))
        {
            Debug.Log("[Rotation] Using WALL rotation");
            WallRotation();
            return;
        }
        Debug.Log("[Rotation] Using GROUND rotation");
        GroundRotation();
    }

    private void WallRotation()
    {
        // if (lastMovementDir.sqrMagnitude < 0.0001f)
        // {
        //     Debug.Log("[WallRotation] SKIPPED: lastMovementDir too small");
        //     return;
        // }
        // Debug.Log("[WallRotation] Running");

        // Vector3 forward = Vector3.ProjectOnPlane(lastMovementDir, currentWallNormal).normalized;
        // Vector3 up = currentWallNormal;
        // Vector3 right = Vector3.Cross(up, forward).normalized;
        // forward = Vector3.Cross(right, up).normalized;

        // Debug.Log("[WallRotation] FIXED Forward: " + forward);
        // Debug.Log("[WallRotation] FIXED Up: " + up);

        // Quaternion targetRotation = Quaternion.LookRotation(forward, up);
        // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (currentWallNormal == Vector3.zero)
            return;

        // Step 1: align UP to wall normal
        Quaternion alignUp = Quaternion.FromToRotation(transform.up, currentWallNormal);

        transform.rotation = alignUp * transform.rotation;

        // Step 2: OPTIONAL - align forward along movement
        if (lastMovementDir.sqrMagnitude > 0.001f)
        {
            Vector3 forward = Vector3.ProjectOnPlane(lastMovementDir, currentWallNormal).normalized;

            Quaternion look = Quaternion.LookRotation(forward, currentWallNormal);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                look,
                rotationSpeed * Time.deltaTime
            );
        }
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

    public void ClearRotationIntent()
    {
        currentRotationPriority = RotationPriority.None;
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
    public Vector3 GetMovementDirection()
    {
        return lastMovementDir;
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

    public void SetWallNormal(Vector3 wallNormal)
    {
        currentWallNormal = wallNormal;
    }

    // public void DetectWall()
    // {
    //     if (!allowSurfaceTraversal)
    //     {
    //         return;
    //     }
    //     var origin = transform.position;
    //     var direction = transform.forward;
    //     var maxDistance = 10f;
    //     var surfaceMask = WallSurfaceMask;
    //     RaycastHit hitSurface;
    //     Debug.DrawRay(origin, direction * maxDistance, Color.pink);

    //     if (Physics.Raycast(origin, direction, out hitSurface, maxDistance, surfaceMask))
    //     {
    //         lastHitWall = hitSurface;
    //         wallDetected = true;
    //     }
    // }

    // public void DetectCeiling()
    // {
    //     if (!allowSurfaceTraversal)
    //     {
    //         return;
    //     }
    //     var origin = transform.position;
    //     var direction = Vector3.up;
    //     var maxDistance = 2f;
    //     var surfaceMask = CeilingSurfaceMask;

    //     if (Physics.Raycast(origin, direction, maxDistance, surfaceMask))
    //     {
    //         ceilingDetected = true;
    //     }
    // }

    public void SetCurrentMovementSurface(MovementSurface surface)
    {
        currentMovementSurface = surface;
    }

    public void SwitchSurface(MovementSurface newSurface)
    {
        Debug.Log("[EnemyMovement] Switching Surface to " + newSurface);
        SetCurrentMovementSurface(newSurface);

        if (newSurface == MovementSurface.Wall)
        {
            OrientToWall(currentWallNormal);
        }
    }

    private void OrientToWall(Vector3 wallNormal)
    {
        Vector3 outward = -wallNormal;
        Vector3 surfaceUp = Vector3.ProjectOnPlane(Vector3.forward, wallNormal).normalized;
        Quaternion targetBodyRotation = Quaternion.LookRotation(outward, surfaceUp);
        transform.rotation = targetBodyRotation;
    }
    public bool IsWallDetected()
    {
        return wallDetected;
    }

    public bool IsCeilingDetected()
    {
        return ceilingDetected;
    }

    public bool IsCurrentSurface(MovementSurface movementSurface)
    {
        return currentMovementSurface == movementSurface;
    }

    public void SetSurfaceTraversalAllowed(bool isCrawler)
    {
        allowSurfaceTraversal = isCrawler;
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

}