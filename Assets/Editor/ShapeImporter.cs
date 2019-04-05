using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VectorGraphics;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using ShapeGraphics;

[Serializable]
[ScriptedImporter(1, "shape")]
public class ShapeImporter : ScriptedImporter
{
    /// <summary>The number of pixels per Unity units.</summary>
    public float SvgPixelsPerUnit
    {
        get { return m_SvgPixelsPerUnit; }
        set { m_SvgPixelsPerUnit = value; }
    }
    [SerializeField] private float m_SvgPixelsPerUnit = 100.0f;

    /// <summary>Preserves the viewport defined in the SVG document.</summary>
    public bool PreserveViewport
    {
        get { return m_PreserveViewport; }
        set { m_PreserveViewport = value; }
    }
    [SerializeField] private bool m_PreserveViewport;

    /// <summary>Use advanced settings.</summary>
    public bool AdvancedMode
    {
        get { return m_AdvancedMode; }
        set { m_AdvancedMode = value; }
    }
    [SerializeField] private bool m_AdvancedMode;

    /// <summary>The predefined resolution used, when not in advanced mode.</summary>
    public int PredefinedResolutionIndex
    {
        get { return m_PredefinedResolutionIndex; }
        set { m_PredefinedResolutionIndex = value; }
    }
    [SerializeField] private int m_PredefinedResolutionIndex = 1;

    /// <summary>The target resolution on which this SVG is displayed.</summary>
    public int TargetResolution
    {
        get { return m_TargetResolution; }
        set { m_TargetResolution = value; }
    }
    [SerializeField] private int m_TargetResolution = 1080;

    /// <summary>An additional scale factor on the target resolution.</summary>
    public float ResolutionMultiplier
    {
        get { return m_ResolutionMultiplier; }
        set { m_ResolutionMultiplier = value; }
    }
    [SerializeField] private float m_ResolutionMultiplier = 1.0f;

    /// <summary>The uniform step distance used for tessellation.</summary>
    public float StepDistance
    {
        get { return m_StepDistance; }
        set { m_StepDistance = value; }
    }
    [SerializeField] private float m_StepDistance = 10.0f;

    /// <summary>Number of samples evaluated on paths.</summary>
    public float SamplingStepDistance
    {
        get { return m_SamplingStepDistance; }
        set { m_SamplingStepDistance = value; }
    }
    [SerializeField] private float m_SamplingStepDistance = 100.0f;

    /// <summary>Enables the "max coord deviation" constraint.</summary>
    public bool MaxCordDeviationEnabled
    {
        get { return m_MaxCordDeviationEnabled; }
        set { m_MaxCordDeviationEnabled = value; }
    }
    [SerializeField] private bool m_MaxCordDeviationEnabled = false;

    /// <summary>Distance on the cord to a straight line between two points after which more tessellation will be generated.</summary>
    public float MaxCordDeviation
    {
        get { return m_MaxCordDeviation; }
        set { m_MaxCordDeviation = value; }
    }
    [SerializeField] private float m_MaxCordDeviation = 1.0f;

    /// <summary>Enables the "max tangent angle" constraint.</summary>
    public bool MaxTangentAngleEnabled
    {
        get { return m_MaxTangentAngleEnabled; }
        set { m_MaxTangentAngleEnabled = value; }
    }
    [SerializeField] private bool m_MaxTangentAngleEnabled = false;

    /// <summary>Max tangent angle (in degrees) after which more tessellation will be generated.</summary>
    public float MaxTangentAngle
    {
        get { return m_MaxTangentAngle; }
        set { m_MaxTangentAngle = value; }
    }
    [SerializeField] private float m_MaxTangentAngle = 5.0f;

    internal enum PredefinedResolution
    {
        Res_2160p,
        Res_1080p,
        Res_720p,
        Res_480p,
        Custom
    }

    public override void OnImportAsset(AssetImportContext ctx)
    {
        // We're using a hardcoded window size of 100x100. This way, using a pixels per point value of 100
        // results in a sprite of size 1 when the SVG file has a viewbox specified.
        SVGParser.SceneInfo sceneInfo;
        using (StreamReader stream = new StreamReader(ctx.assetPath))
        {
            sceneInfo = SVGParser.ImportSVG(stream, 0, 1, 100, 100, PreserveViewport);
        }

        if (sceneInfo.Scene == null || sceneInfo.Scene.Root == null)
        {
            throw new Exception("Wowzers!");
        }

        float stepDist = StepDistance;
        float samplingStepDist = SamplingStepDistance;
        float maxCord = MaxCordDeviationEnabled ? MaxCordDeviation : float.MaxValue;
        float maxTangent = MaxTangentAngleEnabled ? MaxTangentAngle : Mathf.PI * 0.5f;

        if (!AdvancedMode)
        {
            // Automatically compute sensible tessellation options from the
            // vector scene's bouding box and target resolution
            ComputeTessellationOptions(sceneInfo, TargetResolution, ResolutionMultiplier, out stepDist, out maxCord, out maxTangent);
        }

        var tessOptions = new ShapeUtils.TessellationOptions();
        tessOptions.MaxCordDeviation = maxCord;
        tessOptions.MaxTanAngleDeviation = maxTangent;
        tessOptions.SamplingStepSize = 1.0f / (float)samplingStepDist;
        tessOptions.StepDistance = stepDist;

        var rect = Rect.zero;
        if (PreserveViewport)
            rect = sceneInfo.SceneViewport;

        var geometry = ShapeUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);

        string name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);

        var gameObject = new GameObject("Shape" + name, typeof(MeshFilter), typeof(MeshRenderer));

        var mesh = new Mesh();
        mesh.name = "Mesh" + name;
        CalculateSideExtrusion(mesh, geometry);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        var mat = new Material(Shader.Find("Standard"));
        mat.name = "Material" + name;

        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.GetComponent<MeshRenderer>().material = mat;

        ctx.AddObjectToAsset("shape", gameObject);
        ctx.AddObjectToAsset("mesh", mesh);
        ctx.AddObjectToAsset("material", mat);
        ctx.SetMainObject(gameObject);
    }

    Mesh CalculateSideExtrusion(Mesh mesh, List<ShapeUtils.Geometry> geometries)
    {
        var mat4 = Matrix4x4.Scale(new Vector3(1, -1, 1) * 0.005f);

        var verticesAll = Enumerable.Empty<Vector3>();
        var indicesAll = Enumerable.Empty<int>();

        foreach (var geo in geometries)
        //var geo = geometries[0];
        {
            var vertices = Array.ConvertAll<Vector2, Vector3>(geo.Vertices, vec => mat4.MultiplyVector(vec));
            var indices = Array.ConvertAll<ushort, int>(geo.Indices, i => i);
            var paths = Array.ConvertAll<Vector2[], Vector3[]>(geo.Paths, p => Array.ConvertAll<Vector2, Vector3>(p, vec => mat4.MultiplyVector(vec)));

            bool convex = paths.Count() > 1;

            var vertices0 = convex ? vertices.Select(vec => new Vector3(vec.x, vec.y, vec.z + 10)) : vertices.AsEnumerable();
            var vertices1 = !convex ? vertices.Select(vec => new Vector3(vec.x, vec.y, vec.z + 10)) : vertices.AsEnumerable();

            int count0 = vertices0.Count();
            int count1 = count0 + vertices1.Count();

            var indices0 = indices.AsEnumerable();
            var indices1 = indices.Reverse().Select(i => i + count0);

            var vertices2 = new List<Vector3>();
            int count2;
            var indices2 = new List<int>();

            foreach (var path in paths)
            {
                count2 = count1 + vertices2.Count();

                var vertices20 = convex ? path.AsEnumerable() : path.Select(vec => new Vector3(vec.x, vec.y, vec.z + 10));
                var vertices21 = !convex ? path.AsEnumerable() : path.Select(vec => new Vector3(vec.x, vec.y, vec.z + 10));

                var count20 = vertices20.Count();

                for (int i = 0; i < count20; i++)
                {
                    int i1 = Repeat(i, 0, count20);
                    int i2 = Repeat(i + 1, 0, count20);
                    int i3 = i1 + count20;
                    int i4 = i2 + count20;

                    indices2.Add(i4 + count2);
                    indices2.Add(i3 + count2);
                    indices2.Add(i1 + count2);

                    indices2.Add(i2 + count2);
                    indices2.Add(i4 + count2);
                    indices2.Add(i1 + count2);
                }

                vertices2.AddRange(vertices20.Concat(vertices21));
            }

            int countAll = verticesAll.Count();
            verticesAll = verticesAll.Concat(vertices0).Concat(vertices1).Concat(vertices2);
            indicesAll = indicesAll.Concat(indices0.Concat(indices1).Concat(indices2).Select(i => i + countAll));
        }

        mesh.vertices = verticesAll.ToArray();
        mesh.triangles = indicesAll.ToArray();
        mesh.uv = verticesAll.Select(e => new Vector2()).ToArray();

        return mesh;
    }

    int Repeat(int x, int w)
    {
        return ((x % w) + w) % w;
    }

    int Repeat(int x, int min, int max)
    {
        return Repeat(x - min, max - min) + min;
    }

    void ComputeTessellationOptions(SVGParser.SceneInfo sceneInfo, int targetResolution, float multiplier, out float stepDist, out float maxCord, out float maxTangent)
    {
        var bbox = VectorUtils.ApproximateSceneNodeBounds(sceneInfo.Scene.Root);
        float maxDim = Mathf.Max(bbox.width, bbox.height) / SvgPixelsPerUnit;

        // The scene ratio gives a rough estimate of coverage % of the vector scene on the screen.
        // Higher values should result in a more dense tessellation.
        float sceneRatio = maxDim / (targetResolution * multiplier);

        stepDist = float.MaxValue; // No need for uniform step distance
        maxCord = Mathf.Max(0.01f, 75.0f * sceneRatio);
        maxTangent = Mathf.Max(0.1f, 100.0f * sceneRatio);
    }
}