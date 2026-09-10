using RenderWave.Runtime.Core;
using RenderWave.Runtime.Underwater;
using UnityEditor;
using UnityEngine;

namespace RenderWave.Editor
{
    [CustomEditor(typeof(UnderwaterStateController))]
    internal sealed class UnderwaterStateControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty oceanManagerProperty;
        private SerializedProperty targetCameraProperty;
        private SerializedProperty transitionSettingsProperty;
        private SerializedProperty visualSettingsProperty;
        private SerializedProperty overlaySourceMaterialProperty;
        private SerializedProperty overlayShaderProperty;

        private SerializedProperty visualsEnabledProperty;
        private SerializedProperty tintColorProperty;
        private SerializedProperty visibilityDistanceProperty;
        private SerializedProperty maxOpacityProperty;
        private SerializedProperty tintStrengthProperty;
        private SerializedProperty nearSurfaceLightProperty;
        private SerializedProperty enhancedWaterlineProperty;
        private SerializedProperty distortionStrengthProperty;
        private SerializedProperty underwaterVignetteStrengthProperty;
        private SerializedProperty waterlineBandThicknessProperty;
        private SerializedProperty waterlineNoiseScaleProperty;
        private SerializedProperty waterlineEdgeSoftnessProperty;
        private SerializedProperty waterlineHighlightStrengthProperty;
        private SerializedProperty causticsEnabledProperty;
        private SerializedProperty causticsTextureProperty;
        private SerializedProperty causticColorProperty;
        private SerializedProperty causticIntensityProperty;
        private SerializedProperty causticScaleProperty;
        private SerializedProperty causticSpeedProperty;
        private SerializedProperty absorptionStrengthProperty;
        private SerializedProperty deepScatterColorProperty;
        private SerializedProperty depthColorShiftRangeProperty;
        private SerializedProperty depthOcclusionStrengthProperty;
        private SerializedProperty abyssStrengthProperty;
        private SerializedProperty ambientTransmissionFloorProperty;

        private void OnEnable()
        {
            oceanManagerProperty = serializedObject.FindProperty("oceanManager");
            targetCameraProperty = serializedObject.FindProperty("targetCamera");
            transitionSettingsProperty = serializedObject.FindProperty("transitionSettings");
            visualSettingsProperty = serializedObject.FindProperty("visualSettings");
            overlaySourceMaterialProperty = serializedObject.FindProperty("overlaySourceMaterial");
            overlayShaderProperty = serializedObject.FindProperty("overlayShader");

            visualsEnabledProperty = visualSettingsProperty.FindPropertyRelative("visualsEnabled");
            tintColorProperty = visualSettingsProperty.FindPropertyRelative("tintColor");
            visibilityDistanceProperty = visualSettingsProperty.FindPropertyRelative("visibilityDistance");
            maxOpacityProperty = visualSettingsProperty.FindPropertyRelative("maxOpacity");
            tintStrengthProperty = visualSettingsProperty.FindPropertyRelative("tintStrength");
            nearSurfaceLightProperty = visualSettingsProperty.FindPropertyRelative("nearSurfaceLight");
            enhancedWaterlineProperty = visualSettingsProperty.FindPropertyRelative("enhancedWaterline");
            distortionStrengthProperty = visualSettingsProperty.FindPropertyRelative("distortionStrength");
            underwaterVignetteStrengthProperty = visualSettingsProperty.FindPropertyRelative("underwaterVignetteStrength");
            waterlineBandThicknessProperty = visualSettingsProperty.FindPropertyRelative("waterlineBandThickness");
            waterlineNoiseScaleProperty = visualSettingsProperty.FindPropertyRelative("waterlineNoiseScale");
            waterlineEdgeSoftnessProperty = visualSettingsProperty.FindPropertyRelative("waterlineEdgeSoftness");
            waterlineHighlightStrengthProperty = visualSettingsProperty.FindPropertyRelative("waterlineHighlightStrength");
            causticsEnabledProperty = visualSettingsProperty.FindPropertyRelative("causticsEnabled");
            causticsTextureProperty = visualSettingsProperty.FindPropertyRelative("causticsTexture");
            causticColorProperty = visualSettingsProperty.FindPropertyRelative("causticColor");
            causticIntensityProperty = visualSettingsProperty.FindPropertyRelative("causticIntensity");
            causticScaleProperty = visualSettingsProperty.FindPropertyRelative("causticScale");
            causticSpeedProperty = visualSettingsProperty.FindPropertyRelative("causticSpeed");
            absorptionStrengthProperty = visualSettingsProperty.FindPropertyRelative("absorptionStrength");
            deepScatterColorProperty = visualSettingsProperty.FindPropertyRelative("deepScatterColor");
            depthColorShiftRangeProperty = visualSettingsProperty.FindPropertyRelative("depthColorShiftRange");
            depthOcclusionStrengthProperty = visualSettingsProperty.FindPropertyRelative("depthOcclusionStrength");
            abyssStrengthProperty = visualSettingsProperty.FindPropertyRelative("abyssStrength");
            ambientTransmissionFloorProperty = visualSettingsProperty.FindPropertyRelative("ambientTransmissionFloor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var controller = (UnderwaterStateController)target;
            var oceanManager = oceanManagerProperty.objectReferenceValue as OceanManager;

            EditorGUILayout.HelpBox("UnderwaterStateController handles waterline state and the low-cost underwater overlay for the same camera used by OceanManager.", MessageType.Info);

            if (oceanManager == null)
            {
                EditorGUILayout.HelpBox("Assign an OceanManager or add this component from the OceanManager inspector so camera and query references stay aligned.", MessageType.Error);
            }
            else if (targetCameraProperty.objectReferenceValue != null && oceanManager.TargetCamera != null && targetCameraProperty.objectReferenceValue != oceanManager.TargetCamera)
            {
                EditorGUILayout.HelpBox("The underwater camera must match the OceanManager target camera in V1.", MessageType.Error);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto Assign From OceanManager"))
                {
                    serializedObject.ApplyModifiedProperties();
                    if (oceanManager == null)
                    {
                        oceanManager = controller.GetComponent<OceanManager>();
                    }

                    if (oceanManager != null)
                    {
                        RenderWaveEditorUtility.EnsureUnderwaterSetup(oceanManager);
                    }

                    serializedObject.Update();
                }

                if (GUILayout.Button("Use Recommended Shader"))
                {
                    overlayShaderProperty.objectReferenceValue = RenderWaveEditorUtility.LoadUnderwaterShader();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(oceanManagerProperty, new GUIContent("Ocean Manager"));
            EditorGUILayout.PropertyField(targetCameraProperty, new GUIContent("Target Camera"));
            EditorGUILayout.PropertyField(transitionSettingsProperty, new GUIContent("Transition Settings"), true);
            DrawVisualSettings();

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(overlaySourceMaterialProperty, new GUIContent("Overlay Material"));
            EditorGUILayout.PropertyField(overlayShaderProperty, new GUIContent("Overlay Shader"));

            if (overlaySourceMaterialProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Overlay material is optional. If it stays empty, RenderWave will build the runtime overlay from the assigned shader.", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVisualSettings()
        {
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(visualsEnabledProperty, new GUIContent("Visuals Enabled"));
            EditorGUILayout.PropertyField(tintColorProperty, new GUIContent("Tint Color"));
            EditorGUILayout.PropertyField(visibilityDistanceProperty, new GUIContent("Visibility Distance"));
            EditorGUILayout.PropertyField(maxOpacityProperty, new GUIContent("Max Opacity"));
            EditorGUILayout.PropertyField(tintStrengthProperty, new GUIContent("Tint Strength"));
            EditorGUILayout.PropertyField(nearSurfaceLightProperty, new GUIContent("Near Surface Light"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Enhanced Underwater Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enhancedWaterlineProperty, new GUIContent("Enable Enhanced Mode",
                "Keeps the approved Lite path when disabled. When enabled, adds shimmer, vignette, and a richer waterline transition band."));

            if (!enhancedWaterlineProperty.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Enhanced mode is disabled. The Lite underwater overlay is active and visually unchanged.",
                    MessageType.None);
            }
            else
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Volume Feel", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(distortionStrengthProperty, new GUIContent("Distortion Strength",
                    "Subtle screen-space shimmer that helps the scene feel like it sits inside water."));
                EditorGUILayout.PropertyField(underwaterVignetteStrengthProperty, new GUIContent("Vignette Strength",
                    "Darkens oblique screen edges to suggest increased water path length and depth."));

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Transition Band", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(waterlineBandThicknessProperty, new GUIContent("Band Thickness",
                    "Overall width of the water/air transition band during surface crossing."));
                EditorGUILayout.PropertyField(waterlineNoiseScaleProperty, new GUIContent("Noise Scale",
                    "Organic breakup frequency that keeps the waterline from reading as a flat HUD line."));
                EditorGUILayout.PropertyField(waterlineEdgeSoftnessProperty, new GUIContent("Edge Softness",
                    "Softness of the outer edge of the transition band."));
                EditorGUILayout.PropertyField(waterlineHighlightStrengthProperty, new GUIContent("Highlight Strength",
                    "Brightness of the edge glow and foam stipple inside the transition band."));

                EditorGUI.indentLevel--;

                EditorGUILayout.HelpBox(
                    "Enhanced mode adds shimmer, vignette, upward ceiling glow, and a noise-distorted waterline band. " +
                    "All effects are gated on TransitionWeight so nothing is visible when the camera is above water.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Depth Absorption", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(absorptionStrengthProperty, new GUIContent("Absorption Strength",
                "How fast fog density grows as the camera descends. 0 = no depth response, 1 = realistic, 2 = murky water."));
            EditorGUILayout.PropertyField(deepScatterColorProperty, new GUIContent("Deep Scatter Color",
                "The colour the tint shifts toward at depth. Should be dark blue-dominant (red absorbed first)."));
            EditorGUILayout.PropertyField(depthColorShiftRangeProperty, new GUIContent("Color Shift Range (m)",
                "Camera depth in metres at which the tint fully transitions to Deep Scatter Color and fog is at peak density."));
            EditorGUILayout.PropertyField(depthOcclusionStrengthProperty, new GUIContent("Depth Occlusion Strength",
                "Non-linear fog shape applied as the camera descends. " +
                "0 = standard exponential. Around 1-1.5 keeps nearby geometry readable while still swallowing medium/far views."));
            EditorGUILayout.PropertyField(abyssStrengthProperty, new GUIContent("Abyss Strength",
                "Adds extra extinction and deeper tinting specifically when looking downward, especially into open water with no visible bottom."));
            EditorGUILayout.PropertyField(ambientTransmissionFloorProperty, new GUIContent("Ambient Transmission Floor",
                "Minimum retained ambient light at depth. Raise this to keep underwater readable and preserve headroom for future flashlights or point lights."));
            EditorGUILayout.HelpBox(
                "Depth Occlusion Strength shapes general underwater falloff, while Abyss Strength specifically biases downward and bottomless views toward a deeper void. " +
                "Use Ambient Transmission Floor to preserve lighting headroom instead of solving abyss perception with blanket darkening.",
                MessageType.None);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Caustics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(causticsEnabledProperty, new GUIContent("Caustics Enabled"));
            if (causticsEnabledProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(causticsTextureProperty, new GUIContent("Caustics Texture"));
                EditorGUILayout.PropertyField(causticColorProperty, new GUIContent("Caustic Color"));
                EditorGUILayout.PropertyField(causticIntensityProperty, new GUIContent("Caustic Intensity"));
                EditorGUILayout.PropertyField(causticScaleProperty, new GUIContent("Caustic Scale"));
                EditorGUILayout.PropertyField(causticSpeedProperty, new GUIContent("Caustic Speed"));
                EditorGUI.indentLevel--;
            }
        }
    }
}
