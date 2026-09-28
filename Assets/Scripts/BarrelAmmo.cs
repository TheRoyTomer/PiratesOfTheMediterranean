using System;
using TMPro;
using UnityEngine;

public class BarrelAmmo : MonoBehaviour
{
    public void SetHUD(TMP_Text text) => barrelAmmoText = text;

    [SerializeField, Min(0)] private int startingAmmo;
    [SerializeField] private TMP_Text barrelAmmoText;

    public int Count { get; private set; }
    public event Action<int> OnCountChanged;

    // The wave spawner calls this on the inactive instance before Awake.
    public void ConfigureStartingAmmo(int sets)
    {
        startingAmmo = Mathf.Max(0, sets);
    }

    private void Awake()
    {
        Count = Mathf.Max(0, startingAmmo);
    }

    private void Start()
    {
        UpdateBarrelAmmoText();
    }

    public bool TryUseOne()
    {
        if (Count <= 0)
            return false;

        Count--;
        UpdateBarrelAmmoText();
        OnCountChanged?.Invoke(Count);
        return true;
    }

    public void ResetInventory()
    {
        Count = Mathf.Max(0, startingAmmo);
        UpdateBarrelAmmoText();
        OnCountChanged?.Invoke(Count);
    }

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Count += amount;
        UpdateBarrelAmmoText();
        OnCountChanged?.Invoke(Count);
    }

    private void UpdateBarrelAmmoText()
    {
        if (barrelAmmoText != null)
            barrelAmmoText.text = Count.ToString();
    }
}
