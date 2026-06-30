using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AddExplosionForceRuntime))]
public class AddExplosionForceEditor : Editor
{
    public void OnSceneGUI()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown &&
            e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Explosion at: " + hit.point);

                AddExplosionForceRuntime explosionTool =
                    (AddExplosionForceRuntime)target;

                Collider[] colliders =
                    Physics.OverlapSphere(
                        hit.point,
                        explosionTool.explosionRadius);

                foreach (Collider collider in colliders)
                {
                    Rigidbody rb = collider.attachedRigidbody;

                    if (rb != null)
                    {
                        rb.AddExplosionForce(
                            explosionTool.force,
                            hit.point,
                            explosionTool.explosionRadius,
                            1f,
                            ForceMode.Impulse);
                    }
                }

                e.Use();
            }
        }
    }
}