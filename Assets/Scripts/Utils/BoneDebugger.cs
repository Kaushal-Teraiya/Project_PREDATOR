using UnityEngine;

public class BoneDebugger : MonoBehaviour
{
    private void Start()
    {
        Animator animator = GetComponent<Animator>();

        foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone)
                continue;

            Transform t = animator.GetBoneTransform(bone);

            if (t != null) Debug.Log($"{bone} -> {t.name}");
        }
    }
}