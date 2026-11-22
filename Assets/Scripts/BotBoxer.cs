using UnityEngine;

/// <summary>Simple boxing AI: keeps distance, strafes, throws combos, sometimes blocks.</summary>
public class BotBoxer : MonoBehaviour
{
    Vector3 curMove;
    public Fighter fighter;
    public float aggression = 0.5f;   // 0..1 how often it attacks
    public float reaction = 0.3f;     // 0..1 chance to block an incoming punch

    float nextAttack, strafeUntil, blockUntil, retreatUntil;
    float strafeDir = 1f;
    bool reacted;

    void Update()
    {
        if (fighter == null || fighter.opponent == null) return;
        BoutManager bm = BoutManager.I;
        if (fighter.Down || bm == null || !bm.Fighting) { fighter.Blocking = false; return; }
        float dt = Time.deltaTime;
        Fighter p = fighter.opponent;

        Vector3 to = p.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        Vector3 fwd = dist > 0.001f ? to / dist : transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        if (Time.time >= strafeUntil) { strafeDir = Random.value < 0.5f ? -1f : 1f; strafeUntil = Time.time + Random.Range(0.6f, 1.6f); }

        // defend
        if (p.IsWinding && !reacted)
        {
            reacted = true;
            if (dist < 2.3f && Random.value < reaction) blockUntil = Time.time + 0.5f;
        }
        if (!p.IsWinding) reacted = false;
        fighter.Blocking = Time.time < blockUntil && fighter.stamina > 5f && fighter.Stagger <= 0f;

        // tired: back off and recover
        if (fighter.stamina < 18f) retreatUntil = Time.time + 1.2f;
        bool retreat = Time.time < retreatUntil;

        Vector3 move = Vector3.zero;
        if (retreat) move = -fwd + right * strafeDir * 0.4f;
        else
        {
            if (dist > 1.8f) move = fwd;
            else if (dist < 1.15f) move = -fwd;
            move += right * strafeDir * 0.55f;
        }
        if (move.sqrMagnitude > 1f) move.Normalize();
        curMove = Vector3.MoveTowards(curMove, move, 4f * dt);   // accelerate smoothly, no sudden direction flips
        fighter.Move(curMove, dt);

        // attack
        if (!retreat && !fighter.Blocking && dist < 1.75f && Time.time >= nextAttack && fighter.CanAct)
        {
            nextAttack = Time.time + Random.Range(0.25f, 0.9f) / (0.4f + aggression);
            fighter.TryPunch(Random.value > 0.65f);
        }
    }
}
