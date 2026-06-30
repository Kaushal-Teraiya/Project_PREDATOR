using System.Collections.Generic;
using UnityEngine;

public class MeshExtractor : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer originalSkinnedMeshRenderer;
    private Transform clonedRoot;
    private Transform latestDismemberedBone;
    [SerializeField] private string DismemberBoneNamee;
    private Transform[] DismemberedBones;


    [ContextMenu("Extract Limb")]
    public void ExtractLimb()
    {
        Debug.Log("EXTRACT CALLED");

        Mesh sourceMesh = originalSkinnedMeshRenderer.sharedMesh;

        Transform currentBoneToDismember = null;

        foreach (Transform bone in originalSkinnedMeshRenderer.bones)
        {
            if (bone.name == DismemberBoneNamee)
            {
                currentBoneToDismember = bone;
                break;
            }
        }

        if (currentBoneToDismember == null)
        {
            Debug.LogError($"Could not find bone: {DismemberBoneNamee}");
            return;
        }

        latestDismemberedBone = currentBoneToDismember;
        BoneHierarchyCloner cloner = GetComponent<BoneHierarchyCloner>();

        clonedRoot = cloner.CloneDismemberedBoneHeirarchy(currentBoneToDismember, out DismemberedBones);

        Debug.Log($"Detached Bones: {DismemberedBones.Length}");

        foreach (Transform bone in DismemberedBones)
        {
            Debug.Log(bone.name);
        }

        Debug.Log("========== MESH DEBUG ==========");

        Debug.Log($"Mesh Bounds Size: {sourceMesh.bounds.size}");
        Debug.Log($"Mesh Bounds Extents: {sourceMesh.bounds.extents}");
        Debug.Log($"Vertex Count: {sourceMesh.vertexCount}");

        Debug.Log("================================");

        Debug.Log($"BindPoses: {sourceMesh.bindposes.Length}");
        Debug.Log($"Bones: {originalSkinnedMeshRenderer.bones.Length}");
        Debug.Log($"BoneWeights: {sourceMesh.boneWeights.Length}");

        HashSet<int> dismemberedBoneIndices = new(GetDismemberedLimbBoneIndices());//collect each bone index from dismembered limb 
        Dictionary<int, int> boneRemap = new(); // to remap the dismembered limb bones starting from 0 , 1 ,2 ....

        int remapIndex = 0;

        foreach (int boneIndex in dismemberedBoneIndices)
        {
            boneRemap.Add(boneIndex, remapIndex);
            remapIndex++;
            /*
                remap::
                Before     After    //makes the limb be its own individual object by remapping bones from 0 root to "n" leaf of the limb
                 63         0
                 64         1
                 65         2
            */
        }

        Debug.Log("===== DISCOVERED BONES =====");

        foreach (int index in dismemberedBoneIndices)
        {
            Debug.Log($"{index} : {originalSkinnedMeshRenderer.bones[index].name}");
        }

        BoneWeight[] boneWeights = sourceMesh.boneWeights;

        HashSet<int> dismemberedVertices = new();

        for (int i = 0; i < boneWeights.Length; i++) // iterating through all the vertices of the whole character model.
        {
            /*
                    To finalize,  boneweight contains 2 things :: bone weight and bone index ;
                    Bone index is the bone that affects the given vertex ..
                    There are upto 4 supported bones that can influence any given vertex ;; weight of influence is divided accross them;
                    so , bool belongs = ..... determines if the given vertex from the WHOLE ASS MODEL is influenced by the bones that u have just decided to sever:)

                    for e.g , if boneIndex0 = foreArm , weight = 1   ;; boneIndex1.... boneIndex3 all weights 0 meaning the given vertex is influenced only by 1 bone which is foreArm
            */
            BoneWeight boneWeight = boneWeights[i];

            bool vertexBelongs =
                dismemberedBoneIndices.Contains(boneWeight.boneIndex0) ||
                dismemberedBoneIndices.Contains(boneWeight.boneIndex1) ||
                dismemberedBoneIndices.Contains(boneWeight.boneIndex2) ||
                dismemberedBoneIndices.Contains(boneWeight.boneIndex3);

            if (vertexBelongs)
            {
                dismemberedVertices.Add(i);
            }
        }

        Dictionary<int, int> vertexRemap = new();
        List<Vector3> newVertices = new();
        List<int> newTriangleVertices = new();
        List<Vector2> newUVs = new();
        List<Vector3> newNormals = new();
        List<BoneWeight> newBoneWeights = new();

        Vector3[] sourceMeshVertices = sourceMesh.vertices;
        Vector2[] sourceMeshUVs = sourceMesh.uv;
        Vector3[] sourceMeshNormals = sourceMesh.normals;
        int[] sourceMeshTriangleVertices = sourceMesh.triangles; // ! Note bro... this contains vertex of the triangles not triangles itself 

        for (int i = 0; i < sourceMeshTriangleVertices.Length; i += 3)
        {
            //so here you are accessing 3 vertices == 1 triangle
            int a = sourceMeshTriangleVertices[i];
            int b = sourceMeshTriangleVertices[i + 1];
            int c = sourceMeshTriangleVertices[i + 2];

            bool triangleBelongs = dismemberedVertices.Contains(a) && dismemberedVertices.Contains(b) && dismemberedVertices.Contains(c);

            if (!triangleBelongs)
                continue;

            int newA = GetOrCreateVertex(a);
            int newB = GetOrCreateVertex(b);
            int newC = GetOrCreateVertex(c);

            newTriangleVertices.Add(newA);
            newTriangleVertices.Add(newB);
            newTriangleVertices.Add(newC); // reIndexing of a given set of triangles of severed mesh.
        }

        HashSet<int> usedBones = new();

        foreach (BoneWeight boneWeight in newBoneWeights)
        {
            usedBones.Add(boneWeight.boneIndex0);
            usedBones.Add(boneWeight.boneIndex1);
            usedBones.Add(boneWeight.boneIndex2);
            usedBones.Add(boneWeight.boneIndex3);
        }


        foreach (int bone in usedBones)
        {
            Debug.Log($"Bone Index: {bone}");
        }

        Mesh extractedMesh = new Mesh();

        extractedMesh.vertices = newVertices.ToArray();
        extractedMesh.triangles = newTriangleVertices.ToArray();

        if (newUVs.Count == newVertices.Count) extractedMesh.uv = newUVs.ToArray();

        if (newNormals.Count == newVertices.Count) extractedMesh.normals = newNormals.ToArray();

        extractedMesh.boneWeights = newBoneWeights.ToArray();
        Matrix4x4[] bindposes = new Matrix4x4[DismemberedBones.Length];

        foreach (var pair in boneRemap)
        {
            int originalBoneIndex = pair.Key;
            int newBoneIndex = pair.Value;

            bindposes[newBoneIndex] = sourceMesh.bindposes[originalBoneIndex];
        }

        extractedMesh.bindposes = bindposes;
        extractedMesh.RecalculateBounds();

        for (int i = 0; i < DismemberedBones.Length; i++)
        {
            Debug.Log($"{DismemberedBones[i].name} : " + DismemberedBones[i].position);
        }

        SpawnExtractedMesh(extractedMesh);
        currentBoneToDismember.gameObject.AddComponent<Dismembered>();
        RemovePhysicsHierarchy(currentBoneToDismember);
        MeshHider hider = GetComponent<MeshHider>();

        if (hider != null)
        {
            hider.HideLimb(currentBoneToDismember);
        }

        Debug.Log($"Extracted Vertices: {newVertices.Count}");
        Debug.Log($"Extracted Triangles: {newTriangleVertices.Count / 3}");


        int GetOrCreateVertex(int originalIndex)
        {
            if (vertexRemap.TryGetValue(originalIndex, out int existing))
            {
                return existing;
            }

            int newIndex = newVertices.Count;

            vertexRemap.Add(originalIndex, newIndex);

            newVertices.Add(sourceMeshVertices[originalIndex]);

            BoneWeight boneWeight = sourceMesh.boneWeights[originalIndex];

            boneWeight.boneIndex0 = RemapBone(boneWeight.boneIndex0);
            boneWeight.boneIndex1 = RemapBone(boneWeight.boneIndex1);
            boneWeight.boneIndex2 = RemapBone(boneWeight.boneIndex2);
            boneWeight.boneIndex3 = RemapBone(boneWeight.boneIndex3);

            newBoneWeights.Add(boneWeight);

            if (sourceMeshUVs.Length > originalIndex)
            {
                newUVs.Add(sourceMeshUVs[originalIndex]);
            }

            if (sourceMeshNormals.Length > originalIndex)
            {
                newNormals.Add(sourceMeshNormals[originalIndex]);
            }

            return newIndex;
        }

        int RemapBone(int boneIndex)
        {
            if (boneRemap.TryGetValue(boneIndex, out int remapped))
            {
                return remapped;
            }

            return 0;
        }

    }

    public void DismemberBone(string boneName)
    {
        DismemberBoneNamee = boneName;
        ExtractLimb();
    }

    private void MarkSeveredHierarchy(Transform bone)
    {
        SeveredBoneRegistry.dismemberedBones.Add(bone);
        Debug.Log($"Marked Severed: {bone.name}");

        foreach (Transform child in bone)
        {
            MarkSeveredHierarchy(child);
        }
    }

    private void SpawnExtractedMesh(Mesh extractedMesh)
    {
        GameObject dismemberedLimb = new GameObject(DismemberBoneNamee + "_Dismembered");


        Debug.Log($"Zombie Transform Scale: {transform.localScale}");
        Debug.Log($"Zombie Lossy Scale: {transform.lossyScale}");

        Debug.Log($"SMR Transform Scale: {originalSkinnedMeshRenderer.transform.localScale}");
        Debug.Log($"SMR Lossy Scale: {originalSkinnedMeshRenderer.transform.lossyScale}");

        Debug.Log($"New Object Scale Before: {dismemberedLimb.transform.localScale}");


        dismemberedLimb.transform.position = transform.position + transform.right * 2f;
        dismemberedLimb.transform.localScale = transform.lossyScale;
        Debug.Log($"New Object Scale After: {dismemberedLimb.transform.localScale}");


        SkinnedMeshRenderer newSkinnedMeshRenderer = dismemberedLimb.AddComponent<SkinnedMeshRenderer>();
        MeshExtractor meshExtractor = dismemberedLimb.AddComponent<MeshExtractor>();
        newSkinnedMeshRenderer.sharedMesh = extractedMesh;
        dismemberedLimb.AddComponent<BoneHierarchyCloner>();
        meshExtractor.InitializeMeshExtractor(newSkinnedMeshRenderer);
        MeshHider meshHider = dismemberedLimb.AddComponent<MeshHider>();
        meshHider.InitializeMeshHider(newSkinnedMeshRenderer);

        newSkinnedMeshRenderer.bones = DismemberedBones;

        for (int i = 0; i < DismemberedBones.Length; i++)
        {
            if (DismemberedBones[i] == null)
            {
                Debug.LogError($"Bone {i} is NULL");
            }
            else
            {
                Debug.Log($"{i} -> {DismemberedBones[i].name}");
            }
        }

        newSkinnedMeshRenderer.rootBone = clonedRoot;

        newSkinnedMeshRenderer.material = new Material(originalSkinnedMeshRenderer.sharedMaterial);
        clonedRoot.SetParent(dismemberedLimb.transform, true);
        dismemberedLimb.AddComponent<Limb>();
    }

    private List<int> GetDismemberedLimbBoneIndices()
    {
        List<int> result = new();

        Transform startBone = null;

        foreach (Transform bone in originalSkinnedMeshRenderer.bones)
        {
            if (bone.name == DismemberBoneNamee)
            {
                startBone = bone;
                break;
            }
        }

        if (startBone == null)
        {
            Debug.LogError($"Bone not found: {DismemberBoneNamee}");
            return result;
        }

        CollectBoneIndices(startBone, result);

        return result;
    }

    public void InitializeMeshExtractor(SkinnedMeshRenderer renderer)
    {
        originalSkinnedMeshRenderer = renderer;
    }

    private void CollectBoneIndices(Transform currentBoneToProcess, List<int> result)
    {
        bool isDismembered = currentBoneToProcess.GetComponent<Dismembered>() != null;
        bool isNotSameBone = currentBoneToProcess != latestDismemberedBone;

        if (isNotSameBone && isDismembered)
        {
            //Skip adding index into the result so colliders and rigidbodies are not generated for these bones :)
            Debug.Log($"Skipping severed branch: {currentBoneToProcess.name}");
            return;
        }

        int currentBoneIndex = System.Array.IndexOf(originalSkinnedMeshRenderer.bones, currentBoneToProcess); // returns index of "startBone" inside the skinnedMeshRenderer.bones arrray

        if (currentBoneIndex >= 0)
        {
            result.Add(currentBoneIndex);
        }

        foreach (Transform childBone in currentBoneToProcess)
        {
            CollectBoneIndices(childBone, result);
        }
    }

    private void RemovePhysicsHierarchy(Transform currentBoneToProcess) //Start bone means the bone from which a limb is dismembered.
    {
        CharacterJoint joint = currentBoneToProcess.GetComponent<CharacterJoint>();

        if (joint) Destroy(joint);

        Collider collider = currentBoneToProcess.GetComponent<Collider>();

        if (collider) Destroy(collider);

        Rigidbody rigidBody = currentBoneToProcess.GetComponent<Rigidbody>();

        if (rigidBody) Destroy(rigidBody);

        foreach (Transform childBones in currentBoneToProcess)
        {
            RemovePhysicsHierarchy(childBones);
        }
    }

}