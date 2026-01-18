using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    [Header("Sound Settings")]
    public float walkRadius = 5f;
    public float runRadius = 10f;

    public void EmitSound(float radius)
    {
        SoundEvent soundEvent = new SoundEvent(transform.position, radius);
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

        float radiusToDraw = runRadius; // or walkRadius for testing

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
