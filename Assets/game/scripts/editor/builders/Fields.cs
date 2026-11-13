using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.Builders
{
    /// <summary>An enum value written by member name, for enums of assemblies the builders cannot reference.</summary>
    internal readonly struct EnumName
    {
        internal EnumName(string name)
        {
            Name = name;
        }

        internal string Name { get; }
    }

    /// <summary>
    /// Writes serialized fields (private <c>_camelCase</c> fields marked [SerializeField]) through
    /// <see cref="SerializedObject"/>, so internal types can be configured by name. Paths may be dotted for nested
    /// serializable classes (<c>_look._shape</c>). A missing field or a value of the wrong kind throws with the type and
    /// field name, instead of silently leaving the field empty.
    /// </summary>
    internal static class Fields
    {
        /// <summary>Assigns each (path, value) pair on <paramref name="target"/> and applies the changes.</summary>
        internal static void Set(Object target, params (string path, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach ((string path, object value) in values)
            {
                Assign(Find(serialized, path), value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Resizes the array or list at <paramref name="path"/> to <paramref name="count"/> and lets
        /// <paramref name="fill"/> write each element (use <see cref="Relative"/> for fields of nested classes).
        /// </summary>
        internal static void SetList(Object target, string path, int count, Action<SerializedProperty, int> fill)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty list = Find(serialized, path);
            if (!list.isArray || list.propertyType == SerializedPropertyType.String)
            {
                throw new InvalidOperationException($"{Describe(list)} is not an array or list.");
            }

            list.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                fill(list.GetArrayElementAtIndex(i), i);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a field of a nested serializable element.</summary>
        internal static void Relative(SerializedProperty element, string name, object value)
        {
            SerializedProperty property = element.FindPropertyRelative(name)
                ?? throw new InvalidOperationException($"{Describe(element)} has no serialized field '{name}'.");
            Assign(property, value);
        }

        private static SerializedProperty Find(SerializedObject serialized, string path)
        {
            return serialized.FindProperty(path)
                ?? throw new InvalidOperationException(
                    $"{serialized.targetObject.GetType().FullName} has no serialized field '{path}'.");
        }

        private static void Assign(SerializedProperty property, object value)
        {
            switch (value)
            {
                case null:
                    Expect(property, SerializedPropertyType.ObjectReference, "null");
                    property.objectReferenceValue = null;
                    break;
                case string text:
                    Expect(property, SerializedPropertyType.String, text);
                    property.stringValue = text;
                    break;
                case bool flag:
                    Expect(property, SerializedPropertyType.Boolean, flag);
                    property.boolValue = flag;
                    break;
                case int number when property.propertyType == SerializedPropertyType.Float:
                    property.floatValue = number;
                    break;
                case int number:
                    Expect(property, SerializedPropertyType.Integer, number);
                    property.intValue = number;
                    break;
                case float number:
                    Expect(property, SerializedPropertyType.Float, number);
                    property.floatValue = number;
                    break;
                case Color color:
                    Expect(property, SerializedPropertyType.Color, color);
                    property.colorValue = color;
                    break;
                case Vector2 vector:
                    Expect(property, SerializedPropertyType.Vector2, vector);
                    property.vector2Value = vector;
                    break;
                case Vector2Int cell:
                    Expect(property, SerializedPropertyType.Vector2Int, cell);
                    property.vector2IntValue = cell;
                    break;
                case EnumName member:
                    AssignEnum(property, member.Name);
                    break;
                case Array items:
                    AssignArray(property, items);
                    break;
                case Object reference:
                    Expect(property, SerializedPropertyType.ObjectReference, reference);
                    property.objectReferenceValue = reference;
                    if (property.objectReferenceValue != reference)
                    {
                        throw new InvalidOperationException(
                            $"{Describe(property)} does not accept a {reference.GetType().Name} ({reference.name}).");
                    }

                    break;
                default:
                    throw new InvalidOperationException($"{Describe(property)}: unsupported value type {value.GetType().Name}.");
            }
        }

        private static void AssignEnum(SerializedProperty property, string name)
        {
            Expect(property, SerializedPropertyType.Enum, name);
            int index = Array.IndexOf(property.enumNames, name);
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"{Describe(property)} has no enum member '{name}' (members: {string.Join(", ", property.enumNames)}).");
            }

            property.enumValueIndex = index;
        }

        // Object and string arrays fill lists of references or of strings, element by element.
        private static void AssignArray(SerializedProperty property, Array items)
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                throw new InvalidOperationException($"{Describe(property)} is not an array or list.");
            }

            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                Assign(property.GetArrayElementAtIndex(i), items.GetValue(i));
            }
        }

        private static void Expect(SerializedProperty property, SerializedPropertyType type, object value)
        {
            if (property.propertyType != type)
            {
                throw new InvalidOperationException(
                    $"{Describe(property)} is {property.propertyType}; cannot assign '{value}'.");
            }
        }

        private static string Describe(SerializedProperty property)
        {
            return $"{property.serializedObject.targetObject.GetType().FullName}.{property.propertyPath}";
        }
    }
}
