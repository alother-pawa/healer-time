using System;
using Game.Foundation.UI;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Objects of the persistent UI scene, one prefab each: the EventSystem (the game's only one, driven by the UI map of
    /// the game's input actions), the root canvas handed to the UI manager (with the selection highlight drawn above its
    /// popups), the toast canvas (with a nested <c>Toast_Base</c>) and the loading screen. Sort orders keep the canvases
    /// stacked: root 100, toast 200, loading 300. The root and toast canvases follow the player's UI scale
    /// (<see cref="CanvasUIScale"/>); the loading screen keeps its reference size.
    /// </summary>
    internal static class ShellPrefabBuilder
    {
        /// <summary>Sort order of the root canvas handed to the UI manager (also the UI manager's own canvas, see UISettings).</summary>
        internal const int UiRootSortOrder = 100;
        private const int ToastSortOrder = 200;
        private const int LoadingSortOrder = 300;
        private const float ProgressFillInset = 4f;

        // Hold a direction this long before navigation repeats, then repeat at this interval (seconds).
        private const float MoveRepeatDelay = 0.4f;
        private const float MoveRepeatRate = 0.1f;

        internal static void Build()
        {
            BuildEventSystem();
            BuildUIRoot();
            BuildToastCanvas();
            BuildLoadingCanvas();
        }

        // The module gets the asset Core loads, so binding overrides saved by Core apply to UI navigation too. Its actions
        // are assigned by name at runtime (UIInputModuleActions): action references saved in the prefab came back null.
        // The build fails here when the asset lacks one of those actions. A click on empty space keeps the selection, so
        // keyboard and gamepad navigation can continue after a mouse click.
        private static void BuildEventSystem()
        {
            var root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = root.GetComponent<InputSystemUIInputModule>();
            var asset = BuildUtility.Load<InputActionAsset>(BuildPaths.InputActions);
            module.actionsAsset = asset;
            module.deselectOnBackgroundClick = false;
            module.moveRepeatDelay = MoveRepeatDelay;
            module.moveRepeatRate = MoveRepeatRate;
            var actions = root.AddComponent<UIInputModuleActions>();
            foreach (string action in actions.ActionNames)
            {
                if (asset.FindAction(action) == null)
                {
                    throw new InvalidOperationException($"{BuildPaths.InputActions} has no action {action}.");
                }
            }

            BuildUtility.SavePrefab(root, BuildPaths.EventSystem);
        }

        private static void BuildUIRoot()
        {
            RectTransform uiRoot = UiFactory.Rect("UIRoot", null);
            UiFactory.AddCanvas(uiRoot.gameObject, UiRootSortOrder, true);
            uiRoot.gameObject.AddComponent<CanvasUIScale>();
            RectTransform hudLayer = UiFactory.Rect("HudLayer", uiRoot).Stretch();
            RectTransform popupLayer = UiFactory.Rect("PopupLayer", uiRoot).Stretch();
            AddSelectionHighlight(uiRoot);
            Fields.Set(uiRoot.gameObject.AddComponent(GameTypes.UIShellRoot), ("_hudLayer", hudLayer), ("_popupLayer", popupLayer));
            BuildUtility.SavePrefab(uiRoot.gameObject, BuildPaths.UIRoot);
        }

        /// <summary>
        /// Adds the keyboard/gamepad focus frame as the last child of a root canvas, so it draws above everything in it. The
        /// UI navigation package's <see cref="SelectionHighlight"/> moves the frame over the selected control of this canvas only
        /// (each root canvas with selectable controls needs its own) and hides it in Pointer mode.
        /// </summary>
        internal static void AddSelectionHighlight(RectTransform canvas)
        {
            RectTransform holder = UiFactory.Rect("SelectionHighlight", canvas).Stretch();
            RectTransform frame = UiFactory.Rect("FocusFrame", holder).Place(UiFactory.Center, Vector2.zero, new Vector2(200f, 80f));
            UiFactory.AddImage(frame, BuildUtility.Sprite("ui-common/focus-frame")).raycastTarget = false;
            frame.gameObject.SetActive(false);
            Fields.Set(holder.gameObject.AddComponent<SelectionHighlight>(), ("_frame", frame));
        }

        private static void BuildToastCanvas()
        {
            RectTransform toastCanvas = UiFactory.Rect("ToastCanvas", null);
            UiFactory.AddCanvas(toastCanvas.gameObject, ToastSortOrder, false);
            toastCanvas.gameObject.AddComponent<CanvasUIScale>();
            RectTransform toastHost = UiFactory.Rect("ToastHost", toastCanvas)
                .Place(UiFactory.BottomCenter, new Vector2(0f, 120f), new Vector2(720f, 96f));
            GameObject toast = BuildUtility.Instantiate(BuildPaths.ToastBase, toastHost, "Toast");
            ((RectTransform)toast.transform).Stretch();
            BuildUtility.RecordOverrides(toast.transform);
            Fields.Set(toastHost.gameObject.AddComponent(GameTypes.ToastHost), ("_toast", toast.GetComponent<ToastBase>()));
            BuildUtility.SavePrefab(toastCanvas.gameObject, BuildPaths.ToastCanvas);
        }

        private static void BuildLoadingCanvas()
        {
            RectTransform canvas = UiFactory.Rect("LoadingCanvas", null);
            UiFactory.AddCanvas(canvas.gameObject, LoadingSortOrder, true);

            RectTransform screen = UiFactory.Rect("LoadingScreen", canvas).Stretch();
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Component transition = screen.gameObject.AddComponent(GameTypes.LoadingTransition);

            UiFactory.AddImage(UiFactory.Rect("Background", screen).Stretch(), BuildUtility.Sprite("backgrounds/loading-background"));

            // Title, bar and caption; hidden for a plain transition (e.g. into the splash).
            RectTransform details = UiFactory.Rect("Details", screen).Stretch();
            UiFactory.AddText(UiFactory.Rect("Title", details).Place(UiFactory.Center, new Vector2(0f, 120f), new Vector2(1200f, 140f)),
                string.Empty, 88f, Palette.TextLight).Localize("game.title");
            // Track and mask are thin rounded sprites drawn Sliced; the square fill is Filled horizontally and clipped to the
            // mask's rounded ends, so the bar keeps round ends at any progress.
            RectTransform bar = UiFactory.Rect("ProgressBar", details).Place(UiFactory.Center, new Vector2(0f, -120f), new Vector2(800f, 28f));
            UiFactory.AddImage(bar, BuildUtility.Sprite("screens/loading/loading-bar-track"));
            RectTransform fillArea = UiFactory.Rect("FillArea", bar)
                .Stretch(ProgressFillInset, ProgressFillInset, ProgressFillInset, ProgressFillInset);
            UiFactory.AddImage(fillArea, BuildUtility.Sprite("screens/loading/loading-bar-mask")).raycastTarget = false;
            fillArea.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Image fill = UiFactory.AddImage(UiFactory.Rect("Fill", fillArea).Stretch(), BuildUtility.Sprite("screens/loading/loading-bar-fill"));
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            UiFactory.AddText(UiFactory.Rect("Caption", details).Place(UiFactory.Center, new Vector2(0f, -180f), new Vector2(800f, 60f)),
                string.Empty, 36f, Palette.TextLight).Localize("loading.label");

            Fields.Set(transition, ("_group", group), ("_details", details.gameObject), ("_progressFill", fill),
                ("_config", BuildUtility.Load<ScriptableObject>(BuildPaths.LoadingTransition)));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.LoadingCanvas);
        }
    }
}
