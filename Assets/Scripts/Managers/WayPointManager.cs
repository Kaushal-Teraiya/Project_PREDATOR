using System.Collections.Generic;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
    private List<WayPoint> allWayPoints = new List<WayPoint>();

    public void Register(WayPoint wayPoint)
    {
        if (allWayPoints.Contains(wayPoint) || wayPoint == null)
        {
            return;
        }

        allWayPoints.Add(wayPoint);
    }

    public void Unregister(WayPoint wayPoint)
    {
        if (wayPoint == null)
        {
            return;
        }

        allWayPoints.Remove(wayPoint);
    }

    private List<WayPoint> GetInRadius(Vector3 position, float radius)
    {
        List<WayPoint> result = new List<WayPoint>();
        float radiusSqr = radius * radius;
        foreach (var point in allWayPoints)
        {
            if ((point.transform.position - position).sqrMagnitude <= radiusSqr)
            {
                result.Add(point);
            }
        }

        return result;
    }

    public List<WayPoint> GetFreeInRadius(Vector3 position, float radius)
    {
        var inRadius = GetInRadius(position, radius);
        var result = new List<WayPoint>();

        foreach (var point in inRadius)
        {
            if (point.IsFree())
            {
                result.Add(point);
            }
        }

        return result;
    }
}
