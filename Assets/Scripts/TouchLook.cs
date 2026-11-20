using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Drag anywhere on this area to aim.</summary>
public class TouchLook : MonoBehaviour, IDragHandler
{
    public FpsPlayer player;
    public float sensitivity = 0.16f;

    public void OnDrag(PointerEventData e)
    {
        if (player != null) player.AddLook(e.delta.x * sensitivity, e.delta.y * sensitivity);
    }
}
