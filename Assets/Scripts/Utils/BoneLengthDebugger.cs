using UnityEngine;

public class BoneLengthDebugger : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        DrawRecursive(transform);
    }

    private void DrawRecursive(Transform bone)
    {
        Transform child = GetFirstPhysicsChild(bone);

        if (child != null)
        {
            float length = Vector3.Distance(bone.position, child.position);

            Gizmos.color = Color.green;

            Gizmos.DrawLine(bone.position, child.position);

            Vector3 midPoint = (bone.position + child.position) * 0.5f;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(midPoint, length.ToString("F3"));
#endif
        }

        foreach (Transform childBone in bone)
        {
            DrawRecursive(childBone);
        }
    }

    private Transform GetFirstPhysicsChild(Transform bone)
    {
        foreach (Transform child in bone)
        {
            if (IsPhysicsBone(child))
            {
                return child;
            }
        }

        return null;
    }

    private bool IsPhysicsBone(Transform bone)
    {
        string cleanName = bone.name.Replace("_Clone", "").ToLower();

        return
            cleanName == "mixamorig:head" ||

            cleanName.Contains("hips") ||
            cleanName.Contains("spine") ||

            cleanName == "mixamorig:leftarm" ||
            cleanName == "mixamorig:rightarm" ||

            cleanName == "mixamorig:leftforearm" ||
            cleanName == "mixamorig:rightforearm" ||

            cleanName == "mixamorig:lefthand" ||
            cleanName == "mixamorig:righthand" ||

            cleanName == "mixamorig:leftupleg" ||
            cleanName == "mixamorig:rightupleg" ||

            cleanName == "mixamorig:leftleg" ||
            cleanName == "mixamorig:rightleg" ||

            cleanName == "mixamorig:leftfoot" ||
            cleanName == "mixamorig:rightfoot";
    }
}