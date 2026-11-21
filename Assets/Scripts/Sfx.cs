using System;
using UnityEngine;

/// <summary>Tiny procedural sound effects (no audio files needed).</summary>
public static class Sfx
{
    static AudioClip shot, hit, kill, hurt, punch, block, bell;

    static AudioClip Make(string name, float seconds, Func<float, float> f)
    {
        int rate = 44100;
        int len = (int)(rate * seconds);
        float[] d = new float[len];
        for (int i = 0; i < len; i++)
            d[i] = Mathf.Clamp(f((float)i / rate), -1f, 1f);
        AudioClip c = AudioClip.Create(name, len, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    static void Play(AudioClip c, Vector3 p, float v) { AudioSource.PlayClipAtPoint(c, p, v); }

    public static void Shot(Vector3 p, float vol)
    {
        if (shot == null)
            shot = Make("shot", 0.14f, t => (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.8f
                                         + Mathf.Sin(t * 2f * Mathf.PI * 150f) * Mathf.Exp(-t * 28f) * 0.6f);
        Play(shot, p, vol);
    }

    public static void Hit(Vector3 p)
    {
        if (hit == null) hit = Make("hit", 0.07f, t => Mathf.Sin(t * 2f * Mathf.PI * 1200f) * Mathf.Exp(-t * 50f) * 0.7f);
        Play(hit, p, 0.6f);
    }

    public static void Kill(Vector3 p)
    {
        if (kill == null) kill = Make("kill", 0.25f, t => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.1f ? 700f : 1000f)) * Mathf.Exp(-t * 9f) * 0.7f);
        Play(kill, p, 0.7f);
    }

    public static void Hurt(Vector3 p)
    {
        if (hurt == null) hurt = Make("hurt", 0.15f, t => Mathf.Sin(t * 2f * Mathf.PI * 90f) * Mathf.Exp(-t * 18f) * 0.9f);
        Play(hurt, p, 0.7f);
    }
    public static void Punch(Vector3 p)
    {
        if (punch == null) punch = Make("punch", 0.16f, t => ((UnityEngine.Random.value * 2f - 1f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 70f)) * Mathf.Exp(-t * 26f));
        Play(punch, p, 0.9f);
    }

    public static void Block(Vector3 p)
    {
        if (block == null) block = Make("block", 0.08f, t => Mathf.Sin(t * 2f * Mathf.PI * 320f) * Mathf.Exp(-t * 55f) * 0.8f);
        Play(block, p, 0.6f);
    }

    public static void Bell(Vector3 p)
    {
        if (bell == null) bell = Make("bell", 0.9f, t => (Mathf.Sin(t * 2f * Mathf.PI * 880f) + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 1320f)) * Mathf.Exp(-t * 4f) * 0.5f);
        Play(bell, p, 0.7f);
    }
}
