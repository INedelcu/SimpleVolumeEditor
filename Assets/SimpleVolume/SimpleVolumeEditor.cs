#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(SimpleVolume))]
public class SimpleVolumeEditor : Editor
{
    private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

    internal static Color kGizmoHandleColor = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);
    internal static Color kGizmoBoxColor = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x80 / 255f);

    public static GUIContent sizeText = EditorGUIUtility.TrTextContent("Box Size", "The size of the Volume.");
    public static GUIContent offsetText = EditorGUIUtility.TrTextContent("Box Offset", "The offset relative to the Game Object's Transform.");

    SerializedProperty m_Size;
    SerializedProperty m_Offset;

    public void OnEnable()
    {
        m_Size = serializedObject.FindProperty("m_Size");
        m_Offset = serializedObject.FindProperty("m_Offset");

        m_BoundsHandle.handleColor = kGizmoHandleColor;
        m_BoundsHandle.wireframeColor = Color.clear;
    }

    static Matrix4x4 GetLocalSpace(SimpleVolume volume)
    {
        Vector3 t = volume.transform.position;
        return Matrix4x4.TRS(t, Quaternion.identity, Vector3.one);
    }

    private bool ValidateAABB(ref Vector3 center, ref Vector3 size)
    {
        SimpleVolume volume = (SimpleVolume)target;

        Matrix4x4 localSpace = GetLocalSpace(volume);
        Vector3 localTransformPosition = localSpace.inverse.MultiplyPoint3x4(volume.transform.position);

        Bounds b = new Bounds(center, size);

        if (b.Contains(localTransformPosition))
            return false;

        b.Encapsulate(localTransformPosition);

        center = b.center;
        size = b.size;

        return true;
    }

    [DrawGizmo(GizmoType.Active)]
    static void RenderBoxGizmo(SimpleVolume volume, GizmoType gizmoType)
    {
        Color oldColor = Gizmos.color;
        Gizmos.color = kGizmoBoxColor;
        Gizmos.matrix = GetLocalSpace(volume);
        Gizmos.DrawCube(volume.m_Offset, -1f * volume.m_Size);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = oldColor;
    }

    [DrawGizmo(GizmoType.Selected)]
    static void RenderBoxOutline(SimpleVolume volume, GizmoType gizmoType)
    {
        Color oldColor = Gizmos.color;
        Gizmos.color = kGizmoBoxColor;
        Gizmos.matrix = GetLocalSpace(volume);
        Gizmos.DrawWireCube(volume.m_Offset, volume.m_Size);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = oldColor;
    }

    public void OnSceneGUI()
    {
        SimpleVolume volume = (SimpleVolume)target;

        using (new Handles.DrawingScope(GetLocalSpace(volume)))
        {
            m_BoundsHandle.center = volume.m_Offset;
            m_BoundsHandle.size = volume.m_Size;

            EditorGUI.BeginChangeCheck();
            m_BoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 center = m_BoundsHandle.center;
                Vector3 size = m_BoundsHandle.size;
                ValidateAABB(ref center, ref size);
                volume.m_Offset = center;
                volume.m_Size = size;
                EditorUtility.SetDirty(target);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(m_Size, sizeText);
        EditorGUILayout.PropertyField(m_Offset, offsetText);

        if (EditorGUI.EndChangeCheck())
        {
            Vector3 center = m_Offset.vector3Value;
            Vector3 size = m_Size.vector3Value;
            if (ValidateAABB(ref center, ref size))
            {
                m_Offset.vector3Value = center;
                m_Size.vector3Value = size;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

#endif