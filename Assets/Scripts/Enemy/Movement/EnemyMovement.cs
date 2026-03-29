using Mono.Cecil.Cil;
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

    public enum RotationPriority
    {
        None,
        State,
        Proximity,
        Vision
    }


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
    private bool isRunning;
    private bool hasCapturedArcStart;
    private float delayTimer;
    private float arcTimer;
    private float delayDuration;
    private float arcDuration;
    private float arcHeight;
    private Vector3 lungeDirection;
    private Vector3 arcStartPosition;
    private Vector3 arcEndPosition;

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

    private void Update()
    {
        ResetAnimationIntent();
        ResolveSpeed();
        ApplyRotation();
        ExecuteLunge();
        currentSpeed = Mathf.MoveTowards(currentSpeed, enemySpeed, acceleration * Time.deltaTime);
        agent.nextPosition = transform.position;
    }

    public void MoveTo(Vector3 destination)
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

        // transform.position = Vector3.MoveTowards(transform.position, transform.position + movementDir, enemySpeed * Time.deltaTime);
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
                enemySpeed = 20f;
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
            return;
        }

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
            return;
        }


        if (currentRotationPriority != RotationPriority.None)
        {
            RotateTowardsIntent();
            return;
        }

        if (lastMovementDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lastMovementDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

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
                animator.SetBool("EnterReposition", true);
                break;

            default:
                Animator_SetFloat("Speed", 0f);
                break;

        }
    }

    public void StartLunge(Vector3 targetPosition, float delayDuration, float arcDuration, float arcHeight)
    {
        hasCapturedArcStart = false;
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
            isRunning = true;
            delayTimer += Time.deltaTime;
            //  transform.position += lungeDirection * currentSpeed * Time.deltaTime;
            return;
        }

        if (!hasCapturedArcStart)
        {
            arcStartPosition = transform.position;
            hasCapturedArcStart = true;
            isRunning = false;
        }

        arcTimer += Time.deltaTime;
        var t = Mathf.Clamp01(arcTimer / arcDuration);
        Vector3 horizontal = Vector3.Lerp(arcStartPosition, arcEndPosition, t);
        var height = arcHeight * 4 * t * (1 - t);
        horizontal.y += height;
        transform.position = horizontal;
        if (arcTimer >= arcDuration)
        {
            isLunging = false;
            arcTimer = 0f;
            delayTimer = 0f;
        }
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