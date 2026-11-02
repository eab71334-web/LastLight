using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Typewriter dialogue box. A line may be written as "Speaker|text" to change the speaker.
/// This component must live on an object that is always active (not on the panel itself).
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public GameObject panel;
    public Text speakerText;
    public Text bodyText;
    public Button nextButton;
    public float charsPerSecond = 55f;

    string[] lines;
    int index;
    string defaultSpeaker;
    string fullText;
    Action onDone;
    Coroutine typing;

    public bool IsOpen { get { return panel != null && panel.activeSelf; } }

    void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (IsOpen && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) Next();
    }

    public void Show(string speaker, string[] l, Action done = null)
    {
        if (IsOpen || l == null || l.Length == 0) return;
        defaultSpeaker = speaker;
        lines = l;
        index = 0;
        onDone = done;
        panel.SetActive(true);
        ShowLine();
    }

    void ShowLine()
    {
        string raw = lines[index];
        string speaker = defaultSpeaker;
        string text = raw;
        int bar = raw.IndexOf('|');
        if (bar > 0)
        {
            speaker = raw.Substring(0, bar);
            text = raw.Substring(bar + 1);
        }
        speakerText.text = speaker;
        fullText = text;
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Type());
    }

    IEnumerator Type()
    {
        bodyText.text = "";
        float shown = 0f;
        while (shown < fullText.Length)
        {
            shown += charsPerSecond * Time.deltaTime;
            bodyText.text = fullText.Substring(0, Mathf.Min(fullText.Length, (int)shown));
            yield return null;
        }
        bodyText.text = fullText;
        typing = null;
    }

    void Next()
    {
        if (!IsOpen) return;
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
            bodyText.text = fullText;
            return;
        }
        index++;
        if (index >= lines.Length)
        {
            panel.SetActive(false);
            Action cb = onDone;
            onDone = null;
            if (cb != null) cb();
        }
        else
        {
            ShowLine();
        }
    }
}
