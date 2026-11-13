using UnityEngine;

namespace Game.Systems.Splash
{
    /// <summary>
    /// State of the splash, advanced by <see cref="Tick"/> once per frame; the view only draws <see cref="LogoAlpha"/> and
    /// <see cref="IsWaitingForPreload"/>, so the screen never depends on a tween having run. Rules:
    /// <list type="bullet">
    /// <item>fade in → hold → fade out, then wait for the preload; the splash finishes only when both the animation and the
    /// preload are done;</item>
    /// <item>a skip request shortens the animation only: during the fade in or the hold it starts the fade out from the
    /// current alpha; it never shortens the preload wait. A request earlier than the skip delay (a key still held from the
    /// launcher) is dropped;</item>
    /// <item>with animations off the fades take no time: the logo appears at full alpha, holds (reading time, like a toast)
    /// and disappears at once. Turning animations off mid-fade completes that fade.</item>
    /// </list>
    /// </summary>
    internal sealed class SplashSequence
    {
        private readonly float _fadeInSeconds;
        private readonly float _holdSeconds;
        private readonly float _fadeOutSeconds;
        private readonly float _skipDelaySeconds;
        private float _elapsed;
        private float _phaseTime;
        private bool _skipRequested;

        internal SplashSequence(float fadeInSeconds, float holdSeconds, float fadeOutSeconds, float skipDelaySeconds)
        {
            _fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            _holdSeconds = Mathf.Max(0f, holdSeconds);
            _fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            _skipDelaySeconds = Mathf.Max(0f, skipDelaySeconds);
        }

        /// <summary>Current step.</summary>
        internal SplashPhase Phase { get; private set; } = SplashPhase.FadeIn;

        /// <summary>Opacity of the logo, 0..1.</summary>
        internal float LogoAlpha { get; private set; }

        /// <summary>True when the animation is over and the preload is still running.</summary>
        internal bool IsWaitingForPreload => Phase == SplashPhase.WaitForPreload;

        /// <summary>True once the animation and the preload are both done.</summary>
        internal bool IsFinished => Phase == SplashPhase.Finished;

        /// <summary>True when a skip request shortened the animation.</summary>
        internal bool WasSkipped { get; private set; }

        /// <summary>Asks to skip; applied (or dropped as too early) by the next <see cref="Tick"/>.</summary>
        internal void RequestSkip()
        {
            _skipRequested = true;
        }

        /// <summary>Advances by <paramref name="deltaTime"/> unscaled seconds.</summary>
        internal void Tick(float deltaTime, bool preloadDone, bool animate)
        {
            if (Phase == SplashPhase.Finished)
            {
                return;
            }

            float time = Mathf.Max(0f, deltaTime);
            _elapsed += time;
            if (_skipRequested)
            {
                _skipRequested = false;
                if (_elapsed >= _skipDelaySeconds && (Phase == SplashPhase.FadeIn || Phase == SplashPhase.Hold))
                {
                    WasSkipped = true;
                    float fadeOut = animate ? _fadeOutSeconds : 0f;

                    // Start the fade out where its curve has the current alpha, so the logo never jumps.
                    Enter(SplashPhase.FadeOut, (1f - LogoAlpha) * fadeOut);
                }
            }

            Advance(time, preloadDone, animate);
        }

        private void Advance(float time, bool preloadDone, bool animate)
        {
            while (true)
            {
                switch (Phase)
                {
                    case SplashPhase.FadeIn:
                    {
                        float duration = animate ? _fadeInSeconds : 0f;
                        _phaseTime += time;
                        if (_phaseTime >= duration)
                        {
                            time = _phaseTime - duration;
                            LogoAlpha = 1f;
                            Enter(SplashPhase.Hold, 0f);
                            continue;
                        }

                        LogoAlpha = _phaseTime / duration;
                        return;
                    }

                    case SplashPhase.Hold:
                    {
                        LogoAlpha = 1f;
                        _phaseTime += time;
                        if (_phaseTime >= _holdSeconds)
                        {
                            time = _phaseTime - _holdSeconds;
                            Enter(SplashPhase.FadeOut, 0f);
                            continue;
                        }

                        return;
                    }

                    case SplashPhase.FadeOut:
                    {
                        float duration = animate ? _fadeOutSeconds : 0f;
                        _phaseTime += time;
                        if (_phaseTime >= duration)
                        {
                            time = 0f;
                            LogoAlpha = 0f;
                            Enter(SplashPhase.WaitForPreload, 0f);
                            continue;
                        }

                        LogoAlpha = 1f - _phaseTime / duration;
                        return;
                    }

                    case SplashPhase.WaitForPreload:
                        LogoAlpha = 0f;
                        if (preloadDone)
                        {
                            Enter(SplashPhase.Finished, 0f);
                        }

                        return;

                    default:
                        return;
                }
            }
        }

        private void Enter(SplashPhase phase, float phaseTime)
        {
            Phase = phase;
            _phaseTime = phaseTime;
        }
    }
}
