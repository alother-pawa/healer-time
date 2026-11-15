using Game.Foundation.InputPrompts;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.UI.Navigation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Builds every prefab under <c>Assets/game/prefabs</c> in dependency order (nested prefabs before the prefabs that
    /// contain them), then the configs that point at popup prefabs. Objects are assembled in a scratch scene and saved over
    /// any existing prefab, which keeps its GUID. Object ids inside an overwritten prefab are not guaranteed to survive, so
    /// the containers and scenes that use them are rebuilt in the same step.
    /// </summary>
    internal static class PrefabBuilder
    {
        private const float ToastLabelSize = 34f;
        private const float ToastLabelMinSize = 22f;
        private const float ToastLabelInset = 20f;
        private const float GlyphSize = 48f;
        private const float GlyphTextSize = 24f;
        private const float PromptLabelSize = 28f;
        private const float PromptSpacing = 8f;
        private const float PromptBarSpacing = 40f;
        private const int PromptBarPadding = 48;

        /// <summary>The prompts of <c>PromptBar</c>: child name, actions and label key.</summary>
        internal static class Prompts
        {
            internal const string Confirm = "ConfirmPrompt";
            internal const string Back = "BackPrompt";
            internal const string SwitchTab = "SwitchTabPrompt";
        }

        private static readonly (string name, string[] actions, string labelKey)[] PromptBarEntries =
        {
            (Prompts.Confirm, new[] { "UI/Submit" }, "prompt.confirm"),
            (Prompts.Back, new[] { "UI/Cancel" }, "prompt.back"),
            (Prompts.SwitchTab, new[] { "Menu/TabPrevious", "Menu/TabNext" }, "prompt.switch-tab"),
        };

        /// <summary>Builds all prefabs and the configs referencing them. Replaces the open scene with an unsaved scratch scene.</summary>
        internal static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildToastBase();
            BuildInputPrompt();
            BuildPromptBar();
            ShellPrefabBuilder.Build();
            PopupPrefabBuilder.Build();
            MenuPrefabBuilder.Build();
            AssetBuilder.BuildPrefabConfigs();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// A <c>PromptBar</c> instance stretched along the bottom of <paramref name="parent"/>; prompts named in
        /// <paramref name="hidden"/> are deactivated (instance overrides).
        /// </summary>
        internal static GameObject PromptBarInstance(RectTransform parent, float y, float height, params string[] hidden)
        {
            GameObject bar = BuildUtility.Instantiate(BuildPaths.PromptBar, parent, "PromptBar");
            ((RectTransform)bar.transform).BottomStretch(y, height);
            BuildUtility.RecordOverrides(bar.transform);
            foreach (string prompt in hidden)
            {
                GameObject child = BuildUtility.Child(bar.transform, prompt).gameObject;
                child.SetActive(false);
                BuildUtility.RecordOverrides(child);
            }

            return bar;
        }

        /// <summary>Replaces the label key of a prompt of a <c>PromptBar</c> instance (instance override).</summary>
        internal static void SetPromptLabel(GameObject bar, string prompt, string key)
        {
            Transform label = BuildUtility.Child(bar.transform, prompt + "/Label");
            var text = label.GetComponent<TextMeshProUGUI>();
            var localized = label.GetComponent<LocalizedLabel>();
            text.text = key;
            Fields.Set(localized, ("_key", key));
            BuildUtility.RecordOverrides(text, localized);
        }

        // Toasts never take focus: if a toast is ever shown on the UI manager's popup stack it leaves the selection where it is.
        private static void BuildToastBase()
        {
            RectTransform root = UiFactory.Rect("Toast_Base", null).Stretch();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var toast = root.gameObject.AddComponent<ToastBase>();
            root.gameObject.AddComponent<NonModalPopup>();

            RectTransform body = UiFactory.Rect("Body", root).Stretch();
            Image background = UiFactory.AddImage(UiFactory.Rect("Background", body).Stretch(),
                BuildUtility.Sprite("ui-common/toast-background"));
            background.raycastTarget = false;

            RectTransform labelRect = UiFactory.Rect("Label", body)
                .Stretch(ToastLabelInset, ToastLabelInset, ToastLabelInset, ToastLabelInset);
            TextMeshProUGUI label = UiFactory.AddText(labelRect, string.Empty, ToastLabelSize, Palette.Text);
            label.enableAutoSizing = true;
            label.fontSizeMin = ToastLabelMinSize;
            label.fontSizeMax = ToastLabelSize;
            label.raycastTarget = false;
            label.Paragraph();

            Fields.Set(toast, ("_canvasGroup", group), ("_body", body), ("_label", label),
                ("_animation", BuildUtility.Load<ToastAnimationConfig>(BuildPaths.ToastAnimation)));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.ToastBase);
        }

        // Two glyph slots and a label in a row that sizes itself; instances set the actions and the label key.
        private static void BuildInputPrompt()
        {
            RectTransform root = UiFactory.Rect("InputPrompt", null).Place(UiFactory.Center, Vector2.zero, new Vector2(240f, GlyphSize));
            UiFactory.AddRow(root.gameObject, PromptSpacing, TextAnchor.MiddleLeft);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            var prompt = root.gameObject.AddComponent<InputPrompt>();

            InputGlyphView first = GlyphSlot(root, "Glyph1");
            InputGlyphView second = GlyphSlot(root, "Glyph2");
            TextMeshProUGUI label = UiFactory.AddText(UiFactory.Rect("Label", root), string.Empty, PromptLabelSize, Palette.TextMuted,
                TextAlignmentOptions.MidlineLeft);
            label.raycastTarget = false;
            label.Localize("prompt.confirm");

            Fields.Set(prompt, ("_actions", new[] { "UI/Submit" }), ("_glyphs", new Object[] { first, second }),
                ("_label", label.gameObject), ("_layoutElement", layout));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.InputPrompt);
        }

        /// <summary>A glyph slot (icon with a text over it) sized by a <c>LayoutElement</c>, as in the <c>InputPrompt</c> prefab.</summary>
        internal static InputGlyphView GlyphSlot(RectTransform parent, string name)
        {
            RectTransform slot = UiFactory.Rect(name, parent).Place(UiFactory.MiddleLeft, Vector2.zero, new Vector2(GlyphSize, GlyphSize));
            var layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = GlyphSize;
            layout.preferredHeight = GlyphSize;
            Image icon = UiFactory.AddImage(UiFactory.Rect("Icon", slot).Stretch(), BuildUtility.Sprite("ui-glyphs/key"));
            icon.raycastTarget = false;
            TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect("Text", slot).Stretch(), string.Empty, GlyphTextSize, Palette.White);
            text.raycastTarget = false;
            text.fontStyle = FontStyles.Bold;

            var view = slot.gameObject.AddComponent<InputGlyphView>();
            Fields.Set(view, ("_icon", icon), ("_text", text), ("_layout", layout),
                ("_keyFrame", BuildUtility.Sprite("ui-glyphs/key")), ("_height", GlyphSize));
            return view;
        }

        // Confirm, Back and Switch tab, right-aligned; screens hide the prompts they do not use.
        private static void BuildPromptBar()
        {
            RectTransform root = UiFactory.Rect("PromptBar", null).BottomStretch(0f, 72f);
            HorizontalLayoutGroup row = UiFactory.AddRow(root.gameObject, PromptBarSpacing, TextAnchor.MiddleRight);
            row.padding = new RectOffset(PromptBarPadding, PromptBarPadding, 0, 0);

            foreach ((string name, string[] actions, string labelKey) in PromptBarEntries)
            {
                AddPrompt(root, name, actions, labelKey);
            }

            BuildUtility.SavePrefab(root.gameObject, BuildPaths.PromptBar);
        }

        /// <summary>
        /// An <c>InputPrompt</c> instance under <paramref name="parent"/> showing <paramref name="actions"/> and the label key
        /// <paramref name="labelKey"/> (instance overrides).
        /// </summary>
        internal static GameObject AddPrompt(Transform parent, string name, string[] actions, string labelKey)
        {
            GameObject instance = BuildUtility.Instantiate(BuildPaths.InputPrompt, parent, name);
            var prompt = instance.GetComponent<InputPrompt>();
            Fields.Set(prompt, ("_actions", actions));
            Transform label = BuildUtility.Child(instance.transform, "Label");
            var text = label.GetComponent<TextMeshProUGUI>();
            var localized = label.GetComponent<LocalizedLabel>();
            text.text = labelKey;
            Fields.Set(localized, ("_key", labelKey));
            BuildUtility.RecordOverrides(prompt, text, localized);
            return instance;
        }

        /// <summary>A right-aligned row of prompts stretched along the bottom of <paramref name="parent"/> (same spacing as <c>PromptBar</c>).</summary>
        internal static RectTransform PromptRow(RectTransform parent, string name, float y, float height)
        {
            RectTransform row = UiFactory.Rect(name, parent).BottomStretch(y, height);
            HorizontalLayoutGroup layout = UiFactory.AddRow(row.gameObject, PromptBarSpacing, TextAnchor.MiddleRight);
            layout.padding = new RectOffset(PromptBarPadding, PromptBarPadding, 0, 0);
            return row;
        }
    }
}
