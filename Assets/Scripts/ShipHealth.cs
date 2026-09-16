using UnityEngine;

[RequireComponent(typeof(ShipConfiguration))]
public class ShipHealth : MonoBehaviour
{
    private ShipConfig config;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    // Also available to other components before this component's Awake.
    public float MaxHealth => (config != null ? config : GetComponent<ShipConfiguration>().Config).Combat.MaxHealth;
    public bool IsDead => isDead;
    public Vector3 FinalHitPosition { get; private set; }

    public event System.Action OnDeath;

    private void Awake()
    {
        config = GetComponent<ShipConfiguration>().Config;
        currentHealth = config.Combat.MaxHealth;
        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        // Damage without a contact point uses the ship's world position.
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector3 hitPosition)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (currentHealth <= 0f)
        {
            FinalHitPosition = hitPosition;
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        OnDeath?.Invoke();
    }
}
