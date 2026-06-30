using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(BoneVisualizer))]
public class BoneVisualizerEditor : Editor
{
    private Mesh bakedMesh;

    private void OnSceneGUI()
    {
        BoneVisualizer vis = (BoneVisualizer)target;

        if (vis.smr == null)
            return;

        if (bakedMesh == null)
            bakedMesh = new Mesh();

        vis.smr.BakeMesh(bakedMesh);

        Mesh sourceMesh = vis.smr.sharedMesh;

        Transform[] bones = vis.smr.bones;
        BoneWeight[] weights = sourceMesh.boneWeights;
        Vector3[] vertices = bakedMesh.vertices;

        //================ DRAW WHOLE SKELETON =================

        Handles.color = Color.yellow;

        foreach (Transform bone in bones)
        {
            if (bone.parent != null)
            {
                Handles.DrawAAPolyLine(15f,
                    bone.position,
                    bone.parent.position);
            }

            Handles.SphereHandleCap(
                0,
                bone.position,
                Quaternion.identity,
                HandleUtility.GetHandleSize(bone.position) * 0.03f,
                EventType.Repaint);
        }
    }
}