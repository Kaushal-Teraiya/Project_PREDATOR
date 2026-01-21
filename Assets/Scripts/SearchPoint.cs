using UnityEngine;

public class SearchPoint : MonoBehaviour
{
    [SerializeField] private Transform interactionAnchor;
    public Transform InteractionAnchor => interactionAnchor;
    private EnemyBrain currentOccupant;
    public bool IsOccupied => currentOccupant != null;
    public float arrivalRadius { get; private set; } = 0.1f;
    public float searchPointRadius { get; private set; } = 5f;

    public void Release(EnemyBrain requester)
    {
        if (currentOccupant != requester)
        {
            Debug.Log($"{requester.name} tried to release {name} but is not occupant");
            return;
        }
        Debug.Log($"{requester.name} RELEASED {name}");
        arrivalRadius = 0.3f;
        currentOccupant = null;
    }

    public bool TryClaim(EnemyBrain requester)
    {
        if (IsOccupied)
        {
            Debug.Log($"{requester.name} FAILED to claim {name} (occupied by {currentOccupant.name})");
            return false;
        }

        if (requester == null)
        {
            return false;
        }


        if (!requester.CanClaim(this))
        {
            return false;
        }

        currentOccupant = requester;
        arrivalRadius = 10f;

        Debug.Log($"{requester.name} CLAIMED {name}");
        return true;

    }

    public void SetArrivalRadius(float _changedArrivalradius)
    {
        arrivalRadius = _changedArrivalradius;
    }

    public bool IsCenterBlockedFor(EnemyBrain requester)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.8f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") &&
                hit.transform != requester.transform)
            {
                return true;
            }
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (searchPointRadius <= 0f)
            return;

        Gizmos.color = Color.orange;


        DrawCircle(
            transform.position,
            searchPointRadius,
            40
        );
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }


}
