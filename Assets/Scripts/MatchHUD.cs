using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHUD : MonoBehaviour
{
    [SerializeField] private GameManager manager;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        restartButton.onClick.AddListener(Restart);
        resultsPanel.SetActive(false);
    }

    private void Update()
    {
        if (manager == null) return;
        bool finished = manager.State == MatchState.GameOver;
        resultsPanel.SetActive(finished);
        int seconds = Mathf.FloorToInt(manager.SurvivalTime);
        string elapsed = $"{seconds / 60:00}:{seconds % 60:00}";
        if (finished)
        {
            statusText.text = "";
            resultsText.text = $"GAME OVER\n\nWaves completed: {manager.CompletedWaves}\nEnemies destroyed: {manager.EnemiesDestroyed}\nSurvival time: {elapsed}";
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
        GameAudio.Play(GameSound.MenuClick);
        manager.RestartMatch();
    }

    private void OnDestroy()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
    }
}
