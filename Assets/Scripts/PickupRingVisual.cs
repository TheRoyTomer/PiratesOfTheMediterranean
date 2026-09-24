using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PickupRingVisual : MonoBehaviour
{
    [SerializeField] private float radius = 32f;
    [SerializeField] private float baseWidth = 4f;
    [SerializeField] private Color ringColor = new Color(0.72f, 0.3f, 1f, 1f);
    [SerializeField] private float pulseSpeed = 4f;

    private const int Segments = 64;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private LineRenderer lineRenderer;
    private MaterialPropertyBlock propertyBlock;
    public Vector3 WorldCenter => transform.position;
    public float WorldRadius => transform.TransformVector(Vector3.right * radius).magnitude;

    private void OnEnable()
    {
        SetupRing();
    }

    private void OnDisable()
    {
        if (lineRenderer != null)
            lineRenderer.SetPropertyBlock(null);
    }

    private void OnValidate()
    {
        SetupRing();
    }

    private void Update()
    {
        if (!Application.isPlaying || lineRenderer == null)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        lineRenderer.widthMultiplier = baseWidth * Mathf.Lerp(0.85f, 1.25f, pulse);

        Color color = ringColor;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        Material material = lineRenderer.sharedMaterial;
        if (material == null || !material.HasProperty(BaseColorId))
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        lineRenderer.GetPropertyBlock(propertyBlock);

        Color materialColor = material.GetColor(BaseColorId);
        materialColor.a *= Mathf.Lerp(0.35f, 1f, pulse);
        propertyBlock.SetColor(BaseColorId, materialColor);

        lineRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetupRing()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = Segments;
        lineRenderer.widthMultiplier = baseWidth;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.startColor = ringColor;
        lineRenderer.endColor = ringColor;

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            lineRenderer.SetPosition(
                i,
                new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius)
            );
        }
    }
    
    public void Show()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.enabled = true;
    }

    public void Hide()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.enabled = false;
    }
}