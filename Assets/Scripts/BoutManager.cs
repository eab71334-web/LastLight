using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Knockout Night: a run of 5 fights, 90 seconds each. Win to pick an upgrade card and move on.</summary>
public class BoutManager : MonoBehaviour
{
    public static BoutManager I;

    public Fighter player, bot;
    public PlayerBoxer pb;
    public BotBoxer botAI;
    public bool waitForMenu;
    public Button menuButton;
    public Renderer[] botSkin, botShorts;
    public Material bloodMat;
    public Sprite splatSprite;
    public RectTransform splatRoot;

    public Image playerHpFill, playerStFill, botHpFill, hurtOverlay;
    public Text playerNameText, botNameText, timerText, fightText, centerText;

    public GameObject cardPanel;
    public Text[] cardTitle = new Text[3];
    public Text[] cardDesc = new Text[3];
    public Button[] cardButton = new Button[3];

    public GameObject endPanel;
    public Text endTitle, endSub;
    public Button restartButton;

    public bool Fighting { get; private set; }

    class OppDef
    {
        public string name; public float hp, agg, react, dmg; public Color skin, shorts;
        public OppDef(string n, float h, float a, float r, float d, Color s, Color sh)
        { name = n; hp = h; agg = a; react = r; dmg = d; skin = s; shorts = sh; }
    }

    static readonly OppDef[] Opps =
    {
        new OppDef("ROOKIE SAM",     70f,  0.35f, 0.15f, 0.8f,  new Color(0.86f, 0.66f, 0.52f), new Color(0.2f, 0.5f, 0.95f)),
        new OppDef("BRAWLER JAD",    90f,  0.5f,  0.3f,  0.95f, new Color(0.72f, 0.5f, 0.36f),  new Color(0.1f, 0.75f, 0.4f)),
        new OppDef("HAMMER TARIQ",   110f, 0.6f,  0.4f,  1.1f,  new Color(0.55f, 0.37f, 0.27f), new Color(0.95f, 0.75f, 0.1f)),
        new OppDef("SHADOW IDRIS",   120f, 0.75f, 0.55f, 1.15f, new Color(0.45f, 0.3f, 0.22f),  new Color(0.6f, 0.2f, 0.9f)),
        new OppDef("CHAMPION KHALID",150f, 0.85f, 0.7f,  1.25f, new Color(0.35f, 0.24f, 0.18f), new Color(0.9f, 0.1f, 0.2f)),
    };

    static readonly string[] CardNames = { "HEAVY HANDS", "QUICK HANDS", "IRON CHIN", "DEEP LUNGS", "TIGHT GUARD", "FOOTWORK", "SECOND WIND" };
    static readonly string[] CardDescs =
    {
        "Punches hit 25% harder", "Punch 20% faster", "+30 max health", "Stamina comes back 40% faster",
        "Blocking costs 40% less stamina", "Move 20% faster", "Heal 20% more between fights"
    };

    int fightIndex, wins;
    float timeLeft, hurtAlpha, healPct = 0.35f, msgUntil;
    int[] cardIds = new int[3];
    ParticleSystem blood;
    List<GameObject> floorSplats = new List<GameObject>();

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        Application.targetFrameRate = 60;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Start()
    {
        player.opponent = bot;
        bot.opponent = player;
        if (restartButton != null)
            restartButton.onClick.AddListener(() => { MainMenu.skipNext = true; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
        if (menuButton != null)
            menuButton.onClick.AddListener(() => { MainMenu.skipNext = false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
        for (int i = 0; i < cardButton.Length; i++)
        {
            int idx = i;
            if (cardButton[i] != null) cardButton[i].onClick.AddListener(() => PickCard(idx));
        }
        if (cardPanel != null) cardPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (playerNameText != null) playerNameText.text = "YOU";
        blood = MakeBlood();
        if (!waitForMenu) StartFight(0);
    }

    /// <summary>Called by the main menu when Play is pressed.</summary>
    public void BeginRun() { StartFight(0); }

    ParticleSystem MakeBlood()
    {
        GameObject g = new GameObject("BloodFx");
        ParticleSystem ps = g.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.7f;
        main.startSpeed = 3.2f;
        main.startSize = 0.09f;
        main.startColor = new Color(0.7f, 0.02f, 0.05f);
        main.gravityModifier = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = false;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Standard");
        g.GetComponent<ParticleSystemRenderer>().material = new Material(s);
        return ps;
    }

    void Say(string t, float seconds)
    {
        if (centerText == null) return;
        centerText.text = t;
        msgUntil = Time.time + seconds;
    }

    void StartFight(int i)
    {
        fightIndex = i;
        OppDef d = Opps[i];
        bot.displayName = d.name; bot.maxHp = d.hp; bot.damageMul = d.dmg;
        botAI.aggression = d.agg; botAI.reaction = d.react;
        foreach (Renderer r in botSkin) r.material.color = d.skin;
        foreach (Renderer r in botShorts) r.material.color = d.shorts;
        BoxerRig br = bot.GetComponent<BoxerRig>();
        if (br != null) br.Tint(d.skin, d.shorts, Color.Lerp(d.shorts, Color.black, 0.2f));

        float keep = i == 0 ? player.maxHp : Mathf.Min(player.maxHp, player.hp + player.maxHp * healPct);
        player.ResetFighter();
        bot.ResetFighter();
        player.hp = keep;
        player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -2.5f), Quaternion.identity);
        bot.transform.SetPositionAndRotation(new Vector3(0f, 0f, 2.5f), Quaternion.Euler(0f, 180f, 0f));
        timeLeft = 90f;
        if (botNameText != null) botNameText.text = d.name;
        if (fightText != null) fightText.text = "FIGHT " + (i + 1) + " / " + Opps.Length;
        if (ArenaShow.I != null) ArenaShow.I.SetNames("YOU", d.name);
        if (Crowd.I != null) { Crowd.I.SetBase(0.2f + 0.08f * i); Crowd.I.Cheer(0.5f); }
        StartCoroutine(Intro(d.name));
    }

    IEnumerator Intro(string oppName)
    {
        Fighting = false;
        Say(oppName, 1.3f);
        yield return new WaitForSeconds(1.4f);
        Say("READY", 0.7f);
        yield return new WaitForSeconds(0.8f);
        Say("FIGHT!", 1f);
        Sfx.Bell(Camera.main.transform.position);
        if (Crowd.I != null) Crowd.I.Burst(0.7f);
        Fighting = true;
    }

    void Update()
    {
        if (Fighting)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f) { timeLeft = 0f; TimeUp(); }
        }
        if (centerText != null && Time.time > msgUntil) centerText.text = "";
        int t = Mathf.CeilToInt(timeLeft);
        if (timerText != null) timerText.text = (t / 60) + ":" + (t % 60).ToString("00");

        if (playerHpFill != null)
        {
            float f = Mathf.Clamp01(player.hp / player.maxHp);
            playerHpFill.fillAmount = f;
            playerHpFill.color = Color.Lerp(new Color(1f, 0.2f, 0.3f), new Color(0.2f, 1f, 0.6f), f);
        }
        if (playerStFill != null) playerStFill.fillAmount = Mathf.Clamp01(player.stamina / player.maxStamina);
        if (botHpFill != null) botHpFill.fillAmount = Mathf.Clamp01(bot.hp / bot.maxHp);
        if (hurtOverlay != null)
        {
            hurtAlpha = Mathf.MoveTowards(hurtAlpha, 0f, Time.deltaTime * 1.1f);
            hurtOverlay.color = new Color(1f, 0f, 0f, hurtAlpha);
        }
    }

    // ------------------------------------------------------------ hits and knockouts
    public void OnHit(Fighter victim, Fighter attacker, float dmg, bool blocked, bool heavy)
    {
        Vector3 listener = Camera.main.transform.position;
        if (blocked) { Sfx.Block(listener); if (victim == player) pb.Shake(0.1f); if (Crowd.I != null) Crowd.I.Cheer(0.03f); return; }
        Sfx.Punch(listener);
        if (Crowd.I != null)
        {
            Crowd.I.Cheer(heavy ? 0.35f : 0.18f);
            if (victim == player && heavy) Crowd.I.Gasp();
            else if (heavy) Crowd.I.Burst(0.6f);
        }
        if (victim == player)
        {
            pb.Shake(heavy ? 0.6f : 0.3f);
            hurtAlpha = Mathf.Min(0.8f, hurtAlpha + (heavy ? 0.5f : 0.3f));
            ScreenSplat(heavy ? 2 : 1);
        }
        else
        {
            Vector3 p = victim.head != null ? victim.head.position : victim.transform.position + Vector3.up * 1.7f;
            blood.transform.position = p;
            blood.Emit(heavy ? 26 : 12);
            if (Random.value < (heavy ? 0.7f : 0.35f)) FloorSplat(victim.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)));
        }
    }

    void ScreenSplat(int n)
    {
        if (splatRoot == null) return;
        for (int k = 0; k < n; k++)
        {
            float ax = Random.Range(0.15f, 0.85f), ay = Random.Range(0.2f, 0.8f);
            for (int j = 0; j < 4; j++)
            {
                GameObject g = new GameObject("Splat", typeof(RectTransform), typeof(Image), typeof(FadeOutUI));
                g.transform.SetParent(splatRoot, false);
                RectTransform rt = (RectTransform)g.transform;
                rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
                rt.anchoredPosition = new Vector2(Random.Range(-60f, 60f), Random.Range(-60f, 60f));
                float s = Random.Range(60f, 260f);
                rt.sizeDelta = new Vector2(s, s * Random.Range(0.6f, 1.4f));
                Image im = g.GetComponent<Image>();
                im.sprite = splatSprite;
                im.raycastTarget = false;
                im.color = new Color(0.55f, 0.02f, 0.06f, 0.85f);
            }
        }
        while (splatRoot.childCount > 60) DestroyImmediate(splatRoot.GetChild(0).gameObject);
    }

    void FloorSplat(Vector3 pos)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(g.GetComponent<Collider>());
        float r = Random.Range(0.15f, 0.4f);
        g.transform.position = new Vector3(pos.x, 0.025f, pos.z);
        g.transform.localScale = new Vector3(r, 0.004f, r * Random.Range(0.7f, 1.3f));
        if (bloodMat != null) g.GetComponent<Renderer>().sharedMaterial = bloodMat;
        floorSplats.Add(g);
        if (floorSplats.Count > 40) { Destroy(floorSplats[0]); floorSplats.RemoveAt(0); }
    }

    public void OnKnockout(Fighter loser)
    {
        if (!Fighting) return;
        Fighting = false;
        Say("K.O.!", 2.5f);
        if (loser == player) BotWin();
        if (Crowd.I != null) Crowd.I.Roar();
        if (ArenaShow.I != null) ArenaShow.I.Flash();
        Sfx.Bell(Camera.main.transform.position);
        StartCoroutine(AfterFight(loser == bot));
    }

    void BotWin()
    {
        BoxerRig br = bot.GetComponent<BoxerRig>();
        if (br != null) br.Win();
    }

    void TimeUp()
    {
        Fighting = false;
        bool pw = player.hp / player.maxHp >= bot.hp / bot.maxHp;
        Say("TIME!", 2f);
        if (!pw) BotWin();
        Sfx.Bell(Camera.main.transform.position);
        StartCoroutine(AfterFight(pw));
    }

    IEnumerator AfterFight(bool playerWon)
    {
        yield return new WaitForSeconds(2.6f);
        if (playerWon)
        {
            wins++;
            if (fightIndex >= Opps.Length - 1) ShowEnd("CHAMPION", "You beat all " + Opps.Length + " fighters");
            else ShowCards();
        }
        else ShowEnd("KNOCKED OUT", "Fights won: " + wins + " / " + Opps.Length);
    }

    void ShowEnd(string title, string sub)
    {
        if (endTitle != null) endTitle.text = title;
        if (endSub != null) endSub.text = sub;
        if (endPanel != null) endPanel.SetActive(true);
    }

    // ------------------------------------------------------------ upgrade cards
    void ShowCards()
    {
        if (cardPanel == null) { StartFight(fightIndex + 1); return; }
        List<int> pool = new List<int>();
        for (int i = 0; i < CardNames.Length; i++) pool.Add(i);
        for (int i = 0; i < 3; i++)
        {
            int k = Random.Range(0, pool.Count);
            cardIds[i] = pool[k];
            pool.RemoveAt(k);
            if (cardTitle[i] != null) cardTitle[i].text = CardNames[cardIds[i]];
            if (cardDesc[i] != null) cardDesc[i].text = CardDescs[cardIds[i]];
        }
        cardPanel.SetActive(true);
    }

    void PickCard(int slot)
    {
        switch (cardIds[slot])
        {
            case 0: player.damageMul *= 1.25f; break;
            case 1: player.cooldownMul *= 0.8f; break;
            case 2: player.maxHp += 30f; player.hp += 30f; break;
            case 3: player.staminaRegen *= 1.4f; break;
            case 4: player.blockCost *= 0.6f; break;
            case 5: player.moveSpeed *= 1.2f; break;
            case 6: healPct += 0.2f; break;
        }
        cardPanel.SetActive(false);
        StartFight(fightIndex + 1);
    }
}
