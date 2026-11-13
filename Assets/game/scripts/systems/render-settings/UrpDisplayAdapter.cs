using Paowa.Core.Foundation.Bootstrap;
using Paowa.Desktop;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Systems.RenderSettings
{
    /// <summary>
    /// Puts the display service's render scale and brightness hooks into effect on Universal RP.
    /// <list type="bullet">
    /// <item>Render scale: written on a runtime copy of the URP asset assigned to Graphics › Default Render Pipeline, never
    /// on the shared asset (Play Mode in the Editor would save it); the original is put back when the application (or Play
    /// Mode) ends. When the active quality level has a URP asset of its own, that asset is written directly in a build and
    /// left alone in the Editor, with a warning: copying it would need the level switched back to restore it.</item>
    /// <item>Brightness: a global <see cref="Volume"/> (created on first use, kept across scenes) whose
    /// <see cref="ColorAdjustments"/> overrides only post exposure, log2 of the brightness (1 = 0 EV, 2 = +1 EV, 0.25 or
    /// less = −2 EV). Cameras need Post Processing enabled to show it.</item>
    /// </list>
    /// Without a URP asset in use nothing is written; the values stay saved by the display service.
    /// </summary>
    internal sealed class UrpDisplayAdapter
    {
        private const float MinBrightness = 0.25f;
        private const int VolumePriority = 1000;

        private readonly IDisplayService _display;
        private UniversalRenderPipelineAsset _copy;
        private RenderPipelineAsset _original;
#if UNITY_EDITOR
        private bool _warnedQualityAsset;
#endif
        private Volume _volume;
        private ColorAdjustments _adjustments;

        internal UrpDisplayAdapter(IDisplayService display)
        {
            _display = display;
        }

        /// <summary>Applies both values (at startup: the saved ones).</summary>
        internal void ApplyAll()
        {
            ApplyRenderScale();
            ApplyBrightness();
        }

        /// <summary>Follows <see cref="DisplaySettingsChangedEvent"/>; a quality preset may bring its own URP asset.</summary>
        internal void OnDisplaySettingsChanged(DisplaySettingsChangedEvent evt)
        {
            switch (evt.Kind)
            {
                case DisplaySettingKind.RenderScale:
                case DisplaySettingKind.Quality:
                    ApplyRenderScale();
                    break;
                case DisplaySettingKind.Brightness:
                    ApplyBrightness();
                    break;
            }
        }

        /// <summary>Puts the original default render pipeline back.</summary>
        internal void Restore()
        {
            if (_copy == null)
            {
                return;
            }

            if (GraphicsSettings.defaultRenderPipeline == _copy)
            {
                GraphicsSettings.defaultRenderPipeline = _original;
            }

            Object.Destroy(_copy);
            _copy = null;
            _original = null;
        }

        private void ApplyRenderScale()
        {
            if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset levelAsset)
            {
#if UNITY_EDITOR
                if (!_warnedQualityAsset)
                {
                    _warnedQualityAsset = true;
                    Debug.LogWarning("[Game] The quality level has its own URP asset: render scale is applied in builds only. " +
                                     "Leave the quality levels' Render Pipeline Asset empty to preview it in the Editor.");
                }
#else
                levelAsset.renderScale = _display.RenderScale;
#endif
                return;
            }

            if (_copy == null)
            {
                if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset shared))
                {
                    return;
                }

                _original = shared;
                _copy = Object.Instantiate(shared);
                _copy.name = shared.name + " (runtime)";
                GraphicsSettings.defaultRenderPipeline = _copy;
            }

            _copy.renderScale = _display.RenderScale;
        }

        private void ApplyBrightness()
        {
            if (_volume == null)
            {
                if (CoreLifecycle.IsQuitting)
                {
                    return;
                }

                var holder = new GameObject("[Game] Brightness");
                Object.DontDestroyOnLoad(holder);
                _volume = holder.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = VolumePriority;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                // Only post exposure is overridden, so the game's own color grading volumes keep their other values.
                _adjustments = profile.Add<ColorAdjustments>(false);
                _volume.sharedProfile = profile;
            }

            _adjustments.postExposure.Override(Mathf.Log(Mathf.Max(_display.Brightness, MinBrightness), 2f));
        }
    }
}
