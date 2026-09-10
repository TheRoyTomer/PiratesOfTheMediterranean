using RenderWave.Runtime.Wakes;
using UnityEditor;
using UnityEngine;

namespace RenderWave.Editor
{
    [CustomEditor(typeof(WakeSystem))]
    internal sealed class WakeSystemEditor : UnityEditor.Editor
    {
        private SerializedProperty maxActiveWakesProperty;
        private SerializedProperty sourceMaterialProperty;
        private SerializedProperty wakeShaderProperty;
        private SerializedProperty wakeColorProperty;

        private void OnEnable()
        {
            maxActiveWakesProperty = serializedObject.FindProperty("maxActiveWakes");
            sourceMaterialProperty = serializedObject.FindProperty("sourceMaterial");
            wakeShaderProperty = serializedObject.FindProperty("wakeShader");
            wakeColorProperty = serializedObject.FindProperty("wakeColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("WakeSystem is a pooled scene-level renderer for wake strips. Keep one default system in the scene for the easiest V1 workflow.", MessageType.Info);

            var allSystems = Object.FindObjectsByType<WakeSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (allSystems.Length > 1)
            {
                EditorGUILayout.HelpBox("More than one WakeSystem exists in the scene. V1 auto-resolution expects one default system unless emitters are assigned manually.", MessageType.Warning);
            }

            if (sourceMaterialProperty.objectReferenceValue != null &&
                !RenderWaveEditorUtility.HasWakeMaterialContract((Material)sourceMaterialProperty.objectReferenceValue))
            {
                EditorGUILayout.HelpBox("The assigned wake material does not match the RenderWave wake shader contract.", MessageType.Error);
            }
            else if (sourceMaterialProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No wake material is assigned. The wake shader fallback works, but the packaged wake material is the intended default.", MessageType.None);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Assign Packaged Wake Material"))
                {
                    sourceMaterialProperty.objectReferenceValue = RenderWaveEditorUtility.LoadWakeMaterial();
                }

                if (GUILayout.Button("Assign Wake Shader"))
                {
                    wakeShaderProperty.objectReferenceValue = RenderWaveEditorUtility.LoadWakeShader();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(maxActiveWakesProperty, new GUIContent("Max Active Wakes"));
            EditorGUILayout.PropertyField(sourceMaterialProperty, new GUIContent("Source Material"));
            EditorGUILayout.PropertyField(wakeShaderProperty, new GUIContent("Fallback Shader"));
            EditorGUILayout.PropertyField(wakeColorProperty, new GUIContent("Wake Color"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
