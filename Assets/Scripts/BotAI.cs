using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BotAI : MonoBehaviour
{
    public Combatant self;
    public Transform eye;
    public float speed = 4.4f;
    public float damage = 9f;
    public float fireDelay = 0.3f;
    public float aimError = 0.05f;
    public float viewRange = 60f;

    CharacterController cc;
    Combatant target;
    float nextThink, nextShot, strafeUntil, spotTime, vy;
    float strafeSign = 1f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        strafeSign = Random.value < 0.5f ? -1f : 1f;
    }

    void Update()
    {
        if (self == null || !self.Alive || MatchManager.I == null || !MatchManager.I.Playing) return;
        float dt = Time.deltaTime;
        if (Time.time >= nextThink) { nextThink = Time.time + 0.3f; PickTarget(); }

        Vector3 move = Vector3.zero;
        if (target != null && target.Alive)
        {
            Vector3 to = target.eye.position - eye.position;
            float dist = to.magnitude;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            Vector3 fdir = flat.sqrMagnitude > 0.001f ? flat.normalized : transform.forward;
            bool vis = CanSee(target);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(fdir), 260f * dt);

            if (!vis) { move = fdir; spotTime = Time.time; }
            else
            {
                if (dist > 16f) move = fdir; else if (dist < 7f) move = -fdir;
                if (Time.time > strafeUntil)
                {
                    strafeSign = -strafeSign;
                    strafeUntil = Time.time + Random.Range(0.8f, 2f);
                }
                move += Vector3.Cross(Vector3.up, fdir) * strafeSign * 0.8f;
                if (Time.time - spotTime > 0.45f && Time.time >= nextShot) Shoot(dist);
            }
        }

        move = Avoid(move);
        if (move.sqrMagnitude > 1f) move.Normalize();
        Vector3 v = move * speed;
        if (cc.isGrounded) vy = -2f; else vy += -20f * dt;
        v.y = vy;
        cc.Move(v * dt);
    }

    void PickTarget()
    {
        target = null;
        float best = float.MaxValue;
        foreach (Combatant c in MatchManager.I.all)
        {
            if (c == null || c == self || c.team == self.team || !c.Alive) continue;
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; target = c; }
        }
    }

    bool CanSee(Combatant t)
    {
        Vector3 a = eye.position, b = t.eye.position;
        if ((b - a).sqrMagnitude > viewRange * viewRange) return false;
        RaycastHit h;
        if (Physics.Linecast(a, b, out h, ~0, QueryTriggerInteraction.Ignore))
            return h.collider.GetComponentInParent<Combatant>() == t;
        return true;
    }

    Vector3 Avoid(Vector3 m)
    {
        if (m.sqrMagnitude < 0.001f) return m;
        m.y = 0f;
        float mag = m.magnitude;
        Vector3 d = m / mag;
        Vector3 o = transform.position + Vector3.up * 0.6f;
        RaycastHit h;
        if (Physics.SphereCast(o, 0.35f, d, out h, 1.6f, ~0, QueryTriggerInteraction.Ignore)
            && h.collider.GetComponentInParent<Combatant>() == null)
        {
            Vector3 l = Quaternion.Euler(0f, -70f, 0f) * d;
            Vector3 r = Quaternion.Euler(0f, 70f, 0f) * d;
            bool lb = Physics.Raycast(o, l, 2f);
            bool rb = Physics.Raycast(o, r, 2f);
            if (!lb && (rb || strafeSign < 0f)) return l * mag;
            if (!rb) return r * mag;
            return -d * mag;
        }
        return m;
    }

    void Shoot(float dist)
    {
        nextShot = Time.time + fireDelay * Random.Range(0.9f, 1.4f);
        Vector3 aim = target.eye.position + Vector3.down * 0.2f;
        Vector3 dir = (aim - eye.position).normalized + Random.insideUnitSphere * aimError * Mathf.Clamp(dist / 20f, 0.5f, 2f);
        dir.Normalize();
        Vector3 end = eye.position + dir * 150f;
        RaycastHit h;
        if (Physics.Raycast(eye.position, dir, out h, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            end = h.point;
            Combatant c = h.collider.GetComponentInParent<Combatant>();
            if (c != null && c.team != self.team && c.Alive) c.TakeDamage(damage, self);
        }
        Color col = self.team == 0 ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.3f, 0.6f);
        Tracer.Draw(eye.position + dir * 0.6f + Vector3.down * 0.25f, end, col);
        Sfx.Shot(eye.position, 0.22f);
    }
}
