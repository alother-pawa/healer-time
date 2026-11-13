using DG.Tweening;
using Game.Contracts.Presentation;
using UnityEngine;

namespace Game.Foundation.Tweening
{
    /// <summary>DOTween helpers shared by every animated view: the global animation switch and awaiting a tween.</summary>
    public static class TweenGateExtensions
    {
        /// <summary>
        /// Completes <paramref name="tween"/> at once (callbacks included) when animations are disabled, so the end state
        /// is applied in the same frame. A null <paramref name="settings"/> leaves animations on.
        /// </summary>
        public static T Gate<T>(this T tween, IAnimationSettings settings) where T : Tween
        {
            if (tween != null && settings != null && !settings.Enabled)
            {
                tween.Complete(true);
            }

            return tween;
        }

        /// <summary>
        /// Completes when <paramref name="tween"/> completes or is killed (including by <c>SetLink</c> when its object is
        /// destroyed), so an awaiting flow never hangs. Existing completion and kill callbacks keep running.
        /// A tween that already ended (null, killed, completed: every gated tween with animations disabled) gives an
        /// awaitable that is already complete, so the caller continues in the same frame.
        /// Otherwise the caller resumes on a later frame from Unity's player loop, never from inside a DOTween callback:
        /// DOTween runs <c>onKill</c> before it removes the tween from its active list, and Unity resumes an awaiting
        /// method synchronously when its awaitable completes, so resuming inside the callback would let the caller kill or
        /// start tweens while DOTween is half-way through despawning this one (a second removal corrupts its active list).
        /// </summary>
        public static Awaitable AsAwaitable(this Tween tween)
        {
            if (tween == null || !tween.IsActive() || tween.IsComplete())
            {
                var completed = new AwaitableCompletionSource();
                completed.SetResult();
                return completed.Awaitable;
            }

            var ended = new EndFlag();
            TweenCallback previousComplete = tween.onComplete;
            TweenCallback previousKill = tween.onKill;
            tween.onComplete = () =>
            {
                previousComplete?.Invoke();
                ended.IsSet = true;
            };
            tween.onKill = () =>
            {
                previousKill?.Invoke();
                ended.IsSet = true;
            };
            return WaitForEndAsync(tween, ended);
        }

        /// <summary>
        /// Kills <paramref name="tween"/> if it is still running; a null, finished or already killed tween is left alone
        /// (with recycling off a dead tween is never reused, so a stale field cannot kill someone else's tween).
        /// </summary>
        public static void KillIfActive(this Tween tween, bool complete = false)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill(complete);
            }
        }

        private static async Awaitable WaitForEndAsync(Tween tween, EndFlag ended)
        {
            // The activity check also ends the wait if the tween died without its callbacks (DOTween cleared or purged).
            do
            {
                await Awaitable.NextFrameAsync();
            }
            while (!ended.IsSet && tween.IsActive());
        }

        /// <summary>Set by the tween's completion or kill callback; read by the awaiting loop on the next frames.</summary>
        private sealed class EndFlag
        {
            internal bool IsSet;
        }
    }
}
