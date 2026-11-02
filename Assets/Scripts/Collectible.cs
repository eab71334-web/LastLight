using UnityEngine;

/// <summary>One of the three lantern parts.</summary>
public class Collectible : Interactable
{
    public string partName = "Lens";
    public string[] pickupLines;
    public Transform visual;

    float baseY;

    void Start()
    {
        if (visual != null) baseY = visual.localPosition.y;
    }

    void Update()
    {
        if (visual == null) return;
        visual.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
        Vector3 p = visual.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * 2f) * 0.15f;
        visual.localPosition = p;
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;
        if (gm == null || gm.InputLocked) return;

        if (gm.stage == QuestStage.Arrive)
        {
            gm.dialogue.Show("Rian", new[]
            {
                "Something glows here. I should ask the villagers before I touch it."
            });
            return;
        }

        bool all = gm.AddPart();
        string[] lines = pickupLines;
        if (all)
        {
            lines = new string[pickupLines.Length + 1];
            pickupLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = "That's all three. Back to the lighthouse.";
        }
        gm.dialogue.Show("Rian", lines);
        Destroy(gameObject);
    }
}
