using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class FpsPlayer : MonoBehaviour
{
    public Transform cam;
    public VirtualJoystick joystick;
    public HoldButton fireButton;
    public Button jumpButton;
    public Button reloadButton;
    public WeaponController weapon;
    public float speed = 6.5f;
    public float jumpSpeed = 6.2f;
    public float gravity = -20f;

    CharacterController cc;
    float yaw, pitch, vy;
    bool jumpQueued;

    public bool FireHeld
    {
        get
        {
            bool f = fireButton != null && fireButton.Held;
#if UNITY_EDITOR || UNITY_STANDALONE
            f = f || Input.GetKey(KeyCode.F);
#endif
            return f;
        }
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;
    }

    void Start()
    {
        if (jumpButton != null) jumpButton.onClick.AddListener(() => { jumpQueued = true; });
        if (reloadButton != null) reloadButton.onClick.AddListener(() => { if (weapon != null) weapon.Reload(); });
    }

    public void AddLook(float dx, float dy)
    {
        yaw += dx;
        pitch = Mathf.Clamp(pitch - dy, -80f, 80f);
    }

    public void Face(float y) { yaw = y; pitch = 0f; vy = 0f; }

    void Update()
    {
        if (MatchManager.I != null && !MatchManager.I.Playing) return;

        Vector2 m = joystick != null ? joystick.Value : Vector2.zero;
#if UNITY_EDITOR || UNITY_STANDALONE
        m += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetMouseButton(1)) AddLook(Input.GetAxis("Mouse X") * 2f, Input.GetAxis("Mouse Y") * 2f);
        if (Input.GetKeyDown(KeyCode.Space)) jumpQueued = true;
        if (Input.GetKeyDown(KeyCode.R) && weapon != null) weapon.Reload();
#endif
        m = Vector2.ClampMagnitude(m, 1f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cam != null) cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (cc.isGrounded) { vy = -2f; if (jumpQueued) vy = jumpSpeed; }
        else vy += gravity * Time.deltaTime;
        jumpQueued = false;

        Vector3 mv = (transform.right * m.x + transform.forward * m.y) * speed;
        mv.y = vy;
        cc.Move(mv * Time.deltaTime);
    }
}
