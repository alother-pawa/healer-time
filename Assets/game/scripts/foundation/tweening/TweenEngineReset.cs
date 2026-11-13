using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Foundation.Tweening
{
    /// <summary>
    /// Starts every Editor Play session with a clean DOTween. With Enter Play Mode Options (no domain reload) DOTween's
    /// static manager survives from the previous session, including tweens, pools and counters left inconsistent by an
    /// exception; clearing it before anything else runs means one broken session cannot break the next ones. A fresh
    /// domain (builds, or domain reload on) has nothing to clear.
    /// </summary>
    internal static class TweenEngineReset
    {
#if UNITY_EDITOR
        // Attempts beyond one per leftover tween, for failures that are not a tween's callback.
        private const int ExtraAttempts = 3;

        // Clearing fires the onKill callbacks of leftover tweens; DOTween marks each one dead before calling it, so every
        // retry after a throwing callback moves past at least that tween. One attempt per active tween (plus a few) is
        // therefore enough, while a state that keeps failing still ends in a warning instead of a loop.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearLeftovers()
        {
            int maxAttempts = ExtraAttempts;
            try
            {
                maxAttempts += DOTween.TotalActiveTweens();
            }
            catch (Exception)
            {
                // A count that cannot be read leaves the extra attempts.
            }

            Exception failure = null;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    // Destroy = true also resets DOTween's settings, so the next tween re-initialises from DOTweenSettings.
                    DOTween.Clear(true);
                    return;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }

            Debug.LogWarning($"[Tweening] Could not clear DOTween left over from the previous Play session: {failure}");
        }
#endif
    }
}
