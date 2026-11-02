using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6.5f;
    public float height = 1.7f;
    public float yaw;
    public float pitch = 18f;
    public float minPitch = -5f;
    public float maxPitch = 60f;
    public float mouseSensitivity = 3f;

    public void AddLook(float dx, float dy)
    {
        yaw += dx;
        pitch = Mathf.Clamp(pitch - dy, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // editor / desktop testing: hold right mouse button to look
        if (Input.GetMouseButton(1))
            AddLook(Input.GetAxis("Mouse X") * mouseSensitivity, Input.GetAxis("Mouse Y") * mouseSensitivity);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        Vector3 dir = -(rot * Vector3.forward);
        float dist = distance;

        RaycastHit hit;
        if (Physics.SphereCast(focus, 0.25f, dir, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(1.2f, hit.distance - 0.1f);

        transform.position = focus + dir * dist;
        transform.rotation = rot;
    }
}
