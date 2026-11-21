using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Gives an Image (and optional Button) a glossy rounded look, a press animation and a click sound.</summary>
[RequireComponent(typeof(Image))]
public class StyledButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Look { Primary, Danger, Gold, Ghost, Dark, Cyan, Orange, Card }

    public Look look = Look.Primary;
    public bool circle;
    public float alpha = 1f;
    public bool pulse;
    public bool silent;

    Image img;
    Button btn;
    Vector3 baseScale = Vector3.one;
    bool down;
    float s = 1f;

    void Awake()
    {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();
        baseScale = transform.localScale;
        Apply();
    }

    void Apply()
    {
        Color top, bot, bor;
        switch (look)
        {
            case Look.Primary: top = new Color(1f, 0.42f, 0.2f); bot = new Color(0.85f, 0.1f, 0.18f); bor = new Color(1f, 0.82f, 0.55f); break;
            case Look.Danger: top = new Color(0.9f, 0.2f, 0.3f); bot = new Color(0.45f, 0.04f, 0.12f); bor = new Color(1f, 0.55f, 0.6f); break;
            case Look.Gold: top = new Color(1f, 0.86f, 0.32f); bot = new Color(0.86f, 0.52f, 0.06f); bor = new Color(1f, 0.97f, 0.7f); break;
            case Look.Cyan: top = new Color(0.3f, 0.95f, 1f); bot = new Color(0.05f, 0.45f, 0.8f); bor = new Color(0.8f, 1f, 1f); break;
            case Look.Orange: top = new Color(1f, 0.75f, 0.25f); bot = new Color(0.95f, 0.38f, 0.05f); bor = new Color(1f, 0.92f, 0.7f); break;
            case Look.Card: top = new Color(0.2f, 0.26f, 0.55f); bot = new Color(0.07f, 0.08f, 0.22f); bor = new Color(1f, 0.82f, 0.35f); break;
            case Look.Dark: top = new Color(0.2f, 0.2f, 0.3f); bot = new Color(0.07f, 0.07f, 0.12f); bor = new Color(0.45f, 0.45f, 0.65f); break;
            default: top = new Color(1f, 1f, 1f, 0.16f); bot = new Color(1f, 1f, 1f, 0.05f); bor = new Color(1f, 1f, 1f, 0.55f); break;
        }
        if (circle)
            img.sprite = UiKit.Rounded(128, 128, 64, top, bot, bor, 5, true);
        else
        {
            int r = look == Look.Card ? 30 : 30;
            img.sprite = UiKit.Rounded(192, 96, r, top, bot, bor, 4, true);
        }
        img.type = circle ? Image.Type.Simple : Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, alpha);
        img.raycastTarget = true;
    }

    void Update()
    {
        float target = down ? 0.93f : 1f;
        if (pulse && !down) target += Mathf.Sin(Time.unscaledTime * 3f) * 0.025f;
        s = Mathf.Lerp(s, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        transform.localScale = baseScale * s;
        if (btn != null && img != null)
            img.color = new Color(1f, 1f, 1f, btn.interactable ? alpha : alpha * 0.4f);
    }

    public void OnPointerDown(PointerEventData e) { down = true; }

    public void OnPointerUp(PointerEventData e)
    {
        down = false;
        if (!silent && (btn == null || btn.interactable)) Sfx.Click();
    }

    public void OnPointerExit(PointerEventData e) { down = false; }
}
