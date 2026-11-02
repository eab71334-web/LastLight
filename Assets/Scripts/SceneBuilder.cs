using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the whole game scene from code (island, village, lighthouse, characters, UI).
/// Run it from the menu "Story / Rebuild Main Scene" or let BuildScript call it during CI builds.
/// </summary>
public static class SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    // ------------------------------------------------------------------ helpers
    static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static Material Mat(string name, Color c, bool emissive = false, float smooth = 0.1f, float metal = 0f)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        m.SetFloat("_Glossiness", smooth);
        m.SetFloat("_Metallic", metal);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.8f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler,
                           Material mat, Transform parent, bool collider, bool isStatic)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Collider col = go.GetComponent<Collider>();
        if (!collider)
        {
            if (col != null) Object.DestroyImmediate(col);
        }
        else if (type == PrimitiveType.Cylinder)
        {
            // the default cylinder collider is a capsule; use the real shape
            Object.DestroyImmediate(col);
            go.AddComponent<MeshCollider>();
        }
        go.isStatic = isStatic;
        return go;
    }

    // solid, static, has collider
    static GameObject Solid(PrimitiveType t, string n, Vector3 p, Vector3 s, Material m, Transform parent,
                            Vector3 euler = default(Vector3))
    { return Prim(t, n, p, s, euler, m, parent, true, true); }

    // static decoration, no collider
    static GameObject Deco(PrimitiveType t, string n, Vector3 p, Vector3 s, Material m, Transform parent,
                           Vector3 euler = default(Vector3))
    { return Prim(t, n, p, s, euler, m, parent, false, true); }

    // moving part, no collider
    static GameObject Part(PrimitiveType t, string n, Vector3 p, Vector3 s, Material m, Transform parent,
                           Vector3 euler = default(Vector3))
    { return Prim(t, n, p, s, euler, m, parent, false, false); }

    static Light AddLight(Transform parent, string name, LightType type, Vector3 pos, Color color,
                          float intensity, float range, bool enabled)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = type;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        l.enabled = enabled;
        return l;
    }

    // ------------------------------------------------------------------ world pieces
    static void MakeTree(Transform parent, Vector3 pos, float s, Material trunk, Material leaf, Material leaf2)
    {
        GameObject root = new GameObject("Tree");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localScale = Vector3.one * s;
        Solid(PrimitiveType.Cylinder, "Trunk", new Vector3(0, 1.4f, 0), new Vector3(0.45f, 1.4f, 0.45f), trunk, root.transform);
        Deco(PrimitiveType.Sphere, "Leaves", new Vector3(0, 3.7f, 0), new Vector3(3.2f, 3.0f, 3.2f), leaf, root.transform);
        Deco(PrimitiveType.Sphere, "Leaves2", new Vector3(0.6f, 4.7f, 0.3f), new Vector3(2.2f, 2.2f, 2.2f), leaf2, root.transform);
    }

    static void MakeHouse(Transform parent, Vector3 pos, float yRot, Material wall, Material roof,
                          Material door, Material window, Material stone)
    {
        GameObject root = new GameObject("House");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0, yRot, 0);
        Solid(PrimitiveType.Cube, "Walls", new Vector3(0, 1.5f, 0), new Vector3(5, 3, 4), wall, root.transform);
        Deco(PrimitiveType.Cube, "Roof", new Vector3(0, 3.0f, 0), new Vector3(3.6f, 3.6f, 4.6f), roof, root.transform, new Vector3(0, 0, 45));
        Solid(PrimitiveType.Cube, "Chimney", new Vector3(1.5f, 5.0f, 0.8f), new Vector3(0.6f, 1.4f, 0.6f), stone, root.transform);
        Deco(PrimitiveType.Cube, "Door", new Vector3(0, 1.0f, 2.02f), new Vector3(1.1f, 2.0f, 0.1f), door, root.transform);
        Deco(PrimitiveType.Cube, "WindowL", new Vector3(-1.6f, 1.8f, 2.02f), new Vector3(0.8f, 0.8f, 0.1f), window, root.transform);
        Deco(PrimitiveType.Cube, "WindowR", new Vector3(1.6f, 1.8f, 2.02f), new Vector3(0.8f, 0.8f, 0.1f), window, root.transform);
    }

    static Npc MakeNpc(Transform parent, string name, Vector3 pos, float yRot, float scale,
                       Color cloth, Color hair, bool beard)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0, yRot, 0);
        root.transform.localScale = Vector3.one * scale;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.height = 1.9f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.95f, 0);

        Material clothM = Mat("Cloth_" + name, cloth);
        Material skin = Mat("Skin", C(0.87f, 0.68f, 0.55f));
        Material hairM = Mat("Hair_" + name, hair);
        Material dark = Mat("Dark", C(0.05f, 0.05f, 0.05f));

        Part(PrimitiveType.Capsule, "Body", new Vector3(0, 0.9f, 0), new Vector3(0.8f, 0.85f, 0.8f), clothM, root.transform);
        Part(PrimitiveType.Sphere, "Head", new Vector3(0, 1.85f, 0), new Vector3(0.42f, 0.42f, 0.42f), skin, root.transform);
        Part(PrimitiveType.Sphere, "Hair", new Vector3(0, 1.97f, -0.03f), new Vector3(0.46f, 0.3f, 0.46f), hairM, root.transform);
        Part(PrimitiveType.Sphere, "EyeL", new Vector3(-0.07f, 1.88f, 0.18f), new Vector3(0.06f, 0.06f, 0.06f), dark, root.transform);
        Part(PrimitiveType.Sphere, "EyeR", new Vector3(0.07f, 1.88f, 0.18f), new Vector3(0.06f, 0.06f, 0.06f), dark, root.transform);
        if (beard)
            Part(PrimitiveType.Sphere, "Beard", new Vector3(0, 1.72f, 0.1f), new Vector3(0.3f, 0.4f, 0.25f), hairM, root.transform);

        Npc npc = root.AddComponent<Npc>();
        npc.npcName = name;
        npc.prompt = "Talk";
        npc.range = 3.2f;
        return npc;
    }

    static Collectible MakeCollectible(Transform parent, string name, string partName, Vector3 pos,
                                       PrimitiveType shape, Vector3 visualScale, Vector3 visualEuler,
                                       Material mat, Color glow, string[] lines)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        GameObject vis = Part(shape, "Visual", new Vector3(0, 1.0f, 0), visualScale, mat, root.transform, visualEuler);
        AddLight(root.transform, "Glow", LightType.Point, new Vector3(0, 1.2f, 0), glow, 1.6f, 6f, true);
        Collectible c = root.AddComponent<Collectible>();
        c.partName = partName;
        c.pickupLines = lines;
        c.visual = vis.transform;
        c.prompt = "Take";
        c.range = 3.2f;
        return c;
    }

    // ------------------------------------------------------------------ UI helpers
    static RectTransform Stretch(Transform t, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        RectTransform r = (RectTransform)t;
        r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = offMin; r.offsetMax = offMax;
        return r;
    }

    static RectTransform Fixed(Transform t, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform r = (RectTransform)t;
        r.anchorMin = anchor; r.anchorMax = anchor; r.pivot = pivot;
        r.anchoredPosition = pos; r.sizeDelta = size;
        return r;
    }

    static Image MakeImage(Transform parent, string name, Color color, Sprite sprite = null,
                           bool sliced = false, bool raycast = true)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        if (sliced) img.type = Image.Type.Sliced;
        img.raycastTarget = raycast;
        return img;
    }

    static Text MakeText(Transform parent, string name, string text, int size, TextAnchor anchor,
                         Color color, FontStyle style = FontStyle.Normal)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Shadow sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.8f);
        sh.effectDistance = new Vector2(2, -2);
        return t;
    }

    // ------------------------------------------------------------------ main entry
    [MenuItem("Story/Rebuild Main Scene")]
    public static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- lighting & atmosphere
        Light sun = Object.FindObjectOfType<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(38, -32, 0);
        sun.color = new Color(1f, 0.92f, 0.78f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.005f;
        RenderSettings.fogColor = new Color(0.72f, 0.8f, 0.88f);

        Camera cam = Camera.main;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 400f;
        ThirdPersonCamera tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();

        // ---- materials
        Material grass = Mat("Grass", C(0.33f, 0.55f, 0.25f), false, 0.05f);
        Material sand = Mat("Sand", C(0.85f, 0.78f, 0.58f));
        Material sea = Mat("Sea", C(0.08f, 0.30f, 0.45f), false, 0.92f, 0.2f);
        Material dirt = Mat("Dirt", C(0.52f, 0.40f, 0.28f));
        Material trunkM = Mat("Trunk", C(0.35f, 0.24f, 0.15f));
        Material leafM = Mat("Leaf", C(0.18f, 0.42f, 0.20f));
        Material leaf2M = Mat("Leaf2", C(0.25f, 0.50f, 0.22f));
        Material rockM = Mat("Rock", C(0.50f, 0.50f, 0.52f));
        Material wallM = Mat("Wall", C(0.86f, 0.80f, 0.68f));
        Material roofM = Mat("Roof", C(0.60f, 0.25f, 0.18f));
        Material doorM = Mat("Door", C(0.30f, 0.18f, 0.10f));
        Material windowM = Mat("Window", C(1f, 0.85f, 0.45f), true);
        Material stoneM = Mat("Stone", C(0.45f, 0.44f, 0.44f));
        Material white = Mat("Whitewash", C(0.93f, 0.93f, 0.90f));
        Material red = Mat("RedPaint", C(0.72f, 0.16f, 0.14f));
        Material lampOff = Mat("LampOff", C(0.55f, 0.6f, 0.65f), false, 0.8f);
        Material lampOn = Mat("LampOn", C(1f, 0.9f, 0.5f), true);
        Material flameM = Mat("Flame", C(1f, 0.55f, 0.15f), true);
        Material woodM = Mat("Wood", C(0.45f, 0.30f, 0.18f));

        Transform world = new GameObject("World").transform;

        // ---- sea + island
        Deco(PrimitiveType.Plane, "Sea", new Vector3(0, -0.6f, 0), new Vector3(200, 1, 200), sea, world);
        Solid(PrimitiveType.Cylinder, "Beach", new Vector3(0, -1.04f, 0), new Vector3(92, 1, 92), sand, world);
        Solid(PrimitiveType.Cylinder, "Island", new Vector3(0, -1.0f, 0), new Vector3(76, 1, 76), grass, world);
        Deco(PrimitiveType.Cube, "Path", new Vector3(0, 0.02f, 5), new Vector3(2.4f, 0.04f, 38), dirt, world);

        // ---- places (x, z) used to keep trees away: Vector3(x, z, radius)
        List<Vector3> keep = new List<Vector3>();
        Vector3 spawn = new Vector3(0, 0.05f, -14);
        keep.Add(new Vector3(0, -14, 5));
        keep.Add(new Vector3(0, 26, 9));      // lighthouse
        keep.Add(new Vector3(3, 8, 4));       // campfire

        // ---- village
        Transform village = new GameObject("Village").transform;
        village.SetParent(world, false);
        MakeHouse(village, new Vector3(-10, 0, 2), 90, wallM, roofM, doorM, windowM, stoneM);
        MakeHouse(village, new Vector3(10, 0, 0), -90, wallM, roofM, doorM, windowM, stoneM);
        MakeHouse(village, new Vector3(-9, 0, -8), 90, wallM, roofM, doorM, windowM, stoneM);
        MakeHouse(village, new Vector3(9, 0, -9), -90, wallM, roofM, doorM, windowM, stoneM);
        MakeHouse(village, new Vector3(-12, 0, 14), 70, wallM, roofM, doorM, windowM, stoneM);
        keep.Add(new Vector3(-10, 2, 6));
        keep.Add(new Vector3(10, 0, 6));
        keep.Add(new Vector3(-9, -8, 6));
        keep.Add(new Vector3(9, -9, 6));
        keep.Add(new Vector3(-12, 14, 6));

        // campfire
        Transform fire = new GameObject("Campfire").transform;
        fire.SetParent(world, false);
        fire.localPosition = new Vector3(3, 0, 8);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Solid(PrimitiveType.Sphere, "FireStone", new Vector3(Mathf.Cos(a) * 0.9f, 0.1f, Mathf.Sin(a) * 0.9f),
                  new Vector3(0.5f, 0.35f, 0.5f), stoneM, fire);
        }
        Part(PrimitiveType.Sphere, "Flame", new Vector3(0, 0.5f, 0), new Vector3(0.55f, 0.9f, 0.55f), flameM, fire);
        Light fireLight = AddLight(fire, "FireLight", LightType.Point, new Vector3(0, 1.0f, 0), new Color(1f, 0.6f, 0.25f), 2.2f, 10f, true);
        fireLight.gameObject.AddComponent<Flicker>();

        // ---- lighthouse
        Transform lh = new GameObject("Lighthouse").transform;
        lh.SetParent(world, false);
        lh.localPosition = new Vector3(0, 0, 26);
        Solid(PrimitiveType.Cylinder, "Base", new Vector3(0, 0.5f, 0), new Vector3(5.2f, 0.5f, 5.2f), stoneM, lh);
        Solid(PrimitiveType.Cylinder, "Tower", new Vector3(0, 6f, 0), new Vector3(4f, 6f, 4f), white, lh);
        Deco(PrimitiveType.Cylinder, "StripeLow", new Vector3(0, 3.2f, 0), new Vector3(4.08f, 0.8f, 4.08f), red, lh);
        Deco(PrimitiveType.Cylinder, "StripeHigh", new Vector3(0, 7.6f, 0), new Vector3(4.08f, 0.8f, 4.08f), red, lh);
        Deco(PrimitiveType.Cylinder, "Gallery", new Vector3(0, 12.15f, 0), new Vector3(5f, 0.15f, 5f), stoneM, lh);
        GameObject lamp = Deco(PrimitiveType.Cylinder, "LampRoom", new Vector3(0, 13.4f, 0), new Vector3(2.6f, 1.1f, 2.6f), lampOff, lh);
        Deco(PrimitiveType.Sphere, "Cap", new Vector3(0, 14.6f, 0), new Vector3(3.4f, 1.4f, 3.4f), red, lh);
        Deco(PrimitiveType.Cube, "Door", new Vector3(0, 1.1f, -2.0f), new Vector3(1.3f, 2.2f, 0.15f), doorM, lh);

        GameObject rotor = new GameObject("Rotor");
        rotor.transform.SetParent(lh, false);
        rotor.transform.localPosition = new Vector3(0, 13.4f, 0);
        Light spotA = AddLight(rotor.transform, "BeamA", LightType.Spot, Vector3.zero, new Color(1f, 0.92f, 0.65f), 8f, 120f, false);
        spotA.spotAngle = 38f;
        spotA.transform.localRotation = Quaternion.Euler(6, 0, 0);
        Light spotB = AddLight(rotor.transform, "BeamB", LightType.Spot, Vector3.zero, new Color(1f, 0.92f, 0.65f), 8f, 120f, false);
        spotB.spotAngle = 38f;
        spotB.transform.localRotation = Quaternion.Euler(6, 180, 0);
        Light glow = AddLight(lh, "LampGlow", LightType.Point, new Vector3(0, 13.4f, 0), new Color(1f, 0.85f, 0.5f), 6f, 40f, false);

        Lighthouse lighthouse = lh.gameObject.AddComponent<Lighthouse>();
        lighthouse.prompt = "Examine";
        lighthouse.range = 7f;
        lighthouse.lights = new[] { spotA, spotB, glow };
        lighthouse.lampRenderer = lamp.GetComponent<Renderer>();
        lighthouse.lampOn = lampOn;
        lighthouse.rotor = rotor.transform;

        // ---- fishing boat on the east shore
        Solid(PrimitiveType.Cube, "BoatHull", new Vector3(31, 0.35f, 3), new Vector3(1.8f, 0.7f, 4.6f), woodM, world, new Vector3(0, 15, 0));
        Deco(PrimitiveType.Cube, "BoatMast", new Vector3(31, 1.8f, 3), new Vector3(0.12f, 2.4f, 0.12f), woodM, world);
        keep.Add(new Vector3(31, 3, 4));

        // ---- the three lantern parts
        Transform items = new GameObject("Items").transform;
        items.SetParent(world, false);

        Material lensM = Mat("LensGlass", C(0.35f, 0.9f, 1f), true, 0.9f);
        Material crystalM = Mat("FlameCrystal", C(1f, 0.35f, 0.15f), true, 0.7f);
        Material mirrorM = Mat("Mirror", C(0.85f, 0.9f, 1f), true, 1f, 0.9f);

        Vector3 lensPos = new Vector3(27, 0, 10);
        Vector3 crystalPos = new Vector3(-26, 0, 17);
        Vector3 mirrorPos = new Vector3(9, 0, -33);

        MakeCollectible(items, "Lens", "Lens", lensPos, PrimitiveType.Sphere,
            new Vector3(0.75f, 0.75f, 0.3f), Vector3.zero, lensM, new Color(0.4f, 0.9f, 1f),
            new[] { "A great glass lens, wedged between the boulders. Still faintly warm.", "One of three." });
        MakeCollectible(items, "FlameCrystal", "Flame Crystal", crystalPos, PrimitiveType.Cube,
            new Vector3(0.55f, 0.55f, 0.55f), new Vector3(45, 45, 0), crystalM, new Color(1f, 0.4f, 0.2f),
            new[] { "The flame crystal. It hums in my hand, just like the girl said.", "Two to go." });
        MakeCollectible(items, "Mirror", "Mirror", mirrorPos, PrimitiveType.Cube,
            new Vector3(0.9f, 1.2f, 0.08f), Vector3.zero, mirrorM, new Color(0.7f, 0.85f, 1f),
            new[] { "The mirror, half buried in the sand. The tide must have dragged it all the way here." });
        keep.Add(new Vector3(lensPos.x, lensPos.z, 6));
        keep.Add(new Vector3(crystalPos.x, crystalPos.z, 3.5f));
        keep.Add(new Vector3(mirrorPos.x, mirrorPos.z, 5));

        // boulders around the lens
        for (int i = 0; i < 5; i++)
        {
            float a = 0.4f + i * 1.1f;
            Solid(PrimitiveType.Sphere, "Boulder",
                  new Vector3(lensPos.x + Mathf.Cos(a) * 2.7f, 0.4f, lensPos.z + Mathf.Sin(a) * 2.7f),
                  new Vector3(2.2f, 1.7f, 2.0f), rockM, world);
        }

        // ---- characters
        Transform chars = new GameObject("Characters").transform;
        chars.SetParent(world, false);

        Npc sael = MakeNpc(chars, "Sael", new Vector3(-3, 0, 10), 120, 1f, C(0.25f, 0.3f, 0.55f), C(0.85f, 0.85f, 0.85f), true);
        sael.givesQuest = true;
        sael.arriveLines = new[]
        {
            "A stranger washed ashore... and on the very night the sea went dark. What do they call you?",
            "Rian|Rian. What do you mean, the sea went dark?",
            "The Mira Light. For three hundred years it has guided ships past the Teeth, the rocks beyond our shore. Three nights ago its lens, its flame crystal and its mirror vanished from the lamp room.",
            "Rian|And you want me to find them.",
            "I want someone with young legs and stubborn feet. Tobb saw something glowing on the east shore. The child Nim plays in the old forest to the west. And the south beach keeps whatever the tide brings in.",
            "Bring the parts to the lighthouse itself, to the north. Go, Rian. Quickly."
        };
        sael.findLines = new[]
        {
            "The lighthouse stands north, past the village. The lens, the flame crystal, the mirror: find all three.",
            "Tobb is on the east shore, Nim by the western forest, and the south beach keeps its own secrets."
        };
        sael.relightLines = new[]
        {
            "You have them all? Then go! Climb to the lamp room and give the sea its light back."
        };
        sael.doneLines = new[]
        {
            "Look at that... the sea is alive with light again. Thank you, Rian. Mira will remember your name."
        };

        Npc tobb = MakeNpc(chars, "Tobb", new Vector3(21, 0, 5), -70, 1f, C(0.2f, 0.4f, 0.35f), C(0.3f, 0.2f, 0.12f), false);
        tobb.arriveLines = new[]
        {
            "Mind the nets, stranger. Sael will be wanting a word with you. She always does."
        };
        tobb.findLines = new[]
        {
            "I saw a strange glow in the rocks just east of here, past the big boulders. Bright as a star.",
            "The lens, if I had to bet. Go and see for yourself."
        };
        tobb.relightLines = new[]
        {
            "You found them all? Then what are you doing talking to me? Go!"
        };
        tobb.doneLines = new[]
        {
            "First night in years the fish are rising under that beam. Good work."
        };

        Npc nim = MakeNpc(chars, "Nim", new Vector3(-15, 0, 9), 80, 0.7f, C(0.75f, 0.35f, 0.3f), C(0.15f, 0.1f, 0.05f), false);
        nim.arriveLines = new[]
        {
            "You're new! Are you going to fix the light? Everyone says somebody will."
        };
        nim.findLines = new[]
        {
            "I saw a red stone shining deep in the forest, west of here. I was too scared to touch it.",
            "It hums. Like it's alive."
        };
        nim.relightLines = new[]
        {
            "You have all the pieces? Hurry! I want to see it shine!"
        };
        nim.doneLines = new[]
        {
            "It's so bright! I can see it from my bed. Thank you!"
        };
        keep.Add(new Vector3(-3, 10, 4));
        keep.Add(new Vector3(21, 5, 4));
        keep.Add(new Vector3(-15, 9, 4));

        // ---- trees and rocks
        Transform nature = new GameObject("Nature").transform;
        nature.SetParent(world, false);
        System.Random rng = new System.Random(11);
        int placed = 0, tries = 0;
        while (placed < 85 && tries < 2000)
        {
            tries++;
            double ang = rng.NextDouble() * System.Math.PI * 2.0;
            double rad = 6.0 + rng.NextDouble() * 30.0;
            float x = (float)(System.Math.Cos(ang) * rad);
            float z = (float)(System.Math.Sin(ang) * rad);
            if (Blocked(keep, x, z)) continue;
            float s = 0.8f + (float)rng.NextDouble() * 0.7f;
            MakeTree(nature, new Vector3(x, 0, z), s, trunkM, leafM, leaf2M);
            placed++;
        }
        // denser forest around the crystal
        for (int i = 0; i < 16; i++)
        {
            double ang = rng.NextDouble() * System.Math.PI * 2.0;
            double rad = 5.0 + rng.NextDouble() * 7.0;
            float x = crystalPos.x + (float)(System.Math.Cos(ang) * rad);
            float z = crystalPos.z + (float)(System.Math.Sin(ang) * rad);
            if (new Vector2(x, z).magnitude > 36f || Blocked(keep, x, z)) continue;
            MakeTree(nature, new Vector3(x, 0, z), 0.9f + (float)rng.NextDouble() * 0.7f, trunkM, leafM, leaf2M);
        }
        // scattered rocks
        for (int i = 0; i < 30; i++)
        {
            double ang = rng.NextDouble() * System.Math.PI * 2.0;
            double rad = 4.0 + rng.NextDouble() * 33.0;
            float x = (float)(System.Math.Cos(ang) * rad);
            float z = (float)(System.Math.Sin(ang) * rad);
            if (Blocked(keep, x, z)) continue;
            float s = 0.5f + (float)rng.NextDouble() * 1.0f;
            Solid(PrimitiveType.Sphere, "Rock", new Vector3(x, s * 0.15f, z), new Vector3(s * 1.3f, s * 0.8f, s), rockM, nature);
        }

        // ---- player
        GameObject player = new GameObject("Player");
        player.transform.position = spawn;
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45f; cc.stepOffset = 0.4f;
        Material coat = Mat("PlayerCoat", C(0.7f, 0.5f, 0.2f));
        Material pack = Mat("PlayerPack", C(0.35f, 0.22f, 0.12f));
        Material skinM = Mat("Skin", C(0.87f, 0.68f, 0.55f));
        Material hairD = Mat("Hair_Rian", C(0.12f, 0.08f, 0.05f));
        Material darkM = Mat("Dark", C(0.05f, 0.05f, 0.05f));
        Part(PrimitiveType.Capsule, "Body", new Vector3(0, 0.9f, 0), new Vector3(0.7f, 0.8f, 0.7f), coat, player.transform);
        Part(PrimitiveType.Sphere, "Head", new Vector3(0, 1.75f, 0), new Vector3(0.38f, 0.38f, 0.38f), skinM, player.transform);
        Part(PrimitiveType.Sphere, "Hair", new Vector3(0, 1.86f, -0.03f), new Vector3(0.42f, 0.28f, 0.42f), hairD, player.transform);
        Part(PrimitiveType.Sphere, "EyeL", new Vector3(-0.07f, 1.78f, 0.17f), new Vector3(0.06f, 0.06f, 0.06f), darkM, player.transform);
        Part(PrimitiveType.Sphere, "EyeR", new Vector3(0.07f, 1.78f, 0.17f), new Vector3(0.06f, 0.06f, 0.06f), darkM, player.transform);
        Part(PrimitiveType.Cube, "Backpack", new Vector3(0, 1.0f, -0.32f), new Vector3(0.45f, 0.55f, 0.22f), pack, player.transform);
        PlayerController pc = player.AddComponent<PlayerController>();
        tpc.target = player.transform;
        tpc.yaw = 0f;
        pc.cam = cam.transform;

        // ---- UI
        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform ct = canvasGO.transform;
        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // look area (right side), joystick, interact button
        Image lookImg = MakeImage(ct, "LookArea", new Color(0, 0, 0, 0));
        Stretch(lookImg.transform, new Vector2(0.35f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        TouchLook look = lookImg.gameObject.AddComponent<TouchLook>();
        look.cam = tpc;

        Image jbg = MakeImage(ct, "Joystick", new Color(1, 1, 1, 0.15f), knob);
        Fixed(jbg.transform, new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(260, 260), new Vector2(320, 320));
        Image jh = MakeImage(jbg.transform, "Handle", new Color(1, 1, 1, 0.45f), knob, false, false);
        Fixed(jh.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140));
        VirtualJoystick joy = jbg.gameObject.AddComponent<VirtualJoystick>();
        joy.background = (RectTransform)jbg.transform;
        joy.handle = (RectTransform)jh.transform;
        joy.radius = 110f;
        pc.joystick = joy;

        Image ib = MakeImage(ct, "InteractButton", new Color(0.96f, 0.76f, 0.25f, 0.95f), round, true);
        Fixed(ib.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 420), new Vector2(300, 120));
        Button ibBtn = ib.gameObject.AddComponent<Button>();
        ibBtn.targetGraphic = ib;
        Text ibLabel = MakeText(ib.transform, "Label", "Talk", 46, TextAnchor.MiddleCenter, new Color(0.15f, 0.1f, 0.05f), FontStyle.Bold);
        Stretch(ibLabel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ib.gameObject.SetActive(false);
        pc.interactButton = ibBtn;
        pc.interactLabel = ibLabel;

        // HUD
        Text objective = MakeText(ct, "Objective", "", 36, TextAnchor.UpperLeft, Color.white, FontStyle.Bold);
        Fixed(objective.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(1000, 60));
        Text partsT = MakeText(ct, "Parts", "", 36, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.4f), FontStyle.Bold);
        Fixed(partsT.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(500, 60));

        // dialogue
        Image dp = MakeImage(ct, "DialoguePanel", new Color(0.05f, 0.07f, 0.12f, 0.9f), round, true);
        Stretch(dp.transform, new Vector2(0.08f, 0), new Vector2(0.92f, 0), new Vector2(0, 40), new Vector2(0, 330));
        Button dBtn = dp.gameObject.AddComponent<Button>();
        dBtn.transition = Selectable.Transition.None;
        dBtn.targetGraphic = dp;
        Text speaker = MakeText(dp.transform, "Speaker", "", 44, TextAnchor.UpperLeft, new Color(1f, 0.82f, 0.35f), FontStyle.Bold);
        Stretch(speaker.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(45, -75), new Vector2(-45, -18));
        Text body = MakeText(dp.transform, "Body", "", 40, TextAnchor.UpperLeft, Color.white);
        Stretch(body.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(45, 50), new Vector2(-45, -85));
        Text hint = MakeText(dp.transform, "Hint", "tap to continue", 28, TextAnchor.LowerRight, new Color(0.7f, 0.75f, 0.85f));
        Stretch(hint.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(45, 10), new Vector2(-45, 48));
        dp.gameObject.SetActive(false);

        // fade + title card
        Image fade = MakeImage(ct, "Fade", Color.black, null, false, false);
        Stretch(fade.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text title = MakeText(ct, "Title", "", 84, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(title.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 400));

        // end panel
        Image endImg = MakeImage(ct, "EndPanel", new Color(0, 0, 0, 0.78f));
        Stretch(endImg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text endTitle = MakeText(endImg.transform, "EndTitle", "THE LAST LIGHT", 96, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), FontStyle.Bold);
        Fixed(endTitle.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1600, 200));
        Text endSub = MakeText(endImg.transform, "EndSub", "Chapter 1 complete\nThank you for playing.", 46, TextAnchor.MiddleCenter, Color.white);
        Fixed(endSub.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 200));
        Image rb = MakeImage(endImg.transform, "RestartButton", new Color(0.96f, 0.76f, 0.25f, 1f), round, true);
        Fixed(rb.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(420, 120));
        Button rbBtn = rb.gameObject.AddComponent<Button>();
        rbBtn.targetGraphic = rb;
        Text rbLabel = MakeText(rb.transform, "Label", "Play again", 44, TextAnchor.MiddleCenter, new Color(0.15f, 0.1f, 0.05f), FontStyle.Bold);
        Stretch(rbLabel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        endImg.gameObject.SetActive(false);

        // ---- game object that holds the logic
        GameObject game = new GameObject("Game");
        DialogueUI dui = game.AddComponent<DialogueUI>();
        dui.panel = dp.gameObject;
        dui.speakerText = speaker;
        dui.bodyText = body;
        dui.nextButton = dBtn;

        GameManager gm = game.AddComponent<GameManager>();
        gm.player = player.transform;
        gm.dialogue = dui;
        gm.objectiveText = objective;
        gm.partsText = partsT;
        gm.titleText = title;
        gm.fade = fade;
        gm.endPanel = endImg.gameObject;
        gm.restartButton = rbBtn;

        // ---- save
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Main scene built: " + ScenePath);
    }

    // keep list entries are Vector3(x, z, radius); the path down the middle is also kept clear
    static bool Blocked(List<Vector3> keep, float x, float z)
    {
        if (Mathf.Abs(x) < 3f && z > -17f && z < 24f) return true;
        foreach (Vector3 k in keep)
        {
            float dx = x - k.x, dz = z - k.y;
            if (dx * dx + dz * dz < k.z * k.z) return true;
        }
        return false;
    }
}
