using System.Collections.Generic;
using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Core
{
    /// <summary>
    /// Lightweight global registry used by all water queries.
    /// </summary>
    public static class WaterZoneRegistry
    {
        private static readonly List<IWaterZone> Zones = new List<IWaterZone>(8);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Zones.Clear();
        }

        public static void Register(IWaterZone zone)
        {
            if (zone == null || Zones.Contains(zone))
            {
                return;
            }

            Zones.Add(zone);
        }

        public static void Unregister(IWaterZone zone)
        {
            if (zone == null)
            {
                return;
            }

            Zones.Remove(zone);
        }

        public static bool TryQuery(Vector3 worldPosition, float time, out WaterQueryResult result)
        {
            var found = false;
            var bestPriority = int.MinValue;
            var bestSurfaceDistance = float.MaxValue;
            var bestResult = default(WaterQueryResult);

            for (var i = 0; i < Zones.Count; i++)
            {
                var zone = Zones[i];
                if (!zone.TryQuery(worldPosition, time, out var candidate))
                {
                    continue;
                }

                var surfaceDistance = Mathf.Abs(worldPosition.y - candidate.SurfaceHeight);
                if (!found || zone.Priority > bestPriority || (zone.Priority == bestPriority && surfaceDistance < bestSurfaceDistance))
                {
                    found = true;
                    bestPriority = zone.Priority;
                    bestSurfaceDistance = surfaceDistance;
                    bestResult = candidate;
                }
            }

            result = bestResult;
            return found;
        }
    }
}
