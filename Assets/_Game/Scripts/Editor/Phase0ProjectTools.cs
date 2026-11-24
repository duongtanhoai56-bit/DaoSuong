using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DaoSuong.Editor
{
    public static class Phase0ProjectTools
    {
        private const string GameRoot = "Assets/_Game";

        [Serializable]
        private sealed class AssemblyDefinitionSummary
        {
            public string name;
            public string[] references;
        }

        [MenuItem("Đảo Sương/Phase 1/Validate Project Structure")]
        public static void ValidateProjectStructure()
        {
            var asmdefGuids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { GameRoot });
            var names = new HashSet<string>(StringComparer.Ordinal);
            var summaries = new List<AssemblyDefinitionSummary>();
            var errors = 0;

            foreach (var guid in asmdefGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var json = File.ReadAllText(path);
                var summary = JsonUtility.FromJson<AssemblyDefinitionSummary>(json);
                if (summary == null || string.IsNullOrWhiteSpace(summary.name))
                {
                    Debug.LogError($"[Phase0] Invalid asmdef: {path}");
                    errors++;
                    continue;
                }

                if (!names.Add(summary.name))
                {
                    Debug.LogError($"[Phase0] Duplicate assembly name: {summary.name}");
                    errors++;
                }
                summaries.Add(summary);
            }

            // Only validate project-owned assembly references. Package assemblies such as
            // UnityEngine.UI and Unity.InputSystem are resolved by Unity Package Manager.
            foreach (var assembly in summaries)
            {
                if (assembly.references == null) continue;
                foreach (var reference in assembly.references)
                {
                    if (reference.StartsWith("Game.", StringComparison.Ordinal) && !names.Contains(reference))
                    {
                        Debug.LogError($"[Phase0] {assembly.name} references missing project assembly: {reference}");
                        errors++;
                    }
                }
            }

            string[] expected = { "Game.Core", "Game.Gameplay", "Game.UI", "Game.Network", "Game.Gameplay.EditModeTests" };
            foreach (string required in expected)
            {
                if (!names.Contains(required))
                {
                    Debug.LogError($"[Phase0] Required assembly is missing: {required}");
                    errors++;
                }
            }
            if (summaries.Count != expected.Length)
                Debug.LogWarning($"[Phase0] Found {summaries.Count} asmdefs; the combined Phase 0–9 project expects {expected.Length} assemblies (4 runtime + 1 EditMode tests). Check for an unexpected or missing assembly definition.");

            if (!File.Exists("Packages/manifest.json"))
            {
                Debug.LogError("[Phase0] Packages/manifest.json is missing.");
                errors++;
            }

            if (errors == 0)
                Debug.Log($"[DaoSuong] Structure OK: {summaries.Count} asmdefs; project-owned references resolved. This does not replace a Unity compile/build test.");
            else
                Debug.LogError($"[DaoSuong] Structure validation found {errors} error(s).");
        }

        [MenuItem("Đảo Sương/Phase 0/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            const string folder = "Assets/_Game/Scenes/Bootstrap";
            const string scenePath = folder + "/SCN_Bootstrap.unity";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes"))
                    AssetDatabase.CreateFolder(GameRoot, "Scenes");
                AssetDatabase.CreateFolder("Assets/_Game/Scenes", "Bootstrap");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool alreadyListed = false;
            foreach (var buildScene in buildScenes)
            {
                if (string.Equals(buildScene.path, scenePath, StringComparison.Ordinal))
                {
                    alreadyListed = true;
                    break;
                }
            }
            if (!alreadyListed)
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.Refresh();
            Debug.Log($"[Phase0] Created default camera/light bootstrap scene at {scenePath}.");
        }
    }
}
