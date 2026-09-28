using System;
using UnityEngine;
using TMPro;

public class PlayerShipParts : MonoBehaviour
{
    public void SetHUD(TMP_Text text) => shipPartsText = text;

    [Tooltip("Starting inventory restored by ResetInventory.")]
    [SerializeField, Min(0)] private int shipParts;
    private int currentShipParts;
    [SerializeField] private TMP_Text shipPartsText;

    public int ShipParts => currentShipParts;

    public event Action<int> ShipPartsChanged;
    
    private void Awake()
    {
        currentShipParts = Mathf.Max(0, shipParts);
    }

    private void Start()
    {
        UpdateShipPartsText();
    }

    private void UpdateShipPartsText()
    {
        if (shipPartsText != null)
            shipPartsText.text = currentShipParts.ToString();    }

    public void AddShipPart()
    {
        currentShipParts++;
        UpdateShipPartsText();
        ShipPartsChanged?.Invoke(currentShipParts);
    }

    public void ResetInventory()
    {
        currentShipParts = Mathf.Max(0, shipParts);
        UpdateShipPartsText();
        ShipPartsChanged?.Invoke(currentShipParts);
    }

    public bool TryUseShipPart()
    {
        if (currentShipParts <= 0)
            return false;

        currentShipParts--;
        UpdateShipPartsText();
        ShipPartsChanged?.Invoke(currentShipParts);
        return true;
    }
}
