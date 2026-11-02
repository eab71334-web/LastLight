using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum QuestStage { Arrive, FindParts, Relight, Done }

/// <summary>Holds the story state and the HUD.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public const int PartsNeeded = 3;

    public QuestStage stage = QuestStage.Arrive;
    public int parts;
    public Transform player;
    public DialogueUI dialogue;
    public Text objectiveText;
    public Text partsText;
    public Text titleText;
    public Image fade;
    public GameObject endPanel;
    public Button restartButton;

    [HideInInspector] public bool busy;

    public bool InputLocked
    {
        get { return busy || (dialogue != null && dialogue.IsOpen); }
    }

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (endPanel != null) endPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
    }

    void Start()
    {
        Refresh();
        StartCoroutine(Intro());
    }

    public void SetStage(QuestStage s)
    {
        stage = s;
        Refresh();
    }

    /// <summary>Returns true when all parts are collected.</summary>
    public bool AddPart()
    {
        parts++;
        if (parts >= PartsNeeded) stage = QuestStage.Relight;
        Refresh();
        return parts >= PartsNeeded;
    }

    public void Complete()
    {
        stage = QuestStage.Done;
        Refresh();
        StartCoroutine(EndSequence());
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Refresh()
    {
        if (objectiveText != null)
        {
            switch (stage)
            {
                case QuestStage.Arrive: objectiveText.text = "Objective: speak with Elder Sael"; break;
                case QuestStage.FindParts: objectiveText.text = "Objective: find the lantern parts"; break;
                case QuestStage.Relight: objectiveText.text = "Objective: return to the lighthouse"; break;
                default: objectiveText.text = "The light burns again"; break;
            }
        }
        if (partsText != null)
            partsText.text = stage == QuestStage.Arrive ? "" : "Parts " + parts + "/" + PartsNeeded;
    }

    void SetFade(float a)
    {
        if (fade != null) fade.color = new Color(0f, 0f, 0f, a);
        if (titleText != null)
        {
            Color c = titleText.color;
            c.a = a;
            titleText.color = c;
        }
    }

    IEnumerator Intro()
    {
        busy = true;
        if (titleText != null) titleText.text = "THE LAST LIGHT\n<size=44>Chapter 1 - Arrival</size>";
        SetFade(1f);
        yield return new WaitForSeconds(3f);
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            SetFade(Mathf.Clamp01(1f - t / 1.5f));
            yield return null;
        }
        SetFade(0f);
        if (titleText != null) titleText.text = "";
        busy = false;
        if (dialogue != null)
            dialogue.Show("Rian", new[]
            {
                "The shore is silent. Not even gulls. Someone in the village must know where I am.",
                "(Left stick to walk. Drag the right side of the screen to look around. Tap the button that appears to interact.)"
            });
    }

    IEnumerator EndSequence()
    {
        busy = true;
        yield return new WaitForSeconds(6f);
        if (endPanel != null) endPanel.SetActive(true);
    }
}
