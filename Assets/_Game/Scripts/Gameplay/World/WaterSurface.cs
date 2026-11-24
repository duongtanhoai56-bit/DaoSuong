using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    /// <summary>
    /// A four-vertex/two-triangle ocean plane. Only the private water child follows the
    /// camera; the World root and generated island are never translated by this component.
    /// Wave motion is shading-only and is calculated in the water shader.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaterSurface : MonoBehaviour
    {
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Transform follow;
        [SerializeField, Min(100f)] private float extent = 400f;

        private Mesh waterMesh;
        private Transform waterTransform;

        private void Awake()
        {
            if (waterMaterial == null)
            {
                Debug.LogError("[Water] Chưa gán M_Water.", this);
                enabled = false;
                return;
            }

            waterMesh = new Mesh { name = "Water_TwoTriangles" };
            waterMesh.SetVertices(new[]
            {
                new Vector3(-extent, 0f, -extent),
                new Vector3(-extent, 0f, extent),
                new Vector3(extent, 0f, extent),
                new Vector3(extent, 0f, -extent)
            });
            waterMesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0, true);
            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();

            GameObject waterObject = new GameObject("WaterPlane_FollowCamera");
            waterTransform = waterObject.transform;
            waterTransform.SetParent(transform, false);
            waterObject.AddComponent<MeshFilter>().sharedMesh = waterMesh;
            MeshRenderer renderer = waterObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = waterMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void Start()
        {
            if (follow == null && Camera.main != null)
                follow = Camera.main.transform;
            UpdateWaterPosition();
        }

        private void LateUpdate()
        {
            UpdateWaterPosition();
        }

        private void UpdateWaterPosition()
        {
            if (waterTransform == null || follow == null)
                return;

            // Move only the water child. Never move this component's World GameObject.
            Vector3 worldPosition = new Vector3(follow.position.x, IslandGenerator.WaterLevel, follow.position.z);
            waterTransform.position = worldPosition;
        }

        private void OnDestroy()
        {
            if (waterMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(waterMesh);
            else
                DestroyImmediate(waterMesh);
        }

        private void OnValidate()
        {
            extent = Mathf.Max(100f, extent);
        }
    }
}
