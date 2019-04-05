using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeClipper : MonoBehaviour
{
    public Mesh one;
    public Mesh another;

    void Start()
    {
        var one0 = new Net3dBool.Solid(
            VecToNet3dBool(one.vertices),
            one.triangles);
        var another0 = new Net3dBool.Solid(
            VecToNet3dBool(another.vertices),
            another.triangles);

        var modeller = new Net3dBool.BooleanModeller(one0, another0);
        var tmp = modeller.GetIntersection();

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = VecToUnity(tmp.getVertices());
        mesh.triangles = tmp.getIndices();
    }

    Net3dBool.Vector3[] VecToNet3dBool(Vector3[] vecs)
    {
        return Array.ConvertAll<Vector3, Net3dBool.Vector3>(vecs, vec => new Net3dBool.Vector3(vec.x, vec.y, vec.z));
    }

    Vector3[] VecToUnity(Net3dBool.Vector3[] vecs)
    {
        return Array.ConvertAll<Net3dBool.Vector3, Vector3>(vecs, vec => new Vector3((float)vec.x, (float)vec.y, (float)vec.z));
    }
}
