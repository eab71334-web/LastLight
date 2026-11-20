using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the whole Neon Arena scene from code (arena, units, HUD).
/// Run it from the menu "Neon / Rebuild Main Scene" or let BuildScript call it during CI builds.
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


    // ------------------------------------------------------------------ arena
    const float Half = 30f;
    static readonly Color Cyan = new Color(0.1f, 0.85f, 1f);
    static readonly Color Magenta = new Color(1f, 0.15f, 0.6f);

    static Combatant MakeUnit(string name, int team, Vector3 pos, float yaw, bool isPlayer,
                              Material teamMat, Material visor, List<Combatant> all)
    {
        GameObject root = new GameObject(name);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.05f;

        Combatant c = root.AddComponent<Combatant>();
        c.team = team; c.displayName = name; c.isPlayer = isPlayer; c.maxHealth = 100f; c.health = 100f;
        GameObject eye = new GameObject("Eye");
        eye.transform.SetParent(root.transform, false);
        eye.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        c.eye = eye.transform;

        if (!isPlayer)
        {
            Part(PrimitiveType.Capsule, "Body", new Vector3(0, 0.9f, 0), new Vector3(0.8f, 0.85f, 0.8f), teamMat, root.transform);
            Part(PrimitiveType.Sphere, "Head", new Vector3(0, 1.62f, 0), new Vector3(0.42f, 0.42f, 0.42f), teamMat, root.transform);
            Part(PrimitiveType.Cube, "Visor", new Vector3(0, 1.64f, 0.17f), new Vector3(0.34f, 0.1f, 0.14f), visor, root.transform);
            Part(PrimitiveType.Cube, "Gun", new Vector3(0.3f, 1.05f, 0.35f), new Vector3(0.1f, 0.12f, 0.6f), visor, root.transform);
            BotAI ai = root.AddComponent<BotAI>();
            ai.self = c; ai.eye = c.eye;
        }
        all.Add(c);
        return c;
    }

    [MenuItem("Neon/Rebuild Main Scene")]
    public static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- atmosphere
        Light sun = Object.FindObjectOfType<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        sun.color = new Color(0.6f, 0.55f, 1f);
        sun.intensity = 0.5f;
        sun.shadows = LightShadows.None;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.18f, 0.38f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.014f;
        RenderSettings.fogColor = new Color(0.06f, 0.03f, 0.14f);

        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.03f, 0.14f);
        cam.fieldOfView = 78f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;

        // ---- materials
        Material floor = Mat("Floor", new Color(0.06f, 0.06f, 0.11f), false, 0.3f);
        Material wall = Mat("Wall", new Color(0.07f, 0.09f, 0.2f), false, 0.2f);
        Material cover = Mat("Cover", new Color(0.09f, 0.11f, 0.25f), false, 0.25f);
        Material nCyan = Mat("NeonCyan", Cyan, true);
        Material nMag = Mat("NeonMagenta", Magenta, true);
        Material nPurple = Mat("NeonPurple", new Color(0.55f, 0.2f, 1f), true);
        Material teamBlue = Mat("TeamBlue", new Color(0.05f, 0.55f, 0.75f), true);
        Material teamRed = Mat("TeamRed", new Color(0.8f, 0.1f, 0.35f), true);
        Material visorMat = Mat("Visor", new Color(1f, 1f, 1f), true);
        Material gunDark = Mat("GunDark", new Color(0.08f, 0.08f, 0.12f), false, 0.4f, 0.3f);

        // ---- arena
        Transform arena = new GameObject("Arena").transform;
        Solid(PrimitiveType.Cube, "Floor", new Vector3(0, -0.5f, 0), new Vector3(Half * 2, 1, Half * 2), floor, arena);
        for (int i = -5; i <= 5; i++)
        {
            Deco(PrimitiveType.Cube, "GridZ", new Vector3(i * 6f, 0.011f, 0), new Vector3(0.1f, 0.02f, Half * 2), nPurple, arena);
            Deco(PrimitiveType.Cube, "GridX", new Vector3(0, 0.012f, i * 6f), new Vector3(Half * 2, 0.02f, 0.1f), nPurple, arena);
        }
        float L = Half * 2 + 2;
        Solid(PrimitiveType.Cube, "WallN", new Vector3(0, 4, Half + 0.5f), new Vector3(L, 8, 1), wall, arena);
        Solid(PrimitiveType.Cube, "WallS", new Vector3(0, 4, -Half - 0.5f), new Vector3(L, 8, 1), wall, arena);
        Solid(PrimitiveType.Cube, "WallE", new Vector3(Half + 0.5f, 4, 0), new Vector3(1, 8, L), wall, arena);
        Solid(PrimitiveType.Cube, "WallW", new Vector3(-Half - 0.5f, 4, 0), new Vector3(1, 8, L), wall, arena);
        float[] ys = { 0.1f, 7.85f };
        foreach (float y in ys)
        {
            Deco(PrimitiveType.Cube, "StripN", new Vector3(0, y, Half - 0.02f), new Vector3(Half * 2, 0.15f, 0.1f), nCyan, arena);
            Deco(PrimitiveType.Cube, "StripS", new Vector3(0, y, -Half + 0.02f), new Vector3(Half * 2, 0.15f, 0.1f), nCyan, arena);
            Deco(PrimitiveType.Cube, "StripE", new Vector3(Half - 0.02f, y, 0), new Vector3(0.1f, 0.15f, Half * 2), nMag, arena);
            Deco(PrimitiveType.Cube, "StripW", new Vector3(-Half + 0.02f, y, 0), new Vector3(0.1f, 0.15f, Half * 2), nMag, arena);
        }

        // cover: x, z, width, depth, height
        float[,] blocks =
        {
            { 0, 0, 6, 6, 0.8f },
            { 9, 9, 2.5f, 2.5f, 5 }, { -9, 9, 2.5f, 2.5f, 5 }, { 9, -9, 2.5f, 2.5f, 5 }, { -9, -9, 2.5f, 2.5f, 5 },
            { 0, 14, 10, 1.2f, 3 }, { 0, -14, 10, 1.2f, 3 },
            { 18, 0, 1.2f, 10, 3 }, { -18, 0, 1.2f, 10, 3 },
            { 22, 20, 4, 4, 2 }, { -22, -20, 4, 4, 2 }, { 22, -20, 4, 4, 2 }, { -22, 20, 4, 4, 2 }
        };
        for (int i = 0; i < blocks.GetLength(0); i++)
        {
            float x = blocks[i, 0], z = blocks[i, 1], w = blocks[i, 2], d = blocks[i, 3], h = blocks[i, 4];
            Solid(PrimitiveType.Cube, "Cover" + i, new Vector3(x, h / 2f, z), new Vector3(w, h, d), cover, arena);
            Deco(PrimitiveType.Cube, "Cap" + i, new Vector3(x, h + 0.03f, z), new Vector3(w + 0.1f, 0.06f, d + 0.1f),
                 (i % 2 == 0) ? nCyan : nMag, arena);
        }

        AddLight(arena, "LightA", LightType.Point, new Vector3(-24, 5, -24), Cyan, 2.5f, 38f, true);
        AddLight(arena, "LightB", LightType.Point, new Vector3(24, 5, 24), Magenta, 2.5f, 38f, true);
        AddLight(arena, "LightC", LightType.Point, new Vector3(24, 5, -24), new Color(0.6f, 0.3f, 1f), 2f, 38f, true);
        AddLight(arena, "LightD", LightType.Point, new Vector3(-24, 5, 24), new Color(0.6f, 0.3f, 1f), 2f, 38f, true);
        AddLight(arena, "LightCenter", LightType.Point, new Vector3(0, 6, 0), Color.white, 1.2f, 30f, true);

        // ---- spawns
        Transform[] sp0 = new Transform[3];
        Transform[] sp1 = new Transform[3];
        float[] sx = { -8f, 0f, 8f };
        for (int i = 0; i < 3; i++)
        {
            GameObject a = new GameObject("SpawnBlue" + i);
            a.transform.SetParent(arena, false);
            a.transform.position = new Vector3(sx[i], 0.05f, -25f);
            a.transform.rotation = Quaternion.Euler(0, 0, 0);
            sp0[i] = a.transform;
            GameObject b = new GameObject("SpawnRed" + i);
            b.transform.SetParent(arena, false);
            b.transform.position = new Vector3(sx[i], 0.05f, 25f);
            b.transform.rotation = Quaternion.Euler(0, 180, 0);
            sp1[i] = b.transform;
        }

        // ---- units
        List<Combatant> all = new List<Combatant>();
        Combatant pl = MakeUnit("You", 0, sp0[1].position, 0f, true, teamBlue, visorMat, all);
        MakeUnit("Rex", 0, sp0[0].position, 0f, false, teamBlue, visorMat, all);
        MakeUnit("Nova", 0, sp0[2].position, 0f, false, teamBlue, visorMat, all);
        MakeUnit("Vex", 1, sp1[0].position, 180f, false, teamRed, visorMat, all);
        MakeUnit("Kane", 1, sp1[1].position, 180f, false, teamRed, visorMat, all);
        MakeUnit("Ghost", 1, sp1[2].position, 180f, false, teamRed, visorMat, all);

        // ---- player rig: camera + weapon
        cam.transform.SetParent(pl.eye, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        GameObject gun = new GameObject("Weapon");
        gun.transform.SetParent(cam.transform, false);
        gun.transform.localPosition = new Vector3(0.24f, -0.22f, 0.5f);
        gun.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
        Part(PrimitiveType.Cube, "GunBody", new Vector3(0, 0, 0), new Vector3(0.08f, 0.1f, 0.5f), gunDark, gun.transform);
        Part(PrimitiveType.Cube, "GunGlow", new Vector3(0, 0.055f, 0.02f), new Vector3(0.05f, 0.015f, 0.42f), nCyan, gun.transform);
        Part(PrimitiveType.Cube, "GunGrip", new Vector3(0, -0.1f, -0.12f), new Vector3(0.07f, 0.14f, 0.09f), gunDark, gun.transform, new Vector3(15, 0, 0));
        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(gun.transform, false);
        muzzle.transform.localPosition = new Vector3(0, 0.02f, 0.32f);
        Light flash = AddLight(muzzle.transform, "Flash", LightType.Point, Vector3.zero, new Color(1f, 0.9f, 0.5f), 3f, 8f, false);

        FpsPlayer fps = pl.gameObject.AddComponent<FpsPlayer>();
        WeaponController wc = pl.gameObject.AddComponent<WeaponController>();
        wc.cam = cam.transform; wc.owner = pl; wc.player = fps; wc.muzzle = muzzle.transform;
        wc.flash = flash; wc.model = gun.transform;
        fps.cam = cam.transform; fps.weapon = wc;

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

        // aim area (behind everything else)
        Image lookImg = MakeImage(ct, "LookArea", new Color(0, 0, 0, 0));
        Stretch(lookImg.transform, new Vector2(0.3f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        TouchLook look = lookImg.gameObject.AddComponent<TouchLook>();
        look.player = fps;

        Image hurt = MakeImage(ct, "Hurt", new Color(1, 0, 0, 0), null, false, false);
        Stretch(hurt.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject safe = new GameObject("Safe", typeof(RectTransform));
        safe.transform.SetParent(ct, false);
        Stretch(safe.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safe.AddComponent<SafeArea>();
        Transform st = safe.transform;

        // crosshair
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Color cross = new Color(1, 1, 1, 0.9f);
        Fixed(MakeImage(st, "CrossL", cross, null, false, false).transform, mid, mid, new Vector2(-18, 0), new Vector2(16, 3));
        Fixed(MakeImage(st, "CrossR", cross, null, false, false).transform, mid, mid, new Vector2(18, 0), new Vector2(16, 3));
        Fixed(MakeImage(st, "CrossU", cross, null, false, false).transform, mid, mid, new Vector2(0, 18), new Vector2(3, 16));
        Fixed(MakeImage(st, "CrossD", cross, null, false, false).transform, mid, mid, new Vector2(0, -18), new Vector2(3, 16));

        // hit marker
        GameObject hm = new GameObject("HitMarker", typeof(RectTransform));
        hm.transform.SetParent(st, false);
        Fixed(hm.transform, mid, mid, Vector2.zero, new Vector2(60, 60));
        Image h1 = MakeImage(hm.transform, "A", new Color(1, 0.3f, 0.3f, 1), null, false, false);
        Fixed(h1.transform, mid, mid, Vector2.zero, new Vector2(46, 5));
        h1.transform.localRotation = Quaternion.Euler(0, 0, 45);
        Image h2 = MakeImage(hm.transform, "B", new Color(1, 0.3f, 0.3f, 1), null, false, false);
        Fixed(h2.transform, mid, mid, Vector2.zero, new Vector2(46, 5));
        h2.transform.localRotation = Quaternion.Euler(0, 0, -45);

        // joystick
        Image jbg = MakeImage(st, "Joystick", new Color(1, 1, 1, 0.15f), knob);
        Fixed(jbg.transform, Vector2.zero, mid, new Vector2(250, 270), new Vector2(320, 320));
        Image jh = MakeImage(jbg.transform, "Handle", new Color(1, 1, 1, 0.45f), knob, false, false);
        Fixed(jh.transform, mid, mid, Vector2.zero, new Vector2(140, 140));
        VirtualJoystick joy = jbg.gameObject.AddComponent<VirtualJoystick>();
        joy.background = (RectTransform)jbg.transform;
        joy.handle = (RectTransform)jh.transform;
        joy.radius = 110f;
        fps.joystick = joy;

        // buttons (right side)
        Vector2 br = new Vector2(1, 0);
        Image fire = MakeImage(st, "FireButton", new Color(1f, 0.2f, 0.4f, 0.55f), knob);
        Fixed(fire.transform, br, mid, new Vector2(-250, 290), new Vector2(250, 250));
        fps.fireButton = fire.gameObject.AddComponent<HoldButton>();
        Text fl = MakeText(fire.transform, "Label", "FIRE", 44, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(fl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image jump = MakeImage(st, "JumpButton", new Color(0.1f, 0.8f, 1f, 0.45f), knob);
        Fixed(jump.transform, br, mid, new Vector2(-520, 190), new Vector2(170, 170));
        fps.jumpButton = jump.gameObject.AddComponent<Button>();
        fps.jumpButton.targetGraphic = jump;
        Text jl = MakeText(jump.transform, "Label", "JUMP", 32, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(jl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image rel = MakeImage(st, "ReloadButton", new Color(1f, 0.85f, 0.2f, 0.45f), knob);
        Fixed(rel.transform, br, mid, new Vector2(-380, 330), new Vector2(130, 130));
        fps.reloadButton = rel.gameObject.AddComponent<Button>();
        fps.reloadButton.targetGraphic = rel;
        Text rl = MakeText(rel.transform, "Label", "RELOAD", 26, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(rl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // HUD text
        Text ammo = MakeText(st, "Ammo", "30 / 30", 60, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);
        Fixed(ammo.transform, br, br, new Vector2(-50, 30), new Vector2(480, 100));

        Image hbg = MakeImage(st, "HealthBack", new Color(0, 0, 0, 0.55f), round, true, false);
        Fixed(hbg.transform, Vector2.zero, Vector2.zero, new Vector2(60, 40), new Vector2(440, 44));
        Image hfill = MakeImage(hbg.transform, "Fill", new Color(0.2f, 1f, 0.6f), round, false, false);
        hfill.type = Image.Type.Filled;
        hfill.fillMethod = Image.FillMethod.Horizontal;
        hfill.fillOrigin = 0;
        Stretch(hfill.transform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
        Text htext = MakeText(hbg.transform, "Text", "100", 30, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(htext.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Vector2 tc = new Vector2(0.5f, 1f);
        Text timer = MakeText(st, "Timer", "3:00", 60, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(timer.transform, tc, tc, new Vector2(0, -20), new Vector2(260, 80));
        Text sBlue = MakeText(st, "ScoreBlue", "0", 56, TextAnchor.MiddleCenter, new Color(0.3f, 0.9f, 1f), FontStyle.Bold);
        Fixed(sBlue.transform, tc, tc, new Vector2(-210, -22), new Vector2(160, 76));
        Text sRed = MakeText(st, "ScoreRed", "0", 56, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.6f), FontStyle.Bold);
        Fixed(sRed.transform, tc, tc, new Vector2(210, -22), new Vector2(160, 76));

        Vector2 tr = new Vector2(1f, 1f);
        Text feed = MakeText(st, "KillFeed", "", 30, TextAnchor.UpperRight, Color.white, FontStyle.Bold);
        Fixed(feed.transform, tr, tr, new Vector2(-30, -24), new Vector2(560, 180));

        Text center = MakeText(st, "Center", "", 80, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(center.transform, mid, mid, new Vector2(0, 200), new Vector2(1300, 140));

        // end panel
        Image endImg = MakeImage(ct, "EndPanel", new Color(0, 0, 0, 0.78f));
        Stretch(endImg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text endTitle = MakeText(endImg.transform, "Title", "VICTORY", 120, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(endTitle.transform, mid, mid, new Vector2(0, 170), new Vector2(1300, 180));
        Text endScore = MakeText(endImg.transform, "Score", "0 - 0", 80, TextAnchor.MiddleCenter, new Color(0.8f, 0.9f, 1f), FontStyle.Bold);
        Fixed(endScore.transform, mid, mid, new Vector2(0, 30), new Vector2(900, 120));
        Image rb = MakeImage(endImg.transform, "Restart", new Color(0.1f, 0.85f, 1f, 0.95f), round, true);
        Fixed(rb.transform, mid, mid, new Vector2(0, -150), new Vector2(460, 130));
        Button rbBtn = rb.gameObject.AddComponent<Button>();
        rbBtn.targetGraphic = rb;
        Text rbl = MakeText(rb.transform, "Label", "Play again", 50, TextAnchor.MiddleCenter, new Color(0.05f, 0.05f, 0.15f), FontStyle.Bold);
        Stretch(rbl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ---- match logic
        GameObject game = new GameObject("Game");
        MatchManager mm = game.AddComponent<MatchManager>();
        mm.all = all;
        mm.spawns0 = sp0; mm.spawns1 = sp1;
        mm.player = pl; mm.weapon = wc; mm.fps = fps;
        mm.timerText = timer; mm.scoreBlue = sBlue; mm.scoreRed = sRed;
        mm.ammoText = ammo; mm.healthText = htext; mm.centerText = center; mm.feedText = feed;
        mm.endTitle = endTitle; mm.endScore = endScore;
        mm.healthFill = hfill; mm.hurtOverlay = hurt;
        mm.hitMarker = hm; mm.endPanel = endImg.gameObject; mm.restartButton = rbBtn;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Neon Arena scene built: " + ScenePath);
    }
}
