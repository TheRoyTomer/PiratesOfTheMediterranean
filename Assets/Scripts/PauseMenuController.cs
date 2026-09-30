using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the pause panel authored in GameScene; never creates UI objects.</summary>
[DefaultExecutionOrder(-1200)]
[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private AudioSource uiAudio;
    [SerializeField] private AudioClip click;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private static PauseMenuController instance;
    public static bool IsPaused => instance != null && instance.paused;
    // Do not let the click/Space that resumes the game also fire a cannon.
    public static bool BlocksGameplayInput => instance != null &&
        (instance.paused || instance.leaving || Time.frameCount == instance.resumeFrame);

    private bool paused, leaving;
    private int resumeFrame = -1;
    private float previousTimeScale;
    private bool previousAudioPause, previousCursorVisible;
    private CursorLockMode previousCursorLock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private void Awake()
    {
        instance = this;
        panel.SetActive(false);
        resumeButton.onClick.AddListener(Resume);
        quitButton.onClick.AddListener(QuitToMainMenu);
        if (uiAudio != null) uiAudio.ignoreListenerPause = true;
    }

    private void Update()
    {
        if (leaving || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (paused || leaving || GameManager.Instance == null ||
            GameManager.Instance.State == MatchState.GameOver) return;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        panel.SetActive(true);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
        PlayClick();
    }

    public void Resume()
    {
        if (!paused || leaving) return;
        PlayClick();
        RestoreGameplay();
    }

    public void QuitToMainMenu()
    {
        if (!paused || leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError($"Scene '{mainMenuScene}' is missing from Build Settings.", this);
            return;
        }
        leaving = true;
        resumeButton.interactable = quitButton.interactable = false;
        PlayClick();
        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu()
    {
        yield return new WaitForSecondsRealtime(0.15f);
        // Keep the match frozen until its scene is unloaded. OnDisable restores globals.
        yield return SceneManager.LoadSceneAsync(mainMenuScene);
    }

    private void PlayClick()
    {
        if (uiAudio != null && click != null) uiAudio.PlayOneShot(click);
    }

    private void RestoreGameplay()
    {
        if (!paused) return;
        paused = false;
        resumeFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        if (panel != null) panel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDisable() => RestoreGameplay();

    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitToMainMenu);
        if (instance == this) instance = null;
    }
}
