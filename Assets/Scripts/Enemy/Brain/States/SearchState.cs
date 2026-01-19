using UnityEngine;

public class SearchState : IEnemyState
{
    private EnemyBrain brain;
    private float searchDuration;
    private float scanDuration = 5f;
    private float scanStartTime;
    private enum SearchPhase
    {
        Moving,
        Scanning
    }

    private SearchPhase currentPhase;
    private SearchPoint currentTarget;
    public SearchState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        currentPhase = SearchPhase.Moving;
        Debug.Log("searching starts");
        brain.InitializeSearch();
    }
    public void Tick()
    {
        if (currentPhase == SearchPhase.Moving)
        {
            var Enemy = brain.enemyMovement;
            if (currentTarget == null)
            {
                currentTarget = brain.GetCurrentSearchPoint();
            }

            if (currentTarget == null)
            {
                return;
            }
            var direction = currentTarget.transform.position - Enemy.transform.position;


            Enemy.MoveTo(currentTarget.transform.position);
            Enemy.RotateTowards(direction);

            float distance = Vector3.Distance(currentTarget.transform.position, Enemy.transform.position);
            float threshold = 0.2f;

            if (distance < threshold)
            {
                Enemy.Stop();
                currentPhase = SearchPhase.Scanning;
                scanStartTime = Time.time;

            }
        }

        if (currentPhase == SearchPhase.Scanning)
        {
            if (Time.time - scanStartTime < scanDuration)
            {
                //scan
                Debug.Log("Scanning the location");
            }
            else
            {
                currentTarget = null;
                currentPhase = SearchPhase.Moving;
                brain.IncrementSearchIndex();
            }
        }
    }
    public void OnExit()
    {
        Debug.Log("exited searching");
    }
}

