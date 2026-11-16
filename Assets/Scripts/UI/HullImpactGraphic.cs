using UnityEngine;
using UnityEngine.UI;

internal sealed class HullImpactGraphic : MaskableGraphic
{
    private static readonly Vector2[] Edge =
    {
        new(-1f, -1f), new(0f, -1f), new(1f, -1f), new(1f, 0f),
        new(1f, 1f), new(0f, 1f), new(-1f, 1f), new(-1f, 0f)
    };

    private static readonly Color ImpactColor = new(1f, 0.19f, 0.31f);
    private Vector2 bearing;
    private float intensity;

    internal void SetImpact(Vector2 direction, float strength)
    {
        bearing = direction;
        intensity = strength;
        if (gameObject.activeSelf != (strength > 0f)) gameObject.SetActive(strength > 0f);
        if (strength > 0f) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect bounds = rectTransform.rect;
        Vector2 half = bounds.size * 0.5f;
        Vector2 inner = Vector2.Max(Vector2.zero, half - new Vector2(half.x * 0.15f, half.y * 0.22f));

        for (int i = 0; i < Edge.Length; i++)
        {
            Vector2 edge = Edge[i];
            float direction = Mathf.Max(0f, Vector2.Dot(edge.normalized, bearing));
            float weight = bearing == Vector2.zero ? 0.37f : 0.18f + 0.55f * direction * direction;
            Color border = ImpactColor;
            border.a = intensity * weight;
            mesh.AddVert(bounds.center + Vector2.Scale(edge, half), border, Vector2.zero);
            mesh.AddVert(bounds.center + Vector2.Scale(edge, inner), Color.clear, Vector2.zero);
        }

        for (int i = 0; i < Edge.Length; i++)
        {
            int outer = i * 2;
            int next = ((i + 1) % Edge.Length) * 2;
            mesh.AddTriangle(outer, next + 1, next);
            mesh.AddTriangle(outer, outer + 1, next + 1);
        }
    }
}
