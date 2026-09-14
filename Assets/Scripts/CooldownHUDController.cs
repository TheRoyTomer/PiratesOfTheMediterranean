using UnityEngine;
using TMPro;

public class CooldownHUDController : MonoBehaviour
{
    [SerializeField] private WeaponSystem weaponSystem;

    [SerializeField] private TMP_Text frontCooldownText;
    [SerializeField] private TMP_Text leftCooldownText;
    [SerializeField] private TMP_Text rightCooldownText;
    
    
    private void Update()
    {
        UpdateCooldownText(frontCooldownText, weaponSystem.FrontCooldown);
        UpdateCooldownText(leftCooldownText, weaponSystem.LeftCooldown);
        UpdateCooldownText(rightCooldownText, weaponSystem.RightCooldown);
    }

    private void UpdateCooldownText(TMP_Text text, float cooldown)
    {
        if (cooldown <= 0f)
        {
            text.text = "";
            return;
        }

        text.text = cooldown.ToString("F1");

        float colorT = Mathf.Clamp01(cooldown / 7f);

        Color lightPink = new Color(0.95f, 0.25f, 0.35f);
        Color strongRed = new Color(0.75f, 0f, 0f);

        Color color = Color.Lerp(lightPink, strongRed, colorT);

        float alpha = Mathf.Clamp01(cooldown / 1f);

        color.a = alpha;
        text.color = color;
    }
}