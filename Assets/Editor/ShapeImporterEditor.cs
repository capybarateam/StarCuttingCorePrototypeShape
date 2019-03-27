using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[CustomEditor(typeof(ShapeImporter))]
[CanEditMultipleObjects]
internal class ShapeImporterEditor : ScriptedImporterEditor
{
    private enum SettingsType
    {
        Basic,
        Advanced
    }

    private SerializedProperty m_PixelsPerUnit;
    private SerializedProperty m_PreserveViewport;
    private SerializedProperty m_AdvancedMode;
    private SerializedProperty m_StepDistance;
    private SerializedProperty m_SamplingStepDistance;
    private SerializedProperty m_PredefinedResolutionIndex;
    private SerializedProperty m_TargetResolution;
    private SerializedProperty m_ResolutionMultiplier;
    private SerializedProperty m_MaxCordDeviationEnabled;
    private SerializedProperty m_MaxCordDeviation;
    private SerializedProperty m_MaxTangentAngleEnabled;
    private SerializedProperty m_MaxTangentAngle;

    private readonly GUIContent m_PixelsPerUnitText = new GUIContent("Pixels Per Unit", "How many pixels in the SVG correspond to one unit in the world.");
    private readonly GUIContent m_PreserveViewportText = new GUIContent("Preserve Viewport", "Preserve the viewport defined in the SVG document");
    private readonly GUIContent m_SettingsText = new GUIContent("Tessellation Settings");
    private readonly GUIContent m_TargetResolutionText = new GUIContent("Target Resolution", "Target resolution below which the sprite will not look tessellated.");
    private readonly GUIContent m_CustomTargetResolutionText = new GUIContent("Custom Target Resolution");
    private readonly GUIContent m_ResolutionMultiplierText = new GUIContent("Zoom Factor", "Target zoom factor for which the SVG asset should not look tessellated.");
    private readonly GUIContent m_StepDistanceText = new GUIContent("Step Distance", "Distance at which vertices will be generated along the paths. Lower values will result in a more dense tessellation.");
    private readonly GUIContent m_SamplingStepDistanceText = new GUIContent("Sampling Steps", "Number of samples evaluated on paths. Higher values give more accurate results (but takes longer).");
    private readonly GUIContent m_MaxCordDeviationEnabledText = new GUIContent("Max Cord Enabled", "Enables the \"max cord deviation\" tessellation test.");
    private readonly GUIContent m_MaxCordDeviationText = new GUIContent("Max Cord Deviation", "Distance on the cord to a straight line between two points after which more tessellation will be generated.");
    private readonly GUIContent m_MaxTangentAngleEnabledText = new GUIContent("Max Tangent Enabled", "Enables the \"max tangent angle\" tessellation test.");
    private readonly GUIContent m_MaxTangentAngleText = new GUIContent("Max Tangent Angle", "Max tangent angle (in degrees) after which more tessellation will be generated.");

    private readonly GUIContent[] m_SettingOptions = new GUIContent[]
    {
            new GUIContent("Basic"),
            new GUIContent("Advanced")
    };

    private readonly GUIContent[] m_TargetResolutionOptions = new GUIContent[]
    {
            new GUIContent("2160p (4K)"),
            new GUIContent("1080p"),
            new GUIContent("720p"),
            new GUIContent("480p"),
            new GUIContent("Custom")
    };

    public readonly GUIContent[] m_WrapModeContents =
    {
            new GUIContent("Repeat"),
            new GUIContent("Clamp"),
            new GUIContent("Mirror"),
            new GUIContent("Mirror Once")
        };

    public readonly int[] m_WrapModeValues =
    {
            (int)TextureWrapMode.Repeat,
            (int)TextureWrapMode.Clamp,
            (int)TextureWrapMode.Mirror,
            (int)TextureWrapMode.MirrorOnce
        };

    public readonly GUIContent[] m_FilterModeContents =
    {
            new GUIContent("Point"),
            new GUIContent("Bilinear"),
            new GUIContent("Trilinear")
        };

    public readonly int[] m_FilterModeValues =
    {
            (int)FilterMode.Point,
            (int)FilterMode.Bilinear,
            (int)FilterMode.Trilinear
        };

    public readonly GUIContent[] m_SampleCountContents =
    {
            new GUIContent("None"),
            new GUIContent("2 samples"),
            new GUIContent("4 samples"),
            new GUIContent("8 samples")
        };

    public readonly int[] m_SampleCountValues =
    {
            1,
            2,
            4,
            8
        };

    public override void OnEnable()
    {
        m_PixelsPerUnit = serializedObject.FindProperty("m_SvgPixelsPerUnit");
        m_PreserveViewport = serializedObject.FindProperty("m_PreserveViewport");
        m_AdvancedMode = serializedObject.FindProperty("m_AdvancedMode");
        m_PredefinedResolutionIndex = serializedObject.FindProperty("m_PredefinedResolutionIndex");
        m_TargetResolution = serializedObject.FindProperty("m_TargetResolution");
        m_ResolutionMultiplier = serializedObject.FindProperty("m_ResolutionMultiplier");
        m_StepDistance = serializedObject.FindProperty("m_StepDistance");
        m_SamplingStepDistance = serializedObject.FindProperty("m_SamplingStepDistance");
        m_MaxCordDeviationEnabled = serializedObject.FindProperty("m_MaxCordDeviationEnabled");
        m_MaxCordDeviation = serializedObject.FindProperty("m_MaxCordDeviation");
        m_MaxTangentAngleEnabled = serializedObject.FindProperty("m_MaxTangentAngleEnabled");
        m_MaxTangentAngle = serializedObject.FindProperty("m_MaxTangentAngle");
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_PixelsPerUnit, m_PixelsPerUnitText);

        IntToggle(m_PreserveViewport, m_PreserveViewportText);

        EditorGUILayout.Space();

        IntPopup(m_AdvancedMode, m_SettingsText, m_SettingOptions);

        ++EditorGUI.indentLevel;

        if (!m_AdvancedMode.hasMultipleDifferentValues)
        {
            if (m_AdvancedMode.boolValue)
            {
                PropertyField(m_StepDistance, m_StepDistanceText);
                PropertyField(m_SamplingStepDistance, m_SamplingStepDistanceText);

                IntToggle(m_MaxCordDeviationEnabled, m_MaxCordDeviationEnabledText);
                if (!m_MaxCordDeviationEnabled.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledScope(!m_MaxCordDeviationEnabled.boolValue))
                        PropertyField(m_MaxCordDeviation, m_MaxCordDeviationText);
                }

                IntToggle(m_MaxTangentAngleEnabled, m_MaxTangentAngleEnabledText);
                if (!m_MaxTangentAngleEnabled.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledScope(!m_MaxTangentAngleEnabled.boolValue))
                        PropertyField(m_MaxTangentAngle, m_MaxTangentAngleText);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = m_PredefinedResolutionIndex.hasMultipleDifferentValues;
                int resolutionIndex = EditorGUILayout.Popup(m_TargetResolutionText, m_PredefinedResolutionIndex.intValue, m_TargetResolutionOptions);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    m_PredefinedResolutionIndex.intValue = resolutionIndex;
                    if (m_PredefinedResolutionIndex.intValue != (int)ShapeImporter.PredefinedResolution.Custom)
                        m_TargetResolution.intValue = TargetResolutionFromPredefinedValue((ShapeImporter.PredefinedResolution)m_PredefinedResolutionIndex.intValue);
                }

                if (!m_PredefinedResolutionIndex.hasMultipleDifferentValues && m_PredefinedResolutionIndex.intValue == (int)ShapeImporter.PredefinedResolution.Custom)
                    PropertyField(m_TargetResolution, m_CustomTargetResolutionText);

                PropertyField(m_ResolutionMultiplier, m_ResolutionMultiplierText);
            }
        }

        --EditorGUI.indentLevel;

        EditorGUILayout.Space();

        ApplyRevertGUI();
    }

    protected override void Apply()
    {
        base.Apply();

        // Adjust every values to make sure they're in range
        foreach (var target in targets)
        {
            var ShapeImporter = target as ShapeImporter;
            ShapeImporter.SvgPixelsPerUnit = Mathf.Max(0.001f, ShapeImporter.SvgPixelsPerUnit);
            ShapeImporter.StepDistance = Mathf.Max(0.0f, ShapeImporter.StepDistance);
            ShapeImporter.SamplingStepDistance = Mathf.Clamp(ShapeImporter.SamplingStepDistance, 3.0f, 1000.0f);
            ShapeImporter.MaxCordDeviation = Mathf.Max(0.0f, ShapeImporter.MaxCordDeviation);
            ShapeImporter.MaxTangentAngle = Mathf.Clamp(ShapeImporter.MaxTangentAngle, 0.0f, 90.0f);
            ShapeImporter.TargetResolution = (int)Mathf.Max(1, ShapeImporter.TargetResolution);
            ShapeImporter.ResolutionMultiplier = Mathf.Clamp(ShapeImporter.ResolutionMultiplier, 1.0f, 100.0f);
        }
    }

    private void PropertyField(SerializedProperty prop, GUIContent label)
    {
        EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
        EditorGUILayout.PropertyField(prop, label);
        EditorGUI.showMixedValue = false;
    }

    private void IntField(SerializedProperty prop, GUIContent label, params GUILayoutOption[] options)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
        int value = EditorGUILayout.IntField(label, prop.intValue, options);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            prop.intValue = value;
    }

    private void IntPopup(SerializedProperty prop, GUIContent label, GUIContent[] displayedOptions)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
        int value = EditorGUILayout.Popup(label, prop.intValue, displayedOptions);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            prop.intValue = value;
    }

    private void IntPopup(SerializedProperty prop, GUIContent label, GUIContent[] displayedOptions, int[] options)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
        int value = EditorGUILayout.IntPopup(label, prop.intValue, displayedOptions, options);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            prop.intValue = value;
    }

    private void IntToggle(SerializedProperty prop, GUIContent label)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
        bool value = EditorGUILayout.Toggle(label, prop.boolValue);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            prop.boolValue = value;
    }

    private int TargetResolutionFromPredefinedValue(ShapeImporter.PredefinedResolution resolution)
    {
        switch (resolution)
        {
            case ShapeImporter.PredefinedResolution.Res_2160p: return 2160;
            case ShapeImporter.PredefinedResolution.Res_1080p: return 1080;
            case ShapeImporter.PredefinedResolution.Res_720p: return 720;
            case ShapeImporter.PredefinedResolution.Res_480p: return 480;
            default: return 1080;
        }
    }

    public override bool HasPreviewGUI()
    {
        return true;
    }

    public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
    {
        return null;
        //var sprite = ShapeImporter.GetImportedSprite(assetTarget);
        //if (sprite == null)
        //    return null;

        //return BuildPreviewTexture(sprite, width, height);
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        background.Draw(r, false, false, false, false);

        //var sprite = ShapeImporter.GetImportedSprite(assetTarget);
        //if (sprite == null)
        //{
        //    if (assetTarget is Texture2D)
        //        EditorGUI.DrawTextureTransparent(r, (Texture2D)assetTarget, ScaleMode.ScaleToFit, 0.0f, 0);
        //    return;
        //}

        //float zoomLevel = Mathf.Min(r.width / sprite.rect.width, r.height / sprite.rect.height);
        //Rect wantedRect = new Rect(r.x, r.y, sprite.rect.width * zoomLevel, sprite.rect.height * zoomLevel);
        //wantedRect.center = r.center;

        //var previewTex = BuildPreviewTexture(sprite, (int)wantedRect.width, (int)wantedRect.height);
        //if (previewTex != null)
        //{
        //    EditorGUI.DrawTextureTransparent(r, previewTex, ScaleMode.ScaleToFit);
        //    UnityEngine.Object.DestroyImmediate(previewTex);
        //}
    }

    //internal static Texture2D BuildPreviewTexture(Sprite sprite, int width, int height)
    //{
    //    return VectorUtils.RenderSpriteToTexture2D(sprite, width, height, ShapeImporter.GetSVGSpriteMaterial(sprite), 4);
    //}

    //public override string GetInfoString()
    //{
        //var sprite = ShapeImporter.GetImportedSprite(assetTarget);
        //if (sprite == null)
        //{
        //    var tex = assetTarget as Texture2D;
        //    if (tex == null)
        //        return "";
        //    return InternalBridge.GetTextureInfoString(tex);
        //}

        //int vertexCount = sprite.vertices.Length;
        //int indexCount = sprite.triangles.Length;

        //var stats = "" + vertexCount + " Vertices (Pos";

        //int vertexSize = sizeof(float) * 2;
        //if (sprite.HasVertexAttribute(VertexAttribute.Color))
        //{
        //    stats += ", Col";
        //    vertexSize += 4;
        //}
        //if (sprite.HasVertexAttribute(VertexAttribute.TexCoord0))
        //{
        //    stats += ", TexCoord0";
        //    vertexSize += sizeof(float) * 2;
        //}
        //if (sprite.HasVertexAttribute(VertexAttribute.TexCoord1))
        //{
        //    stats += ", TexCoord1";
        //    vertexSize += sizeof(float) * 2;
        //}
        //if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
        //{
        //    stats += ", TexCoord2";
        //    vertexSize += sizeof(float) * 2;
        //}

        //stats += ") " + HumanReadableSize(vertexSize * vertexCount + indexCount * 2);

        //return stats;
    //}

    //private static string HumanReadableSize(int bytes)
    //{
    //    var units = new string[] { "B", "KB", "MB", "GB", "TB" };

    //    int order = 0;
    //    while (bytes >= 2014 && order < units.Length - 1)
    //    {
    //        ++order;
    //        bytes /= 1024;
    //    }

    //    if (order >= units.Length)
    //        return "" + bytes;

    //    return String.Format("{0:0.#} {1}", bytes, units[order]);
    //}
}
