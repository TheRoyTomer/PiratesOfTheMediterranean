using RenderWave.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Runtime.Core
{
    internal sealed class OceanFarFieldSystem
    {
        private const string DefaultShaderName = "RenderWave/URP/OceanFarField";
        private const int RadialBandCount = 6;
        private const int SegmentsPerSide = 16;

        private static readonly int SurfaceColorId = Shader.PropertyToID("_SurfaceColor");
        private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
        private static readonly int LargeWaveDirectionId = Shader.PropertyToID("_LargeWaveDirection");
        private static readonly int SmallWaveDirectionId = Shader.PropertyToID("_SmallWaveDirection");
        private static readonly int LargeWaveDataId = Shader.PropertyToID("_LargeWaveData");
        private static readonly int SmallWaveDataId = Shader.PropertyToID("_SmallWaveData");
        private static readonly int RenderWaveTimeId = Shader.PropertyToID("_RenderWaveTime");

        private readonly Transform root;
        private readonly OceanChunkSettings chunkSettings;
        private readonly OceanFarFieldSettings settings;
        private readonly MeshFilter meshFilter;
        private readonly MeshRenderer meshRenderer;
        private readonly Material runtimeMaterial;

        private Mesh sharedMesh;
        private float lastInnerHalfExtent = float.MinValue;
        private float lastOuterHalfExtent = float.MinValue;
        private float lastMeshBoundsHeight = float.MinValue;
        public OceanFarFieldSystem(
            Transform managerTransform,
            OceanChunkSettings chunkSettings,
            OceanFarFieldSettings settings,
            Material sourceMaterial,
            Shader shader)
        {
            this.chunkSettings = chunkSettings;
            this.settings = settings;

            var rootObject = new GameObject("Ocean Far Field");
            rootObject.transform.SetParent(managerTransform, false);
            rootObject.layer = managerTransform.gameObject.layer;
            root = rootObject.transform;

            meshFilter = rootObject.AddComponent<MeshFilter>();
            meshRenderer = rootObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            runtimeMaterial = CreateRuntimeMaterial(sourceMaterial, shader, managerTransform);
            if (runtimeMaterial == null)
            {
                meshRenderer.enabled = false;
                return;
            }

            meshRenderer.sharedMaterial = runtimeMaterial;
        }

        public bool IsValid => runtimeMaterial != null && meshRenderer != null && meshFilter != null;

        public void SetActive(bool isActive)
        {
            if (root != null && root.gameObject.activeSelf != isActive)
            {
                root.gameObject.SetActive(isActive);
            }
        }

        public void Dispose()
        {
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }

            if (sharedMesh != null)
            {
                Object.Destroy(sharedMesh);
                sharedMesh = null;
            }

            if (runtimeMaterial != null)
            {
                Object.Destroy(runtimeMaterial);
            }
        }

        public void Update(Vector3 cameraPosition, float baseHeight, WaterZoneSettings zoneSettings, WaterWaveParameters waveParameters, float time)
        {
            if (!IsValid)
            {
                return;
            }

            var anchorX = Mathf.RoundToInt(cameraPosition.x / chunkSettings.ChunkSize);
            var anchorZ = Mathf.RoundToInt(cameraPosition.z / chunkSettings.ChunkSize);
            var innerHalfExtent = Mathf.Max(chunkSettings.ChunkSize, ResolveNearFieldHalfExtent() - ResolveInnerUnderlap());
            var outerHalfExtent = Mathf.Max(settings.FarDistance, innerHalfExtent + chunkSettings.ChunkSize);

            if (sharedMesh == null ||
                !Mathf.Approximately(innerHalfExtent, lastInnerHalfExtent) ||
                !Mathf.Approximately(outerHalfExtent, lastOuterHalfExtent) ||
                !Mathf.Approximately(lastMeshBoundsHeight, waveParameters.RecommendedMeshBoundsHeight))
            {
                RebuildMesh(innerHalfExtent, outerHalfExtent, waveParameters.RecommendedMeshBoundsHeight);
                lastInnerHalfExtent = innerHalfExtent;
                lastOuterHalfExtent = outerHalfExtent;
                lastMeshBoundsHeight = waveParameters.RecommendedMeshBoundsHeight;
            }

            var rootPosition = root.parent != null ? root.parent.position : Vector3.zero;
            root.localPosition = new Vector3(
                (anchorX * chunkSettings.ChunkSize) - rootPosition.x,
                (baseHeight + settings.VerticalOffset) - rootPosition.y,
                (anchorZ * chunkSettings.ChunkSize) - rootPosition.z);

            runtimeMaterial.SetColor(SurfaceColorId, zoneSettings.SurfaceColor);
            runtimeMaterial.SetColor(DeepColorId, zoneSettings.UnderwaterColor);
            runtimeMaterial.SetVector(LargeWaveDirectionId, new Vector4(waveParameters.LargeDirection.x, waveParameters.LargeDirection.y, 0f, 0f));
            runtimeMaterial.SetVector(SmallWaveDirectionId, new Vector4(waveParameters.SmallDirection.x, waveParameters.SmallDirection.y, 0f, 0f));
            runtimeMaterial.SetVector(LargeWaveDataId, waveParameters.LargeWaveData);
            runtimeMaterial.SetVector(SmallWaveDataId, waveParameters.SmallWaveData);
            runtimeMaterial.SetFloat(RenderWaveTimeId, time);
        }

        private float ResolveNearFieldHalfExtent()
        {
            return (chunkSettings.ChunkRadius + 0.5f) * chunkSettings.ChunkSize;
        }

        private float ResolveInnerUnderlap()
        {
            return Mathf.Clamp(chunkSettings.ChunkSize * 0.25f, 0.5f, chunkSettings.ChunkSize * 0.5f);
        }

        private void RebuildMesh(float innerHalfExtent, float outerHalfExtent, float meshBoundsHeight)
        {
            if (sharedMesh != null)
            {
                Object.Destroy(sharedMesh);
            }

            sharedMesh = BuildRingMesh(innerHalfExtent, outerHalfExtent, meshBoundsHeight);
            meshFilter.sharedMesh = sharedMesh;
        }

        private Material CreateRuntimeMaterial(Material sourceMaterial, Shader explicitShader, Object context)
        {
            var resolvedShader = explicitShader != null ? explicitShader : Shader.Find(DefaultShaderName);
            if (resolvedShader == null)
            {
                Debug.LogError("RenderWave could not find the required far-field shader.", context);
                return null;
            }

            var material = new Material(resolvedShader)
            {
                name = "RenderWave Ocean Far Field Runtime Material",
                hideFlags = HideFlags.DontSave
            };

            if (sourceMaterial != null)
            {
                material.CopyPropertiesFromMaterial(sourceMaterial);
            }

            if (!HasRequiredMaterialProperties(material))
            {
                Debug.LogError("RenderWave far-field shader does not match the expected property contract.", context);
                Object.Destroy(material);
                return null;
            }

            return material;
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
                   material.HasProperty(RenderWaveTimeId);
        }

        private static Mesh BuildRingMesh(float innerHalfExtent, float outerHalfExtent, float meshBoundsHeight)
        {
            var verticesPerRing = SegmentsPerSide * 4;
            var ringCount = RadialBandCount + 1;
            var vertexCount = verticesPerRing * ringCount;
            var quadCount = verticesPerRing * RadialBandCount;

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[quadCount * 6];

            var vertexIndex = 0;
            for (var ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                var radialT = ringIndex / (float)RadialBandCount;
                var halfExtent = Mathf.Lerp(innerHalfExtent, outerHalfExtent, radialT);

                for (var perimeterIndex = 0; perimeterIndex < verticesPerRing; perimeterIndex++)
                {
                    var perimeterT = perimeterIndex / (float)verticesPerRing;
                    var perimeterPoint = EvaluateSquarePerimeter(halfExtent, perimeterT);

                    vertices[vertexIndex] = new Vector3(perimeterPoint.x, 0f, perimeterPoint.y);
                    normals[vertexIndex] = Vector3.up;
                    uvs[vertexIndex] = new Vector2(perimeterT, radialT);
                    vertexIndex++;
                }
            }

            var triangleIndex = 0;
            for (var ringIndex = 0; ringIndex < RadialBandCount; ringIndex++)
            {
                var currentRingStart = ringIndex * verticesPerRing;
                var nextRingStart = (ringIndex + 1) * verticesPerRing;

                for (var perimeterIndex = 0; perimeterIndex < verticesPerRing; perimeterIndex++)
                {
                    var current = currentRingStart + perimeterIndex;
                    var currentNext = currentRingStart + ((perimeterIndex + 1) % verticesPerRing);
                    var next = nextRingStart + perimeterIndex;
                    var nextNext = nextRingStart + ((perimeterIndex + 1) % verticesPerRing);

                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = nextNext;
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = nextNext;
                    triangles[triangleIndex++] = currentNext;
                }
            }

            var mesh = new Mesh
            {
                name = "RenderWave Ocean Far Field"
            };

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(outerHalfExtent * 2f, meshBoundsHeight, outerHalfExtent * 2f));

            return mesh;
        }

        private static Vector2 EvaluateSquarePerimeter(float halfExtent, float perimeterT)
        {
            var sideT = Mathf.Repeat(perimeterT, 1f) * 4f;
            if (sideT < 1f)
            {
                return new Vector2(Mathf.Lerp(-halfExtent, halfExtent, sideT), halfExtent);
            }

            if (sideT < 2f)
            {
                return new Vector2(halfExtent, Mathf.Lerp(halfExtent, -halfExtent, sideT - 1f));
            }

            if (sideT < 3f)
            {
                return new Vector2(Mathf.Lerp(halfExtent, -halfExtent, sideT - 2f), -halfExtent);
            }

            return new Vector2(-halfExtent, Mathf.Lerp(-halfExtent, halfExtent, sideT - 3f));
        }
    }
}
