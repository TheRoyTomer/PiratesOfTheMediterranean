using RenderWave.Runtime.Data;
using RenderWave.Runtime.Zones;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RenderWave.Editor
{
    [CustomEditor(typeof(OceanWaterZone))]
    internal sealed class OceanWaterZoneEditor : UnityEditor.Editor
    {
        private static readonly GUIContent[] SupportedZoneTypeOptions =
        {
            new GUIContent("Ocean"),
            new GUIContent("Lake"),
            new GUIContent("Pool")
        };

        private static readonly int[] SupportedZoneTypeValues =
        {
            (int)WaterZoneType.Ocean,
            (int)WaterZoneType.Lake,
            (int)WaterZoneType.Pool
        };

        private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

        private SerializedProperty priorityProperty;
        private SerializedProperty coverageModeProperty;
        private SerializedProperty rectangleSizeProperty;
        private SerializedProperty settingsProperty;

        private void OnEnable()
        {
            priorityProperty = serializedObject.FindProperty("priority");
            coverageModeProperty = serializedObject.FindProperty("coverageMode");
            rectangleSizeProperty = serializedObject.FindProperty("rectangleSize");
            settingsProperty = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var zone = (OceanWaterZone)target;
            DrawStatus(zone);
            EditorGUILayout.Space(6f);
            DrawCoverage();
            EditorGUILayout.Space(4f);
            DrawWaterBodyProfiles(zone);
            EditorGUILayout.Space(4f);
            DrawSurfaceSettings();
            EditorGUILayout.Space(4f);
            DrawColorAndGameplaySettings();
            DrawSummary(zone);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var zone = (OceanWaterZone)target;
            if (zone.CoverageMode != OceanZoneCoverageMode.Rectangle)
            {
                return;
            }

            serializedObject.Update();

            using (new Handles.DrawingScope(new Color(0.12f, 0.72f, 0.9f, 1f), Matrix4x4.TRS(zone.Center, Quaternion.identity, Vector3.one)))
            {
                boundsHandle.center = Vector3.zero;
                boundsHandle.size = new Vector3(Mathf.Max(1f, zone.RectangleSize.x), 0.1f, Mathf.Max(1f, zone.RectangleSize.y));

                EditorGUI.BeginChangeCheck();
                boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Resize RenderWave Rectangle Zone");
                    var size = boundsHandle.size;
                    rectangleSizeProperty.vector2Value = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.z));
                    serializedObject.ApplyModifiedProperties();
                }

                Handles.Label(
                    new Vector3(0f, 0.2f, 0f),
                    $"RenderWave Rectangle Zone\n{zone.RectangleSize.x:0.#} x {zone.RectangleSize.y:0.#}",
                    EditorStyles.boldLabel);
            }
        }

        private void DrawStatus(OceanWaterZone zone)
        {
            EditorGUILayout.HelpBox("OceanWaterZone defines where water exists and what gameplay/rendering data it exposes. For open ocean, Infinite coverage is the normal path. For contained water bodies like lakes or pools, use Rectangle coverage and a contained-water profile.", MessageType.Info);

            if (zone.CoverageMode == OceanZoneCoverageMode.Rectangle && !RenderWaveEditorUtility.HasIdentityRotationAndUnitScale(zone.transform))
            {
                EditorGUILayout.HelpBox("Rectangle coverage is axis-aligned in world space. Keep this transform at identity rotation and unit scale.", MessageType.Error);
            }

            switch (zone.Settings.ZoneType)
            {
                case WaterZoneType.Lake:
                case WaterZoneType.Pool:
                    if (zone.CoverageMode != OceanZoneCoverageMode.Rectangle)
                    {
                        EditorGUILayout.HelpBox("Lake and Pool profiles are intended for Rectangle coverage. Infinite coverage technically works, but it is usually the wrong product setup.", MessageType.Warning);
                    }
                    break;

                case WaterZoneType.River:
                    EditorGUILayout.HelpBox("This zone is using a legacy unsupported River value. RenderWave does not ship river tooling. Convert it to Ocean or Lake before treating the scene as release-ready.", MessageType.Error);
                    break;
            }
        }

        private void DrawCoverage()
        {
            EditorGUILayout.LabelField("Coverage", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(priorityProperty, new GUIContent("Priority"));
            EditorGUILayout.PropertyField(coverageModeProperty, new GUIContent("Coverage Mode"));

            if ((OceanZoneCoverageMode)coverageModeProperty.enumValueIndex == OceanZoneCoverageMode.Rectangle)
            {
                EditorGUILayout.PropertyField(rectangleSizeProperty, new GUIContent("Rectangle Size"));
                if (GUILayout.Button("Use Infinite Coverage"))
                {
                    coverageModeProperty.enumValueIndex = (int)OceanZoneCoverageMode.Infinite;
                }
            }
            else if (GUILayout.Button("Switch To Rectangle Coverage"))
            {
                coverageModeProperty.enumValueIndex = (int)OceanZoneCoverageMode.Rectangle;
            }
        }

        private void DrawSurfaceSettings()
        {
            EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("baseHeight"), new GUIContent("Base Height"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("flowDirection"), new GUIContent("Flow Direction"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("vertexDisplacementStrength"), new GUIContent("Displacement Strength"));

            if (GUILayout.Button("Use Transform Y As Base Height"))
            {
                settingsProperty.FindPropertyRelative("baseHeight").floatValue = ((OceanWaterZone)target).transform.position.y;
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Large Waves", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("largeWaveIntensity"), new GUIContent("Intensity"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("largeWaveLength"), new GUIContent("Length"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("largeWaveSpeed"), new GUIContent("Speed"));

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Small Waves", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("smallWaveIntensity"), new GUIContent("Intensity"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("smallWaveLength"), new GUIContent("Length"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("smallWaveSpeed"), new GUIContent("Speed"));
        }

        private void DrawWaterBodyProfiles(OceanWaterZone zone)
        {
            EditorGUILayout.LabelField("Water Body Profiles", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use these only to define contained-water behavior and defaults. Ocean surface look and shading presets still belong on OceanManager.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Lake Profile"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyLakeZoneProfile(zone);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Apply Pool Profile"))
                {
                    serializedObject.ApplyModifiedProperties();
                    RenderWaveEditorUtility.ApplyPoolZoneProfile(zone);
                    serializedObject.Update();
                }
            }
        }

        private void DrawColorAndGameplaySettings()
        {
            EditorGUILayout.LabelField("Color And Gameplay", EditorStyles.boldLabel);
            DrawSupportedZoneTypeField();
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("surfaceColor"), new GUIContent("Surface Color"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("underwaterColor"), new GUIContent("Underwater Color"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("fogDensity"), new GUIContent("Fog Density"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("allowSwimming"), new GUIContent("Allow Swimming"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("underwaterEffectsEnabled"), new GUIContent("Underwater Effects"));
        }

        private void DrawSupportedZoneTypeField()
        {
            var zoneTypeProperty = settingsProperty.FindPropertyRelative("zoneType");
            var currentValue = zoneTypeProperty.intValue;

            if (currentValue == (int)WaterZoneType.River)
            {
                EditorGUILayout.HelpBox("River is hidden from the public workflow because it is not a finished product feature.", MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Convert To Ocean"))
                    {
                        zoneTypeProperty.intValue = (int)WaterZoneType.Ocean;
                    }

                    if (GUILayout.Button("Convert To Lake"))
                    {
                        zoneTypeProperty.intValue = (int)WaterZoneType.Lake;
                    }
                }

                return;
            }

            var selectedIndex = 0;
            for (var i = 0; i < SupportedZoneTypeValues.Length; i++)
            {
                if (SupportedZoneTypeValues[i] == currentValue)
                {
                    selectedIndex = i;
                    break;
                }
            }

            var nextIndex = EditorGUILayout.Popup(new GUIContent("Zone Type"), selectedIndex, SupportedZoneTypeOptions);
            zoneTypeProperty.intValue = SupportedZoneTypeValues[nextIndex];
        }

        private void DrawSummary(OceanWaterZone zone)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                $"Estimated max displacement: {zone.MaxVerticalDisplacement:0.###}\nCoverage mode: {zone.CoverageMode}\nScene handle editing is available for rectangle coverage directly in the Scene view.",
                MessageType.None);
        }
    }
}
