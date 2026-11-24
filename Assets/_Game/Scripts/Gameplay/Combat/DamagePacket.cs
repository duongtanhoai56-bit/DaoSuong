using System;
using Game.Core;

namespace DaoSuong.Combat
{
    public readonly struct DamagePacket
    {
        public StableId SourceId { get; }
        public float Amount { get; }

        public DamagePacket(StableId sourceId, float amount)
        {
            if (amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            SourceId = sourceId;
            Amount = amount;
        }
    }
}
