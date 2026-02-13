using Mono.Cecil.Cil;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float enemySpeed = 10f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationThreshold = 2f;
    [SerializeField] private float separationWeight = 0.5f;

    private ProximitySensor proximitySensor;
    private bool canRotate;

    private AvoidanceSteering avoidance;
    public enum MovementMode
    {
        Idle,
        Investigate,
        Chase,
        Search
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


    void Awake()
    {
        proximitySensor = GetComponentInChildren<ProximitySensor>();
        avoidance = GetComponent<AvoidanceSteering>();
        canRotate = true;
    }

    private void Update()
    {
        ResolveSpeed();
        ApplyRotation();
    }

    public void MoveTo(Vector3 destination)
    {
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);

        Vector3 Direction = destination - transform.position;
        Direction.y = 0f;
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

        transform.position = Vector3.MoveTowards(transform.position, transform.position + movementDir, enemySpeed * Time.deltaTime);
        Debug.DrawRay(transform.position, movementDir * 2f, Color.cyan);

    }

    public bool RotateTowardsIntent()
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

        Debug.Log($"Intent Dir Magnitude: {direction.magnitude}");
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        bool rotationComplete = angle <= rotationThreshold;
        Debug.Log($"Angle to target: {Quaternion.Angle(transform.rotation, targetRotation)}");

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

            default:
                enemySpeed = 3f;
                break;

        }
    }


    public void RotationIntent(RotationPriority priority, Vector3 position)
    {
        Debug.Log($"Trying to set rotation intent: {priority}, Current: {currentRotationPriority}");

        if (priority < currentRotationPriority)
        {
            return;
        }

        currentRotationTargetPosition = position;
        currentRotationPriority = priority;
        Debug.Log($"Rotation intent set to {priority}");
    }
    public void Stop()
    {
        enemySpeed = 0f;
    }

    private void ApplyRotation()
    {
        Debug.Log($"ApplyRotation - Current Priority: {currentRotationPriority}");

        if (!canRotate)
        {
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
    }

    public void ClearRotationIntent()
    {
        currentRotationPriority = RotationPriority.None;
    }

}