using KWS;
using UnityEngine;

[DefaultExecutionOrder(1100)]
public sealed class CannonReticleHUD : MonoBehaviour
{
    [SerializeField] private CameraModeController cameraModes;
    [SerializeField] private LineRenderer[] lines;
    private WeaponSystem weapons;
    private ShipHealth health;
    private FiringDirectionController direction;

    public void BindPlayer(ShipHealth player)
    {
        health = player;
        weapons = player.GetComponent<WeaponSystem>();
        direction = player.GetComponent<FiringDirectionController>();
    }

    private void Awake()
    {
        // Tiled UVs measure distance along each line, so dash spacing stays consistent.
        if (lines != null)
            foreach (LineRenderer line in lines)
                if (line != null) line.textureMode = LineTextureMode.Tile;
        HideAll();
    }

    private void LateUpdate()
    {
        bool visible = health != null && !health.IsDead && weapons != null && direction != null &&
            cameraModes != null && cameraModes.IsFiringViewActive &&
            !PauseMenuController.IsPaused && WaterSystem.Instance != null;
        if (!visible) { HideAll(); return; }
        Transform[] points = weapons.GetFirePoints(direction.SelectedDirection);
        for (int i = 0; i < lines.Length; i++)
        {
            bool show = i < points.Length && weapons.TryPredictWaterImpact(points[i],
                WaterSystem.Instance.WaterLevel, out _);
            if (show)
            {
                weapons.TryPredictWaterImpact(points[i], WaterSystem.Instance.WaterLevel, out Vector3 impact);
                lines[i].SetPosition(0, points[i].position);
                lines[i].SetPosition(1, impact);
            }
            lines[i].enabled = show;
        }
    }

    private void HideAll()
    {
        if (lines == null) return;
        foreach (LineRenderer line in lines)
            if (line != null) line.enabled = false;
    }

    private void OnDisable() => HideAll();
}
