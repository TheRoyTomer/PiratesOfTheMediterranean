using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Measures the real menu Play path in the Editor, not a cold standalone build.</summary>
public static class LoadingTimeValidation
{
    public const string ResultKey = "Pirates.LoadingTimeValidation";
    private static bool running;

    [MenuItem("Tools/Validation/Measure Menu Loading")]
    public static async void Run()
    {
        if (running || !EditorApplication.isPlaying) return;
        running = true;
        var results = new List<string>();
        SessionState.SetString(ResultKey, "RUNNING");
        try
        {
            for (int sample = 1; sample <= 3; sample++)
            {
                if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    var menuLoad = SceneManager.LoadSceneAsync("MainMenu");
                    while (!menuLoad.isDone) await NextFrame();
                }
                // Give menu Start and its first frames time to settle before simulating Play.
                await NextFrame();
                await NextFrame();
                var menu = Object.FindFirstObjectByType<MainMenuController>();
                if (menu == null) throw new InvalidOperationException("Main menu not found.");
                var clock = Stopwatch.StartNew();
                double loadedMs = -1;
                UnityEngine.Events.UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) =>
                {
                    if (scene.name == "GameScene") loadedMs = clock.Elapsed.TotalMilliseconds;
                };
                SceneManager.sceneLoaded += loaded;
                double maxGapMs = 0;
                double previousMs = 0;
                try
                {
                    menu.Play();
                    menu.Play(); // Repeated Play must not start another transition.
                    if (!GameplayLoadingScreen.IsLoading) throw new InvalidOperationException("Loading screen did not start.");
                    float lastProgress = 0;
                    int preparationFrames = 0;
                    while (loadedMs < 0)
                    {
                        await NextFrame();
                        double nowMs = clock.Elapsed.TotalMilliseconds;
                        maxGapMs = Math.Max(maxGapMs, nowMs - previousMs);
                        previousMs = nowMs;
                        if (nowMs > 120000) throw new TimeoutException("Game scene load exceeded two minutes.");
                    }
                    while (GameplayLoadingScreen.IsLoading)
                    {
                        var screen = Object.FindFirstObjectByType<GameplayLoadingScreen>();
                        var manager = GameManager.Instance;
                        if (screen.Progress < lastProgress) throw new InvalidOperationException("Progress moved backwards.");
                        lastProgress = screen.Progress;
                        if (Time.timeScale != 0f || !PauseMenuController.BlocksGameplayInput)
                            throw new InvalidOperationException("Gameplay was not gated during preparation.");
                        if (manager != null && (manager.SurvivalTime != 0f || manager.CurrentWave != 0))
                            throw new InvalidOperationException("Match began behind loading screen.");
                        preparationFrames++;
                        await NextFrame();
                        double nowMs = clock.Elapsed.TotalMilliseconds;
                        maxGapMs = Math.Max(maxGapMs, nowMs - previousMs);
                        previousMs = nowMs;
                        if (nowMs > 120000) throw new TimeoutException("Preparation exceeded two minutes.");
                    }
                    // Include two subsequent player-loop frames, beyond sceneLoaded/Awake.
                    for (int i = 0; i < 2; i++)
                    {
                        int frame = Time.frameCount;
                        while (Time.frameCount == frame) await NextFrame();
                        double nowMs = clock.Elapsed.TotalMilliseconds;
                        maxGapMs = Math.Max(maxGapMs, nowMs - previousMs);
                        previousMs = nowMs;
                    }
                    var pool = Object.FindFirstObjectByType<CannonballPool>();
                    if (pool == null || GameManager.Instance == null || GameManager.Instance.Player == null)
                        throw new InvalidOperationException("Gameplay did not initialize.");
                    if (pool.Effects.CreatedCount != 564 || pool.PreparedCount != 20 || Time.timeScale != 1f || AudioListener.pause)
                        throw new InvalidOperationException("Pools incomplete or game still paused at handoff.");
                    results.Add($"Run {sample}: sceneLoaded={loadedMs:F1} ms; ready and two gameplay frames={clock.Elapsed.TotalMilliseconds:F1} ms; longest observed update gap={maxGapMs:F1} ms; preparation frames={preparationFrames}; VFX prewarm={pool.Effects.PrewarmMilliseconds:F1} ms ({pool.Effects.CreatedCount} instances); projectile prewarm={pool.ProjectilePrewarmMilliseconds:F1} ms. PASS: progress monotonic, input/time/waves gated, full pools at handoff, repeated Play ignored.");
                    SessionState.SetString(ResultKey, "RUNNING\n" + string.Join("\n", results));
                }
                finally { SceneManager.sceneLoaded -= loaded; }
            }
            SessionState.SetString(ResultKey, "COMPLETE — Editor measurements; OS/asset caches may be warm. No artificial delay. Update gaps include other engine/editor work, not just pooling.\n" + string.Join("\n", results));
        }
        catch (Exception ex)
        {
            SessionState.SetString(ResultKey, "FAILED: " + ex + "\n" + string.Join("\n", results));
            UnityEngine.Debug.LogException(ex);
        }
        finally { running = false; }
    }

    private static async Task NextFrame()
    {
        await Task.Yield();
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode stopped during measurement.");
    }
}
