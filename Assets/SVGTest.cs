using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SVGMeshUnity;

public class SVGTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var mesh = GetComponent<SVGMesh>();
        var svg = new SVGData();
        svg.Path("M256.5,31.2L13.7,451.7h485.6L256.5,31.2z M232.9,180L336,410.7H129.8L232.9,180z");
        mesh.Fill(svg);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
