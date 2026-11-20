using UnityEngine;

/// <summary>Short-lived bullet trail.</summary>
public static class Tracer
{
    static Material mat;

    public static void Draw(Vector3 a, Vector3 b, Color c)
    {
        if (mat == null)
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Standard");
            mat = new Material(s);
        }
        GameObject go = new GameObject("Tracer");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = 0.05f;
        lr.endWidth = 0.02f;
        lr.startColor = c;
        lr.endColor = new Color(c.r, c.g, c.b, 0f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        Object.Destroy(go, 0.06f);
    }
}
