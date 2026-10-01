#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Temporary photography helper. The compilation guard excludes it from builds.
public sealed class GameScreenshotCapture : MonoBehaviour
{
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private bool capturing;
    private double captureStarted;
    private static GameScreenshotCapture instance;

    public static string CaptureFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../Screenshots"));

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (EditorApplication.isPlaying)
            EnsureInstance();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            EnsureInstance();
        else if (state == PlayModeStateChange.ExitingPlayMode && instance != null)
            instance.CancelCapture();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var helper = new GameObject("Editor Screenshot Helper") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(helper);
        instance = helper.AddComponent<GameScreenshotCapture>();
    }

    private void OnEnable() => EditorApplication.update += CheckTimeout;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame
            && EditorWindow.focusedWindow != null
            && EditorWindow.focusedWindow.GetType().Name == "GameView")
            Capture();
    }

    [MenuItem("Tools/Screenshots/Capture Game Without HUD")]
    public static void Capture()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || GameplayLoadingScreen.IsLoading)
            return;
        EnsureInstance();
        if (instance.capturing) return;
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        instance.capturing = true;
        instance.captureStarted = EditorApplication.timeSinceStartup;
        instance.StartCoroutine(instance.SaveScreenshot());
    }

    [MenuItem("Tools/Screenshots/Open Screenshot Folder")]
    private static void OpenFolder()
    {
        Directory.CreateDirectory(CaptureFolder);
        EditorUtility.RevealInFinder(CaptureFolder);
    }

    private IEnumerator SaveScreenshot()
    {
        // Capture a fresh rendered frame with screen and world-space UI hidden.
        yield return null;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.enabled) continue;
            hiddenCanvases.Add(canvas);
            canvas.enabled = false;
        }

        yield return new WaitForEndOfFrame();
        Texture2D pixels = null;
        try
        {
            pixels = ScreenCapture.CaptureScreenshotAsTexture();
            RestoreCanvases();
            Directory.CreateDirectory(CaptureFolder);
            string path = Path.Combine(CaptureFolder, "Battle_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".png");
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Debug.Log("Game screenshot saved (without HUD): " + path);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save game screenshot: " + exception.Message);
        }
        finally
        {
            if (pixels != null) Destroy(pixels);
            RestoreCanvases();
            capturing = false;
        }
    }

    private void RestoreCanvases()
    {
        foreach (Canvas canvas in hiddenCanvases)
            if (canvas != null) canvas.enabled = true;
        hiddenCanvases.Clear();
    }

    private void CheckTimeout()
    {
        // WaitForEndOfFrame may stall if the user switches away from the Game View.
        if (capturing && EditorApplication.timeSinceStartup - captureStarted > 3)
            CancelCapture();
    }

    private void CancelCapture()
    {
        StopAllCoroutines();
        RestoreCanvases();
        capturing = false;
    }

    private void OnDisable()
    {
        EditorApplication.update -= CheckTimeout;
        CancelCapture();
    }
}
#endif
