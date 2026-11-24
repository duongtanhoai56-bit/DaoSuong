using UnityEngine;

namespace Game.Network
{
    /// <summary>Pure validation rules for client-submitted avatar poses.</summary>
    public static class NetworkAvatarRules
    {
        public static bool IsFinite(Vector3 value)
            => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        public static bool IsFinite(Quaternion value)
            => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

        public static bool IsValidRotation(Quaternion value)
        {
            if (!IsFinite(value)) return false;
            float lengthSquared = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return lengthSquared >= 0.25f && lengthSquared <= 4f;
        }

        public static Quaternion NormalizeSafe(Quaternion value)
        {
            if (!IsValidRotation(value)) return Quaternion.identity;
            float length = Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            return length > 0.0001f
                ? new Quaternion(value.x / length, value.y / length, value.z / length, value.w / length)
                : Quaternion.identity;
        }

        public static bool IsWithinWorldBounds(Vector3 position, float halfSize, float horizontalMargin, float minY, float maxY)
        {
            if (!IsFinite(position) || !IsFinite(halfSize) || !IsFinite(horizontalMargin) ||
                !IsFinite(minY) || !IsFinite(maxY) || halfSize <= 0f || horizontalMargin < 0f || minY > maxY)
                return false;

            float limit = halfSize + horizontalMargin;
            return Mathf.Abs(position.x) <= limit && Mathf.Abs(position.z) <= limit &&
                   position.y >= minY && position.y <= maxY;
        }

        public static bool IsWithinSpeed(Vector3 previous, Vector3 current, double elapsedSeconds,
            float maxSpeed, float speedTolerance, float packetSlack)
        {
            if (!IsFinite(previous) || !IsFinite(current) ||
                double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds <= 0d ||
                !IsFinite(maxSpeed) || !IsFinite(speedTolerance) || !IsFinite(packetSlack) ||
                maxSpeed <= 0f || speedTolerance < 0f || packetSlack < 0f)
                return false;

            // Slack is a small packet-noise allowance; speed tolerance scales with elapsed
            // time, avoiding a large fixed allowance being multiplied by a 20 Hz tick rate.
            double allowedDistance = (maxSpeed + speedTolerance) * elapsedSeconds + packetSlack;
            return Vector3.Distance(previous, current) <= allowedDistance;
        }


        public static bool IsWithinSpawnRadius(Vector3 position, Vector3 expectedSpawn, float radius)
        {
            if (!IsFinite(position) || !IsFinite(expectedSpawn) || !IsFinite(radius) || radius < 0f)
                return false;
            return (position - expectedSpawn).sqrMagnitude <= radius * radius;
        }

        /// <summary>Never accept an initial client pose until the authoritative world spawn is ready.</summary>
        public static bool IsValidInitialPose(bool worldReady, Vector3 position, Vector3 expectedSpawn, float radius)
            => worldReady && IsWithinSpawnRadius(position, expectedSpawn, radius);

        /// <summary>Wrap-safe monotonically increasing sequence check.</summary>
        public static bool IsNewerSequence(uint candidate, uint previous)
            => unchecked((int)(candidate - previous)) > 0;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
