using System;
using UnityEngine;

/// <summary>Tiny procedural sound effects (no audio files needed).</summary>
public static class Sfx
{
    static AudioClip shot, hit, kill, hurt, punch, block, bell, click, cheer, gasp, clap, ambience, whoosh;

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

    static void PlayNear(AudioClip c, float v)
    {
        Vector3 p = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(c, p, v);
    }

    public static void Click()
    {
        if (click == null) click = Make("click", 0.07f, t => Mathf.Sin(t * 2f * Mathf.PI * (900f + t * 4000f)) * Mathf.Exp(-t * 55f) * 0.5f);
        PlayNear(click, 0.8f);
    }

    public static void Whoosh(Vector3 p)
    {
        if (whoosh == null)
        {
            float lp = 0f;
            whoosh = Make("whoosh", 0.18f, t =>
            {
                float n = UnityEngine.Random.value * 2f - 1f;
                lp += (n - lp) * (0.05f + t * 1.2f);
                return lp * 2.2f * Mathf.Sin(t / 0.18f * Mathf.PI);
            });
        }
        Play(whoosh, p, 0.5f);
    }

    /// <summary>Crowd roar / cheer burst (filtered noise plus rough vowel-like tones).</summary>
    public static void Cheer(float vol)
    {
        if (cheer == null)
        {
            float lp = 0f, lp2 = 0f;
            cheer = Make("cheer", 1.8f, t =>
            {
                float n = UnityEngine.Random.value * 2f - 1f;
                lp += (n - lp) * 0.16f; lp2 += (lp - lp2) * 0.4f;
                float band = (lp - lp2) * 6f;
                float env = Mathf.Clamp01(t / 0.15f) * Mathf.Exp(-Mathf.Max(0f, t - 0.45f) * 1.9f);
                float vib = 1f + Mathf.Sin(t * 37f) * 0.015f;
                float voice = (Mathf.Sin(t * 2f * Mathf.PI * 260f * vib) + 0.6f * Mathf.Sin(t * 2f * Mathf.PI * 520f * vib)) * 0.07f;
                return (band + voice) * env;
            });
        }
        PlayNear(cheer, vol);
    }

    /// <summary>"Ooooh" crowd gasp.</summary>
    public static void Gasp(float vol)
    {
        if (gasp == null)
        {
            float lp = 0f;
            gasp = Make("gasp", 1.0f, t =>
            {
                float n = UnityEngine.Random.value * 2f - 1f;
                lp += (n - lp) * 0.1f;
                float env = Mathf.Sin(Mathf.Clamp01(t / 1.0f) * Mathf.PI);
                float f = 300f + 120f * Mathf.Clamp01(t / 0.6f);
                return (lp * 2.5f + Mathf.Sin(t * 2f * Mathf.PI * f) * 0.12f) * env;
            });
        }
        PlayNear(gasp, vol);
    }

    public static void Clap(float vol)
    {
        if (clap == null) clap = Make("clap", 0.09f, t => (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.7f);
        PlayNear(clap, vol);
    }

    /// <summary>Looping murmur of a big crowd.</summary>
    public static AudioClip Ambience()
    {
        if (ambience == null)
        {
            float lp = 0f, lp2 = 0f;
            float len = 4f;
            ambience = Make("ambience", len, t =>
            {
                float n = UnityEngine.Random.value * 2f - 1f;
                lp += (n - lp) * 0.07f; lp2 += (lp - lp2) * 0.07f;
                float wob = 0.75f + 0.25f * Mathf.Sin(t * 2f * Mathf.PI * 0.5f) + 0.1f * Mathf.Sin(t * 2f * Mathf.PI * 1.7f);
                float edge = Mathf.Min(1f, Mathf.Min(t / 0.05f, (len - t) / 0.05f));
                return lp2 * 9f * wob * edge;
            });
        }
        return ambience;
    }
}
