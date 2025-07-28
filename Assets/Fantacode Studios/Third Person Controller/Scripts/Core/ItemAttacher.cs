using FS_ThirdPerson;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_Core
{
    public class ItemAttacher : MonoBehaviour
    {
        [SerializeField] List<ItemHolderSlot> itemHolderSlots = new List<ItemHolderSlot>();

        Dictionary<int, AttachedItem> attachedItems = new Dictionary<int, AttachedItem>();

        SkinnedMeshRenderer characterSMR;
        Dictionary<string, Transform> characterBones;

        public Action<Item> OnItemAttach;
        public Action<Item> OnItemDettach;

        public ItemEquipper ItemEquipper { get; private set; }
        public EquippableItem DefaultItem { get; set; }

        private void Awake()
        {
            ItemEquipper = GetComponent<ItemEquipper>();
            characterSMR = GetComponentInChildren<SkinnedMeshRenderer>();
            SaveCharacterBones();
        }

        public void AttachItem(Item item)
        {
            if (!item.Equippable || !item.attachModel) return;

            // Instantiate new item
            GameObject itemInstance = null;

            // Parent to corresponding attachment point
            ItemHolderSlot slot = itemHolderSlots.FirstOrDefault(i => i.equipmentSlotIndex == item.equipmentSlotIndex);

            if (attachedItems.ContainsKey(item.equipmentSlotIndex))
            {
                var prevItem = attachedItems[item.equipmentSlotIndex];
                if (prevItem != null)
                    DettachItem(prevItem.item, enableDefaultItems: false);
            }

            if (item.isSkinnedMesh)
            {
                var obj = Instantiate(item.modelPrefab, transform);

                var smr = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                smr.rootBone = characterSMR.rootBone;

                // Match bone names between equipment and character
                Transform[] boneMap = new Transform[smr.bones.Length];
                for (int i = 0; i < smr.bones.Length; i++)
                {
                    string boneName = smr.bones[i].name;
                    boneMap[i] = FindBoneByName(smr.rootBone, boneName);
                }

                smr.bones = boneMap;
                smr.transform.SetParent(transform, true);
                Destroy(obj);

                itemInstance = smr.gameObject;
            }
            else
            {
                var slotTransform = slot?.holder;
                if (slotTransform == null)
                {
                    Debug.LogWarning("No attachment slot found for, attaching to root!");
                    slotTransform = characterSMR.rootBone;
                }

                itemInstance = Instantiate(item.modelPrefab, slotTransform);
               
            }
            if (itemInstance != null)
            {
                itemInstance.transform.localPosition = item.localPosition;
                itemInstance.transform.localRotation = Quaternion.Euler(item.localRotation);
            }

            // Disable objects for the equipment slot when attaching this item
            if (slot != null)
            {
                foreach (var obj in slot.objectsToDisable)
                {
                    obj.SetActive(false);
                }
            }

            // Track the attached item
            var attachedItem = new AttachedItem()
            {
                item = item,
                itemInstance = itemInstance
            };
            if (attachedItems.ContainsKey(item.equipmentSlotIndex))
                attachedItems[item.equipmentSlotIndex] = attachedItem;
            else
                attachedItems.Add(item.equipmentSlotIndex, attachedItem);

            OnItemAttach?.Invoke(item);
        }

        public void DettachItem(Item item, bool enableDefaultItems=true)
        {
            var kvp = attachedItems.FirstOrDefault(i => i.Value.item == item);
            var attachedItem = kvp.Value;

            if (attachedItem != null)
            {
                if (attachedItem.isEquipped)
                {
                    if (DefaultItem != null)
                        ItemEquipper.EquipItem(DefaultItem, playEquipAnimation: false);
                    else
                        ItemEquipper.UnEquipItem(playUnEquipAnimation: false);
                }

                attachedItems.Remove(kvp.Key);
                Destroy(attachedItem.itemInstance);

                if (enableDefaultItems)
                {
                    // Re-enable any objects that were disabled when this item was attached
                    ItemHolderSlot slot = itemHolderSlots.FirstOrDefault(i => i.equipmentSlotIndex == item.equipmentSlotIndex);
                    if (slot != null)
                    {
                        foreach (var obj in slot.objectsToDisable)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
            }
            

            OnItemDettach?.Invoke(item);    
        }

        // Function to mark an attached item as equipped
        public void EquipItem(Item item)
        {
            var kvp = attachedItems.FirstOrDefault(i => i.Value.item == item);
            var attachedItem = kvp.Value;

            if (attachedItem != null)
            {
                attachedItem.isEquipped = true;
                attachedItem.itemInstance.SetActive(false);
            }
        }

        // Function to mark an attached item as not equipped
        public void UnEquipItem(Item item)
        {
            var kvp = attachedItems.FirstOrDefault(i => i.Value.item == item);
            var attachedItem = kvp.Value;

            if (attachedItem != null)
            {
                attachedItem.isEquipped = false;
                attachedItem.itemInstance.SetActive(true);
            }
        }

        public Item GetAttachedItem(int equipmentSlot)
        {
            if (attachedItems.ContainsKey(equipmentSlot))
                return attachedItems[equipmentSlot].item;
            else
                return null;
        }

        
        Transform FindBoneByName(Transform root, string name)
        {
            if (characterBones.ContainsKey(name))
                return characterBones[name];

            return null;
        }

        void SaveCharacterBones()
        {
            characterBones = new Dictionary<string, Transform>();

            var root = characterSMR.rootBone;
            var childTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var bone in characterSMR.bones)
            {
                var boneTransform = childTransforms.FirstOrDefault(child => child.name == bone.name);
                if (boneTransform != null)
                    characterBones.Add(bone.name, boneTransform);
            }
        }

        public float GetTotalDefense()
        {
            var armorCategory = Resources.Load<ItemCategory>("Category/Armor");
            var armorItems = attachedItems.Values.Where(i => i.item.category == armorCategory).Select(i => i.item).ToList();

            float totalDefense = 0;
            for (int i = 0; i < armorItems.Count; i++)
                totalDefense += armorItems[i].GetAttributeValue<float>("defense");

            return totalDefense;
        }
    }

    public class AttachedItem
    {
        public Item item;
        public GameObject itemInstance;
        public bool isEquipped = false;
    }

    [Serializable]
    public class ItemHolderSlot
    {
        [ShowEquipmentDropdown]
        public int equipmentSlotIndex;
        public Transform holder;

        [Tooltip("Objects that will be disabled while attaching an item")]
        public List<GameObject> objectsToDisable = new List<GameObject>();
    }
}
