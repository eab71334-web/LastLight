using UnityEngine;

/// <summary>A talkable character. What they say depends on the quest stage.</summary>
public class Npc : Interactable
{
    public string npcName = "Villager";
    public bool givesQuest;
    public string[] arriveLines;
    public string[] findLines;
    public string[] relightLines;
    public string[] doneLines;

    void Reset() { prompt = "Talk"; }

    void Update()
    {
        GameManager gm = GameManager.I;
        if (gm == null || gm.player == null) return;
        Vector3 d = gm.player.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f && d.sqrMagnitude < range * range * 1.6f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 4f * Time.deltaTime);
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;
        if (gm == null || gm.InputLocked) return;

        string[] lines;
        switch (gm.stage)
        {
            case QuestStage.Arrive: lines = arriveLines; break;
            case QuestStage.FindParts: lines = findLines; break;
            case QuestStage.Relight: lines = relightLines; break;
            default: lines = doneLines; break;
        }

        bool startQuest = givesQuest && gm.stage == QuestStage.Arrive;
        gm.dialogue.Show(npcName, lines, () =>
        {
            if (startQuest) gm.SetStage(QuestStage.FindParts);
        });
    }
}
