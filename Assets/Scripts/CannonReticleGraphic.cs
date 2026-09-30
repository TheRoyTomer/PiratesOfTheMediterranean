using UnityEngine;
using UnityEngine.UI;

// Screen projection of a straight muzzle-to-impact guide, without endpoint symbols.
public sealed class CannonReticleGraphic : MaskableGraphic
{
    [SerializeField] private float thickness = 2f;
    [SerializeField] private float outline = 1f;
    [SerializeField] private Color outlineColor = new Color(0.025f, 0.02f, 0.015f, 1f);
    private Vector2 start;
    private Vector2 end;

    public void SetLine(Vector2 from, Vector2 to)
    {
        if ((start - from).sqrMagnitude < 0.001f && (end - to).sqrMagnitude < 0.001f) return;
        start = from;
        end = to;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        AddLine(vh, thickness + outline * 2f, outlineColor);
        AddLine(vh, thickness, color);
    }

    private void AddLine(VertexHelper vh, float lineWidth, Color tint)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < 0.001f) return;
        Vector2 side = new Vector2(-delta.y, delta.x).normalized * lineWidth * 0.5f;
        int first = vh.currentVertCount;
        vh.AddVert(start - side, tint, Vector2.zero);
        vh.AddVert(start + side, tint, Vector2.zero);
        vh.AddVert(end + side, tint, Vector2.zero);
        vh.AddVert(end - side, tint, Vector2.zero);
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first, first + 2, first + 3);
    }
}
