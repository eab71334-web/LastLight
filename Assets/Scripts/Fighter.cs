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
    public float damageMul = 1f, cooldownMul = 1f, staminaRegen = 18f, blockCost = 1f, moveSpeed = 1.7f;
    public Transform gloveL, gloveR, head;
    public float reachZ = 0.75f;

    public bool Blocking { get; set; }
    public bool Ducking { get; set; }
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
    int ptype; float ppower = 0.5f; bool plegend;
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

    public bool TryPunch(bool cross) { return Throw(0, !cross, 0.5f, false); }

    /// <summary>type 0 straight, 1 hook, 2 uppercut. power 0..1 comes from how fast the stick was flicked.</summary>
    public bool Throw(int type, bool left, float power, bool legend)
    {
        if (!CanAct || Blocking || pendingKind != 0 || Time.time < nextPunch) return false;
        bool cross = type != 0 || !left;   // hooks and uppercuts always count as heavy
        float cost = type == 0 ? (cross ? 15f : 8f) : 14f + (type == 2 ? 2f : 0f);
        if (stamina < cost) return false;
        stamina -= cost;
        ptype = type; ppower = power; plegend = legend;
        pendingKind = cross ? 2 : 1;
        float wind = type == 0 ? (cross ? 0.16f : 0.09f) : (type == 1 ? 0.2f : 0.22f);
        impactAt = Time.time + wind;
        nextPunch = Time.time + (type == 0 ? (cross ? 0.6f : 0.32f) : 0.7f) * cooldownMul;
        animLeft = left;
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
        float sp = moveSpeed * (Blocking ? 0.5f : 1f) * (Ducking ? 0.6f : 1f);
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
        if (!isPlayer) FaceOpponent(dt);
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
        float range = ptype == 0 ? (cross ? 1.85f : 1.7f) : (ptype == 1 ? 1.55f : 1.4f);
        float cone = ptype == 0 ? 55f : (ptype == 1 ? 75f : 45f);
        if (to.magnitude > range) return;
        if (Vector3.Angle(transform.forward, to) > cone) return;
        float baseDmg = ptype == 0 ? (cross ? 13f : 7f) : (ptype == 1 ? 12f : 15f);
        float dmg = baseDmg * damageMul * (0.75f + 0.5f * ppower);
        if (ptype == 2 && Ducking) dmg *= 1.25f;      // nose shot from below
        if (plegend) dmg *= 1.6f;
        opponent.ReceiveHit(dmg, this, cross, ptype, ppower, plegend);
    }

    public void ReceiveHit(float dmg, Fighter from, bool heavy, int type = 0, float power = 0.5f, bool legend = false)
    {
        if (Down) return;
        if (Ducking && type == 0 && Random.value < 0.6f) { Sfx.Whoosh(transform.position + Vector3.up * 1.2f); return; }   // ducked under a straight
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
            if (Rig != null) Rig.Impulse(away, 0.3f);
        }
        else
        {
            hp -= dmg;
            stagger = heavy ? 0.32f : 0.16f;
            pendingKind = 0;
            transform.position += away * (heavy ? 0.45f : 0.2f);
            if (Rig != null) { Rig.Hit(false, heavy); Rig.Impulse(away, heavy ? 1f : 0.55f); }
        }
        ClampToRing();
        BoutManager.I.OnHit(this, from, dmg, blocked, heavy, type, power, legend);
        if (hp <= 0f) { hp = 0f; Down = true; Blocking = false; if (Rig != null) Rig.Knockout(); BoutManager.I.OnKnockout(this); }
    }

    float blk;

    Vector3 FpTarget(float sg, float ex, int type)
    {
        Vector3 g = new Vector3(sg * (0.24f - 0.15f * blk), -0.10f + 0.13f * blk, 0.52f - 0.02f * blk);
        Vector3 e;
        if (type == 1) e = new Vector3(-sg * 0.10f, 0.0f, 0.50f + reachZ * 0.55f);
        else if (type == 2) e = new Vector3(sg * 0.03f, 0.10f, 0.45f + reachZ * 0.5f);
        else e = new Vector3(sg * 0.07f, -0.02f, 0.52f + reachZ);
        Vector3 p = Vector3.Lerp(g, e, ex);
        float arc = 4f * ex * (1f - ex);
        if (type == 1) p.x += sg * 0.32f * arc;      // hook sweeps in from the side
        if (type == 2) p.y -= 0.40f * arc;           // uppercut dips then rises
        return p;
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
        FpArm fl = gloveL.GetComponent<FpArm>(), fr = gloveR.GetComponent<FpArm>();
        if (fl != null && fl.Ready && fr != null && fr.Ready)
        {
            blk += ((Blocking ? 1f : 0f) - blk) * Mathf.Min(1f, Time.deltaTime * 14f);
            float exL = animLeft ? a : 0f, exR = animLeft ? 0f : a;
            float k2 = Mathf.Min(1f, Time.deltaTime * 30f);
            gloveL.localPosition = Vector3.Lerp(gloveL.localPosition, FpTarget(-1f, exL, ptype), k2);
            gloveR.localPosition = Vector3.Lerp(gloveR.localPosition, FpTarget(1f, exR, ptype), k2);
            fl.SetPose(blk, exL, ptype); fr.SetPose(blk, exR, ptype);
            return;
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
