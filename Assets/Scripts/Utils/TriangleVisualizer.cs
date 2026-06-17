using System.Collections.Generic;
using UnityEngine;

public class TriangleVisualizer : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer smr;

    private readonly List<Vector3> debugVertices = new();
    private readonly List<(Vector3 a, Vector3 b, Vector3 c)> debugTriangles = new();

    private void Start()
    {
        debugVertices.Clear();
        debugTriangles.Clear();

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

        HashSet<int> severedVertices = new();

        // Find vertices influenced by the lower leg branch
        for (int i = 0; i < weights.Length; i++)
        {
            BoneWeight bw = weights[i];

            bool belongs =
                severedBoneIndices.Contains(bw.boneIndex0) ||
                severedBoneIndices.Contains(bw.boneIndex1) ||
                severedBoneIndices.Contains(bw.boneIndex2) ||
                severedBoneIndices.Contains(bw.boneIndex3);

            if (belongs)
            {
                severedVertices.Add(i);
                debugVertices.Add(smr.transform.TransformPoint(vertices[i]));
            }
        }

        // Find triangles made entirely from severed vertices
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

                // Draw every 10th triangle to reduce clutter
                if (affectedTriangles % 10 == 0)
                {
                    debugTriangles.Add((
                        smr.transform.TransformPoint(vertices[a]),
                        smr.transform.TransformPoint(vertices[b]),
                        smr.transform.TransformPoint(vertices[c])
                    ));
                }
            }
        }

        Debug.Log($"Affected Vertices: {severedVertices.Count}");
        Debug.Log($"Affected Triangles: {affectedTriangles}");
    }

    private void OnDrawGizmos()
    {
        // Draw vertices
        Gizmos.color = Color.red;

        foreach (Vector3 point in debugVertices)
        {
            Gizmos.DrawSphere(point, 0.01f);
        }

        // Draw triangle wireframes
        Gizmos.color = Color.green;

        foreach (var tri in debugTriangles)
        {
            Gizmos.DrawLine(tri.a, tri.b);
            Gizmos.DrawLine(tri.b, tri.c);
            Gizmos.DrawLine(tri.c, tri.a);
        }
    }
}