using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Deterministic value noise for seeded world generation. It deliberately avoids Unity's
    /// global Random state and Perlin implementation so Android/iOS clients running the same
    /// game build and settings use the same lattice samples.
    /// </summary>
    public static class IslandNoise
    {
        public static float Smooth(float a, float b, float x)
        {
            if (Mathf.Abs(b - a) < 0.00001f)
                return x >= b ? 1f : 0f;

            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        public static float Fbm(float x, float z, float frequency, int octaves, Vector2 offset)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float normalization = 0f;
            int count = Mathf.Max(1, octaves);

            for (int octave = 0; octave < count; octave++)
            {
                float sampleX = x * frequency + offset.x;
                float sampleZ = z * frequency + offset.y;
                sum += ValueNoise(sampleX, sampleZ, 1013 + octave * 7919) * amplitude;
                normalization += amplitude;
                frequency *= 2f;
                amplitude *= 0.5f;
            }

            return normalization > 0f ? sum / normalization : 0f;
        }

        public static float Ridge(float x, float z, float frequency, Vector2 offset)
        {
            float noise = ValueNoise(x * frequency + offset.x, z * frequency + offset.y, 65537);
            return 1f - Mathf.Abs(noise * 2f - 1f);
        }

        public static float Hash01(int x, int z, int seed)
        {
            unchecked
            {
                uint hash = (uint)x * 374761393u;
                hash ^= (uint)z * 668265263u;
                hash ^= (uint)seed * 144665u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) * (1f / 16777216f);
            }
        }

        private static float ValueNoise(float x, float z, int salt)
        {
            int x0 = Mathf.FloorToInt(x);
            int z0 = Mathf.FloorToInt(z);
            int x1 = x0 + 1;
            int z1 = z0 + 1;
            float tx = Smooth(0f, 1f, x - x0);
            float tz = Smooth(0f, 1f, z - z0);

            float a = Hash01(x0, z0, salt);
            float b = Hash01(x1, z0, salt);
            float c = Hash01(x0, z1, salt);
            float d = Hash01(x1, z1, salt);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }
    }
}
