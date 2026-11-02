using System.Collections.Generic;
using UnityEngine;

/// <summary>Anything the player can talk to / pick up / examine.</summary>
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    public string prompt = "Talk";
    public float range = 3.2f;

    protected virtual void OnEnable() { All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    public abstract void Interact();
}
