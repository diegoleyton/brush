using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = Color.white;
    public Color bottomColor = Color.black;
    public bool horizontal = false;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        UIVertex vertex = new UIVertex();

        // Encontramos los extremos para normalizar
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float value = horizontal ? vertex.position.x : vertex.position.y;
            if (value > max) max = value;
            if (value < min) min = value;
        }

        // Aplicamos el gradiente
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float value = horizontal ? vertex.position.x : vertex.position.y;
            float t = Mathf.InverseLerp(min, max, value);
            vertex.color *= Color.Lerp(bottomColor, topColor, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}