using System.IO;
using UnityEngine;

/// <summary>
/// First-person arm + boxing glove cut from the same human mesh as the opponent (data in Resources/Boxer/fparm.bytes).
/// Sits on the glove root that Fighter moves for punches; hides the old primitive fist/arm when the data loads.
/// </summary>
public class FpArm : MonoBehaviour
{
    public bool left;
    public Material skinMat, wrapMat;

    static Mesh[] cache;
    Transform rot;
    public bool Ready { get { return rot != null; } }

    /// <summary>blk 0..1 = guard raised, ext 0..1 = punch extension. Same numbers as the browser preview.</summary>
    public void SetPose(float blk, float ext, int type = 0)
    {
        if (rot == null) return;
        float sg = left ? -1f : 1f;
        // end pose per punch type: 0 straight, 1 hook (knuckles turned in), 2 uppercut (fist tipped up)
        float pE = type == 2 ? -0.35f : 0.15f, yE = type == 1 ? -0.30f : 0.02f, rE = type == 1 ? 1.5f : (type == 2 ? 0.35f : 0.9f);
        float pitch = (0.35f + 0.6f * blk) * (1f - ext) + pE * ext;
        float yaw = sg * ((0.12f + 0.18f * blk) * (1f - ext) + yE * ext);
        float roll = sg * ((0.6f - 0.3f * blk) * (1f - ext) + rE * ext);
        rot.localRotation = Quaternion.Euler(-pitch * Mathf.Rad2Deg, -yaw * Mathf.Rad2Deg, 0f) * Quaternion.Euler(0f, 0f, roll * Mathf.Rad2Deg);
    }

    void Awake()
    {
        Mesh m = Get(left ? 0 : 1);
        if (m == null) return;

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        GameObject rotGo = new GameObject("FpRot");
        rot = rotGo.transform;
        rot.SetParent(transform, false);
        rot.localScale = Vector3.one * 1.4f;
        SetPose(0f, 0f);

        GameObject go = new GameObject("FpMesh");
        go.transform.SetParent(rot, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.08f);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { skinMat, wrapMat };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static Mesh Get(int side)
    {
        if (cache == null)
        {
            TextAsset ta = Resources.Load<TextAsset>("Boxer/fparm");
            if (ta == null) return null;
            cache = new Mesh[2];
            using (BinaryReader br = new BinaryReader(new MemoryStream(ta.bytes)))
            {
                br.ReadBytes(4);
                for (int s = 0; s < 2; s++)
                {
                    int n = br.ReadInt32();
                    Vector3[] v = new Vector3[n], nr = new Vector3[n];
                    for (int i = 0; i < n; i++) v[i] = new Vector3(-br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    for (int i = 0; i < n; i++) nr[i] = new Vector3(-br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    Mesh m = new Mesh();
                    m.name = "FpArm" + s;
                    m.vertices = v;
                    m.normals = nr;
                    m.subMeshCount = 2;
                    for (int k = 0; k < 2; k++)
                    {
                        int c = br.ReadInt32();
                        int[] t = new int[c * 3];
                        for (int i = 0; i < c; i++)
                        {
                            int a = br.ReadInt32(), b = br.ReadInt32(), d = br.ReadInt32();
                            t[i * 3] = a; t[i * 3 + 1] = d; t[i * 3 + 2] = b;   // x mirrored + reversed winding, same convention as boxer.bytes
                        }
                        m.SetTriangles(t, k);
                    }
                    m.RecalculateBounds();
                    cache[s] = m;
                }
            }
        }
        return cache[side];
    }
}
