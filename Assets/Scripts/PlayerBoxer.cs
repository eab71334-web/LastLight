using UnityEngine;
using UnityEngine.UI;

/// <summary>Reads touch/keyboard input for the player boxer and drives the first-person camera.</summary>
public class PlayerBoxer : MonoBehaviour
{
    public Fighter fighter;
    public Transform cam;
    public VirtualJoystick joystick;
    public HoldButton blockButton;
    public Button jabButton, crossButton;

    float shake, bobT, camY = 1.65f, roll;

    void Start()
    {
        if (jabButton != null) jabButton.onClick.AddListener(() => fighter.TryPunch(false));
        if (crossButton != null) crossButton.onClick.AddListener(() => fighter.TryPunch(true));
    }

    public void Shake(float s) { shake = Mathf.Max(shake, s); }

    void Update()
    {
        float dt = Time.deltaTime;
        bool fighting = BoutManager.I != null && BoutManager.I.Fighting;
        Vector2 m = joystick != null ? joystick.Value : Vector2.zero;
        bool block = blockButton != null && blockButton.Held;
#if UNITY_EDITOR || UNITY_STANDALONE
        m += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.J)) fighter.TryPunch(false);
        if (Input.GetKeyDown(KeyCode.K)) fighter.TryPunch(true);
        block = block || Input.GetKey(KeyCode.L);
#endif
        m = Vector2.ClampMagnitude(m, 1f);
        fighter.Blocking = fighting && !fighter.Down && fighter.Stagger <= 0f && block;
        if (fighting && !fighter.Down)
        {
            Vector3 dir = transform.forward * m.y + transform.right * m.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            fighter.Move(dir, dt);
        }
        bobT += dt * (4f + m.magnitude * 6f);
    }

    void LateUpdate()
    {
        if (cam == null || fighter.opponent == null) return;
        float dt = Time.deltaTime;
        float targetY = fighter.Down ? 0.35f : 1.65f;
        camY = Mathf.Lerp(camY, targetY, dt * (fighter.Down ? 3f : 10f));
        roll = Mathf.Lerp(roll, fighter.Down ? 70f : 0f, dt * 3f);

        Vector3 pos = transform.position + Vector3.up * (camY + Mathf.Sin(bobT) * 0.025f);
        Vector3 target = fighter.opponent.transform.position + Vector3.up * 1.55f;
        Quaternion want = Quaternion.LookRotation((target - pos).normalized);
        if (fighter.Down) want = Quaternion.Euler(-20f, transform.eulerAngles.y, 0f);
        cam.rotation = Quaternion.Slerp(cam.rotation, want, 1f - Mathf.Exp(-16f * dt));
        shake = Mathf.MoveTowards(shake, 0f, dt * 2.2f);
        Vector3 off = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake * 0.06f;
        cam.position = pos + off;
        cam.rotation = cam.rotation * Quaternion.Euler(Random.Range(-1f, 1f) * shake * 3f, 0f, roll + Random.Range(-1f, 1f) * shake * 3f);
    }
}
