using System;
using System.Collections.Generic;
using Game.Contracts.Settings;
using Paowa.Core.Messaging;
using Paowa.Settings;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Default <see cref="IGameSettings"/>: an applier of the game's own settings that keeps their committed values (the
    /// saved ones when it registers, then every commit; the settings service stores them). A committed UI scale is also
    /// published as <see cref="UIScaleChangedEvent"/> for the canvases, a committed text size as <see cref="TextScaleChangedEvent"/>. Ids without a committed value read as the neutral
    /// values below, e.g. when the settings service is disabled or a game removes the definition from its catalog.
    /// </summary>
    internal sealed class GameSettings : IGameSettings, ISettingApplier
    {
        private const float Neutral = 1f;

        private static readonly HashSet<string> Ids = new HashSet<string>(StringComparer.Ordinal)
        {
            GameSettingIds.CameraShake,
            GameSettingIds.UIScale,
            GameSettingIds.TextScale,
            GameSettingIds.ReduceFlashing,
        };

        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, SettingValue> _values = new Dictionary<string, SettingValue>(StringComparer.Ordinal);

        internal GameSettings(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public float CameraShake => Float(GameSettingIds.CameraShake, Neutral);





        public float UIScale => Float(GameSettingIds.UIScale, Neutral);

        public float TextScale => Float(GameSettingIds.TextScale, Neutral);

        public bool ReduceFlashing => Bool(GameSettingIds.ReduceFlashing, false);




        public bool Handles(SettingDefinition definition)
        {
            return Ids.Contains(definition.Id);
        }

        public void Apply(SettingDefinition definition, SettingValue value)
        {
            _values[definition.Id] = value;
            if (definition.Id == GameSettingIds.UIScale && _eventBus != null)
            {
                _eventBus.Publish(new UIScaleChangedEvent(UIScale));
            }
            else if (definition.Id == GameSettingIds.TextScale && _eventBus != null)
            {
                _eventBus.Publish(new TextScaleChangedEvent(TextScale));
            }
        }

        private float Float(string id, float fallback)
        {
            return _values.TryGetValue(id, out SettingValue value) ? value.AsFloat() : fallback;
        }


        private bool Bool(string id, bool fallback)
        {
            return _values.TryGetValue(id, out SettingValue value) ? value.AsBool() : fallback;
        }
    }
}
