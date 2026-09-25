using System;
using UnityEngine;
using TMPro;

public class PlayerShipParts : MonoBehaviour
{
    [SerializeField] private int shipParts;
    [SerializeField] private TMP_Text shipPartsText;

    public int ShipParts => shipParts;

    public event Action<int> ShipPartsChanged;
    
    private void Start()
    {
        UpdateShipPartsText();
    }

    private void UpdateShipPartsText()
    {
        if (shipPartsText != null)
            shipPartsText.text = shipParts.ToString();    }

    public void AddShipPart()
    {
        shipParts++;
        UpdateShipPartsText();
        ShipPartsChanged?.Invoke(shipParts);
    }

    public bool TryUseShipPart()
    {
        if (shipParts <= 0)
            return false;

        shipParts--;
        UpdateShipPartsText();
        ShipPartsChanged?.Invoke(shipParts);
        return true;
    }
}