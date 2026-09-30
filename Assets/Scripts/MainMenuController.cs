using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene-owned navigation and audio for the main menu.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "GameScene";
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button backButton;
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
        feedback.text = "";
        if (music != null && music.clip != null) music.Play();
        Select(playButton);
    }

    private void Update()
    {
        if (loading || !helpPanel.activeSelf || Keyboard.current == null) return;
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
        loading = true;
        Click();
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        foreach (var button in homePanel.GetComponentsInChildren<Button>()) button.interactable = false;
        feedback.text = "Preparing to sail...";
        // Let the click play and the loading label render before loading the arena.
        yield return new WaitForSecondsRealtime(0.15f);
        yield return SceneManager.LoadSceneAsync(gameplayScene);
    }

    public void OpenHelp()
    {
        if (loading) return;
        Click();
        homePanel.SetActive(false);
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
