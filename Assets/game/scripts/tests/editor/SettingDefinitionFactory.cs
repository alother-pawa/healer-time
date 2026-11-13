using System.Collections.Generic;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Creates in-memory setting definitions with their private serialized fields written through
    /// <see cref="SerializedObject"/> (as the builders do), and destroys them in <see cref="DestroyAll"/>.
    /// </summary>
    internal sealed class SettingDefinitionFactory
    {
        private readonly List<Object> _created = new List<Object>();

        internal T Create<T>(string id, string tabId, params (string field, object value)[] fields) where T : SettingDefinition
        {
            var definition = ScriptableObject.CreateInstance<T>();
            definition.name = id;
            _created.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_tabId").stringValue = tabId;
            serialized.FindProperty("_labelKey").stringValue = "label." + id;
            foreach ((string field, object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                switch (value)
                {
                    case string text:
                        property.stringValue = text;
                        break;
                    case int number:
                        property.intValue = number;
                        break;
                    case bool flag:
                        property.boolValue = flag;
                        break;
                    case float number:
                        property.floatValue = number;
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        /// <summary>A key binding row slot.</summary>
        internal KeyBindingSettingDefinition Binding(string id, string action, string scheme, string part, int slot)
        {
            return Create<KeyBindingSettingDefinition>(id, "controls", ("_action", action), ("_controlScheme", scheme),
                ("_compositePart", part), ("_slot", slot));
        }

        internal void DestroyAll()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }
    }
}
