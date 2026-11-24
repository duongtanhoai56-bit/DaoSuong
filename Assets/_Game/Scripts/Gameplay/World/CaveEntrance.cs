using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Interactable surface portal bound to a generated underground dungeon.</summary>
    [DisallowMultipleComponent]
    public sealed class CaveEntrance : GatherTarget
    {
        [SerializeField] private int index;
        private CaveManager manager;
        private CaveDefinition definition;
        private SphereCollider interactionCollider;

        public int Index => index;
        public CaveDefinition Definition => definition;
        public override bool IsAvailable => isActiveAndEnabled && manager != null && definition != null && !manager.IsInsideCave;

        public void Initialize(int value)
        {
            index = Mathf.Max(0, value);
            EnsureInteractionCollider();
        }

        public void Bind(CaveManager caveManager, CaveDefinition caveDefinition)
        {
            manager = caveManager;
            definition = caveDefinition;
            EnsureInteractionCollider();
        }

        public bool Interact()
        {
            if (!IsAvailable) return false;
            return manager.TryEnter(this);
        }

        public override void Respawn() { }

        private void EnsureInteractionCollider()
        {
            if (interactionCollider == null) interactionCollider = GetComponent<SphereCollider>();
            if (interactionCollider == null) interactionCollider = gameObject.AddComponent<SphereCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 1f, 3.1f);
            interactionCollider.radius = 2.65f;
        }
    }
}
