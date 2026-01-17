using Unity.VisualScripting;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    [SerializeField] private float stopDistance = 1.2f;
    public void MoveTo(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime);
    }
    public void Stop() { }
    public bool HasReached(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        if (distance <= stopDistance)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
