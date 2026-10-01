using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Persistent, prefab-authored overlay for the menu-to-game transition.</summary>
public sealed class GameplayLoadingScreen : MonoBehaviour
{
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button returnButton;
    [SerializeField, Min(0.25f)] private float prewarmBudgetMilliseconds = 2f;
    private static GameplayLoadingScreen instance;
    private static int releasedFrame = -1;
    private bool running;
    private bool returning;
    public static bool IsLoading => instance != null && instance.running;
    public static bool BlocksGameplayInput => IsLoading || Time.frameCount == releasedFrame;
    public float Progress { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; releasedFrame = -1; }

    public static bool Begin(GameplayLoadingScreen prefab, string sceneName)
    {
        if (IsLoading || prefab == null || !Application.CanStreamedLevelBeLoaded(sceneName)) return false;
        var screen = Instantiate(prefab);
        if (screen.progressFill == null || screen.statusText == null || screen.returnButton == null)
        {
            Debug.LogError("Loading screen prefab has missing UI references.");
            Destroy(screen.gameObject);
            return false;
        }
        instance = screen;
        screen.running = true;
        DontDestroyOnLoad(screen.gameObject);
        screen.returnButton.gameObject.SetActive(false);
        screen.returnButton.onClick.AddListener(screen.ReturnToMenu);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        screen.StartCoroutine(screen.Load(sceneName));
        return true;
    }

    private IEnumerator Load(string sceneName)
    {
        SetProgress(0f, "Loading the battlefield...");
        // Present the overlay before starting scene IO/activation. No artificial minimum duration.
        yield return null;
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(sceneName); }
        catch (Exception exception) { Fail(exception); }
        if (operation == null) yield break;
        while (!operation.isDone)
        {
            // Unity's scene progress stops at 0.9 before activation. This is a work-stage weight, not an ETA.
            SetProgress(0.7f * Mathf.Clamp01(operation.progress / 0.9f), "Loading the battlefield...");
            yield return null;
        }
        SetProgress(0.7f, "Preparing to sail...");
        // Awake has registered pool targets; let Start initialize HUD/player state while time and input stay gated.
        yield return null;
        var manager = GameManager.Instance;
        var pool = FindFirstObjectByType<CannonballPool>();
        if (manager == null || !manager.enabled || !manager.IsInitialized || pool == null)
        {
            Fail(new InvalidOperationException("Gameplay initialization did not complete."));
            yield break;
        }
        int total = pool.InitialPoolSize + pool.Effects.PrewarmTargetCount;
        bool pending = true;
        while (pending)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                do
                {
                    pending = pool.PrepareOne();
                }
                while (pending && (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 /
                    System.Diagnostics.Stopwatch.Frequency < prewarmBudgetMilliseconds);
            }
            catch (Exception exception) { Fail(exception); yield break; }
            int completed = pool.PreparedCount + pool.Effects.PrewarmedCount;
            SetProgress(0.7f + 0.28f * (total == 0 ? 1f : Mathf.Clamp01((float)completed / total)), "Preparing to sail...");
            if (pending) yield return null;
        }
        // Render a prepared gameplay frame under the overlay before handing control to the player.
        yield return null;
        if (manager == null || !manager.IsInitialized || pool == null || pool.PrepareOne())
        {
            Fail(new InvalidOperationException("Gameplay readiness changed during loading."));
            yield break;
        }
        SetProgress(1f, "Ready to sail");
        yield return new WaitForEndOfFrame();
        Release();
        Destroy(gameObject);
    }

    private void SetProgress(float value, string label)
    {
        Progress = Mathf.Max(Progress, value);
        progressFill.fillAmount = Progress;
        statusText.text = label;
    }

    private void Fail(Exception exception)
    {
        Debug.LogException(exception, this);
        statusText.text = "Unable to prepare the voyage. Return to the main menu and try again.";
        returnButton.gameObject.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnButton.gameObject);
    }

    private void ReturnToMenu()
    {
        if (returning) return;
        returning = true;
        returnButton.interactable = false;
        StartCoroutine(Return());
    }

    private IEnumerator Return()
    {
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync("MainMenu"); }
        catch (Exception exception) { Fail(exception); }
        if (operation == null)
        {
            returning = false;
            returnButton.interactable = true;
            yield break;
        }
        yield return operation;
        Release();
        Destroy(gameObject);
    }

    private void Release()
    {
        running = false;
        releasedFrame = Time.frameCount;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (instance == this) instance = null;
    }

    private void OnDestroy()
    {
        if (instance == this) Release();
    }
}
