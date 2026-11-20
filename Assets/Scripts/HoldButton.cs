using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Button that reports whether a finger is holding it (used for FIRE).</summary>
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool Held { get; private set; }
    public void OnPointerDown(PointerEventData e) { Held = true; }
    public void OnPointerUp(PointerEventData e) { Held = false; }
    void OnDisable() { Held = false; }
}
