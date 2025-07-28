using FS_ThirdPerson;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FS_Core
{
    public class ItemDataEditor : Editor
    {
        SerializedProperty nameProp, descriptionProp, iconProp, categoryProp;
        Item itemData;

        static bool generalSettingsFoldout = true;
        static bool attributesFoldout = true;

        public virtual void OnEnable()
        {
            itemData = (Item)target;

            nameProp = serializedObject.FindProperty("name");
            descriptionProp = serializedObject.FindProperty("description");
            iconProp = serializedObject.FindProperty("icon");
            categoryProp = serializedObject.FindProperty("category");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawFoldout(ref generalSettingsFoldout, "General Settings", () =>
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(nameProp);
                    EditorGUILayout.PropertyField(descriptionProp, GUILayout.Height(45));

                    // Icon with preview
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(iconProp);
                    if (iconProp.objectReferenceValue != null)
                    {
                        GUILayout.Label(AssetPreview.GetAssetPreview(iconProp.objectReferenceValue),
                            GUILayout.Width(50), GUILayout.Height(50));
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(categoryProp);
                }
            });

            if (itemData.category != null && itemData.category.attributes.Count > 0)
            {
                if (itemData.attributeValues == null ||
                    itemData.attributeValues.Count != itemData.category.attributes.Count)
                {
                    itemData.attributeValues = new List<ItemAttribute>();
                    for (int i = 0; i < itemData.category.attributes.Count; i++)
                    {
                        itemData.attributeValues.Add(new ItemAttribute(
                            itemData.category.attributes[i].attributeType,
                            itemData.category.attributes[i].attributeName));
                    }
                }

                DrawFoldout(ref attributesFoldout, "Attribute Settings", () =>
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        foreach (var attribute in itemData.attributeValues)
                        {
                            switch (attribute.attributeType)
                            {
                                case ItemAttributeType.Integer:
                                    attribute.intValue = EditorGUILayout.IntField(
                                        attribute.attributeName, attribute.intValue);
                                    break;
                                case ItemAttributeType.Decimal:
                                    attribute.floatValue = EditorGUILayout.FloatField(
                                        attribute.attributeName, attribute.floatValue);
                                    break;
                                case ItemAttributeType.Text:
                                    attribute.stringValue = EditorGUILayout.TextField(
                                        attribute.attributeName, attribute.stringValue);
                                    break;
                            }
                        }
                    }
                });
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void DrawFoldout(ref bool foldout, string label, System.Action drawer)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            foldout = EditorGUILayout.Foldout(foldout, label, true);
            if (foldout)
            {
                drawer();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }
}
