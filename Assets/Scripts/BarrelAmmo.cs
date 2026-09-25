using System;
using TMPro;
using UnityEngine;

public class BarrelAmmo : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingAmmo;
    [SerializeField] private TMP_Text barrelAmmoText;

    public int Count { get; private set; }
    public event Action<int> OnCountChanged;

    private void Awake()
    {
        Count = startingAmmo;
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