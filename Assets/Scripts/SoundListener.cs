using UnityEngine;

public class SoundListener : MonoBehaviour
{
    public float hearingMultiplier = 1f;
    public float memoryDuration = 5f;

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
            // TEMP behavior (will improve later)
            transform.position = lastHeardPosition;
        }
    }

    public void ProcessSound(SoundEvent soundEvent)
    {
        float distance = Vector3.Distance(transform.position, soundEvent.position);
        float effectiveRadius = soundEvent.radius * hearingMultiplier;

        if (distance <= effectiveRadius)
        {
            lastHeardPosition = soundEvent.position;
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
