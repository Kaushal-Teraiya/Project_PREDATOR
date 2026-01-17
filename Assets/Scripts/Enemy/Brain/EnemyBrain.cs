using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    private SoundSensor soundSensor;
    private EnemyMovement enemyMovement;
    private IEnemyState currentState;
    private IdleState idleState;
    private InvestigateState investigateState;
    private SearchState searchState;

    void Awake()
    {
        soundSensor = GetComponent<SoundSensor>();
        enemyMovement = GetComponent<EnemyMovement>();
    }
    void Start()
    {
        idleState = new IdleState(this);
        searchState = new SearchState(this);
        investigateState = new InvestigateState(this);
    }

    void Update()
    {
        currentState?.Tick();
    }

    private void SwitchState(IEnemyState newState)
    {
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();

    }
}
