using UnityEngine;

public struct SoundEvent
{
    public Vector3 position;
    public float radius;
    public float time;

    public SoundEvent(Vector3 position , float radius)
    {
        this.position = position;
        this.radius = radius;
        this.time = Time.time;
    }
}
