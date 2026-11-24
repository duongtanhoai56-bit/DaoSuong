using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CaveExitPortal : GatherTarget
    {
        private CaveManager manager;
        public override bool IsAvailable => isActiveAndEnabled && manager != null && manager.IsInsideCave;

        public void Initialize(CaveManager caveManager) => manager = caveManager;

        public bool Interact() => IsAvailable && manager.ExitCave();

        public override void Respawn() { }
    }
}
