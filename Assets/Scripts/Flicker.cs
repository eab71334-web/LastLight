using UnityEngine;

/// <summary>Fire-like flicker for a light.</summary>
[RequireComponent(typeof(Light))]
public class Flicker : MonoBehaviour
{
    Light l;
    float baseIntensity;
    float seed;

    void Awake()
    {
        l = GetComponent<Light>();
        baseIntensity = l.intensity;
        seed = Random.value * 10f;
    }

    void Update()
    {
        l.intensity = baseIntensity * (0.75f + 0.5f * Mathf.PerlinNoise(Time.time * 6f, seed));
    }
}
