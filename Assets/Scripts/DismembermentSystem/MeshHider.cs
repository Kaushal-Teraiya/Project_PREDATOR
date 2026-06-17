using System.Collections.Generic;
using UnityEngine;

public class MeshHider : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    [SerializeField] private string severBoneName;
    private Transform dismemberedBone;
    private Mesh originalMesh;
    private Mesh runtimeMesh;


    public void HideLimb(Transform bone)
    {
        if (bone == null)
        {
            Debug.LogError("Bone is null");
            return;
        }

        dismemberedBone = bone;

        Debug.Log($"Hiding {dismemberedBone.name}");

        if (originalMesh == null)
        {
            originalMesh = originalSkinnedMeshRenderer.sharedMesh;
        }

        Mesh sourceMesh = originalSkinnedMeshRenderer.sharedMesh;

        Mesh newMesh = Instantiate(sourceMesh);
        HashSet<int> severedBoneIndices = new(GetLimbBoneIndices());

        BoneWeight[] boneWeights = newMesh.boneWeights;

        HashSet<int> dismemberedVertices = new();

        for (int i = 0; i < boneWeights.Length; i++)
        {
            BoneWeight bw = boneWeights[i];

            bool vertexBelongs =
                severedBoneIndices.Contains(bw.boneIndex0) ||
                severedBoneIndices.Contains(bw.boneIndex1) ||
                severedBoneIndices.Contains(bw.boneIndex2) ||
                severedBoneIndices.Contains(bw.boneIndex3);

            if (vertexBelongs)
            {
                dismemberedVertices.Add(i);
            }
        }

        int[] triangleVertices = newMesh.triangles;

        List<int> remainingTrianglesVertices = new();

        for (int i = 0; i < triangleVertices.Length; i += 3)
        {
            int a = triangleVertices[i];
            int b = triangleVertices[i + 1];
            int c = triangleVertices[i + 2];

            bool removeTriangle = dismemberedVertices.Contains(a) && dismemberedVertices.Contains(b) && dismemberedVertices.Contains(c);

            if (!removeTriangle)
            {
                remainingTrianglesVertices.Add(a);
                remainingTrianglesVertices.Add(b);
                remainingTrianglesVertices.Add(c);
            }
        }

        newMesh.triangles = remainingTrianglesVertices.ToArray();
        newMesh.RecalculateBounds();

        originalSkinnedMeshRenderer.sharedMesh = newMesh;

        Debug.Log($"Removed Vertices: {dismemberedVertices.Count}");
        Debug.Log($"Remaining Triangles: {remainingTrianglesVertices.Count / 3}");
    }

    [ContextMenu("Restore Original Mesh")]
    public void RestoreOriginalMesh()
    {
        Destroy(runtimeMesh);

        runtimeMesh = Instantiate(originalMesh);

        originalSkinnedMeshRenderer.sharedMesh = runtimeMesh;
    }

    private List<int> GetLimbBoneIndices()
    {
        List<int> result = new();

        if (dismemberedBone == null)
        {
            Debug.LogError($"Bone not found: {severBoneName}");
            return result;
        }

        CollectBoneIndices(dismemberedBone, result);

        return result;
    }

    private void CollectBoneIndices(Transform dismemberedBone, List<int> result)
    {
        int boneIndex = System.Array.IndexOf(originalSkinnedMeshRenderer.bones, dismemberedBone);

        if (boneIndex >= 0)
        {
            result.Add(boneIndex);
        }

        foreach (Transform childBone in dismemberedBone)
        {
            CollectBoneIndices(childBone, result);
        }
    }

    public void InitializeMeshHider(SkinnedMeshRenderer renderer)
    {
        originalSkinnedMeshRenderer = renderer;
        originalMesh = renderer.sharedMesh;
    }
}