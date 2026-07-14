using UnityEngine;

public class WayPoint : MonoBehaviour
{
    private EnemyBrain currentOccupant;
    public bool isClaimed => currentOccupant != null;
    private WayPointManager wayPointManager;

    void Awake()
    {
        wayPointManager = FindAnyObjectByType<WayPointManager>();
        if (wayPointManager != null)
        {
            wayPointManager.Register(this);
        }
    }

    public bool TryClaimWayPoint(EnemyBrain requester)
    {
        if (isClaimed)
        {
            return false;
        }

        if (requester == null)
        {
            return false;
        }

        currentOccupant = requester;
        Debug.Log("[WayPoint] wayPoint claimed by: " + requester.name);
        return true;
    }

    public void ReleaseWayPoint(EnemyBrain requester)
    {
        if (currentOccupant != requester)
        {
            return;
        }

        currentOccupant = null;
        Debug.Log("[WayPoint] wayPoint released by ." + requester.name);
    }

    public bool IsFree()
    {
        return currentOccupant == null;
    }

    public bool IsClaimedByOther(EnemyBrain requester)
    {
        return currentOccupant != null && currentOccupant != requester;
    }

    void OnDestroy()
    {
        if (wayPointManager != null)
        {
            wayPointManager.Unregister(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isClaimed ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }

}
