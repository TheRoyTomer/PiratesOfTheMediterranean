using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MatchHUD : MonoBehaviour
{
    [SerializeField] private GameManager manager;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text wavesValue;
    [SerializeField] private TMP_Text enemiesValue;
    [SerializeField] private TMP_Text survivalValue;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuScene = "MainMenu";
    private bool showingResults;
    private bool leaving;
    private ShipSinking playerSinking;

    private void Awake()
    {
        restartButton.onClick.AddListener(Restart);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        resultsPanel.SetActive(false);
    }

    private void Update()
    {
        if (manager == null) return;
        if (playerSinking == null && manager.Player != null)
            playerSinking = manager.Player.GetComponent<ShipSinking>();
        bool finished = manager.State == MatchState.GameOver;
        bool revealResults = finished && (playerSinking == null || playerSinking.HasStartedFinalDescent);
        resultsPanel.SetActive(revealResults);
        if (revealResults && !showingResults && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        showingResults = revealResults;
        int seconds = Mathf.FloorToInt(manager.SurvivalTime);
        string elapsed = $"{seconds / 60:00}:{seconds % 60:00}";
        if (finished)
        {
            statusText.text = "";
            wavesValue.text = manager.CompletedWaves.ToString();
            enemiesValue.text = manager.EnemiesDestroyed.ToString();
            survivalValue.text = elapsed;
            return;
        }
        switch (manager.State)
        {
            case MatchState.Fighting:
                statusText.text = $"WAVE {manager.CurrentWave}   |   {manager.EnemiesRemaining}/{manager.EnemiesAtWaveStart} Alive   |   {elapsed}";
                break;
            case MatchState.WaitingForSpawnPoints:
                statusText.text = $"WAVE {manager.CurrentWave + 1} — WAITING FOR CLEAR SPAWN POINTS";
                break;
            default:
                statusText.text = $"WAVE {manager.CurrentWave + 1} IN {Mathf.CeilToInt(manager.Countdown)}   |   {elapsed}";
                break;
        }
    }

    private void Restart()
    {
        if (leaving) return;
        leaving = true;
        GameAudio.Play(GameSound.MenuClick);
        manager.RestartMatch();
    }

    private void ReturnToMainMenu()
    {
        if (leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError($"Scene '{mainMenuScene}' is missing from Build Settings.", this);
            return;
        }
        leaving = true;
        GameAudio.Play(GameSound.MenuClick);
        SceneManager.LoadScene(mainMenuScene);
    }

    private void OnDestroy()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
    }
}
