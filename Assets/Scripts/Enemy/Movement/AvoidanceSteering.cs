using Unity.Mathematics;
using UnityEngine;

public class AvoidanceSteering : MonoBehaviour
{
    private enum AvoidanceMode
    {
        None,
        Avoid,
        Esc180
    }

    private struct AvoidanceIntent
    {
        public Vector3 baseAvoidanceForward;
        public int avoidanceSide;
        public float yaw;
        public float avoidanceStartTime;
    }

    [Header("Detection")]
    [SerializeField] private float dotProductThreshold;
    [SerializeField] private float Zombie_DistanceThreshold;
    [SerializeField] private float Player_DistanceThreshold;

    [Header("Timing")]
    [SerializeField] private float Zombie_avoidanceMinLockTime = 0.5f;
    [SerializeField] private float Player_avoidanceMinLockTime = 1.2f;
    [SerializeField] private float escalationTime;

    [Header("Angles")]
    [SerializeField] private float Zombie_defaultYaw;
    [SerializeField] private float Player_defaultYaw;
    [SerializeField] private float Zombie_escalatedYaw;
    [SerializeField] private float Player_escalatedYaw;

    private ProximitySensor proximitySensor;
    private AvoidanceMode mode = AvoidanceMode.None;
    private AvoidanceIntent intent;
    private Quaternion yawRotation;
    private int handedness;
    private bool CanRotate;
    private bool avoidanceEnabled;

    public bool HasOverrideDirection(out Vector3 overrideDir)
    {
        if (mode == AvoidanceMode.None)
        {
            overrideDir = Vector3.zero;
            return false;
        }

        overrideDir = yawRotation * intent.baseAvoidanceForward;
        return true;
    }

    void Update()
    {
        UpdateAvoidance();
    }

    void Awake()
    {
        proximitySensor = GetComponentInChildren<ProximitySensor>();
        handedness = UnityEngine.Random.value < 0.5f ? -1 : 1;

    }

    private void UpdateAvoidance()
    {
        if (mode == AvoidanceMode.None)
        {
            if (!CanRotate)
                return;
            Zombie_TryCommitAvoidance();
            if (avoidanceEnabled)
            {
                Player_TryCommitAvoidance();//Minor fix don't let avoidance work with player in states like chase state
            }
            return;
        }

        float elapsedTime = Time.time - intent.avoidanceStartTime;

        if (elapsedTime < Zombie_avoidanceMinLockTime)
        {
            return;
        }

        // if (proximitySensor.HasNearbyPlayer() && elapsedTime < Player_avoidanceMinLockTime)
        // {
        //     return;
        // }

        if (!OppositeSideHasPressure() && IsForwardAligned())
        {
            Release();
            return;
        }

        if (mode == AvoidanceMode.Avoid && elapsedTime >= escalationTime)
        {
            Escalate();
        }
    }

    private void Zombie_TryCommitAvoidance()
    {
        foreach (var otherZombie in proximitySensor.NearbyZombies())
        {
            Vector3 fromZombieToOtherZombie = otherZombie.transform.position - transform.position;
            fromZombieToOtherZombie.y = 0f;

            if (fromZombieToOtherZombie.magnitude > Zombie_DistanceThreshold)
            {
                continue;
            }

            fromZombieToOtherZombie.Normalize();

            if (Vector3.Dot(transform.forward, fromZombieToOtherZombie) < dotProductThreshold)
            {
                continue;
            }

            Commit(transform.forward, fromZombieToOtherZombie);
            break;
        }
    }

    private void Player_TryCommitAvoidance()
    {
        foreach (var player in proximitySensor.NearbyPlayer())
        {
            Vector3 fromZombieToPlayer = player.transform.position - transform.position;
            fromZombieToPlayer.y = 0f;

            if (fromZombieToPlayer.magnitude > Player_DistanceThreshold)
            {
                continue;
            }

            fromZombieToPlayer.Normalize();

            if (Vector3.Dot(transform.forward, fromZombieToPlayer) < dotProductThreshold)
            {
                continue;
            }

            Commit(transform.forward, fromZombieToPlayer);
            break;
        }
    }


    private void Commit(Vector3 baseForward, Vector3 directionFromZtoOtherZ)
    {
        intent.baseAvoidanceForward = baseForward.normalized;
        intent.avoidanceStartTime = Time.time;

        Vector3 crossProduct = Vector3.Cross(intent.baseAvoidanceForward, directionFromZtoOtherZ);

        if (Mathf.Abs(crossProduct.y) < 0.05f)
        {
            intent.avoidanceSide = handedness;
        }
        else
        {
            intent.avoidanceSide = (int)Mathf.Sign(crossProduct.y);
        }

        if (proximitySensor.HasNearbyPlayer())
        {

            intent.yaw = Player_defaultYaw * intent.avoidanceSide;
            yawRotation = Quaternion.Euler(0f, intent.yaw, 0f);
            mode = AvoidanceMode.Avoid;
        }
        else
        {
            intent.yaw = Zombie_defaultYaw * intent.avoidanceSide;
            yawRotation = Quaternion.Euler(0f, intent.yaw, 0f);
            mode = AvoidanceMode.Avoid;
        }
    }

    private bool OppositeSideHasPressure()
    {
        foreach (var otherZombie in proximitySensor.NearbyZombies())
        {
            Vector3 fromZombieToOther = otherZombie.transform.position - transform.position;
            fromZombieToOther.y = 0f;

            if (fromZombieToOther.magnitude > Zombie_DistanceThreshold)
            {
                continue;
            }

            fromZombieToOther.Normalize();

            if (Vector3.Dot(intent.baseAvoidanceForward, fromZombieToOther) < dotProductThreshold)
            {
                continue;
            }

            Vector3 crossProduct = Vector3.Cross(intent.baseAvoidanceForward, fromZombieToOther);
            float side = Mathf.Sign(crossProduct.y);

            if (side == -intent.avoidanceSide)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsForwardAligned() // this function checks if the body has rotated enough to face the avoidance direction.
    {
        Vector3 current = transform.forward;
        current.y = 0f;
        current.Normalize();

        Vector3 avoidedForward = yawRotation * intent.baseAvoidanceForward;
        return Vector3.Dot(current, avoidedForward) > 0.7f;

    }

    private void Escalate()
    {
        if (proximitySensor.HasNearbyPlayer())
        {
            intent.yaw = Player_defaultYaw * intent.avoidanceSide;
            yawRotation = Quaternion.Euler(0f, intent.yaw, 0f);
            mode = AvoidanceMode.Esc180;
        }
        else
        {
            intent.yaw = Zombie_defaultYaw * intent.avoidanceSide;
            yawRotation = Quaternion.Euler(0f, intent.yaw, 0f);
            mode = AvoidanceMode.Esc180;
        }
    }

    private void Release()
    {
        mode = AvoidanceMode.None;
        intent = default;
        yawRotation = Quaternion.identity;
        intent.avoidanceStartTime = Time.time;
    }

    public void SetRotationPermission(bool _canRotate)
    {
        CanRotate = _canRotate;
    }

    public bool IsAvoiding()
    {
        return mode != AvoidanceMode.None;
    }

    public void SetPlayerAvoidance(bool _value)
    {
        avoidanceEnabled = _value;
    }
}
