using RenderWave.Runtime.Core;
using UnityEngine;

namespace RenderWave.Runtime.Underwater
{
    /// <summary>
    /// Per-camera underwater state driver with stable waterline crossing and a lightweight visual overlay.
    /// </summary>
    [AddComponentMenu("RenderWave/Underwater State Controller")]
    [DisallowMultipleComponent]
    public sealed class UnderwaterStateController : MonoBehaviour
    {
        [SerializeField] private OceanManager oceanManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private UnderwaterTransitionSettings transitionSettings = new UnderwaterTransitionSettings();
        [SerializeField] private UnderwaterVisualSettings visualSettings = new UnderwaterVisualSettings();
        [SerializeField] private Material overlaySourceMaterial;
        [SerializeField] private Shader overlayShader;

        private UnderwaterFullscreenOverlay overlay;
        private UnderwaterStateData currentState;
        private bool stableUnderwater;
        private float transitionWeight;
        private bool warnedAboutCamera;
        private bool warnedAboutConfiguration;

        /// <summary>
        /// Most recent evaluated underwater state for the configured camera.
        /// </summary>
        public UnderwaterStateData CurrentState => currentState;

        private void Reset()
        {
            oceanManager = GetComponent<OceanManager>();
            if (oceanManager != null)
            {
                targetCamera = oceanManager.TargetCamera;
            }
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

            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        private void OnDisable()
        {
            stableUnderwater = false;
            transitionWeight = 0f;
            currentState = default;

            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (overlay != null)
            {
                overlay.Dispose();
                overlay = null;
            }
        }

        private void OnValidate()
        {
            transitionSettings ??= new UnderwaterTransitionSettings();
            visualSettings ??= new UnderwaterVisualSettings();
            transitionSettings.Validate();
            visualSettings.Validate();

            if (oceanManager == null)
            {
                oceanManager = GetComponent<OceanManager>();
            }

            if (oceanManager != null && targetCamera == null)
            {
                targetCamera = oceanManager.TargetCamera;
            }

            ValidateConfiguration(logWarnings: true);
        }

        private void LateUpdate()
        {
            if (!EnsureInitialized())
            {
                UpdateStateWithoutWater();
                return;
            }

            var camera = ResolveCamera();
            if (camera == null)
            {
                UpdateStateWithoutWater();
                SetOverlayActive(false);
                return;
            }

            camera.depthTextureMode |= DepthTextureMode.Depth;

            var hasWaterQuery = WaterLevelQueryService.TryGetWaterQuery(camera.transform.position, out var waterQuery);
            var signedDistance = hasWaterQuery ? waterQuery.SignedDistanceToSurface : float.PositiveInfinity;
            var visualsAllowed = hasWaterQuery && waterQuery.UnderwaterEffectsEnabled;

            stableUnderwater = EvaluateStableUnderwaterState(hasWaterQuery, signedDistance);

            var targetWeight = stableUnderwater && visualsAllowed ? 1f : 0f;
            var fadeSpeed = targetWeight > transitionWeight ? transitionSettings.FadeInSpeed : transitionSettings.FadeOutSpeed;
            transitionWeight = Mathf.MoveTowards(transitionWeight, targetWeight, fadeSpeed * Time.deltaTime);

            currentState = new UnderwaterStateData(
                hasWaterQuery,
                stableUnderwater,
                visualsAllowed,
                signedDistance,
                hasWaterQuery ? waterQuery.SurfaceHeight : 0f,
                transitionWeight,
                waterQuery);

            if (visualSettings.VisualsEnabled && currentState.VisualsActive)
            {
                overlay.UpdateForCamera(camera);
                overlay.Apply(visualSettings, currentState, Time.time);
                SetOverlayActive(true);
            }
            else
            {
                SetOverlayActive(false);
            }
        }

        private void Initialize()
        {
            transitionSettings ??= new UnderwaterTransitionSettings();
            visualSettings ??= new UnderwaterVisualSettings();
            transitionSettings.Validate();
            visualSettings.Validate();

            if (!ValidateConfiguration(logWarnings: true))
            {
                return;
            }

            if (visualSettings.VisualsEnabled && overlay == null)
            {
                overlay = new UnderwaterFullscreenOverlay(overlaySourceMaterial, overlayShader);
                if (!overlay.IsValid)
                {
                    Debug.LogError("RenderWave underwater overlay could not create a valid runtime material.", this);
                    overlay = null;
                }
            }
        }

        private bool EnsureInitialized()
        {
            if (!ValidateConfiguration(logWarnings: false))
            {
                return false;
            }

            if (visualSettings.VisualsEnabled && overlay == null)
            {
                Initialize();
            }

            return !visualSettings.VisualsEnabled || overlay != null;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
            {
                warnedAboutCamera = false;
                return targetCamera;
            }

            if (!warnedAboutCamera)
            {
                Debug.LogError(
                    "RenderWave UnderwaterStateController requires an explicit active target camera that matches the supported OceanManager camera contract.",
                    this);
                warnedAboutCamera = true;
            }

            return null;
        }

        private bool EvaluateStableUnderwaterState(bool hasWaterQuery, float signedDistance)
        {
            if (!hasWaterQuery)
            {
                return false;
            }

            if (stableUnderwater)
            {
                return signedDistance < transitionSettings.ExitThreshold;
            }

            return signedDistance <= -transitionSettings.EnterThreshold;
        }

        private void UpdateStateWithoutWater()
        {
            transitionWeight = Mathf.MoveTowards(transitionWeight, 0f, transitionSettings.FadeOutSpeed * Time.deltaTime);
            stableUnderwater = false;
            currentState = new UnderwaterStateData(false, false, false, float.PositiveInfinity, 0f, transitionWeight, default);
        }

        private void SetOverlayActive(bool isActive)
        {
            if (overlay != null)
            {
                overlay.SetActive(isActive);
            }
        }

        private bool ValidateConfiguration(bool logWarnings)
        {
            transitionSettings ??= new UnderwaterTransitionSettings();
            visualSettings ??= new UnderwaterVisualSettings();
            transitionSettings.Validate();
            visualSettings.Validate();

            if (oceanManager == null)
            {
                oceanManager = GetComponent<OceanManager>();
            }

            if (oceanManager != null && targetCamera == null)
            {
                targetCamera = oceanManager.TargetCamera;
            }

            var isValid = true;

            if (targetCamera == null)
            {
                if (logWarnings && !warnedAboutConfiguration)
                {
                    Debug.LogError(
                        "RenderWave UnderwaterStateController requires an explicit target camera or an OceanManager with an assigned target camera.",
                        this);
                    warnedAboutConfiguration = true;
                }

                isValid = false;
            }

            if (oceanManager != null && oceanManager.TargetCamera != null && targetCamera != null && oceanManager.TargetCamera != targetCamera)
            {
                if (logWarnings)
                {
                    Debug.LogError(
                        "RenderWave UnderwaterStateController target camera must match the OceanManager target camera for Phase 3.",
                        this);
                }

                isValid = false;
            }

            return isValid;
        }
    }
}
