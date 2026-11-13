using Game.Contracts.Presentation;
using Game.Contracts.Settings;
using Paowa.Core.Messaging;
using Paowa.Settings;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Bridges the <c>gameplay.animations</c> toggle and <see cref="IAnimationSettings"/>, which already stores the switch:
    /// as an <see cref="ISettingValueSource"/> the settings service reads the value from there and never saves a second
    /// copy. A switch changed elsewhere (code, a debug tool) is announced to the settings rows through
    /// <see cref="ISettingsService.NotifyExternalChange"/>.
    /// </summary>
    internal sealed class AnimationSettingApplier : ISettingApplier, ISettingValueSource
    {
        private readonly IAnimationSettings _animations;

        internal AnimationSettingApplier(IAnimationSettings animations, ISettingsService settings, IEventBus eventBus)
        {
            _animations = animations;

            // Lives for the application, like the module that registers it.
            eventBus.Subscribe<AnimationSettingsChangedEvent>(evt =>
            {
                if (settings.TryGetDefinition(GameSettingIds.Animations, out _))
                {
                    settings.NotifyExternalChange(GameSettingIds.Animations);
                }
            });
        }

        public bool Handles(SettingDefinition definition)
        {
            return definition.Id == GameSettingIds.Animations;
        }

        public void Apply(SettingDefinition definition, SettingValue value)
        {
            _animations.Enabled = value.AsBool();
        }

        public bool TryRead(SettingDefinition definition, out SettingValue value)
        {
            value = SettingValue.FromBool(_animations.Enabled);
            return true;
        }
    }
}
