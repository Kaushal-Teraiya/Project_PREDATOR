using UnityEngine;
public struct HitResult
{
    public GameObject hitObject;
    public Vector3 hitPosition;
    public Vector3 hitNormal;
    public Vector3 hitDirection;
    public float hitForce;
    public bool success;
}

