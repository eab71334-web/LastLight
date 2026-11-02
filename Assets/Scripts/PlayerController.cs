using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5.5f;
    public float turnSpeed = 12f;
    public float gravity = -25f;
    public float maxRadius = 43f;

    public VirtualJoystick joystick;
    public Transform cam;
    public Button interactButton;
    public Text interactLabel;

    CharacterController cc;
    float vy;
    Vector3 spawn;
    Interactable nearest;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        spawn = transform.position;
        if (interactButton != null) interactButton.onClick.AddListener(Interact);
    }

    void Update()
    {
        bool locked = GameManager.I != null && GameManager.I.InputLocked;

        Vector2 input = Vector2.zero;
        if (!locked)
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (joystick != null) input += joystick.Value;
            input = Vector2.ClampMagnitude(input, 1f);
        }

        Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 move = fwd * input.y + right * input.x;

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * Time.deltaTime;
        cc.Move((move * speed + Vector3.up * vy) * Time.deltaTime);

        if (move.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);
        }

        // keep the player on the island
        Vector3 p = transform.position;
        Vector2 flat = new Vector2(p.x, p.z);
        if (flat.magnitude > maxRadius)
        {
            Vector3 push = new Vector3(-flat.x, 0f, -flat.y).normalized * (flat.magnitude - maxRadius);
            cc.Move(push);
        }
        if (transform.position.y < -6f) Respawn();

        UpdateNearest(locked);
        if (!locked && nearest != null && Input.GetKeyDown(KeyCode.E)) Interact();
    }

    void UpdateNearest(bool locked)
    {
        nearest = null;
        float best = float.MaxValue;
        foreach (Interactable it in Interactable.All)
        {
            Vector3 d = it.transform.position - transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist <= it.range && dist < best)
            {
                best = dist;
                nearest = it;
            }
        }

        bool show = !locked && nearest != null;
        if (interactButton != null && interactButton.gameObject.activeSelf != show)
            interactButton.gameObject.SetActive(show);
        if (show && interactLabel != null) interactLabel.text = nearest.prompt;
    }

    public void Interact()
    {
        if (GameManager.I != null && GameManager.I.InputLocked) return;
        if (nearest != null) nearest.Interact();
    }

    void Respawn()
    {
        cc.enabled = false;
        transform.position = spawn;
        cc.enabled = true;
        vy = 0f;
    }
}
