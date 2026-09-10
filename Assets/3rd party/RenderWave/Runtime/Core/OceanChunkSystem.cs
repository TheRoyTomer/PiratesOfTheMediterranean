using RenderWave.Runtime.Data;
using RenderWave.Runtime.Zones;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Runtime.Core
{
    internal sealed class OceanChunkSystem
    {
        private const float ContainedZoneAnchorEpsilon = 0.001f;

        private readonly Transform root;
        private readonly OceanChunkSettings settings;
        private readonly OceanWaterZone zone;
        private readonly ChunkInstance[] chunks;

        private Mesh sharedMesh;
        private int lastAnchorX = int.MinValue;
        private int lastAnchorZ = int.MinValue;
        private float lastBaseHeight = float.MinValue;
        private float lastMeshBoundsHeight = float.MinValue;
        private Vector3 lastRootPosition = new Vector3(float.NaN, float.NaN, float.NaN);
        private Vector3 lastZoneCenter = new Vector3(float.NaN, float.NaN, float.NaN);
        private Vector2 lastZoneSize = new Vector2(float.NaN, float.NaN);
        private OceanZoneCoverageMode lastCoverageMode = (OceanZoneCoverageMode)(-1);

        public OceanChunkSystem(Transform managerTransform, OceanChunkSettings settings, OceanWaterZone zone, Material material, float meshBoundsHeight)
        {
            this.settings = settings;
            this.zone = zone;

            var rootObject = new GameObject("Ocean Chunks");
            rootObject.transform.SetParent(managerTransform, false);
            root = rootObject.transform;

            chunks = new ChunkInstance[settings.TotalChunkCount];
            sharedMesh = BuildChunkMesh(settings.ChunkSize, settings.MeshResolution, meshBoundsHeight);
            lastMeshBoundsHeight = meshBoundsHeight;
            CreateChunks(material);
        }

        public void SetActive(bool isActive)
        {
            if (root != null && root.gameObject.activeSelf != isActive)
            {
                root.gameObject.SetActive(isActive);
            }
        }

        public void InvalidateLayout()
        {
            lastAnchorX = int.MinValue;
            lastAnchorZ = int.MinValue;
            lastBaseHeight = float.MinValue;
            lastMeshBoundsHeight = float.MinValue;
            lastRootPosition = new Vector3(float.NaN, float.NaN, float.NaN);
            lastZoneCenter = new Vector3(float.NaN, float.NaN, float.NaN);
            lastZoneSize = new Vector2(float.NaN, float.NaN);
            lastCoverageMode = (OceanZoneCoverageMode)(-1);
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
        }

        public void Update(Vector3 cameraPosition, float baseHeight, float meshBoundsHeight)
        {
            var anchorPosition = ResolveAnchorPosition(cameraPosition);
            var anchorX = Mathf.RoundToInt(anchorPosition.x / settings.ChunkSize);
            var anchorZ = Mathf.RoundToInt(anchorPosition.z / settings.ChunkSize);
            var rootPosition = root.parent != null ? root.parent.position : Vector3.zero;
            var zoneCenter = zone != null ? zone.Center : Vector3.zero;
            var zoneSize = zone != null ? zone.RectangleSize : Vector2.zero;
            var coverageMode = zone != null ? zone.CoverageMode : OceanZoneCoverageMode.Infinite;

            if (anchorX == lastAnchorX &&
                anchorZ == lastAnchorZ &&
                Mathf.Approximately(baseHeight, lastBaseHeight) &&
                Mathf.Approximately(meshBoundsHeight, lastMeshBoundsHeight) &&
                rootPosition == lastRootPosition &&
                zoneCenter == lastZoneCenter &&
                zoneSize == lastZoneSize &&
                coverageMode == lastCoverageMode)
            {
                return;
            }

            lastAnchorX = anchorX;
            lastAnchorZ = anchorZ;
            lastBaseHeight = baseHeight;
            lastMeshBoundsHeight = meshBoundsHeight;
            lastRootPosition = rootPosition;
            lastZoneCenter = zoneCenter;
            lastZoneSize = zoneSize;
            lastCoverageMode = coverageMode;

            UpdateMeshBounds(meshBoundsHeight);
            var index = 0;

            for (var z = -settings.ChunkRadius; z <= settings.ChunkRadius; z++)
            {
                for (var x = -settings.ChunkRadius; x <= settings.ChunkRadius; x++)
                {
                    var worldX = (anchorX + x) * settings.ChunkSize;
                    var worldZ = (anchorZ + z) * settings.ChunkSize;

                    ref var chunk = ref chunks[index];
                    chunk.Transform.localPosition = new Vector3(worldX - rootPosition.x, baseHeight - rootPosition.y, worldZ - rootPosition.z);

                    var isVisible = zone == null || zone.IntersectsChunk(worldX, worldZ, settings.ChunkSize);
                    if (chunk.Renderer.enabled != isVisible)
                    {
                        chunk.Renderer.enabled = isVisible;
                    }

                    index++;
                }
            }
        }

        private Vector3 ResolveAnchorPosition(Vector3 cameraPosition)
        {
            if (zone == null || zone.CoverageMode != OceanZoneCoverageMode.Rectangle)
            {
                return cameraPosition;
            }

            var totalFootprintSize = settings.ChunkCountPerAxis * settings.ChunkSize;
            var zoneSize = zone.RectangleSize;

            if (zoneSize.x <= totalFootprintSize + ContainedZoneAnchorEpsilon &&
                zoneSize.y <= totalFootprintSize + ContainedZoneAnchorEpsilon)
            {
                return zone.Center;
            }

            return cameraPosition;
        }

        private void CreateChunks(Material material)
        {
            var index = 0;
            var allowDynamicOcclusion = zone != null && zone.CoverageMode == OceanZoneCoverageMode.Rectangle;

            for (var z = -settings.ChunkRadius; z <= settings.ChunkRadius; z++)
            {
                for (var x = -settings.ChunkRadius; x <= settings.ChunkRadius; x++)
                {
                    var chunkObject = new GameObject($"Chunk_{x}_{z}");
                    chunkObject.transform.SetParent(root, false);

                    var meshFilter = chunkObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = sharedMesh;

                    var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = material;
                    meshRenderer.shadowCastingMode = settings.ShadowCastingMode;
                    meshRenderer.receiveShadows = settings.ReceiveShadows;
                    meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    meshRenderer.allowOcclusionWhenDynamic = allowDynamicOcclusion;

                    chunks[index] = new ChunkInstance(chunkObject.transform, meshRenderer);
                    index++;
                }
            }
        }

        private void UpdateMeshBounds(float meshBoundsHeight)
        {
            if (sharedMesh == null)
            {
                return;
            }

            sharedMesh.bounds = new Bounds(Vector3.zero, new Vector3(settings.ChunkSize, meshBoundsHeight, settings.ChunkSize));
        }

        private static Mesh BuildChunkMesh(float chunkSize, int resolution, float meshBoundsHeight)
        {
            var vertexCountPerAxis = resolution + 1;
            var vertexCount = vertexCountPerAxis * vertexCountPerAxis;
            var quadCount = resolution * resolution;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[quadCount * 6];
            var normals = new Vector3[vertexCount];

            var halfSize = chunkSize * 0.5f;
            var step = chunkSize / resolution;
            var vertexIndex = 0;

            for (var z = 0; z < vertexCountPerAxis; z++)
            {
                for (var x = 0; x < vertexCountPerAxis; x++)
                {
                    vertices[vertexIndex] = new Vector3((x * step) - halfSize, 0f, (z * step) - halfSize);
                    uvs[vertexIndex] = new Vector2((float)x / resolution, (float)z / resolution);
                    normals[vertexIndex] = Vector3.up;
                    vertexIndex++;
                }
            }

            var triangleIndex = 0;
            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var rowStart = z * vertexCountPerAxis;
                    var nextRowStart = (z + 1) * vertexCountPerAxis;
                    var bottomLeft = rowStart + x;
                    var bottomRight = bottomLeft + 1;
                    var topLeft = nextRowStart + x;
                    var topRight = topLeft + 1;

                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            var mesh = new Mesh
            {
                name = "RenderWave Ocean Chunk"
            };

            if (vertexCount > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(chunkSize, meshBoundsHeight, chunkSize));

            return mesh;
        }

        private struct ChunkInstance
        {
            public ChunkInstance(Transform transform, MeshRenderer renderer)
            {
                Transform = transform;
                Renderer = renderer;
            }

            public Transform Transform;
            public MeshRenderer Renderer;
        }
    }
}
