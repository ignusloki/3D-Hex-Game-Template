using UnityEngine;

public static class HexNoiseUtility
{
    public static float SampleFractalNoise(
        Vector2 samplePosition,
        float frequency,
        int octaves,
        float persistence,
        float lacunarity,
        Vector2 offset)
    {
        float amplitude = 1f;
        float totalAmplitude = 0f;
        float noiseValue = 0f;
        float scaledFrequency = frequency;

        for (int octave = 0; octave < octaves; octave++)
        {
            float sampleX = (samplePosition.x + offset.x) * scaledFrequency;
            float sampleY = (samplePosition.y + offset.y) * scaledFrequency;
            noiseValue += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= persistence;
            scaledFrequency *= lacunarity;
        }

        return totalAmplitude > 0f ? noiseValue / totalAmplitude : 0f;
    }

    public static Vector2 ToSamplePosition(HexCoordinates coordinates)
    {
        float x = coordinates.Column + ((coordinates.Row & 1) == 1 ? 0.5f : 0f);
        float y = coordinates.Row * 0.8660254f;
        return new Vector2(x, y);
    }
}
