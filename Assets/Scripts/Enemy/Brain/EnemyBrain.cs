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
    [SerializeField] private float investigationThreshold = 8f;
    private Vector3 currentInvestigationCenter;
    private float currentInvestigationRadius;
    [SerializeField]private float InvestigateAreaRadius;

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

    private void SetInvestigateState()
    {
        currentInvestigationCenter = soundSensor.LastHeardPosition;
        currentInvestigationRadius = InvestigateAreaRadius;
        investigateState.SetAreaCenter_AreaRadius(currentInvestigationCenter, currentInvestigationRadius);

    }

    private void CheckStateChange()
    {
        if (currentState == idleState && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= investigationThreshold)
        {
            SetInvestigateState();
            SwitchState(investigateState);
        }

        if (currentState == investigateState && soundSensor.HasValidSound() && soundSensor.LastHeardRadius >= investigationThreshold)
        {
            float distance = Vector3.Distance(soundSensor.LastHeardPosition, currentInvestigationCenter);
            if (distance > currentInvestigationRadius)
            {
                SetInvestigateState();
                SwitchState(investigateState);
            }
        }

        if (currentState == investigateState && investigateState.hasReachedDestination)
        {
            SwitchState(searchState);
        }

        if (currentState == searchState && searchState.isSearchComplete)
        {
            SwitchState(idleState);
        }
    }
}
