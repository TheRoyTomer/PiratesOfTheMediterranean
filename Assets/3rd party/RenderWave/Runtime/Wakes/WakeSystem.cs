using System.Collections.Generic;
using RenderWave.Runtime.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Runtime.Wakes
{
    /// <summary>
    /// Central pooled wake renderer that batches lightweight wake quads into a single dynamic mesh.
    /// </summary>
    [AddComponentMenu("RenderWave/Wake System")]
    [DisallowMultipleComponent]
    public sealed class WakeSystem : MonoBehaviour
    {
        private static readonly int WakeColorId = Shader.PropertyToID("_WakeColor");
        private const int RecommendedMaxActiveWakes = 512;

        private static WakeSystem defaultInstance;

        [SerializeField, Min(32)] private int maxActiveWakes = 256;
        [SerializeField] private Material sourceMaterial;
        [SerializeField] private Shader wakeShader;
        [SerializeField] private Color wakeColor = new Color(0.82f, 0.93f, 0.98f, 0.6f);

        private readonly List<WakeEmitter> emitters = new List<WakeEmitter>(16);

        private WakeInstance[] wakes;
        private int wakeCount;
        private Mesh wakeMesh;
        private Material runtimeMaterial;
        private Transform renderRoot;
        private MeshRenderer meshRenderer;
        private Vector3[] vertices;
        private Vector2[] uvs;
        private Color32[] colors;
        private int[] triangles;
        private bool warnedAboutMultipleSystems;

        public static WakeSystem Default => defaultInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            defaultInstance = null;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            SetAsDefault();
            DiscoverEmitters();
            SetRendererActive(false);
        }

        private void OnDisable()
        {
            ClearWakeState();
            SetRendererActive(false);

            if (defaultInstance == this)
            {
                defaultInstance = null;
            }
        }

        private void OnDestroy()
        {
            if (defaultInstance == this)
            {
                defaultInstance = null;
            }

            if (renderRoot != null)
            {
                Destroy(renderRoot.gameObject);
                renderRoot = null;
            }

            if (wakeMesh != null)
            {
                Destroy(wakeMesh);
                wakeMesh = null;
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private void OnValidate()
        {
            maxActiveWakes = Mathf.Max(32, maxActiveWakes);

            if (maxActiveWakes > RecommendedMaxActiveWakes)
            {
                Debug.LogWarning(
                    $"RenderWave WakeSystem maxActiveWakes {maxActiveWakes} exceeds the recommended V1 limit of {RecommendedMaxActiveWakes}. This can increase transparent overdraw and dynamic mesh cost.",
                    this);
            }
        }

        private void LateUpdate()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            runtimeMaterial.SetColor(WakeColorId, wakeColor);

            var deltaTime = Time.deltaTime;
            for (var i = emitters.Count - 1; i >= 0; i--)
            {
                var emitter = emitters[i];
                if (emitter == null)
                {
                    emitters.RemoveAt(i);
                    continue;
                }

                if (!emitter.isActiveAndEnabled)
                {
                    continue;
                }

                emitter.Tick(this, deltaTime);
            }

            UpdateWakeLifetimes(deltaTime);
            RebuildMesh();
        }

        public void RegisterEmitter(WakeEmitter emitter)
        {
            if (emitter == null || emitters.Contains(emitter))
            {
                return;
            }

            emitters.Add(emitter);
        }

        public void UnregisterEmitter(WakeEmitter emitter)
        {
            if (emitter == null)
            {
                return;
            }

            emitters.Remove(emitter);
        }

        internal bool EmitWake(Vector3 worldPosition, Vector3 forward, float width, float length, float lifetime, float fadeStart, float intensity, float surfaceOffset)
        {
            if (!WaterLevelQueryService.TryGetWaterQuery(worldPosition, out var waterQuery))
            {
                return false;
            }

            return EmitWake(worldPosition, waterQuery, forward, width, length, lifetime, fadeStart, intensity, surfaceOffset);
        }

        internal bool EmitWake(Vector3 worldPosition, in RenderWave.Runtime.Data.WaterQueryResult waterQuery, Vector3 forward, float width, float length, float lifetime, float fadeStart, float intensity, float surfaceOffset)
        {
            var wake = new WakeInstance
            {
                Position = new Vector3(worldPosition.x, waterQuery.SurfaceHeight + surfaceOffset, worldPosition.z),
                Forward = forward.normalized,
                SurfaceNormal = waterQuery.SurfaceNormal,
                Width = width,
                Length = length,
                Lifetime = lifetime,
                Age = 0f,
                FadeStart = fadeStart,
                Intensity = intensity,
                SurfaceOffset = surfaceOffset
            };

            if (wakeCount < wakes.Length)
            {
                wakes[wakeCount] = wake;
                wakeCount++;
                return true;
            }

            var oldestIndex = 0;
            var oldestAgeNormalized = float.MinValue;
            for (var i = 0; i < wakeCount; i++)
            {
                var ageNormalized = wakes[i].Age / Mathf.Max(wakes[i].Lifetime, 0.0001f);
                if (ageNormalized > oldestAgeNormalized)
                {
                    oldestAgeNormalized = ageNormalized;
                    oldestIndex = i;
                }
            }

            wakes[oldestIndex] = wake;
            return true;
        }

        private void Initialize()
        {
            maxActiveWakes = Mathf.Max(32, maxActiveWakes);

            if (wakes == null || wakes.Length != maxActiveWakes)
            {
                wakes = new WakeInstance[maxActiveWakes];
                vertices = new Vector3[maxActiveWakes * 4];
                uvs = new Vector2[maxActiveWakes * 4];
                colors = new Color32[maxActiveWakes * 4];
                triangles = new int[maxActiveWakes * 6];
                BuildStaticMeshData();
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = CreateRuntimeMaterial();
            }

            if (runtimeMaterial == null)
            {
                return;
            }

            runtimeMaterial.SetColor(WakeColorId, wakeColor);

            if (renderRoot == null)
            {
                var rootObject = new GameObject("Wake Renderer");
                rootObject.hideFlags = HideFlags.DontSave;
                rootObject.transform.SetParent(transform, false);
                renderRoot = rootObject.transform;

                var meshFilter = rootObject.AddComponent<MeshFilter>();
                wakeMesh = new Mesh
                {
                    name = "RenderWave Wake Mesh"
                };
                wakeMesh.MarkDynamic();
                meshFilter.sharedMesh = wakeMesh;

                meshRenderer = rootObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = runtimeMaterial;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.allowOcclusionWhenDynamic = false;

                wakeMesh.vertices = vertices;
                wakeMesh.uv = uvs;
                wakeMesh.colors32 = colors;
                wakeMesh.triangles = triangles;
                wakeMesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
            }
            else if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = runtimeMaterial;
            }

            if (wakeMesh != null)
            {
                ApplyStaticMeshData();
            }
        }

        private bool EnsureInitialized()
        {
            if (runtimeMaterial != null && wakeMesh != null && renderRoot != null)
            {
                return true;
            }

            Initialize();
            return runtimeMaterial != null && wakeMesh != null && renderRoot != null;
        }

        private void SetAsDefault()
        {
            if (defaultInstance == null || defaultInstance == this)
            {
                defaultInstance = this;
                warnedAboutMultipleSystems = false;
                return;
            }

            if (!warnedAboutMultipleSystems)
            {
                Debug.LogWarning("RenderWave WakeSystem supports one default scene wake system in V1. Additional systems will not be auto-resolved by WakeEmitter.", this);
                warnedAboutMultipleSystems = true;
            }
        }

        private void DiscoverEmitters()
        {
            var discoveredEmitters = FindObjectsByType<WakeEmitter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < discoveredEmitters.Length; i++)
            {
                RegisterEmitter(discoveredEmitters[i]);
            }
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
                var shader = wakeShader != null ? wakeShader : Shader.Find("RenderWave/URP/WakeSurface");
                if (shader == null)
                {
                    Debug.LogError("RenderWave WakeSystem could not find the wake shader.", this);
                    return null;
                }

                material = new Material(shader)
                {
                    name = "RenderWave Wake Runtime Material",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (!material.HasProperty(WakeColorId))
            {
                Debug.LogError("RenderWave WakeSystem requires a material that exposes the RenderWave wake shader property contract.", this);
                Destroy(material);
                return null;
            }

            return material;
        }

        private void UpdateWakeLifetimes(float deltaTime)
        {
            for (var i = wakeCount - 1; i >= 0; i--)
            {
                var wake = wakes[i];
                wake.Age += deltaTime;

                if (wake.Age >= wake.Lifetime)
                {
                    var lastIndex = wakeCount - 1;
                    wakes[i] = wakes[lastIndex];
                    wakes[lastIndex] = default;
                    wakeCount--;
                    continue;
                }

                wakes[i] = wake;
            }
        }

        private void RebuildMesh()
        {
            if (wakeMesh == null || meshRenderer == null)
            {
                return;
            }

            if (wakeCount <= 0)
            {
                SetRendererActive(false);
                wakeMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
                return;
            }

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var i = 0; i < wakes.Length; i++)
            {
                if (i < wakeCount)
                {
                    UpdateWakeSurface(ref wakes[i]);
                    WriteWakeQuad(i, wakes[i], ref min, ref max);
                }
                else
                {
                    ClearWakeQuad(i);
                }
            }

            wakeMesh.vertices = vertices;
            wakeMesh.colors32 = colors;
            wakeMesh.bounds = new Bounds((min + max) * 0.5f, Vector3.Max(max - min, Vector3.one * 0.5f));
            SetRendererActive(true);
        }

        private void UpdateWakeSurface(ref WakeInstance wake)
        {
            if (WaterLevelQueryService.TryGetWaterQuery(wake.Position, out var waterQuery))
            {
                wake.Position = new Vector3(wake.Position.x, waterQuery.SurfaceHeight + wake.SurfaceOffset, wake.Position.z);
                wake.SurfaceNormal = waterQuery.SurfaceNormal;
            }
        }

        private void WriteWakeQuad(int wakeIndex, WakeInstance wake, ref Vector3 min, ref Vector3 max)
        {
            var normalizedAge = wake.Age / Mathf.Max(wake.Lifetime, 0.0001f);
            var fadeFactor = 1f;
            if (normalizedAge > wake.FadeStart)
            {
                var fadeProgress = Mathf.InverseLerp(wake.FadeStart, 1f, normalizedAge);
                fadeFactor = 1f - fadeProgress;
            }

            var width = Mathf.Lerp(wake.Width, wake.Width * 1.12f, normalizedAge);
            var length = Mathf.Lerp(wake.Length, wake.Length * 0.92f, normalizedAge);
            var alpha = Mathf.Clamp01(wake.Intensity * fadeFactor);

            var forward = wake.Forward.sqrMagnitude > 0.0001f ? wake.Forward.normalized : Vector3.forward;
            var normal = wake.SurfaceNormal.sqrMagnitude > 0.0001f ? wake.SurfaceNormal.normalized : Vector3.up;
            var right = Vector3.Cross(normal, forward).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            var backOffset = forward * (length * 0.5f);
            var sideOffset = right * (width * 0.5f);

            var v0 = transform.InverseTransformPoint(wake.Position - sideOffset - backOffset);
            var v1 = transform.InverseTransformPoint(wake.Position + sideOffset - backOffset);
            var v2 = transform.InverseTransformPoint(wake.Position - sideOffset + backOffset);
            var v3 = transform.InverseTransformPoint(wake.Position + sideOffset + backOffset);

            var baseVertex = wakeIndex * 4;
            vertices[baseVertex] = v0;
            vertices[baseVertex + 1] = v1;
            vertices[baseVertex + 2] = v2;
            vertices[baseVertex + 3] = v3;

            var color = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            colors[baseVertex] = color;
            colors[baseVertex + 1] = color;
            colors[baseVertex + 2] = color;
            colors[baseVertex + 3] = color;

            min = Vector3.Min(min, v0);
            min = Vector3.Min(min, v1);
            min = Vector3.Min(min, v2);
            min = Vector3.Min(min, v3);
            max = Vector3.Max(max, v0);
            max = Vector3.Max(max, v1);
            max = Vector3.Max(max, v2);
            max = Vector3.Max(max, v3);
        }

        private void ClearWakeQuad(int wakeIndex)
        {
            var baseVertex = wakeIndex * 4;
            var clear = Vector3.zero;
            var transparent = new Color32(255, 255, 255, 0);

            vertices[baseVertex] = clear;
            vertices[baseVertex + 1] = clear;
            vertices[baseVertex + 2] = clear;
            vertices[baseVertex + 3] = clear;

            colors[baseVertex] = transparent;
            colors[baseVertex + 1] = transparent;
            colors[baseVertex + 2] = transparent;
            colors[baseVertex + 3] = transparent;
        }

        private void BuildStaticMeshData()
        {
            for (var i = 0; i < maxActiveWakes; i++)
            {
                var baseVertex = i * 4;
                var baseTriangle = i * 6;

                uvs[baseVertex] = new Vector2(0f, 0f);
                uvs[baseVertex + 1] = new Vector2(1f, 0f);
                uvs[baseVertex + 2] = new Vector2(0f, 1f);
                uvs[baseVertex + 3] = new Vector2(1f, 1f);

                triangles[baseTriangle] = baseVertex;
                triangles[baseTriangle + 1] = baseVertex + 2;
                triangles[baseTriangle + 2] = baseVertex + 1;
                triangles[baseTriangle + 3] = baseVertex + 2;
                triangles[baseTriangle + 4] = baseVertex + 3;
                triangles[baseTriangle + 5] = baseVertex + 1;
            }
        }

        private void ApplyStaticMeshData()
        {
            wakeMesh.Clear();
            wakeMesh.vertices = vertices;
            wakeMesh.uv = uvs;
            wakeMesh.colors32 = colors;
            wakeMesh.triangles = triangles;
            wakeMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        }

        private void ClearWakeState()
        {
            wakeCount = 0;
            if (wakes == null)
            {
                return;
            }

            for (var i = 0; i < wakes.Length; i++)
            {
                wakes[i] = default;
                ClearWakeQuad(i);
            }

            if (wakeMesh != null)
            {
                wakeMesh.vertices = vertices;
                wakeMesh.colors32 = colors;
                wakeMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            }
        }

        private void SetRendererActive(bool isActive)
        {
            if (renderRoot != null && renderRoot.gameObject.activeSelf != isActive)
            {
                renderRoot.gameObject.SetActive(isActive);
            }
        }

        private struct WakeInstance
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Vector3 SurfaceNormal;
            public float Width;
            public float Length;
            public float Lifetime;
            public float Age;
            public float FadeStart;
            public float Intensity;
            public float SurfaceOffset;
        }
    }
}
