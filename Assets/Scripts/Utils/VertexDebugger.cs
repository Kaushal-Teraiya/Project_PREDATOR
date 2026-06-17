using System.Collections.Generic;
using UnityEngine;

public class VertexDebugger : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer smr;

    private readonly List<Vector3> debugVertices = new();

    private void Start()
    {
        Mesh mesh = smr.sharedMesh;

        HashSet<int> severedBoneIndices = new()
        {
            63, // LeftLeg
            64, // LeftFoot
            65, // LeftToeBase
            66  // LeftToe_End
        };

        BoneWeight[] weights = mesh.boneWeights;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            BoneWeight bw = weights[i];

            bool belongs =
                severedBoneIndices.Contains(bw.boneIndex0) ||
                severedBoneIndices.Contains(bw.boneIndex1) ||
                severedBoneIndices.Contains(bw.boneIndex2) ||
                severedBoneIndices.Contains(bw.boneIndex3);

            if (belongs)
            {
                Vector3 worldPos = smr.transform.TransformPoint(vertices[i]);
                debugVertices.Add(worldPos);
            }
        }

        Debug.Log($"Found {debugVertices.Count} vertices");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (Vector3 point in debugVertices)
        {
            Gizmos.DrawSphere(point, 0.005f);
        }
    }
}