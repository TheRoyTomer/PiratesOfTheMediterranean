using UnityEngine;

/// <summary>Hull contact audio and creaks synchronized with the existing capsize phases.</summary>
[RequireComponent(typeof(ShipHealth), typeof(Rigidbody))]
public sealed class ShipAudio : MonoBehaviour
{
    private ShipHealth health;
    private ShipSinking sinking;
    private AudioSource creaks;
    private float nextCollision;

    private void Awake()
    {
        health = GetComponent<ShipHealth>();
        sinking = GetComponent<ShipSinking>();
    }

    private void Update()
    {
        if (GameAudio.Instance == null) return;
        bool rolling = sinking != null && sinking.IsCapsizing;
        if (rolling && creaks == null) creaks = GameAudio.Instance.CreateCapsizeSource(transform);
        if (creaks == null) return;
        if (rolling && !creaks.isPlaying && creaks.clip != null) creaks.Play();
        creaks.volume = Mathf.MoveTowards(creaks.volume,
            rolling ? GameAudio.Instance.CreakVolume : 0f, Time.deltaTime * 1.5f);
        if (!rolling && creaks.volume <= 0f) creaks.Stop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (health.IsDead || Time.time < nextCollision || collision.contactCount == 0) return;
        var otherShip = collision.collider.GetComponentInParent<ShipHealth>();
        if (otherShip != null && (otherShip.IsDead || health.GetInstanceID() > otherShip.GetInstanceID())) return;
        // Projectiles already have their own impact sound.
        if (collision.collider.GetComponentInParent<Cannonball>() != null) return;
        float strength = 0f;
        for (int i = 0; i < collision.contactCount; i++)
            strength = Mathf.Max(strength, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(i).normal)));
        if (strength < 1f) return;
        nextCollision = Time.time + 0.75f;
        GameAudio.Play(GameSound.ShipCollision, collision.GetContact(0).point, Mathf.Clamp01(strength / 20f));
    }

    private void OnDisable()
    {
        if (creaks != null) { creaks.Stop(); creaks.volume = 0f; }
    }
}
