using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the whole Knockout Night scene from code (ring, boxers, HUD).
/// Run it from the menu "Neon / Rebuild Main Scene" or let BuildScript call it during CI builds.
/// </summary>
public static partial class SceneBuilder
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


    // ------------------------------------------------------------------ ring
    static Transform MakeGlove(Transform parent, string name, Vector3 localPos, Material glove, Material arm,
                               List<Renderer> armList, float armLen)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        Part(PrimitiveType.Sphere, "Fist", Vector3.zero, new Vector3(0.28f, 0.26f, 0.3f), glove, root.transform);
        GameObject a = Part(PrimitiveType.Cube, "Arm", new Vector3(0, 0, -armLen / 2f - 0.1f), new Vector3(0.12f, 0.12f, armLen), arm, root.transform);
        if (armList != null) armList.Add(a.GetComponent<Renderer>());
        return root.transform;
    }

    [MenuItem("Boxing/Rebuild Main Scene")]
    public static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- atmosphere
        Light sun = Object.FindObjectOfType<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(60, -20, 0);
        sun.color = new Color(0.7f, 0.65f, 1f);
        sun.intensity = 0.35f;
        sun.shadows = LightShadows.None;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.13f, 0.26f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.028f;
        RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.07f);

        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.02f, 0.07f);
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 120f;

        // ---- materials
        Material ground = Mat("Ground", new Color(0.05f, 0.05f, 0.09f), false, 0.1f);
        Material apron = Mat("Apron", new Color(0.12f, 0.1f, 0.2f), false, 0.2f);
        Material canvasM = Mat("RingCanvas", new Color(0.16f, 0.2f, 0.5f), false, 0.15f);
        Material postM = Mat("Post", new Color(0.7f, 0.08f, 0.12f), false, 0.4f);
        Material ropeC = Mat("RopeCyan", new Color(0.1f, 0.85f, 1f), true);
        Material ropeM = Mat("RopeMagenta", new Color(1f, 0.15f, 0.6f), true);
        Material ropeW = Mat("RopeWhite", new Color(1f, 1f, 1f), true);
        Material glove = Mat("GloveRed", new Color(0.85f, 0.06f, 0.1f), false, 0.5f);
        Material gloveBot = Mat("GloveDark", new Color(0.08f, 0.08f, 0.1f), false, 0.5f);
        Material skin = Mat("Skin", new Color(0.8f, 0.6f, 0.46f), false, 0.3f);
        Material shorts = Mat("Shorts", new Color(0.2f, 0.5f, 0.95f), false, 0.2f);
        Material bloodMat = Mat("Blood", new Color(0.4f, 0.02f, 0.05f), false, 0.7f);
        Material[] crowd =
        {
            Mat("Crowd1", new Color(0.12f, 0.1f, 0.2f)), Mat("Crowd2", new Color(0.2f, 0.1f, 0.15f)),
            Mat("Crowd3", new Color(0.08f, 0.14f, 0.22f)), Mat("Crowd4", new Color(0.18f, 0.16f, 0.1f)),
            Mat("CrowdGlow1", new Color(0.1f, 0.85f, 1f), true), Mat("CrowdGlow2", new Color(1f, 0.15f, 0.6f), true)
        };

        // ---- ring and arena
        Transform arena = new GameObject("Arena").transform;
        Solid(PrimitiveType.Cube, "Ground", new Vector3(0, -1.7f, 0), new Vector3(90, 1, 90), ground, arena);
        Solid(PrimitiveType.Cube, "Apron", new Vector3(0, -0.6f, 0), new Vector3(12, 1.2f, 12), apron, arena);
        Deco(PrimitiveType.Cube, "Canvas", new Vector3(0, 0.02f, 0), new Vector3(10.6f, 0.04f, 10.6f), canvasM, arena);
        BuildRingAndStage(arena, ropeC, ropeM, ropeW, postM);

        // lights
        AddLight(arena, "Spot1", LightType.Point, new Vector3(3, 6, 3), new Color(1f, 0.95f, 0.85f), 2.2f, 16f, true);
        AddLight(arena, "Spot2", LightType.Point, new Vector3(-3, 6, 3), new Color(1f, 0.95f, 0.85f), 2.2f, 16f, true);
        AddLight(arena, "Spot3", LightType.Point, new Vector3(3, 6, -3), new Color(1f, 0.95f, 0.85f), 2.2f, 16f, true);
        AddLight(arena, "Spot4", LightType.Point, new Vector3(-3, 6, -3), new Color(1f, 0.95f, 0.85f), 2.2f, 16f, true);
        AddLight(arena, "Rim1", LightType.Point, new Vector3(-12, 3, 0), new Color(0.1f, 0.85f, 1f), 2f, 24f, true);
        AddLight(arena, "Rim2", LightType.Point, new Vector3(12, 3, 0), new Color(1f, 0.15f, 0.6f), 2f, 24f, true);

        // ---- player rig
        GameObject playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(0, 0, -2.5f);
        Fighter pf = playerGO.AddComponent<Fighter>();
        pf.isPlayer = true; pf.displayName = "YOU";
        PlayerBoxer pb = playerGO.AddComponent<PlayerBoxer>();
        pb.fighter = pf; pb.cam = cam.transform;
        pf.gloveL = MakeGlove(cam.transform, "GloveL", new Vector3(-0.3f, -0.32f, 0.55f), glove, skin, null, 0.7f);
        pf.gloveR = MakeGlove(cam.transform, "GloveR", new Vector3(0.3f, -0.32f, 0.55f), glove, skin, null, 0.7f);

        // ---- opponent
        GameObject botGO = new GameObject("Opponent");
        botGO.transform.position = new Vector3(0, 0, 2.5f);
        botGO.transform.rotation = Quaternion.Euler(0, 180, 0);
        Fighter bf = botGO.AddComponent<Fighter>();
        bf.displayName = "ROOKIE SAM";
        BotBoxer bb = botGO.AddComponent<BotBoxer>();
        bb.fighter = bf;
        List<Renderer> skinR = new List<Renderer>();
        List<Renderer> shortR = new List<Renderer>();
        // simple primitive body, used only if the realistic model fails to load
        Transform bt = new GameObject("Fallback").transform;
        bt.SetParent(botGO.transform, false);
        skinR.Add(Part(PrimitiveType.Cube, "Torso", new Vector3(0, 1.3f, 0), new Vector3(0.64f, 0.7f, 0.34f), skin, bt).GetComponent<Renderer>());
        GameObject head = Part(PrimitiveType.Sphere, "Head", new Vector3(0, 1.82f, 0), new Vector3(0.3f, 0.34f, 0.32f), skin, bt);
        skinR.Add(head.GetComponent<Renderer>());
        shortR.Add(Part(PrimitiveType.Cube, "Shorts", new Vector3(0, 0.8f, 0), new Vector3(0.66f, 0.45f, 0.36f), shorts, bt).GetComponent<Renderer>());
        skinR.Add(Part(PrimitiveType.Cube, "LegL", new Vector3(-0.16f, 0.3f, 0), new Vector3(0.22f, 0.6f, 0.24f), skin, bt).GetComponent<Renderer>());
        skinR.Add(Part(PrimitiveType.Cube, "LegR", new Vector3(0.16f, 0.3f, 0), new Vector3(0.22f, 0.6f, 0.24f), skin, bt).GetComponent<Renderer>());
        bf.gloveL = MakeGlove(bt, "GloveL", new Vector3(-0.32f, 1.3f, 0.3f), gloveBot, skin, skinR, 0.4f);
        bf.gloveR = MakeGlove(bt, "GloveR", new Vector3(0.32f, 1.3f, 0.3f), gloveBot, skin, skinR, 0.4f);
        bf.head = head.transform;
        BoxerRig rig = botGO.AddComponent<BoxerRig>();
        rig.fallbackRoot = bt.gameObject;
        rig.skinMat = Mat("BoxerSkin", new Color(0.86f, 0.66f, 0.52f), false, 0.32f);
        rig.shortsMat = Mat("BoxerShorts", new Color(0.2f, 0.5f, 0.95f), false, 0.25f);
        rig.bandMat = Mat("BoxerBand", new Color(0.95f, 0.95f, 0.95f), false, 0.25f);
        rig.hairMat = Mat("BoxerHair", new Color(0.05f, 0.04f, 0.035f), false, 0.3f);
        rig.gloveMat = Mat("BoxerGlove", new Color(0.08f, 0.08f, 0.1f), false, 0.55f);
        rig.tapeMat = Mat("BoxerTape", new Color(0.92f, 0.92f, 0.88f), false, 0.2f);
        rig.eyeWhiteMat = Mat("BoxerEyeWhite", new Color(0.95f, 0.95f, 0.93f), false, 0.6f);
        rig.irisMat = Mat("BoxerIris", new Color(0.12f, 0.07f, 0.04f), false, 0.7f);

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

        Image hurt = MakeImage(ct, "Hurt", new Color(1, 0, 0, 0), null, false, false);
        Stretch(hurt.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject splatGO = new GameObject("Splats", typeof(RectTransform));
        splatGO.transform.SetParent(ct, false);
        Stretch(splatGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject safe = new GameObject("Safe", typeof(RectTransform));
        safe.transform.SetParent(ct, false);
        Stretch(safe.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safe.AddComponent<SafeArea>();
        Transform st = safe.transform;
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Vector2 tl = new Vector2(0, 1), tr = new Vector2(1, 1), tc = new Vector2(0.5f, 1f), br = new Vector2(1, 0);

        // fighter bars
        Text pName = MakeText(st, "PlayerName", "YOU", 40, TextAnchor.MiddleLeft, new Color(0.3f, 0.9f, 1f), FontStyle.Bold);
        Fixed(pName.transform, tl, tl, new Vector2(40, -16), new Vector2(500, 56));
        Image pBack = MakeImage(st, "PlayerHpBack", new Color(0, 0, 0, 0.55f), round, true, false);
        Fixed(pBack.transform, tl, tl, new Vector2(40, -76), new Vector2(720, 40));
        Image pFill = MakeImage(pBack.transform, "Fill", new Color(0.2f, 1f, 0.6f), round, false, false);
        pFill.type = Image.Type.Filled; pFill.fillMethod = Image.FillMethod.Horizontal; pFill.fillOrigin = 0;
        Stretch(pFill.transform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
        Image sBack = MakeImage(st, "StaminaBack", new Color(0, 0, 0, 0.5f), round, true, false);
        Fixed(sBack.transform, tl, tl, new Vector2(40, -124), new Vector2(420, 18));
        Image sFill = MakeImage(sBack.transform, "Fill", new Color(1f, 0.85f, 0.2f), round, false, false);
        sFill.type = Image.Type.Filled; sFill.fillMethod = Image.FillMethod.Horizontal; sFill.fillOrigin = 0;
        Stretch(sFill.transform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

        Text bName = MakeText(st, "BotName", "ROOKIE SAM", 40, TextAnchor.MiddleRight, new Color(1f, 0.3f, 0.6f), FontStyle.Bold);
        Fixed(bName.transform, tr, tr, new Vector2(-40, -16), new Vector2(600, 56));
        Image bBack = MakeImage(st, "BotHpBack", new Color(0, 0, 0, 0.55f), round, true, false);
        Fixed(bBack.transform, tr, tr, new Vector2(-40, -76), new Vector2(720, 40));
        Image bFill = MakeImage(bBack.transform, "Fill", new Color(1f, 0.25f, 0.45f), round, false, false);
        bFill.type = Image.Type.Filled; bFill.fillMethod = Image.FillMethod.Horizontal; bFill.fillOrigin = 1;
        Stretch(bFill.transform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));

        Text timer = MakeText(st, "Timer", "1:30", 64, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(timer.transform, tc, tc, new Vector2(0, -14), new Vector2(240, 80));
        Text fightT = MakeText(st, "FightNo", "FIGHT 1 / 5", 28, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 1f), FontStyle.Bold);
        Fixed(fightT.transform, tc, tc, new Vector2(0, -92), new Vector2(300, 40));
        Text center = MakeText(st, "Center", "", 110, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(center.transform, mid, mid, new Vector2(0, 180), new Vector2(1500, 180));

        // joystick
        Image jbg = MakeImage(st, "Joystick", new Color(1, 1, 1, 0.15f), knob);
        Fixed(jbg.transform, Vector2.zero, mid, new Vector2(260, 260), new Vector2(320, 320));
        Image jh = MakeImage(jbg.transform, "Handle", new Color(1, 1, 1, 0.45f), knob, false, false);
        Fixed(jh.transform, mid, mid, Vector2.zero, new Vector2(140, 140));
        VirtualJoystick joy = jbg.gameObject.AddComponent<VirtualJoystick>();
        joy.background = (RectTransform)jbg.transform;
        joy.handle = (RectTransform)jh.transform;
        joy.radius = 110f;
        pb.joystick = joy;

        // punch buttons
        Image cross = MakeImage(st, "CrossButton", new Color(1f, 0.2f, 0.4f, 0.6f), knob);
        Fixed(cross.transform, br, mid, new Vector2(-190, 300), new Vector2(260, 260));
        pb.crossButton = cross.gameObject.AddComponent<Button>(); pb.crossButton.targetGraphic = cross;
        StyledButton sbCross = cross.gameObject.AddComponent<StyledButton>(); sbCross.look = StyledButton.Look.Primary; sbCross.circle = true; sbCross.alpha = 0.9f; sbCross.silent = true;
        Text cl = MakeText(cross.transform, "Label", "CROSS", 44, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(cl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image jab = MakeImage(st, "JabButton", new Color(1f, 0.55f, 0.15f, 0.6f), knob);
        Fixed(jab.transform, br, mid, new Vector2(-470, 200), new Vector2(210, 210));
        pb.jabButton = jab.gameObject.AddComponent<Button>(); pb.jabButton.targetGraphic = jab;
        StyledButton sbJab = jab.gameObject.AddComponent<StyledButton>(); sbJab.look = StyledButton.Look.Orange; sbJab.circle = true; sbJab.alpha = 0.9f; sbJab.silent = true;
        Text jl = MakeText(jab.transform, "Label", "JAB", 40, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(jl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image blk = MakeImage(st, "BlockButton", new Color(0.1f, 0.8f, 1f, 0.55f), knob);
        Fixed(blk.transform, br, mid, new Vector2(-470, 450), new Vector2(200, 200));
        pb.blockButton = blk.gameObject.AddComponent<HoldButton>();
        StyledButton sbBlk = blk.gameObject.AddComponent<StyledButton>(); sbBlk.look = StyledButton.Look.Cyan; sbBlk.circle = true; sbBlk.alpha = 0.9f; sbBlk.silent = true;
        Text bl = MakeText(blk.transform, "Label", "BLOCK", 38, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(bl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // upgrade cards
        Image cardImg = MakeImage(ct, "CardPanel", new Color(0, 0, 0, 0.82f));
        Stretch(cardImg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text cardHead = MakeText(cardImg.transform, "Head", "YOU WON! PICK AN UPGRADE", 70, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(cardHead.transform, tc, tc, new Vector2(0, -110), new Vector2(1600, 120));
        Text[] cTitles = new Text[3];
        Text[] cDescs = new Text[3];
        Button[] cBtns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            Image cb = MakeImage(cardImg.transform, "Card" + i, new Color(0.1f, 0.13f, 0.32f, 0.98f), round, true);
            Fixed(cb.transform, mid, mid, new Vector2((i - 1) * 560, -30), new Vector2(500, 600));
            Button bt2 = cb.gameObject.AddComponent<Button>();
            bt2.targetGraphic = cb;
            StyledButton sbCard = cb.gameObject.AddComponent<StyledButton>(); sbCard.look = StyledButton.Look.Card;
            Text t1 = MakeText(cb.transform, "Title", "CARD", 52, TextAnchor.MiddleCenter, new Color(0.3f, 0.9f, 1f), FontStyle.Bold);
            Stretch(t1.transform, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);
            Text t2 = MakeText(cb.transform, "Desc", "", 38, TextAnchor.UpperCenter, Color.white);
            Stretch(t2.transform, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.52f), Vector2.zero, Vector2.zero);
            cTitles[i] = t1; cDescs[i] = t2; cBtns[i] = bt2;
        }
        cardImg.gameObject.SetActive(false);

        // end panel
        Image endImg = MakeImage(ct, "EndPanel", new Color(0, 0, 0, 0.82f));
        Stretch(endImg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text endTitle = MakeText(endImg.transform, "Title", "KNOCKED OUT", 120, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(endTitle.transform, mid, mid, new Vector2(0, 170), new Vector2(1500, 180));
        Text endSub = MakeText(endImg.transform, "Sub", "", 56, TextAnchor.MiddleCenter, new Color(0.8f, 0.9f, 1f), FontStyle.Bold);
        Fixed(endSub.transform, mid, mid, new Vector2(0, 30), new Vector2(1200, 100));
        Image rb = MakeImage(endImg.transform, "Restart", new Color(0.1f, 0.85f, 1f, 0.95f), round, true);
        Fixed(rb.transform, mid, mid, new Vector2(0, -150), new Vector2(460, 130));
        Button rbBtn = rb.gameObject.AddComponent<Button>();
        rbBtn.targetGraphic = rb;
        StyledButton sbRb = rb.gameObject.AddComponent<StyledButton>(); sbRb.look = StyledButton.Look.Primary; sbRb.pulse = true;
        Text rbl = MakeText(rb.transform, "Label", "PLAY AGAIN", 50, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(rbl.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text lMenuBtn;
        Button menuBtn = MenuButton(endImg.transform, "MenuButton", "MAIN MENU", StyledButton.Look.Dark, new Vector2(0, 0), new Vector2(460, 110), 42, false, out lMenuBtn);
        Fixed(menuBtn.transform, mid, mid, new Vector2(0, -310), new Vector2(460, 110));
        endImg.gameObject.SetActive(false);

        // ---- bout logic
        GameObject game = new GameObject("Game");
        BoutManager bm = game.AddComponent<BoutManager>();
        bm.player = pf; bm.bot = bf; bm.pb = pb; bm.botAI = bb;
        bm.botSkin = skinR.ToArray(); bm.botShorts = shortR.ToArray();
        bm.bloodMat = bloodMat; bm.splatSprite = round; bm.splatRoot = (RectTransform)splatGO.transform;
        bm.playerHpFill = pFill; bm.playerStFill = sFill; bm.botHpFill = bFill; bm.hurtOverlay = hurt;
        bm.playerNameText = pName; bm.botNameText = bName; bm.timerText = timer; bm.fightText = fightT; bm.centerText = center;
        bm.cardPanel = cardImg.gameObject; bm.cardTitle = cTitles; bm.cardDesc = cDescs; bm.cardButton = cBtns;
        bm.endPanel = endImg.gameObject; bm.endTitle = endTitle; bm.endSub = endSub; bm.restartButton = rbBtn;
        bm.menuButton = menuBtn; bm.waitForMenu = true;
        BuildMenu(ct, safe, pb, bf, new[] { pf.gloveL.gameObject, pf.gloveR.gameObject }, round);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Knockout Night scene built: " + ScenePath);
    }
}
