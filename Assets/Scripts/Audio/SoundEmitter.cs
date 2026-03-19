using System;
using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    // private  float walkRadius = 5f;
    // public  float runRadius {get; private set;}= 10f;
    // private float soundValue
    private SoundSource debugProfile;

    //Use this function in all the things that can potentially or obviously make a sound , don't forget to create a sound profile for each of those objects---
    //      ---then pass the profile from that objects script into EmitSound(profile) functin rest of the work will be handled by the emitter script :)))...
    public void EmitSound(SoundSource soundSource)
    {
        debugProfile = soundSource;
        SoundEvent soundEvent = new SoundEvent(transform.position, soundSource.Radius, soundSource.Value);
        Debug.DrawLine(
          soundEvent.position,
          soundEvent.position + Vector3.up * soundEvent.radius,
          Color.yellow,
          0.1f
      );

        // TEMP TEST: find all listeners
        SoundSensor[] listeners = FindObjectsByType<SoundSensor>(0);

        foreach (SoundSensor listener in listeners)
        {
            listener.ProcessSound(soundEvent);
        }

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (debugProfile == null)
        {
            return;
        }
        float radiusToDraw = debugProfile.Radius; // or walkRadius for testing

        DrawCircle(
            transform.position,
            radiusToDraw,
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
