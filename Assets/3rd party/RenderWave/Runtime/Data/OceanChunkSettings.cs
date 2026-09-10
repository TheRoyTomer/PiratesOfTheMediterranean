using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderWave.Runtime.Data
{
    /// <summary>
    /// Controls chunk density and mesh density for the camera-following ocean surface.
    /// </summary>
    [Serializable]
    public sealed class OceanChunkSettings
    {
        [SerializeField, Min(0)] private int chunkRadius = 4;
        [SerializeField, Min(16f)] private float chunkSize = 128f;
        [SerializeField, Range(2, 128)] private int meshResolution = 16;
        [SerializeField] private bool enableDistanceCulling;
        [SerializeField, Min(0f)] private float maxRenderDistance = 120f;
        [SerializeField] private ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;
        [SerializeField] private bool receiveShadows;

        public int ChunkRadius => chunkRadius;
        public float ChunkSize => chunkSize;
        public int MeshResolution => meshResolution;
        public bool EnableDistanceCulling => enableDistanceCulling;
        public float MaxRenderDistance => maxRenderDistance;
        public ShadowCastingMode ShadowCastingMode => shadowCastingMode;
        public bool ReceiveShadows => receiveShadows;
        public int ChunkCountPerAxis => (chunkRadius * 2) + 1;
        public int TotalChunkCount => ChunkCountPerAxis * ChunkCountPerAxis;
        public int VerticesPerChunk => (meshResolution + 1) * (meshResolution + 1);
        public int TriangleCountPerChunk => meshResolution * meshResolution * 2;
        public int EstimatedTotalVertexCount => VerticesPerChunk * TotalChunkCount;

        public void Validate()
        {
            chunkRadius = Mathf.Max(0, chunkRadius);
            chunkSize = Mathf.Max(16f, chunkSize);
            meshResolution = Mathf.Clamp(meshResolution, 2, 128);
            maxRenderDistance = Mathf.Max(0f, maxRenderDistance);
        }
    }
}
