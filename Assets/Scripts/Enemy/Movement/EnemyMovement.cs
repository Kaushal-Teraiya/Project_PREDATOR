using Unity.VisualScripting;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    //[SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float enemySpeed = 10f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationThreshold = 2f;
    public void MoveTo(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, enemySpeed * Time.deltaTime);
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
