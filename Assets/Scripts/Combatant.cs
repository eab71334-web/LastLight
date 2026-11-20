using UnityEngine;

/// <summary>A fighter (player or bot): team, health, damage.</summary>
public class Combatant : MonoBehaviour
{
    public int team;
    public string displayName = "Unit";
    public float maxHealth = 100f;
    public float health = 100f;
    public bool isPlayer;
    public Transform eye;

    public bool Alive { get; private set; }
    float lastHurt;

    void Awake() { health = maxHealth; Alive = true; }

    void Update()
    {
        if (isPlayer && Alive && health < maxHealth && Time.time - lastHurt > 4f)
            health = Mathf.Min(maxHealth, health + 25f * Time.deltaTime);
    }

    public void TakeDamage(float dmg, Combatant by)
    {
        if (!Alive) return;
        health -= dmg;
        lastHurt = Time.time;
        MatchManager mm = MatchManager.I;
        if (isPlayer && mm != null) mm.OnPlayerHurt();
        if (by != null && by.isPlayer && mm != null) mm.OnHitMarker();
        if (health <= 0f)
        {
            health = 0f;
            Alive = false;
            if (mm != null) mm.OnKilled(this, by);
        }
    }

    public void Revive() { health = maxHealth; Alive = true; lastHurt = Time.time; }
}
