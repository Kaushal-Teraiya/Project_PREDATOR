using System.Collections.Generic;
using UnityEngine;

public class ProximitySensor : MonoBehaviour
{
    private HashSet<EnemyMovement> otherZombies = new HashSet<EnemyMovement>();
    private EnemyMovement myself;
    //private Transform myself;
    void Awake()
    {
        myself = GetComponentInParent<EnemyMovement>();
    }
    void OnTriggerEnter(Collider other)
    {

        Debug.Log("Trigger entered by: " + other.name);


        var fella = other.gameObject.GetComponentInParent<EnemyMovement>();

        if (fella != null && fella != myself)
        {
            otherZombies.Add(fella);
        }

    }

    void OnTriggerExit(Collider other)
    {
        var fella = other.gameObject.GetComponentInParent<EnemyMovement>();

        if (fella != null && fella != myself)
        {
            otherZombies.Remove(fella);
        }

    }

    public Vector3 GetSeparationDirection()
    {
        if (otherZombies.Count == 0)
        {
            Debug.Log("empty hashSet");
            return Vector3.zero;
        }

        Vector3 separation = Vector3.zero;
        foreach (var zombie in otherZombies)
        {
            Vector3 direction = myself.transform.position - zombie.transform.position;
            direction = new Vector3(direction.x, 0, direction.z);
            Vector3 normalizedDirection = direction.normalized;
            separation += normalizedDirection;
        }

        if (separation == Vector3.zero)
        {
            return Vector3.zero;
        }
        else
        {
            return separation.normalized;
        }
    }
}
