using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Core
{
    /// <summary>
    /// Contract used by the central registry and gameplay query service.
    /// </summary>
    public interface IWaterZone
    {
        int Priority { get; }
        bool TryQuery(Vector3 worldPosition, float time, out WaterQueryResult result);
    }
}
