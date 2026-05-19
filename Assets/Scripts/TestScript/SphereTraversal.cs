using UnityEngine;

public class SphereCrawler : MonoBehaviour
{
    [Header("Sphere")]
    public Transform sphereCenter;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float turnSpeed = 360f;

    [Header("Target")]
    public float targetDistance = 3f;
    public float targetReachDistance = 0.5f;

    [Header("Debug")]
    public bool drawDebug = true;

    private Vector3 crawlTarget;
    private Vector3 currentNormal;

    private float sphereRadius;

    // DEBUG
    private Vector3 debugMoveDir;
    private Vector3 debugProjectedDir;
    private Vector3 debugTarget;

    public float surfaceOffset = 1.2f;

    void Start()
    {
        // STORE INITIAL SPHERE RADIUS
        sphereRadius =
            Vector3.Distance(
                transform.position,
                sphereCenter.position
            )-surfaceOffset;

        PickNewTarget();
    }

    void Update()
    {
        Crawl();
    }

    void Crawl()
    {
        // CURRENT SPHERE NORMAL
        currentNormal =
            (transform.position - sphereCenter.position).normalized;

        // DIRECTION TO TARGET
        Vector3 moveDir =
            crawlTarget - transform.position;

        debugMoveDir = moveDir;

        // PICK NEW TARGET
        if (moveDir.magnitude < targetReachDistance)
        {
            PickNewTarget();
            return;
        }

        moveDir.Normalize();

        // KEEP MOVEMENT TANGENT TO SPHERE
        moveDir =
            Vector3.ProjectOnPlane(
                moveDir,
                currentNormal
            ).normalized;

        debugProjectedDir = moveDir;

        // SAFETY
        if (moveDir.sqrMagnitude < 0.001f)
        {
            PickNewTarget();
            return;
        }

        // MOVE TANGENTLY
        Vector3 newPosition =
            transform.position +
            moveDir *
            moveSpeed *
            Time.deltaTime;

        // REPROJECT TO SPHERE SURFACE
        Vector3 fromCenter =
            (newPosition - sphereCenter.position).normalized;

        transform.position =
            sphereCenter.position +
            fromCenter * sphereRadius;

        // UPDATE NORMAL AFTER MOVEMENT
        currentNormal =
            (transform.position - sphereCenter.position).normalized;

        // FACE MOVEMENT DIRECTION
        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDir,
                currentNormal
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    void PickNewTarget()
    {
        // RANDOM TANGENT DIRECTION
        Vector3 randomDir =
            Random.onUnitSphere;

        randomDir =
            Vector3.ProjectOnPlane(
                randomDir,
                currentNormal
            ).normalized;

        // MOVE ALONG TANGENT
        Vector3 rawTarget =
            transform.position +
            randomDir * targetDistance;

        // PROJECT TARGET BACK TO SPHERE
        Vector3 sphereDir =
            (rawTarget - sphereCenter.position).normalized;

        crawlTarget =
            sphereCenter.position +
            sphereDir * sphereRadius;

        debugTarget = crawlTarget;
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || sphereCenter == null)
        {
            return;
        }

        // NORMAL
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(
            transform.position,
            currentNormal * 2f
        );

        // FORWARD
        Gizmos.color = Color.green;
        Gizmos.DrawRay(
            transform.position,
            transform.forward * 2f
        );

        // RAW MOVE DIRECTION
        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            transform.position,
            debugMoveDir.normalized * 2f
        );

        // PROJECTED MOVE DIRECTION
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(
            transform.position,
            debugProjectedDir * 2f
        );

        // TARGET
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(
            debugTarget,
            0.2f
        );

        // LINE TO CENTER
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(
            transform.position,
            sphereCenter.position
        );
    }
}