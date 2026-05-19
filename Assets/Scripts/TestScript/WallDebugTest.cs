using UnityEngine;

public class WallMountTest : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Detection")]
    public float rayDistance = 2f;
    public float headHeight = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float turnSpeed = 5f;
    public float attachDistance = 0.8f;

    [Header("Mount")]
    public float rotationSpeed = 5f;
    public float positionSpeed = 5f;
    public float wallOffset = 0.3f;

    [Header("Timing")]
    public float tweakTiming = 0.12f; // 🔥 delay before mount starts

    private bool hasTarget = false;
    private bool isApproaching = false;
    private bool waitingForMount = false;

    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private Vector3 wallPoint;
    private Vector3 wallNormal;

    private float delayTimer = 0f;
    public float jumpForce = 0.5f;   // upward
    public float pushForce = 0.3f;   // forward
    public float crawlSpeed = 3f;
    public float roamDistance = 3f;

    private Vector3 crawlTarget;
    private bool isCrawling = false;

    void Update()
    {
        if (!hasTarget)
        {
            DetectWall();
        }
        else if (isApproaching)
        {
            ApproachWall();
        }
        else if (waitingForMount)
        {
            WaitBeforeMount();
        }
        else if (isCrawling)
        {
            CrawlAlongWall();
        }
        else
        {
            SmoothMount();
        }
    }
    void CrawlAlongWall()
    {
        Vector3 dir = (crawlTarget - transform.position);

        if (dir.magnitude < 0.2f)
        {
            StartCrawling(); // pick new point
            return;
        }

        dir.Normalize();

        // 🔥 move
        transform.position += dir * crawlSpeed * Time.deltaTime;

        // 🔥 rotate to face movement direction ALONG WALL
        Vector3 forward = Vector3.ProjectOnPlane(dir, wallNormal);

        Quaternion lookRot = Quaternion.LookRotation(forward, wallNormal);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            lookRot,
            turnSpeed * Time.deltaTime
        );
    }
    // 🔥 STEP 1 — Detect wall
    void DetectWall()
    {
        RaycastHit hit;

        Vector3 origin = transform.position + Vector3.up * headHeight;

        if (Physics.Raycast(origin, transform.forward, out hit, rayDistance))
        {
            Debug.DrawRay(origin, transform.forward * rayDistance, Color.blue);
            Debug.DrawRay(hit.point, hit.normal * 2f, Color.yellow);

            wallPoint = hit.point;
            wallNormal = hit.normal;

            hasTarget = true;
            isApproaching = true;
        }
        else
        {
            Debug.DrawRay(origin, transform.forward * rayDistance, Color.red);
        }
    }

    // 🔥 STEP 2 — Walk toward wall
    void ApproachWall()
    {
        Vector3 dir = (wallPoint - transform.position);
        dir.y = 0f;

        float distance = dir.magnitude;

        if (distance < attachDistance)
        {
            animator.SetTrigger("CrawlJump");
            Vector3 jumpDir = transform.up * jumpForce + transform.forward * pushForce;
            transform.position += jumpDir;

            delayTimer = 0f;
            waitingForMount = true;
            isApproaching = false;
            return;
        }

        dir.Normalize();

        Quaternion lookRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRot,
            turnSpeed * Time.deltaTime
        );

        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // 🔥 STEP 3 — Wait for animation to start
    void WaitBeforeMount()
    {
        delayTimer += Time.deltaTime;

        if (delayTimer > tweakTiming)
        {
            SetupMount();
            waitingForMount = false;
        }
    }

    // 🔥 STEP 4 — Compute mount target
    void SetupMount()
    {
        Vector3 forward = -wallNormal;

        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;

        if (up.sqrMagnitude < 0.001f)
        {
            up = Vector3.ProjectOnPlane(Vector3.forward, forward).normalized;
        }

        targetRotation = Quaternion.LookRotation(forward, up);

        // adjust model orientation
        targetRotation *= Quaternion.Euler(-90f, 0f, 0f);

        targetPosition = wallPoint + wallNormal * wallOffset;
    }

    // 🔥 STEP 5 — Smooth mount
    void SmoothMount()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            positionSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f &&
    Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            StartCrawling();
        }
    }

    void StartCrawling()
    {
        isCrawling = true;

        // 🔥 random direction ALONG wall
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir = Vector3.ProjectOnPlane(randomDir, wallNormal).normalized;

        Vector3 rawTarget = transform.position + randomDir * roamDistance;

        // 🔥 PROJECT TARGET BACK ONTO WALL
        RaycastHit hit;

        Vector3 origin = rawTarget + wallNormal * 1f;

        if (Physics.Raycast(origin, -wallNormal, out hit, 3f))
        {
            crawlTarget = hit.point + wallNormal * wallOffset;
        }
        else
        {
            // fallback → just stay where you are
            crawlTarget = transform.position;
        }
    }
}