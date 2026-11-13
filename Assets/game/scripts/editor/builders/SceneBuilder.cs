using System;
using System.Linq;
using Game.Contracts.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Builds the scenes from empty scenes and saves them over any existing file (GUIDs are kept), then opens Boot.
    /// Scenes hold prefab instances plus the objects that only make sense in one scene (cameras, entries, the audio
    /// listener); scene-only references between them are wired here. Flow: Boot loads UI (persistent), then Splash, which
    /// goes to MainMenu (to FirstLaunch first, once per install); New game and Continue go to the Gameplay demo.
    /// Every scene has the same root groups (Docs/STRUCTURE.md), kept even when empty so the place of a new object is
    /// obvious; objects that must stay at the scene root (DontDestroyOnLoad) are created at runtime, never here:
    /// <code>
    /// Systems   scene entries and controllers, the EventSystem
    /// Cameras   cameras
    /// World     gameplay scenes only: Environment, Characters, Props, Effects
    /// UI        canvas prefabs
    /// Audio     the audio listener, ambience
    /// </code>
    /// </summary>
    internal static class SceneBuilder
    {
        private const string MainCameraTag = "MainCamera";
        private static readonly string[] WorldGroups = { "Environment", "Characters", "Props", "Effects" };

        /// <summary>Builds UI, Boot, Splash, MainMenu, Gameplay and FirstLaunch.</summary>
        internal static void Build()
        {
            BuildUI();
            BuildBoot();
            BuildContent(BuildPaths.SceneSplash, "SplashCamera", BuildPaths.SplashCanvas, "SplashCanvas", false);
            BuildContent(BuildPaths.SceneMainMenu, "MenuCamera", BuildPaths.MainMenuCanvas, "MainMenuCanvas", false);
            BuildContent(BuildPaths.SceneGameplay, "GameplayCamera", BuildPaths.GameplayCanvas, "GameplayCanvas", true);
            BuildContent(BuildPaths.SceneFirstLaunch, "FirstLaunchCamera", BuildPaths.FirstLaunchCanvas, "FirstLaunchCanvas", false);
            BuildArtPreview(ArtSpec.Load());
            EditorSceneManager.OpenScene(BuildPaths.SceneBoot);
        }

        /// <summary>Replaces the open scene with a new empty one.</summary>
        internal static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        /// <summary>Saves <paramref name="scene"/> at <paramref name="path"/>, replacing the file (its GUID is kept).</summary>
        internal static void Save(Scene scene, string path)
        {
            BuildUtility.EnsureFolder(BuildPaths.Scenes);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException($"Could not save scene {path}.");
            }
        }

        /// <summary>A connected instance of the prefab at <paramref name="prefab"/> under <paramref name="parent"/>.</summary>
        internal static GameObject Place(string prefab, string name, Transform parent)
        {
            return BuildUtility.Instantiate(prefab, parent, name);
        }

        /// <summary>The root groups of the open scene (World only when <paramref name="world"/>), with World's own groups.</summary>
        internal static SceneGroups AddGroups(bool world)
        {
            Transform systems = WorldFactory.Child("Systems", null).transform;
            Transform cameras = WorldFactory.Child("Cameras", null).transform;
            if (world)
            {
                Transform worldRoot = WorldFactory.Child("World", null).transform;
                foreach (string group in WorldGroups)
                {
                    WorldFactory.Child(group, worldRoot);
                }
            }

            Transform ui = WorldFactory.Child("UI", null).transform;
            Transform audio = WorldFactory.Child("Audio", null).transform;
            return new SceneGroups(systems, cameras, ui, audio);
        }

        // Loads the persistent UI scene, then switches to the splash. Its camera keeps the Game view drawn until then; its
        // audio listener covers the frames before the UI scene's listener exists (BootSceneEntry switches it off after).
        private static void BuildBoot()
        {
            Scene scene = NewScene();
            SceneGroups groups = AddGroups(false);
            GameObject bootCamera = WorldFactory.Child("BootCamera", groups.Cameras, new Vector3(0f, 0f, -10f));
            WorldFactory.AddCamera(bootCamera, Palette.Night);
            var listener = WorldFactory.Child("BootAudioListener", groups.Audio).AddComponent<AudioListener>();
            Component entry = WorldFactory.Child("BootEntry", groups.Systems).AddComponent(GameTypes.BootSceneEntry);
            Fields.Set(entry, ("_persistentUiScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyUI)),
                ("_firstScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeySplash)), ("_bootListener", listener));
            Save(scene, BuildPaths.SceneBoot);
        }

        // Persistent UI scene: the game's only EventSystem and AudioListener, and one prefab per canvas.
        private static void BuildUI()
        {
            Scene scene = NewScene();
            SceneGroups groups = AddGroups(false);
            Place(BuildPaths.EventSystem, "EventSystem", groups.Systems);
            WorldFactory.Child("AudioListener", groups.Audio).AddComponent<AudioListener>();
            Place(BuildPaths.UIRoot, "UIRoot", groups.UI);
            Place(BuildPaths.ToastCanvas, "ToastCanvas", groups.UI);
            Place(BuildPaths.LoadingCanvas, "LoadingCanvas", groups.UI);
            Save(scene, BuildPaths.SceneUI);
        }

        // Every world image of the art spec (game/...) side by side at its pivot on the ground line, sheets playing their
        // first tag, in the World group of its kind; an editor-only scene (not in Build Settings) to check sizes, pivots
        // and animations of placeholders or final art.
        private static void BuildArtPreview(ArtSpec spec)
        {
            Scene scene = NewScene();
            SceneGroups groups = AddGroups(true);
            GameObject cameraObject = WorldFactory.Child("PreviewCamera", groups.Cameras, new Vector3(0f, 1.5f, -10f));
            WorldFactory.AddCamera(cameraObject, Palette.Night).orthographicSize = 3f;
            cameraObject.tag = MainCameraTag;
            Transform world = GameObject.Find("World").transform;

            float x = 0f;
            foreach (SpriteSpec image in spec.sprites.Where(image => image.path.StartsWith("game/", StringComparison.Ordinal)))
            {
                var sprite = BuildUtility.Load<Sprite>($"{BuildPaths.Sprites}/{image.path}.png");
                Place(world, image.path, sprite, ref x, (float)image.width / spec.pixelsPerUnit, null);
            }

            foreach (SheetSpec sheet in spec.sheets)
            {
                Sprite first = AssetDatabase.LoadAllAssetsAtPath($"{BuildPaths.Sprites}/{sheet.path}.png").OfType<Sprite>()
                    .FirstOrDefault(sprite => sprite.name == ArtImport.FrameName(sheet, sheet.tags[0], 0));
                var controller = BuildUtility.Load<RuntimeAnimatorController>(AnimationBuilder.ControllerPath(sheet));
                Place(world, sheet.path, first, ref x, (float)sheet.frameWidth / spec.pixelsPerUnit, controller);
            }

            Save(scene, BuildPaths.SceneArtPreview);
        }

        // One image under the World group named after its folder (characters, props, effects), left to right with a gap.
        private static void Place(Transform world, string path, Sprite sprite, ref float x, float width, RuntimeAnimatorController controller)
        {
            string[] parts = path.Split('/');
            string kind = parts.Length > 1 ? char.ToUpperInvariant(parts[1][0]) + parts[1].Substring(1) : "Props";
            Transform group = world.Find(kind) != null ? world.Find(kind) : world.Find("Props");
            GameObject go = WorldFactory.Child(parts[parts.Length - 1], group, new Vector3(x + width / 2f, 0f, 0f));
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            if (controller != null)
            {
                go.AddComponent<Animator>().runtimeAnimatorController = controller;
            }

            x += width + 0.5f;
        }

        // A content scene: its camera (no audio listener: the UI scene owns the only one) and its canvas prefab; gameplay
        // scenes also get the World groups.
        private static void BuildContent(string path, string cameraName, string canvasPrefab, string canvasName, bool world)
        {
            Scene scene = NewScene();
            SceneGroups groups = AddGroups(world);
            GameObject cameraObject = WorldFactory.Child(cameraName, groups.Cameras, new Vector3(0f, 0f, -10f));
            WorldFactory.AddCamera(cameraObject, Palette.Night);
            cameraObject.tag = MainCameraTag;
            Place(canvasPrefab, canvasName, groups.UI);
            Save(scene, path);
        }
    }

    /// <summary>The root groups every scene has (see <see cref="SceneBuilder"/>).</summary>
    internal readonly struct SceneGroups
    {
        internal SceneGroups(Transform systems, Transform cameras, Transform ui, Transform audio)
        {
            Systems = systems;
            Cameras = cameras;
            UI = ui;
            Audio = audio;
        }

        internal Transform Systems { get; }

        internal Transform Cameras { get; }

        internal Transform UI { get; }

        internal Transform Audio { get; }
    }
}
