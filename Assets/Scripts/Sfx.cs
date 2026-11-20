using System;
using UnityEngine;

/// <summary>Tiny procedural sound effects (no audio files needed).</summary>
public static class Sfx
{
    static AudioClip shot, hit, kill, hurt;

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
}
