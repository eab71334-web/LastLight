using UnityEngine;
using UnityEngine.UI;

/// <summary>Blood splat on the screen: stays a moment, then fades and removes itself.</summary>
public class FadeOutUI : MonoBehaviour
{
    public float life = 3.5f;
    float t;
    Image im;
    Color start;

    void Start() { im = GetComponent<Image>(); if (im != null) start = im.color; }

    void Update()
    {
        t += Time.deltaTime;
        if (im != null) im.color = new Color(start.r, start.g, start.b, start.a * (1f - Mathf.Clamp01((t - 1f) / (life - 1f))));
        if (t >= life) Destroy(gameObject);
    }
}
