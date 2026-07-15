using UnityEngine;

public class RigidbodyAdder : MonoBehaviour
{
    [ContextMenu("Add Rigidbodies")]
    public void AddRigidbodies()
    {
        int count = 0;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = col.gameObject.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                count++;
            }
        }

        Debug.Log($"Added {count} rigidbodies.");
    }
}