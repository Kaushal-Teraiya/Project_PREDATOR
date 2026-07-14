using UnityEngine;

public class LimbHitBoxGenerator : MonoBehaviour
{
    [ContextMenu("Generate HitBoxes")]
    public void GenerateHitBoxes()
    {
        RemoveExistingHitBoxes();
        foreach (Transform child in transform)
        {
            RecursiveHitBoxGenerate(child);
        }
    }

    private void RecursiveHitBoxGenerate(Transform bone)
    {
        if (BoneColliderUtility.IsValidBone(bone))
        {
            Collider collider = BoneColliderUtility.CreateCollider(transform, bone);
            collider.isTrigger = false;
            LimbHitBox hitBox = BoneColliderUtility.GetOrAdd<LimbHitBox>(bone.gameObject);
            hitBox.bone = bone;
            hitBox.dismemberment = GetComponent<MeshExtractor>();
        }

        foreach (Transform child in bone)
        {
            RecursiveHitBoxGenerate(child);
        }
    }
    private void RemoveExistingHitBoxes()
    {
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            DestroyImmediate(collider);
        }

        foreach (var LimbHitBox in GetComponentsInChildren<LimbHitBox>())
        {
            DestroyImmediate(LimbHitBox);
        }
    }
}
