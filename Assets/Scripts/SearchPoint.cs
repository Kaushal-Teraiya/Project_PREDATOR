using UnityEngine;

public class SearchPoint : MonoBehaviour
{
    [SerializeField] private Transform interactionAnchor;
    public Transform InteractionAnchor => interactionAnchor;
    private EnemyBrain currentOccupant;
    public bool IsOccupied => currentOccupant != null;

    public void Release(EnemyBrain requester)
    {
        if (currentOccupant != requester)
        {
            return;
        }

        currentOccupant = null;
    }

    public bool TryClaim(EnemyBrain requester)
    {
        if (IsOccupied)
        {
            return false;
        }

        if (requester == null)
        {
            return false;
        }

        currentOccupant = requester;
        return true;

    }
}
