using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>Creates world-space objects: transforms and cameras.</summary>
    internal static class WorldFactory
    {
        /// <summary>A new object under <paramref name="parent"/> (scene root when null) at a local position.</summary>
        internal static GameObject Child(string name, Transform parent, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        /// <summary>Adds an orthographic camera clearing to a solid color.</summary>
        internal static Camera AddCamera(GameObject go, Color background)
        {
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            return camera;
        }
    }
}
