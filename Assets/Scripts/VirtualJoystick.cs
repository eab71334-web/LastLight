using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>On-screen stick. Put on the background image (center pivot); the handle must not block raycasts.</summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;
    public float radius = 110f;

    public Vector2 Value { get; private set; }

    public void OnPointerDown(PointerEventData e) { OnDrag(e); }

    public void OnDrag(PointerEventData e)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out local))
            return;
        Vector2 v = Vector2.ClampMagnitude(local, radius);
        if (handle != null) handle.anchoredPosition = v;
        Value = v / radius;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Value = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
