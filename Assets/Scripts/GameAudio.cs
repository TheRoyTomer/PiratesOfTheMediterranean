using UnityEngine;

public enum GameSound
{
    CannonFire, CannonImpact, CannonWaterSplash, CannonWhistle, ShipCollision,
    BarrelWaterSplash, HeavyExplosion, CapsizeSplash, Pickup, Repair,
    SwitchFireDirection, ActionDenied, MenuClick
}

/// <summary>Scene audio routing. Detached pooled voices survive projectile/ship destruction.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class GameAudio : MonoBehaviour
{
    [System.Serializable]
    public sealed class Sound
    {
        public GameSound id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
        public bool spatial = true;
        [Min(1f)] public float minDistance = 25f;
        [Min(2f)] public float maxDistance = 450f;
        [Min(0f)] public float minimumInterval = 0.04f;
        [Min(1)] public int maxVoices = 6;
    }

    public static GameAudio Instance { get; private set; }
    public Transform Player => player != null ? player.transform : null;

    public void SetPlayer(PlayerInputController ship)
    {
        player = ship;
        playerBody = ship.GetComponent<Rigidbody>();
        playerHealth = ship.GetComponent<ShipHealth>();
    }

    [SerializeField] private PlayerInputController player;
    [SerializeField] private Sound[] sounds;
    [Header("Environment")]
    [SerializeField] private AudioClip ocean;
    [SerializeField, Range(0f, 1f)] private float oceanVolume = 0.18f;
    [SerializeField] private AudioClip bowWater;
    [SerializeField, Range(0f, 1f)] private float bowWaterVolume = 0.2f;
    [SerializeField] private AudioClip sailRustle;
    [SerializeField, Range(0f, 1f)] private float sailVolume = 0.08f;
    [SerializeField] private AudioClip capsizeCreak;
    [SerializeField, Range(0f, 1f)] private float creakVolume = 0.4f;
    [Header("Reserved for menu screens")]
    [SerializeField] private AudioClip menuMusic;

    private readonly AudioSource[] voices = new AudioSource[32];
    private readonly GameSound[] voiceIds = new GameSound[32];
    private float[] nextAllowed;
    private AudioSource oceanSource, bowSource, sailSource;
    private Rigidbody playerBody;
    private ShipHealth playerHealth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        nextAllowed = new float[System.Enum.GetValues(typeof(GameSound)).Length];
        for (int i = 0; i < voices.Length; i++)
            voices[i] = CreateSource("Voice " + i, transform, null, false);
        oceanSource = CreateSource("Ocean", transform, ocean, true);
        bowSource = CreateSource("Bow water", transform, bowWater, true);
        sailSource = CreateSource("Sails", transform, sailRustle, true);
        if (player != null)
        {
            playerBody = player.GetComponent<Rigidbody>();
            playerHealth = player.GetComponent<ShipHealth>();
        }
    }

    private void Update()
    {
        bool sailing = player != null && player.isActiveAndEnabled &&
            playerHealth != null && !playerHealth.IsDead;
        float speed = sailing && playerBody != null
            ? Vector3.ProjectOnPlane(playerBody.linearVelocity, Vector3.up).magnitude : 0f;
        FadeLoop(oceanSource, oceanVolume);
        FadeLoop(bowSource, bowWaterVolume * Mathf.Clamp01(speed / 20f));
        FadeLoop(sailSource, sailing ? sailVolume * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(speed / 20f)) : 0f);
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        foreach (var source in new[] { oceanSource, bowSource, sailSource })
            if (source != null && source.clip != null && !source.isPlaying) source.Play();
    }

    private static void FadeLoop(AudioSource source, float target)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * 0.25f);
    }

    private static AudioSource CreateSource(string label, Transform parent, AudioClip clip, bool loop)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.dopplerLevel = 0f;
        source.clip = clip;
        source.loop = loop;
        source.volume = 0f;
        if (clip != null && loop) source.Play();
        return source;
    }

    public static void Play(GameSound id, Vector3 position = default, float intensity = 1f)
    {
        if (Instance != null && Instance.isActiveAndEnabled)
            Instance.PlaySound(id, position, intensity);
    }

    private void PlaySound(GameSound id, Vector3 position, float intensity)
    {
        if (sounds == null || (Time.timeScale <= 0f && id != GameSound.MenuClick)) return;
        Sound sound = System.Array.Find(sounds, entry => entry.id == id);
        if (sound == null || sound.clip == null || intensity <= 0f) return;
        float now = Time.unscaledTime;
        if (now < nextAllowed[(int)id]) return;
        // Ignore distant events before consuming the concurrency/interval allowance.
        if (sound.spatial && Player != null &&
            (position - Player.position).sqrMagnitude > sound.maxDistance * sound.maxDistance) return;
        int count = 0, free = -1;
        for (int i = 0; i < voices.Length; i++)
        {
            if (!voices[i].isPlaying) { if (free < 0) free = i; }
            else if (voiceIds[i] == id) count++;
        }
        if (free < 0 || count >= sound.maxVoices) return;
        nextAllowed[(int)id] = now + sound.minimumInterval;
        var source = voices[free];
        voiceIds[free] = id;
        source.transform.position = position;
        source.clip = sound.clip;
        source.volume = sound.volume * Mathf.Clamp01(intensity);
        source.spatialBlend = sound.spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = sound.minDistance;
        source.maxDistance = sound.maxDistance;
        source.priority = sound.spatial ? 128 : 64;
        source.Play();
    }

    public AudioSource CreateCapsizeSource(Transform ship)
    {
        var source = CreateSource("Capsize creaks", ship, capsizeCreak, false);
        source.loop = true;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 25f;
        source.maxDistance = 250f;
        return source;
    }

    public float CreakVolume => creakVolume;

    private void OnDisable()
    {
        foreach (var source in GetComponentsInChildren<AudioSource>()) source.Stop();
        if (Instance == this) Instance = null;
    }
}
