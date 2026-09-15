using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Debug.Log($"{name} destroyed");
        }
    }
}