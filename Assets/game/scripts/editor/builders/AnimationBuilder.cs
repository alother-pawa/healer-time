using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// One AnimationClip per tag of every sheet in <see cref="ArtSpec"/> (frames of the tag's row at its frame time,
    /// looping or not, on a SpriteRenderer's sprite) and one Animator Controller per sheet with a state per tag, the first
    /// tag as default, under <c>data/animations/&lt;sheet&gt;/</c>. Clips are rewritten on every run and keep their GUIDs;
    /// a controller is created once and only gains states for new tags, so states and transitions a game adds survive.
    /// World sprites only: a UI Image would need its own clip binding.
    /// </summary>
    internal static class AnimationBuilder
    {
        private static readonly EditorCurveBinding SpriteBinding =
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");

        internal static void Build(ArtSpec spec)
        {
            foreach (SheetSpec sheet in spec.sheets)
            {
                string folder = $"{BuildPaths.Animations}/{sheet.Name}";
                BuildUtility.EnsureFolder(folder);
                Dictionary<string, Sprite> frames = AssetDatabase.LoadAllAssetsAtPath($"{BuildPaths.Sprites}/{sheet.path}.png")
                    .OfType<Sprite>()
                    .ToDictionary(sprite => sprite.name);
                var clips = sheet.tags.Select(tag => WriteClip($"{folder}/{sheet.Name}_{tag.name}.anim", sheet, tag, frames)).ToList();
                WriteController($"{folder}/{sheet.Name}.controller", sheet, clips);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>The controller of a sheet (built by <see cref="Build"/>).</summary>
        internal static string ControllerPath(SheetSpec sheet)
        {
            return $"{BuildPaths.Animations}/{sheet.Name}/{sheet.Name}.controller";
        }

        // Keys at every frame start, plus the last frame again at the end so it shows for its full time.
        private static AnimationClip WriteClip(string path, SheetSpec sheet, TagSpec tag, Dictionary<string, Sprite> frames)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            float frameSeconds = tag.frameMs / 1000f;
            clip.frameRate = 1f / frameSeconds;
            var keys = new ObjectReferenceKeyframe[tag.frames + 1];
            for (int i = 0; i <= tag.frames; i++)
            {
                string name = ArtImport.FrameName(sheet, tag, Mathf.Min(i, tag.frames - 1));
                if (!frames.TryGetValue(name, out Sprite sprite))
                {
                    throw new System.InvalidOperationException($"Sprite {name} missing from {sheet.path}.png; was the sheet sliced?");
                }

                keys[i] = new ObjectReferenceKeyframe { time = i * frameSeconds, value = sprite };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = tag.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void WriteController(string path, SheetSpec sheet, List<AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                                            ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            for (int i = 0; i < clips.Count; i++)
            {
                AnimationClip clip = clips[i];
                if (machine.states.Any(child => child.state.motion == clip))
                {
                    continue;
                }

                AnimatorState state = machine.AddState(sheet.tags[i].name);
                state.motion = clip;
                if (machine.defaultState == null)
                {
                    machine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
        }
    }
}
