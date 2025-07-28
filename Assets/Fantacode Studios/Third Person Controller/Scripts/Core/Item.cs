using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FS_Core
{
    public class Item : FSObject
    {
        public new string name;
        [TextArea]
        public string description;
        public Sprite icon;
        public ItemCategory category;
        public bool overrideCategorySettings;
        public bool overrideCategoryAttributes;

        public int equipmentSlotIndex = -1;
        public bool attachModel;
        public GameObject modelPrefab;
        public bool isSkinnedMesh = true;
        public Vector3 localPosition;
        public Vector3 localRotation;

        public bool sellable;
        public bool droppable;

        public float weight;
        public bool canCraft;
        public CurrencyAmount priceToCraft;
        public bool canDismantle;
        public CurrencyAmount priceToDismantle;
        public List<ItemStack> ingredients;
        public bool overrideDismantleItems;
        public List<ItemStack> dismantleToItems;

        public List<ItemStack> DismantleItems => overrideDismantleItems ? dismantleToItems : ingredients;



        public CurrencyAmount price;
        public bool requiresMultipleCurrencies = false;
        public List<CurrencyAmount> currenciesRequired;

        public bool overrideAction;
        public ItemAction itemAction;

        public List<ItemAttribute> attributeValues = new List<ItemAttribute>();
        public List<ItemAttribute> overrideAttributes = new List<ItemAttribute>();

        public GameObject pickupPrefab;

        public string Name => name;
        public string Description => description;
        public Sprite Icon => icon;
        
        public GameObject PickupPrefab => pickupPrefab;

        public bool Stackable => category.Stackable;
        public bool Equippable => category.Equippable;
        public int EquipmentSlotIndex => equipmentSlotIndex;

        public bool Consumable => category.Consumable;
        public bool Sellable => overrideCategorySettings ? sellable : category.Sellable;
        public bool Droppable => overrideCategorySettings ? droppable : category.Droppable;

        public List<ItemAttribute> Attributes => overrideCategoryAttributes ? overrideAttributes : attributeValues;

        public ItemAction ItemAction => overrideAction ? itemAction : category.itemAction;

        public List<ItemStack> Ingredients => ingredients;

        public ItemCategory Category => category;

        public T GetAttributeValue<T>(string attributeName)
        {
            var attribute = Attributes.FirstOrDefault(a => a.attributeName.ToLower() == attributeName.ToLower());
            if (attribute != null)
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)attribute.intValue;
                else if (typeof(T) == typeof(float))
                    return (T)(object)attribute.floatValue;
                else
                    return (T)(object)attribute.stringValue;
            }
            else
            {
                return default(T);
            }
        }

        public virtual bool Use(GameObject character)
        {
            if (ItemAction != null)
            {
                if (!ItemAction.CanUse(this, character))
                    return false;

                ItemAction.OnUse(this, character);
                return true;
            }

            return true;
        }

        public virtual bool Equip(GameObject character, ItemAttacher itemAttacher = null)
        {
            if (attachModel)
            {
                //ItemEquipper itemEquipper = null;
                //if (itemAttacher != null)
                //    itemEquipper = itemAttacher.ItemEquipper;

                //if (itemEquipper?.EquippedItem != null && itemEquipper?.EquippedItem?.Category == category)
                //    itemEquipper?.EquipItem(this as EquippableItem, playEquipAnimation: false);
                //else

                itemAttacher?.AttachItem(this);

            }

            if (ItemAction != null)
            {
                if (!ItemAction.CanEquip(this, character))
                    return false;

                ItemAction.OnEquip(this, character);
                return true;
            }

            return false;
        }

        public virtual bool Unequip(GameObject character, ItemAttacher itemAttacher = null)
        {
            if (attachModel)
            {
                //ItemEquipper itemEquipper = null;
                //if (itemAttacher != null)
                //    itemEquipper = itemAttacher.ItemEquipper;

                //if (itemEquipper?.EquippedItem == this)
                //    itemEquipper?.UnEquipItem(playUnEquipAnimation: false);
                //else
                //    itemAttacher.DettachItem(this);

                itemAttacher.DettachItem(this);
            }

            if (ItemAction != null)
            {
                if (!ItemAction.CanEquip(this, character))
                    return false;

                ItemAction.OnUnequip(this, character);
                return true;
            }

            return false;
        }

        public void Init(string newName)
        {
            name = newName;
            category = Resources.Load<ItemCategory>("Category/Default Category");
        }


#if UNITY_EDITOR
        private void Awake()
        {
            if (category == null)
                EditorApplication.update += CallClickInput;
        }
        void CallClickInput()
        {
            if (EditorWindow.focusedWindow != null)
            {
                EditorApplication.update -= CallClickInput;
                EditorWindow.focusedWindow.SendEvent(new Event { keyCode = KeyCode.Return, type = EventType.KeyDown });
                EditorWindow.focusedWindow.SendEvent(new Event { keyCode = KeyCode.Return, type = EventType.KeyUp });
                SetCategory();
            }
        }
#endif

        public virtual void SetCategory() { }
    }

    // Stack of same items
    [Serializable]
    public class ItemStack : ISerializationCallbackReceiver
    {
        [SerializeField] Item item;
        [SerializeField] int count = 1;

        public Item Item {
            get => item;
            set => item = value;
        }
        public int Count {
            get => count;
            set => count = value;
        }

        [SerializeField, HideInInspector]
        private bool serialized;

        public void OnAfterDeserialize()
        {
            if (!serialized)
            {
                count = 1;
            }
        }

        public void OnBeforeSerialize()
        {
            if (serialized) return;
            serialized = true;
        }
    }
}
