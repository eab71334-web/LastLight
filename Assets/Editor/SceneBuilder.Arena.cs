using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Arena dressing (ring details, truss, light show, screens, ringside), the crowd and the main menu.
public static partial class SceneBuilder
{
    static TextMesh MakeTextMesh(Transform parent, string name, string text, Vector3 pos, Vector3 euler,
                                 Color color, int fontSize, float charSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        TextMesh tm = go.AddComponent<TextMesh>();
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = f;
        tm.text = text;
        tm.fontSize = fontSize;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = f.material;
        return tm;
    }

    static Material MakeAdditive(string name)
    {
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) return null;
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.3f));
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ ring, ringside, truss, screens, crowd
    static void BuildRingAndStage(Transform arena, Material ropeC, Material ropeM, Material ropeW, Material postM)
    {
        float e = 5.3f;
        Material padRed = Mat("PadRed", new Color(0.75f, 0.05f, 0.1f), false, 0.35f);
        Material padWhite = Mat("PadWhite", new Color(0.92f, 0.92f, 0.95f), false, 0.35f);
        Material padBlue = Mat("PadBlue", new Color(0.08f, 0.2f, 0.7f), false, 0.35f);
        Material metal = Mat("TrussMetal", new Color(0.12f, 0.12f, 0.15f), false, 0.6f, 0.8f);
        Material led = Mat("LedStrip", new Color(0.2f, 0.9f, 1f), true, 0.3f);
        Material screenFrame = Mat("ScreenFrame", new Color(0.04f, 0.04f, 0.06f), false, 0.5f, 0.5f);
        Material screenFace = Mat("ScreenFace", new Color(0.03f, 0.06f, 0.22f), true, 0.2f);
        Material wood = Mat("RingsideWood", new Color(0.18f, 0.1f, 0.06f), false, 0.4f);
        Material gold = Mat("BellGold", new Color(0.95f, 0.75f, 0.2f), false, 0.8f, 0.9f);
        Material lamp = Mat("LampWhite", new Color(1f, 1f, 1f), true, 0.3f);
        Material white = Mat("LineWhite", new Color(0.9f, 0.9f, 0.95f), false, 0.3f);
        Material[] pads = { padRed, padWhite, padBlue };

        // ---- ropes (round) and posts with corner pads
        Material[] ropes = { ropeC, ropeM, ropeW };
        float[] hs = { 0.55f, 0.95f, 1.35f };
        for (int i = 0; i < 3; i++)
        {
            Deco(PrimitiveType.Cylinder, "RopeN" + i, new Vector3(0, hs[i], e), new Vector3(0.06f, e, 0.06f), ropes[i], arena, new Vector3(0, 0, 90));
            Deco(PrimitiveType.Cylinder, "RopeS" + i, new Vector3(0, hs[i], -e), new Vector3(0.06f, e, 0.06f), ropes[i], arena, new Vector3(0, 0, 90));
            Deco(PrimitiveType.Cylinder, "RopeE" + i, new Vector3(e, hs[i], 0), new Vector3(0.06f, e, 0.06f), ropes[i], arena, new Vector3(90, 0, 0));
            Deco(PrimitiveType.Cylinder, "RopeW" + i, new Vector3(-e, hs[i], 0), new Vector3(0.06f, e, 0.06f), ropes[i], arena, new Vector3(90, 0, 0));
        }
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Deco(PrimitiveType.Cylinder, "Post", new Vector3(sx * e, 0.95f, sz * e), new Vector3(0.2f, 0.95f, 0.2f), postM, arena);
                for (int i = 0; i < 3; i++)
                    Deco(PrimitiveType.Cube, "Pad", new Vector3(sx * e, hs[i], sz * e), new Vector3(0.36f, 0.36f, 0.36f), pads[i], arena, new Vector3(0, 45, 0));
                Deco(PrimitiveType.Cube, "PostCap", new Vector3(sx * e, 1.92f, sz * e), new Vector3(0.38f, 0.1f, 0.38f), padRed, arena, new Vector3(0, 45, 0));
            }

        // ---- canvas details: border lines and logo
        float b = 4.75f;
        Deco(PrimitiveType.Cube, "LineN", new Vector3(0, 0.046f, b), new Vector3(b * 2, 0.008f, 0.08f), white, arena);
        Deco(PrimitiveType.Cube, "LineS", new Vector3(0, 0.046f, -b), new Vector3(b * 2, 0.008f, 0.08f), white, arena);
        Deco(PrimitiveType.Cube, "LineE", new Vector3(b, 0.046f, 0), new Vector3(0.08f, 0.008f, b * 2), white, arena);
        Deco(PrimitiveType.Cube, "LineW", new Vector3(-b, 0.046f, 0), new Vector3(0.08f, 0.008f, b * 2), white, arena);
        TextMesh logo = MakeTextMesh(arena, "CanvasLogo", "KNOCKOUT NIGHT", new Vector3(0, 0.055f, 0), new Vector3(90, 0, 0),
                                     new Color(0.95f, 0.15f, 0.2f), 80, 0.1f);

        // ---- LED skirt strips around the apron
        List<Renderer> strips = new List<Renderer>();
        strips.Add(Deco(PrimitiveType.Cube, "LedN", new Vector3(0, -0.15f, 6.03f), new Vector3(12.1f, 0.12f, 0.06f), led, arena).GetComponent<Renderer>());
        strips.Add(Deco(PrimitiveType.Cube, "LedS", new Vector3(0, -0.15f, -6.03f), new Vector3(12.1f, 0.12f, 0.06f), led, arena).GetComponent<Renderer>());
        strips.Add(Deco(PrimitiveType.Cube, "LedE", new Vector3(6.03f, -0.15f, 0), new Vector3(0.06f, 0.12f, 12.1f), led, arena).GetComponent<Renderer>());
        strips.Add(Deco(PrimitiveType.Cube, "LedW", new Vector3(-6.03f, -0.15f, 0), new Vector3(0.06f, 0.12f, 12.1f), led, arena).GetComponent<Renderer>());

        // ---- ring steps (south side)
        Deco(PrimitiveType.Cube, "StepHigh", new Vector3(3.5f, -0.7f, -6.45f), new Vector3(2.2f, 1.0f, 0.9f), metal, arena);
        Deco(PrimitiveType.Cube, "StepLow", new Vector3(3.5f, -0.95f, -7.35f), new Vector3(2.2f, 0.5f, 0.9f), metal, arena);

        // ---- timekeeper table with bell and laptops
        Deco(PrimitiveType.Cube, "Table", new Vector3(-3.5f, -0.75f, -7.3f), new Vector3(3.2f, 0.9f, 0.9f), wood, arena);
        Deco(PrimitiveType.Cylinder, "Bell", new Vector3(-2.6f, -0.26f, -7.3f), new Vector3(0.22f, 0.04f, 0.22f), gold, arena);
        Deco(PrimitiveType.Cube, "Laptop1", new Vector3(-4.2f, -0.28f, -7.25f), new Vector3(0.55f, 0.02f, 0.38f), lamp, arena);
        Deco(PrimitiveType.Cube, "Laptop2", new Vector3(-3.4f, -0.28f, -7.25f), new Vector3(0.55f, 0.02f, 0.38f), lamp, arena);

        // ---- TV camera jibs
        List<Transform> jibs = new List<Transform>();
        for (int s = 0; s < 2; s++)
        {
            float sx = s == 0 ? -1f : 1f;
            Vector3 basePos = new Vector3(sx * 7.6f, -0.95f, sx * 3f);
            GameObject holder = new GameObject("CameraRig" + s);
            holder.transform.SetParent(arena, false);
            holder.transform.position = basePos;
            holder.transform.rotation = Quaternion.LookRotation(new Vector3(-basePos.x, 0, -basePos.z).normalized, Vector3.up);
            Deco(PrimitiveType.Cube, "CamBase", Vector3.zero, new Vector3(1.4f, 0.5f, 1.4f), metal, holder.transform);
            GameObject pivot = new GameObject("JibPivot");
            pivot.transform.SetParent(holder.transform, false);
            pivot.transform.localPosition = new Vector3(0, 0.6f, 0);
            Deco(PrimitiveType.Cylinder, "JibArm", new Vector3(0, 0, 1.6f), new Vector3(0.09f, 1.6f, 0.09f), metal, pivot.transform, new Vector3(90, 0, 0));
            Deco(PrimitiveType.Cube, "CamBody", new Vector3(0, 0, 3.3f), new Vector3(0.5f, 0.4f, 0.8f), screenFrame, pivot.transform);
            Deco(PrimitiveType.Cylinder, "CamLens", new Vector3(0, 0, 3.85f), new Vector3(0.28f, 0.16f, 0.28f), metal, pivot.transform, new Vector3(90, 0, 0));
            Deco(PrimitiveType.Sphere, "Tally", new Vector3(0, 0.26f, 3.2f), new Vector3(0.1f, 0.1f, 0.1f), Mat("Tally", new Color(1f, 0.05f, 0.05f), true), pivot.transform);
            jibs.Add(pivot.transform);
        }

        // ---- lighting truss with moving beam fixtures
        float ty = 9.5f;
        Deco(PrimitiveType.Cube, "TrussN", new Vector3(0, ty, 8), new Vector3(16.4f, 0.3f, 0.3f), metal, arena);
        Deco(PrimitiveType.Cube, "TrussS", new Vector3(0, ty, -8), new Vector3(16.4f, 0.3f, 0.3f), metal, arena);
        Deco(PrimitiveType.Cube, "TrussE", new Vector3(8, ty, 0), new Vector3(0.3f, 0.3f, 16.4f), metal, arena);
        Deco(PrimitiveType.Cube, "TrussW", new Vector3(-8, ty, 0), new Vector3(0.3f, 0.3f, 16.4f), metal, arena);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Deco(PrimitiveType.Cylinder, "Cable", new Vector3(sx * 8, ty + 4f, sz * 8), new Vector3(0.05f, 4f, 0.05f), metal, arena);

        Material beamMat = MakeAdditive("BeamAdditive");
        Material poolMat = beamMat;
        int movers = 6;
        Transform[] moverT = new Transform[movers];
        Transform[] poolT = beamMat != null ? new Transform[movers] : null;
        for (int i = 0; i < movers; i++)
        {
            float a = i * Mathf.PI * 2f / movers + 0.3f;
            GameObject m = new GameObject("Mover" + i);
            m.transform.SetParent(arena, false);
            m.transform.position = new Vector3(Mathf.Cos(a) * 8f, ty - 0.35f, Mathf.Sin(a) * 8f);
            m.transform.rotation = Quaternion.LookRotation(Vector3.down);
            Deco(PrimitiveType.Cube, "Body", new Vector3(0, 0, 0.2f), new Vector3(0.4f, 0.4f, 0.6f), screenFrame, m.transform);
            Deco(PrimitiveType.Sphere, "Lens", new Vector3(0, 0, 0.5f), new Vector3(0.3f, 0.3f, 0.12f), lamp, m.transform);
            moverT[i] = m.transform;
            if (poolT != null)
                poolT[i] = Deco(PrimitiveType.Cylinder, "LightPool", new Vector3(0, 0.07f, 0), new Vector3(2.8f, 0.004f, 2.8f), poolMat, arena).transform;
        }

        // ---- big screens above the crowd
        List<TextMesh> screenTexts = new List<TextMesh>();
        for (int s = 0; s < 4; s++)
        {
            float a = s * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            GameObject sc = new GameObject("Screen" + s);
            sc.transform.SetParent(arena, false);
            sc.transform.position = dir * 18f + new Vector3(0, 8f, 0);
            sc.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);   // +Z outward, readable side faces the ring
            Deco(PrimitiveType.Cube, "Frame", Vector3.zero, new Vector3(9.2f, 5.4f, 0.4f), screenFrame, sc.transform);
            Deco(PrimitiveType.Cube, "Face", new Vector3(0, 0, -0.25f), new Vector3(8.6f, 4.8f, 0.08f), screenFace, sc.transform);
            TextMesh tm = MakeTextMesh(sc.transform, "Names", "YOU\nVS\nOPPONENT", new Vector3(0, 0, -0.35f), Vector3.zero, new Color(1f, 0.95f, 0.85f), 80, 0.1f);
            screenTexts.Add(tm);
        }

        // ---- show controller
        GameObject showGO = new GameObject("ArenaShow");
        showGO.transform.SetParent(arena, false);
        ArenaShow show = showGO.AddComponent<ArenaShow>();
        show.movers = moverT;
        show.beamMat = beamMat;
        show.pools = poolT;
        show.ledStrips = strips.ToArray();
        show.screenTexts = screenTexts.ToArray();
        show.jibs = jibs.ToArray();
        List<TextMesh> fit = new List<TextMesh>(screenTexts);
        fit.Add(logo);
        show.fitTexts = fit.ToArray();
        float[] widths = new float[fit.Count];
        for (int i = 0; i < widths.Length; i++) widths[i] = 6.2f;
        widths[widths.Length - 1] = 7.6f;
        show.fitWidths = widths;

        // ---- crowd
        GameObject crowdGO = new GameObject("Crowd");
        Crowd cr = crowdGO.AddComponent<Crowd>();
        cr.shirtMats = new[]
        {
            Mat("Shirt0", new Color(0.7f, 0.1f, 0.12f)), Mat("Shirt1", new Color(0.1f, 0.2f, 0.6f)),
            Mat("Shirt2", new Color(0.9f, 0.9f, 0.92f)), Mat("Shirt3", new Color(0.12f, 0.12f, 0.14f)),
            Mat("Shirt4", new Color(0.1f, 0.5f, 0.25f)), Mat("Shirt5", new Color(0.85f, 0.65f, 0.1f)),
            Mat("Shirt6", new Color(0.5f, 0.15f, 0.55f)), Mat("Shirt7", new Color(0.1f, 0.65f, 0.75f))
        };
        cr.skinMats = new[]
        {
            Mat("FanSkin0", new Color(0.93f, 0.76f, 0.62f)), Mat("FanSkin1", new Color(0.8f, 0.6f, 0.45f)),
            Mat("FanSkin2", new Color(0.6f, 0.42f, 0.3f)), Mat("FanSkin3", new Color(0.4f, 0.27f, 0.2f)),
            Mat("FanSkin4", new Color(0.88f, 0.68f, 0.52f))
        };
        cr.hairMats = new[]
        {
            Mat("FanHair0", new Color(0.05f, 0.04f, 0.04f)), Mat("FanHair1", new Color(0.25f, 0.15f, 0.08f)),
            Mat("FanHair2", new Color(0.55f, 0.4f, 0.15f)), Mat("FanHair3", new Color(0.35f, 0.35f, 0.38f))
        };
        cr.legMat = Mat("FanLegs", new Color(0.1f, 0.12f, 0.2f));
        cr.seatMat = Mat("Bleacher", new Color(0.09f, 0.09f, 0.14f), false, 0.2f);
        cr.flashMat = Mat("CamFlash", new Color(1f, 1f, 1f), true);
        cr.floorY = -1.2f;
    }

    // ------------------------------------------------------------------ main menu
    static Button MenuButton(Transform parent, string name, string label, StyledButton.Look look, Vector2 pos, Vector2 size,
                             int fontSize, bool pulse, out Text labelText)
    {
        Image img = MakeImage(parent, name, Color.white);
        Fixed(img.transform, Vector2.zero, Vector2.zero, pos, size);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        StyledButton sb = img.gameObject.AddComponent<StyledButton>();
        sb.look = look;
        sb.pulse = pulse;
        labelText = MakeText(img.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(labelText.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return b;
    }

    static void MakeOutline(Text t, Color c, float d)
    {
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(d, -d);
    }

    static MainMenu BuildMenu(Transform ct, GameObject hudRoot, PlayerBoxer pb, Fighter bf, GameObject[] hide, Sprite round)
    {
        GameObject root = new GameObject("MenuRoot", typeof(RectTransform));
        root.transform.SetParent(ct, false);
        Stretch(root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject ctrl = new GameObject("MenuController");
        MainMenu mm = ctrl.AddComponent<MainMenu>();

        Image fade = MakeImage(root.transform, "LeftFade", Color.white, null, false, false);
        Stretch(fade.transform, Vector2.zero, new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero);

        // ---- main panel
        GameObject main = new GameObject("Main", typeof(RectTransform));
        main.transform.SetParent(root.transform, false);
        Stretch(main.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(main.transform, false);
        RectTransform titleRT = Fixed(titleGO.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -60), new Vector2(1100, 340));
        Text t1 = MakeText(titleGO.transform, "Line1", "KNOCKOUT", 170, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        Stretch(t1.transform, new Vector2(0, 0.45f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        MakeOutline(t1, new Color(0.9f, 0.1f, 0.2f, 0.9f), 4f);
        Text t2 = MakeText(titleGO.transform, "Line2", "NIGHT", 170, TextAnchor.MiddleLeft, new Color(1f, 0.35f, 0.2f), FontStyle.Bold);
        Stretch(t2.transform, new Vector2(0, 0f), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
        MakeOutline(t2, new Color(1f, 0.85f, 0.4f, 0.9f), 3f);
        titleRT.pivot = new Vector2(0, 1);

        Text tag = MakeText(main.transform, "Tagline", "FIRST-PERSON BOXING TOURNAMENT", 34, TextAnchor.MiddleLeft, new Color(0.75f, 0.85f, 1f), FontStyle.Bold);
        Fixed(tag.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, -410), new Vector2(1000, 50));

        Text lPlay, lOnline, lHow, lSet, lQuit;
        Button play = MenuButton(main.transform, "PlayButton", "PLAY", StyledButton.Look.Primary, new Vector2(110, 480), new Vector2(640, 140), 70, true, out lPlay);
        Button online = MenuButton(main.transform, "OnlineButton", "ONLINE", StyledButton.Look.Dark, new Vector2(110, 360), new Vector2(520, 100), 44, false, out lOnline);
        Button how = MenuButton(main.transform, "HowButton", "HOW TO PLAY", StyledButton.Look.Ghost, new Vector2(110, 250), new Vector2(520, 100), 42, false, out lHow);
        Button settingsB = MenuButton(main.transform, "SettingsButton", "SETTINGS", StyledButton.Look.Ghost, new Vector2(110, 140), new Vector2(520, 100), 42, false, out lSet);
        Button quit = MenuButton(main.transform, "QuitButton", "QUIT", StyledButton.Look.Ghost, new Vector2(110, 30), new Vector2(520, 90), 38, false, out lQuit);

        Text ver = MakeText(main.transform, "Version", "v0.4  -  OFFLINE", 28, TextAnchor.LowerRight, new Color(1, 1, 1, 0.55f), FontStyle.Bold);
        Fixed(ver.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 24), new Vector2(500, 44));

        // ---- settings panel
        GameObject sp = new GameObject("Settings", typeof(RectTransform));
        sp.transform.SetParent(root.transform, false);
        Stretch(sp.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image sdim = MakeImage(sp.transform, "Dim", new Color(0, 0, 0, 0.6f));
        Stretch(sdim.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image sCard = MakeImage(sp.transform, "Card", new Color(0.06f, 0.07f, 0.17f, 0.97f), round, true);
        Fixed(sCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 700));
        Text sTitle = MakeText(sCard.transform, "Title", "SETTINGS", 72, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(sTitle.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(800, 110));
        Text lSound, lQual, lBack1, lBack2;
        Button sound = MenuButton(sCard.transform, "SoundButton", "SOUND: ON", StyledButton.Look.Cyan, new Vector2(150, 360), new Vector2(600, 110), 44, false, out lSound);
        Button qual = MenuButton(sCard.transform, "QualityButton", "GRAPHICS", StyledButton.Look.Cyan, new Vector2(150, 220), new Vector2(600, 110), 44, false, out lQual);
        Button back1 = MenuButton(sCard.transform, "BackButton", "BACK", StyledButton.Look.Dark, new Vector2(250, 60), new Vector2(400, 100), 42, false, out lBack1);
        sp.SetActive(false);

        // ---- how to play panel
        GameObject hp = new GameObject("HowTo", typeof(RectTransform));
        hp.transform.SetParent(root.transform, false);
        Stretch(hp.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image hdim = MakeImage(hp.transform, "Dim", new Color(0, 0, 0, 0.6f));
        Stretch(hdim.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image hCard = MakeImage(hp.transform, "Card", new Color(0.06f, 0.07f, 0.17f, 0.97f), round, true);
        Fixed(hCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 800));
        Text hTitle = MakeText(hCard.transform, "Title", "HOW TO PLAY", 72, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Fixed(hTitle.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1100, 110));
        Text hBody = MakeText(hCard.transform, "Body",
            "LEFT STICK  -  MOVE AND CIRCLE YOUR OPPONENT\nJAB  -  FAST, LIGHT PUNCH\nCROSS  -  SLOWER, HEAVY PUNCH\nBLOCK  -  HOLD TO GUARD (USES STAMINA)\n\nBEAT 5 FIGHTERS IN A ROW.\nPICK AN UPGRADE CARD AFTER EVERY WIN.",
            40, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 1f), FontStyle.Bold);
        Stretch(hBody.transform, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
        Text lBack3;
        Button back2 = MenuButton(hCard.transform, "BackButton", "BACK", StyledButton.Look.Dark, new Vector2(450, 40), new Vector2(400, 100), 42, false, out lBack3);
        hp.SetActive(false);

        mm.menuRoot = root;
        mm.mainPanel = main; mm.settingsPanel = sp; mm.howPanel = hp; mm.hudRoot = hudRoot;
        mm.playBtn = play; mm.onlineBtn = online; mm.howBtn = how; mm.settingsBtn = settingsB; mm.quitBtn = quit;
        mm.backBtn1 = back1; mm.backBtn2 = back2; mm.soundBtn = sound; mm.qualityBtn = qual;
        mm.soundLabel = lSound; mm.qualityLabel = lQual; mm.onlineLabel = lOnline;
        mm.leftFade = fade; mm.titleRoot = titleRT;
        mm.pb = pb; mm.bot = bf; mm.hideInMenu = hide;
        return mm;
    }
}
