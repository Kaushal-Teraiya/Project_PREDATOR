using UnityEngine;
using UnityEngine.AI;

public class NavmeshAgenttestScript : MonoBehaviour
{
    private NavMeshAgent agent;
    public GameObject testDestination;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(testDestination.transform.position);
    }
}
