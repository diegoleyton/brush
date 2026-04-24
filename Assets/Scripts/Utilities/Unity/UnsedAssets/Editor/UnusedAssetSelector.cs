#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Flowbit.Utilities.Unity.UnusedAssets
{
    public static class UnusedAssetSelector
    {
        [MenuItem("Tools/Unused Assets/Select Unused AudioClips")]
        private static void SelectUnusedAudioClips()
        {
            SelectUnusedAssetsOfType<AudioClip>();
        }

        [MenuItem("Tools/Unused Assets/Select Unused Textures")]
        private static void SelectUnusedTextures()
        {
            SelectUnusedAssetsOfType<Texture2D>();
        }

        [MenuItem("Tools/Unused Assets/Select Unused Sprites")]
        private static void SelectUnusedSprites()
        {
            SelectUnusedAssetsOfType<Sprite>();
        }

        private static void SelectUnusedAssetsOfType<T>() where T : Object
        {
            var candidateGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" });
            var candidatePaths = new HashSet<string>(
                candidateGuids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(IsRealAssetFile)
            );

            var rootPaths = GetRootAssetPaths();
            var usedPaths = new HashSet<string>();

            int totalRoots = rootPaths.Count;
            for (int i = 0; i < totalRoots; i++)
            {
                string root = rootPaths[i];

                var deps = AssetDatabase.GetDependencies(root, true);
                foreach (var dep in deps)
                {
                    if (candidatePaths.Contains(dep))
                        usedPaths.Add(dep);
                }

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Scanning dependencies",
                        $"{i}/{totalRoots} roots scanned",
                        totalRoots == 0 ? 1f : (float)i / totalRoots);
                }
            }

            EditorUtility.ClearProgressBar();

            var unusedPaths = candidatePaths
                .Where(path => !usedPaths.Contains(path))
                .OrderBy(path => path)
                .ToList();

            var unusedObjects = unusedPaths
                .Select(path => AssetDatabase.LoadAssetAtPath<T>(path))
                .Where(obj => obj != null)
                .Cast<Object>()
                .ToArray();

            Selection.objects = unusedObjects;

            Debug.Log(
                $"Unused {typeof(T).Name}: {unusedObjects.Length}\n" +
                string.Join("\n", unusedPaths));

            if (unusedObjects.Length > 0)
                EditorGUIUtility.PingObject(unusedObjects[0]);
        }

        private static List<string> GetRootAssetPaths()
        {
            var roots = new HashSet<string>();

            AddAssetsOfType("t:Scene", roots);
            AddAssetsOfType("t:Prefab", roots);
            AddAssetsOfType("t:ScriptableObject", roots);
            AddAssetsOfType("t:Material", roots);
            AddAssetsOfType("t:AnimatorController", roots);

            return roots
                .Where(IsRealAssetFile)
                .OrderBy(p => p)
                .ToList();
        }

        private static void AddAssetsOfType(string filter, HashSet<string> result)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsRealAssetFile(path))
                    result.Add(path);
            }
        }

        private static bool IsRealAssetFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!path.StartsWith("Assets/"))
                return false;

            if (AssetDatabase.IsValidFolder(path))
                return false;

            if (!File.Exists(path))
                return false;

            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".cs" ||
                extension == ".js" ||
                extension == ".boo" ||
                extension == ".meta" ||
                extension == ".asmdef" ||
                extension == ".asmref")
            {
                return false;
            }

            return true;
        }
    }
}
#endif