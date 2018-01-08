using UnityEngine;
using System.Linq;
using UnityEditor;

public class FlipAxes : AssetPostprocessor
{
    void OnPostprocessModel(GameObject g)
    {
       var mesh = g.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
        mesh.vertices = mesh.vertices.Select(v => new Vector3(-v.x, v.z, v.y)).ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        Debug.Log("Mesh inverted");
    }
}