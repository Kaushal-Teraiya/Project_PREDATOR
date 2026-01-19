using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    private SoundSensor soundSensor;
    private EnemyMovement Movement_Enemy;
    public EnemyMovement enemyMovement => Movement_Enemy;
    private IEnemyState currentState;
    private IdleState idleState;
    private InvestigateState investigateState;
    private SearchState searchState;
    private Vector3 currentInvestigationCenter;
    private float currentInvestigationRadius;
    private GameObject player;
    [SerializeField] private float InvestigateAreaRadius;
    [SerializeField] private float proximityRadius;
    [SerializeField] private float investigationThreshold = 8f;
    private List<SearchPoint> availableSearchPoints = new List<SearchPoint>();
    private List<SearchPoint> selectedSearchPoints = new List<SearchPoint>();
    public int currentSearchIndex { get; private set; }
    public bool IsSearchComplete
    {
        get
        {
            return currentSearchIndex >= selectedSearchPoints.Count;
        }
    }


    void Awake()
    {
        soundSensor = GetComponent<SoundSensor>();
        Movement_Enemy = GetComponent<EnemyMovement>();
    }
    void Start()
    {
        idleState = new IdleState(this);
        searchState = new SearchState(this);
        investigateState = new InvestigateState(this);
        SwitchState(idleState);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        currentState?.Tick();
        CheckStateChange();
    }

    private void SwitchState(IEnemyState newState)
    {
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();

    }

    private void InitializeInvestigateState()
    {
        currentInvestigationCenter = soundSensor.LastHeardPosition;
        currentInvestigationRadius = InvestigateAreaRadius;
        investigateState.SetAreaCenter_AreaRadius(currentInvestigationCenter, currentInvestigationRadius);
    }

    public void InitializeSearch()
    {
        selectedSearchPoints.Clear();
        availableSearchPoints = GetComponents<SearchPoint>().ToList();
        ShuffleSearchPoints();
        var pickN = availableSearchPoints.Count * 0.4;
        int rounUp = (int)Mathf.Ceil((float)pickN);
        int finalN = Mathf.Clamp(rounUp, 2, 8);
        finalN = (int)MathF.Min(finalN, availableSearchPoints.Count);
        for (int i = 0; i < finalN; i++)
        {
            selectedSearchPoints.Add(availableSearchPoints[i]);
        }

        currentSearchIndex = 0;

    }

    public SearchPoint GetCurrentSearchPoint()
    {
        if (currentSearchIndex >= 0 && currentSearchIndex < selectedSearchPoints.Count)
        {
            return selectedSearchPoints[currentSearchIndex];
        }
        else
        {
            return null;
        }
    }

    public void IncrementSearchIndex()
    {
        currentSearchIndex++;
    }





    private void ShuffleSearchPoints()
    {
        for (int currentIndex = availableSearchPoints.Count - 1; currentIndex > 0; currentIndex--)
        {
            int randomIndex = UnityEngine.Random.Range(0, currentIndex + 1);
            var temp = availableSearchPoints[currentIndex];
            availableSearchPoints[currentIndex] = availableSearchPoints[randomIndex];
            availableSearchPoints[randomIndex] = temp;
        }
    }



    private void CheckStateChange()
    {
        float proximityCheck = Vector3.Distance(player.transform.position, transform.position);
        if (proximityCheck < proximityRadius)
        {
            //later we can add closest player for multiplayer here
            Debug.Log("player is right in front of me");
            return; //just for now so it doesnt start executing other states
        }
        if (currentState == idleState && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= investigationThreshold)
        {
            InitializeInvestigateState();
            SwitchState(investigateState);
        }

        if ((currentState == investigateState || currentState == searchState )&& soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= investigationThreshold)
        {
            float distance = Vector3.Distance(soundSensor.LastHeardPosition, currentInvestigationCenter);
            if (distance > currentInvestigationRadius)
            {
                InitializeInvestigateState();
                SwitchState(investigateState);
            }
        }

        if (currentState == investigateState && investigateState.hasReachedDestination)
        {
            SwitchState(searchState);
        }

        if (currentState == searchState && IsSearchComplete)
        {
            SwitchState(idleState);
        }
    }


    void OnDrawGizmos()
    {
        if (currentInvestigationRadius <= 0f)
            return;

        Gizmos.color = Color.red;

        DrawCircle(
            currentInvestigationCenter,
            currentInvestigationRadius,
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