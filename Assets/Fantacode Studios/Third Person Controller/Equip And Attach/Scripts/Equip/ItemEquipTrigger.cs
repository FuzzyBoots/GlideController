using FS_Core;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class ItemEquipTrigger : Hotspot
    {
        public EquippableItem itemData;
        public Transform itemSpawnPoint;
        public float showcaseItemScale = 1;

        GameObject itemObject;

        private void OnEnable()
        {
            if (itemData != null && itemData.modelPrefab != null)
            {
                var trigger = GetComponent<Collider>();
                itemObject = Instantiate(itemData.modelPrefab);
                itemObject.transform.parent = this.transform;
                itemObject.transform.position = itemSpawnPoint.position;
                itemObject.transform.rotation = Quaternion.identity;
                itemObject.transform.localScale *= showcaseItemScale;
            }

        }

        public override void ShowIndicator(bool state)
        {
            base.ShowIndicator(state);

            if (interactText != null)
                interactText.text = "Press [E] to equip";
        }

        public override void Interact(HotspotDetector detector)
        {
            var equippableItemController = detector.GetComponent<ItemEquipper>();
            if (!equippableItemController.PreventItemSwitching)
            {
                equippableItemController.EquipItem(itemData, false);
                Destroy(transform.gameObject);
            }

            base.Interact(detector);
        }

        private void Update()
        {
            if (itemObject != null)
            {
                itemObject.transform.Rotate(Vector3.up * Time.deltaTime * 100f);
            }
        }
    }
}