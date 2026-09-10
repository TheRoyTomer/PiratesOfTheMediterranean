using UnityEditor;

namespace RenderWave.Editor
{
    internal static class RenderWaveMenuItems
    {
        [MenuItem("GameObject/RenderWave/Classic Ocean Rig", false, 10)]
        private static void CreateClassicOceanRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.ClassicOcean);
        }

        [MenuItem("GameObject/RenderWave/Enhanced Ocean Rig", false, 11)]
        private static void CreateEnhancedOceanRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.EnhancedOcean);
        }

        [MenuItem("GameObject/RenderWave/Enhanced Ocean + Underwater", false, 12)]
        private static void CreateEnhancedUnderwaterRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.EnhancedUnderwater);
        }

        [MenuItem("GameObject/RenderWave/Enhanced Ocean + Wakes", false, 13)]
        private static void CreateEnhancedWakeReadyRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.EnhancedWakeReady);
        }

        [MenuItem("GameObject/RenderWave/Commercial Presets/Calm Ocean Rig", false, 30)]
        private static void CreateCalmOceanRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.CalmOcean);
        }

        [MenuItem("GameObject/RenderWave/Commercial Presets/Storm Ocean Rig", false, 31)]
        private static void CreateStormOceanRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.StormOcean);
        }

        [MenuItem("GameObject/RenderWave/Commercial Presets/Tropical Ocean Rig", false, 32)]
        private static void CreateTropicalOceanRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.TropicalOcean);
        }

        [MenuItem("GameObject/RenderWave/Commercial Presets/Deep Ocean Rig", false, 33)]
        private static void CreateDeepOceanRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.DeepOcean);
        }

        [MenuItem("GameObject/RenderWave/Contained Water/Lake Rig", false, 40)]
        private static void CreateLakeRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.LakeWater);
        }

        [MenuItem("GameObject/RenderWave/Contained Water/Pool Rig", false, 41)]
        private static void CreatePoolRig()
        {
            RenderWaveEditorUtility.CreateOceanRig(RenderWaveRigTemplate.PoolWater);
        }
    }
}
