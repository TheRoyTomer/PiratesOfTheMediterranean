using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Created only at runtime in the Editor or a development build.
public sealed class DebugTrajectoryTrail : MonoBehaviour
{
    private TrailRenderer trajectory;
    private Material trailMaterial;
    private bool visible = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateTrail()
    {
        GameObject player = GameObject.Find("PlayerShip");
        if (player == null || player.GetComponent<DebugTrajectoryTrail>() != null)
            return;

        Shader shader = Resources.Load<Shader>("DebugTrajectory");
        if (shader == null)
        {
            Debug.LogWarning("Debug trajectory shader is unavailable.");
            return;
        }

        DebugTrajectoryTrail controller = player.AddComponent<DebugTrajectoryTrail>();
        GameObject path = new GameObject("DebugTrajectoryTrail");
        path.transform.SetParent(player.transform, false);
        TrailRenderer trail = path.AddComponent<TrailRenderer>();
        trail.emitting = false;
        trail.time = 120f;
        trail.widthMultiplier = 4f;
        trail.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        trail.minVertexDistance = 0.5f;
        trail.alignment = LineAlignment.View;
        trail.numCapVertices = 4;
        trail.numCornerVertices = 2;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.autodestruct = false;
        controller.trailMaterial = new Material(shader)
        {
            name = "DebugTrajectory (Runtime)",
            hideFlags = HideFlags.DontSave
        };
        trail.sharedMaterial = controller.trailMaterial;
        controller.trajectory = trail;
        trail.Clear();
        trail.emitting = true;
    }
#endif

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || trajectory == null)
            return;

        if (keyboard.tKey.wasPressedThisFrame)
        {
            visible = !visible;
            // Start a fresh segment on re-enable; never bridge an unrecorded journey.
            if (visible)
                trajectory.Clear();
            trajectory.enabled = visible;
            trajectory.emitting = visible;
        }

        if (keyboard.rKey.wasPressedThisFrame)
            trajectory.Clear();
    }

    private void OnDisable()
    {
        if (trajectory != null)
        {
            trajectory.emitting = false;
            trajectory.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (trajectory != null)
        {
            trajectory.Clear();
            trajectory.enabled = visible;
            trajectory.emitting = visible;
        }
    }

    private void OnDestroy()
    {
        if (trajectory != null)
            Destroy(trajectory.gameObject);
        if (trailMaterial != null)
            Destroy(trailMaterial);
    }
}
