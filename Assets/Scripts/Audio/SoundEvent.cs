using UnityEngine;

public struct SoundEvent
{
    public Vector3 position;
    public float  radius;
    public float time;
    public int value;

    public SoundEvent(Vector3 position , float radius , int value)
    {
        this.position = position;
        this.radius = radius;
        this.time = Time.time;
        this.value = value;
    }
}
