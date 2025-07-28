using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace FS_Util
{

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionField { get; }
        public object ExpectedValue { get; }
        public ComparisonType Comparison { get; }

        public ShowIfAttribute(string conditionField, object expectedValue, ComparisonType comparison = ComparisonType.Equals)
        {
            ConditionField = conditionField;
            ExpectedValue = expectedValue;
            Comparison = comparison;
        }
    }

    public enum ComparisonType
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShouldShow(property) ? EditorGUI.GetPropertyHeight(property, label, true) : 0;
        }

        private bool ShouldShow(SerializedProperty property)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            SerializedProperty conditionProperty = property.serializedObject.FindProperty(showIf.ConditionField);

            if (conditionProperty == null)
                return false;

            object conditionValue = GetPropertyValue(conditionProperty);
            if (conditionValue == null)
                return false;

            return CompareValues(conditionValue, showIf.ExpectedValue, showIf.Comparison);
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.Enum: return property.enumValueIndex;
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Float: return property.floatValue;
                default: return null;
            }
        }

        private bool CompareValues(object a, object b, ComparisonType comparison)
        {
            try
            {
                double aVal = Convert.ToDouble(a);
                double bVal = Convert.ToDouble(b);

                return comparison switch
                {
                    ComparisonType.Equals => aVal == bVal,
                    ComparisonType.NotEquals => aVal != bVal,
                    ComparisonType.GreaterThan => aVal > bVal,
                    ComparisonType.LessThan => aVal < bVal,
                    ComparisonType.GreaterThanOrEqual => aVal >= bVal,
                    ComparisonType.LessThanOrEqual => aVal <= bVal,
                    _ => false,
                };
            }
            catch
            {
                return a.Equals(b);
            }
        }
    }

#endif
}