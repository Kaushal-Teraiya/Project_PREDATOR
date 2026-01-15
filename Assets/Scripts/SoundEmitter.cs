using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    [Header("Sound Settings")]
    public float walkRadius = 5f;
    public float runRadius = 10f;

    public void EmitSound(float radius)
    {
        SoundEvent soundEvent = new SoundEvent(transform.position , radius);
          Debug.DrawLine(
            soundEvent.position,
            soundEvent.position + Vector3.up * soundEvent.radius,
            Color.yellow,
            0.1f
        );

           // TEMP TEST: find all listeners
    SoundListener[] listeners = FindObjectsByType<SoundListener>(0);

    foreach (SoundListener listener in listeners)
    {
        if (listener.CanHearSound(soundEvent))
        {
            Debug.Log(listener.name + " heard the sound!");
        }
    }
      
    }
}
