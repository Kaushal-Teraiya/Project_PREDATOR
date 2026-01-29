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
        var fella = other.gameObject.GetComponentInParent<EnemyMovement>();

        if (fella != null && fella != myself)
        {
            otherZombies.Add(fella);
        }

    }

    public IEnumerable<EnemyMovement> NearbyZombies()
    {
        foreach (var zombie in otherZombies)
        {
            yield return zombie;
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

    public bool HasNearbyEnemy()
    {
        return otherZombies.Count > 0;
    }

    public Vector3 GetSeparationDirection()
    {
        if (otherZombies.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 separation = Vector3.zero;
        
        foreach (var zombie in otherZombies)
        {
            Vector3 direction = myself.transform.position - zombie.transform.position;
            direction.y = 0f;
            direction.Normalize();
            separation += direction;
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
