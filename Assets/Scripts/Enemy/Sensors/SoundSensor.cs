using UnityEngine;

public class SoundSensor : MonoBehaviour
{
    [Header("Hearing")]
    public float hearingMultiplier = 1f;
    public float memoryDuration = 5f;

    [Header("Sound Memory (Read Only)")]
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardRadius { get; private set; }
    public float LastHeardTime { get; private set; }
    public bool HasHeardSound { get; private set; }

    public void ProcessSound(SoundEvent soundEvent)
    {
        float distance = Vector3.Distance(transform.position, soundEvent.position);
        LastHeardRadius = soundEvent.radius;
        float effectiveRadius = LastHeardRadius * hearingMultiplier;

        if (distance > effectiveRadius)
            return;

        // Flatten to ground level
        LastHeardPosition = new Vector3(
            soundEvent.position.x,
            transform.position.y,
            soundEvent.position.z
        );

        LastHeardTime = Time.time;
        HasHeardSound = true;

      //  Debug.Log($"{name} heard sound at {LastHeardPosition}");
    }

    public bool HasValidSound()
    {
        if (!HasHeardSound)
            return false;

        if (Time.time - LastHeardTime > memoryDuration)
        {
            HasHeardSound = false;
            return false;
        }

        return true;
    }

    public void ClearSound()
    {
        HasHeardSound = false;
    }
}
