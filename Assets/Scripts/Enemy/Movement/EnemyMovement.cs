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

    private ProximitySensor proximitySensor;
    private bool canRotate;

    private AvoidanceSteering avoidance;


    void Awake()
    {
        proximitySensor = GetComponentInChildren<ProximitySensor>();
        avoidance = GetComponent<AvoidanceSteering>();
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

        movementDir = movementDir.normalized;

        if (canRotate)
        {
            RotateTowards(movementDir);
        }

        transform.position = Vector3.MoveTowards(transform.position, transform.position + movementDir, enemySpeed * Time.deltaTime);
        Debug.DrawRay(transform.position, movementDir * 2f, Color.cyan);

    }

    public bool RotateTowards(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(worldDirection.normalized);
        if (worldDirection.sqrMagnitude < 0.0001f)
            return true;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        return angle <= rotationThreshold;
    }

    public bool IsAvoiding()
    {
        return avoidance.IsAvoiding();
    }
    public void SetRotationPermission(bool allowRotate)
    {
        canRotate = allowRotate;
    }


    public void Stop() { }

}