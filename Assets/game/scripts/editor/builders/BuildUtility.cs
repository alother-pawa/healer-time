using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Shared helpers of the builders: folders, create-or-update assets, required asset lookups (sprites, prefabs),
    /// hex colors and prefab-instance override recording. Every lookup throws with the missing path.
    /// </summary>
    internal static class BuildUtility
    {
        private const string Separator = "/";

        /// <summary>Creates every missing folder of an asset path such as <c>Assets/game/data/ui</c>.</summary>
        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int split = folder.LastIndexOf(Separator, StringComparison.Ordinal);
            string parent = folder.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(split + 1));
        }

        /// <summary>Folder part of an asset path.</summary>
        internal static string FolderOf(string assetPath)
        {
            return assetPath.Substring(0, assetPath.LastIndexOf(Separator, StringComparison.Ordinal));
        }

        /// <summary>
        /// Loads the ScriptableObject at <paramref name="path"/>, creating it when missing. An existing asset keeps its GUID
        /// and the values the caller does not overwrite.
        /// </summary>
        internal static ScriptableObject Asset(Type type, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null)
            {
                if (existing.GetType() != type)
                {
                    throw new InvalidOperationException(
                        $"{path} is a {existing.GetType().Name}, expected {type.Name}. Delete it and build again.");
                }

                return existing;
            }

            EnsureFolder(FolderOf(path));
            ScriptableObject created = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        /// <summary>Typed form of <see cref="Asset(Type, string)"/> for public types.</summary>
        internal static T Asset<T>(string path) where T : ScriptableObject
        {
            return (T)Asset(typeof(T), path);
        }

        /// <summary>Loads an asset that an earlier step must have created.</summary>
        internal static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null
                ? asset
                : throw new InvalidOperationException($"Missing {typeof(T).Name} at {path}. Run the earlier build steps first.");
        }

        /// <summary>
        /// A placeholder sprite from <c>art/sprites</c> by its path without extension (for example
        /// <c>loading/loading-background</c>).
        /// </summary>
        internal static Sprite Sprite(string path)
        {
            string assetPath = $"{BuildPaths.Sprites}/{path}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            return sprite != null
                ? sprite
                : throw new InvalidOperationException(
                    $"Sprite {assetPath} not found. Run Tools/ShapeGen/generate_shapes.py, then 'Project Settings' "
                    + "(sprite import).");
        }

        /// <summary>A component of type <paramref name="type"/> on the root of the prefab at <paramref name="path"/>.</summary>
        internal static Component PrefabComponent(string path, Type type)
        {
            Component component = Load<GameObject>(path).GetComponent(type);
            return component != null
                ? component
                : throw new InvalidOperationException($"Prefab {path} has no {type.Name} on its root.");
        }

        /// <summary>A component that must be on <paramref name="target"/>.</summary>
        internal static Component Require(GameObject target, Type type)
        {
            Component component = target.GetComponent(type);
            return component != null
                ? component
                : throw new InvalidOperationException($"{target.name} has no {type.Name}.");
        }

        /// <summary>Parses <c>#RRGGBB</c> with an optional alpha.</summary>
        internal static Color Hex(string hex, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException($"Invalid color '{hex}'.");
            }

            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Records direct property changes on objects that belong to a prefab instance (nested prefabs, variants, scene
        /// instances); without this, Unity drops them when the instance is saved.
        /// </summary>
        internal static void RecordOverrides(params Object[] targets)
        {
            foreach (Object target in targets)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
            }
        }

        /// <summary>Instantiates a prefab asset as a connected instance under <paramref name="parent"/>.</summary>
        internal static GameObject Instantiate(string prefabPath, Transform parent, string name)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(prefabPath), parent);
            instance.name = name;
            RecordOverrides(instance);
            return instance;
        }

        /// <summary>Child transform at <paramref name="path"/> under <paramref name="root"/>, or an error naming it.</summary>
        internal static Transform Child(Transform root, string path)
        {
            Transform child = root.Find(path);
            return child != null ? child : throw new InvalidOperationException($"{root.name} has no child '{path}'.");
        }

        /// <summary>Saves <paramref name="root"/> as the prefab at <paramref name="path"/> and destroys the scene copy.</summary>
        internal static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolder(FolderOf(path));
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            Object.DestroyImmediate(root);
            if (!success)
            {
                throw new InvalidOperationException($"Could not save prefab {path}.");
            }

            return saved;
        }
    }
}
