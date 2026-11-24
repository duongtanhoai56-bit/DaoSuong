using System;

namespace Game.Core
{
    public readonly struct StableId : IEquatable<StableId>
    {
        public string Value { get; }

        public StableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Stable ID cannot be null or whitespace.", nameof(value));
            Value = value.Trim();
        }

        public bool Equals(StableId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(StableId left, StableId right) => left.Equals(right);
        public static bool operator !=(StableId left, StableId right) => !left.Equals(right);
    }
}
