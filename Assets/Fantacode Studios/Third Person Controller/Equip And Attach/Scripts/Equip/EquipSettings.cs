using FS_Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_Core
{
    public class EquipSettings : MonoBehaviour
    {
        [SerializeField] EquipHotspot equipHotspot;

        public EquipHotspot EquipHotspot => equipHotspot;

        public static EquipSettings i { get; private set; }
        private void Awake()
        {
            i = this;
        }
    }
}
