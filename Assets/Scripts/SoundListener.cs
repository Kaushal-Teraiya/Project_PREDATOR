using UnityEngine;

public class SoundListener : MonoBehaviour
{
    public float hearingMultiplier = 1f;

    public bool CanHearSound(SoundEvent soundEvent)
    {
        float distance = Vector3.Distance(transform.position, soundEvent.position);
        float effectiveRadius = soundEvent.radius * hearingMultiplier;

        return distance <= effectiveRadius;
    }
}
