using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.Splash
{
    /// <summary>Timing, preload and destination of the splash scene.</summary>
    [CreateAssetMenu(menuName = "Game/Config/Splash", fileName = "SplashConfig")]
    internal sealed class SplashConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float _fadeInSeconds = 0.6f;
        [Tooltip("Time the logo stays fully visible; applies with animations disabled too.")]
        [Min(0f)] [SerializeField] private float _holdSeconds = 1.6f;
        [Min(0f)] [SerializeField] private float _fadeOutSeconds = 0.6f;
        [Tooltip("Skip requests earlier than this (a key still held from the launcher) are ignored.")]
        [Min(0f)] [SerializeField] private float _skipDelaySeconds = 0.4f;
        [Tooltip("Longest wait for the first language; after it the splash continues with a warning.")]
        [Min(0f)] [SerializeField] private float _localizationTimeoutSeconds = 5f;
        [Tooltip("Optional shaders warmed up during the splash.")]
        [SerializeField] private ShaderVariantCollection _shaderVariants;
        [SerializeField] private SceneKey _nextScene;

        internal float FadeInSeconds => _fadeInSeconds;

        internal float HoldSeconds => _holdSeconds;

        internal float FadeOutSeconds => _fadeOutSeconds;

        internal float SkipDelaySeconds => _skipDelaySeconds;

        internal float LocalizationTimeoutSeconds => _localizationTimeoutSeconds;

        internal ShaderVariantCollection ShaderVariants => _shaderVariants;

        internal SceneKey NextScene => _nextScene;
    }
}
