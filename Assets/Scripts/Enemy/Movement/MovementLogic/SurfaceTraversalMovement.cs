using UnityEngine;

/// <summary>
/// Surface-traversal movement: wall/ceiling/generic-surface crawling, surface transitions,
/// mounting/alignment, and crawl-target selection.
/// Owns only surface-algorithm state. Shared data (currentSurfaceNormal, config values,
/// component toggles) is read/written back through the owner.
/// </summary>
/// 
/// 13th Aug 2026 Refactored Enemy Movement into Ground & Arbitrary Surface Specific Logic
public class SurfaceTraversalMovement : IMovementLogic
{
    private readonly EnemyMovement owner;
    private readonly Transform transform;

    // ---- Traversal state ----
    private EnemyMovement.CrawlIntent crawlIntent;
    private EnemyMovement.TraversalContext currentTraversalContext;
    private EnemyMovement.TraversalPhase currentTraversalPhase;
    private EnemyMovement.MovementSurface detectedGeometry;
    private EnemyMovement.MovementSurface currentGeometryIdentity;

    public EnemyMovement.MovementSurface DetectedGeometry => detectedGeometry;
    public EnemyMovement.TraversalPhase CurrentPhase => currentTraversalPhase;
    public EnemyMovement.TraversalContext CurrentContext => currentTraversalContext;

    // ---- Surface / target data ----
    public SurfaceCrawlAbility SurfaceCrawlAbility { get; private set; }
    private SurfaceInfo currentSurface;
    private SurfaceInfo detectedSurface;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 crawlTarget;
    private Vector3 crawlTargetSphere;
    private Vector3 desiredSurfaceDirection;

    private bool isChangingSurface;
    private bool isTransitionLocked;
    private bool isTransitionAllowed;
    private float sphereRadius;

    // ---- Edge detection ----
    private bool wasOnEdge;
    private float edgeResetTimer;

    // ---- Debug gizmo data (drawn via DrawGizmos/DrawGizmosSelected, called from EnemyMovement) ----
    private Vector3 debugEyePosition;
    private Vector3 debugProbeOrigin;
    private Vector3 debugRayDirection;
    private Vector3 debugHitPoint;
    private Vector3 debugHitNormal;
    private bool debugDidHit;
    private Vector3 debugSurfaceCheckOrigin;
    private Vector3 debugSurfaceCheckDirection;
    private bool debugSurfaceCheckEdge;
    private Vector3 debugRawTarget;
    private Vector3 debugRayOrigin;

    // initialCrawlIntent / initialTransitionAllowed seed from EnemyMovement's [SerializeField]
    // inspector values (crawlIntent / isTransitionAllowed) — those stay declared on EnemyMovement
    // since they're author-configured, but the *runtime* copies live here from this point on.
    public SurfaceTraversalMovement(EnemyMovement owner, Transform transform,
        EnemyMovement.CrawlIntent initialCrawlIntent, bool initialTransitionAllowed)
    {
        this.owner = owner;
        this.transform = transform;
        crawlIntent = initialCrawlIntent;
        isTransitionAllowed = initialTransitionAllowed;
    }

    #region Movement API

    public void MoveTo(Vector3 destination)
    {
        if (!IsCurrentPhase(EnemyMovement.TraversalPhase.Traversing))
        {
            return;
        }

        Vector3 directionToCrawlTarget = crawlTarget - transform.position;

        EvaluateNewSurfaceTransition(directionToCrawlTarget);
        CheckSurfaceContinuity();
        UpdateCrawlTarget(directionToCrawlTarget);
        ExecuteSurfaceTraversal(directionToCrawlTarget);
    }

    // Wall/ceiling rotation RotationOnWall() stub
    public void Rotate()
    {
    }

    #endregion

    #region Edge Timer

    public void TickEdgeTimer()
    {
        if (!wasOnEdge)
        {
            return;
        }

        edgeResetTimer -= Time.deltaTime;

        if (edgeResetTimer <= 0f)
        {
            wasOnEdge = false;
        }
    }

    #endregion

    #region Surface Detection & Traversal Context

    private void UpdateTraversalContext(bool wallAhead)
    {
        currentTraversalContext = wallAhead ? EnemyMovement.TraversalContext.Inside : EnemyMovement.TraversalContext.Outside;
    }

    private void ProbeNewSurface()
    {
        if (isTransitionLocked)
        {
            return;
        }

        Vector3 eyePosition = transform.position + transform.forward * owner.OffsetForNewSurface;
        debugEyePosition = eyePosition;

        Vector3 probeOrigin = eyePosition + (-transform.up * owner.SphereCenterOffsetFromEye);
        debugProbeOrigin = probeOrigin;

        Vector3 rayDirection = -transform.forward;
        debugRayDirection = rayDirection;

        debugDidHit = false;

        if (Physics.Raycast(probeOrigin, rayDirection, out RaycastHit hit, 5f, SurfaceCrawlAbility.TraversableSurfaceMask))
        {
            debugDidHit = true;
            isChangingSurface = true;
            debugHitPoint = hit.point;
            debugHitNormal = hit.normal;
            var targetSurface = DetermineMovementSurface(hit);

            if (!CanTransitionTo(targetSurface))
            {
                Debug.Log($"Transition blocked : {targetSurface}");
                return;
            }

            float dotProduct = Vector3.Dot(currentSurface.surfaceHitNormal, hit.normal);
            Debug.Log($"DOT : {dotProduct}");

            if (dotProduct < owner.SurfaceTransitionThreshold)
            {
                isTransitionLocked = true;
                owner.RequestAnimation(new AnimationIntent(AnimationType.CrawlJump, 300));

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
                PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance);
                owner.InvokeAfter(owner.UnlockAfterTime, UnlockTransition);
                return;
            }
        }
        else
        {
            PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance * 0.5f);
        }
    }

    private Vector3 GetProjectableVector(Vector3 surfaceNormal)
    {
        /*This function calculates which world axis is pointing into the current surface that the enemy AI is On ,
        it returns the opposite of that world axis which can be used to project onto the new plane for perfect / correct body alignment on the new surface 
        after transition*/
        if (IsCurrentContext(EnemyMovement.TraversalContext.Inside))
        {
            return surfaceNormal;
        }

        if (IsCurrentContext(EnemyMovement.TraversalContext.Outside))
        {
            return -surfaceNormal;
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
        //
        // Vector3 inwardAxis = Vector3.right;
        //
        // float lowestDot = Mathf.Infinity;
        //
        // foreach (var axis in worldAxes)
        // {
        //     float dot = Vector3.Dot(surfaceNormal, axis);
        //
        //     Debug.Log($"Axis : {axis} | Dot : {dot}");
        //
        //     // MOST NEGATIVE DOT
        //     // axis points MOST INTO wall
        //     if (dot < lowestDot)
        //     {
        //         lowestDot = dot;
        //         inwardAxis = axis;
        //     }
        // }
        //
        // Debug.Log($"INWARD AXIS : {inwardAxis}");
        //
        // // Return opposite of inward axis
        // Vector3 projectable = -inwardAxis;
    }

    #endregion

    #region Surface Alignment & Orientation

    private void ComputeSurfaceAlignment(SurfaceInfo newSurface)
    {
        Vector3 alignmentVector = GetProjectableVector(currentSurface.surfaceHitNormal);

        currentSurface = newSurface;
        var forward = Vector3.ProjectOnPlane(alignmentVector, currentSurface.surfaceHitNormal).normalized;
        targetRotation = Quaternion.LookRotation(forward, currentSurface.surfaceHitNormal);
        targetPosition = currentSurface.surfaceHitPoint + currentSurface.surfaceHitNormal * SurfaceCrawlAbility.surfaceOffset;

        currentTraversalPhase = EnemyMovement.TraversalPhase.Mounting;
        owner.SetAgentEnabled(false);
    }

    #endregion

    #region Traversal Movement

    private void CheckSurfaceContinuity()
    {
        var origin = transform.position + transform.forward * owner.EyeOffset + owner.currentSurfaceNormal * 1f;
        debugSurfaceCheckOrigin = origin;
        debugSurfaceCheckDirection = -owner.currentSurfaceNormal * 3f;

        debugSurfaceCheckEdge = Physics.Raycast(origin, -owner.currentSurfaceNormal, 3f, SurfaceCrawlAbility.TraversableSurfaceMask);

        if (!debugSurfaceCheckEdge)
        {
            if (!wasOnEdge)
            {
                wasOnEdge = true;
                edgeResetTimer = owner.EdgeResetDelay;
                owner.NotifyEdgeDetected();
            }

            if (isTransitionAllowed)
            {
                UpdateTraversalContext(false);
                ProbeNewSurface();
            }
        }
        else
        {
            wasOnEdge = false;
        }
    }

    private void EvaluateNewSurfaceTransition(Vector3 directionToCrawlTarget)
    {
        if (IsCurrentPhase(EnemyMovement.TraversalPhase.Transitioning))
        {
            return;
        }
        //29th may 2026 
        // To Disable surface transition and let the crawler stay on the current surface we will disable forward raycast that checks surface ahead for transition//
        /*This Function is esentially for checking walls for TraversalContext.Inside ,
        To Apply the same Transition Gate for TraversalContext.Outside we must gate Probing
        inside DetectNewSurface() Function as well.*/

        var startPoint = transform.position + transform.up * owner.FrontWallDetectionDistance;
        if (Physics.Raycast(startPoint, directionToCrawlTarget.normalized, out RaycastHit hit, 5f, SurfaceCrawlAbility.TraversableSurfaceMask))
        {
            var targetSurface = DetermineMovementSurface(hit);
            if (!CanTransitionTo(targetSurface))
            {
                if (targetSurface == EnemyMovement.MovementSurface.Ground)
                {
                    PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance);
                }
                return;
            }

            detectedSurface.surfaceHitNormal = hit.normal;
            detectedSurface.surfaceHitPoint = hit.point;
            detectedSurface.hitCollider = hit.collider;
            detectedSurface.surfaceTag = hit.collider.tag;

            SetDetectedGeometry(DetermineMovementSurface(hit));
            currentTraversalPhase = EnemyMovement.TraversalPhase.Transitioning;
            owner.RequestAnimation(new AnimationIntent(AnimationType.CrawlJump, 300));
            UpdateTraversalContext(true);
            ComputeSurfaceAlignment(detectedSurface);
        }
    }

    private EnemyMovement.MovementSurface DetermineMovementSurface(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Sphere"))
        {
            return EnemyMovement.MovementSurface.GenericSurface;
        }

        float upVectorDot = Vector3.Dot(hit.normal, Vector3.up);
        if (upVectorDot > 0.7f)
        {
            return EnemyMovement.MovementSurface.Ground;
        }
        else if (upVectorDot < -0.7f)
        {
            return EnemyMovement.MovementSurface.Ceiling;
        }
        else
        {
            return EnemyMovement.MovementSurface.Wall;
        }
    }

    private void StickToSurface()
    {
        Vector3 origin = transform.position + owner.currentSurfaceNormal;

        if (Physics.Raycast(origin, -owner.currentSurfaceNormal, out RaycastHit hit, 3f, SurfaceCrawlAbility.TraversableSurfaceMask))
        {
            transform.position = hit.point + hit.normal * SurfaceCrawlAbility.surfaceOffset;
        }
    }

    private void UpdateCrawlTarget(Vector3 directionToCrawlTarget)
    {
        if (directionToCrawlTarget.magnitude < 0.2f)
        {
            PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance);
        }
    }

    private void ExecuteSurfaceTraversal(Vector3 directionToCrawlTarget)
    {
        directionToCrawlTarget.Normalize();

        if (!isChangingSurface)
        {
            transform.position += directionToCrawlTarget * SurfaceCrawlAbility.crawlSpeed * Time.deltaTime;
            Vector3 forward = Vector3.ProjectOnPlane(directionToCrawlTarget, owner.currentSurfaceNormal).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(forward, owner.currentSurfaceNormal);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                SurfaceCrawlAbility.turnSpeed * Time.deltaTime
            );

            StickToSurface();
        }
    }

    #endregion

    #region Direction Helpers

    private Vector3 GetCrawlDirection(EnemyMovement.CrawlIntent intent)
    {
        switch (intent)
        {
            case EnemyMovement.CrawlIntent.None:
                return transform.position;
            case EnemyMovement.CrawlIntent.SurfaceUp:
                return GetSurfaceUpDirection();
            case EnemyMovement.CrawlIntent.SurfaceDown:
                return GetSurfaceDownDirection();
            case EnemyMovement.CrawlIntent.SurfaceLeft:
                return GetSurfaceLeftDirection();
            case EnemyMovement.CrawlIntent.SurfaceRight:
                return GetSurfaceRightDirection();
            case EnemyMovement.CrawlIntent.DesiredDirection:
                return desiredSurfaceDirection;
            case EnemyMovement.CrawlIntent.Random:
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
        Vector3 surfaceUpDir = Vector3.ProjectOnPlane(Vector3.up, owner.currentSurfaceNormal).normalized;
        Vector3 surfaceRight = Vector3.Cross(owner.currentSurfaceNormal, surfaceUpDir).normalized;
        return surfaceRight;
    }

    private Vector3 GetSurfaceDownDirection()
    {
        return Vector3.ProjectOnPlane(Vector3.down, owner.currentSurfaceNormal).normalized;
    }

    private Vector3 GetSurfaceUpDirection()
    {
        return Vector3.ProjectOnPlane(Vector3.up, owner.currentSurfaceNormal).normalized;
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
        randomDir = Vector3.ProjectOnPlane(randomDir, owner.currentSurfaceNormal).normalized;
        return randomDir;
    }

    public EnemyMovement.CrawlIntent GetContinuationDirection(Vector3 toPlayer)
    {
        float leftDot = Vector3.Dot(toPlayer.normalized, GetSurfaceLeftDirection());
        float rightDot = Vector3.Dot(toPlayer.normalized, GetSurfaceRightDirection());
        Debug.Log($"LeftDot : {leftDot}");
        Debug.Log($"RightDot : {rightDot}");
        return leftDot > rightDot ? EnemyMovement.CrawlIntent.SurfaceLeft : EnemyMovement.CrawlIntent.SurfaceRight;
    }

    public void SetDesiredDirection(Vector3 destination)
    {
        desiredSurfaceDirection = destination;
    }

    #endregion

    #region Mounting & Transitions

    public void BeginSurfaceMount(SurfaceInfo surfaceInfo, SurfaceCrawlAbility surfaceCrawlAbility, Vector3 projectionVector)
    {
        if (IsCurrentPhase(EnemyMovement.TraversalPhase.Mounting))
        {
            return;
        }

        SurfaceCrawlAbility = surfaceCrawlAbility;
        currentSurface = surfaceInfo;

        Vector3 normal = surfaceInfo.surfaceHitNormal;
        Vector3 forward = Vector3.ProjectOnPlane(projectionVector, normal).normalized;

        if (forward.sqrMagnitude < 0.001f)
        {
            Debug.Log("FALLBACKKKKKKKKKKKKKKKKKKKEIE");
            forward = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        }

        targetRotation = Quaternion.LookRotation(forward, normal);
        float offsetDirection = IsCurrentContext(EnemyMovement.TraversalContext.Inside) ? 1f : -1f;
        targetPosition = surfaceInfo.surfaceHitPoint + normal * surfaceCrawlAbility.surfaceOffset * offsetDirection;
        currentTraversalPhase = EnemyMovement.TraversalPhase.Mounting;
        owner.SetAgentEnabled(false);
    }

    public void AlignToSurface()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            SurfaceCrawlAbility.rotationSpeed * Time.deltaTime
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            SurfaceCrawlAbility.positionSpeed * Time.deltaTime
        );

        float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);
        float positionDifference = Vector3.Distance(transform.position, targetPosition);

        if (rotationDifference < 20f && positionDifference < 0.05f)
        {
            transform.rotation = targetRotation;
            transform.position = targetPosition;
            owner.NotifyTransitionComplete();
            InitializeTraversal();
        }
    }

    private void InitializeTraversal()
    {
        currentTraversalPhase = EnemyMovement.TraversalPhase.Traversing;
        owner.SetCurrentMovementSurface(detectedGeometry);

        if (IsDetectedGeometry(EnemyMovement.MovementSurface.Wall) || IsDetectedGeometry(EnemyMovement.MovementSurface.GenericSurface))
        {
            owner.SetCurrentSurfaceNormal(currentSurface.surfaceHitNormal);
            SetCrawlIntent(EnemyMovement.CrawlIntent.Random);
            PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance);
        }
        else if (IsDetectedGeometry(EnemyMovement.MovementSurface.Ceiling) || IsDetectedGeometry(EnemyMovement.MovementSurface.Ground))
        {
            owner.SetCurrentSurfaceNormal(currentSurface.surfaceHitNormal);
            SetCrawlIntent(EnemyMovement.CrawlIntent.Random);
            PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance);

            if (owner.IsCurrentMovememntSurface(EnemyMovement.MovementSurface.Ground))
            {
                owner.SetAgentEnabled(true);
            }
        }

        owner.ResetAnimationIntent();
        owner.RequestAnimation(new AnimationIntent(AnimationType.SurfaceCrawl, 70));
    }

    private void UnlockTransition()
    {
        isTransitionLocked = false;
    }

    public void InvokeUnlockTransition(float delay)
    {
        owner.InvokeAfter(delay, UnlockTransition);
    }

    #endregion

    #region Traversal Targets

    private void PickNewCrawlTarget(int remainingAttempts, float currentRoamDistance)
    {
        if (remainingAttempts <= 0)
        {
            return;
        }

        for (int i = 0; i < owner.NumberOfAttempts; i++)
        {
            Vector3 crawlDir = GetCrawlDirection(crawlIntent);
            Vector3 rawTarget = transform.position + crawlDir * currentRoamDistance;
            debugRawTarget = rawTarget;

            Vector3 origin = rawTarget + owner.currentSurfaceNormal * 1f;
            debugRayOrigin = origin;

            if (Physics.Raycast(origin, -owner.currentSurfaceNormal, out RaycastHit hit, 3f, SurfaceCrawlAbility.TraversableSurfaceMask))
            {
                crawlTarget = hit.point + hit.normal * SurfaceCrawlAbility.surfaceOffset;
                debugHitPoint = crawlTarget;
                return;
            }
        }

        float shrunkRoamDistance = currentRoamDistance * 0.5f;
        PickNewCrawlTarget(remainingAttempts - 1, shrunkRoamDistance);
    }

    private void PickNewCrawlTarget_Sphere()
    {
        Vector3 randomSurfaceDirection = UnityEngine.Random.onUnitSphere;

        crawlTargetSphere = owner.SphereCenter.position + randomSurfaceDirection * sphereRadius;

        Debug.Log("SphereCenter: " + owner.SphereCenter.position);
        Debug.Log("SphereRadius: " + sphereRadius);
        Debug.Log("Target: " + crawlTargetSphere);
    }

    #endregion

    #region Boolean / State API

    public void ResetCrawlIntent()
    {
        crawlIntent = EnemyMovement.CrawlIntent.None;
    }

    public bool IsMounting() => IsCurrentPhase(EnemyMovement.TraversalPhase.Mounting);

    public bool IsCurrentPhase(EnemyMovement.TraversalPhase phase)
    {
        return currentTraversalPhase == phase;
    }

    public bool IsCurrentContext(EnemyMovement.TraversalContext context)
    {
        return currentTraversalContext == context;
    }

    public bool IsCurrentGeometryIdentity(EnemyMovement.MovementSurface geometryIdentity)
    {
        return currentGeometryIdentity == geometryIdentity;
    }

    public void SetDetectedGeometry(EnemyMovement.MovementSurface geometry)
    {
        detectedGeometry = geometry;
    }

    public void SetCrawlIntent(EnemyMovement.CrawlIntent intent)
    {
        crawlIntent = intent;
        PickNewCrawlTarget(owner.NumberOfAttempts, SurfaceCrawlAbility.roamDistance);
    }

    public bool IsDetectedGeometry(EnemyMovement.MovementSurface geometry)
    {
        return detectedGeometry == geometry;
    }

    public void SetTransitionAllowed(bool allowed)
    {
        isTransitionAllowed = allowed;
    }

    public void SetPhase(EnemyMovement.TraversalPhase phase)
    {
        currentTraversalPhase = phase;
    }

    public bool CanTransitionTo(EnemyMovement.MovementSurface targetSurface)
    {
        if (owner.IsCurrentMovememntSurface(EnemyMovement.MovementSurface.Wall) && targetSurface == EnemyMovement.MovementSurface.Wall)
        {
            return true;
        }

        return isTransitionAllowed;
    }

    #endregion

    #region Gizmos

    public void DrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(debugRawTarget, 0.12f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(debugRayOrigin, 0.12f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(debugRayOrigin, debugHitPoint);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(debugHitPoint, 0.15f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(crawlTarget, 0.15f);

        Gizmos.color = debugSurfaceCheckEdge ? Color.pink : Color.hotPink;
        Gizmos.DrawLine(debugSurfaceCheckOrigin, debugSurfaceCheckOrigin + debugSurfaceCheckDirection);
        Gizmos.DrawSphere(debugSurfaceCheckOrigin, 0.1f);
    }

    public void DrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(debugEyePosition, 0.08f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(debugProbeOrigin, 0.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(debugProbeOrigin, debugRayDirection * 5f);

        if (debugDidHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(debugHitPoint, 0.12f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(debugHitPoint, debugHitNormal * 2f);
        }
    }

    #endregion
}