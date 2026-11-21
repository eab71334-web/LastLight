using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stadium crowd: tiered bleachers full of low-poly spectators. The front tiers are animated and react to the fight
/// (cheer on hits, gasp when the player is hurt, jump and roar on a knockout, camera flashes), the back tiers are static.
/// Everything is generated at runtime from the materials assigned by SceneBuilder.
/// </summary>
public class Crowd : MonoBehaviour
{
    public static Crowd I;

    public Material[] shirtMats, skinMats, hairMats;
    public Material legMat, seatMat, flashMat;
    public int sections = 8, tiers = 6, animatedTiers = 3;
    public float innerRadius = 9.5f, tierDepth = 1.05f, tierRise = 0.55f;
    public float floorY = -1.2f;

    class Fan
    {
        public Transform root, armL, armR;
        public Vector3 basePos; public Quaternion baseRot;
        public float phase, freq, excite;
    }

    readonly List<Fan> fans = new List<Fan>();
    Mesh cubeMesh, sphereMesh;
    Transform[] flashes;
    float[] flashT;
    AudioSource amb;
    float energy = 0.25f, baseEnergy = 0.25f, gaspT, clapT, time;

    void Awake()
    {
        I = this;
        cubeMesh = MakeCube();
        sphereMesh = MakeSphere(10, 7);
        Build();
        amb = gameObject.AddComponent<AudioSource>();
        amb.clip = Sfx.Ambience();
        amb.loop = true;
        amb.spatialBlend = 0f;
        amb.volume = 0.15f;
        amb.Play();
    }

    // ------------------------------------------------------------ public reactions
    public void SetBase(float b) { baseEnergy = Mathf.Clamp01(b); }
    public void Cheer(float amount) { energy = Mathf.Min(1f, energy + amount); }
    public void Burst(float vol) { Sfx.Cheer(vol); }
    public void Roar() { energy = 1f; Sfx.Cheer(1f); }
    public void Gasp() { gaspT = 1.2f; energy = Mathf.Max(0.2f, energy * 0.6f); Sfx.Gasp(0.8f); }

    // ------------------------------------------------------------ mesh helpers
    static Mesh MakeCube()
    {
        Vector3[] v = new Vector3[24];
        Vector3[] n = new Vector3[24];
        int[] t = new int[36];
        Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        for (int f = 0; f < 6; f++)
        {
            Vector3 d = dirs[f];
            Vector3 a = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 b = Vector3.Cross(d, a);
            int o = f * 4;
            v[o] = (d - a - b) * 0.5f; v[o + 1] = (d - a + b) * 0.5f; v[o + 2] = (d + a + b) * 0.5f; v[o + 3] = (d + a - b) * 0.5f;
            for (int k = 0; k < 4; k++) n[o + k] = d;
            int ti = f * 6;
            t[ti] = o; t[ti + 1] = o + 1; t[ti + 2] = o + 2; t[ti + 3] = o; t[ti + 4] = o + 2; t[ti + 5] = o + 3;
        }
        Mesh m = new Mesh();
        m.name = "CrowdCube";
        m.vertices = v; m.normals = n; m.triangles = t;
        // make sure triangles face outward
        FixWinding(m);
        m.RecalculateBounds();
        return m;
    }

    static void FixWinding(Mesh m)
    {
        Vector3[] v = m.vertices;
        int[] t = m.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            Vector3 fn = Vector3.Cross(b - a, c - a);
            Vector3 cen = (a + b + c) / 3f;
            if (Vector3.Dot(fn, cen) < 0f) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
        }
        m.triangles = t;
    }

    static Mesh MakeSphere(int lon, int lat)
    {
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        for (int j = 0; j <= lat; j++)
        {
            float th = Mathf.PI * j / lat;
            for (int i = 0; i <= lon; i++)
            {
                float ph = 2f * Mathf.PI * i / lon;
                v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.5f);
            }
        }
        for (int j = 0; j < lat; j++)
            for (int i = 0; i < lon; i++)
            {
                int a = j * (lon + 1) + i, b = a + 1, c = a + lon + 1, d = c + 1;
                t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
            }
        Mesh m = new Mesh();
        m.name = "CrowdSphere";
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        FixWinding(m);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    GameObject Piece(Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject g = new GameObject("p");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return g;
    }

    static Material Pick(Material[] a) { return a != null && a.Length > 0 ? a[Random.Range(0, a.Length)] : null; }

    // ------------------------------------------------------------ build
    void Build()
    {
        Random.InitState(1234);
        Transform seatsRoot = new GameObject("Bleachers").transform; seatsRoot.SetParent(transform, false);
        Transform staticFans = new GameObject("StaticFans").transform; staticFans.SetParent(transform, false);
        Transform liveFans = new GameObject("LiveFans").transform; liveFans.SetParent(transform, false);
        List<Vector3> flashSpots = new List<Vector3>();

        for (int s = 0; s < sections; s++)
        {
            float ang = s * Mathf.PI * 2f / sections;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 tan = new Vector3(-dir.z, 0f, dir.x);
            Quaternion face = Quaternion.LookRotation(-dir, Vector3.up);
            for (int k = 0; k < tiers; k++)
            {
                float r = innerRadius + k * tierDepth;
                float width = 2f * r * Mathf.Tan(Mathf.PI / sections);
                float top = floorY + (k + 1) * tierRise;
                // bleacher block
                GameObject blk = Piece(cubeMesh, seatMat, seatsRoot,
                    dir * (r + tierDepth * 0.5f) + new Vector3(0f, (floorY + top) * 0.5f, 0f),
                    new Vector3(width + 0.4f, top - floorY, tierDepth));
                blk.transform.rotation = face;
                blk.isStatic = true;

                int count = Mathf.Max(3, Mathf.FloorToInt(width / 0.7f));
                float step = width / count;
                bool live = k < animatedTiers;
                for (int c = 0; c < count; c++)
                {
                    float off = -width * 0.5f + step * (c + 0.5f);
                    Vector3 pos = dir * (r + tierDepth * 0.55f) + tan * off + new Vector3(0f, top, 0f);
                    pos += new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f));
                    Quaternion rot = face * Quaternion.Euler(0f, Random.Range(-18f, 18f), 0f);
                    Fan f = MakeFan(live ? liveFans : staticFans, pos, rot, live);
                    if (live)
                    {
                        fans.Add(f);
                        if (k == 0 && Random.value < 0.25f) flashSpots.Add(pos + Vector3.up * 1.7f);
                    }
                }
            }
        }

        // camera flash pool
        int fc = Mathf.Min(28, flashSpots.Count);
        flashes = new Transform[fc];
        flashT = new float[fc];
        for (int i = 0; i < fc; i++)
        {
            GameObject g = Piece(cubeMesh, flashMat, transform, flashSpots[Random.Range(0, flashSpots.Count)], new Vector3(0.16f, 0.16f, 0.04f));
            g.SetActive(false);
            flashes[i] = g.transform;
        }

        StaticBatchingUtility.Combine(staticFans.gameObject);
        StaticBatchingUtility.Combine(seatsRoot.gameObject);
        Random.InitState((int)(System.DateTime.Now.Ticks & 0x7fffffff));
    }

    Fan MakeFan(Transform parent, Vector3 pos, Quaternion rot, bool live)
    {
        GameObject rootGO = new GameObject("Fan");
        Transform root = rootGO.transform;
        root.SetParent(parent, false);
        root.position = pos;
        root.rotation = rot;
        Material shirt = Pick(shirtMats);
        Piece(cubeMesh, legMat, root, new Vector3(0f, 0.43f, 0f), new Vector3(0.36f, 0.86f, 0.22f));
        Piece(cubeMesh, shirt, root, new Vector3(0f, 1.16f, 0f), new Vector3(0.5f, 0.6f, 0.28f));
        Piece(sphereMesh, Pick(skinMats), root, new Vector3(0f, 1.62f, 0f), new Vector3(0.25f, 0.29f, 0.27f));
        if (Random.value < 0.6f)
            Piece(sphereMesh, Pick(hairMats), root, new Vector3(0f, 1.71f, -0.01f), new Vector3(0.27f, 0.2f, 0.29f));

        Transform pivotL = new GameObject("ArmL").transform; pivotL.SetParent(root, false); pivotL.localPosition = new Vector3(-0.31f, 1.4f, 0f);
        Transform pivotR = new GameObject("ArmR").transform; pivotR.SetParent(root, false); pivotR.localPosition = new Vector3(0.31f, 1.4f, 0f);
        Piece(cubeMesh, shirt, pivotL, new Vector3(0f, -0.25f, 0f), new Vector3(0.12f, 0.5f, 0.12f));
        Piece(cubeMesh, shirt, pivotR, new Vector3(0f, -0.25f, 0f), new Vector3(0.12f, 0.5f, 0.12f));

        Fan f = new Fan();
        f.root = root; f.armL = pivotL; f.armR = pivotR;
        f.basePos = pos; f.baseRot = rot;
        f.phase = Random.Range(0f, 6.28f); f.freq = Random.Range(1.6f, 2.8f); f.excite = Random.Range(0.1f, 0.85f);
        if (!live && Random.value < 0.22f)
        {
            pivotL.localRotation = Quaternion.Euler(-150f, 0f, -12f);
            pivotR.localRotation = Quaternion.Euler(-150f, 0f, 12f);
        }
        return f;
    }

    // ------------------------------------------------------------ update
    void Update()
    {
        float dt = Time.deltaTime;
        time += dt;
        energy = Mathf.MoveTowards(energy, baseEnergy, dt * 0.10f);
        gaspT -= dt;
        if (amb != null) amb.volume = Mathf.Lerp(amb.volume, 0.12f + 0.6f * energy, dt * 3f);

        // rhythmic clapping when the crowd is warmed up
        clapT -= dt;
        if (clapT <= 0f && energy > 0.45f) { Sfx.Clap(0.25f * energy); clapT = 0.5f; }

        for (int i = 0; i < fans.Count; i++)
        {
            Fan f = fans[i];
            float raise = Mathf.SmoothStep(0f, 1f, (energy - f.excite) / 0.3f);
            float w = time * f.freq * 2f * Mathf.PI + f.phase;
            bool covering = gaspT > 0f && f.excite > 0.35f;
            float bob = covering ? 0f : Mathf.Abs(Mathf.Sin(w)) * 0.22f * raise;
            f.root.position = f.basePos + new Vector3(0f, bob, 0f);
            f.root.rotation = f.baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(w * 0.5f) * 4f * (0.3f + energy));
            float a = -raise * 150f + Mathf.Sin(w * 2f) * 22f * raise;
            float spread = 14f * raise;
            if (covering) { a = -128f; spread = 25f; }
            f.armL.localRotation = Quaternion.Euler(a, 0f, -spread);
            f.armR.localRotation = Quaternion.Euler(a + (covering ? 0f : Mathf.Sin(w * 2f + 1.3f) * 15f * raise), 0f, spread);
        }

        // camera flashes
        if (flashes != null && energy > 0.3f && Random.value < dt * energy * 14f)
        {
            int k = Random.Range(0, flashes.Length);
            if (flashT[k] <= 0f) { flashT[k] = 0.07f; flashes[k].gameObject.SetActive(true); }
        }
        if (flashes != null)
            for (int i = 0; i < flashes.Length; i++)
                if (flashT[i] > 0f)
                {
                    flashT[i] -= dt;
                    if (flashT[i] <= 0f) flashes[i].gameObject.SetActive(false);
                }
    }
}
