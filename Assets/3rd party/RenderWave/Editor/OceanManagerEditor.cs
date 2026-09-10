using RenderWave.Runtime.Core;
using RenderWave.Runtime.Data;
using RenderWave.Runtime.Zones;
using UnityEditor;
using UnityEngine;

namespace RenderWave.Editor
{
    [CustomEditor(typeof(OceanManager))]
    internal sealed class OceanManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty targetCameraProperty;
        private SerializedProperty oceanZoneProperty;
        private SerializedProperty chunkSettingsProperty;
        private SerializedProperty farFieldSettingsProperty;
        private SerializedProperty sourceMaterialProperty;
        private SerializedProperty oceanShaderProperty;
        private SerializedProperty farFieldShaderProperty;

        private void OnEnable()
        {
            targetCameraProperty = serializedObject.FindProperty("targetCamera");
            oceanZoneProperty = serializedObject.FindProperty("oceanZone");
            chunkSettingsProperty = serializedObject.FindProperty("chunkSettings");
            farFieldSettingsProperty = serializedObject.FindProperty("farFieldSettings");
            sourceMaterialProperty = serializedObject.FindProperty("sourceMaterial");
            oceanShaderProperty = serializedObject.FindProperty("oceanShader");
            farFieldShaderProperty = serializedObject.FindProperty("farFieldShader");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var manager = (OceanManager)target;
            var zone = manager.OceanZone != null ? manager.OceanZone : manager.GetComponent<OceanWaterZone>();

            DrawStatus(manager, zone);
            EditorGUILayout.Space(6f);
            DrawQuickActions(manager);
            EditorGUILayout.Space(6f);
            DrawCoreReferences();
            EditorGUILayout.Space(4f);
            DrawRenderingSettings();
            EditorGUILayout.Space(4f);
            DrawChunkSettings();
            EditorGUILayout.Space(4f);
            DrawFarFieldSettings(zone);
            DrawChunkSummary();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatus(OceanManager manager, OceanWaterZone zone)
        {
            EditorGUILayout.HelpBox("RenderWave OceanManager is the main setup point for camera, chunk density, presets, underwater hookup, and wake-ready scene creation.", MessageType.Info);

            if (!RenderWaveEditorUtility.IsUrpActive())
            {
                EditorGUILayout.HelpBox("URP is not active. RenderWave V1 is authored for the Universal Render Pipeline.", MessageType.Warning);
            }

            if (targetCameraProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign the single render-driving camera. RenderWave V1 does not auto-manage multiple render cameras.", MessageType.Error);
            }

            if (oceanZoneProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("OceanManager needs an OceanWaterZone on the same GameObject so rendering and queries stay aligned.", MessageType.Error);
            }
            else if (zone != null && zone.gameObject != manager.gameObject)
            {
                EditorGUILayout.HelpBox("OceanManager and OceanWaterZone must live on the same GameObject in V1.", MessageType.Error);
            }

            if (!RenderWaveEditorUtility.HasIdentityRotationAndUnitScale(manager.transform))
            {
                EditorGUILayout.HelpBox("The OceanManager hierarchy must use identity rotation and unit scale. Non-uniform transform setups are intentionally not supported in V1.", MessageType.Error);
            }

            if (sourceMaterialProperty.objectReferenceValue != null &&
                !RenderWaveEditorUtility.HasOceanMaterialContract((Material)sourceMaterialProperty.objectReferenceValue))
            {
                EditorGUILayout.HelpBox("The assigned material does not match the RenderWave ocean shader contract. Use one of the packaged RenderWave ocean materials or the RenderWave ocean shader.", MessageType.Error);
            }
            else if (sourceMaterialProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No source material is assigned. RenderWave can fall back to the shader at runtime, but a packaged preset material gives the intended out-of-box look.", MessageType.Info);
            }

            if (zone != null)
            {
                switch (zone.Settings.ZoneType)
                {
                    case WaterZoneType.Lake:
                    case WaterZoneType.Pool:
                        EditorGUILayout.HelpBox("This rig is using a contained-water profile. Keep coverage on Rectangle and treat it as a contained surface, not as open ocean.", MessageType.None);

                        if (zone.Settings.ZoneType == WaterZoneType.Pool &&
                            !chunkSettingsProperty.FindPropertyRelative("enableDistanceCulling").boolValue)
                        {
                            EditorGUILayout.HelpBox("Pool rigs usually benefit from distance culling. If the pool is still rendering from unnecessarily long distances, enable it in Chunk Settings.", MessageType.Warning);
                        }
                        break;

                    case WaterZoneType.River:
                        EditorGUILayout.HelpBox("Zone Type is set to River, but RenderWave does not yet ship real river tooling. Change this only if you intentionally want query metadata, not a supported river workflow.", MessageType.Error);
                        break;
                }

                var farFieldEnabled = farFieldSettingsProperty.FindPropertyRelative("enabled").boolValue;
                if (farFieldEnabled && zone.CoverageMode != OceanZoneCoverageMode.Infinite)
                {
                    EditorGUILayout.HelpBox("Far Field is only used for Infinite coverage. It is ignored on contained Lake/Pool rectangle rigs.", MessageType.Info);
                }
                else if (farFieldEnabled && zone.Settings.ZoneType != WaterZoneType.Ocean)
                {
                    EditorGUILayout.HelpBox("Far Field is intended for open ocean coverage only. If you enable it on contained water profiles, the setting is ignored by design.", MessageType.Info);
                }
            }
        }

        private void DrawQuickActions(OceanManager manager)
        {
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto Assign References"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.AutoAssignOceanReferences(manager);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Use Main Camera"))
                {
                    targetCameraProperty.objectReferenceValue = RenderWaveEditorUtility.FindRecommendedCamera();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Classic / Retro"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyClassicOceanPreset(manager);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Apply Enhanced Ocean"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyEnhancedOceanPreset(manager);
                    serializedObject.Update();
                }
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Commercial Presets", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("These presets change both the packaged material look and the ocean-zone wave defaults. Use them as demo-ready starting points, then fine-tune only if the scene really needs it.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Calm"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyCalmOceanPreset(manager);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Storm"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyStormOceanPreset(manager);
                    serializedObject.Update();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Tropical"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyTropicalOceanPreset(manager);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Deep"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyDeepOceanPreset(manager);
                    serializedObject.Update();
                }
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Contained Water", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("These convert the current rig into a contained water setup. This is suitable for lakes and pools on the same chunked water architecture. It is not river tooling.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Lake"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyLakeWaterProfile(manager);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Pool"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyPoolWaterProfile(manager);
                    serializedObject.Update();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Underwater Controller"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.EnsureUnderwaterSetup(manager);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Add Wake System"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.EnsureWakeReadySetup(manager);
                    serializedObject.Update();
                }
            }
        }

        private void DrawCoreReferences()
        {
            EditorGUILayout.LabelField("Core References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetCameraProperty, new GUIContent("Target Camera"));
            EditorGUILayout.PropertyField(oceanZoneProperty, new GUIContent("Ocean Zone"));
        }

        private void DrawRenderingSettings()
        {
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourceMaterialProperty, new GUIContent("Source Material"));
            EditorGUILayout.PropertyField(oceanShaderProperty, new GUIContent("Fallback Shader"));
            EditorGUILayout.PropertyField(farFieldShaderProperty, new GUIContent("Far Field Shader"));
        }

        private void DrawChunkSettings()
        {
            EditorGUILayout.LabelField("Chunk Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("chunkRadius"), new GUIContent("Chunk Radius"));
            EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("chunkSize"), new GUIContent("Chunk Size"));
            EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("meshResolution"), new GUIContent("Mesh Resolution"));
            EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("enableDistanceCulling"), new GUIContent("Enable Distance Culling"));

            if (chunkSettingsProperty.FindPropertyRelative("enableDistanceCulling").boolValue)
            {
                EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("maxRenderDistance"), new GUIContent("Max Render Distance"));
            }

            EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("shadowCastingMode"), new GUIContent("Shadow Casting"));
            EditorGUILayout.PropertyField(chunkSettingsProperty.FindPropertyRelative("receiveShadows"), new GUIContent("Receive Shadows"));
        }

        private void DrawFarFieldSettings(OceanWaterZone zone)
        {
            EditorGUILayout.LabelField("Far Field", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Far Field keeps infinite ocean visible at long range using a cheaper horizon mesh. It is not used for Lake or Pool rigs.", MessageType.None);

            EditorGUILayout.PropertyField(farFieldSettingsProperty.FindPropertyRelative("enabled"), new GUIContent("Enable Far Field"));
            if (!farFieldSettingsProperty.FindPropertyRelative("enabled").boolValue)
            {
                return;
            }

            EditorGUILayout.PropertyField(farFieldSettingsProperty.FindPropertyRelative("farDistance"), new GUIContent("Far Distance"));
            EditorGUILayout.PropertyField(farFieldSettingsProperty.FindPropertyRelative("verticalOffset"), new GUIContent("Vertical Offset"));

            if (zone != null && zone.CoverageMode == OceanZoneCoverageMode.Infinite)
            {
                var chunkRadius = chunkSettingsProperty.FindPropertyRelative("chunkRadius").intValue;
                var chunkSize = chunkSettingsProperty.FindPropertyRelative("chunkSize").floatValue;
                var nearHalfExtent = (chunkRadius + 0.5f) * chunkSize;
                var farDistance = farFieldSettingsProperty.FindPropertyRelative("farDistance").floatValue;

                if (farDistance <= nearHalfExtent + chunkSize)
                {
                    EditorGUILayout.HelpBox("Far Distance is too close to the near chunk footprint. Increase it, or this far-field layer adds little practical value.", MessageType.Warning);
                }
            }
        }

        private void DrawChunkSummary()
        {
            var chunkRadius = chunkSettingsProperty.FindPropertyRelative("chunkRadius").intValue;
            var meshResolution = chunkSettingsProperty.FindPropertyRelative("meshResolution").intValue;
            var totalChunks = ((chunkRadius * 2) + 1) * ((chunkRadius * 2) + 1);
            var verticesPerChunk = (meshResolution + 1) * (meshResolution + 1);
            var totalVertices = totalChunks * verticesPerChunk;

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                $"Visible chunks: {totalChunks}\nEstimated vertices: {totalVertices}\nUse presets first, then raise chunk radius or mesh resolution only when you really need the extra coverage or density.",
                MessageType.None);
        }
    }
}
