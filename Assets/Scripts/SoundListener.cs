using UnityEngine;

public class SoundListener : MonoBehaviour
{
    public float hearingMultiplier = 1f;
    public float memoryDuration = 5f;
    public float InvestigateSpeed = 2f;
    public float investigateStopDistance = 0.3f;
    public float rotationSpeed = 360f; // degrees per second


    private enum State
    {
        Idle,
        Investigate
    }

    private State currentState;

    [Header("Memory")]
    public Vector3 lastHeardPosition;
    public float lastHeardTime;
    public bool hasHeardSound;

    void Start()
    {
        currentState = State.Idle;
    }

    void Update()
    {
        if (hasHeardSound)
        {
            float timeSinceHeard = Time.time - lastHeardTime;

            if (timeSinceHeard <= memoryDuration)
            {
                SetState(State.Investigate);
            }
            else
            {
                hasHeardSound = false;
                SetState(State.Idle);
            }
        }
        else
        {
            SetState(State.Idle);
        }

        if (currentState == State.Investigate)
        {
            transform.position = Vector3.MoveTowards(transform.position, lastHeardPosition, InvestigateSpeed * Time.deltaTime);
            float distance = Vector3.Distance(transform.position, lastHeardPosition);
            Vector3 direction = lastHeardPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            if (distance <= investigateStopDistance)
            {
                hasHeardSound = false;
                SetState(State.Idle);
            }
        }
    }

    public void ProcessSound(SoundEvent soundEvent)
    {
        float distance = Vector3.Distance(transform.position, soundEvent.position);
        float effectiveRadius = soundEvent.radius * hearingMultiplier;

        if (distance <= effectiveRadius)
        {
            lastHeardPosition = new Vector3(soundEvent.position.x, transform.position.y, soundEvent.position.z);
            lastHeardTime = Time.time;
            hasHeardSound = true;
            currentState = State.Investigate;
            Debug.Log(name + " remembered sound at " + lastHeardPosition);
        }
    }

    void SetState(State newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        Debug.Log(name + " changed state to " + currentState);
    }

}
