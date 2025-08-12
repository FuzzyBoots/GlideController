using FS_Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class EquippableItemEditor : AnimationPreviewHandler
    {
        public enum Animations { Equip, UnEquip }
        private EquippableItem equippableItem;

        private SerializedProperty name;
        private SerializedProperty description;
        private SerializedProperty icon;
        private SerializedProperty spawnModel;

        public SerializedProperty equipmentSlotIndex;
        public SerializedProperty attachModel;
        public SerializedProperty isSkinnedMesh;
        public SerializedProperty localPosition;
        public SerializedProperty localRotation;

        private SerializedProperty modelPrefab;
        private SerializedProperty holderBone;
        private SerializedProperty localPositionR;
        private SerializedProperty localRotationR;
        private SerializedProperty localPositionL;
        private SerializedProperty localRotationL;
        private SerializedProperty unEquipDuringActions;
        private SerializedProperty isDualItem;
        private SerializedProperty overrideController;
        private SerializedProperty itemEquippedIdleClip;
        private SerializedProperty itemEquippedIdleClipMask;
        private SerializedProperty itemEnableTime;
        private SerializedProperty equipAudio;
        private SerializedProperty equipClip;
        private SerializedProperty itemDisableTime;
        private SerializedProperty unEquipAudio;
        private SerializedProperty unEquipClip;

        static bool generalFoldout = true;
        static bool attributesFoldout = true;
        static bool equipFoldout;
        static bool unEquipFoldout;
        static bool equipmentSettings;
        EquipmentSlotsDatabase equipmentSlots;

        public virtual bool ShowDualItemProperty => false;

        public override void OnEnable()
        {
            base.OnEnable();
            equipmentSlots = Resources.LoadAll<EquipmentSlotsDatabase>("").FirstOrDefault();
            equippableItem = (EquippableItem)target;
            targetData = equippableItem;

            name = serializedObject.FindProperty("name");
            description = serializedObject.FindProperty("description");
            icon = serializedObject.FindProperty("icon");
            spawnModel = serializedObject.FindProperty("spawnModel");

            equipmentSlotIndex = serializedObject.FindProperty("equipmentSlotIndex");
            attachModel = serializedObject.FindProperty("attachModel");
            isSkinnedMesh = serializedObject.FindProperty("isSkinnedMesh");
            localPosition = serializedObject.FindProperty("localPosition");
            localRotation = serializedObject.FindProperty("localRotation");


            modelPrefab = serializedObject.FindProperty("modelPrefab");
            holderBone = serializedObject.FindProperty("holderBone");
            localPositionR = serializedObject.FindProperty("localPositionR");
            localRotationR = serializedObject.FindProperty("localRotationR");
            localPositionL = serializedObject.FindProperty("localPositionL");
            localRotationL = serializedObject.FindProperty("localRotationL");
            unEquipDuringActions = serializedObject.FindProperty("unEquipDuringActions");
            isDualItem = serializedObject.FindProperty("isDualItem");
            overrideController = serializedObject.FindProperty("overrideController");
            itemEquippedIdleClip = serializedObject.FindProperty("itemEquippedIdleClip");
            itemEquippedIdleClipMask = serializedObject.FindProperty("itemEquippedIdleClipMask");
            itemEnableTime = serializedObject.FindProperty("itemEnableTime");
            equipAudio = serializedObject.FindProperty("equipAudio");
            equipClip = serializedObject.FindProperty("equipClip");
            itemDisableTime = serializedObject.FindProperty("itemDisableTime");
            unEquipAudio = serializedObject.FindProperty("unEquipAudio");
            unEquipClip = serializedObject.FindProperty("unEquipClip");

            OnStart(equippableItem.equipClip.clip);
        }

        private void DrawEquipmentSlots()
        {
            if (equippableItem.equipmentSlotIndex == -1)
                equippableItem.equipmentSlotIndex = equippableItem.category.equipmentSlotIndex;

            var slots = equipmentSlots.EquipmentSlotList;
            string[] slotNames = slots.ToArray();

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup("Equipment Slot", equippableItem.equipmentSlotIndex, slotNames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(equippableItem, "Changed Equipment Slot");
                equippableItem.equipmentSlotIndex = newSelectedIndex;
                EditorUtility.SetDirty(equippableItem);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            DrawFoldout(ref generalFoldout, "General Settings", () => 
            {
                EditorGUILayout.PropertyField(name);
                EditorGUILayout.PropertyField(description, GUILayout.Height(45));
                icon.objectReferenceValue = (Sprite)EditorGUILayout.ObjectField("Icon", icon.objectReferenceValue, typeof(Sprite), false);

                EditorGUILayout.PropertyField(modelPrefab);
                // Attributes Section
                if (equippableItem.category != null)
                {
                    GUILayout.Space(5);
                    if (equippableItem.Attributes.Count > 0 || equippableItem.category.Attributes.Count > 0)
                    {
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {

                            if (equippableItem.Attributes.Count != equippableItem.category.Attributes.Count)
                                equippableItem.category.UpdateItemAttributeBasedOnCategory(equippableItem);
                            attributesFoldout = EditorGUILayout.Foldout(attributesFoldout, "Attribute Values", true);
                            if (attributesFoldout)
                            {
                                EditorGUI.indentLevel++;
                                foreach (var attribute in equippableItem.Attributes)
                                {
                                    switch (attribute.attributeType)
                                    {
                                        case ItemAttributeType.Integer:
                                            attribute.intValue = EditorGUILayout.IntField(attribute.attributeName, attribute.intValue);
                                            break;
                                        case ItemAttributeType.Decimal:
                                            attribute.floatValue = EditorGUILayout.FloatField(attribute.attributeName, attribute.floatValue);
                                            break;
                                        case ItemAttributeType.Text:
                                            attribute.stringValue = EditorGUILayout.TextField(attribute.attributeName, attribute.stringValue);
                                            break;
                                    }
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                }
            });


            DrawFoldout(ref equipmentSettings, "Equipment Configuration", () =>
            {
                // Equipment Configuration
                // Equipment Slots
                if (equipmentSlots != null)
                {
                    DrawEquipmentSlots();
                }

                EditorGUILayout.PropertyField(holderBone);
                EditorGUILayout.PropertyField(unEquipDuringActions, new GUIContent("Unequip During Actions", "If true, item will be unequipped when performing other actions"));

                if (ShowDualItemProperty)
                {
                    EditorGUILayout.PropertyField(isDualItem);
                }
                EditorGUILayout.PropertyField(spawnModel, new GUIContent("Spawn On Equip"));

                if (spawnModel.boolValue)
                {
                    // Transform Settings based on holder bone
                    if (holderBone.enumValueIndex == (int)HumanBodyBones.RightHand)
                    {
                        DrawTransformSection("Right Hand Transform", localPositionR, localRotationR);
                    }
                    else if (holderBone.enumValueIndex == (int)HumanBodyBones.LeftHand)
                    {
                        DrawTransformSection("Left Hand Transform", localPositionL, localRotationL);
                    }
                }

                // Model Attachment Settings
                EditorGUILayout.PropertyField(attachModel, new GUIContent("Attach On Body"));
                if (attachModel.boolValue)
                {
                    EditorGUILayout.PropertyField(isSkinnedMesh);
                    DrawTransformSection("Model Transform", localPosition, localRotation);
                }

                GUILayout.Space(5);

                // Animation Configuration
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(overrideController);
                    EditorGUILayout.PropertyField(itemEquippedIdleClip);
                    EditorGUILayout.PropertyField(itemEquippedIdleClipMask);

                    // Equip Settings
                    EditorGUI.indentLevel++;
                    equipFoldout = EditorGUILayout.Foldout(equipFoldout, "Equip Settings", true);
                    if (equipFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(equipClip);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            ChangeAnimationClip(Animations.Equip);
                        }
                        if (equippableItem.equipClip.clip != null)
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(itemEnableTime);
                            if (EditorGUI.EndChangeCheck())
                            {
                                previewTime = itemEnableTime.floatValue * clip.length;
                                UpdatePreview();
                            }
                        }
                        EditorGUILayout.PropertyField(equipAudio);
                        EditorGUI.indentLevel--;
                    }

                    // Unequip Settings
                    unEquipFoldout = EditorGUILayout.Foldout(unEquipFoldout, "Unequip Settings", true);
                    if (unEquipFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(unEquipClip);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            ChangeAnimationClip(Animations.UnEquip);
                        }
                        if (equippableItem.unEquipClip.clip != null)
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(itemDisableTime);
                            if (EditorGUI.EndChangeCheck())
                            {
                                previewTime = itemDisableTime.floatValue * clip.length;
                                UpdatePreview();
                            }
                        }
                        EditorGUILayout.PropertyField(unEquipAudio);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;

            });

            serializedObject.ApplyModifiedProperties();
        }
        private void DrawTransformSection(string label, SerializedProperty position, SerializedProperty rotation)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(position, new GUIContent("Position"));
            EditorGUILayout.PropertyField(rotation, new GUIContent("Rotation"));
            EditorGUILayout.EndVertical();
        }
        private void DrawFoldout(ref bool toggle, string label, System.Action drawer)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox); // Start HelpBox
            EditorGUI.indentLevel++;
            toggle = EditorGUILayout.Foldout(toggle, label, true);
            if (toggle)
            {
                EditorGUI.indentLevel++;
                drawer();
                EditorGUI.indentLevel--;
                //EditorGUILayout.Space(5);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical(); // End HelpBox

        }
    }
}