using RenderWave.Runtime.Data;
using RenderWave.Runtime.Zones;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Runtime.Core
{
    /// <summary>
    /// Phase 1 runtime entry point that renders a chunked ocean around a single assigned camera.
    /// </summary>
    [AddComponentMenu("RenderWave/Ocean Manager")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OceanWaterZone))]
    public sealed class OceanManager : MonoBehaviour
    {
        private const int RecommendedMaxChunkRadius = 8;
        private const int RecommendedMaxMeshResolution = 64;
        private const int RecommendedMaxVisibleChunkCount = 169;
        private const float RecommendedMaxWaveDisplacement = 8f;
        private const float RecommendedFarFieldMargin = 128f;

        private static readonly int SurfaceColorId = Shader.PropertyToID("_SurfaceColor");
        private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
        private static readonly int LargeWaveDirectionId = Shader.PropertyToID("_LargeWaveDirection");
        private static readonly int SmallWaveDirectionId = Shader.PropertyToID("_SmallWaveDirection");
        private static readonly int LargeWaveDataId = Shader.PropertyToID("_LargeWaveData");
        private static readonly int SmallWaveDataId = Shader.PropertyToID("_SmallWaveData");
        private static readonly int RenderWaveTimeId = Shader.PropertyToID("_RenderWaveTime");
        private static readonly int ZoneClipCenterId = Shader.PropertyToID("_ZoneClipCenter");
        private static readonly int ZoneClipHalfExtentsId = Shader.PropertyToID("_ZoneClipHalfExtents");
        private static readonly int ZoneClipEnabledId = Shader.PropertyToID("_ZoneClipEnabled");
        private static readonly int ZoneBoundaryEpsilonId = Shader.PropertyToID("_ZoneBoundaryEpsilon");

        [SerializeField] private Camera targetCamera;
        [SerializeField] private OceanWaterZone oceanZone;
        [SerializeField] private OceanChunkSettings chunkSettings = new OceanChunkSettings();
        [SerializeField] private OceanFarFieldSettings farFieldSettings = new OceanFarFieldSettings();
        [SerializeField] private Material sourceMaterial;
        [SerializeField] private Shader oceanShader;
        [SerializeField] private Shader farFieldShader;

        private OceanChunkSystem chunkSystem;
        private OceanFarFieldSystem farFieldSystem;
        private Material runtimeMaterial;
        private bool runtimeZoneRegistered;
        private bool warnedAboutMissingCamera;

        public Camera TargetCamera => targetCamera;
        public OceanWaterZone OceanZone => oceanZone;

        private void Reset()
        {
            oceanZone = GetComponent<OceanWaterZone>();
            targetCamera = Camera.main;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            RegisterZoneForRuntime();
            chunkSystem.SetActive(true);
            farFieldSystem?.SetActive(ShouldUseFarField());
            chunkSystem.InvalidateLayout();
        }

        private void OnDisable()
        {
            UnregisterZoneForRuntime();

            if (chunkSystem != null)
            {
                chunkSystem.SetActive(false);
            }

            farFieldSystem?.SetActive(false);
        }

        private void OnValidate()
        {
            chunkSettings ??= new OceanChunkSettings();
            chunkSettings.Validate();
            farFieldSettings ??= new OceanFarFieldSettings();
            farFieldSettings.Validate();

            if (oceanZone == null)
            {
                oceanZone = GetComponent<OceanWaterZone>();
            }

            if (targetCamera == null && !Application.isPlaying)
            {
                targetCamera = Camera.main;
            }

            ValidateConfiguration(logWarnings: true);
        }

        private void LateUpdate()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            var currentCamera = ResolveCamera();
            if (currentCamera == null)
            {
                if (chunkSystem != null)
                {
                    chunkSystem.SetActive(false);
                }

                farFieldSystem?.SetActive(false);

                return;
            }

            warnedAboutMissingCamera = false;
            if (ShouldCullRenderByDistance(currentCamera.transform.position))
            {
                chunkSystem.SetActive(false);
                farFieldSystem?.SetActive(false);
                return;
            }

            chunkSystem.SetActive(true);

            var settings = oceanZone.Settings;
            var waveParameters = oceanZone.WaveParameters;
            var time = Time.time;

            UpdateMaterial(settings, waveParameters, time);
            chunkSystem.Update(currentCamera.transform.position, settings.BaseHeight, waveParameters.RecommendedMeshBoundsHeight);

            if (ShouldUseFarField())
            {
                EnsureFarFieldInitialized();
                if (farFieldSystem != null)
                {
                    farFieldSystem.SetActive(true);
                    farFieldSystem.Update(currentCamera.transform.position, settings.BaseHeight, settings, waveParameters, time);
                }
            }
            else
            {
                farFieldSystem?.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnregisterZoneForRuntime();

            if (chunkSystem != null)
            {
                chunkSystem.Dispose();
                chunkSystem = null;
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }

            if (farFieldSystem != null)
            {
                farFieldSystem.Dispose();
                farFieldSystem = null;
            }
        }

        private bool EnsureInitialized()
        {
            if (!ValidateConfiguration(logWarnings: false))
            {
                return false;
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = CreateRuntimeMaterial();
            }

            if (runtimeMaterial == null)
            {
                return false;
            }

            if (chunkSystem == null)
            {
                chunkSystem = new OceanChunkSystem(
                    transform,
                    chunkSettings,
                    oceanZone,
                    runtimeMaterial,
                    oceanZone.WaveParameters.RecommendedMeshBoundsHeight);
            }

            if (ShouldUseFarField())
            {
                EnsureFarFieldInitialized();
            }

            return true;
        }

        private void Initialize()
        {
            if (!ValidateConfiguration(logWarnings: true))
            {
                return;
            }

            EnsureInitialized();
        }

        private void RegisterZoneForRuntime()
        {
            if (runtimeZoneRegistered || oceanZone == null || !oceanZone.isActiveAndEnabled)
            {
                return;
            }

            WaterZoneRegistry.Register(oceanZone);
            runtimeZoneRegistered = true;
        }

        private void UnregisterZoneForRuntime()
        {
            if (!runtimeZoneRegistered || oceanZone == null)
            {
                return;
            }

            WaterZoneRegistry.Unregister(oceanZone);
            runtimeZoneRegistered = false;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
            {
                return targetCamera;
            }

            if (!warnedAboutMissingCamera)
            {
                Debug.LogError(
                    "RenderWave Phase 1 requires an explicit target camera. Assign the single render-driving camera to OceanManager.",
                    this);
                warnedAboutMissingCamera = true;
            }

            return null;
        }

        private Material CreateRuntimeMaterial()
        {
            Material material;

            if (sourceMaterial != null)
            {
                material = new Material(sourceMaterial)
                {
                    name = $"{sourceMaterial.name} (Runtime)",
                    hideFlags = HideFlags.DontSave
                };
            }
            else
            {
                var resolvedShader = oceanShader != null ? oceanShader : Shader.Find("RenderWave/URP/OceanSurface");
                if (resolvedShader == null)
                {
                    Debug.LogError("RenderWave OceanManager could not find the required ocean shader.", this);
                    return null;
                }

                material = new Material(resolvedShader)
                {
                    name = "RenderWave Ocean Runtime Material",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (!HasRequiredMaterialProperties(material))
            {
                Debug.LogError("RenderWave OceanManager requires a material that uses the RenderWave Phase 1 property contract.", this);
                Destroy(material);
                return null;
            }

            return material;
        }

        private void UpdateMaterial(WaterZoneSettings settings, WaterWaveParameters waveParameters, float time)
        {
            runtimeMaterial.SetColor(SurfaceColorId, settings.SurfaceColor);
            runtimeMaterial.SetColor(DeepColorId, settings.UnderwaterColor);
            runtimeMaterial.SetVector(LargeWaveDirectionId, new Vector4(waveParameters.LargeDirection.x, waveParameters.LargeDirection.y, 0f, 0f));
            runtimeMaterial.SetVector(SmallWaveDirectionId, new Vector4(waveParameters.SmallDirection.x, waveParameters.SmallDirection.y, 0f, 0f));
            runtimeMaterial.SetVector(LargeWaveDataId, waveParameters.LargeWaveData);
            runtimeMaterial.SetVector(SmallWaveDataId, waveParameters.SmallWaveData);
            runtimeMaterial.SetFloat(RenderWaveTimeId, time);

            if (oceanZone.CoverageMode == OceanZoneCoverageMode.Rectangle)
            {
                var center = oceanZone.Center;
                var halfExtents = oceanZone.RectangleSize * 0.5f;
                runtimeMaterial.SetVector(ZoneClipCenterId, new Vector4(center.x, center.z, 0f, 0f));
                runtimeMaterial.SetVector(ZoneClipHalfExtentsId, new Vector4(halfExtents.x, halfExtents.y, 0f, 0f));
                runtimeMaterial.SetFloat(ZoneClipEnabledId, 1f);
                runtimeMaterial.SetFloat(ZoneBoundaryEpsilonId, oceanZone.CoveragePadding);
            }
            else
            {
                runtimeMaterial.SetFloat(ZoneClipEnabledId, 0f);
                runtimeMaterial.SetFloat(ZoneBoundaryEpsilonId, 0f);
            }
        }

        private void EnsureFarFieldInitialized()
        {
            if (farFieldSystem != null)
            {
                return;
            }

            var sourceForFarField = sourceMaterial != null ? sourceMaterial : runtimeMaterial;
            farFieldSystem = new OceanFarFieldSystem(transform, chunkSettings, farFieldSettings, sourceForFarField, farFieldShader);
            if (!farFieldSystem.IsValid)
            {
                farFieldSystem.Dispose();
                farFieldSystem = null;
            }
        }

        private bool ShouldUseFarField()
        {
            return oceanZone != null &&
                   oceanZone.CoverageMode == OceanZoneCoverageMode.Infinite &&
                   oceanZone.Settings.ZoneType == WaterZoneType.Ocean &&
                   farFieldSettings != null &&
                   farFieldSettings.Enabled;
        }

        private bool ShouldCullRenderByDistance(Vector3 cameraPosition)
        {
            if (oceanZone == null || !chunkSettings.EnableDistanceCulling || oceanZone.CoverageMode != OceanZoneCoverageMode.Rectangle)
            {
                return false;
            }

            var center = oceanZone.Center;
            var halfExtents = oceanZone.RectangleSize * 0.5f;
            var closestPoint = new Vector3(
                Mathf.Clamp(cameraPosition.x, center.x - halfExtents.x, center.x + halfExtents.x),
                oceanZone.Settings.BaseHeight,
                Mathf.Clamp(cameraPosition.z, center.z - halfExtents.y, center.z + halfExtents.y));

            var sqrDistance = (cameraPosition - closestPoint).sqrMagnitude;
            return sqrDistance > (chunkSettings.MaxRenderDistance * chunkSettings.MaxRenderDistance);
        }

        private bool ValidateConfiguration(bool logWarnings)
        {
            chunkSettings ??= new OceanChunkSettings();
            chunkSettings.Validate();
            farFieldSettings ??= new OceanFarFieldSettings();
            farFieldSettings.Validate();

            if (oceanZone == null)
            {
                oceanZone = GetComponent<OceanWaterZone>();
            }

            if (oceanZone == null)
            {
                if (logWarnings)
                {
                    Debug.LogError("RenderWave OceanManager requires an OceanWaterZone on the same GameObject.", this);
                }

                return false;
            }

            var isValid = true;

            if (oceanZone.gameObject != gameObject)
            {
                if (logWarnings)
                {
                    Debug.LogError(
                        "RenderWave Phase 1 requires OceanManager and OceanWaterZone to live on the same GameObject so render and query lifecycle stay aligned.",
                        this);
                }

                isValid = false;
            }

            if (!HasIdentityRotationAndUnitScale(transform))
            {
                if (logWarnings)
                {
                    Debug.LogError(
                        "RenderWave Phase 1 requires the OceanManager transform hierarchy to use identity rotation and unit scale.",
                        this);
                }

                isValid = false;
            }

            if (oceanZone.CoverageMode == OceanZoneCoverageMode.Rectangle && !HasIdentityRotationAndUnitScale(oceanZone.transform))
            {
                if (logWarnings)
                {
                    Debug.LogError(
                        "RenderWave Phase 1 rectangle coverage is axis-aligned in world space and requires identity rotation and unit scale on the OceanWaterZone.",
                        oceanZone);
                }

                isValid = false;
            }

            if (targetCamera == null && Application.isPlaying)
            {
                if (logWarnings)
                {
                    Debug.LogError(
                        "RenderWave Phase 1 requires an explicit target camera assignment. One OceanManager supports one render-driving camera.",
                        this);
                }

                isValid = false;
            }

            if (!IsUniversalRenderPipelineActive() && logWarnings)
            {
                Debug.LogWarning("RenderWave Phase 1 is designed for URP. Assign a Universal Render Pipeline asset before testing.", this);
            }

            if (logWarnings)
            {
                LogConfigurationWarnings();
            }

            return isValid;
        }

        private void LogConfigurationWarnings()
        {
            if (chunkSettings.ChunkRadius > RecommendedMaxChunkRadius)
            {
                Debug.LogWarning(
                    $"RenderWave chunk radius {chunkSettings.ChunkRadius} exceeds the recommended Phase 1 limit of {RecommendedMaxChunkRadius} and may create too many draw calls.",
                    this);
            }

            if (chunkSettings.MeshResolution > RecommendedMaxMeshResolution)
            {
                Debug.LogWarning(
                    $"RenderWave mesh resolution {chunkSettings.MeshResolution} exceeds the recommended Phase 1 limit of {RecommendedMaxMeshResolution} and may become expensive at scale.",
                    this);
            }

            if (chunkSettings.TotalChunkCount > RecommendedMaxVisibleChunkCount)
            {
                Debug.LogWarning(
                    $"RenderWave is configured to render {chunkSettings.TotalChunkCount} chunks. This exceeds the recommended Phase 1 limit of {RecommendedMaxVisibleChunkCount} visible chunks.",
                    this);
            }

            if (oceanZone != null)
            {
                var maxWaveDisplacement = oceanZone.MaxVerticalDisplacement;
                if (maxWaveDisplacement > RecommendedMaxWaveDisplacement)
                {
                    Debug.LogWarning(
                        $"RenderWave maximum wave displacement {maxWaveDisplacement:0.###} exceeds the recommended Phase 1 limit of {RecommendedMaxWaveDisplacement:0.###}. This increases culling bounds and query sensitivity.",
                        oceanZone);
                }

                if (maxWaveDisplacement > chunkSettings.ChunkSize * 0.25f)
                {
                    Debug.LogWarning(
                        "RenderWave wave displacement is very large relative to chunk size. This can cause unstable shading and poor commercial-default behavior.",
                        oceanZone);
                }
            }

            if (ShouldUseFarField())
            {
                var nearHalfExtent = ((chunkSettings.ChunkRadius + 0.5f) * chunkSettings.ChunkSize);
                if (farFieldSettings.FarDistance <= nearHalfExtent + RecommendedFarFieldMargin)
                {
                    Debug.LogWarning(
                        "RenderWave far-field distance is too close to the near chunk footprint. Increase Far Distance or disable Far Field if you do not need long-range horizon coverage.",
                        this);
                }
            }
        }

        private static bool HasRequiredMaterialProperties(Material material)
        {
            return material != null &&
                   material.HasProperty(SurfaceColorId) &&
                   material.HasProperty(DeepColorId) &&
                   material.HasProperty(LargeWaveDirectionId) &&
                   material.HasProperty(SmallWaveDirectionId) &&
                   material.HasProperty(LargeWaveDataId) &&
                   material.HasProperty(SmallWaveDataId) &&
                   material.HasProperty(RenderWaveTimeId) &&
                   material.HasProperty(ZoneClipCenterId) &&
                   material.HasProperty(ZoneClipHalfExtentsId) &&
                   material.HasProperty(ZoneClipEnabledId) &&
                   material.HasProperty(ZoneBoundaryEpsilonId);
        }

        private static bool HasIdentityRotationAndUnitScale(Transform targetTransform)
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

        private static bool IsUniversalRenderPipelineActive()
        {
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            return renderPipeline != null && renderPipeline.GetType().Name.Contains("UniversalRenderPipelineAsset");
        }
    }
}
