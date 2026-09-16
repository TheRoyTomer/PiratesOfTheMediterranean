using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipConfiguration : MonoBehaviour
{
    [SerializeField] private ShipConfig config;

    // The serialized reference is available before any component's Awake runs.
    public ShipConfig Config
    {
        get
        {
            if (config == null)
                throw new MissingReferenceException($"{name}: ShipConfiguration requires a ShipConfig asset.");

            return config;
        }
    }
}
