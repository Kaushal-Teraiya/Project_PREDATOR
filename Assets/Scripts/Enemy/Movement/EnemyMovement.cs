using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyMovement : MonoBehaviour
{

    //[SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float enemySpeed = 10f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationThreshold = 2f;
    [SerializeField] private float separationWeight = 0.5f;
    private ProximitySensor proximitySensor;

    void Awake()
    {
        proximitySensor = GetComponentInChildren<ProximitySensor>();
    }
    public void MoveTo(Vector3 destination)
    {
        Vector3 Direction = destination - transform.position;
        Direction = new Vector3(Direction.x, 0f, Direction.z);
        Vector3 normalizedDirection = Direction.normalized;
        Vector3 separationDirection = proximitySensor.GetSeparationDirection();
        Vector3 finalDir = normalizedDirection + separationDirection * separationWeight;
        Vector3  finalDirection = finalDir.normalized;
        transform.position = Vector3.MoveTowards(transform.position, transform.position + finalDirection, enemySpeed * Time.deltaTime);
    }

    public bool RotateTowards(Vector3 worldDirection)
    {
        Vector3 flatDirection = new Vector3(worldDirection.x, 0f, worldDirection.z);
        if (flatDirection.sqrMagnitude < 0.0001f)
            return true;

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        return angle <= rotationThreshold;
    }
    public void Stop() { }

}
