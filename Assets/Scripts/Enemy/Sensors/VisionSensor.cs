using System;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [SerializeField] private float maxViewDistance; // Make sure to increase this for realistic effect.. can be tweakable per enemy type
    [SerializeField] private Transform player;
    [SerializeField] private float visionAngle = 120f;
    [SerializeField] private LayerMask obstacleMask;
    private bool hasLineOfSight;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    public bool HasLineOfSight => hasLineOfSight;
    public float LastSeenTime => lastSeenTime;
    public Vector3 LastSeenPosition => lastSeenPosition;
    private Vector3 previousPlayerPosition;
    public event Action<Vector3> OnPeripheralGlimpse;
    private float glimpseCooldownTimer;
    public float AngleFactor { get; private set; }
    //[SerializeField] private float visiblity = 1f;
    [SerializeField] private float chaseThreshold;
    [SerializeField] private float chaseDistanceThresholdInDarkness;
    [SerializeField] private float investigateThreshold;
    [SerializeField] private float verticalDotProductThreshold = 0.3f;
    public float effectiveViewDistance { get; private set; }

    private float environmentVisibility = 1f;
    private float visionClarity = 1f;
    private IVisibilityProvider visibilityProvider;
    private IPlayerMovement playerMovement;
    public enum visibilityResult
    {
        None,
        Investigate,
        Chase
    }

    public visibilityResult VisibilityResult;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        previousPlayerPosition = player.position;
        visibilityProvider = player.GetComponent<IVisibilityProvider>();
        playerMovement = player.GetComponent<IPlayerMovement>();
        environmentVisibility = Mathf.Clamp01(1f - RenderSettings.fogDensity);
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        SetHasLOS(false);

        float distanceBtwEnemyNPlayer = Vector3.Distance(transform.position, player.transform.position);
        effectiveViewDistance = maxViewDistance * environmentVisibility;
        if (distanceBtwEnemyNPlayer > effectiveViewDistance)
        {
            SetHasLOS(false);
            return;
        }

        Vector3 displacement = player.position - previousPlayerPosition;
        float speed = displacement.magnitude / Time.deltaTime;

        float movementFactor = speed / 10;
        movementFactor = Mathf.Max(0.2f, movementFactor);

        Vector3 directionToTarget = (player.transform.position - transform.position).normalized;

        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (angle > visionAngle * 0.5f)
        {
            SetHasLOS(false);
            AngleFactor = 0f;
            previousPlayerPosition = player.position;
            return;
        }

        Vector3 rayOrigin = transform.position;
        Vector3 targetPoint = player.transform.position + Vector3.up * 1.2f;
        Vector3 rayDirection = (targetPoint - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin, targetPoint);
        RaycastHit hitInfo;

        if (Physics.Raycast(rayOrigin, rayDirection, out hitInfo, rayDistance, obstacleMask, QueryTriggerInteraction.Ignore)) //QueryTriggerInteraction.Ignore ignores trigger colliders this was added so that light zones dont obstruct zombie's Line of sight
        {
            if (hitInfo.collider.CompareTag("Player"))
            {
                SetHasLOS(true);

                visionClarity = visibilityProvider.GetVisibility();

                if (!visibilityProvider.IsInDarkEnvironment)
                {
                    lastSeenPosition = player.transform.position;
                    lastSeenTime = Time.time;
                }
                else// This line fixes the bug where the zombie would update the player position even if he was in the dark!!
                {
                    if (visionClarity > investigateThreshold || distanceBtwEnemyNPlayer <= chaseDistanceThresholdInDarkness)
                    {
                        lastSeenPosition = player.transform.position;
                        lastSeenTime = Time.time;
                    }
                }
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

        float angleFactor = 1f - angle / (visionAngle * 0.5f);
        angleFactor = Mathf.Clamp01(angleFactor);
        bool peripheralVision;
        AngleFactor = angleFactor;

        if (visibilityProvider.IsInDarkEnvironment)
        {
            peripheralVision = angleFactor > 0f && angleFactor < 0.5f && VisibilityResult != visibilityResult.None && distanceBtwEnemyNPlayer <= chaseDistanceThresholdInDarkness;
        }
        else
        {
            peripheralVision = angleFactor > 0f && angleFactor < 0.5f;
        }


        if (peripheralVision && hasLineOfSight)
        {
            glimpseCooldownTimer += Time.deltaTime;
            if (glimpseCooldownTimer >= 1f)
            {
                //Debug.Log($"[VisionSensor] Peripheral glimpse at angleFactor: {angleFactor}");
                OnPeripheralGlimpse?.Invoke(player.transform.position);
                glimpseCooldownTimer = 0f;
            }
        }
        else
        {
            glimpseCooldownTimer = 0f;
        }

        // Later we can handle smoke grenade visiblity here raycast check (early return)
        //Light source player visiblity (early return) 

        VisibilityResult = visibilityResult.None;

        if (hasLineOfSight)
        {
            visionClarity = visibilityProvider.GetVisibility();
            //Debug.Log("[VisionSensor] Vision clarity " + visionClarity);
            if (visibilityProvider.IsInDarkEnvironment)
            {
                if (distanceBtwEnemyNPlayer <= chaseDistanceThresholdInDarkness && hasLineOfSight)
                {
                    VisibilityResult = visibilityResult.Chase;
                    return;
                }

                if (visionClarity > chaseThreshold)
                {
                    VisibilityResult = visibilityResult.Chase;
                }
                else if (visionClarity > investigateThreshold)
                {
                    //investigate directly
                    VisibilityResult = visibilityResult.Investigate;
                }
                else if (playerMovement.IsPerformingAction && visionClarity > visibilityProvider.BaseVisibility)
                {
                    //some reaction like agressive scream or animation that shows that zombie is ready to investigate
                    //suspicion accumulation can be done here so that player have time to save themselves from alerting zombies
                    VisibilityResult = visibilityResult.Investigate;
                }
                else
                {
                    VisibilityResult = visibilityResult.None;
                }
            }
            else
            {
                VisibilityResult = visibilityResult.Chase;
            }

        }

        previousPlayerPosition = player.position;
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
        Vector3 forward = transform.forward.normalized;

        float halfFov = visionAngle * 0.5f;
        float coneRadius = Mathf.Tan(halfFov * Mathf.Deg2Rad) * maxViewDistance;

        Vector3 right = transform.right.normalized;
        Vector3 up = transform.up.normalized;
        Vector3 baseCenter = origin + forward * maxViewDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, maxViewDistance);

        Gizmos.color = Color.darkBlue;

        int segments = 10;
        Vector3 previousPoint = baseCenter + right * coneRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = baseCenter + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * coneRadius;

            Gizmos.DrawLine(previousPoint, nextPoint);
            Gizmos.DrawLine(origin, nextPoint);

            previousPoint = nextPoint;
        }

        Gizmos.color = hasLineOfSight ? Color.green : Color.red;
        Gizmos.DrawRay(origin, forward * maxViewDistance);

        Gizmos.color = Color.magenta;

        Vector3 endPoint = origin + forward * chaseDistanceThresholdInDarkness;
        Gizmos.DrawLine(origin, endPoint);

#if UNITY_EDITOR
        Handles.color = hasLineOfSight ? Color.green : Color.red;
        Handles.DrawAAPolyLine(10f, origin, origin + forward * maxViewDistance);

        Handles.color = Color.magenta;
        Handles.DrawAAPolyLine(10f, origin, endPoint);
#endif
    }

}
