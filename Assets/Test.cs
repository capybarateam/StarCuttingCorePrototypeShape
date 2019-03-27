using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

public class Test : MonoBehaviour
{
    public System.IO.TextReader svg;

    // Start is called before the first frame update
    void Start()
    {
        SVGParser.ImportSVG(svg, 0, 1, 100, 100, false);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
