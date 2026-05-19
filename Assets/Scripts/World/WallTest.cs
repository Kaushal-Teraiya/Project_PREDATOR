using UnityEngine;

public class WallTest : MonoBehaviour
{
    public Vector3 wallNormal = new Vector3(-1, 0, 0);

    void Update()
    {
        // build a stable forward direction
        Vector3 forward = Vector3.Cross(wallNormal, Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.Cross(wallNormal, Vector3.right);

        forward.Normalize();

        Quaternion target = Quaternion.LookRotation(forward, wallNormal);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            360f * Time.deltaTime
        );
    }
}