using System.Collections.Generic;
using Game.Foundation.InputPrompts;
using NUnit.Framework;
using Paowa.Gamepad;
using Paowa.Input;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Prompt resolution against a fake glyph service: action and part parsing, hidden slots for unbound actions, text on
    /// the key frame without a sprite, the glyph table's label over a sprite, no text over a sprite whose text is only the
    /// binding's display string.
    /// </summary>
    public sealed class InputPromptResolverTests
    {
        private readonly List<Object> _created = new List<Object>();
        private FakeGlyphs _glyphs;

        [SetUp]
        public void SetUp()
        {
            _glyphs = new FakeGlyphs();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [TestCase("UI/Submit", "UI/Submit", null)]
        [TestCase("Gameplay/Move/up", "Gameplay/Move", "up")]
        public void SplitsActionAndPart(string spec, string action, string part)
        {
            Assert.That(InputPromptResolver.TrySplit(spec, out string parsedAction, out string parsedPart), Is.True);
            Assert.That(parsedAction, Is.EqualTo(action));
            Assert.That(parsedPart, Is.EqualTo(part));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Submit")]
        [TestCase("UI//Submit")]
        [TestCase("A/B/C/D")]
        public void RejectsMalformedActions(string spec)
        {
            Assert.That(InputPromptResolver.TrySplit(spec, out _, out _), Is.False);
            Assert.That(InputPromptResolver.Resolve(_glyphs, spec).IsVisible, Is.False);
        }

        [Test]
        public void AnUnboundActionIsHidden()
        {
            Assert.That(InputPromptResolver.Resolve(_glyphs, "UI/Submit").IsVisible, Is.False);
        }

        [Test]
        public void WithoutAGlyphServiceNothingShows()
        {
            Assert.That(InputPromptResolver.Resolve(null, "UI/Submit").IsVisible, Is.False);
        }

        [Test]
        public void TextOnlyGlyphsDrawTheirTextOnTheKeyFrame()
        {
            _glyphs.Add("UI/Submit", null, new InputGlyph(null, "Enter", null));

            PromptGlyph glyph = InputPromptResolver.Resolve(_glyphs, "UI/Submit");

            Assert.That(glyph.IsVisible, Is.True);
            Assert.That(glyph.Sprite, Is.Null);
            Assert.That(glyph.ShowText, Is.True);
            Assert.That(glyph.Text, Is.EqualTo("Enter"));
        }

        [Test]
        public void ASpriteWithATableLabelShowsTheLabelOverIt()
        {
            Sprite frame = CreateSprite(new Vector4(4f, 4f, 4f, 4f));
            _glyphs.Add("Menu/TabNext", null, new InputGlyph(frame, "RB", null, true));

            PromptGlyph glyph = InputPromptResolver.Resolve(_glyphs, "Menu/TabNext");

            Assert.That(glyph.Sprite, Is.SameAs(frame));
            Assert.That(glyph.ShowText, Is.True);
            Assert.That(glyph.Text, Is.EqualTo("RB"));
        }

        [Test]
        public void ASpriteWithoutATableLabelShowsNoText()
        {
            // The text is the binding's display string, a fallback: the sprite already shows the control.
            Sprite picture = CreateSprite(Vector4.zero);
            _glyphs.Add("UI/Cancel", null, new InputGlyph(picture, "Circle", null, false));

            PromptGlyph glyph = InputPromptResolver.Resolve(_glyphs, "UI/Cancel");

            Assert.That(glyph.Sprite, Is.SameAs(picture));
            Assert.That(glyph.ShowText, Is.False);
        }

        [Test]
        public void ACompositePartIsLookedUpByItsName()
        {
            _glyphs.Add("Gameplay/Move", "up", new InputGlyph(null, "W", null));

            Assert.That(InputPromptResolver.Resolve(_glyphs, "Gameplay/Move/up").Text, Is.EqualTo("W"));
            Assert.That(InputPromptResolver.Resolve(_glyphs, "Gameplay/Move").IsVisible, Is.False);
        }

        [Test]
        public void EachResolveReadsTheGlyphServiceAgain()
        {
            // The prompt keeps no glyph of its own: whatever the service returns now (another device) is shown.
            _glyphs.Add("UI/Submit", null, new InputGlyph(null, "Enter", null));
            Assert.That(InputPromptResolver.Resolve(_glyphs, "UI/Submit").Text, Is.EqualTo("Enter"));

            _glyphs.Add("UI/Submit", null, new InputGlyph(CreateSprite(new Vector4(2f, 2f, 2f, 2f)), "A", null, true));
            Assert.That(InputPromptResolver.Resolve(_glyphs, "UI/Submit").Text, Is.EqualTo("A"));
        }

        private Sprite CreateSprite(Vector4 border)
        {
            var texture = new Texture2D(16, 16);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            _created.Add(sprite);
            _created.Add(texture);
            return sprite;
        }

        /// <summary>Returns configured glyphs per action and part, like the device in use would.</summary>
        private sealed class FakeGlyphs : IInputGlyphs
        {
            private readonly Dictionary<string, InputGlyph> _glyphs = new Dictionary<string, InputGlyph>();

            public GlyphSet Set { get; set; }

            public GamepadFamily LastGamepadFamily => GamepadFamily.Generic;

            public string CurrentControlScheme => string.Empty;

            internal void Add(string action, string part, InputGlyph glyph)
            {
                _glyphs[Key(action, part)] = glyph;
            }

            public InputGlyph GetActionGlyph(string action, string partName = null)
            {
                return _glyphs.TryGetValue(Key(action, partName), out InputGlyph glyph) ? glyph : default;
            }

            public InputGlyph GetGlyph(InputBindingInfo binding, InputDeviceKind kind, GamepadFamily family)
            {
                return default;
            }

            private static string Key(string action, string part)
            {
                return action + "|" + (part ?? string.Empty);
            }
        }
    }
}
