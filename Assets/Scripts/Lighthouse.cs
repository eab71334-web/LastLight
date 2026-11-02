using UnityEngine;

public class Lighthouse : Interactable
{
    public Light[] lights;
    public Renderer lampRenderer;
    public Material lampOn;
    public Transform rotor;

    bool lit;

    void Update()
    {
        if (lit && rotor != null) rotor.Rotate(0f, 45f * Time.deltaTime, 0f);
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;
        if (gm == null || gm.InputLocked) return;

        switch (gm.stage)
        {
            case QuestStage.Arrive:
                gm.dialogue.Show("Rian", new[]
                {
                    "The old lighthouse. Cold and dark.",
                    "I should find out what is going on in the village first."
                });
                break;

            case QuestStage.FindParts:
                gm.dialogue.Show("Rian", new[]
                {
                    "The lamp room is empty. No lens, no flame crystal, no mirror.",
                    "Parts found: " + gm.parts + " of " + GameManager.PartsNeeded + "."
                });
                break;

            case QuestStage.Relight:
                gm.dialogue.Show("Rian", new[]
                {
                    "The lens... the crystal... the mirror. They fit as if they were made for this place.",
                    "Here goes."
                }, () =>
                {
                    TurnOn();
                    gm.Complete();
                });
                break;

            default:
                gm.dialogue.Show("Rian", new[]
                {
                    "Let it shine. Let every ship find its way home."
                });
                break;
        }
    }

    void TurnOn()
    {
        lit = true;
        foreach (Light l in lights) l.enabled = true;
        if (lampRenderer != null && lampOn != null) lampRenderer.sharedMaterial = lampOn;
    }
}
