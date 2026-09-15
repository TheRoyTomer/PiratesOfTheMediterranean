using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public Vector3 FinalHitPosition { get; private set; }

    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
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

        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}");

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

        Debug.Log($"{name} destroyed");
    }
}
