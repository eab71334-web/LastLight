using UnityEngine;

/// <summary>Keeps the HUD inside the screen notch / rounded corners.</summary>
public class SafeArea : MonoBehaviour
{
    RectTransform rt;
    Rect last;

    void Awake() { rt = (RectTransform)transform; Apply(); }
    void Update() { if (Screen.safeArea != last) Apply(); }

    void Apply()
    {
        Rect sa = Screen.safeArea;
        last = sa;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Vector2 min = sa.position;
        Vector2 max = sa.position + sa.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
