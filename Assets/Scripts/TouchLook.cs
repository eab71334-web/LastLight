using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Drag anywhere on this area to rotate the camera.</summary>
public class TouchLook : MonoBehaviour, IDragHandler
{
    public ThirdPersonCamera cam;
    public float sensitivity = 0.14f;

    public void OnDrag(PointerEventData e)
    {
        if (cam != null) cam.AddLook(e.delta.x * sensitivity, e.delta.y * sensitivity);
    }
}
