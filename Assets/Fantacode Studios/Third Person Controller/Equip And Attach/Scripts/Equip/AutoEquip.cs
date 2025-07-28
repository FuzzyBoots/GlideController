using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_Core
{
    public class AutoEquip : MonoBehaviour
    {
        [SerializeField] private List<EquippableItem> equippedItems = new();

        private ItemEquipper equipHandler;

            
        // Start is called before the first frame update
        void Start()
        {
            equipHandler = GetComponent<ItemEquipper>();

            foreach (var item in equippedItems)
            {
                equipHandler.EquipItem(item);
            }
        }
    }
}