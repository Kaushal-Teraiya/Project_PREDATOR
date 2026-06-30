// using UnityEditor;
// using UnityEngine;

// [CustomEditor(typeof(AddExplosionForceRuntime))]
// public class AddExplosionForceEditor : Editor
// {
//     public void OnSceneGUI()
//     {
//         Event e = Event.current;

//         if (e.type == EventType.MouseDown)
//         {
//             Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

//             if (Physics.Raycast(ray, out RaycastHit hit))
//             {
//                 Debug.Log("EDITOR TOOL FOR EXPLOSION " + hit.point);
//                 Collider[] colliders = Physics.OverlapSphere(hit.point, 5f);

//                 foreach (var collider in colliders)
//                 {
//                     Rigidbody rigidbody = collider.attachedRigidbody;

//                     if (rigidbody != null)
//                     {
//                         rigidbody.AddExplosionForce(1000f, hit.point, 5f, 1f, ForceMode.Impulse);
//                     }
//                 }
//             }
//         }
//     }
// }
