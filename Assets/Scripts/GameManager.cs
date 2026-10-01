using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MatchState { Starting, WaitingForSpawnPoints, Fighting, BetweenWaves, GameOver }

/// <summary>
/// Scene-local entry point for match management. Reloading the gameplay scene
/// creates a fresh manager along with its scene-owned systems.
/// </summary>
[DefaultExecutionOrder(-1100)]
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Enemy Spawning")]
    [Tooltip("Minimum horizontal distance from the player to an enemy spawn point, in world units.")]
    [SerializeField, Min(0f)] private float minimumEnemySpawnDistance = 300f;

    public float MinimumEnemySpawnDistance => minimumEnemySpawnDistance;

    [SerializeField] private ShipHealth playerPrefab;
    [SerializeField] private PlayerSceneBindings playerSceneBindings;
    [SerializeField] private float playerSpawnHeight = -5f;
    private ShipHealth player;
    public ShipHealth Player => player;
    public bool IsInitialized { get; private set; }
    [Tooltip("EnemyShip prefab variant used for wave spawning.")]
    [SerializeField] private ShipHealth enemyPrefab;
    [SerializeField] private Transform patrolPointsRoot;
    [SerializeField] private CannonballPool cannonballPool;
    [SerializeField] private Transform spawnPointsRoot;
    [SerializeField, Min(0f)] private float firstWaveDelay = 5f;
    [SerializeField, Min(0f)] private float betweenWavesDelay = 10f;
    [Tooltip("Minimum horizontal separation between ship centers, including sinking ships.")]
    [SerializeField, Min(1f)] private float shipSpawnClearance = 150f;

    [Header("Enemy Starting Barrel Sets")]
    [SerializeField, Min(1)] private int firstWaveWithBarrels = 1;
    [Tooltip("Each chance is rolled only if every preceding roll failed.")]
    [SerializeField, Range(0f, 100f)] private float fourSetsChancePercent = 20f;
    [SerializeField, Range(0f, 100f)] private float threeSetsChancePercent = 40f;
    [SerializeField, Range(0f, 100f)] private float twoSetsChancePercent = 60f;
    [SerializeField, Range(0f, 100f)] private float oneSetChancePercent = 80f;

    public MatchState State { get; private set; } = MatchState.Starting;
    public int CurrentWave { get; private set; }
    public int CompletedWaves { get; private set; }
    public int EnemiesDestroyed { get; private set; }
    public int EnemiesRemaining => enemies.Count;
    public int EnemiesAtWaveStart { get; private set; }
    public float SurvivalTime { get; private set; }
    public float Countdown { get; private set; }

    private readonly List<ShipHealth> enemies = new();
    private readonly List<ShipHealth> spawnedShips = new();
    private readonly List<Transform> spawnPoints = new();
    private readonly List<Transform> candidates = new();
    private readonly List<Transform> selectedPoints = new();
    private float nextSpawnAttempt;
    [SerializeField] private float enemySpawnHeight = 0f;
    private Transform spawnStagingRoot;

    public static int EnemyCountForWave(int wave)
    {
        if (wave <= 1) return 1;
        if (wave <= 3) return 2;
        if (wave <= 6) return 3;
        return 4;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Remove only this component: the object may host other scene systems.
            enabled = false;
            Destroy(this);
            return;
        }

        Instance = this;

        if (playerPrefab == null || playerSceneBindings == null || playerPrefab.gameObject.scene.IsValid() ||
            playerPrefab.GetComponent<PlayerInputController>() == null ||
            enemyPrefab == null || spawnPointsRoot == null ||
            patrolPointsRoot == null || cannonballPool == null || enemyPrefab.gameObject.scene.IsValid() ||
            enemyPrefab.GetComponent<AIController>() == null ||
            enemyPrefab.GetComponent<EnemyHudController>() == null ||
            enemyPrefab.GetComponent<WeaponSystem>() == null ||
            enemyPrefab.GetComponent<BarrelAmmo>() == null)
        {
            Debug.LogError("GameManager requires a player, enemy prefab with AI/HUD/weapons, patrol points, cannonball pool, and spawn points.", this);
            enabled = false;
            return;
        }

        // Instantiate below an inactive parent so scene dependencies are assigned before Awake/OnEnable.
        var staging = new GameObject("Ship Spawn Staging");
        staging.SetActive(false);
        staging.transform.SetParent(transform, false);
        spawnStagingRoot = staging.transform;
        foreach (Transform point in spawnPointsRoot)
            if (point.gameObject.activeSelf) spawnPoints.Add(point);

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("GameManager has no active spawn points.", this);
            enabled = false;
            return;
        }

        Transform start = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Vector3 position = start.position;
        position.y = playerSpawnHeight;
        Quaternion rotation = Quaternion.Euler(0f, start.eulerAngles.y, 0f);
        player = Instantiate(playerPrefab, position, rotation, spawnStagingRoot);
        player.name = "PlayerShip";
        player.gameObject.SetActive(false);
        player.GetComponent<WeaponSystem>().SetCannonballPool(cannonballPool);
        playerSceneBindings.Bind(player);
        player.transform.SetParent(null, true);
        player.OnDeath += EndMatch;
        player.gameObject.SetActive(true);
        if (player.TryGetComponent<Rigidbody>(out var body))
        {
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        Countdown = firstWaveDelay;
    }

    private void Start()
    {
        if (player == null) return;
        player.Heal(player.MaxHealth);
        player.GetComponent<BarrelAmmo>().ResetInventory();
        player.GetComponent<PlayerShipParts>().ResetInventory();
        IsInitialized = true;
    }

    private void Update()
    {
        if (GameplayLoadingScreen.BlocksGameplayInput || PauseMenuController.IsPaused || State == MatchState.GameOver) return;
        SurvivalTime += Time.deltaTime;

        // Deaths are observed at 0 HP, independently of the sinking sequence.
        if (State == MatchState.Fighting)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] != null && !enemies[i].IsDead) continue;
                if (enemies[i] != null) EnemiesDestroyed++;
                enemies.RemoveAt(i);
            }
            if (enemies.Count == 0)
            {
                CompletedWaves++;
                State = MatchState.BetweenWaves;
                Countdown = betweenWavesDelay;
            }
            return;
        }

        Countdown = Mathf.Max(0f, Countdown - Time.deltaTime);
        if (Countdown > 0f || Time.time < nextSpawnAttempt) return;
        if (!TrySpawnWave())
        {
            State = MatchState.WaitingForSpawnPoints;
            nextSpawnAttempt = Time.time + 1f;
        }
    }

    private bool TrySpawnWave()
    {
        int count = EnemyCountForWave(CurrentWave + 1);
        candidates.Clear();
        ShipHealth[] ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        foreach (Transform point in spawnPoints)
        {
            if (point == null || !point.gameObject.activeInHierarchy ||
                HorizontalDistanceSquared(point.position, player.transform.position) < minimumEnemySpawnDistance * minimumEnemySpawnDistance)
                continue;
            bool occupied = false;
            foreach (ShipHealth ship in ships)
                if (HorizontalDistanceSquared(point.position, ship.transform.position) < shipSpawnClearance * shipSpawnClearance)
                { occupied = true; break; }
            if (!occupied) candidates.Add(point);
        }

        // Shuffle, then search for a complete, pairwise-separated group. Never partially spawn a wave.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        selectedPoints.Clear();
        if (!SelectPoints(0, count)) return false;

        CurrentWave++;
        EnemiesAtWaveStart = count;
        foreach (Transform point in selectedPoints)
        {
            Vector3 position = point.position;
            position.y = enemySpawnHeight;
            ShipHealth enemy = Instantiate(enemyPrefab, position, Quaternion.Euler(0f, point.eulerAngles.y, 0f), spawnStagingRoot);
            enemy.gameObject.SetActive(false);
            enemy.GetComponent<BarrelAmmo>().ConfigureStartingAmmo(RollEnemyBarrelSets(CurrentWave));
            enemy.GetComponent<AIController>().ConfigureSceneReferences(player.transform, patrolPointsRoot);
            enemy.GetComponent<WeaponSystem>().SetCannonballPool(cannonballPool);
            enemy.GetComponent<EnemyHudController>().SetPlayer(player.transform);
            enemy.transform.SetParent(null, true);
            enemy.name = $"Enemy Wave {CurrentWave} - {point.name}";
            enemies.Add(enemy);
            spawnedShips.Add(enemy);
            enemy.gameObject.SetActive(true);
        }
        State = MatchState.Fighting;
        return true;
    }

    private int RollEnemyBarrelSets(int wave)
    {
        if (wave < firstWaveWithBarrels) return 0;
        if (Random.Range(0, 1000000) < fourSetsChancePercent * 10000f) return 4;
        if (Random.Range(0, 1000000) < threeSetsChancePercent * 10000f) return 3;
        if (Random.Range(0, 1000000) < twoSetsChancePercent * 10000f) return 2;
        if (Random.Range(0, 1000000) < oneSetChancePercent * 10000f) return 1;
        return 0;
    }

    private bool SelectPoints(int startIndex, int remaining)
    {
        if (remaining == 0) return true;
        for (int i = startIndex; i <= candidates.Count - remaining; i++)
        {
            Transform candidate = candidates[i];
            bool separated = true;
            foreach (Transform selected in selectedPoints)
                if (HorizontalDistanceSquared(candidate.position, selected.position) < shipSpawnClearance * shipSpawnClearance)
                { separated = false; break; }
            if (!separated) continue;
            selectedPoints.Add(candidate);
            if (SelectPoints(i + 1, remaining - 1)) return true;
            selectedPoints.RemoveAt(selectedPoints.Count - 1);
        }
        return false;
    }

    private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return (a - b).sqrMagnitude;
    }

    private void EndMatch()
    {
        if (State == MatchState.GameOver) return;
        // Include kills from this frame even if player death happened before Update.
        foreach (ShipHealth enemy in enemies)
            if (enemy != null && enemy.IsDead) EnemiesDestroyed++;
        enemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        if (State == MatchState.Fighting && enemies.Count == 0)
            CompletedWaves++;
        State = MatchState.GameOver;
        Countdown = 0f;
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    private void LateUpdate()
    {
        // Sinking deactivates ships. Retire finished clones so long matches do not accumulate them.
        for (int i = spawnedShips.Count - 1; i >= 0; i--)
        {
            ShipHealth ship = spawnedShips[i];
            if (ship != null && ship.gameObject.activeSelf) continue;
            if (ship != null) Destroy(ship.gameObject);
            spawnedShips.RemoveAt(i);
        }
    }

    private void OnDestroy()
    {
        if (player != null) player.OnDeath -= EndMatch;
        if (Instance == this)
            Instance = null;
    }
}
