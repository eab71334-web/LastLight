using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime-generated UI sprites (rounded glossy buttons, soft fades, glow) so no art files are needed.</summary>
public static class UiKit
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>Rounded rectangle with vertical gradient, border and a soft gloss on the top half. 9-sliced.</summary>
    public static Sprite Rounded(int w, int h, int radius, Color top, Color bottom, Color border, int borderW, bool gloss)
    {
        string key = "r" + w + "x" + h + "_" + radius + "_" + top + bottom + border + borderW + gloss;
        Sprite cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        float hw = w * 0.5f, hh = h * 0.5f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Abs(fx - hw) - (hw - radius);
                float cy = Mathf.Abs(fy - hh) - (hh - radius);
                float outside = new Vector2(Mathf.Max(cx, 0f), Mathf.Max(cy, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(cx, cy), 0f);
                float dist = outside + inside - radius;           // negative inside the shape
                float alpha = Mathf.Clamp01(0.5f - dist);
                Color fill = Color.Lerp(bottom, top, fy / h);
                if (gloss && fy > hh)
                {
                    float g = 0.16f * (1f - (fy - hh) / hh);
                    fill = Color.Lerp(fill, Color.white, g);
                }
                float bt = Mathf.Clamp01(dist + borderW + 0.5f);
                Color c = Color.Lerp(fill, border, bt);
                c.a = alpha * Mathf.Lerp(fill.a, border.a, bt);
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        cache[key] = s;
        return s;
    }

    /// <summary>Horizontal alpha fade (solid on one side, transparent on the other).</summary>
    public static Sprite HFade(Color c, bool solidLeft)
    {
        string key = "h" + c + solidLeft;
        Sprite cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;
        int w = 128, h = 4;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)(w - 1);
            if (!solidLeft) t = 1f - t;
            float a = Mathf.Pow(1f - t, 1.4f) * c.a;
            for (int y = 0; y < h; y++) px[y * w + x] = new Color(c.r, c.g, c.b, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        cache[key] = s;
        return s;
    }

    /// <summary>Soft radial glow (white, alpha falls off to the edge).</summary>
    public static Sprite Glow()
    {
        Sprite cached;
        if (cache.TryGetValue("glow", out cached) && cached != null) return cached;
        int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        cache["glow"] = s;
        return s;
    }
}
