using System.Collections.Generic;
using UnityEngine;

public class TriangleDebugger : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer smr;

    private void Start()
    {
        Mesh mesh = smr.sharedMesh;

        HashSet<int> severedBoneIndices = new()
        {
            63,
            64,
            65,
            66
        };

        HashSet<int> severedVertices = new();

        BoneWeight[] weights = mesh.boneWeights;

        for (int i = 0; i < weights.Length; i++)
        {
            BoneWeight bw = weights[i];

            bool belongs =
                severedBoneIndices.Contains(bw.boneIndex0) ||
                severedBoneIndices.Contains(bw.boneIndex1) ||
                severedBoneIndices.Contains(bw.boneIndex2) ||
                severedBoneIndices.Contains(bw.boneIndex3);

            if (belongs) severedVertices.Add(i);
        }

        int[] triangles = mesh.triangles;

        int affectedTriangles = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            if (severedVertices.Contains(a) && severedVertices.Contains(b) && severedVertices.Contains(c))
            {
                affectedTriangles++;
            }
        }

        Debug.Log($"Affected Vertices: {severedVertices.Count}");
        Debug.Log($"Affected Triangles: {affectedTriangles}");
    }
}