using System.Collections.Generic;
using UnityEngine;

public class BoneHierarchyCloner : MonoBehaviour
{
    public Transform CloneDismemberedBoneHeirarchy(Transform currentBoneToDismember, out Transform[] clonedBones)
    {
        if (currentBoneToDismember == null)
        {
            Debug.LogError("Source root is null");
            clonedBones = null;
            return null;
        }

        List<Transform> clonedBoneList = new();

        Transform clonedRoot = CloneBonesRecursively(currentBoneToDismember, null, clonedBoneList);

        clonedBones = clonedBoneList.ToArray();

        return clonedRoot;
    }

    private Transform CloneBonesRecursively(Transform currentBoneToProcess, Transform parent, List<Transform> clonedBonesList)
    {
        GameObject dismemberedLimb = new(currentBoneToProcess.name + "_Clone");
        Transform cloneTransform = dismemberedLimb.transform;

        clonedBonesList.Add(cloneTransform);

        if (parent == null)
        {
            cloneTransform.position = currentBoneToProcess.position;
            cloneTransform.rotation = currentBoneToProcess.rotation;
            cloneTransform.localScale = currentBoneToProcess.lossyScale;
        }
        else
        {
            cloneTransform.SetParent(parent, false);
            cloneTransform.localPosition = currentBoneToProcess.localPosition;
            cloneTransform.localRotation = currentBoneToProcess.localRotation;
            cloneTransform.localScale = currentBoneToProcess.localScale;
        }

        foreach (Transform childBone in currentBoneToProcess)
        {
            if (childBone.GetComponent<Dismembered>() != null)
            {
                //Dont collect index for already dismembered Bones so they dont get cloned at all
                Debug.Log($"Skipping severed branch: {childBone.name}");
                continue;
            }

            CloneBonesRecursively(childBone, cloneTransform, clonedBonesList);
        }

        return cloneTransform;
    }
}