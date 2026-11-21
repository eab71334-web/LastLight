using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu: cinematic camera orbiting the ring while the opponent shadow-boxes, glossy buttons,
/// settings (sound, graphics) and a how-to-play page. Pressing PLAY starts the tournament.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public static bool skipNext;

    public GameObject menuRoot, mainPanel, settingsPanel, howPanel, hudRoot;
    public Button playBtn, onlineBtn, howBtn, settingsBtn, quitBtn, backBtn1, backBtn2, soundBtn, qualityBtn;
    public Text soundLabel, qualityLabel, onlineLabel;
    public Image leftFade;
    public RectTransform titleRoot;
    public PlayerBoxer pb;
    public Fighter bot;
    public GameObject[] hideInMenu;

    bool inMenu;
    float orbit = 200f;
    float onlineMsgUntil;
    Camera cam;

    void Awake()
    {
        if (leftFade != null) { leftFade.sprite = UiKit.HFade(new Color(0.02f, 0.01f, 0.06f, 0.92f), true); leftFade.type = Image.Type.Simple; leftFade.color = Color.white; }
        Wire(playBtn, StartGame);
        Wire(onlineBtn, () => { onlineMsgUntil = Time.unscaledTime + 1.8f; });
        Wire(howBtn, () => Show(howPanel));
        Wire(settingsBtn, () => Show(settingsPanel));
        Wire(quitBtn, () => Application.Quit());
        Wire(backBtn1, () => Show(mainPanel));
        Wire(backBtn2, () => Show(mainPanel));
        Wire(soundBtn, ToggleSound);
        Wire(qualityBtn, CycleQuality);
        if (PlayerPrefs.HasKey("sound")) AudioListener.volume = PlayerPrefs.GetInt("sound") == 1 ? 1f : 0f;
        if (PlayerPrefs.HasKey("quality"))
        {
            int q = Mathf.Clamp(PlayerPrefs.GetInt("quality"), 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(q, true);
        }
        RefreshLabels();
    }

    static void Wire(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.AddListener(a); }

    void Start()
    {
        cam = Camera.main;
        if (skipNext) { skipNext = false; StartCoroutine(DelayedStart()); return; }
        EnterMenu();
    }

    IEnumerator DelayedStart()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        yield return null;
        StartGame();
    }

    void EnterMenu()
    {
        inMenu = true;
        if (menuRoot != null) menuRoot.SetActive(true);
        Show(mainPanel);
        if (hudRoot != null) hudRoot.SetActive(false);
        if (pb != null) pb.enabled = false;
        if (hideInMenu != null) foreach (GameObject g in hideInMenu) if (g != null) g.SetActive(false);
        if (bot != null) bot.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f));
        if (cam != null) cam.fieldOfView = 52f;
        if (Crowd.I != null) Crowd.I.SetBase(0.35f);
        StartCoroutine(ShadowBox());
    }

    IEnumerator ShadowBox()
    {
        BoxerRig rig = bot != null ? bot.GetComponent<BoxerRig>() : null;
        yield return new WaitForSeconds(1f);
        while (inMenu)
        {
            if (rig != null) rig.Punch(Random.value > 0.6f);
            if (Crowd.I != null) Crowd.I.Cheer(0.06f);
            yield return new WaitForSeconds(Random.Range(1.1f, 2.4f));
        }
    }

    void Show(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        if (howPanel != null) howPanel.SetActive(panel == howPanel);
    }

    void StartGame()
    {
        inMenu = false;
        if (menuRoot != null) menuRoot.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(true);
        if (pb != null) pb.enabled = true;
        if (hideInMenu != null) foreach (GameObject g in hideInMenu) if (g != null) g.SetActive(true);
        if (cam != null) cam.fieldOfView = 75f;
        if (ArenaShow.I != null) ArenaShow.I.Flash();
        if (Crowd.I != null) Crowd.I.Roar();
        if (BoutManager.I != null) BoutManager.I.BeginRun();
    }

    void ToggleSound()
    {
        bool on = AudioListener.volume > 0.5f;
        AudioListener.volume = on ? 0f : 1f;
        PlayerPrefs.SetInt("sound", on ? 0 : 1);
        RefreshLabels();
    }

    void CycleQuality()
    {
        int n = QualitySettings.names.Length;
        int q = (QualitySettings.GetQualityLevel() + 1) % Mathf.Max(1, n);
        QualitySettings.SetQualityLevel(q, true);
        PlayerPrefs.SetInt("quality", q);
        RefreshLabels();
    }

    void RefreshLabels()
    {
        if (soundLabel != null) soundLabel.text = "SOUND: " + (AudioListener.volume > 0.5f ? "ON" : "OFF");
        if (qualityLabel != null)
        {
            string[] names = QualitySettings.names;
            int q = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, names.Length - 1));
            qualityLabel.text = "GRAPHICS: " + (names.Length > 0 ? names[q].ToUpper() : "AUTO");
        }
    }

    void Update()
    {
        if (!inMenu) return;
        if (onlineLabel != null)
            onlineLabel.text = Time.unscaledTime < onlineMsgUntil ? "COMING SOON" : "ONLINE";
        if (titleRoot != null)
            titleRoot.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.012f);
    }

    void LateUpdate()
    {
        if (!inMenu || cam == null) return;
        orbit += Time.deltaTime * 7f;
        float a = orbit * Mathf.Deg2Rad;
        float radius = 6.6f + Mathf.Sin(Time.time * 0.21f) * 0.7f;
        float h = 1.75f + Mathf.Sin(Time.time * 0.33f) * 0.35f;
        Vector3 center = new Vector3(0f, 1.25f, 0f);
        // keep the camera on the right half so the menu text on the left stays clear of the action
        cam.transform.position = center + new Vector3(Mathf.Sin(a) * radius, h - 1.25f, Mathf.Cos(a) * radius);
        cam.transform.LookAt(center);
        cam.transform.LookAt(center - cam.transform.right * 1.2f + Vector3.up * 0.05f);
    }
}
