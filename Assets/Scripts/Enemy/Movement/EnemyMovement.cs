using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

public class EnemyMovement : MonoBehaviour
{

    //[SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float enemySpeed = 10f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationThreshold = 2f;
    [SerializeField] private float separationWeight = 0.5f;
    [SerializeField] private float dotProductThreshold = 0.85f;
    [SerializeField] private float distanceBetweenZombiesThreshold = 0.5f;
    [SerializeField] private float avoidanceDuration = 2f; //time before escalation takes place 
    [SerializeField] private float avoidanceMinLockTime = 0.25f; //mandatory time for which you should hold avoidance before releasing 

    private bool avoidanceCommitted;
    private bool pressureStillExists;
    private float avoidanceYaw; //float angle that will be converted to a Quaternion rotation
    private int avoidanceSide; //the side AI chooses to turn to avoid the blockage
    private float avoidanceStartTime;
    private bool hasEscalated; //when default angle doesnt resolve blockage
    private float handedness; //random + or - if the AI bumps into each other head-on
    private bool hasFrontObstruction;
    private Quaternion rotationYaw; //Y axis rotation
    private ProximitySensor proximitySensor;
    private Vector3 baseAvoidanceForward; //last recorded intent direction when avoidance was first commited 
    private bool canRotate;
    private bool loggedAvoidance;


    void Awake()
    {
        proximitySensor = GetComponentInChildren<ProximitySensor>();
        handedness = UnityEngine.Random.value < 0.5f ? -1f : 1f;

    }
    public void MoveTo(Vector3 destination)
    {
        hasFrontObstruction = false;
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);

        Vector3 Direction = destination - transform.position;
        Direction = new Vector3(Direction.x, 0f, Direction.z);
        Vector3 normalizedDirection = Direction.normalized;
        Vector3 separationDirection = proximitySensor.GetSeparationDirection();
        Vector3 fromZombieToOtherZombie = Vector3.zero;

        foreach (var otherZombie in proximitySensor.NearbyZombies())
        {
            fromZombieToOtherZombie = otherZombie.transform.position - transform.position;
            fromZombieToOtherZombie.y = 0f;
            float distanceBetwweenZombies = Vector3.Distance(transform.position, otherZombie.transform.position);

            if (distanceBetwweenZombies > distanceBetweenZombiesThreshold)
            {
                continue;
            }
            fromZombieToOtherZombie.Normalize();

            if (Vector3.Dot(normalizedDirection, fromZombieToOtherZombie) >= dotProductThreshold)
            {
                hasFrontObstruction = true;
                break;
            }
        }

        if (hasFrontObstruction && !avoidanceCommitted && canRotate)
        {
            //calc avoidance bias here
            avoidanceCommitted = true;
            baseAvoidanceForward = normalizedDirection;
            avoidanceStartTime = Time.time;
            Vector3 crossProduct = Vector3.Cross(normalizedDirection, fromZombieToOtherZombie);

            if (Mathf.Abs(crossProduct.y) < 0.05f)
            {
                // Head-on encounter => use handedness
                avoidanceSide = (int)Mathf.Sign(handedness);
            }
            else
            {
                // Normal case => use geometry
                avoidanceSide = (int)Mathf.Sign(crossProduct.y);
            }
            avoidanceYaw = 75f * avoidanceSide; // default
            rotationYaw = Quaternion.Euler(0f, avoidanceYaw, 0f);

        }

        if (avoidanceCommitted)
        {
            if (!hasEscalated && Time.time - avoidanceStartTime >= avoidanceDuration)
            {
                avoidanceYaw = 90f * avoidanceSide;
                hasEscalated = true;
                rotationYaw = Quaternion.Euler(0f, avoidanceYaw, 0f);
            }


            pressureStillExists = false;
            foreach (var otherZombie in proximitySensor.NearbyZombies())
            {
                var directionFrom_ZombieToOther = otherZombie.transform.position - transform.position;
                directionFrom_ZombieToOther.y = 0f;
                float ZombieDistance = Vector3.Distance(otherZombie.transform.position, transform.position);
                directionFrom_ZombieToOther.Normalize();
                var crossProduct = Vector3.Cross(transform.forward, directionFrom_ZombieToOther);
                if (/*Mathf.Sign(crossProduct.y) == pressureSide &&*/ ZombieDistance > distanceBetweenZombiesThreshold)
                {
                    //do not release
                    continue;
                }



                if (Vector3.Dot(transform.forward, directionFrom_ZombieToOther) < dotProductThreshold)
                    continue;

                //check pressure/blockage side
                float side = Mathf.Sign(crossProduct.y); //signifies left or right blockage + or - 
                if (side == -avoidanceSide) //if the blockage is on left and i chose to turn right keep checking left and vice versa
                {
                    pressureStillExists = true;
                    break;
                } 
            }

            //release
            if (!pressureStillExists && CanReleaseAvoidance())
            {
                avoidanceCommitted = false;
                rotationYaw = Quaternion.identity;
                hasEscalated = false;
                avoidanceStartTime = 0f;
            }

        }
        Vector3 movementDir;

        if (avoidanceCommitted)
        {
            movementDir = rotationYaw * baseAvoidanceForward;
        }
        else
        {
            movementDir = normalizedDirection;
        }

        movementDir += separationDirection * separationWeight;
        movementDir = movementDir.normalized;

        transform.position = Vector3.MoveTowards(transform.position, transform.position + movementDir, enemySpeed * Time.deltaTime);
        Debug.DrawRay(transform.position, movementDir * 2f, Color.cyan);

        if (avoidanceCommitted && canRotate)
        {
            var desiredForward = rotationYaw * baseAvoidanceForward;
            var desiredRotation = Quaternion.LookRotation(desiredForward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
            Debug.Log("AVOIDANCE ROTATING BODY");
            Debug.DrawRay(transform.position, desiredForward * 2f, Color.green);


        }

        if (avoidanceCommitted && !loggedAvoidance)
        {
            Debug.Log("AVOIDANCE STARTED");
            loggedAvoidance = true;
        }

        if (!avoidanceCommitted && loggedAvoidance)
        {
            Debug.Log("AVOIDANCE ENDED");
            loggedAvoidance = false;
        }


    }

    public bool RotateTowards(Vector3 worldDirection)
    {
        Vector3 flatDirection = new Vector3(worldDirection.x, 0f, worldDirection.z);
        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
        if (flatDirection.sqrMagnitude < 0.0001f)
            return true;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        return angle <= rotationThreshold;
    }

    public bool isAvoiding()
    {
        return avoidanceCommitted;
    }

    public void SetRotationPermission(bool allowRotate)
    {
        canRotate = allowRotate;
    }
    bool CanReleaseAvoidance()
    {
        return Time.time - avoidanceStartTime >= avoidanceMinLockTime; //if the timer has passed the desired time
    }

    public void Stop() { }

}