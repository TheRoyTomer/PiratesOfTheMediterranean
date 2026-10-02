using KWS;
using TMPro;
using UnityEngine;

/// <summary>Scene-owned dependencies assigned before the spawned player's Awake/OnEnable.</summary>
public sealed class PlayerSceneBindings : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private CameraModeController cameraModes;
    [SerializeField] private CannonReticleHUD cannonReticles;
    [SerializeField] private GameAudio gameAudio;
    [SerializeField] private PlayerHealthHUDController healthHUD;
    [SerializeField] private CooldownHUDController cooldownHUD;
    [SerializeField] private FiringDirectionUI firingDirectionUI;
    [SerializeField] private TMP_Text barrelAmmoText;
    [SerializeField] private TMP_Text shipPartsText;
    [SerializeField] private KWS_DynamicWavesSimulationZone wakeSimulation;
    [Header("Camera paths relative to the PlayerShip prefab root")]
    [SerializeField] private string frontCameraPath = "CameraViews/FrontFiringCamera";
    [SerializeField] private string rightCameraPath = "CameraViews/RightFiringCamera";
    [SerializeField] private string leftCameraPath = "CameraViews/LeftFiringCamera";
    [SerializeField] private string backCameraPath = "CameraViews/BackFiringCamera";

    public void Bind(ShipHealth player)
    {
        var input = player.GetComponent<PlayerInputController>();
        var direction = player.GetComponent<FiringDirectionController>();
        if (cannonReticles != null) cannonReticles.BindPlayer(player);
        input.SetCameraController(cameraModes);
        cameraFollow.SetTarget(player.transform);
        cameraModes.BindPlayer(direction,
            player.transform.Find(frontCameraPath).GetComponent<Camera>(),
            player.transform.Find(rightCameraPath).GetComponent<Camera>(),
            player.transform.Find(leftCameraPath).GetComponent<Camera>(),
            player.transform.Find(backCameraPath).GetComponent<Camera>());
        gameAudio.SetPlayer(input);
        healthHUD.SetPlayer(player);
        cooldownHUD.SetWeaponSystem(player.GetComponent<WeaponSystem>());
        firingDirectionUI.SetController(direction);
        player.GetComponent<BarrelAmmo>().SetHUD(barrelAmmoText);
        player.GetComponent<PlayerShipParts>().SetHUD(shipPartsText);
        wakeSimulation.FollowTarget = player.gameObject;
        DebugTopCamera.CreateForPlayer(cameraFollow.GetComponent<Camera>(), player.transform, cameraModes);
    }
}
