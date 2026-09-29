using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ground-based movement: NavMesh pathing + ground-plane rotation.
/// Owns only ground-algorithm state (path, corner index, repath timer, last movement dir).
/// Everything shared (speed, rotation intent, config) is read back through the owner.
/// </summary>
/// 
/// 13th Aug 2026 Refactored Enemy Movement into Ground & Arbitrary Surface Specific Logic.
public class GroundMovement : IMovementLogic
{
    private readonly EnemyMovement owner;
    private readonly Transform transform;
    private readonly NavMeshAgent agent;
    private readonly AvoidanceSteering avoidance;

    private readonly NavMeshPath currentPath = new NavMeshPath();
    private int currentCornerIndex;
    private float repathTimer;

    public Vector3 LastMovementDir { get; private set; }

    public GroundMovement(EnemyMovement owner, Transform transform, NavMeshAgent agent, AvoidanceSteering avoidance)
    {
        this.owner = owner;
        this.transform = transform;
        this.agent = agent;
        this.avoidance = avoidance;
    }

    public void MoveTo(Vector3 destination)
    {
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);

        if (!agent.enabled)
            return;

        if (!agent.isOnNavMesh)
        {
            bool recovered = TryRecoverToNavMesh();

            Debug.Log($"OFF NAVMESH | Position: {transform.position} | Recovery: {recovered} | OnNavMesh: {agent.isOnNavMesh}");

            if (!recovered)
                return;
        }

        repathTimer -= Time.deltaTime;

        if (!TryGetReachableGroundTarget(destination, out destination))
            return;

        if (repathTimer <= 0f)
        {
            if (agent.CalculatePath(destination, currentPath))
            {
                if (currentPath.status == NavMeshPathStatus.PathComplete)
                {
                    currentCornerIndex = 0;
                }
            }
            repathTimer = owner.RepathInterval;
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

        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        Vector3 normalizedDirection = direction.normalized;

        if (avoidance != null)
        {
            avoidance.SetRotationPermission(owner.CanRotate);
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

        if (movementDir.sqrMagnitude > 0.0001f)
        {
            LastMovementDir = movementDir.normalized;
        }

        movementDir = movementDir.normalized;

        transform.position += movementDir * owner.CurrentSpeed * Time.deltaTime;
        // agent.Warp(transform.position);

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit groundHit, 1f, NavMesh.AllAreas))
        {
            transform.position = groundHit.position;
        }

        Debug.DrawRay(transform.position, movementDir * 2f, Color.cyan);
    }

    public void Rotate()
    {
        Quaternion beforeRotation = transform.rotation;

        if (avoidance != null && avoidance.IsAvoiding())
        {
            if (LastMovementDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(LastMovementDir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    owner.RotationSpeed * Time.deltaTime
                );
            }
            UpdateRotationAnimation(beforeRotation);
            return;
        }

        if (owner.CurrentRotationPriority != EnemyMovement.RotationPriority.None)
        {
            RotateTowardsIntent();
            UpdateRotationAnimation(beforeRotation);
            return;
        }

        if (LastMovementDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(LastMovementDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, owner.RotationSpeed * Time.deltaTime);
        }

        UpdateRotationAnimation(beforeRotation);
        owner.ClearRotationIntent();
    }

    private bool RotateTowardsIntent()
    {
        if (!owner.CanRotate)
        {
            Debug.Log("Rotation blocked by canRotate");
            return false;
        }

        if (owner.CurrentRotationPriority == EnemyMovement.RotationPriority.None)
        {
            return false;
        }

        Vector3 direction = owner.CurrentRotationTargetPosition - transform.position;
        direction.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, owner.RotationSpeed * Time.deltaTime);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        bool rotationComplete = angle <= owner.RotationThreshold;

        // NOTE: preserved as-is from the original — rotationComplete is computed but the
        // function always returns false, same as before the split. Flagging in case that
        // was meant to gate ClearRotationIntent() and got lost somewhere along the way.
        return false;
    }

    private void UpdateRotationAnimation(Quaternion previousRotation)
    {
        float angleDelta = Quaternion.Angle(previousRotation, transform.rotation);
        if (angleDelta < 0.2f)
        {
            owner.Animator_SetFloat("TurnAmount", 0f);
            return;
        }

        float signedAngle = Vector3.SignedAngle(previousRotation * Vector3.forward, transform.forward, Vector3.up);
        float normalizedTurn = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
        owner.Animator_SetFloat("TurnAmount", normalizedTurn);
    }

    private bool TryRecoverToNavMesh()
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (!NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            10f,
            filter))
        {
            return false;
        }

        agent.Warp(hit.position);

        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            transform.position = hit.position;
            agent.enabled = true;
        }

        return agent.isOnNavMesh;
    }

    private bool TryGetReachableGroundTarget(Vector3 target, out Vector3 reachableTarget)
    {
        reachableTarget = transform.position;

        Vector3 searchPosition = target;
        searchPosition.y = transform.position.y;

        if (!NavMesh.SamplePosition(searchPosition, out NavMeshHit hit, 20f, agent.areaMask))
            return false;

        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        reachableTarget = hit.position;
        return true;
    }
}
