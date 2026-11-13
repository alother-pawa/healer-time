using Paowa.Settings;

namespace Game.Tests.Editor
{
    /// <summary>A setting kind the settings screen has no row for (row factory tests); its own file so Unity can create it.</summary>
    internal sealed class UnknownSettingDefinition : SettingDefinition
    {
        public override SettingValueType ValueType => SettingValueType.Bool;
    }
}
