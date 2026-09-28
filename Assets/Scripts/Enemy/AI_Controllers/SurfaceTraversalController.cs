using UnityEngine;

public class SurfaceTraversalController
{
    public enum Goal
    {
        None,
        Top,
        Middle,
        Bottom,
        FreeMove,
        ReachPlayer,
        ResolveProjectionCollapse
    }

    private readonly EnemyBrain brain;
    private readonly EnemyMovement enemyMovement;
    private SurfaceCrawlAbility surfaceCrawlAbility;

    private SurfaceInfo currentSurfaceInfo;

    private Goal goal = Goal.Middle;
    private Goal activeGoal = Goal.Middle;
    private Goal overrideGoal=  Goal.None;

    private EnemyMovement.CrawlIntent continuationDirection = EnemyMovement.CrawlIntent.SurfaceRight;
    private Vector3 lastValidCrawlDirection;

    private bool isWaitingToMount;
    private float mountTimer;

    private float progressTimer;
    private float previousDistanceToPlayer;
    private float progressCheckTimer;
    private int stagnantChecks;
    private int increasingDistanceChecks;

    private Goal lastEdgeHandledGoal;

    public Goal CurrentGoal => goal;
    public Goal ActiveGoal => activeGoal;
    public Goal OverrideGoal => overrideGoal;
    public SurfaceInfo CurrentSurfaceInfo => currentSurfaceInfo;

    public SurfaceTraversalController(EnemyBrain brain, EnemyMovement enemyMovement)
    {
        this.brain = brain;
        this.enemyMovement = enemyMovement;
        surfaceCrawlAbility = brain.SurfaceCrawlAbility;
    }

    public void Tick()
    {
        ExecutePreferredSurfaceIntent();
    }

    public void InitializeInvestigate()
    {
        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall))
        {
            if (surfaceCrawlAbility != null)
            {
                goal = Goal.None;
                overrideGoal = Goal.FreeMove;
            }
        }
    }

    public void InitializeSearch()
    {
        if (IsInMiddle())
        {
            if (surfaceCrawlAbility != null)
            {
                goal = Goal.None;
                overrideGoal = Goal.FreeMove;
            }
        }
    }

    public void InitializeChase()
    {
        if (surfaceCrawlAbility != null)
        {
            goal = Goal.ReachPlayer;
            overrideGoal = Goal.ReachPlayer;
        }
    }

    public void InitializeWander()
    {
        if (surfaceCrawlAbility != null)
        {
            goal = Goal.None;
            overrideGoal = Goal.FreeMove;
        }
    }

    public void InitializePursuitMonitoring()
    {
        previousDistanceToPlayer = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        stagnantChecks = 0;
        increasingDistanceChecks = 0;
        progressCheckTimer = 0f;
    }

    public void HandleTransitionComplete()
    {
        if (overrideGoal == Goal.ResolveProjectionCollapse)
        {
            overrideGoal = Goal.None;
        }
    }

    public void HandleEdgeDetected()
    {
        if (activeGoal == lastEdgeHandledGoal)
        {
            return;
        }

        lastEdgeHandledGoal = activeGoal;

        switch (activeGoal)
        {
            case Goal.ReachPlayer:
            case Goal.ResolveProjectionCollapse:
                Debug.Log("TRYINGGGGGGGGGG");

                if (IsAtTop())
                {
                    overrideGoal = Goal.Bottom;
                }
                else
                {
                    overrideGoal = UnityEngine.Random.value < 0.7f ? Goal.Top : Goal.Bottom;
                }

                break;

            case Goal.Top:
                overrideGoal = Goal.Bottom;
                break;
        }
    }

    private void ExecutePreferredSurfaceIntent()
    {
        if (activeGoal != lastEdgeHandledGoal)
        {
            lastEdgeHandledGoal = Goal.None;
        }

        if (overrideGoal != Goal.None)
        {
            if (HasReachedGoal(overrideGoal))
            {
                overrideGoal = Goal.None;
            }
        }

        activeGoal = overrideGoal != Goal.None ? overrideGoal : goal;

        if (HasReachedGoal(activeGoal))
        {
            CompleteGoal();
            return;
        }

        switch (activeGoal)
        {
            case Goal.Top:
                enemyMovement.SetSurfaceTransitionAllowed(true);
                ExecuteCeilingGoal();
                break;

            case Goal.Bottom:
                enemyMovement.SetSurfaceTransitionAllowed(true);
                ExecuteGroundGoal();
                break;

            case Goal.Middle:
                enemyMovement.SetSurfaceTransitionAllowed(true);
                ExecuteWallGoal();
                break;

            case Goal.FreeMove:
                enemyMovement.SetSurfaceTransitionAllowed(true);
                ExecuteGenericSurfaceGoal();
                break;

            case Goal.None:
                enemyMovement.SetSurfaceTransitionAllowed(false);
                break;

            case Goal.ReachPlayer:
                enemyMovement.SetSurfaceTransitionAllowed(false);
                ExecuteReachPlayerGoal();
                break;

            case Goal.ResolveProjectionCollapse:
                ExecuteProjectionCollapseGoal();
                break;

            default:
                Debug.Log("Default FallBack");
                break;
        }
    }

    private void ExecuteGenericSurfaceGoal()
    {
        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.GenericSurface))
        {
            enemyMovement.SetCrawlIntent(EnemyMovement.CrawlIntent.Random);
            return;
        }
    }

    private void ExecuteCeilingGoal()
    {
        ChooseSurface();

        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall) ||
            enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.GenericSurface))
        {
            enemyMovement.SetCrawlIntent(EnemyMovement.CrawlIntent.SurfaceUp);
            return;
        }
    }

    private void ExecuteGroundGoal()
    {
        ChooseSurface();

        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall) ||
            enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.GenericSurface))
        {
            enemyMovement.SetCrawlIntent(EnemyMovement.CrawlIntent.SurfaceDown);
            return;
        }
    }

    private void ExecuteWallGoal()
    {
        ChooseSurface();

        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall))
        {
            enemyMovement.SetCrawlIntent(EnemyMovement.CrawlIntent.Random);
            return;
        }
    }

    private void ExecuteProjectionCollapseGoal()
    {
        ExecuteFallBackRoute();
    }

    private void ExecuteFallBackRoute()
    {
        Debug.Log("Fallback Route.");

        overrideGoal = UnityEngine.Random.value < 0.5f ? Goal.Top : Goal.Bottom;

        stagnantChecks = 0;
        increasingDistanceChecks = 0;
    }

    private void ExecuteReachPlayerGoal()
    {
        if (!IsOnWallOrCeiling())
        {
            return;
        }

        UpdatePursuitProgress();
        UpdateReachPlayerTimer();

        if (HasTimedOut())
        {
            Debug.Log("ReachPlayer timed Out No progress was made ");
            ExecuteFallBackRoute();
            progressTimer = 0f;
            return;
        }

        if (HasReachedPlayer() && !brain.IsInState(brain.AttackState))
        {
            Debug.Log("Near Player!!");
            brain.SwitchState(brain.AttackState);
            return;
        }

        if (SurfaceTraversal_IsMovingAwayFromPlayer())
        {
            Debug.Log("Wrong Route" + increasingDistanceChecks);
            ExecuteFallBackRoute();
            return;
        }

        if (SurfaceTraversal_HasMadeNoProgress())
        {
            Debug.Log("No Progress" + stagnantChecks);
            ExecuteFallBackRoute();
            return;
        }

        Vector3 toPlayer = brain.player.transform.position - brain.transform.position;
        Vector3 surfaceDirection = Vector3.ProjectOnPlane(toPlayer, enemyMovement.currentSurfaceNormal);

        if (ProjectionCollapsed(toPlayer, surfaceDirection))
        {
            ResolveCollapse();
            return;
        }

        ExecuteNormalPursuit(surfaceDirection);
    }

    private void UpdateReachPlayerTimer()
    {
        progressTimer += Time.deltaTime;
    }

    private void UpdatePursuitProgress()
    {
        progressCheckTimer += Time.deltaTime;

        if (progressCheckTimer < brain.ProgressCheckInterval)
        {
            return;
        }

        progressCheckTimer = 0f;

        float currentDistance = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        float distanceDelta = previousDistanceToPlayer - currentDistance;

        if (distanceDelta > brain.MinimumProgressDistance)
        {
            stagnantChecks = 0;
            increasingDistanceChecks = 0;
            Debug.Log("Progressing towards the player.");
        }
        else
        {
            stagnantChecks++;
            Debug.Log("No significant progress. Stagnant checks: " + stagnantChecks);
        }

        if (currentDistance > previousDistanceToPlayer)
        {
            increasingDistanceChecks++;
        }
        else
        {
            increasingDistanceChecks = 0;
        }

        previousDistanceToPlayer = currentDistance;
    }

    private void ResolveCollapse()
    {
        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ceiling))
        {
            Debug.Log("Not using Collapse resolver on ceiling");
            return;
        }

        Debug.Log("Projection direction Collapsed.");

        continuationDirection = enemyMovement.GetContinuationDirection(
            brain.PlayerMovement.GetLastPlayerMovementDirection());

        overrideGoal = Goal.ResolveProjectionCollapse;

        Debug.Log("received crawl intent :: " + continuationDirection);
    }

    private void ExecuteNormalPursuit(Vector3 surfaceDirection)
    {
        lastValidCrawlDirection = surfaceDirection.normalized;
        enemyMovement.SetDesiredSurfaceDirection(lastValidCrawlDirection);
        enemyMovement.SetCrawlIntent(EnemyMovement.CrawlIntent.DesiredDirection);
    }

    private void ChooseSurface()
    {
        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ceiling) ||
            enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ground))
        {
            EvaluateSurface();
            return;
        }
    }

    private bool TryNearbySurface(out SurfaceInfo surfaceHitInfo)
    {
        surfaceCrawlAbility = brain.SurfaceCrawlAbility;

        if (surfaceCrawlAbility == null)
        {
            surfaceHitInfo = default;
            return false;
        }

        float bestDistance = float.MaxValue;
        SurfaceInfo bestWall = default;

        Vector3[] directions =
        {
            brain.transform.forward,
            -brain.transform.forward,
            brain.transform.right,
            -brain.transform.right,
            (brain.transform.forward + brain.transform.right).normalized,
            (brain.transform.forward - brain.transform.right).normalized,
            (-brain.transform.forward + brain.transform.right).normalized,
            (-brain.transform.forward - brain.transform.right).normalized
        };

        foreach (var direction in directions)
        {
            Debug.DrawRay(
                brain.transform.position,
                direction * surfaceCrawlAbility.rayDistance,
                Color.cyan);

            Vector3 origin = brain.transform.position + brain.transform.up * surfaceCrawlAbility.headHeight;

            if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hitSurface,
                surfaceCrawlAbility.rayDistance,
                surfaceCrawlAbility.TraversableSurfaceMask,
                QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(hitSurface.normal, Vector3.down);

                if (angle > surfaceCrawlAbility.minTiltAngle &&
                    angle < surfaceCrawlAbility.maxTiltAngle)
                {
                    float distance = hitSurface.distance;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestWall.surfaceHitPoint = hitSurface.point;
                        bestWall.surfaceHitNormal = hitSurface.normal;
                        bestWall.hitCollider = hitSurface.collider;
                        bestWall.surfaceTag = hitSurface.collider.tag;

                        if (hitSurface.collider.CompareTag("Wall"))
                        {
                            enemyMovement.SetDetectedGeometry(EnemyMovement.MovementSurface.Wall);
                        }
                        else
                        {
                            enemyMovement.SetDetectedGeometry(EnemyMovement.MovementSurface.GenericSurface);
                        }

                        StoreSurfaceInfo(bestWall);
                    }
                }
            }
        }

        if (bestDistance < float.MaxValue)
        {
            surfaceHitInfo = bestWall;
            return true;
        }

        surfaceHitInfo = default;
        return false;
    }

    private bool TryDescendNearbySurface(out SurfaceInfo surfaceHitInfo)
    {
        surfaceHitInfo = default;

        surfaceCrawlAbility = brain.SurfaceCrawlAbility;

        if (surfaceCrawlAbility == null)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        SurfaceInfo bestWall = default;

        Vector3[] directions =
        {
            brain.transform.forward,
            -brain.transform.forward,
            brain.transform.right,
            -brain.transform.right,
            (brain.transform.forward + brain.transform.right).normalized,
            (brain.transform.forward - brain.transform.right).normalized,
            (-brain.transform.forward + brain.transform.right).normalized,
            (-brain.transform.forward - brain.transform.right).normalized
        };

        foreach (var direction in directions)
        {
            Vector3 edgePoint = brain.transform.position + direction * surfaceCrawlAbility.rayDistance;

            Debug.DrawRay(
                brain.transform.position,
                direction * surfaceCrawlAbility.rayDistance,
                Color.cyan);

            if (Physics.Raycast(
                edgePoint,
                Vector3.down,
                out RaycastHit groundHit,
                5f,
                surfaceCrawlAbility.TraversableSurfaceMask,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 probeOrigin = edgePoint + Vector3.down * 2f;

            Debug.DrawRay(edgePoint, Vector3.down * 2f, Color.yellow);

            if (Physics.Raycast(
                probeOrigin,
                -direction,
                out RaycastHit hitSurface,
                surfaceCrawlAbility.rayDistance,
                surfaceCrawlAbility.TraversableSurfaceMask,
                QueryTriggerInteraction.Ignore))
            {
                Debug.DrawLine(probeOrigin, hitSurface.point, Color.green);

                float angle = Vector3.Angle(hitSurface.normal, Vector3.up);

                if (angle > surfaceCrawlAbility.minTiltAngle &&
                    angle < surfaceCrawlAbility.maxTiltAngle)
                {
                    float distance = Vector3.Distance(brain.transform.position, hitSurface.point);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;

                        bestWall.surfaceHitPoint = hitSurface.point;
                        bestWall.surfaceHitNormal = hitSurface.normal;
                        bestWall.hitCollider = hitSurface.collider;
                        bestWall.surfaceTag = hitSurface.collider.tag;

                        if (hitSurface.collider.CompareTag("Wall"))
                        {
                            enemyMovement.SetDetectedGeometry(EnemyMovement.MovementSurface.Wall);
                        }
                        else
                        {
                            enemyMovement.SetDetectedGeometry(EnemyMovement.MovementSurface.GenericSurface);
                        }

                        StoreSurfaceInfo(bestWall);
                    }
                }
            }
        }

        if (bestDistance < float.MaxValue)
        {
            surfaceHitInfo = bestWall;

            Debug.DrawLine(
                brain.transform.position,
                bestWall.surfaceHitPoint,
                Color.magenta);

            return true;
        }

        return false;
    }

    private void EvaluateSurface()
    {
        if (goal == Goal.FreeMove)
        {
            return;
        }

        surfaceCrawlAbility = brain.SurfaceCrawlAbility;

        if (surfaceCrawlAbility == null)
        {
            return;
        }

        if (enemyMovement.IsMounting())
        {
            return;
        }

        if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall))
        {
            return;
        }

        if (TryNearbySurface(out SurfaceInfo surfaceInfo))
        {
            if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ceiling))
            {
                Vector3 toWall = surfaceInfo.surfaceHitPoint - brain.transform.position;
                Vector3 desiredDirection = Vector3.ProjectOnPlane(
                    toWall,
                    enemyMovement.currentSurfaceNormal).normalized;

                enemyMovement.SetDesiredSurfaceDirection(desiredDirection);
                enemyMovement.SetCrawlIntent(EnemyMovement.CrawlIntent.DesiredDirection);
                return;
            }

            InitializeMounting(surfaceCrawlAbility, surfaceInfo, Vector3.up);
        }
        else if (
            TryDescendNearbySurface(out SurfaceInfo surfaceHitInfo) &&
            enemyMovement.IsCurrentTraversalContext(EnemyMovement.TraversalContext.Outside) &&
            enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ground))
        {
            InitializeMounting(surfaceCrawlAbility, surfaceHitInfo, Vector3.down);
        }
    }

    private void InitializeMounting(
        SurfaceCrawlAbility surfaceCrawlAbility,
        SurfaceInfo surfaceInfo,
        Vector3 projectionVector)
    {
        float distanceToWall = Vector3.Distance(
            brain.transform.position,
            surfaceInfo.surfaceHitPoint);

        if (distanceToWall <= surfaceCrawlAbility.attachDistance)
        {
            if (enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall) ||
                enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.GenericSurface))
            {
                return;
            }

            if (!isWaitingToMount)
            {
                enemyMovement.RequestAnimation(
                    new AnimationIntent(AnimationType.CrawlJump, 200));

                enemyMovement.Stop();

                isWaitingToMount = true;
                mountTimer = 0f;

                return;
            }

            mountTimer += Time.deltaTime;

            if (mountTimer >= surfaceCrawlAbility.tweakTiming)
            {
                enemyMovement.BeginSurfaceMount(
                    surfaceInfo,
                    surfaceCrawlAbility,
                    projectionVector);

                isWaitingToMount = false;
            }

            return;
        }

        enemyMovement.RequestAnimation(
            new AnimationIntent(AnimationType.SurfaceCrawl, 58));

        enemyMovement.SetMovementMode(EnemyMovement.MovementMode.Chase);
        enemyMovement.MoveTo(surfaceInfo.surfaceHitPoint);
    }

    private void StoreSurfaceInfo(SurfaceInfo surfaceInfo)
    {
        currentSurfaceInfo = surfaceInfo;
    }

    private bool IsAtTop()
    {
        if (enemyMovement.IsCurrentTraversalContext(EnemyMovement.TraversalContext.Outside))
        {
            return enemyMovement.IsCurrentMovementSurface(
                EnemyMovement.MovementSurface.Ground);
        }

        return enemyMovement.IsCurrentMovementSurface(
            EnemyMovement.MovementSurface.Ceiling);
    }

    private bool IsInMiddle()
    {
        return !IsAtBottom() && !IsAtTop();
    }

    private bool IsAtBottom()
    {
        if (enemyMovement.IsCurrentTraversalContext(EnemyMovement.TraversalContext.None))
        {
            return enemyMovement.IsCurrentMovementSurface(
                EnemyMovement.MovementSurface.Ground);
        }

        return enemyMovement.IsCurrentTraversalContext(
            EnemyMovement.TraversalContext.Inside) &&
            enemyMovement.IsCurrentMovementSurface(
                EnemyMovement.MovementSurface.Ground);
    }

    private bool HasReachedGoal(Goal targetGoal)
    {
        switch (targetGoal)
        {
            case Goal.Top:
                return IsAtTop();

            case Goal.Bottom:
                return IsAtBottom();

            case Goal.Middle:
                return IsInMiddle();

            case Goal.FreeMove:
                return false;

            case Goal.None:
                return true;

            case Goal.ReachPlayer:
                return false;

            case Goal.ResolveProjectionCollapse:
                return false;

            default:
                Debug.Log(":Check your HasReachedGoal() Function. ERROR");
                return false;
        }
    }

    private void CompleteGoal()
    {
        goal = Goal.None;
        enemyMovement.SetSurfaceTransitionAllowed(false);
    }

    private bool ProjectionCollapsed(Vector3 toPlayer, Vector3 surfaceDirection)
    {
        bool projectionCollapsed = surfaceDirection.magnitude < brain.CollapseThreshold;
        bool playerStillFar = toPlayer.magnitude > brain.PlayerVicinityThreshold;
        return projectionCollapsed && playerStillFar;
    }

    private bool IsOnWallOrCeiling()
    {
        return enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Wall) ||
               enemyMovement.IsCurrentMovementSurface(EnemyMovement.MovementSurface.Ceiling);
    }

    private bool HasReachedPlayer()
    {
        float distanceToPlayer = Vector3.Distance(
            brain.transform.position,
            brain.player.transform.position);

        bool playerClose = distanceToPlayer <= brain.PlayerVicinityThreshold;

        if (!playerClose)
        {
            return false;
        }

        return HasClearance();
    }

    private bool HasClearance()
    {
        Vector3 directionToPlayer =
            brain.player.transform.position - brain.transform.position;

        float distance = directionToPlayer.magnitude;

        if (Physics.Raycast(
            brain.transform.position,
            directionToPlayer.normalized,
            out RaycastHit hit,
            distance,
            brain.ClearanceMask))
        {
            return false;
        }

        return true;
    }

    private bool SurfaceTraversal_HasMadeNoProgress()
    {
        return stagnantChecks >= 5;
    }

    private bool SurfaceTraversal_IsMovingAwayFromPlayer()
    {
        return increasingDistanceChecks >= 5;
    }

    private bool HasTimedOut()
    {
        return progressTimer >= brain.MaxReachPlayerDuration;
    }
}