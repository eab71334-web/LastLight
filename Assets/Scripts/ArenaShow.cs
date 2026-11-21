using UnityEngine;

/// <summary>
/// Arena light show: sweeping spotlight beams with light pools on the canvas, animated LED boards around the ring,
/// big screens showing the fighter names, and a TV camera jib that slowly moves. Text is auto-fitted to its target width.
/// </summary>
public class ArenaShow : MonoBehaviour
{
    public static ArenaShow I;

    public Transform[] movers;              // beam origins (aimed at the ring)
    public Material beamMat;                // additive particle material (may be null)
    public Transform[] pools;               // flat discs that follow the beam on the canvas
    public Renderer[] ledStrips;
    public TextMesh[] screenTexts;
    public TextMesh[] fitTexts;             // texts to auto-fit
    public float[] fitWidths;
    public Transform[] jibs;

    Renderer[] beams;
    float flash;
    bool fitted;
    string nameA = "YOU", nameB = "OPPONENT";

    void Awake()
    {
        I = this;
        if (beamMat == null || movers == null) return;
        Mesh cone = MakeCone(16, 22f, 3.2f);
        beams = new Renderer[movers.Length];
        for (int i = 0; i < movers.Length; i++)
        {
            GameObject g = new GameObject("Beam");
            g.transform.SetParent(movers[i], false);
            g.AddComponent<MeshFilter>().sharedMesh = cone;
            MeshRenderer r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = beamMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = new Material(beamMat);   // own instance so each beam can have its own colour
            beams[i] = r;
        }
        if (pools != null)
            for (int i = 0; i < pools.Length; i++)
            {
                Renderer pr = pools[i].GetComponent<Renderer>();
                if (pr != null)
                {
                    pr.material = new Material(beamMat);
                    pr.material.mainTexture = UiKit.Glow().texture;
                }
            }
    }

    static Mesh MakeCone(int seg, float length, float radius)
    {
        Vector3[] v = new Vector3[seg + 2];
        Color[] c = new Color[seg + 2];
        int[] t = new int[seg * 3];
        v[0] = Vector3.zero; c[0] = new Color(1f, 1f, 1f, 0.55f);
        for (int i = 0; i <= seg; i++)
        {
            if (i == seg) break;
            float a = 2f * Mathf.PI * i / seg;
            v[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, length);
            c[i + 1] = new Color(1f, 1f, 1f, 0f);
        }
        v[seg + 1] = v[1]; c[seg + 1] = c[1];
        for (int i = 0; i < seg; i++) { t[i * 3] = 0; t[i * 3 + 1] = i + 1; t[i * 3 + 2] = i + 2; }
        Mesh m = new Mesh();
        m.name = "BeamCone";
        m.vertices = v; m.colors = c; m.triangles = t;
        m.RecalculateBounds();
        return m;
    }

    public void SetNames(string a, string b)
    {
        nameA = a; nameB = b;
        if (screenTexts != null)
            for (int i = 0; i < screenTexts.Length; i++)
                if (screenTexts[i] != null) screenTexts[i].text = nameA + "\nVS\n" + nameB;
        fitted = false;
    }

    public void Flash() { flash = 1f; }

    void LateUpdate()
    {
        float t = Time.time;
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.5f);

        if (movers != null)
            for (int i = 0; i < movers.Length; i++)
            {
                if (movers[i] == null) continue;
                float ph = i * 1.7f;
                Vector3 target = new Vector3(Mathf.Sin(t * 0.7f + ph) * 3.6f, 0.05f, Mathf.Cos(t * 0.53f + ph * 1.3f) * 3.6f);
                Vector3 dir = target - movers[i].position;
                movers[i].rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                Color col = Color.HSVToRGB(Mathf.Repeat(t * 0.05f + i * 0.17f, 1f), 0.75f, 1f);
                float inten = 0.5f + 0.5f * Mathf.Sin(t * 1.3f + ph) + flash * 1.5f;
                if (beams != null && beams[i] != null && beams[i].material != null)
                    beams[i].material.SetColor("_TintColor", new Color(col.r, col.g, col.b, 0.35f * Mathf.Clamp01(0.45f + inten * 0.5f)));
                if (pools != null && i < pools.Length && pools[i] != null)
                {
                    pools[i].position = new Vector3(target.x, 0.07f, target.z);
                    Renderer pr = pools[i].GetComponent<Renderer>();
                    if (pr != null && pr.material != null)
                        pr.material.SetColor("_TintColor", new Color(col.r, col.g, col.b, 0.5f * Mathf.Clamp01(0.4f + inten * 0.4f)));
                }
            }

        if (ledStrips != null)
            for (int i = 0; i < ledStrips.Length; i++)
            {
                if (ledStrips[i] == null) continue;
                Color c = Color.HSVToRGB(Mathf.Repeat(t * 0.12f + i * 0.25f, 1f), 0.9f, 1f) * (1.6f + flash * 3f);
                ledStrips[i].material.SetColor("_EmissionColor", c);
            }

        if (jibs != null)
            for (int i = 0; i < jibs.Length; i++)
                if (jibs[i] != null) jibs[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.25f + i) * 14f, 0f);

        if (!fitted) FitTexts();
    }

    void FitTexts()
    {
        if (fitTexts == null || fitWidths == null) { fitted = true; return; }
        bool any = false;
        for (int i = 0; i < fitTexts.Length && i < fitWidths.Length; i++)
        {
            TextMesh tm = fitTexts[i];
            if (tm == null) continue;
            Renderer r = tm.GetComponent<Renderer>();
            if (r == null) continue;
            float w = r.bounds.size.x;
            if (w > 0.01f)
            {
                // measure along the text's own width regardless of rotation
                float cur = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
                if (cur > 0.01f) tm.transform.localScale *= fitWidths[i] / cur;
                any = true;
            }
        }
        if (any) fitted = true;
    }
}
