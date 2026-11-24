using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    /// <summary>
    /// Deterministically generates one low-poly island. Terrain is split into independent
    /// chunk renderers so Unity can frustum-cull chunks outside the camera view.
    /// Keep the World transform at zero/identity/one for the intended mobile setup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IslandGenerator : MonoBehaviour
    {
        public const float WaterLevel = 0f;
        public const int GeneratorAlgorithmVersion = 2;

        [Header("Generation")]
        [SerializeField] private IslandSettings settings;
        [SerializeField] private int seed = 12345;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Transform player;
        [SerializeField] private bool generateOnStart = true;

        private IslandSettings activeSettings;
        private int cells;
        private float size;
        private float half;
        private float cellSize;
        private float[,] heights;
        private Vector2 hillOffset;
        private Vector2 moistureOffset;
        private Vector2 coastOffset;
        private Vector2 ridgeOffset;
        private Vector2 mountainCenter;
        private Vector2 swampCenter;
        private float mountainRadius;
        private float swampRadius;
        private float mountainAngle;
        private int treeCount;
        private int rockCount;
        private int generatedChunkCount;
        private int estimatedTriangleCount;
        private Transform generatedRoot;
        private IslandInstanceRenderer instanceRenderer;
        private readonly List<Mesh> generatedMeshes = new List<Mesh>();
        private readonly List<Vector3> caveEntrances = new List<Vector3>(3);
        private readonly List<GameObject> treeObjects = new List<GameObject>();
        private readonly List<GameObject> rockObjects = new List<GameObject>();

        public bool IsGenerated { get; private set; }
        public int Seed => seed;
        public int Checksum { get; private set; }
        public Vector3 SpawnPoint { get; private set; }
        public float HalfSize => half;
        public int TreeCount => treeCount;
        public int RockCount => rockCount;
        public int ChunkCount => generatedChunkCount;
        public int EstimatedTriangleCount => estimatedTriangleCount + 2; // Include the separate two-triangle water plane.
        public int InstanceBatchCount => instanceRenderer != null ? instanceRenderer.BatchCount : 0;
        public int InstanceCount => instanceRenderer != null ? instanceRenderer.InstanceCount : 0;
        public bool UsesGpuInstancing => instanceRenderer != null && instanceRenderer.UsesGpuInstancing;
        public IReadOnlyList<Vector3> CaveEntrancesWorld => caveEntrances;
        /// <summary>Stable generation order: identical seed/settings produce identical node indices.</summary>
        public IReadOnlyList<GameObject> TreeObjects => treeObjects;
        public IReadOnlyList<GameObject> RockObjects => rockObjects;
        public event Action Generated;

        private void Start()
        {
            if (generateOnStart)
                Generate(seed);
        }

        private void Update()
        {
            // Underground dungeons intentionally use negative world Y. Do not treat a
            // player who entered a registered cave as a fall-out-of-world recovery case.
            if (!IsGenerated || player == null) return;
            CaveManager cave = CaveManager.Instance;
            bool insideCave = cave != null && cave.IsInsideCave;
            if (!CaveRules.ShouldRecoverFromSurfaceFall(insideCave, player.position.y, -25f)) return;

            // Only the template island owns the player reference. In an archipelago the
            // player may be on a remote island, so recover at the active island's spawn.

            IslandGenerator safeIsland = this;
            ArchipelagoManager manager = ArchipelagoManager.Instance;
            if (manager != null)
            {
                IslandGenerator active = manager.GetGenerator(manager.ActiveIslandId);
                if (active != null && active.IsGenerated) safeIsland = active;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.position = safeIsland.SpawnPoint + Vector3.up * 0.2f;
            if (controller != null) controller.enabled = true;
            PlayerController movement = player.GetComponent<PlayerController>();
            if (movement != null) movement.ResetMotion();
        }

        private void OnDestroy()
        {
            DestroyGeneratedMeshes();
        }

        /// <summary>Assigns data before generation. Used by the archipelago runtime to reuse one world template.</summary>
        public void Configure(IslandSettings islandSettings, Material sharedTerrainMaterial, int islandSeed,
            Transform playerTransform, bool autoGenerate = false)
        {
            settings = islandSettings;
            terrainMaterial = sharedTerrainMaterial;
            seed = islandSeed;
            player = playerTransform;
            generateOnStart = autoGenerate;
        }

        /// <summary>Generate the same island for the same seed and settings.</summary>
        public void Generate(int newSeed) => Generate(newSeed, true);

        /// <summary>Generate without moving the player; useful for remote islands in an archipelago.</summary>
        public void Generate(int newSeed, bool placePlayerAtSpawn)
        {
            if (settings == null || terrainMaterial == null)
            {
                Debug.LogError("[Island] Thiếu IslandSettings hoặc Terrain Material.", this);
                return;
            }

            if (transform.rotation != Quaternion.identity || transform.localScale != Vector3.one)
                Debug.LogWarning("[Island] World nên giữ Rotation (0,0,0) và Scale (1,1,1) để giữ đúng kích thước và spawn.", this);

            ClearGenerated();
            seed = newSeed;
            activeSettings = settings;
            size = activeSettings.worldSize;
            half = size * 0.5f;
            cells = activeSettings.chunksPerSide * activeSettings.cellsPerChunk;
            cellSize = size / cells;
            generatedChunkCount = 0;
            treeCount = 0;
            rockCount = 0;
            estimatedTriangleCount = 0;

            generatedRoot = new GameObject("Island_Generated").transform;
            generatedRoot.SetParent(transform, false);

            SetupSeedOffsets();
            BuildHeightMap();
            BuildTerrainChunks();
            BuildProps();
            MixSettingsIntoChecksum();
            BuildBoundary();

            Vector3 localSpawn = FindBeachSpawnPoint();
            SpawnPoint = transform.TransformPoint(localSpawn);
            IsGenerated = true;
            if (placePlayerAtSpawn) PlacePlayerAtSpawn();

            // Include the terrain topology and prop counts in the diagnostic line.
            Debug.Log($"[Island] seed={seed} chunks={generatedChunkCount} terrainTris={cells * cells * 2} trees={treeCount} rocks={rockCount} caves={caveEntrances.Count} estimatedTris={estimatedTriangleCount} checksum={Checksum}", this);
            Generated?.Invoke();
        }

        /// <summary>World-space height query used by gameplay systems such as swamp weather.</summary>
        public float GetHeight(float worldX, float worldZ)
        {
            if (heights == null)
                return transform.position.y;

            Vector3 local = transform.InverseTransformPoint(new Vector3(worldX, transform.position.y, worldZ));
            float localHeight = GetHeightLocal(local.x, local.z);
            return transform.TransformPoint(new Vector3(local.x, localHeight, local.z)).y;
        }

        /// <summary>World-space biome query.</summary>
        public Biome GetBiome(float worldX, float worldZ)
        {
            if (heights == null)
                return Biome.Sea;

            Vector3 local = transform.InverseTransformPoint(new Vector3(worldX, transform.position.y, worldZ));
            return GetBiomeLocal(local.x, local.z);
        }

        public bool ContainsWorldPosition(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return Mathf.Abs(local.x) <= half && Mathf.Abs(local.z) <= half;
        }

        public bool TryGetRandomLandPoint(int pointSeed, out Vector3 worldPoint, bool avoidBeach = false)
        {
            worldPoint = SpawnPoint;
            if (!IsGenerated || heights == null || activeSettings == null) return false;
            DeterministicRandom random = new DeterministicRandom(unchecked(pointSeed * 486187739 + seed));
            float limit = half * 0.66f;
            for (int attempt = 0; attempt < 96; attempt++)
            {
                float x = random.Range(-limit, limit);
                float z = random.Range(-limit, limit);
                float height = GetHeightLocal(x, z);
                if (height < 0.8f) continue;
                Biome biome = GetBiomeLocal(x, z);
                if (biome == Biome.Sea || (avoidBeach && biome == Biome.Beach)) continue;
                worldPoint = transform.TransformPoint(new Vector3(x, height, z));
                return true;
            }
            return false;
        }

        public bool IsLandAt(Vector3 worldPosition)
        {
            if (!IsGenerated || !ContainsWorldPosition(worldPosition)) return false;
            return GetHeight(worldPosition.x, worldPosition.z) > 0.05f;
        }

        private void SetupSeedOffsets()
        {
            DeterministicRandom random = new DeterministicRandom(seed);
            Vector2 NextOffset() => new Vector2(random.Range(100f, 1000f), random.Range(100f, 1000f));
            hillOffset = NextOffset();
            moistureOffset = NextOffset();
            coastOffset = NextOffset();
            ridgeOffset = NextOffset();

            mountainAngle = random.Range(0f, Mathf.PI * 2f);
            Vector2 direction = new Vector2(Mathf.Cos(mountainAngle), Mathf.Sin(mountainAngle));
            Vector2 center = new Vector2(half, half);
            mountainCenter = center + direction * (size * 0.11f);
            swampCenter = center - direction * (size * 0.24f);
            mountainRadius = activeSettings.mountainRadius * size;
            swampRadius = activeSettings.swampRadius * size;
        }

        private void BuildHeightMap()
        {
            heights = new float[cells + 1, cells + 1];
            unchecked
            {
                uint hash = 2166136261u;
                for (int z = 0; z <= cells; z++)
                {
                    for (int x = 0; x <= cells; x++)
                    {
                        float height = ComputeHeight(x * cellSize, z * cellSize);
                        heights[x, z] = height;
                        int quantized = Mathf.RoundToInt(height * 100f);
                        hash = (hash ^ (uint)quantized) * 16777619u;
                    }
                }
                // Settings that affect the map are represented in the checksum as well.
                hash = (hash ^ (uint)activeSettings.chunksPerSide) * 16777619u;
                hash = (hash ^ (uint)activeSettings.cellsPerChunk) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(activeSettings.worldSize * 100f)) * 16777619u;
                Checksum = unchecked((int)hash);
            }
        }

        private float ComputeHeight(float localX, float localZ)
        {
            float nx = (localX / size - 0.5f) * 2f;
            float nz = (localZ / size - 0.5f) * 2f;
            float radialDistance = Mathf.Sqrt(nx * nx + nz * nz);
            radialDistance += (IslandNoise.Fbm(localX, localZ, 0.015f, 3, coastOffset) - 0.5f) * activeSettings.coastRoughness;
            float landMask = 1f - IslandNoise.Smooth(activeSettings.islandCore, activeSettings.islandEdge, radialDistance);

            float hills = IslandNoise.Fbm(localX, localZ, 0.012f, 4, hillOffset);
            float lowland = landMask * (activeSettings.lowlandHeight + hills * activeSettings.hillHeight);
            float height = Mathf.Lerp(activeSettings.seabedDepth, lowland, IslandNoise.Smooth(0f, 0.35f, landMask));

            float mountainDistance = Vector2.Distance(new Vector2(localX, localZ), mountainCenter) / Mathf.Max(1f, mountainRadius);
            float mountainMask = 1f - IslandNoise.Smooth(0f, 1f, mountainDistance);
            float ridge = IslandNoise.Ridge(localX, localZ, 0.025f, ridgeOffset);
            height += mountainMask * (0.45f + 0.55f * ridge) * activeSettings.mountainHeight * IslandNoise.Smooth(0.2f, 0.6f, landMask);

            float swampDistance = Vector2.Distance(new Vector2(localX, localZ), swampCenter) / Mathf.Max(1f, swampRadius);
            float swampMask = (1f - IslandNoise.Smooth(0.3f, 1f, swampDistance)) * IslandNoise.Smooth(0.3f, 0.55f, landMask);
            height = Mathf.Lerp(height, Mathf.Clamp(height, 0.5f, 2.2f), swampMask);

            return Mathf.Round(height * 100f) / 100f;
        }

        private float Moisture(float localX, float localZ)
        {
            float value = IslandNoise.Fbm(localX, localZ, 0.01f, 3, moistureOffset) * 0.7f;
            float distance = Vector2.Distance(new Vector2(localX, localZ), swampCenter) / Mathf.Max(1f, swampRadius);
            float swampMask = 1f - IslandNoise.Smooth(0.4f, 1f, distance);
            return Mathf.Max(value, swampMask);
        }

        private float GetHeightLocal(float localX, float localZ)
        {
            float gridX = Mathf.Clamp((localX + half) / cellSize, 0f, cells - 0.0001f);
            float gridZ = Mathf.Clamp((localZ + half) / cellSize, 0f, cells - 0.0001f);
            int x = Mathf.FloorToInt(gridX);
            int z = Mathf.FloorToInt(gridZ);
            float tx = gridX - x;
            float tz = gridZ - z;
            float h00 = heights[x, z];
            float h10 = heights[x + 1, z];
            float h01 = heights[x, z + 1];
            float h11 = heights[x + 1, z + 1];
            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        private float SlopeNormalYLocal(float localX, float localZ)
        {
            float gx = (GetHeightLocal(localX + cellSize, localZ) - GetHeightLocal(localX - cellSize, localZ)) / (2f * cellSize);
            float gz = (GetHeightLocal(localX, localZ + cellSize) - GetHeightLocal(localX, localZ - cellSize)) / (2f * cellSize);
            return 1f / Mathf.Sqrt(1f + gx * gx + gz * gz);
        }

        private Biome GetBiomeLocal(float localX, float localZ)
        {
            float height = GetHeightLocal(localX, localZ);
            return Classify(height, Moisture(localX + half, localZ + half), SlopeNormalYLocal(localX, localZ));
        }

        private Biome Classify(float height, float moisture, float normalY)
        {
            if (height < WaterLevel) return Biome.Sea;
            if (height >= activeSettings.snowStartHeight) return Biome.Snow;
            if (moisture >= activeSettings.swampMoisture && height < activeSettings.swampMaxHeight) return Biome.Swamp;
            if (height <= activeSettings.beachMaxHeight) return Biome.Beach;
            if (height >= activeSettings.mountainStartHeight || normalY < activeSettings.rockSlope) return Biome.Mountain;
            return Biome.Forest;
        }

        private Color ColorOf(Biome biome)
        {
            switch (biome)
            {
                case Biome.Sea: return activeSettings.seabed;
                case Biome.Beach: return activeSettings.sand;
                case Biome.Swamp: return activeSettings.swamp;
                case Biome.Mountain: return activeSettings.rock;
                case Biome.Snow: return activeSettings.snow;
                default: return activeSettings.forest;
            }
        }

        private void BuildTerrainChunks()
        {
            Transform terrainRoot = new GameObject("Terrain").transform;
            terrainRoot.SetParent(generatedRoot, false);
            int cellsPerChunk = activeSettings.cellsPerChunk;

            for (int chunkZ = 0; chunkZ < activeSettings.chunksPerSide; chunkZ++)
            {
                for (int chunkX = 0; chunkX < activeSettings.chunksPerSide; chunkX++)
                {
                    MeshBuilder builder = new MeshBuilder();
                    for (int localZ = 0; localZ < cellsPerChunk; localZ++)
                    {
                        for (int localX = 0; localX < cellsPerChunk; localX++)
                            AddCell(builder, chunkX * cellsPerChunk + localX, chunkZ * cellsPerChunk + localZ);
                    }

                    Mesh mesh = builder.ToMesh($"Terrain_{chunkX}_{chunkZ}");
                    generatedMeshes.Add(mesh);
                    GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}");
                    chunk.transform.SetParent(terrainRoot, false);
                    chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = terrainMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = true;
                    renderer.allowOcclusionWhenDynamic = true;
                    chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
                    generatedChunkCount++;
                }
            }

            // Unity performs frustum culling per MeshRenderer; each chunk owns a tight mesh bound.
            estimatedTriangleCount += cells * cells * 2;
        }

        private Vector3 GridPoint(int x, int z)
        {
            return new Vector3(-half + x * cellSize, heights[x, z], -half + z * cellSize);
        }

        private void AddCell(MeshBuilder builder, int x, int z)
        {
            Vector3 p00 = GridPoint(x, z);
            Vector3 p10 = GridPoint(x + 1, z);
            Vector3 p01 = GridPoint(x, z + 1);
            Vector3 p11 = GridPoint(x + 1, z + 1);

            if (((x + z) & 1) == 0)
            {
                AddTerrainTriangle(builder, p00, p01, p11, x, z, 0);
                AddTerrainTriangle(builder, p00, p11, p10, x, z, 1);
            }
            else
            {
                AddTerrainTriangle(builder, p00, p01, p10, x, z, 2);
                AddTerrainTriangle(builder, p10, p01, p11, x, z, 3);
            }
        }

        private void AddTerrainTriangle(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, int x, int z, int variant)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.y < 0f)
                normal = -normal;
            normal.Normalize();

            Vector3 center = (a + b + c) / 3f;
            Biome biome = Classify(center.y, Moisture(center.x + half, center.z + half), normal.y);
            Color color = ColorOf(biome);
            float shade = 0.9f + 0.2f * IslandNoise.Hash01(x, z, seed + variant);
            color = new Color(color.r * shade, color.g * shade, color.b * shade, 1f);
            builder.Tri(a, b, c, MeshBuilder.Encode(color), center + Vector3.down * 1000f);
        }

        private void BuildProps()
        {
            treeObjects.Clear();
            rockObjects.Clear();
            DeterministicRandom random = new DeterministicRandom(unchecked(seed * 7919 + 13));
            Transform propsRoot = new GameObject("Props").transform;
            propsRoot.SetParent(generatedRoot, false);
            instanceRenderer = propsRoot.gameObject.AddComponent<IslandInstanceRenderer>();

            List<Vector3> treePositions = new List<Vector3>();
            List<Vector3> rockPositions = new List<Vector3>();
            List<Vector3> caveCandidates = new List<Vector3>();
            float step = activeSettings.propGridStep;

            for (float z = -half + step; z < half - step; z += step)
            {
                for (float x = -half + step; x < half - step; x += step)
                {
                    float px = x + random.Range(-step * 0.4f, step * 0.4f);
                    float pz = z + random.Range(-step * 0.4f, step * 0.4f);
                    float roll = random.Next01();
                    Biome biome = GetBiomeLocal(px, pz);
                    Vector3 position = new Vector3(px, GetHeightLocal(px, pz), pz);
                    float patch = IslandNoise.Fbm(px + half, pz + half, 0.03f, 2, hillOffset);

                    switch (biome)
                    {
                        case Biome.Forest:
                            if (patch > 0.42f && roll < 0.8f)
                                treePositions.Add(position);
                            else if (roll > 0.96f)
                                rockPositions.Add(position);
                            break;
                        case Biome.Mountain:
                            if (roll > 0.8f)
                                rockPositions.Add(position);
                            if (position.y >= activeSettings.mountainStartHeight && position.y <= activeSettings.snowStartHeight - 3f)
                                caveCandidates.Add(position);
                            break;
                        case Biome.Beach:
                            if (roll > 0.93f)
                                rockPositions.Add(position);
                            break;
                    }
                }
            }

            Shuffle(treePositions, ref random);
            Shuffle(rockPositions, ref random);
            Shuffle(caveCandidates, ref random);

            Mesh treeMesh = LowPolyMeshes.Tree();
            generatedMeshes.Add(treeMesh);
            Transform treesRoot = new GameObject("Trees").transform;
            treesRoot.SetParent(propsRoot, false);
            treeCount = Mathf.Min(activeSettings.maxTrees, treePositions.Count);
            MixChecksum(treeCount);
            int treeTriangles = treeMesh.triangles.Length / 3;
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 treePosition = treePositions[i] + Vector3.down * 0.1f;
                GameObject tree = SpawnInstancedColliderProp("Tree", treesRoot, treePosition,
                    Quaternion.Euler(0f, random.Range(0f, 360f), 0f),
                    Vector3.one * random.Range(0.8f, 1.4f), treeMesh, instanceRenderer, GetChunkIndex(treePosition));
                treeObjects.Add(tree);
                CapsuleCollider collider = tree.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 2.05f, 0f);
                collider.height = 4.1f;
                collider.radius = 0.42f;
                MixChecksum(Mathf.RoundToInt(tree.transform.localPosition.x * 100f));
                MixChecksum(Mathf.RoundToInt(tree.transform.localPosition.z * 100f));
                MixChecksum(Mathf.RoundToInt(tree.transform.localEulerAngles.y * 10f));
                MixChecksum(Mathf.RoundToInt(tree.transform.localScale.x * 100f));
            }
            estimatedTriangleCount += treeTriangles * treeCount;

            Mesh[] rockMeshes = { LowPolyMeshes.Rock(1), LowPolyMeshes.Rock(2), LowPolyMeshes.Rock(3) };
            generatedMeshes.AddRange(rockMeshes);
            Transform rocksRoot = new GameObject("Rocks").transform;
            rocksRoot.SetParent(propsRoot, false);
            rockCount = Mathf.Min(activeSettings.maxRocks, rockPositions.Count);
            MixChecksum(rockCount);
            int rockTriangles = rockMeshes[0].triangles.Length / 3;
            for (int i = 0; i < rockCount; i++)
            {
                float scale = random.Range(0.8f, 2.2f);
                Vector3 rockPosition = rockPositions[i] + Vector3.up * 0.1f;
                GameObject rock = SpawnInstancedColliderProp("Rock", rocksRoot, rockPosition,
                    Quaternion.Euler(0f, random.Range(0f, 360f), 0f),
                    new Vector3(scale, scale * random.Range(0.7f, 1.2f), scale), rockMeshes[i % rockMeshes.Length],
                    instanceRenderer, GetChunkIndex(rockPosition));
                rockObjects.Add(rock);
                SphereCollider collider = rock.AddComponent<SphereCollider>();
                collider.center = new Vector3(0f, 0.2f, 0f);
                collider.radius = 0.7f;
                MixChecksum(Mathf.RoundToInt(rock.transform.localPosition.x * 100f));
                MixChecksum(Mathf.RoundToInt(rock.transform.localPosition.z * 100f));
                MixChecksum(Mathf.RoundToInt(rock.transform.localEulerAngles.y * 10f));
                MixChecksum(Mathf.RoundToInt(rock.transform.localScale.x * 100f));
            }
            estimatedTriangleCount += rockTriangles * rockCount;

            BuildCaveEntrances(caveCandidates, propsRoot);
            MixChecksum(caveEntrances.Count);
            foreach (Vector3 caveWorld in caveEntrances)
            {
                Vector3 caveLocal = transform.InverseTransformPoint(caveWorld);
                MixChecksum(Mathf.RoundToInt(caveLocal.x * 100f));
                MixChecksum(Mathf.RoundToInt(caveLocal.z * 100f));
            }
        }

        private void BuildCaveEntrances(List<Vector3> candidates, Transform propsRoot)
        {
            Transform caveRoot = new GameObject("Caves").transform;
            caveRoot.SetParent(propsRoot, false);
            Mesh caveRockMesh = LowPolyMeshes.Rock(7);
            Mesh caveMouthMesh = LowPolyMeshes.Box(new Vector3(3f, 2.4f, 1.2f), new Color(0.025f, 0.025f, 0.035f));
            generatedMeshes.Add(caveRockMesh);
            generatedMeshes.Add(caveMouthMesh);

            for (int i = 0; i < candidates.Count && caveEntrances.Count < activeSettings.caveEntrances; i++)
            {
                Vector3 candidate = candidates[i];
                bool farEnough = true;
                float minimumSpacingSqr = activeSettings.caveMinSpacing * activeSettings.caveMinSpacing;
                foreach (Vector3 existingWorld in caveEntrances)
                {
                    Vector3 existingLocal = transform.InverseTransformPoint(existingWorld);
                    if ((existingLocal - candidate).sqrMagnitude < minimumSpacingSqr)
                    {
                        farEnough = false;
                        break;
                    }
                }
                if (!farEnough)
                    continue;

                Vector2 radial = new Vector2(candidate.x + half, candidate.z + half) - mountainCenter;
                Vector3 outward = new Vector3(radial.x, 0f, radial.y);
                outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;
                Quaternion facing = Quaternion.LookRotation(outward, Vector3.up);

                int index = caveEntrances.Count;
                GameObject entranceObject = new GameObject("CaveEntrance_" + index);
                entranceObject.transform.SetParent(caveRoot, false);
                entranceObject.transform.localPosition = candidate;
                entranceObject.transform.localRotation = facing;
                CaveEntrance entrance = entranceObject.AddComponent<CaveEntrance>();
                entrance.Initialize(index);

                GameObject rock = SpawnProp("CaveRock", caveRockMesh, entranceObject.transform,
                    Vector3.up * 1.15f, Quaternion.identity, new Vector3(4.5f, 3.8f, 4.5f));
                SphereCollider rockCollider = rock.AddComponent<SphereCollider>();
                rockCollider.center = new Vector3(0f, 0.15f, 0f);
                rockCollider.radius = 0.8f;

                SpawnProp("CaveMouth", caveMouthMesh, entranceObject.transform,
                    new Vector3(0f, 1.1f, 3.45f), Quaternion.identity, Vector3.one);

                caveEntrances.Add(transform.TransformPoint(candidate));
            }

            int caveCount = caveEntrances.Count;
            estimatedTriangleCount += caveCount * ((caveRockMesh.triangles.Length + caveMouthMesh.triangles.Length) / 3);
            if (caveCount < activeSettings.caveEntrances)
                Debug.LogWarning($"[Island] Chỉ tìm thấy {caveCount}/{activeSettings.caveEntrances} vị trí hang phù hợp với seed {seed}.", this);
        }


        private int GetChunkIndex(Vector3 localPosition)
        {
            float chunkWorldSize = size / activeSettings.chunksPerSide;
            int chunkX = Mathf.Clamp(Mathf.FloorToInt((localPosition.x + half) / chunkWorldSize), 0, activeSettings.chunksPerSide - 1);
            int chunkZ = Mathf.Clamp(Mathf.FloorToInt((localPosition.z + half) / chunkWorldSize), 0, activeSettings.chunksPerSide - 1);
            return chunkZ * activeSettings.chunksPerSide + chunkX;
        }

        private GameObject SpawnInstancedColliderProp(string objectName, Transform parent, Vector3 localPosition,
            Quaternion localRotation, Vector3 localScale, Mesh mesh, IslandInstanceRenderer instanceRenderer, int chunkIndex)
        {
            GameObject instance = new GameObject(objectName);
            Transform itemTransform = instance.transform;
            itemTransform.SetParent(parent, false);
            itemTransform.localPosition = localPosition;
            itemTransform.localRotation = localRotation;
            itemTransform.localScale = localScale;
            instanceRenderer.Register(itemTransform, mesh, terrainMaterial, chunkIndex);
            return instance;
        }

        private GameObject SpawnProp(string objectName, Mesh mesh, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            GameObject instance = new GameObject(objectName);
            Transform itemTransform = instance.transform;
            itemTransform.SetParent(parent, false);
            itemTransform.localPosition = localPosition;
            itemTransform.localRotation = localRotation;
            itemTransform.localScale = localScale;

            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = terrainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.allowOcclusionWhenDynamic = true;
            return instance;
        }

        private static void Shuffle<T>(List<T> values, ref DeterministicRandom random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        private void MixSettingsIntoChecksum()
        {
            MixChecksum(GeneratorAlgorithmVersion);
            MixChecksum(seed);
            MixFloat(activeSettings.worldSize);
            MixChecksum(activeSettings.chunksPerSide);
            MixChecksum(activeSettings.cellsPerChunk);
            MixFloat(activeSettings.islandCore);
            MixFloat(activeSettings.islandEdge);
            MixFloat(activeSettings.coastRoughness);
            MixFloat(activeSettings.seabedDepth);
            MixFloat(activeSettings.lowlandHeight);
            MixFloat(activeSettings.hillHeight);
            MixFloat(activeSettings.mountainHeight);
            MixFloat(activeSettings.mountainRadius);
            MixFloat(activeSettings.swampRadius);
            MixFloat(activeSettings.beachMaxHeight);
            MixFloat(activeSettings.swampMaxHeight);
            MixFloat(activeSettings.swampMoisture);
            MixFloat(activeSettings.mountainStartHeight);
            MixFloat(activeSettings.snowStartHeight);
            MixFloat(activeSettings.rockSlope);
            MixChecksum(activeSettings.maxTrees);
            MixChecksum(activeSettings.maxRocks);
            MixChecksum(activeSettings.caveEntrances);
            MixFloat(activeSettings.propGridStep);
            MixFloat(activeSettings.caveMinSpacing);
            MixColor(activeSettings.seabed);
            MixColor(activeSettings.sand);
            MixColor(activeSettings.swamp);
            MixColor(activeSettings.forest);
            MixColor(activeSettings.rock);
            MixColor(activeSettings.snow);
        }

        private void MixFloat(float value)
        {
            MixChecksum(Mathf.RoundToInt(value * 10000f));
        }

        private void MixColor(Color value)
        {
            Color32 color = value;
            MixChecksum(color.r | (color.g << 8) | (color.b << 16) | (color.a << 24));
        }

        private void MixChecksum(int value)
        {
            unchecked
            {
                Checksum = (int)(((uint)Checksum ^ (uint)value) * 16777619u);
            }
        }

        private void BuildBoundary()
        {
            Transform boundary = new GameObject("Boundary").transform;
            boundary.SetParent(generatedRoot, false);
            const float thickness = 5f;
            const float height = 60f;
            CreateBoundaryWall(boundary, new Vector3(-half - thickness * 0.5f, 0f, 0f), new Vector3(thickness, height, size + thickness * 2f));
            CreateBoundaryWall(boundary, new Vector3(half + thickness * 0.5f, 0f, 0f), new Vector3(thickness, height, size + thickness * 2f));
            CreateBoundaryWall(boundary, new Vector3(0f, 0f, -half - thickness * 0.5f), new Vector3(size + thickness * 2f, height, thickness));
            CreateBoundaryWall(boundary, new Vector3(0f, 0f, half + thickness * 0.5f), new Vector3(size + thickness * 2f, height, thickness));
        }

        private static void CreateBoundaryWall(Transform parent, Vector3 center, Vector3 dimensions)
        {
            GameObject wall = new GameObject("BoundaryWall");
            wall.transform.SetParent(parent, false);
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = dimensions;
        }

        private Vector3 FindBeachSpawnPoint()
        {
            // Search all coastal directions at quarter-cell intervals. Select a dry beach
            // sample around waist-high terrain instead of relying on a single ray that may skip it.
            const int angularSamples = 128;
            float radialStep = Mathf.Max(0.5f, cellSize * 0.25f);
            Vector3 best = Vector3.zero;
            float bestScore = float.PositiveInfinity;
            bool foundBeach = false;
            float preferredAngle = mountainAngle + Mathf.PI;

            for (int angleIndex = 0; angleIndex < angularSamples; angleIndex++)
            {
                float angle = angleIndex * (Mathf.PI * 2f / angularSamples);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float angularPenalty = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, preferredAngle * Mathf.Rad2Deg)) / 180f;

                for (float radius = half * 0.99f; radius >= 0f; radius -= radialStep)
                {
                    float x = direction.x * radius;
                    float z = direction.y * radius;
                    float height = GetHeightLocal(x, z);
                    if (height < 0.05f || height > activeSettings.beachMaxHeight)
                        continue;

                    Biome biome = GetBiomeLocal(x, z);
                    if (biome != Biome.Beach || SlopeNormalYLocal(x, z) < 0.88f)
                        continue;

                    float score = Mathf.Abs(height - 0.9f) + angularPenalty * 0.15f + radius * 0.0001f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = new Vector3(x, height, z);
                        foundBeach = true;
                    }
                }
            }

            if (foundBeach)
                return best;

            Debug.LogWarning("[Island] Không tìm được ô Beach đúng phân loại; dùng điểm đất khô gần mép đảo làm phương án dự phòng.", this);
            float fallbackBest = float.PositiveInfinity;
            for (int i = 0; i < 256; i++)
            {
                float angle = i * (Mathf.PI * 2f / 256f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (float radius = half * 0.98f; radius >= 0f; radius -= radialStep)
                {
                    Vector3 candidate = new Vector3(direction.x * radius, 0f, direction.y * radius);
                    candidate.y = GetHeightLocal(candidate.x, candidate.z);
                    if (candidate.y < 0.1f)
                        continue;
                    float score = Mathf.Abs(candidate.y - 1f) + radius * 0.0001f;
                    if (score < fallbackBest)
                    {
                        fallbackBest = score;
                        best = candidate;
                    }
                }
            }
            return best;
        }

        private void PlacePlayerAtSpawn()
        {
            if (player == null)
                return;

            Vector3 worldCenter = transform.position;
            Vector3 inward = worldCenter - SpawnPoint;
            inward.y = 0f;
            Quaternion rotation = inward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(inward.normalized, Vector3.up)
                : Quaternion.identity;

            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;
            player.SetPositionAndRotation(SpawnPoint + Vector3.up * 0.12f, rotation);
            if (characterController != null)
                characterController.enabled = true;
        }

        private void ClearGenerated()
        {
            if (generatedRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(generatedRoot.gameObject);
                else
                    DestroyImmediate(generatedRoot.gameObject);
            }

            DestroyGeneratedMeshes();
            generatedRoot = null;
            instanceRenderer = null;
            caveEntrances.Clear();
            treeObjects.Clear();
            rockObjects.Clear();
            heights = null;
            IsGenerated = false;
        }

        private void DestroyGeneratedMeshes()
        {
            foreach (Mesh mesh in generatedMeshes)
            {
                if (mesh == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(mesh);
                else
                    DestroyImmediate(mesh);
            }
            generatedMeshes.Clear();
        }
    }
}
