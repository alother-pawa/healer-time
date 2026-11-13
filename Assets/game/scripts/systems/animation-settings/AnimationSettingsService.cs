using DG.Tweening;
using Game.Contracts.Presentation;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;

namespace Game.Systems.AnimationSettings
{
    /// <summary>
    /// Default <see cref="IAnimationSettings"/>, persisted in Core's global store. Turning animations off
    /// completes every running tween, so flows waiting on one continue at once with the end state applied.
    /// </summary>
    internal sealed class AnimationSettingsService : IAnimationSettings
    {
        private const string EnabledKey = "game.animations.enabled";

        private readonly ISaveStore _store;
        private readonly IEventBus _eventBus;
        private bool _enabled;

        internal AnimationSettingsService(ISaveStore store, IEventBus eventBus)
        {
            _store = store;
            _eventBus = eventBus;
            _enabled = store == null || store.Get(EnabledKey, true);
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                _store?.Set(EnabledKey, value);
                if (!value)
                {
                    DOTween.CompleteAll(true);
                }

                _eventBus.Publish(new AnimationSettingsChangedEvent(value));
            }
        }
    }
}
