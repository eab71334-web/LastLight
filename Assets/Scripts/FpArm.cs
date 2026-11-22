using System.IO;
using UnityEngine;

/// <summary>
/// First-person arm + boxing glove cut from the same human mesh as the opponent (data in Resources/Boxer/fparm.bytes).
/// Sits on the glove root that Fighter moves for punches; hides the old primitive fist/arm when the data loads.
/// </summary>
public class FpArm : MonoBehaviour
{
    public bool left;
    public Material skinMat, gloveMat;

    static Mesh[] cache;

    void Awake()
    {
        Mesh m = Get(left ? 0 : 1);
        if (m == null) return;

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        GameObject rot = new GameObject("FpRot");
        rot.transform.SetParent(transform, false);
        rot.transform.localRotation = Quaternion.Euler(-26f, left ? 7f : -7f, 0f) * Quaternion.Euler(0f, 0f, left ? 57f : -57f);

        GameObject go = new GameObject("FpMesh");
        go.transform.SetParent(rot.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.115f);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { skinMat, gloveMat };
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
                    for (int i = 0; i < n; i++) v[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    for (int i = 0; i < n; i++) nr[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
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
                            t[i * 3] = a; t[i * 3 + 1] = d; t[i * 3 + 2] = b;   // reversed winding (right-handed data into Unity)
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
