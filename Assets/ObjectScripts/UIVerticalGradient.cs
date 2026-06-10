using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image에 위→아래 수직 그라디언트 틴트를 입히는 효과.
/// 단색 사각형이 '위에서 빛을 받는 면'처럼 보여 입체감이 생긴다.
/// (SlidingPuzzleUI의 액자 명암에 사용)
/// </summary>
[AddComponentMenu("UI/Effects/Vertical Gradient")]
public class UIVerticalGradient : BaseMeshEffect
{
    public Color topColor = Color.white;
    public Color bottomColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        var verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < verts.Count; i++)
        {
            float y = verts[i].position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        float height = Mathf.Max(0.0001f, maxY - minY);

        for (int i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            float t = (v.position.y - minY) / height;
            v.color = (Color)v.color * Color.Lerp(bottomColor, topColor, t);
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}
