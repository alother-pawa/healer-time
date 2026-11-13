using System.Collections.Generic;
using Game.Systems.Settings;
using NUnit.Framework;
using Paowa.Input.Rebinding;
using Paowa.Settings;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Row factory of the settings popup: each definition kind maps to its row prefab kind, key binding slots of one action,
    /// part and scheme share one row in slot order, other schemes get their own rows, and the scheme bar lists the binding
    /// schemes in order of appearance.
    /// </summary>
    public sealed class SettingRowPlannerTests
    {
        private readonly SettingDefinitionFactory _factory = new SettingDefinitionFactory();

        [TearDown]
        public void TearDown()
        {
            _factory.DestroyAll();
        }

        [Test]
        public void EveryKindMapsToItsRow()
        {
            Assert.That(SettingRowPlanner.KindOf(_factory.Create<ToggleSettingDefinition>("t", "gameplay")), Is.EqualTo(SettingRowKind.Toggle));
            Assert.That(SettingRowPlanner.KindOf(_factory.Create<SliderSettingDefinition>("s", "gameplay")), Is.EqualTo(SettingRowKind.Slider));
            Assert.That(SettingRowPlanner.KindOf(_factory.Create<ChoiceSettingDefinition>("c", "graphics")), Is.EqualTo(SettingRowKind.Choice));
            Assert.That(SettingRowPlanner.KindOf(_factory.Binding("k", "Gameplay/Jump", "KeyboardMouse", "", 0)),
                Is.EqualTo(SettingRowKind.KeyBinding));
            Assert.That(SettingRowPlanner.KindOf(_factory.Create<ButtonSettingDefinition>("b", "controls")), Is.EqualTo(SettingRowKind.Button));
            Assert.That(SettingRowPlanner.KindOf(_factory.Create<UnknownSettingDefinition>("x", "gameplay")), Is.EqualTo(SettingRowKind.Unknown));
        }

        [Test]
        public void UnknownKindsAndNullEntriesAreLeftOut()
        {
            var definitions = new List<SettingDefinition>
            {
                _factory.Create<ToggleSettingDefinition>("a", "gameplay"), null, _factory.Create<UnknownSettingDefinition>("x", "gameplay"),
                _factory.Create<SliderSettingDefinition>("b", "gameplay"),
            };

            List<SettingRowPlan> rows = SettingRowPlanner.Plan(definitions);

            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].Definitions[0].Id, Is.EqualTo("a"));
            Assert.That(rows[1].Kind, Is.EqualTo(SettingRowKind.Slider));
        }

        [Test]
        public void SlotsOfOneBindingShareARowInSlotOrder()
        {
            KeyBindingSettingDefinition secondary = _factory.Binding("jump.2", "Gameplay/Jump", "KeyboardMouse", "", 1);
            KeyBindingSettingDefinition primary = _factory.Binding("jump.1", "Gameplay/Jump", "KeyboardMouse", "", 0);
            ToggleSettingDefinition between = _factory.Create<ToggleSettingDefinition>("invert", "controls");

            List<SettingRowPlan> rows = SettingRowPlanner.Plan(new SettingDefinition[] { secondary, between, primary });

            Assert.That(rows, Has.Count.EqualTo(2), "The second slot joins the first row instead of making its own.");
            Assert.That(rows[0].Kind, Is.EqualTo(SettingRowKind.KeyBinding));
            Assert.That(rows[0].Definitions, Is.EqualTo(new SettingDefinition[] { primary, secondary }));
            Assert.That(rows[0].Scheme, Is.EqualTo("KeyboardMouse"));
            Assert.That(rows[1].Definitions[0], Is.SameAs(between));
        }

        [Test]
        public void PartsAndSchemesMakeSeparateRows()
        {
            var definitions = new SettingDefinition[]
            {
                _factory.Binding("up", "Gameplay/Move", "KeyboardMouse", "up", 0),
                _factory.Binding("down", "Gameplay/Move", "KeyboardMouse", "down", 0),
                _factory.Binding("pad", "Gameplay/Move", "Gamepad", "", 0),
            };

            List<SettingRowPlan> rows = SettingRowPlanner.Plan(definitions);

            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[2].Scheme, Is.EqualTo("Gamepad"));
            Assert.That(SettingRowPlanner.BindingSchemes(rows), Is.EqualTo(new[] { "KeyboardMouse", "Gamepad" }));
        }

        [Test]
        public void AResetBindingsButtonBelongsToItsScheme()
        {
            ButtonSettingDefinition reset = _factory.Create<ButtonSettingDefinition>("reset", "controls",
                ("_actionId", KeyBindingActionIds.ResetBindings), ("_argument", "Gamepad"));
            ButtonSettingDefinition other = _factory.Create<ButtonSettingDefinition>("other", "controls",
                ("_actionId", SettingActionIds.ResetTab), ("_argument", "Gamepad"));

            Assert.That(SettingRowPlanner.SchemeOf(reset), Is.EqualTo("Gamepad"));
            Assert.That(SettingRowPlanner.SchemeOf(other), Is.Empty, "Only reset-bindings buttons follow the scheme filter.");
            Assert.That(SettingRowPlanner.SchemeOf(_factory.Create<SliderSettingDefinition>("sens", "controls")), Is.Empty);
        }

        [Test]
        public void SchemeRowsWithoutBindingsDoNotMakeASchemeBar()
        {
            ButtonSettingDefinition reset = _factory.Create<ButtonSettingDefinition>("reset", "controls",
                ("_actionId", KeyBindingActionIds.ResetBindings), ("_argument", "Gamepad"));

            Assert.That(SettingRowPlanner.BindingSchemes(SettingRowPlanner.Plan(new SettingDefinition[] { reset })), Is.Empty);
        }
    }
}
