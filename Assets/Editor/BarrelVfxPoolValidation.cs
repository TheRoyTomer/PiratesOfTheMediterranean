using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BarrelVfxPoolValidation
{
    public const string ResultKey = "Pirates.BarrelVfxPoolValidation";
    private static bool running;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Validation/Barrel VFX Pool")]
    public static async void Run()
    {
        if (running || !EditorApplication.isPlaying) return;
        running = true;
        SessionState.SetString(ResultKey, "RUNNING");
        var root = new GameObject("Barrel VFX Validation");
        var pool = root.AddComponent<VfxPool>();
        BarrelStrikeController strike = null;
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BarrelsGroup.prefab");
            var template = prefab.GetComponent<BarrelStrikeController>();
            template.PrewarmEffects(pool);
            template.PrewarmEffects(pool);
            Check(pool.CreatedCount == 164, "Shared budgets: 100 entry splashes + 32 explosions + 32 explosion splashes");

            for (int cycle = 0; cycle < 2; cycle++)
            {
                strike = Object.Instantiate(template, new Vector3(5000, -100, 5000), Quaternion.identity);
                strike.enabled = false;
                strike.BeginRelease(Vector3.zero, null, pool);
                foreach (var barrel in strike.GetComponentsInChildren<BarrelController>())
                {
                    barrel.enabled = false;
                    // Supply a deterministic water sample; still exercise the real water-entry method.
                    typeof(BarrelController).GetField("hasSurfaceSample", Private).SetValue(barrel, true);
                    typeof(BarrelController).GetField("lastSurfaceHeight", Private).SetValue(barrel, 0f);
                    typeof(BarrelController).GetMethod("TryEnterWater", Private).Invoke(barrel, null);
                    Check(barrel.IsFloating, "Barrel enters water");
                }
                Check(pool.ActiveCount == 4, "Four entry splashes");
                strike.Detonate();
                strike.Detonate();
                Check(pool.ActiveCount == 12, "Four explosions and four explosion splashes; duplicate detonation ignored");
                await Wait(0.2);
                Check(strike == null && pool.ActiveCount == 12, "VFX survive destruction of barrel group");
                foreach (Transform effect in root.transform)
                {
                    if (!effect.gameObject.activeInHierarchy) continue;
                    int particles = 0;
                    foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>()) particles += ps.particleCount;
                    Check(particles > 0, "Playback on cycle " + cycle + ": " + effect.name);
                    foreach (var curve in effect.GetComponentsInChildren<KWS.KWS_EffectorAnimationCurve>())
                    {
                        float elapsed = (float)typeof(KWS.KWS_EffectorAnimationCurve).GetField("_leftTime", Private).GetValue(curve);
                        Check(elapsed > 0f && elapsed < 1f, "KWS animation resets on reuse");
                    }
                }
                double deadline = EditorApplication.timeSinceStartup + 12;
                while (pool.ActiveCount > 0)
                {
                    await Task.Yield();
                    Check(EditorApplication.isPlaying && EditorApplication.timeSinceStartup < deadline, "Timed return within lifetime");
                }
                Check(pool.CreatedCount == 164, "Second use creates no additional VFX");
            }
            SessionState.SetString(ResultKey, "PASS: real barrel water entry and detonation twice; 4 entry splashes + 4 explosions + 4 explosion splashes per cycle; duplicate detonation ignored; effects survive barrel destruction; particles and KWS curves restart; all effects return after the configured 8 seconds; 164 prewarmed instances, no growth.");
        }
        catch (Exception ex)
        {
            SessionState.SetString(ResultKey, "FAIL: " + ex);
            Debug.LogException(ex);
        }
        finally
        {
            Time.timeScale = previousScale;
            if (strike != null) Object.Destroy(strike.gameObject);
            if (root != null) Object.Destroy(root);
            running = false;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Wait(double seconds)
    {
        double until = EditorApplication.timeSinceStartup + seconds;
        do { await Task.Yield(); Check(EditorApplication.isPlaying, "Play Mode stopped"); }
        while (EditorApplication.timeSinceStartup < until);
    }
}
