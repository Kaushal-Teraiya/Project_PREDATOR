using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        SoundSystem.Emit(soundEvent);
    }

    void OnDrawGizmos()
    {
        if (debugProfile == null)
            return;

#if UNITY_EDITOR
        Handles.color = Color.red;
#else
    Gizmos.color = Color.red;
#endif

        DrawCircle(transform.position, debugProfile.Radius, 60);
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

#if UNITY_EDITOR
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(5f, prevPoint, nextPoint); // Thickness = 5
#else
        Gizmos.DrawLine(prevPoint, nextPoint);
#endif

            prevPoint = nextPoint;
        }
    }

}
