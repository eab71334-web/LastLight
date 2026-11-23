using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Reads touch/keyboard input for the player boxer and drives the first-person camera.</summary>
public class PlayerBoxer : MonoBehaviour
{
    public Fighter fighter;
    public Transform cam;
    public VirtualJoystick joystick;
    public HoldButton blockButton;
    public Button jabButton, crossButton, handButton;
    public VirtualJoystick punchStick;
    public HoldButton duckButton;
    public Text handLabel;
    bool leftHand = true, armed = true, ducked;
    float stickStart = -1f, bufUntil; int bufType; bool bufLeft; float bufPower;

    float shake, bobT, camY = 1.65f, roll;
    float yaw, pitch;
    bool yawInit;
    public float lookSpeed = 0.16f;   // degrees per pixel of finger drag

    void Start()
    {
        if (jabButton != null) jabButton.onClick.AddListener(() => Punch(false));
        if (crossButton != null) crossButton.onClick.AddListener(() => Punch(true));
        if (handButton != null) handButton.onClick.AddListener(() => { leftHand = !leftHand; ShowHand(); });
        ShowHand();
    }

    public void Shake(float s) { shake = Mathf.Max(shake, s); }

    float kickP, kickY, kickPV, kickYV;
    /// <summary>Head snaps when you are hit.</summary>
    public void Kick(bool heavy)
    {
        float k = heavy ? 1f : 0.5f;
        kickPV += 55f * k;
        kickYV += Random.Range(-45f, 45f) * k;
    }

    /// <summary>Turn to face the opponent (start of a fight).</summary>
    public void FaceOpponentNow()
    {
        if (fighter == null || fighter.opponent == null) return;
        Vector3 to = fighter.opponent.transform.position - transform.position;
        yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        pitch = 0f;
        yawInit = true;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cam != null) cam.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void ShowHand() { if (handLabel != null) handLabel.text = leftHand ? "LEFT\nHAND" : "RIGHT\nHAND"; }

    void Punch(bool cross) { DoPunch(0, !cross, cross ? 0.6f : 0.5f); }

    // Flick the punch stick: up = straight, sideways = hook, down = uppercut. Faster flick = harder punch.
    void ReadPunchStick()
    {
        if (punchStick == null) return;
        Vector2 v = punchStick.Value;
        float mag = v.magnitude;
        if (mag < 0.2f) { armed = true; stickStart = -1f; return; }
        if (stickStart < 0f) stickStart = Time.unscaledTime;
        if (!armed || mag < 0.55f) return;
        armed = false;
        float power = Mathf.Clamp01(1.15f - (Time.unscaledTime - stickStart) / 0.28f);
        int type = Mathf.Abs(v.x) > Mathf.Abs(v.y) * 1.1f ? 1 : (v.y < -0.3f ? 2 : 0);
        bool left = type == 1 ? v.x < 0f : leftHand;
        DoPunch(type, left, power);
    }

    void DoPunch(int type, bool left, float power)
    {
        bufType = type; bufLeft = left; bufPower = power; bufUntil = Time.unscaledTime + 0.2f;
        TryBuffered();
    }

    void TryBuffered()
    {
        if (Time.unscaledTime > bufUntil) return;
        if (AimAndThrow()) bufUntil = 0f;
    }

    // Light aim assist: a punch thrown roughly at the opponent turns you most of the way to them.
    bool AimAndThrow()
    {
        if (!fighter.CanAct || fighter.Blocking || fighter.IsWinding) return false;
        bool legend = bufPower >= 0.9f && bufType > 0 && Random.value < (bufType == 2 && fighter.Ducking ? 0.7f : 0.4f);
        return PunchNow(bufType, bufLeft, bufPower, legend);
    }

    bool PunchNow(int type, bool left, float power, bool legend)
    {
        if (fighter.opponent != null && !fighter.Down)
        {
            Vector3 to = fighter.opponent.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude < 2.3f)
            {
                float target = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float diff = Mathf.DeltaAngle(yaw, target);
                if (Mathf.Abs(diff) < 50f) yaw += diff * 0.7f;
            }
        }
        return fighter.Throw(type, left, power, legend);
    }

    void ReadLook()
    {
        Vector2 d = Vector2.zero;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Moved) continue;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) continue;
            if (t.position.x < Screen.width * 0.25f && t.position.y < Screen.height * 0.6f) continue;   // joystick corner
            d += t.deltaPosition;
        }
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            d += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 12f;
        if (Input.GetKey(KeyCode.LeftArrow)) d.x -= 400f * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) d.x += 400f * Time.deltaTime;
#endif
        yaw += d.x * lookSpeed;
        pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -35f, 40f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool fighting = BoutManager.I != null && BoutManager.I.Fighting;
        Vector2 m = joystick != null ? joystick.Value : Vector2.zero;
        bool block = blockButton != null && blockButton.Held;
#if UNITY_EDITOR || UNITY_STANDALONE
        m += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.J)) Punch(false);
        if (Input.GetKeyDown(KeyCode.K)) Punch(true);
        block = block || Input.GetKey(KeyCode.L);
        ducked = Input.GetKey(KeyCode.C);
#endif
        bool duck = ducked || (duckButton != null && duckButton.Held);
        fighter.Ducking = fighting && !fighter.Down && duck;
        if (fighting && !fighter.Down) { ReadPunchStick(); TryBuffered(); }
        if (!yawInit) FaceOpponentNow();
        if (fighting && !fighter.Down) ReadLook();
        if (!fighter.Down) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
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
        if (cam == null) return;
        float dt = Time.deltaTime;
        float targetY = fighter.Down ? 0.35f : (fighter.Ducking ? 1.1f : 1.65f);
        camY = Mathf.Lerp(camY, targetY, dt * (fighter.Down ? 3f : 10f));
        roll = Mathf.Lerp(roll, fighter.Down ? 70f : 0f, dt * 3f);

        Vector3 pos = transform.position + Vector3.up * (camY + Mathf.Sin(bobT) * 0.025f);
        Quaternion want = Quaternion.Euler(pitch, yaw, 0f);
        if (fighter.Down) want = Quaternion.Euler(-20f, yaw, 0f);
        kickPV += (-90f * kickP - 12f * kickPV) * dt; kickP += kickPV * dt;
        kickYV += (-90f * kickY - 12f * kickYV) * dt; kickY += kickYV * dt;
        want = want * Quaternion.Euler(kickP, kickY, 0f);
        cam.rotation = Quaternion.Slerp(cam.rotation, want, 1f - Mathf.Exp(-16f * dt));
        shake = Mathf.MoveTowards(shake, 0f, dt * 2.2f);
        Vector3 off = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake * 0.06f;
        cam.position = pos + off;
        cam.rotation = cam.rotation * Quaternion.Euler(Random.Range(-1f, 1f) * shake * 3f, 0f, roll + Random.Range(-1f, 1f) * shake * 3f);
    }
}
