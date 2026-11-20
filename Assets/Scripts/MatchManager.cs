using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Team deathmatch: score, timer, respawns, HUD, end of match.</summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager I;

    public List<Combatant> all = new List<Combatant>();
    public Transform[] spawns0;
    public Transform[] spawns1;
    public Combatant player;
    public WeaponController weapon;
    public FpsPlayer fps;

    public Text timerText, scoreBlue, scoreRed, ammoText, healthText, centerText, feedText, endTitle, endScore;
    public Image healthFill, hurtOverlay;
    public GameObject hitMarker, endPanel;
    public Button restartButton;

    public int scoreLimit = 20;
    public float matchTime = 180f;

    public bool Playing { get; private set; }

    int[] score = new int[2];
    float timeLeft, hurtAlpha, hitUntil, msgUntil;
    List<string> feed = new List<string>();

    void Awake()
    {
        I = this;
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
        timeLeft = matchTime;
        Playing = true;
        if (restartButton != null)
            restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        if (endPanel != null) endPanel.SetActive(false);
        if (hitMarker != null) hitMarker.SetActive(false);
        Say("FIGHT!", 1.6f);
        RefreshScore();
    }

    void Say(string s, float seconds) { if (centerText != null) { centerText.text = s; msgUntil = Time.time + seconds; } }

    void Update()
    {
        if (Playing)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f) { timeLeft = 0f; EndMatch(); }
        }

        if (timerText != null)
        {
            int t = Mathf.CeilToInt(timeLeft);
            timerText.text = (t / 60) + ":" + (t % 60).ToString("00");
        }
        if (centerText != null && Time.time > msgUntil) centerText.text = "";

        if (player != null)
        {
            float f = Mathf.Clamp01(player.health / player.maxHealth);
            if (healthFill != null)
            {
                healthFill.fillAmount = f;
                healthFill.color = Color.Lerp(new Color(1f, 0.2f, 0.3f), new Color(0.2f, 1f, 0.6f), f);
            }
            if (healthText != null) healthText.text = Mathf.CeilToInt(player.health).ToString();
        }
        if (weapon != null && ammoText != null)
            ammoText.text = weapon.Reloading ? "RELOADING" : weapon.Ammo + " / " + weapon.magSize;

        if (hurtOverlay != null)
        {
            hurtAlpha = Mathf.MoveTowards(hurtAlpha, 0f, Time.deltaTime * 1.2f);
            hurtOverlay.color = new Color(1f, 0f, 0f, hurtAlpha);
        }
        if (hitMarker != null && hitMarker.activeSelf && Time.time > hitUntil) hitMarker.SetActive(false);
    }

    void RefreshScore()
    {
        if (scoreBlue != null) scoreBlue.text = score[0].ToString();
        if (scoreRed != null) scoreRed.text = score[1].ToString();
    }

    public void OnPlayerHurt() { hurtAlpha = 0.55f; Sfx.Hurt(Camera.main.transform.position); }

    public void OnHitMarker()
    {
        if (hitMarker != null) { hitMarker.SetActive(true); hitUntil = Time.time + 0.12f; }
        Sfx.Hit(Camera.main.transform.position);
    }

    static string Tag(Combatant c)
    {
        return "<color=" + (c.team == 0 ? "#4de1ff" : "#ff4d9a") + ">" + c.displayName + "</color>";
    }

    public void OnKilled(Combatant victim, Combatant killer)
    {
        if (!Playing) return;
        if (killer != null && killer.team != victim.team)
        {
            score[killer.team]++;
            feed.Add(Tag(killer) + "  >  " + Tag(victim));
            if (feed.Count > 4) feed.RemoveAt(0);
            if (feedText != null) feedText.text = string.Join("\n", feed.ToArray());
            if (killer.isPlayer) { Sfx.Kill(Camera.main.transform.position); Say("ELIMINATED " + victim.displayName.ToUpper(), 1.2f); }
            RefreshScore();
        }
        if (victim.isPlayer) Say("You were eliminated", 2.5f);
        if (score[0] >= scoreLimit || score[1] >= scoreLimit) { EndMatch(); return; }
        StartCoroutine(Respawn(victim));
    }

    void SetActiveState(Combatant c, bool on)
    {
        foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
            if (!c.isPlayer || r.transform.IsChildOf(c.eye) == false) r.enabled = on;
        CharacterController cc = c.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = on;
        FpsPlayer p = c.GetComponent<FpsPlayer>();
        if (p != null) p.enabled = on;
        BotAI b = c.GetComponent<BotAI>();
        if (b != null) b.enabled = on;
    }

    IEnumerator Respawn(Combatant c)
    {
        SetActiveState(c, false);
        yield return new WaitForSeconds(c.isPlayer ? 3f : 2.5f);
        if (!Playing) yield break;
        Transform[] list = c.team == 0 ? spawns0 : spawns1;
        Transform sp = list[Random.Range(0, list.Length)];
        c.transform.position = sp.position;
        c.transform.rotation = sp.rotation;
        c.Revive();
        FpsPlayer p = c.GetComponent<FpsPlayer>();
        if (p != null) p.Face(sp.eulerAngles.y);
        SetActiveState(c, true);
        if (c.isPlayer) Say("FIGHT!", 1f);
    }

    void EndMatch()
    {
        if (!Playing) return;
        Playing = false;
        string title = score[0] > score[1] ? "VICTORY" : (score[0] < score[1] ? "DEFEAT" : "DRAW");
        if (endTitle != null) endTitle.text = title;
        if (endScore != null) endScore.text = score[0] + "  -  " + score[1];
        if (endPanel != null) endPanel.SetActive(true);
    }
}
