using RenderWave.Runtime.Core;
using RenderWave.Runtime.Data;
using RenderWave.Runtime.Underwater;
using RenderWave.Runtime.Wakes;
using RenderWave.Runtime.Zones;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Editor
{
    internal enum RenderWaveRigTemplate
    {
        ClassicOcean = 0,
        EnhancedOcean = 1,
        EnhancedUnderwater = 2,
        EnhancedWakeReady = 3,
        CalmOcean = 4,
        StormOcean = 5,
        TropicalOcean = 6,
        DeepOcean = 7,
        LakeWater = 8,
        PoolWater = 9
    }

    internal enum RenderWaveOceanPreset
    {
        Classic = 0,
        Enhanced = 1,
        Calm = 2,
        Storm = 3,
        Tropical = 4,
        Deep = 5
    }

    internal enum RenderWaveWaterBodyProfile
    {
        Lake = 0,
        Pool = 1
    }

    internal static class RenderWaveEditorUtility
    {
        private const string ClassicOceanMaterialGuid = "33b9617f3368c20469ee45af91143868";
        private const string EnhancedOceanMaterialGuid = "57bbee713d0e8a740a02caabe1958886";
        private const string CalmOceanMaterialGuid = "a7f3a6cb1c8748c4b265e34f0d51aa11";
        private const string StormOceanMaterialGuid = "b2c8e1f39fb54fb3a60ad3e8c6a14b72";
        private const string TropicalOceanMaterialGuid = "c4d7f91ab8f3473ab0a28e731e6f5493";
        private const string DeepOceanMaterialGuid = "d19ae2c54f5b4d56a884fd3c27b1e690";
        private const string WakeMaterialGuid = "e5d083f844e9945468deba552626ee0e";
        private const string OceanShaderGuid = "37b5379acb97e264b9886c87ba8accf6";
        private const string FarFieldShaderGuid = "b66b0a0f93d243b4a2bf984fcfb533bc";
        private const string UnderwaterShaderGuid = "dcc2160a504bb784398d416f57527ff9";
        private const string WakeShaderGuid = "f8adad9ba5410ca41a2ea6d97faddf5b";

        private const string ClassicOceanMaterialPath = "Assets/RenderWave/URP/RenderWave Ocean Classic.mat";
        private const string EnhancedOceanMaterialPath = "Assets/RenderWave/URP/RenderWave Ocean High.mat";
        private const string CalmOceanMaterialPath = "Assets/RenderWave/URP/RenderWave Ocean Calm.mat";
        private const string StormOceanMaterialPath = "Assets/RenderWave/URP/RenderWave Ocean Storm.mat";
        private const string TropicalOceanMaterialPath = "Assets/RenderWave/URP/RenderWave Ocean Tropical.mat";
        private const string DeepOceanMaterialPath = "Assets/RenderWave/URP/RenderWave Ocean Deep.mat";
        private const string WakeMaterialPath = "Assets/RenderWave/URP/RenderWave Wakes.mat";
        private const string OceanShaderPath = "Assets/RenderWave/Shaders/RenderWaveOcean.shader";
        private const string FarFieldShaderPath = "Assets/RenderWave/Shaders/RenderWaveFarField.shader";
        private const string UnderwaterShaderPath = "Assets/RenderWave/Shaders/RenderWaveUnderwaterOverlay.shader";
        private const string WakeShaderPath = "Assets/RenderWave/Shaders/RenderWaveWake.shader";

        public static bool IsUrpActive()
        {
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            return renderPipeline != null && renderPipeline.GetType().Name.Contains("UniversalRenderPipelineAsset");
        }

        public static bool HasIdentityRotationAndUnitScale(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return false;
            }

            var rotation = targetTransform.rotation;
            var scale = targetTransform.lossyScale;
            return Quaternion.Angle(rotation, Quaternion.identity) <= 0.01f &&
                   Mathf.Abs(scale.x - 1f) <= 0.001f &&
                   Mathf.Abs(scale.y - 1f) <= 0.001f &&
                   Mathf.Abs(scale.z - 1f) <= 0.001f;
        }

        public static Camera FindRecommendedCamera()
        {
            return Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        }

        public static Material LoadClassicOceanMaterial() => LoadAssetByGuidOrPath<Material>(ClassicOceanMaterialGuid, ClassicOceanMaterialPath);

        public static Material LoadEnhancedOceanMaterial() => LoadAssetByGuidOrPath<Material>(EnhancedOceanMaterialGuid, EnhancedOceanMaterialPath);

        public static Material LoadCalmOceanMaterial() => LoadAssetByGuidOrPath<Material>(CalmOceanMaterialGuid, CalmOceanMaterialPath);

        public static Material LoadStormOceanMaterial() => LoadAssetByGuidOrPath<Material>(StormOceanMaterialGuid, StormOceanMaterialPath);

        public static Material LoadTropicalOceanMaterial() => LoadAssetByGuidOrPath<Material>(TropicalOceanMaterialGuid, TropicalOceanMaterialPath);

        public static Material LoadDeepOceanMaterial() => LoadAssetByGuidOrPath<Material>(DeepOceanMaterialGuid, DeepOceanMaterialPath);

        public static Material LoadWakeMaterial() => LoadAssetByGuidOrPath<Material>(WakeMaterialGuid, WakeMaterialPath);

        public static Shader LoadOceanShader() => LoadAssetByGuidOrPath<Shader>(OceanShaderGuid, OceanShaderPath);

        public static Shader LoadFarFieldShader() => LoadAssetByGuidOrPath<Shader>(FarFieldShaderGuid, FarFieldShaderPath);

        public static Shader LoadUnderwaterShader() => LoadAssetByGuidOrPath<Shader>(UnderwaterShaderGuid, UnderwaterShaderPath);

        public static Shader LoadWakeShader() => LoadAssetByGuidOrPath<Shader>(WakeShaderGuid, WakeShaderPath);

        public static bool HasOceanMaterialContract(Material material)
        {
            return material != null &&
                   material.HasProperty("_SurfaceColor") &&
                   material.HasProperty("_DeepColor") &&
                   material.HasProperty("_LargeWaveDirection") &&
                   material.HasProperty("_SmallWaveDirection") &&
                   material.HasProperty("_LargeWaveData") &&
                   material.HasProperty("_SmallWaveData") &&
                   material.HasProperty("_RenderWaveTime");
        }

        public static bool HasWakeMaterialContract(Material material)
        {
            return material != null && material.HasProperty("_WakeColor");
        }

        public static void AutoAssignOceanReferences(OceanManager manager)
        {
            if (manager == null)
            {
                return;
            }

            var zone = EnsureOceanZone(manager.gameObject);
            Undo.RecordObject(manager, "Auto Assign RenderWave References");
            var serializedObject = new SerializedObject(manager);
            serializedObject.FindProperty("oceanZone").objectReferenceValue = zone;

            var targetCamera = serializedObject.FindProperty("targetCamera");
            if (targetCamera.objectReferenceValue == null)
            {
                targetCamera.objectReferenceValue = FindRecommendedCamera();
            }

            if (serializedObject.FindProperty("oceanShader").objectReferenceValue == null)
            {
                serializedObject.FindProperty("oceanShader").objectReferenceValue = LoadOceanShader();
            }

            if (serializedObject.FindProperty("farFieldShader").objectReferenceValue == null)
            {
                serializedObject.FindProperty("farFieldShader").objectReferenceValue = LoadFarFieldShader();
            }

            serializedObject.ApplyModifiedProperties();
            MarkDirty(manager);
        }

        public static void ApplyClassicOceanPreset(OceanManager manager)
        {
            ApplyOceanPreset(manager, RenderWaveOceanPreset.Classic);
        }

        public static void ApplyEnhancedOceanPreset(OceanManager manager)
        {
            ApplyOceanPreset(manager, RenderWaveOceanPreset.Enhanced);
        }

        public static void ApplyCalmOceanPreset(OceanManager manager)
        {
            ApplyOceanPreset(manager, RenderWaveOceanPreset.Calm);
        }

        public static void ApplyStormOceanPreset(OceanManager manager)
        {
            ApplyOceanPreset(manager, RenderWaveOceanPreset.Storm);
        }

        public static void ApplyTropicalOceanPreset(OceanManager manager)
        {
            ApplyOceanPreset(manager, RenderWaveOceanPreset.Tropical);
        }

        public static void ApplyDeepOceanPreset(OceanManager manager)
        {
            ApplyOceanPreset(manager, RenderWaveOceanPreset.Deep);
        }

        public static void ApplyLakeWaterProfile(OceanManager manager)
        {
            ApplyWaterBodyProfile(manager, RenderWaveWaterBodyProfile.Lake);
        }

        public static void ApplyPoolWaterProfile(OceanManager manager)
        {
            ApplyWaterBodyProfile(manager, RenderWaveWaterBodyProfile.Pool);
        }

        public static void ApplyLakeZoneProfile(OceanWaterZone zone)
        {
            ApplyContainedZoneProfile(zone, RenderWaveWaterBodyProfile.Lake);
        }

        public static void ApplyPoolZoneProfile(OceanWaterZone zone)
        {
            ApplyContainedZoneProfile(zone, RenderWaveWaterBodyProfile.Pool);
        }

        public static UnderwaterStateController EnsureUnderwaterSetup(OceanManager manager)
        {
            if (manager == null)
            {
                return null;
            }

            AutoAssignOceanReferences(manager);
            var controller = GetOrAddComponent<UnderwaterStateController>(manager.gameObject);
            Undo.RecordObject(controller, "Configure RenderWave Underwater");

            var managerSerialized = new SerializedObject(manager);
            var managerCamera = managerSerialized.FindProperty("targetCamera").objectReferenceValue as Camera;

            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("oceanManager").objectReferenceValue = manager;
            serializedObject.FindProperty("targetCamera").objectReferenceValue = managerCamera != null ? managerCamera : FindRecommendedCamera();
            serializedObject.FindProperty("overlayShader").objectReferenceValue = LoadUnderwaterShader();
            serializedObject.ApplyModifiedProperties();

            MarkDirty(controller);
            return controller;
        }

        public static WakeSystem EnsureWakeReadySetup(OceanManager manager)
        {
            if (manager == null)
            {
                return null;
            }

            var existingSystem = Object.FindFirstObjectByType<WakeSystem>();
            var wakeSystem = existingSystem;
            if (wakeSystem == null)
            {
                var wakeObject = new GameObject("RenderWave Wake System");
                Undo.RegisterCreatedObjectUndo(wakeObject, "Create RenderWave Wake System");
                wakeObject.transform.SetParent(manager.transform, false);
                wakeSystem = Undo.AddComponent<WakeSystem>(wakeObject);
            }

            Undo.RecordObject(wakeSystem, "Configure RenderWave Wake System");
            var serializedObject = new SerializedObject(wakeSystem);
            serializedObject.FindProperty("sourceMaterial").objectReferenceValue = LoadWakeMaterial();
            serializedObject.FindProperty("wakeShader").objectReferenceValue = LoadWakeShader();
            serializedObject.ApplyModifiedProperties();

            MarkDirty(wakeSystem);
            return wakeSystem;
        }

        public static OceanManager CreateOceanRig(RenderWaveRigTemplate template)
        {
            var root = new GameObject(GetRigName(template));
            Undo.RegisterCreatedObjectUndo(root, "Create RenderWave Ocean Rig");
            root.transform.position = Vector3.zero;

            var zone = Undo.AddComponent<OceanWaterZone>(root);
            var manager = Undo.AddComponent<OceanManager>(root);
            AutoAssignOceanReferences(manager);

            switch (template)
            {
                case RenderWaveRigTemplate.ClassicOcean:
                    ApplyClassicOceanPreset(manager);
                    break;

                case RenderWaveRigTemplate.EnhancedOcean:
                    ApplyEnhancedOceanPreset(manager);
                    break;

                case RenderWaveRigTemplate.EnhancedUnderwater:
                    ApplyEnhancedOceanPreset(manager);
                    EnsureUnderwaterSetup(manager);
                    break;

                case RenderWaveRigTemplate.EnhancedWakeReady:
                    ApplyEnhancedOceanPreset(manager);
                    EnsureWakeReadySetup(manager);
                    break;

                case RenderWaveRigTemplate.CalmOcean:
                    ApplyCalmOceanPreset(manager);
                    break;

                case RenderWaveRigTemplate.StormOcean:
                    ApplyStormOceanPreset(manager);
                    break;

                case RenderWaveRigTemplate.TropicalOcean:
                    ApplyTropicalOceanPreset(manager);
                    break;

                case RenderWaveRigTemplate.DeepOcean:
                    ApplyDeepOceanPreset(manager);
                    break;

                case RenderWaveRigTemplate.LakeWater:
                    ApplyLakeWaterProfile(manager);
                    break;

                case RenderWaveRigTemplate.PoolWater:
                    ApplyPoolWaterProfile(manager);
                    break;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            MarkDirty(zone);
            MarkDirty(manager);
            return manager;
        }

        private static void ApplyOceanPreset(OceanManager manager, RenderWaveOceanPreset preset)
        {
            if (manager == null)
            {
                return;
            }

            AutoAssignOceanReferences(manager);
            var zone = EnsureOceanZone(manager.gameObject);
            var material = GetOceanMaterialForPreset(preset);
            var presetLabel = GetPresetDisplayName(preset);

            Undo.RecordObjects(new Object[] { manager, zone }, $"Apply {presetLabel} Ocean Preset");

            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("oceanZone").objectReferenceValue = zone;
            managerSerialized.FindProperty("targetCamera").objectReferenceValue = FindRecommendedCamera();
            managerSerialized.FindProperty("sourceMaterial").objectReferenceValue = material;
            managerSerialized.FindProperty("oceanShader").objectReferenceValue = LoadOceanShader();
            managerSerialized.FindProperty("farFieldShader").objectReferenceValue = LoadFarFieldShader();

            var chunkSettings = managerSerialized.FindProperty("chunkSettings");
            chunkSettings.FindPropertyRelative("chunkSize").floatValue = 128f;
            chunkSettings.FindPropertyRelative("enableDistanceCulling").boolValue = false;
            chunkSettings.FindPropertyRelative("maxRenderDistance").floatValue = 120f;
            chunkSettings.FindPropertyRelative("shadowCastingMode").enumValueIndex = (int)ShadowCastingMode.Off;
            chunkSettings.FindPropertyRelative("receiveShadows").boolValue = false;
            ApplyChunkPreset(preset, chunkSettings);

            var farFieldSettings = managerSerialized.FindProperty("farFieldSettings");
            farFieldSettings.FindPropertyRelative("enabled").boolValue = true;
            farFieldSettings.FindPropertyRelative("farDistance").floatValue = 4096f;
            farFieldSettings.FindPropertyRelative("verticalOffset").floatValue = -0.08f;
            managerSerialized.ApplyModifiedProperties();

            var zoneSerialized = new SerializedObject(zone);
            zoneSerialized.FindProperty("priority").intValue = 0;
            zoneSerialized.FindProperty("coverageMode").enumValueIndex = 0;
            zoneSerialized.FindProperty("rectangleSize").vector2Value = new Vector2(4096f, 4096f);

            var settings = zoneSerialized.FindProperty("settings");
            settings.FindPropertyRelative("zoneType").enumValueIndex = 0;
            settings.FindPropertyRelative("baseHeight").floatValue = manager.transform.position.y;
            settings.FindPropertyRelative("allowSwimming").boolValue = true;
            settings.FindPropertyRelative("underwaterEffectsEnabled").boolValue = true;
            ApplyZonePreset(preset, settings);
            zoneSerialized.ApplyModifiedProperties();

            MarkDirty(manager);
            MarkDirty(zone);
        }

        private static string GetRigName(RenderWaveRigTemplate template)
        {
            switch (template)
            {
                case RenderWaveRigTemplate.ClassicOcean:
                    return "RenderWave Classic Ocean";
                case RenderWaveRigTemplate.EnhancedOcean:
                    return "RenderWave Enhanced Ocean";
                case RenderWaveRigTemplate.EnhancedUnderwater:
                    return "RenderWave Enhanced Ocean + Underwater";
                case RenderWaveRigTemplate.EnhancedWakeReady:
                    return "RenderWave Enhanced Ocean + Wake Ready";
                case RenderWaveRigTemplate.CalmOcean:
                    return "RenderWave Calm Ocean";
                case RenderWaveRigTemplate.StormOcean:
                    return "RenderWave Storm Ocean";
                case RenderWaveRigTemplate.TropicalOcean:
                    return "RenderWave Tropical Ocean";
                case RenderWaveRigTemplate.DeepOcean:
                    return "RenderWave Deep Ocean";
                case RenderWaveRigTemplate.LakeWater:
                    return "RenderWave Lake";
                case RenderWaveRigTemplate.PoolWater:
                    return "RenderWave Pool";
                default:
                    return "RenderWave Ocean";
            }
        }

        private static Material GetOceanMaterialForPreset(RenderWaveOceanPreset preset)
        {
            switch (preset)
            {
                case RenderWaveOceanPreset.Classic:
                    return LoadClassicOceanMaterial();
                case RenderWaveOceanPreset.Enhanced:
                    return LoadEnhancedOceanMaterial();
                case RenderWaveOceanPreset.Calm:
                    return LoadCalmOceanMaterial();
                case RenderWaveOceanPreset.Storm:
                    return LoadStormOceanMaterial();
                case RenderWaveOceanPreset.Tropical:
                    return LoadTropicalOceanMaterial();
                case RenderWaveOceanPreset.Deep:
                    return LoadDeepOceanMaterial();
                default:
                    return LoadEnhancedOceanMaterial();
            }
        }

        private static string GetPresetDisplayName(RenderWaveOceanPreset preset)
        {
            switch (preset)
            {
                case RenderWaveOceanPreset.Classic:
                    return "Classic";
                case RenderWaveOceanPreset.Enhanced:
                    return "Enhanced";
                case RenderWaveOceanPreset.Calm:
                    return "Calm";
                case RenderWaveOceanPreset.Storm:
                    return "Storm";
                case RenderWaveOceanPreset.Tropical:
                    return "Tropical";
                case RenderWaveOceanPreset.Deep:
                    return "Deep";
                default:
                    return "Enhanced";
            }
        }

        private static void ApplyChunkPreset(RenderWaveOceanPreset preset, SerializedProperty chunkSettings)
        {
            switch (preset)
            {
                case RenderWaveOceanPreset.Classic:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 4;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 12;
                    break;
                case RenderWaveOceanPreset.Calm:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 5;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 18;
                    break;
                case RenderWaveOceanPreset.Storm:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 5;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 22;
                    break;
                case RenderWaveOceanPreset.Tropical:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 5;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 18;
                    break;
                case RenderWaveOceanPreset.Deep:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 5;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 20;
                    break;
                case RenderWaveOceanPreset.Enhanced:
                default:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 5;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 20;
                    break;
            }
        }

        private static void ApplyZonePreset(RenderWaveOceanPreset preset, SerializedProperty settings)
        {
            switch (preset)
            {
                case RenderWaveOceanPreset.Classic:
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.08f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 0.75f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.08f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 5f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 0.95f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.28f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 36f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.24f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.09f, 0.46f, 0.58f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.03f, 0.18f, 0.25f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.018f;
                    break;
                case RenderWaveOceanPreset.Calm:
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.06f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 0.7f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.08f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 5.5f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 0.85f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.24f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 42f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.22f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.16f, 0.59f, 0.70f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.04f, 0.17f, 0.25f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.014f;
                    break;
                case RenderWaveOceanPreset.Storm:
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.22f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 1.18f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.18f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 6.5f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 1.6f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.95f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 36f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.48f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.09f, 0.32f, 0.42f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.02f, 0.10f, 0.16f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.038f;
                    break;
                case RenderWaveOceanPreset.Tropical:
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.1f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 0.9f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.11f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 5.5f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 1.1f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.4f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 40f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.3f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.17f, 0.70f, 0.66f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.03f, 0.22f, 0.23f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.016f;
                    break;
                case RenderWaveOceanPreset.Deep:
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.12f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 0.98f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.13f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 6f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 1.15f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.55f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 52f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.28f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.07f, 0.29f, 0.42f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.01f, 0.08f, 0.14f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.03f;
                    break;
                case RenderWaveOceanPreset.Enhanced:
                default:
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.15f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 1f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.14f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 6f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 1.3f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.65f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 42f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.35f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.08f, 0.42f, 0.55f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.02f, 0.16f, 0.22f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.025f;
                    break;
            }
        }

        private static void ApplyWaterBodyProfile(OceanManager manager, RenderWaveWaterBodyProfile profile)
        {
            if (manager == null)
            {
                return;
            }

            AutoAssignOceanReferences(manager);
            var zone = EnsureOceanZone(manager.gameObject);
            var material = GetMaterialForWaterBodyProfile(profile);

            Undo.RecordObjects(new Object[] { manager, zone }, $"Apply {GetWaterBodyProfileDisplayName(profile)} Water Body Profile");

            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("oceanZone").objectReferenceValue = zone;
            managerSerialized.FindProperty("targetCamera").objectReferenceValue = FindRecommendedCamera();
            managerSerialized.FindProperty("sourceMaterial").objectReferenceValue = material;
            managerSerialized.FindProperty("oceanShader").objectReferenceValue = LoadOceanShader();
            managerSerialized.FindProperty("farFieldShader").objectReferenceValue = LoadFarFieldShader();

            var chunkSettings = managerSerialized.FindProperty("chunkSettings");
            chunkSettings.FindPropertyRelative("shadowCastingMode").enumValueIndex = (int)ShadowCastingMode.Off;
            chunkSettings.FindPropertyRelative("receiveShadows").boolValue = false;
            ApplyContainedChunkProfile(profile, chunkSettings);

            var farFieldSettings = managerSerialized.FindProperty("farFieldSettings");
            farFieldSettings.FindPropertyRelative("enabled").boolValue = false;
            farFieldSettings.FindPropertyRelative("farDistance").floatValue = 2048f;
            farFieldSettings.FindPropertyRelative("verticalOffset").floatValue = -0.08f;
            managerSerialized.ApplyModifiedProperties();

            ApplyContainedZoneProfile(zone, profile);

            MarkDirty(manager);
            MarkDirty(zone);
        }

        private static void ApplyContainedZoneProfile(OceanWaterZone zone, RenderWaveWaterBodyProfile profile)
        {
            if (zone == null)
            {
                return;
            }

            Undo.RecordObject(zone, $"Apply {GetWaterBodyProfileDisplayName(profile)} Zone Profile");

            var zoneSerialized = new SerializedObject(zone);
            zoneSerialized.FindProperty("priority").intValue = 0;
            zoneSerialized.FindProperty("coverageMode").enumValueIndex = (int)OceanZoneCoverageMode.Rectangle;

            var settings = zoneSerialized.FindProperty("settings");
            settings.FindPropertyRelative("baseHeight").floatValue = zone.transform.position.y;
            settings.FindPropertyRelative("allowSwimming").boolValue = true;
            settings.FindPropertyRelative("underwaterEffectsEnabled").boolValue = true;
            ApplyContainedZoneSettings(profile, zoneSerialized.FindProperty("rectangleSize"), settings);
            zoneSerialized.ApplyModifiedProperties();

            MarkDirty(zone);
        }

        private static void ApplyContainedChunkProfile(RenderWaveWaterBodyProfile profile, SerializedProperty chunkSettings)
        {
            switch (profile)
            {
                case RenderWaveWaterBodyProfile.Pool:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 2;
                    chunkSettings.FindPropertyRelative("chunkSize").floatValue = 16f;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 10;
                    chunkSettings.FindPropertyRelative("enableDistanceCulling").boolValue = true;
                    chunkSettings.FindPropertyRelative("maxRenderDistance").floatValue = 120f;
                    break;
                case RenderWaveWaterBodyProfile.Lake:
                default:
                    chunkSettings.FindPropertyRelative("chunkRadius").intValue = 3;
                    chunkSettings.FindPropertyRelative("chunkSize").floatValue = 96f;
                    chunkSettings.FindPropertyRelative("meshResolution").intValue = 16;
                    chunkSettings.FindPropertyRelative("enableDistanceCulling").boolValue = false;
                    chunkSettings.FindPropertyRelative("maxRenderDistance").floatValue = 300f;
                    break;
            }
        }

        private static void ApplyContainedZoneSettings(RenderWaveWaterBodyProfile profile, SerializedProperty rectangleSize, SerializedProperty settings)
        {
            switch (profile)
            {
                case RenderWaveWaterBodyProfile.Pool:
                    rectangleSize.vector2Value = new Vector2(48f, 24f);
                    settings.FindPropertyRelative("zoneType").enumValueIndex = (int)WaterZoneType.Pool;
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 0.22f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.025f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 3.5f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 0.45f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.04f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 14f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.08f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.18f, 0.70f, 0.78f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.05f, 0.21f, 0.25f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.01f;
                    break;

                case RenderWaveWaterBodyProfile.Lake:
                default:
                    rectangleSize.vector2Value = new Vector2(768f, 768f);
                    settings.FindPropertyRelative("zoneType").enumValueIndex = (int)WaterZoneType.Lake;
                    settings.FindPropertyRelative("flowDirection").vector2Value = new Vector2(1f, 0.03f);
                    settings.FindPropertyRelative("vertexDisplacementStrength").floatValue = 0.55f;
                    settings.FindPropertyRelative("smallWaveIntensity").floatValue = 0.05f;
                    settings.FindPropertyRelative("smallWaveLength").floatValue = 5f;
                    settings.FindPropertyRelative("smallWaveSpeed").floatValue = 0.6f;
                    settings.FindPropertyRelative("largeWaveIntensity").floatValue = 0.14f;
                    settings.FindPropertyRelative("largeWaveLength").floatValue = 28f;
                    settings.FindPropertyRelative("largeWaveSpeed").floatValue = 0.12f;
                    settings.FindPropertyRelative("surfaceColor").colorValue = new Color(0.14f, 0.45f, 0.52f, 1f);
                    settings.FindPropertyRelative("underwaterColor").colorValue = new Color(0.03f, 0.14f, 0.18f, 1f);
                    settings.FindPropertyRelative("fogDensity").floatValue = 0.018f;
                    break;
            }
        }

        private static Material GetMaterialForWaterBodyProfile(RenderWaveWaterBodyProfile profile)
        {
            switch (profile)
            {
                case RenderWaveWaterBodyProfile.Pool:
                    return LoadTropicalOceanMaterial();
                case RenderWaveWaterBodyProfile.Lake:
                default:
                    return LoadCalmOceanMaterial();
            }
        }

        private static string GetWaterBodyProfileDisplayName(RenderWaveWaterBodyProfile profile)
        {
            switch (profile)
            {
                case RenderWaveWaterBodyProfile.Pool:
                    return "Pool";
                case RenderWaveWaterBodyProfile.Lake:
                default:
                    return "Lake";
            }
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static OceanWaterZone EnsureOceanZone(GameObject target)
        {
            return GetOrAddComponent<OceanWaterZone>(target);
        }

        private static T LoadAssetByGuidOrPath<T>(string guid, string fallbackPath) where T : Object
        {
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var guidPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(guidPath))
                {
                    var guidAsset = AssetDatabase.LoadAssetAtPath<T>(guidPath);
                    if (guidAsset != null)
                    {
                        return guidAsset;
                    }
                }
            }

            return AssetDatabase.LoadAssetAtPath<T>(fallbackPath);
        }

        private static void MarkDirty(Object target)
        {
            if (target == null)
            {
                return;
            }

            EditorUtility.SetDirty(target);
            if (target is Component component)
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
        }
    }
}
