using RenderWave.Runtime.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Runtime.Underwater
{
    internal sealed class UnderwaterFullscreenOverlay
    {
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int VisibilityDistanceId = Shader.PropertyToID("_VisibilityDistance");
        private static readonly int MaxOpacityId = Shader.PropertyToID("_MaxOpacity");
        private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        private static readonly int NearSurfaceLightId = Shader.PropertyToID("_NearSurfaceLight");
        private static readonly int TransitionWeightId = Shader.PropertyToID("_TransitionWeight");
        private static readonly int SignedDistanceId = Shader.PropertyToID("_SignedDistanceToSurface");
        private static readonly int DepthBelowSurfaceId = Shader.PropertyToID("_DepthBelowSurface");
        private static readonly int EnhancedModeEnabledId = Shader.PropertyToID("_EnhancedModeEnabled");
        private static readonly int CausticsTexId = Shader.PropertyToID("_CausticsTex");
        private static readonly int CausticsEnabledId = Shader.PropertyToID("_CausticsEnabled");
        private static readonly int CausticColorId = Shader.PropertyToID("_CausticColor");
        private static readonly int CausticIntensityId = Shader.PropertyToID("_CausticIntensity");
        private static readonly int CausticScaleId = Shader.PropertyToID("_CausticScale");
        private static readonly int CausticSpeedId = Shader.PropertyToID("_CausticSpeed");
        private static readonly int AbsorptionStrengthId = Shader.PropertyToID("_AbsorptionStrength");
        private static readonly int DeepScatterColorId = Shader.PropertyToID("_DeepScatterColor");
        private static readonly int DepthColorShiftRangeId = Shader.PropertyToID("_DepthColorShiftRange");
        private static readonly int DepthOcclusionStrengthId = Shader.PropertyToID("_DepthOcclusionStrength");
        private static readonly int AbyssStrengthId = Shader.PropertyToID("_AbyssStrength");
        private static readonly int AmbientTransmissionFloorId = Shader.PropertyToID("_AmbientTransmissionFloor");
        private static readonly int WaterlineEnabledId = Shader.PropertyToID("_WaterlineEnabled");
        private static readonly int WaterlineLeftYId = Shader.PropertyToID("_WaterlineLeftY");
        private static readonly int WaterlineCenterYId = Shader.PropertyToID("_WaterlineCenterY");
        private static readonly int WaterlineRightYId = Shader.PropertyToID("_WaterlineRightY");
        private static readonly int WaterlineEdgeSoftnessId = Shader.PropertyToID("_WaterlineEdgeSoftness");
        private static readonly int WaterlineHighlightStrengthId = Shader.PropertyToID("_WaterlineHighlightStrength");
        private static readonly int WaterlineBandThicknessId = Shader.PropertyToID("_WaterlineBandThickness");
        private static readonly int WaterlineNoiseScaleId = Shader.PropertyToID("_WaterlineNoiseScale");
        private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
        private static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
        private static Mesh sharedQuadMesh;

        private readonly Material material;
        private readonly Transform overlayTransform;
        private readonly GameObject overlayObject;
        private readonly MeshRenderer meshRenderer;

        private Camera attachedCamera;

        public UnderwaterFullscreenOverlay(Material sourceMaterial, Shader fallbackShader)
        {
            material = CreateMaterial(sourceMaterial, fallbackShader);
            if (material == null)
            {
                return;
            }

            overlayObject = new GameObject("Underwater Overlay");
            overlayTransform = overlayObject.transform;

            var meshFilter = overlayObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetOrCreateQuadMesh();

            meshRenderer = overlayObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.allowOcclusionWhenDynamic = false;

            SetActive(false);
        }

        public bool IsValid => material != null && overlayObject != null;

        public void SetActive(bool isActive)
        {
            if (overlayObject != null && overlayObject.activeSelf != isActive)
            {
                overlayObject.SetActive(isActive);
            }
        }

        public void UpdateForCamera(Camera camera)
        {
            if (!IsValid || camera == null)
            {
                return;
            }

            if (attachedCamera != camera)
            {
                attachedCamera = camera;
                overlayTransform.SetParent(camera.transform, false);
                overlayTransform.localRotation = Quaternion.identity;
            }

            var distance = Mathf.Max(camera.nearClipPlane + 0.05f, 0.05f);
            float height;
            float width;

            if (camera.orthographic)
            {
                height = camera.orthographicSize * 2f;
                width = height * camera.aspect;
            }
            else
            {
                height = 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
                width = height * camera.aspect;
            }

            overlayTransform.localPosition = new Vector3(0f, 0f, distance);
            overlayTransform.localScale = new Vector3(width, height, 1f);
        }

        public void Apply(UnderwaterVisualSettings settings, UnderwaterStateData state, float time)
        {
            if (!IsValid)
            {
                return;
            }

            material.SetColor(TintColorId, settings.TintColor);
            material.SetFloat(VisibilityDistanceId, settings.VisibilityDistance);
            material.SetFloat(MaxOpacityId, settings.MaxOpacity);
            material.SetFloat(TintStrengthId, settings.TintStrength);
            material.SetFloat(NearSurfaceLightId, settings.NearSurfaceLight);
            material.SetFloat(TransitionWeightId, state.TransitionWeight);
            material.SetFloat(SignedDistanceId, state.SignedDistanceToSurface);
            material.SetFloat(DepthBelowSurfaceId, state.DepthBelowSurface);
            material.SetFloat(EnhancedModeEnabledId, settings.EnhancedModeEnabled ? 1f : 0f);

            material.SetFloat(AbsorptionStrengthId, settings.AbsorptionStrength);
            material.SetColor(DeepScatterColorId, settings.DeepScatterColor);
            material.SetFloat(DepthColorShiftRangeId, settings.DepthColorShiftRange);
            material.SetFloat(DepthOcclusionStrengthId, settings.DepthOcclusionStrength);
            material.SetFloat(AbyssStrengthId, settings.AbyssStrength);
            material.SetFloat(AmbientTransmissionFloorId, settings.AmbientTransmissionFloor);

            if (settings.CausticsEnabled && settings.CausticsTexture != null)
            {
                material.SetTexture(CausticsTexId, settings.CausticsTexture);
                material.SetFloat(CausticsEnabledId, 1f);
                material.SetColor(CausticColorId, settings.CausticColor);
                material.SetFloat(CausticIntensityId, settings.CausticIntensity);
                material.SetFloat(CausticScaleId, settings.CausticScale);
                material.SetVector(CausticSpeedId, new Vector4(settings.CausticSpeed.x, settings.CausticSpeed.y, time, 0f));
            }
            else
            {
                material.SetFloat(CausticsEnabledId, 0f);
                material.SetFloat(CausticIntensityId, 0f);
            }

            if (settings.EnhancedModeEnabled)
            {
                material.SetFloat(DistortionStrengthId, settings.DistortionStrength);
                material.SetFloat(VignetteStrengthId, settings.UnderwaterVignetteStrength);
                material.SetFloat(WaterlineBandThicknessId, settings.WaterlineBandThickness);
                material.SetFloat(WaterlineNoiseScaleId, settings.WaterlineNoiseScale);
            }
            else
            {
                material.SetFloat(DistortionStrengthId, 0f);
                material.SetFloat(VignetteStrengthId, 0f);
                material.SetFloat(WaterlineEnabledId, 0f);
            }

            // Project the waterline only during the transition crossing window so
            // the overlay never carries visible state when the camera is above water.
            if (settings.EnhancedModeEnabled && attachedCamera != null && state.IsTransitioning)
            {
                ProjectWaterline(state, settings);
            }
            else
            {
                material.SetFloat(WaterlineEnabledId, 0f);
            }
        }

        private void ProjectWaterline(UnderwaterStateData state, UnderwaterVisualSettings settings)
        {
            var camera = attachedCamera;
            var baseDistance = Mathf.Max(camera.nearClipPlane + 1.5f, 2.5f);
            var surfaceBias = state.HasWaterQuery && float.IsFinite(state.SignedDistanceToSurface)
                ? Mathf.Abs(state.SignedDistanceToSurface) * 3f
                : 0f;
            var projectionDistance = Mathf.Clamp(baseDistance + surfaceBias, baseDistance, 12f);

            if (!TryProjectWaterSample(camera, 0f, projectionDistance, state.SurfaceHeight, out var leftY) ||
                !TryProjectWaterSample(camera, 1f, projectionDistance, state.SurfaceHeight, out var rightY))
            {
                material.SetFloat(WaterlineEnabledId, 0f);
                return;
            }

            // Always sample the screen-center point so the Bézier curve in the
            // shader actually curves — without it, leftY/rightY collapse to a
            // straight line (the Bézier degenerates to a lerp when centerY is
            // exactly the arithmetic mean).
            if (!TryProjectWaterSample(camera, 0.5f, projectionDistance, state.SurfaceHeight, out var centerY))
            {
                centerY = 0.5f * (leftY + rightY);
            }

            material.SetFloat(WaterlineEnabledId, 1f);
            material.SetFloat(WaterlineLeftYId, leftY);
            material.SetFloat(WaterlineCenterYId, centerY);
            material.SetFloat(WaterlineRightYId, rightY);
            material.SetFloat(WaterlineEdgeSoftnessId, settings.WaterlineEdgeSoftness);
            material.SetFloat(WaterlineHighlightStrengthId, settings.WaterlineHighlightStrength);
            material.SetFloat(WaterlineBandThicknessId, settings.WaterlineBandThickness);
            material.SetFloat(WaterlineNoiseScaleId, settings.WaterlineNoiseScale);
        }

        private static bool TryProjectWaterSample(Camera camera, float viewportX, float projectionDistance,
                                                   float fallbackSurfaceHeight, out float viewportY)
        {
            var ray = camera.ViewportPointToRay(new Vector3(viewportX, 0.5f, 0f));
            var samplePosition = ray.origin + (ray.direction * projectionDistance);

            var surfaceHeight = fallbackSurfaceHeight;
            if (WaterLevelQueryService.TryGetWaterQuery(samplePosition, out var waterQuery))
            {
                surfaceHeight = waterQuery.SurfaceHeight;
            }

            var worldPoint = new Vector3(samplePosition.x, surfaceHeight, samplePosition.z);
            var viewportPoint = camera.WorldToViewportPoint(worldPoint);
            if (viewportPoint.z < 0f || !float.IsFinite(viewportPoint.y))
            {
                viewportY = 0f;
                return false;
            }

            viewportY = viewportPoint.y;
            return true;
        }

        public void Dispose()
        {
            if (overlayObject != null)
            {
                Object.Destroy(overlayObject);
            }

            if (material != null)
            {
                Object.Destroy(material);
            }
        }

        private static Material CreateMaterial(Material sourceMaterial, Shader fallbackShader)
        {
            Material instance;

            if (sourceMaterial != null)
            {
                instance = new Material(sourceMaterial)
                {
                    name = $"{sourceMaterial.name} (Runtime)",
                    hideFlags = HideFlags.DontSave
                };
            }
            else
            {
                var shader = fallbackShader != null ? fallbackShader : Shader.Find("RenderWave/URP/UnderwaterOverlay");
                if (shader == null)
                {
                    return null;
                }

                instance = new Material(shader)
                {
                    name = "RenderWave Underwater Overlay (Runtime)",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (HasRequiredProperties(instance))
            {
                return instance;
            }

            Object.Destroy(instance);
            return null;
        }

        private static bool HasRequiredProperties(Material candidate)
        {
            return candidate != null &&
                   candidate.HasProperty(TintColorId) &&
                   candidate.HasProperty(VisibilityDistanceId) &&
                   candidate.HasProperty(MaxOpacityId) &&
                   candidate.HasProperty(TintStrengthId) &&
                   candidate.HasProperty(NearSurfaceLightId) &&
                   candidate.HasProperty(TransitionWeightId) &&
                   candidate.HasProperty(SignedDistanceId) &&
                   candidate.HasProperty(DepthBelowSurfaceId) &&
                   candidate.HasProperty(EnhancedModeEnabledId) &&
                   candidate.HasProperty(CausticsTexId) &&
                   candidate.HasProperty(CausticsEnabledId) &&
                   candidate.HasProperty(CausticColorId) &&
                   candidate.HasProperty(CausticIntensityId) &&
                   candidate.HasProperty(CausticScaleId) &&
                   candidate.HasProperty(CausticSpeedId) &&
                   candidate.HasProperty(AbsorptionStrengthId) &&
                   candidate.HasProperty(DeepScatterColorId) &&
                   candidate.HasProperty(DepthColorShiftRangeId) &&
                   candidate.HasProperty(DepthOcclusionStrengthId) &&
                   candidate.HasProperty(AbyssStrengthId) &&
                   candidate.HasProperty(AmbientTransmissionFloorId) &&
                   candidate.HasProperty(WaterlineEnabledId) &&
                   candidate.HasProperty(WaterlineLeftYId) &&
                   candidate.HasProperty(WaterlineCenterYId) &&
                   candidate.HasProperty(WaterlineRightYId) &&
                   candidate.HasProperty(WaterlineEdgeSoftnessId) &&
                   candidate.HasProperty(WaterlineHighlightStrengthId) &&
                   candidate.HasProperty(WaterlineBandThicknessId) &&
                   candidate.HasProperty(WaterlineNoiseScaleId) &&
                   candidate.HasProperty(DistortionStrengthId) &&
                   candidate.HasProperty(VignetteStrengthId);
        }

        private static Mesh GetOrCreateQuadMesh()
        {
            if (sharedQuadMesh != null)
            {
                return sharedQuadMesh;
            }

            sharedQuadMesh = new Mesh
            {
                name = "RenderWave Underwater Quad"
            };

            sharedQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };

            sharedQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            sharedQuadMesh.normals = new[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };

            sharedQuadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            sharedQuadMesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 0.1f));
            return sharedQuadMesh;
        }
    }
}
