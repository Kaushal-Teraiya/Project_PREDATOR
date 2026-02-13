using Unity.VisualScripting;
using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [SerializeField] private float maxViewDistance = 4f; // Make sure to increase this for realistic effect.. can be tweakable per enemy type
    [SerializeField] private Transform player;
    [SerializeField] private float dotProductThreshold = 0.3f;
    [SerializeField] private LayerMask obstacleMask;
    private bool hasLineOfSight;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    public bool HasLineOfSight => hasLineOfSight;
    public float LastSeenTime => lastSeenTime;
    public Vector3 LastSeenPosition => lastSeenPosition;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        SetHasLOS(false);

        float distanceBtwEnemyNPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceBtwEnemyNPlayer > maxViewDistance)
        {
            SetHasLOS(false);
            return;
        }

        Vector3 directionToTarget = player.transform.position - transform.position;
        directionToTarget.y = 0f;
        directionToTarget.Normalize();
        float dotProduct = Vector3.Dot(transform.forward, directionToTarget);
        if (dotProduct < dotProductThreshold)
        {
            SetHasLOS(false);
            return;
        }

        Vector3 rayOrigin = transform.position;
        Vector3 targetPoint = player.transform.position + Vector3.up * 1.2f;
        Vector3 rayDirection = (targetPoint - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin , targetPoint);
        RaycastHit hitInfo;

        if (Physics.Raycast(rayOrigin, rayDirection, out hitInfo, rayDistance))
        {
            if (hitInfo.collider.CompareTag("Player"))
            {
                SetHasLOS(true);
                lastSeenPosition = player.transform.position;
                lastSeenTime = Time.time;
            }
            else
            {
                SetHasLOS(false);
            }
        }
        else
        {
            SetHasLOS(false);
        }

    }

    private void SetHasLOS(bool _hasLineOfSight)
    {
        hasLineOfSight = _hasLineOfSight;
    }

    private void OnDrawGizmosSelected()
    {
        DrawVisionGizmos();
    }

    private void DrawVisionGizmos()
    {
        Vector3 origin = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, maxViewDistance);

        float halfFov = Mathf.Acos(dotProductThreshold) * Mathf.Rad2Deg;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 leftBoundary = Quaternion.Euler(0f, -halfFov, 0f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, halfFov, 0f) * forward;

        Gizmos.color = Color.darkBlue;
        Gizmos.DrawRay(origin, leftBoundary * maxViewDistance);
        Gizmos.DrawRay(origin, rightBoundary * maxViewDistance);

        Gizmos.color = Color.green;
        if (!hasLineOfSight)
        {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawRay(origin, forward * maxViewDistance);



    }


}
