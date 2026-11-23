using UnityEngine;

/// <summary>Little flying bits (teeth, mouthguard) that tumble and bounce on the ring floor.</summary>
public class Gib : MonoBehaviour
{
    public Vector3 vel, spin;
    float life = 5f;
    bool rest;
    static Material toothMat, guardMat;

    static Material Make(Color c)
    {
        Shader s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material m = new Material(s);
        m.color = c;
        return m;
    }

    /// <summary>kind 0 = tooth, 1 = mouthguard.</summary>
    public static void Spawn(Vector3 pos, Vector3 v, int kind)
    {
        if (toothMat == null) { toothMat = Make(new Color(0.96f, 0.93f, 0.82f)); guardMat = Make(new Color(0.15f, 0.55f, 0.95f)); }
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(g.GetComponent<Collider>());
        g.name = kind == 0 ? "Tooth" : "Mouthguard";
        g.transform.position = pos;
        g.transform.localScale = kind == 0 ? new Vector3(0.022f, 0.034f, 0.018f) : new Vector3(0.085f, 0.02f, 0.05f);
        g.transform.rotation = Random.rotation;
        g.GetComponent<Renderer>().sharedMaterial = kind == 0 ? toothMat : guardMat;
        Gib gb = g.AddComponent<Gib>();
        gb.vel = v;
        gb.spin = new Vector3(Random.Range(-720f, 720f), Random.Range(-720f, 720f), Random.Range(-720f, 720f));
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f) { Destroy(gameObject); return; }
        if (rest) return;
        vel.y -= 9.8f * dt;
        transform.position += vel * dt;
        transform.Rotate(spin * dt, Space.Self);
        Vector3 p = transform.position;
        if (p.y < 0.04f)
        {
            p.y = 0.04f;
            if (vel.y < -1.2f) { vel.y = -vel.y * 0.35f; vel.x *= 0.6f; vel.z *= 0.6f; spin *= 0.6f; }
            else rest = true;
            transform.position = p;
        }
    }
}
