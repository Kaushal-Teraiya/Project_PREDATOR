// using UnityEngine;

// public class BoneWeightDebugger : MonoBehaviour
// {
//     [SerializeField] private SkinnedMeshRenderer smr;

//     private void Start()
//     {
//         Mesh mesh = smr.sharedMesh;

//         BoneWeight[] weights = mesh.boneWeights;

//         int targetBoneIndex = -1;

//         for (int i = 0; i < smr.bones.Length; i++)
//         {
//             if (smr.bones[i].name == "mixamorig:LeftLeg")
//             {
//                 targetBoneIndex = i;
//                 break;
//             }
//         }

//         Debug.Log($"LeftLeg Bone Index = {targetBoneIndex}");

//         int vertexCount = 0;

//         foreach (BoneWeight weight in weights)
//         {
//             bool belongs =
//                 weight.boneIndex0 == targetBoneIndex ||
//                 weight.boneIndex1 == targetBoneIndex ||
//                 weight.boneIndex2 == targetBoneIndex ||
//                 weight.boneIndex3 == targetBoneIndex;

//             if (belongs)
//                 vertexCount++;
//         }

//         Debug.Log($"Vertices influenced by LeftLeg: {vertexCount}");
//     }
// }
using UnityEngine;
using System.Collections.Generic;

public class BoneWeightDebugger : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer smr;

    private void Start()
    {
        Animator animator = GetComponent<Animator>();

        Transform leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);

        if (leftLowerLeg == null)
        {
            Debug.LogError("LeftLowerLeg bone not found!");
            return;
        }

        Debug.Log("===== Descendant Bones =====");

        List<Transform> limbBones = new();

        foreach (Transform bone in leftLowerLeg.GetComponentsInChildren<Transform>())
        {
            limbBones.Add(bone);
            Debug.Log(bone.name);
        }

        Debug.Log("===== Bone Indices =====");

        foreach (Transform limbBone in limbBones)
        {
            for (int i = 0; i < smr.bones.Length; i++)
            {
                if (smr.bones[i] == limbBone)
                {
                    Debug.Log($"{limbBone.name} -> Bone Index {i}");
                    break;
                }
            }
        }
    }
}