using UnityEngine;

/// <summary>A boxer (player or bot): health, stamina, punches, blocking, hits.</summary>
public class Fighter : MonoBehaviour
{
    public const float RingHalf = 4.3f;

    public string displayName = "Fighter";
    public bool isPlayer;
    public Fighter opponent;
    public float maxHp = 100f, hp = 100f;
    public float maxStamina = 100f, stamina = 100f;
    public float damageMul = 1f, cooldownMul = 1f, staminaRegen = 18f, blockCost = 1f, moveSpeed = 3.4f;
    public Transform gloveL, gloveR, head;
    public float reachZ = 0.75f;

    public bool Blocking { get; set; }
    public bool Down { get; private set; }
    public float Stagger { get { return stagger; } }
    public bool IsWinding { get { return pendingKind != 0; } }
    public bool CanAct
    {
        get { return !Down && stagger <= 0f && BoutManager.I != null && BoutManager.I.Fighting; }
    }

    float stagger, nextPunch, impactAt, animStart, animDur, fallT, fallYaw;
    int pendingKind; // 0 none, 1 jab, 2 cross
    bool animLeft;
    Vector3 restL, restR;
    BoxerRig rig; bool rigTried;
    BoxerRig Rig { get { if (!rigTried) { rig = GetComponent<BoxerRig>(); rigTried = true; } return rig; } }

    void Start()
    {
        if (gloveL != null) restL = gloveL.localPosition;
        if (gloveR != null) restR = gloveR.localPosition;
    }

    public void ResetFighter()
    {
        hp = maxHp; stamina = maxStamina; Down = false; stagger = 0f; pendingKind = 0; fallT = 0f;
        Blocking = false; animDur = 0f; nextPunch = 0f;
        if (Rig != null) Rig.ResetPose();
    }

    public bool TryPunch(bool cross)
    {
        if (!CanAct || Blocking || pendingKind != 0 || Time.time < nextPunch) return false;
        float cost = cross ? 15f : 8f;
        if (stamina < cost) return false;
        stamina -= cost;
        pendingKind = cross ? 2 : 1;
        float wind = cross ? 0.16f : 0.09f;
        impactAt = Time.time + wind;
        nextPunch = Time.time + (cross ? 0.6f : 0.32f) * cooldownMul;
        animLeft = !cross;
        animStart = Time.time;
        animDur = wind * 2f + 0.12f;
        if (Rig != null) Rig.Punch(cross);
        Sfx.Whoosh(transform.position + Vector3.up * 1.4f);
        return true;
    }

    public void Move(Vector3 dir, float dt)
    {
        if (!CanAct) return;
        if (Rig != null && dir.sqrMagnitude > 0.01f) Rig.NoteMove();
        float sp = moveSpeed * (Blocking ? 0.5f : 1f);
        transform.position += dir * sp * dt;
        Separate();
        ClampToRing();
    }

    void Separate()
    {
        if (opponent == null) return;
        Vector3 d = transform.position - opponent.transform.position;
        d.y = 0f;
        float m = d.magnitude;
        if (m < 0.8f)
        {
            d = m < 0.001f ? -transform.forward : d / m;
            transform.position = opponent.transform.position + d * 0.8f;
        }
    }

    void ClampToRing()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -RingHalf, RingHalf);
        p.z = Mathf.Clamp(p.z, -RingHalf, RingHalf);
        transform.position = p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (stagger > 0f) stagger -= dt;
        if (BoutManager.I == null) return;

        if (Down)
        {
            if (!isPlayer && Rig == null)
            {
                fallT += dt * 2.2f;
                float k = Mathf.Clamp01(fallT);
                transform.rotation = Quaternion.Euler(Mathf.Lerp(0f, -85f, k), fallYaw, 0f);
                Vector3 p = transform.position; p.y = Mathf.Lerp(0f, 0.15f, k); transform.position = p;
            }
            return;
        }
        fallYaw = transform.eulerAngles.y;

        if (pendingKind != 0 && Time.time >= impactAt) { Resolve(); pendingKind = 0; }
        float regen = staminaRegen * (Blocking ? 0.4f : 1f);
        stamina = Mathf.Min(maxStamina, stamina + regen * dt);
        if (Rig != null) Rig.SetBlock(Blocking);
        FaceOpponent(dt);
        AnimateGloves();
    }

    void FaceOpponent(float dt)
    {
        if (opponent == null) return;
        Vector3 to = opponent.transform.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(to.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-14f * dt));
    }

    void Resolve()
    {
        if (opponent == null || opponent.Down) return;
        bool cross = pendingKind == 2;
        Vector3 to = opponent.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude > (cross ? 1.85f : 1.7f)) return;
        if (Vector3.Angle(transform.forward, to) > 55f) return;
        opponent.ReceiveHit((cross ? 13f : 7f) * damageMul, this, cross);
    }

    public void ReceiveHit(float dmg, Fighter from, bool heavy)
    {
        if (Down) return;
        Vector3 away = transform.position - from.transform.position;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;
        bool blocked = Blocking && stamina > 0f;
        if (blocked)
        {
            hp -= dmg * 0.15f;
            stamina -= dmg * 1.6f * blockCost;
            transform.position += away * 0.08f;
            if (stamina <= 0f) { stamina = 0f; Blocking = false; stagger = 0.55f; if (Rig != null) Rig.Stagger(); }
            else if (Rig != null) Rig.Hit(true, heavy);
        }
        else
        {
            hp -= dmg;
            stagger = heavy ? 0.32f : 0.16f;
            pendingKind = 0;
            transform.position += away * (heavy ? 0.45f : 0.2f);
            if (Rig != null) Rig.Hit(false, heavy);
        }
        ClampToRing();
        BoutManager.I.OnHit(this, from, dmg, blocked, heavy);
        if (hp <= 0f) { hp = 0f; Down = true; Blocking = false; if (Rig != null) Rig.Knockout(); BoutManager.I.OnKnockout(this); }
    }

    void AnimateGloves()
    {
        if (gloveL == null || gloveR == null) return;
        float a = 0f;
        if (animDur > 0f)
        {
            float t = (Time.time - animStart) / animDur;
            if (t >= 1f) animDur = 0f; else a = Mathf.Sin(t * Mathf.PI);
        }
        Vector3 tl = restL, tr = restR;
        if (Blocking)
        {
            tl = restL + new Vector3(0.2f, 0.25f, 0.1f);
            tr = restR + new Vector3(-0.2f, 0.25f, 0.1f);
        }
        if (animLeft) tl = Vector3.Lerp(tl, new Vector3(restL.x * 0.3f, restL.y + 0.08f, restL.z + reachZ), a);
        else tr = Vector3.Lerp(tr, new Vector3(restR.x * 0.3f, restR.y + 0.08f, restR.z + reachZ), a);
        float k = Mathf.Min(1f, Time.deltaTime * 30f);
        gloveL.localPosition = Vector3.Lerp(gloveL.localPosition, tl, k);
        gloveR.localPosition = Vector3.Lerp(gloveR.localPosition, tr, k);
    }
}
