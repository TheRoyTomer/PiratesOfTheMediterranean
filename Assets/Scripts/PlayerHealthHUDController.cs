using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class PlayerHealthHUDController : MonoBehaviour
{
    [SerializeField] private ShipHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text percentageText;

    private float lastHealth = float.NaN;
    private float lastMaxHealth = float.NaN;

    private void OnEnable()
    {
        lastHealth = float.NaN;
        lastMaxHealth = float.NaN;
    }

    private void LateUpdate()
    {
        if (playerHealth == null || healthFill == null)
            return;

        float current = playerHealth.CurrentHealth;
        float maximum = playerHealth.MaxHealth;
        if (current == lastHealth && maximum == lastMaxHealth)
            return;

        float fraction = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        healthFill.fillAmount = fraction;
        healthFill.color = GetHealthColor(fraction);
        if (percentageText != null)
            percentageText.SetText("{0}%", Mathf.RoundToInt(fraction * 100f));
        lastHealth = current;
        lastMaxHealth = maximum;
    }

    private static Color GetHealthColor(float fraction)
    {
        if (fraction >= 0.75f)
            return Color.green;

        Color orange = new Color(1f, 0.5f, 0f);
        if (fraction >= 0.35f)
            return Color.Lerp(Color.green, orange, Mathf.InverseLerp(0.75f, 0.35f, fraction));

        if (fraction >= 0.05f)
            return Color.Lerp(orange, Color.red, Mathf.InverseLerp(0.35f, 0.05f, fraction));

        return Color.red;
    }
}
