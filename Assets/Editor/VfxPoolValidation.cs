using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Run in Play Mode via Tools/Validation/VFX Pool. Results are also kept in SessionState.</summary>
public static class VfxPoolValidation
{
    public const string ResultKey = "Pirates.VfxPoolValidation";
    private static bool running;

    [MenuItem("Tools/Validation/VFX Pool")]
    public static async void Run()
    {
        if (running) return;
        if (!EditorApplication.isPlaying)
        {
            SessionState.SetString(ResultKey, "Enter Play Mode before running VFX validation.");
            return;
        }
        running = true;
        SessionState.SetString(ResultKey, "RUNNING");
        var root = new GameObject("VFX Pool Validation");
        var pool = root.AddComponent<VfxPool>();
        GameObject ship = null;
        float originalTimeScale = Time.timeScale;
        var report = new List<string>();
        try
        {
            Time.timeScale = 1f;
            string[] names = { "CannonMuzzleFlash", "CannonballImpactExplosion", "CannonballImpactSmoke", "WaterBallSplash" };
            var prefabs = new GameObject[names.Length];
            Vector3 position = new Vector3(5000f, 50f, 5000f);
            for (int i = 0; i < names.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Used/" + names[i] + ".prefab");
                Check(prefabs[i] != null, "Missing prefab " + names[i]);
                // A deliberately small isolated pool exercises exhaustion even with larger gameplay defaults.
                pool.Prewarm(prefabs[i], 12);
            }
            Check(pool.CreatedCount == 48 && pool.ActiveCount == 0, "Prewarm counts");

            for (int cycle = 0; cycle < 2; cycle++)
            {
                foreach (var prefab in prefabs) pool.Play(prefab, position, Quaternion.identity, 0.5f);
                await Wait(0.15f);
                var roots = ActiveRoots(root);
                Check(roots.Count == 4, "All effect types active");
                foreach (var effect in roots)
                {
                    int count = 0;
                    foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        Check(ps.main.stopAction == ParticleSystemStopAction.None, "Self-destruction disabled");
                        count += ps.particleCount;
                    }
                    Check(count > 0, "Particles restart: " + effect.name + ", cycle " + cycle);
                }
                await Until(() => pool.ActiveCount == 0);
            }
            Check(pool.CreatedCount == 48, "Reuse did not instantiate");
            report.Add("PASS: all four real prefabs emit on first and second use; timed return and stop-action override.");

            ship = new GameObject("VFX test target");
            ship.transform.position = position;
            pool.Play(prefabs[2], position + Vector3.right * 3f, Quaternion.identity,
                follow: ship.transform, delay: 0.2f);
            ship.transform.position += Vector3.forward * 10f;
            ship.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            await Wait(0.3f);
            var smoke = ActiveRoots(root)[0];
            Check(Vector3.Distance(smoke.transform.position, ship.transform.TransformPoint(Vector3.right * 3f)) < 0.01f,
                "Delayed smoke follows translated and rotated target");
            Check(Quaternion.Angle(smoke.transform.rotation, ship.transform.rotation) < 0.01f, "Smoke rotation follows target");
            ship.SetActive(false);
            await Until(() => pool.ActiveCount == 0);
            ship.SetActive(true);
            pool.Play(prefabs[2], position, Quaternion.identity, follow: ship.transform, delay: 0.5f);
            Object.Destroy(ship);
            await Until(() => pool.ActiveCount == 0);
            Check(ActiveRoots(root).Count == 0, "Destroyed delayed target cancels smoke");
            report.Add("PASS: delayed following smoke; target deactivation and destruction return/cancel safely.");

            pool.Play(prefabs[0], position, Quaternion.identity);
            await Until(() => pool.ActiveCount == 0, 20f);
            pool.Play(prefabs[2], position, Quaternion.identity);
            await Until(() => pool.ActiveCount == 0, 25f);
            report.Add("PASS: muzzle and smoke return naturally after playback without Destroy.");

            pool.Play(prefabs[3], position, Quaternion.identity, 0.2f);
            Time.timeScale = 0f;
            await Wait(0.35f);
            Check(pool.ActiveCount == 1, "Pause preserves timed effect");
            Time.timeScale = 1f;
            await Until(() => pool.ActiveCount == 0);
            report.Add("PASS: lifetime respects game pause.");

            int beforeGrowth = pool.CreatedCount;
            for (int i = 0; i < 18; i++) pool.Play(prefabs[0], position, Quaternion.identity, 0.1f);
            Check(pool.CreatedCount == beforeGrowth + 6 && pool.ActiveCount == 18, "Exhaustion grows only missing instances");
            await Until(() => pool.ActiveCount == 0);
            int afterGrowth = pool.CreatedCount;
            for (int i = 0; i < 18; i++) pool.Play(prefabs[0], position, Quaternion.identity, 0.1f);
            Check(pool.CreatedCount == afterGrowth, "Expanded pool is reused");
            await Until(() => pool.ActiveCount == 0);
            pool.Play(prefabs[2], position, Quaternion.identity, delay: 2f);
            pool.Play(prefabs[0], position, Quaternion.identity);
            pool.enabled = false;
            Check(pool.ActiveCount == 0 && ActiveRoots(root).Count == 0, "Disable clears active and pending effects");
            pool.enabled = true;
            report.Add("PASS: pool exhaustion/reuse and owner-disable cleanup.");

            // Compare only synchronous spawn work; this is not a gameplay FPS benchmark.
            long calibrationBefore = GC.GetAllocatedBytesForCurrentThread();
            var calibration = new byte[4096];
            bool allocationCounterWorks = GC.GetAllocatedBytesForCurrentThread() > calibrationBefore;
            GC.KeepAlive(calibration);
            long pooledBytes = 0;
            double pooledMs = 0;
            int createdBeforeBatches = pool.CreatedCount;
            for (int batch = 0; batch < 3; batch++)
            {
                var clock = Stopwatch.StartNew();
                long bytes = GC.GetAllocatedBytesForCurrentThread();
                foreach (var prefab in prefabs)
                    for (int i = 0; i < 12; i++) pool.Play(prefab, position, Quaternion.identity, 0.05f);
                pooledBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
                clock.Stop();
                pooledMs += clock.Elapsed.TotalMilliseconds;
                await Until(() => pool.ActiveCount == 0);
            }
            Check(pool.CreatedCount == createdBeforeBatches, "144 warmed plays create no instances");
            long baselineBytes = 0;
            double baselineMs = 0;
            for (int batch = 0; batch < 3; batch++)
            {
                var clock = Stopwatch.StartNew();
                long bytes = GC.GetAllocatedBytesForCurrentThread();
                foreach (var prefab in prefabs)
                    for (int i = 0; i < 12; i++)
                        Object.Destroy(Object.Instantiate(prefab, position, Quaternion.identity), 0.05f);
                baselineBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
                clock.Stop();
                baselineMs += clock.Elapsed.TotalMilliseconds;
                await Wait(0.15f);
            }
            string allocations = allocationCounterWorks
                ? $"Managed bytes: baseline {baselineBytes}, pool {pooledBytes}."
                : "Managed allocation counter unavailable in this runtime.";
            report.Add($"SPAWN COMPARISON (144 effects, Editor): baseline 144 instances, {baselineMs:F2} ms; warmed pool 0 new instances, {pooledMs:F2} ms. {allocations} Excludes rendering and deferred destruction; not an FPS claim.");
            SessionState.SetString(ResultKey, string.Join("\n", report));
            UnityEngine.Debug.Log("VFX pool validation passed.\n" + string.Join("\n", report));
        }
        catch (Exception ex)
        {
            SessionState.SetString(ResultKey, string.Join("\n", report) + "\nFAIL: " + ex);
            UnityEngine.Debug.LogException(ex);
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            if (ship != null) Object.Destroy(ship);
            if (root != null) Object.Destroy(root);
            running = false;
        }
    }

    private static List<GameObject> ActiveRoots(GameObject root)
    {
        var result = new List<GameObject>();
        foreach (Transform child in root.transform)
            if (child.gameObject.activeInHierarchy) result.Add(child.gameObject);
        return result;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Wait(float seconds)
    {
        double until = EditorApplication.timeSinceStartup + seconds;
        do { await Task.Yield(); Check(EditorApplication.isPlaying, "Play Mode stopped during validation"); }
        while (EditorApplication.timeSinceStartup < until);
    }

    private static async Task Until(Func<bool> condition, float timeout = 5f)
    {
        double until = EditorApplication.timeSinceStartup + timeout;
        while (!condition())
        {
            await Task.Yield();
            Check(EditorApplication.isPlaying, "Play Mode stopped during validation");
            Check(EditorApplication.timeSinceStartup < until, "Timed out waiting for pool return");
        }
    }
}
