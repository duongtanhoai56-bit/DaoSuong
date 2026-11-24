using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    /// <summary>
    /// Explicit GPU instancing for static island props. Each tree/rock remains its own
    /// GameObject with its own Collider, while rendering is grouped by mesh and terrain chunk.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IslandInstanceRenderer : MonoBehaviour
    {
        private const int MaximumInstancesPerDraw = 511;

        private struct BatchKey
        {
            public Mesh mesh;
            public int chunkIndex;

            public bool Equals(BatchKey other) => mesh == other.mesh && chunkIndex == other.chunkIndex;
            public override bool Equals(object obj) => obj is BatchKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((mesh != null ? mesh.GetInstanceID() : 0) * 397) ^ chunkIndex;
                }
            }
        }

        private sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public readonly List<Transform> transforms = new List<Transform>();
            public readonly Matrix4x4[] matrices = new Matrix4x4[MaximumInstancesPerDraw];
            public Bounds worldBounds;
            public bool hasBounds;
        }

        private readonly Dictionary<BatchKey, Batch> batches = new Dictionary<BatchKey, Batch>();
        private bool instancingSupported;
        private bool fallbackQueued;
        private int registeredCount;

        public int BatchCount => batches.Count;
        public int InstanceCount => registeredCount;
        public bool UsesGpuInstancing => instancingSupported && batches.Count > 0;

        private void Awake()
        {
            instancingSupported = SystemInfo.supportsInstancing;
        }

        public void Register(Transform instance, Mesh mesh, Material material, int chunkIndex)
        {
            if (instance == null || mesh == null || material == null)
                return;

            registeredCount++;
            if (!instancingSupported || !material.enableInstancing)
            {
                AddFallbackRenderer(instance, mesh, material);
                return;
            }

            BatchKey key = new BatchKey { mesh = mesh, chunkIndex = chunkIndex };
            if (!batches.TryGetValue(key, out Batch batch))
            {
                batch = new Batch { mesh = mesh, material = material };
                batches.Add(key, batch);
            }

            batch.transforms.Add(instance);
            Vector3 position = instance.position;
            if (!batch.hasBounds)
            {
                batch.worldBounds = new Bounds(position, Vector3.one * 6f);
                batch.hasBounds = true;
            }
            else
            {
                batch.worldBounds.Encapsulate(position);
            }
        }

        private void LateUpdate()
        {
            foreach (KeyValuePair<BatchKey, Batch> pair in batches)
            {
                Batch batch = pair.Value;
                int count = 0;
                for (int i = 0; i < batch.transforms.Count; i++)
                {
                    Transform instance = batch.transforms[i];
                    if (instance == null || !instance.gameObject.activeInHierarchy)
                        continue;

                    if (count >= MaximumInstancesPerDraw)
                    {
                        Submit(batch, count);
                        if (fallbackQueued) break;
                        count = 0;
                    }

                    batch.matrices[count++] = instance.localToWorldMatrix;
                }

                if (fallbackQueued) break;
                if (count > 0)
                    Submit(batch, count);
                if (fallbackQueued) break;
            }

            if (fallbackQueued)
            {
                // A platform can report instancing support but still reject a render call.
                // Convert every registered prop to a normal MeshRenderer instead of failing Play.
                foreach (Batch batch in batches.Values)
                {
                    foreach (Transform instance in batch.transforms)
                    {
                        if (instance != null)
                            AddFallbackRenderer(instance, batch.mesh, batch.material);
                    }
                }
                batches.Clear();
                instancingSupported = false;
                fallbackQueued = false;
                enabled = false;
            }
        }

        private void Submit(Batch batch, int count)
        {
            Bounds bounds = batch.worldBounds;
            bounds.Expand(new Vector3(10f, 12f, 10f));
            RenderParams renderParams = new RenderParams(batch.material)
            {
                worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                layer = gameObject.layer,
                lightProbeUsage = LightProbeUsage.Off
            };

            try
            {
                Graphics.RenderMeshInstanced(ref renderParams, batch.mesh, 0, batch.matrices, count);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogWarning($"[Island] GPU instancing không khả dụng trên thiết bị này; chuyển sang MeshRenderer. {exception.Message}", this);
                fallbackQueued = true;
            }
        }

        private static void AddFallbackRenderer(Transform instance, Mesh mesh, Material material)
        {
            MeshFilter filter = instance.GetComponent<MeshFilter>();
            if (filter == null)
                filter = instance.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = instance.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }
    }
}
