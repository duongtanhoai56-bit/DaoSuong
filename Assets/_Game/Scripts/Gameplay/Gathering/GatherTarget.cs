using System;
using UnityEngine;

namespace Game.Gameplay
{
    public abstract class GatherTarget : MonoBehaviour
    {
        public event Action<GatherTarget> Depleted;
        public float RespawnAt { get; protected set; }
        public abstract bool IsAvailable { get; }
        public abstract void Respawn();

        protected void RaiseDepleted() => Depleted?.Invoke(this);
    }
}
