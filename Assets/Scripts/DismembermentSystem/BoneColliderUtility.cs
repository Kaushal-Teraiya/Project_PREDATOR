using UnityEngine;
public static class BoneColliderUtility
{
    public static Collider CreateCollider(Transform root, Transform bone)
    {
        string boneName = bone.name.ToLower();

        if (boneName == "mixamorig:head_clone".ToLower())
        {
            SphereCollider sphereCollider = GetOrAdd<SphereCollider>(bone.gameObject);

            Transform _child = bone.childCount > 0 ? bone.GetChild(0) : null;

            if (_child != null)
            {
                sphereCollider.radius = Vector3.Distance(bone.position, _child.position) * 0.35f;
            }
            else
            {
                sphereCollider.radius = 0.12f;
            }

            sphereCollider.radius = 0.12f;
            return sphereCollider;
        }

        if (boneName.Contains("spine") || boneName.Contains("hips"))
        {
            BoxCollider boxCollider = GetOrAdd<BoxCollider>(bone.gameObject);

            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(0.25f, 0.25f, 0.15f);

            return boxCollider;
        }

        Transform child = GetFirstPhysicsChild(bone);

        if (child == null)
        {
            if (boneName.Contains("hand"))
            {
                BoxCollider boxCollider = GetOrAdd<BoxCollider>(bone.gameObject);

                boxCollider.center = new Vector3(0f, 0.1f, 0f);
                boxCollider.size = new Vector3(0.1f, 0.1f, 0.02f);

                return boxCollider;
            }

            CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(bone.gameObject);

            capsule.direction = 1;
            capsule.height = 0.15f;
            capsule.radius = 0.04f;

            return capsule;
        }

        float length = Vector3.Distance(bone.position, child.position);

        Debug.Log($"{bone.name} -> {child.name} Length: {length}");
        Debug.Log($"{bone.name} Scale: {bone.lossyScale}");
        Debug.Log($"{bone.name} -> {child.name} Length: {length}");

        CapsuleCollider capsuleCollider = GetOrAdd<CapsuleCollider>(bone.gameObject);

        Vector3 localDir = bone.InverseTransformPoint(child.position);

        Vector3 absoluteDirection = new Vector3(Mathf.Abs(localDir.x), Mathf.Abs(localDir.y), Mathf.Abs(localDir.z));

        if (absoluteDirection.x > absoluteDirection.y && absoluteDirection.x > absoluteDirection.z)
        {
            capsuleCollider.direction = 0;
        }
        else if (absoluteDirection.y > absoluteDirection.z)
        {
            capsuleCollider.direction = 1;
        }
        else
        {
            capsuleCollider.direction = 2;
        }

        capsuleCollider.center = localDir * 0.5f;

        if (boneName.Contains("hand"))
        {
            capsuleCollider.center += localDir.normalized * 0.1f;
        }

        float scale = root.lossyScale.x;

        // 5.64 is the reference zombie scale
        float t = Mathf.Clamp01(scale / 5.64f);

        float heightDivisor = Mathf.Lerp(1.5f, 6f, t); //Proportional capsule collider sizing based on zombie scale and length between the bones.
        float radiusDivisor = Mathf.Lerp(8f, 26f, t);

        capsuleCollider.height = length / heightDivisor;
        capsuleCollider.radius = length / radiusDivisor;

        return capsuleCollider;
    }

    public static bool IsValidBone(Transform bone)
    {
        string cleanName = bone.name
                    .Replace("_Clone", "")
                    .ToLower();

        return cleanName == "mixamorig:head" ||
               cleanName.Contains("hips") ||
               cleanName.Contains("spine") ||
               cleanName == "mixamorig:neck" ||
               cleanName == "mixamorig:leftshoulder" ||
               cleanName == "mixamorig:rightshoulder" ||
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

    public static Transform GetFirstPhysicsChild(Transform bone)
    {
        foreach (Transform child in bone)
        {
            if (IsValidBone(child))
                return child;
        }

        return null;
    }

    public static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }
}