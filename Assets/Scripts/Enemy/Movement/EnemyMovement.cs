using Unity.VisualScripting;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    //[SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float enemySpeed = 10f;
    public void MoveTo(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, enemySpeed*Time.deltaTime);
    }
    public void Stop() { }
    
}
