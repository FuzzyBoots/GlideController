using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SD_GlidingSystem
{
    [CreateAssetMenu(fileName = "New Glider", menuName = "Gliding System/Create Glider")]
    public class GliderItem : EquippableItem
    {
        public AnimGraphClipInfo glidingClip;
        public GameObject glider;

        public override void SetCategory()
        {
            category = Resources.Load<ItemCategory>("Category/Glider");
        }
    }
}