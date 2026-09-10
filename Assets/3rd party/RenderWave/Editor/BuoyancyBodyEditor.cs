using RenderWave.Runtime.Buoyancy;
using UnityEditor;
using UnityEngine;

namespace RenderWave.Editor
{
    [CustomEditor(typeof(BuoyancyBody))]
    internal sealed class BuoyancyBodyEditor : UnityEditor.Editor
    {
        private SerializedProperty settingsProperty;
        private SerializedProperty sampleModeProperty;
        private SerializedProperty buoyancyForceProperty;
        private SerializedProperty verticalDampingProperty;
        private SerializedProperty submersionDepthClampProperty;
        private SerializedProperty surfaceOffsetProperty;
        private SerializedProperty samplePointOffsetProperty;
        private SerializedProperty sampleFootprintHalfExtentsProperty;
        private SerializedProperty forceRampDurationProperty;
        private SerializedProperty alignmentStrengthProperty;
        private SerializedProperty alignmentDampingProperty;

        private bool showAdvanced;

        private void OnEnable()
        {
            settingsProperty = serializedObject.FindProperty("settings");
            sampleModeProperty = settingsProperty.FindPropertyRelative("sampleMode");
            buoyancyForceProperty = settingsProperty.FindPropertyRelative("buoyancyForce");
            verticalDampingProperty = settingsProperty.FindPropertyRelative("verticalDamping");
            submersionDepthClampProperty = settingsProperty.FindPropertyRelative("submersionDepthClamp");
            surfaceOffsetProperty = settingsProperty.FindPropertyRelative("surfaceOffset");
            samplePointOffsetProperty = settingsProperty.FindPropertyRelative("samplePointOffset");
            sampleFootprintHalfExtentsProperty = settingsProperty.FindPropertyRelative("sampleFootprintHalfExtents");
            forceRampDurationProperty = settingsProperty.FindPropertyRelative("forceRampDuration");
            alignmentStrengthProperty = settingsProperty.FindPropertyRelative("alignmentStrength");
            alignmentDampingProperty = settingsProperty.FindPropertyRelative("alignmentDamping");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var body = (BuoyancyBody)target;

            DrawStatus(body);
            EditorGUILayout.Space(6f);
            DrawCoreSettings();
            EditorGUILayout.Space(4f);
            DrawAlignmentSettings();
            EditorGUILayout.Space(4f);
            DrawAdvancedSettings();
            EditorGUILayout.Space(4f);
            DrawRuntimeState(body);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatus(BuoyancyBody body)
        {
            EditorGUILayout.HelpBox(
                "RenderWave Buoyancy Body applies lightweight buoyancy forces so this Rigidbody floats on the water surface. Single Point is the safe default. Four Point improves pitch and roll stability on boats and rafts, but costs four water queries per physics tick.",
                MessageType.Info);

            var rb = body.GetComponent<Rigidbody>();
            if (rb == null)
            {
                EditorGUILayout.HelpBox("No Rigidbody found. BuoyancyBody requires a Rigidbody.", MessageType.Error);
                return;
            }

            if (rb.isKinematic)
            {
                EditorGUILayout.HelpBox(
                    "Rigidbody is Kinematic. Buoyancy forces have no effect on kinematic bodies.",
                    MessageType.Warning);
            }

            if (rb.interpolation == RigidbodyInterpolation.None)
            {
                EditorGUILayout.HelpBox(
                    "Rigidbody Interpolation is set to None. For smoother floating on the ocean surface, consider setting Interpolation to Interpolate.",
                    MessageType.Info);
            }

            if (rb.useGravity == false)
            {
                EditorGUILayout.HelpBox(
                    "Rigidbody has Use Gravity disabled. Buoyancy is designed to oppose gravity - without gravity the object will float upward indefinitely.",
                    MessageType.Warning);
            }
        }

        private void DrawCoreSettings()
        {
            EditorGUILayout.LabelField("Buoyancy", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sampleModeProperty, new GUIContent("Sample Mode", "Single Point is cheapest. Four Point uses one sample per corner for better pitch and roll behavior."));
            EditorGUILayout.PropertyField(buoyancyForceProperty, new GUIContent("Buoyancy Force", "Upward force per meter of submersion."));
            EditorGUILayout.PropertyField(verticalDampingProperty, new GUIContent("Vertical Damping", "Damping force opposing vertical velocity. Prevents endless bouncing."));
            EditorGUILayout.PropertyField(surfaceOffsetProperty, new GUIContent("Surface Offset", "Target float height relative to water surface."));

            if ((BuoyancySampleMode)sampleModeProperty.enumValueIndex == BuoyancySampleMode.FourPoint)
            {
                EditorGUILayout.HelpBox(
                    "Four Point is intended for medium boats, rafts, and longer props that look wrong with one sample. It is still Buoyancy Lite, not hull simulation.",
                    MessageType.None);
            }
        }

        private void DrawAlignmentSettings()
        {
            EditorGUILayout.LabelField("Surface Alignment", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(alignmentStrengthProperty, new GUIContent("Alignment Strength", "Torque strength for tilting toward the wave surface normal. Zero disables alignment."));
            EditorGUILayout.PropertyField(alignmentDampingProperty, new GUIContent("Alignment Damping", "Damping torque to prevent rotational oscillation."));

            if (alignmentStrengthProperty.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Surface alignment is disabled. The object will float without tilting.", MessageType.None);
            }
        }

        private void DrawAdvancedSettings()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (!showAdvanced)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(samplePointOffsetProperty, new GUIContent("Sample Point Offset", "Local-space offset for the center water query position."));

            if ((BuoyancySampleMode)sampleModeProperty.enumValueIndex == BuoyancySampleMode.FourPoint)
            {
                EditorGUILayout.PropertyField(sampleFootprintHalfExtentsProperty, new GUIContent("Sample Footprint Half Extents", "Half-size of the four-point sampling footprint in local X/Z."));
            }

            EditorGUILayout.PropertyField(submersionDepthClampProperty, new GUIContent("Submersion Depth Clamp", "Max depth contributing to force. Prevents launch on deep spawns."));
            EditorGUILayout.PropertyField(forceRampDurationProperty, new GUIContent("Force Ramp Duration", "Seconds over which force ramps up after activation."));
            EditorGUI.indentLevel--;
        }

        private void DrawRuntimeState(BuoyancyBody body)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.Toggle("Has Water", body.HasWater);
                EditorGUILayout.IntField("Active Sample Count", body.LastActiveSampleCount);
                EditorGUILayout.FloatField("Surface Height", body.LastSurfaceHeight);
                EditorGUILayout.FloatField("Signed Distance", body.LastSignedDistance);
            }
        }
    }
}
