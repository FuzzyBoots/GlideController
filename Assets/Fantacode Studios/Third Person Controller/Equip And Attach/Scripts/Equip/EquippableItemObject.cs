using FS_Core;
using System.Linq;
using UnityEngine;
namespace FS_ThirdPerson
{
    public class EquippableItemObject : Hotspot
    {
        [Tooltip("Data associated with this equippable item.")]
        public EquippableItem itemData;

        Rigidbody rb;
        Collider collider;

        public bool EnableEquip { get; set; }


        public override void OnEnable()
        {
            base.OnEnable();
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (collider == null)
                collider = GetComponents<Collider>().FirstOrDefault(c => !c.isTrigger);

            HandlePhysics(false, false);
        }

        public void HandlePhysics(bool enable = true, bool enableEquip = true)
        {
            if (rb != null)
                rb.isKinematic = !enable;
            if (collider != null)
                collider.enabled = enable;
            this.EnableEquip = enableEquip;
        }

        public override void ShowIndicator(bool state)
        {
            base.ShowIndicator(state);

            if (interactText != null)
                interactText.text = "Press [E] to equip item";
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
    }
}
