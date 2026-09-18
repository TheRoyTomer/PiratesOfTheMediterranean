using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ShorelineBoundaryPath))]
public sealed class ShorelineBoundaryPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Move numbered points in Scene View or edit the list. Regenerate explicitly after editing. Point Y is ignored; bottom/top define local vertical extent. Use a dedicated asset folder per boundary.", MessageType.Info);
        if (GUILayout.Button("Generate / Update Colliders"))
        {
            var path = (ShorelineBoundaryPath)target;
            if (string.IsNullOrEmpty(path.generatedAssetFolder))
                path.generatedAssetFolder = AssetDatabase.GenerateUniqueAssetPath("Assets/Environment/ShorelineBoundaries/" + path.GetInstanceID());
            Generate(path);
        }
    }

    void OnSceneGUI()
    {
        var p = (ShorelineBoundaryPath)target;
        for (int i = 0; i < p.controlPoints.Count; i++)
        {
            var world = p.transform.TransformPoint(p.controlPoints[i]);
            Handles.Label(world, i.ToString());
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(world, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(p, "Move shoreline point");
                var local = p.transform.InverseTransformPoint(moved); local.y = 0;
                p.controlPoints[i] = local;
                EditorUtility.SetDirty(p);
            }
            if (i > 0) Handles.DrawLine(p.transform.TransformPoint(p.controlPoints[i - 1]), world);
        }
        if (p.closed && p.controlPoints.Count > 2)
            Handles.DrawLine(p.transform.TransformPoint(p.controlPoints[0]), p.transform.TransformPoint(p.controlPoints[p.controlPoints.Count - 1]));
    }

    public static void Generate(ShorelineBoundaryPath p)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Editor-time generation only.");
        int count = p.controlPoints.Count;
        if (count < (p.closed ? 3 : 2) || p.top <= p.bottom || p.thickness <= 0 || p.edgesPerSection < 1)
            throw new InvalidOperationException("Invalid shoreline dimensions/path.");
        if (string.IsNullOrEmpty(p.generatedAssetFolder) || !p.generatedAssetFolder.StartsWith("Assets/" ) || p.generatedAssetFolder.Contains(".."))
            throw new InvalidOperationException("Choose a dedicated Assets folder.");
        int edges = p.closed ? count : count - 1;
        for (int i = 0; i < edges; i++)
            if (Vector3.ProjectOnPlane(p.controlPoints[(i + 1) % count] - p.controlPoints[i], Vector3.up).sqrMagnitude < .01f)
                throw new InvalidOperationException("Duplicate shoreline points.");
        Directory.CreateDirectory(p.generatedAssetFolder);
        AssetDatabase.Refresh();
        if (!p.generatedRoot)
        {
            var root = new GameObject("Generated_Collider_Sections");
            Undo.RegisterCreatedObjectUndo(root, "Create shoreline sections");
            root.transform.SetParent(p.transform, false); p.generatedRoot = root.transform;
        }
        for (int i = p.generatedRoot.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(p.generatedRoot.GetChild(i).gameObject);
        var back = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            var prev = p.controlPoints[p.closed ? (i + count - 1) % count : Mathf.Max(0, i - 1)];
            var next = p.controlPoints[p.closed ? (i + 1) % count : Mathf.Min(count - 1, i + 1)];
            var tangent = Vector3.ProjectOnPlane(next - prev, Vector3.up).normalized;
            back[i] = Vector3.Cross(Vector3.up, tangent) * (p.landOnRight ? 1 : -1) * p.thickness;
        }
        int section = 0;
        for (int start = 0; start < edges; start += p.edgesPerSection)
        {
            int length = Mathf.Min(p.edgesPerSection, edges - start);
            var v = new List<Vector3>(); var tris = new List<int>();
            for (int j = 0; j <= length; j++)
            {
                int index = (start + j) % count;
                var front = p.controlPoints[index]; front.y = 0;
                var rear = front + back[index];
                // Only rear corners extend across seams; the sea-facing edges match exactly.
                if (j == 0 || j == length)
                {
                    int a = j == 0 ? index : (index + count - 1) % count;
                    int b = j == 0 ? (index + 1) % count : index;
                    var tangent = Vector3.ProjectOnPlane(p.controlPoints[b] - p.controlPoints[a], Vector3.up).normalized;
                    rear += tangent * p.landSideOverlap * (j == 0 ? -1 : 1);
                }
                v.Add(front + Vector3.up * p.bottom); v.Add(front + Vector3.up * p.top);
                v.Add(rear + Vector3.up * p.bottom); v.Add(rear + Vector3.up * p.top);
            }
            for (int j = 0; j < length; j++)
            {
                int a = j * 4, b = a + 4;
                Quad(tris, a, b, b + 1, a + 1); Quad(tris, a + 2, a + 3, b + 3, b + 2);
                // Static non-convex walls do not need a closed volume. Leave top
                // and bottom open so neither can become a climbable contact floor.
            }
            Quad(tris, 0, 1, 3, 2); int end = length * 4; Quad(tris, end, end + 2, end + 3, end + 1);
            if (!p.landOnRight) for (int i = 0; i < tris.Count; i += 3) { int a = tris[i]; tris[i] = tris[i + 2]; tris[i + 2] = a; }
            string assetPath = p.generatedAssetFolder + "/Section_" + section.ToString("D2") + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, assetPath); }
            else Undo.RegisterCompleteObjectUndo(mesh, "Update boundary mesh");
            mesh.Clear(); mesh.name = "Shoreline_Section_" + section; mesh.SetVertices(v); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
            var go = new GameObject(mesh.name); Undo.RegisterCreatedObjectUndo(go, "Generate shoreline collider");
            go.transform.SetParent(p.generatedRoot, false); go.layer = p.gameObject.layer; go.isStatic = true;
            var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = false;
            collider.sharedMaterial = p.material; collider.isTrigger = false;
            section++;
        }
        EditorUtility.SetDirty(p); EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
    }

    static void Quad(List<int> t, int a, int b, int c, int d)
    { t.Add(a); t.Add(b); t.Add(c); t.Add(a); t.Add(c); t.Add(d); }
}
