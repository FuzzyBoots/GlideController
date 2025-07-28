using FS_Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FS_ThirdPerson
{
    public class ItemEquipper : MonoBehaviour
    {
        [field: HideInInspector] public List<EquippableItem> equippableItems = new List<EquippableItem>();

        public EquippableItem EquippedItem { get; set; }

        public bool IdleAnimationStopped { get; private set; }
        public EquippableItemHolder LeftHandHolder { get; private set; }
        public EquippableItemHolder RightHandHolder { get; private set; }

        public EquippableItemObject EquippedItemRight => RightHandHolder?.currentItem;
        public EquippableItemObject EquippedItemLeft => LeftHandHolder?.currentItem;
        public EquippableItemObject EquippedItemObject => EquippedItem?.holderBone == HumanBodyBones.RightHand? RightHandHolder.currentItem : LeftHandHolder.currentItem;
            
       

        public bool IsChangingItem => IsEquippingItem || IsUnEquippingItem;
        public bool IsEquippingItem { get; set; }
        public bool IsUnEquippingItem { get; set; }
        public bool PreventItemSwitching { get; set; } = false;
        public bool PreventItemUnEquip { get; set; } = false;
        public bool InterruptItemSwitching { get; set; } = false;


        public Action<EquippableItem> OnItemBecameUnused;

        public Action<EquippableItem> OnEquip;
        public Action OnUnEquip;

        public Action<EquippableItem> OnEquipComplete;
        public Action<EquippableItem> OnBeforeItemDisable;
        public Action OnUnEquipComplete;


        Animator animator;
        AnimGraph animGraph;
        ItemAttacher itemAttacher;

        public void Awake()
        {
            animator = GetComponent<Animator>();
            animGraph = GetComponent<AnimGraph>();
            itemAttacher = GetComponent<ItemAttacher>();

            LeftHandHolder = animator.GetBoneTransform(HumanBodyBones.LeftHand).GetComponentInChildren<EquippableItemHolder>();
            RightHandHolder = animator.GetBoneTransform(HumanBodyBones.RightHand).GetComponentInChildren<EquippableItemHolder>();
        }

        /// <summary>
        /// Adds an equippable item to the equippable items list if it is not already present.
        /// </summary>
        /// <param name="item">The equippable item to add.</param>
        public void AddItem(EquippableItem item)
        {
            if (!equippableItems.Contains(item))
                equippableItems.Add(item);
        }

        public void RemoveItem(EquippableItem item)
        {
            if (equippableItems.Contains(item))
            {
                equippableItems.Remove(item);
            }
        }

        #region Equip

        public void EquipItem(EquippableItem itemData, bool playEquipAnimation = true, Action onItemEnabled = null, Action onPrevItemDisabled = null)
        {
            StartCoroutine(Equip(itemData, playEquipAnimation, onItemEnabled, onPrevItemDisabled));
        }

        private IEnumerator Equip(EquippableItem itemData, bool playEquipAnimation = true, Action onItemEnabled = null, Action onPrevItemDisabled = null)
        {
            // Exit if the item is null, the same as the currently equipped item, or if switching is prevented
            if (itemData == null || EquippedItem == itemData || PreventItemSwitching || IsChangingItem)
                yield break;

            if (EquippedItem != null)
            {
                // Unequip the currently equipped item before equipping the new one
                UnEquipItem(playEquipAnimation, onItemDisabled: onPrevItemDisabled);

                // Wait until the item is completely unequipped before proceeding
                yield return new WaitUntil(() => !IsChangingItem);
            }

            // Exit if an interruption occurs during item switching
            if (InterruptItemSwitching)
                yield break;

            IsEquippingItem = true;

            // Attach the new item to the character (returns whether the attachment was successful)
            var hasItem = SetItem(itemData);

            // Create an action to enable the item at the correct animation timing
            var itemEnableAction = new ActionData()
            {
                normalizeTime = itemData.itemEnableTime,
                action = () => { 
                    EnableItem(itemData, playEquipAnimation);
                    onItemEnabled?.Invoke();
                }
            };

            EquippedItem = itemData;
            OnEquip?.Invoke(itemData);

            // If an equip animation exists, play it and trigger the enable action at the right moment
            if (playEquipAnimation && itemData.equipClip.clip != null)
                yield return animGraph.CrossfadeAsync(null, itemData.equipClip, mask: Mask.Arm, actions: itemEnableAction);
            else
                itemEnableAction.action?.Invoke(); // Immediately enable the item if no animation is played

            OnEquipComplete?.Invoke(itemData);
            IsEquippingItem = false;
        }
        bool SetItem(EquippableItem equippableItemData)
        {
            var hasItem = false;

            if (equippableItemData.isDualItem)
            {
                hasItem = SpawnItem(equippableItemData, RightHandHolder, equippableItemData.localPositionR, equippableItemData.localRotationR, HumanBodyBones.RightHand);
                hasItem = SpawnItem(equippableItemData, LeftHandHolder, equippableItemData.localPositionL, equippableItemData.localRotationL, HumanBodyBones.LeftHand);
            }
            else if (equippableItemData.holderBone == HumanBodyBones.RightHand)
            {
                hasItem = SpawnItem(equippableItemData, RightHandHolder, equippableItemData.localPositionR, equippableItemData.localRotationR, HumanBodyBones.RightHand);
            }
            else if (equippableItemData.holderBone == HumanBodyBones.LeftHand)
            {
                hasItem = SpawnItem(equippableItemData, LeftHandHolder, equippableItemData.localPositionL, equippableItemData.localRotationL, HumanBodyBones.LeftHand);
            }

            if (hasItem)
                animGraph.PlayLoopingAnimation(equippableItemData.itemEquippedIdleClip, mask: equippableItemData.itemEquippedIdleClipMask, isActAsAnimatorOutput: true);
            else
                Debug.LogWarning("Cannot equip item because the ItemObject is null.");


            return hasItem;
        }
        bool SpawnItem(EquippableItem itemData, EquippableItemHolder itemHolder, Vector3 localPosition, Vector3 localRotation, HumanBodyBones bodyBone)
        {
            var items = animator.GetBoneTransform(bodyBone).GetComponentsInChildren<EquippableItemObject>(true);
            EquippableItemObject currentItem = items.FirstOrDefault(i => i.itemData == itemData && i.GetComponentInParent<EquippableItemHolder>() == itemHolder);
            
            if (currentItem == null)
            {
                GameObject weaponObject = null;
                if (itemData.modelPrefab != null)
                {
                    weaponObject = Instantiate(itemData.modelPrefab, itemHolder.transform);
                    weaponObject.transform.localPosition = localPosition;
                    weaponObject.transform.localRotation = Quaternion.Euler(localRotation);
                    weaponObject.SetActive(false);
                }
                else
                {
                    // For Hand Combat
                    weaponObject = animator.GetBoneTransform(itemData.holderBone).transform.Find(itemData.holderBone.ToString() + "Collider").gameObject;
                }

                if (weaponObject == null) return false;

                currentItem = weaponObject.GetComponent<EquippableItemObject>();
                currentItem.itemData = itemData;
            }

            AddItem(currentItem.itemData);

            itemHolder.currentItem = currentItem;

            return true;
        }
        void EnableItem(EquippableItem equippableItemData, bool playEquipAnimation)
        {
            if (equippableItemData.isDualItem)
            {
                EquippedItemRight?.gameObject?.SetActive(true);
                EquippedItemLeft?.gameObject?.SetActive(true);
            }
            else
            {
                EquippedItemObject?.gameObject?.SetActive(true);
            }
            if (equippableItemData.overrideController)
                animGraph.UpdateOverrideController(equippableItemData.overrideController, playEquipAnimation);
        }

        #endregion

        #region UnEquip

        public void UnEquipItem(bool playUnEquipAnimation = true, bool dropItem = false, Action onItemDisabled = null)
        {
            StartCoroutine(UnEquipItemAsync(playUnEquipAnimation, dropItem, onItemDisabled));
        }

        private IEnumerator UnEquipItemAsync(bool playUnEquipAnimation = true, bool dropItem = false, Action onItemDisabled = null)
        {
            // Exit if there is no currently equipped item, an item switch is already in progress, 
            // or if unequipping is prevented
            if (EquippedItem == null || IsChangingItem || PreventItemUnEquip)
                yield break;

            // Store the currently equipped item reference before unequipping
            var itemData = EquippedItem;

            // Mark that an item is currently being unequipped
            IsUnEquippingItem = true;

            // Define an action that disables the weapon once the unequip animation reaches a certain point
            var weaponDisableAction = new ActionData()
            {
                normalizeTime = EquippedItem.itemDisableTime,
                action = () =>
                {
                    // Invoke the completion callback for unequipping
                    OnBeforeItemDisable?.Invoke(itemData);

                    // Disable the currently equipped weapon
                    DisableCurrentItem(dropItem);
                    onItemDisabled?.Invoke();

                    // Clear the currently equipped item reference
                    EquippedItem = null;

                    // Remove item references from both hand handlers
                    RightHandHolder.currentItem = null;
                    LeftHandHolder.currentItem = null;

                    // Stop any looping animations that were playing
                    animGraph.StopLoopingAnimations(true);

                    // Reset the animation override controller
                    animGraph.UpdateOverrideController(null, playUnEquipAnimation);
                }
            };

            OnUnEquip?.Invoke();

            if (playUnEquipAnimation && itemData.unEquipClip.clip != null)
            {
                yield return animGraph.CrossfadeAsync(null, itemData.unEquipClip, mask: Mask.Arm, actions: weaponDisableAction);
            }
            else
            {
                // If no animation is played, immediately invoke the disable action
                yield return null;      // Unity seems to crash if we do not wait for a frame
                weaponDisableAction.action?.Invoke();
            }

            OnUnEquipComplete?.Invoke();

            IsUnEquippingItem = false;
        }
        void DisableCurrentItem(bool dropItem = false)
        {
            if (EquippedItem.isDualItem)
            {
                if (!dropItem)
                {
                    EquippedItemRight.gameObject.SetActive(false);
                    EquippedItemLeft.gameObject.SetActive(false);
                }
            }
            else
            {
                if (!dropItem)
                {
                    EquippedItemObject.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Drop the specified equippable item from the equippable items list.
        /// </summary>
        /// <param name="item">The equippable item to be removed.</param>
        /// <param name="destroyItem">
        /// If true, the item will be destroyed. 
        /// If false, the item will remain but physics will be enabled, causing it to fall by gravity.
        public void DropItem(EquippableItem item = null, bool destroyItem = true)
        {
            if(item == null)
            {
                item = EquippedItem;
                if(item == null) return;
            }
            if (equippableItems.Contains(item))
            {
                equippableItems.Remove(item);
            }
            if (EquippedItem == item)
            {
                UnEquipItem(false, !destroyItem);
                if (item.isDualItem)
                {
                    //var itemCloneR = Instantiate(EquippedItemRight, EquippedItemRight.transform.parent);
                    //itemCloneR.transform.SetPositionAndRotation(EquippedItemRight.transform.position, EquippedItemRight.transform.rotation);
                    //itemCloneR.gameObject.SetActive(false);
                    //var itemCloneL = Instantiate(EquippedItemLeft, EquippedItemLeft.transform.parent);
                    //itemCloneL.transform.SetPositionAndRotation(EquippedItemLeft.transform.position, EquippedItemLeft.transform.rotation);
                    //itemCloneL.gameObject.SetActive(false);
                    EquippedItemLeft.transform.parent = null;
                    EquippedItemRight.transform.parent = null;

                   

                    if (destroyItem)
                    {
                        Destroy(EquippedItemRight.gameObject);
                        Destroy(EquippedItemLeft.gameObject);
                    }
                    else
                    {
                        EquippedItemRight.HandlePhysics(true);
                        EquippedItemLeft.HandlePhysics(true);
                    }
                }
                else
                {
                    //var itemClone = Instantiate(EquippedItemObject, EquippedItemObject.transform.parent);
                    //itemClone.transform.SetPositionAndRotation(EquippedItemObject.transform.position, EquippedItemObject.transform.rotation);
                    //itemClone.gameObject.SetActive(false);

                    EquippedItemObject.transform.parent = null;

                    if (destroyItem)
                    {
                        Destroy(EquippedItemObject.gameObject);
                    }
                    else
                    {
                        EquippedItemObject.HandlePhysics(true);
                    }
                }
                OnItemBecameUnused?.Invoke(item);
            }
        }

        #endregion

        #region Utilities

        public void ResumeIdleAnimation()
        {
            if (IdleAnimationStopped)
            {
                animGraph.PlayLoopingAnimation(EquippedItem.itemEquippedIdleClip, mask: EquippedItem.itemEquippedIdleClipMask, isActAsAnimatorOutput: true);
                IdleAnimationStopped = false;
            }
        }

        public void StopIdleAnimation()
        {
            animGraph.StopLoopingAnimations(true);
            IdleAnimationStopped = true;
        }

        #endregion

        private void OnGUI()
        {
            //GUILayout.Label(EquippedItem != null ? EquippedItem.name : "Empty");
        }
    }



    public class CustomAction
    {
        public Action action = null;
    }
}
