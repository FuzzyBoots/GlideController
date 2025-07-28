using FS_Core;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_ThirdPerson
{
    [CustomEditor(typeof(ArmorItem))]
    public class ArmorItemEditor : Editor
    {
        SerializedProperty nameProp, description, icon, category;
        SerializedProperty overrideCategorySettings, overrideCategoryAttributes;
        SerializedProperty equipmentSlotIndex, attachModel, modelPrefab, isSkinnedMesh;
        SerializedProperty localPosition, localRotation;

        EquipmentSlotsDatabase equipmentSlots;

        ArmorItem armorItem;


        private void OnEnable()
        {
            armorItem = target as ArmorItem;

            nameProp = serializedObject.FindProperty("name");
            description = serializedObject.FindProperty("description");
            icon = serializedObject.FindProperty("icon");
            category = serializedObject.FindProperty("category");
            overrideCategorySettings = serializedObject.FindProperty("overrideCategorySettings");
            overrideCategoryAttributes = serializedObject.FindProperty("overrideCategoryAttributes");

            equipmentSlotIndex = serializedObject.FindProperty("equipmentSlotIndex");
            attachModel = serializedObject.FindProperty("attachModel");
            modelPrefab = serializedObject.FindProperty("modelPrefab");
            isSkinnedMesh = serializedObject.FindProperty("isSkinnedMesh");
            localPosition = serializedObject.FindProperty("localPosition");
            localRotation = serializedObject.FindProperty("localRotation");

            equipmentSlots = Resources.LoadAll<EquipmentSlotsDatabase>("").FirstOrDefault();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nameProp, new GUIContent("Name"));
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(category);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Category Overrides", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(overrideCategorySettings);
            EditorGUILayout.PropertyField(overrideCategoryAttributes);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(attachModel);


            if (attachModel.boolValue)
            {
                if (equipmentSlots != null)
                {
                    var slots = equipmentSlots.EquipmentSlotList;

                    int newSelectedIndex = EditorGUILayout.Popup("Attachment Slot",
                        armorItem.equipmentSlotIndex, slots.ToArray());

                    if (newSelectedIndex != armorItem.equipmentSlotIndex)
                    {
                        armorItem.equipmentSlotIndex = newSelectedIndex;
                    }
                }
                EditorGUILayout.PropertyField(modelPrefab);
                EditorGUILayout.PropertyField(isSkinnedMesh);
                if (!isSkinnedMesh.boolValue)
                {
                    EditorGUILayout.PropertyField(localPosition);
                    EditorGUILayout.PropertyField(localRotation);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}