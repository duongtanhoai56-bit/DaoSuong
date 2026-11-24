namespace Game.Gameplay
{
    /// <summary>
    /// Small deterministic PRNG (SplitMix32 style). Avoids depending on System.Random's
    /// implementation details so the same seed/settings produce the same prop sequence.
    /// </summary>
    public struct DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            state = unchecked((uint)seed) + 0x9E3779B9u;
            if (state == 0u)
                state = 0xA341316Cu;
        }

        public uint NextUInt()
        {
            unchecked
            {
                state += 0x9E3779B9u;
                uint z = state;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }

        public float Next01()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float minInclusive, float maxExclusive)
        {
            return minInclusive + (maxExclusive - minInclusive) * Next01();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }
    }
}
