using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Scene-owned navigation and audio for the main menu.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "GameScene";
    [SerializeField] private GameplayLoadingScreen loadingScreenPrefab;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button backButton;
    [Header("Credits")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private ScrollRect creditsScroll;
    [SerializeField, Min(1f)] private float creditsScrollSpeed = 45f;
    private float creditsHold;
    [Header("Illustrated help — objects authored in the scene")]
    [SerializeField] private GameObject[] helpPages;
    [SerializeField] private TMP_Text helpHeading;
    [SerializeField] private TMP_Text pageNumber;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    private int currentHelpPage;
    private static readonly string[] HelpTitles =
    {
        "COMMAND YOUR SHIP", "CHOOSE YOUR VIEW", "COLLECT SUPPLIES",
        "KNOW YOUR HUD", "SURVIVE THE WAVES"
    };
    [SerializeField] private TMP_Text feedback;
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource uiAudio;
    [SerializeField] private AudioClip click;
    private bool loading;

    private void Awake()
    {
        if (previousPageButton != null) previousPageButton.onClick.AddListener(PreviousHelpPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextHelpPage);
    }

    private void OnDestroy()
    {
        if (previousPageButton != null) previousPageButton.onClick.RemoveListener(PreviousHelpPage);
        if (nextPageButton != null) nextPageButton.onClick.RemoveListener(NextHelpPage);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        homePanel.SetActive(true);
        helpPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        feedback.text = "";
        if (music != null && music.clip != null) music.Play();
        Select(playButton);
    }

    private void Update()
    {
        if (loading) return;
        if (creditsPanel != null && creditsPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseCredits();
            else ScrollCredits();
            return;
        }
        if (!helpPanel.activeSelf || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseHelp();
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame) PreviousHelpPage();
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame) NextHelpPage();
    }

    public void Play()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            feedback.text = "The game scene is unavailable.";
            Debug.LogError("Main menu cannot load scene: " + gameplayScene, this);
            return;
        }
        Click();
        if (!GameplayLoadingScreen.Begin(loadingScreenPrefab, gameplayScene))
        {
            feedback.text = "Unable to open the loading screen.";
            return;
        }
        loading = true;
        foreach (var button in homePanel.GetComponentsInChildren<Button>()) button.interactable = false;
    }

    public void OpenHelp()
    {
        if (loading) return;
        Click();
        homePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        helpPanel.SetActive(true);
        ShowHelpPage(0);
        Select(backButton);
    }

    public void PreviousHelpPage() => ChangeHelpPage(-1);
    public void NextHelpPage() => ChangeHelpPage(1);

    private void ChangeHelpPage(int direction)
    {
        if (loading || !helpPanel.activeSelf || helpPages == null) return;
        int next = currentHelpPage + direction;
        if (next < 0 || next >= helpPages.Length) return;
        Click();
        ShowHelpPage(next);
    }

    // Only changes visibility of scene-authored pages; creates no UI objects.
    public void ShowHelpPage(int index)
    {
        if (helpPages == null || helpPages.Length == 0) return;
        currentHelpPage = Mathf.Clamp(index, 0, helpPages.Length - 1);
        for (int i = 0; i < helpPages.Length; i++)
            if (helpPages[i] != null) helpPages[i].SetActive(i == currentHelpPage);
        if (helpHeading != null && currentHelpPage < HelpTitles.Length)
            helpHeading.text = HelpTitles[currentHelpPage];
        if (pageNumber != null) pageNumber.text = $"{currentHelpPage + 1} / {helpPages.Length}";
        if (previousPageButton != null) previousPageButton.interactable = currentHelpPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = currentHelpPage < helpPages.Length - 1;
        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (nextPageButton != null && selected == nextPageButton.gameObject && !nextPageButton.interactable)
                Select(previousPageButton);
            else if (previousPageButton != null && selected == previousPageButton.gameObject && !previousPageButton.interactable)
                Select(nextPageButton);
        }
    }

    public void CloseHelp()
    {
        Click();
        helpPanel.SetActive(false);
        homePanel.SetActive(true);
        Select(helpButton);
    }

    public void OpenCredits()
    {
        if (loading || creditsPanel == null) return;
        Click();
        homePanel.SetActive(false);
        helpPanel.SetActive(false);
        creditsPanel.SetActive(true);
        creditsHold = 3f;
        Canvas.ForceUpdateCanvases();
        if (creditsScroll != null)
        {
            creditsScroll.StopMovement();
            creditsScroll.verticalNormalizedPosition = 1f;
        }
        Select(creditsBackButton);
    }

    public void CloseCredits()
    {
        if (loading || creditsPanel == null || !creditsPanel.activeSelf) return;
        Click();
        creditsPanel.SetActive(false);
        homePanel.SetActive(true);
        Select(creditsButton);
    }

    private void ScrollCredits()
    {
        if (creditsScroll == null || creditsScroll.content == null || creditsScroll.viewport == null) return;
        float travel = creditsScroll.content.rect.height - creditsScroll.viewport.rect.height;
        if (travel <= 0f) return;
        if (creditsHold > 0f)
        {
            creditsHold -= Time.unscaledDeltaTime;
            return;
        }
        creditsScroll.StopMovement();
        if (creditsScroll.verticalNormalizedPosition <= 0f)
        {
            creditsScroll.verticalNormalizedPosition = 1f;
            creditsHold = 3f;
            return;
        }
        creditsScroll.verticalNormalizedPosition = Mathf.Max(0f,
            creditsScroll.verticalNormalizedPosition - creditsScrollSpeed * Time.unscaledDeltaTime / travel);
        if (creditsScroll.verticalNormalizedPosition <= 0f) creditsHold = 4f;
    }

    public void Quit()
    {
        if (loading) return;
        Click();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Click()
    {
        if (uiAudio != null && click != null) uiAudio.PlayOneShot(click);
    }

    private static void Select(Button button)
    {
        if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
