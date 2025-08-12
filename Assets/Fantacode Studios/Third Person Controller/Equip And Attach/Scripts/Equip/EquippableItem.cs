using FS_Core;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class EquippableItem : Item
    {
        [Tooltip("If true, a model for this item will be spawned when equipped.")]
        public bool spawnModel = true;

#if UNITY_EDITOR
        [RestrictEnumValues(17, 18)]
#endif
        [Tooltip("The bone on the character where the item will be attached when equipped.")]
        public HumanBodyBones holderBone = HumanBodyBones.RightHand;

        [Tooltip("Local position offset for the item when held in the right hand.")]
        public Vector3 localPositionR;
        [Tooltip("Local rotation offset for the item when held in the right hand.")]
        public Vector3 localRotationR;

        [Tooltip("Local position offset for the item when held in the left hand.")]
        public Vector3 localPositionL;
        [Tooltip("Local rotation offset for the item when held in the left hand.")]
        public Vector3 localRotationL;

        [Tooltip("If true, the item will be unequipped automatically during specific actions like parkour, climbing, etc.")]
        public bool unEquipDuringActions;

        [Tooltip("If true, this item is dual-wielded (used in both hands).")]
        public bool isDualItem = false;

        [Tooltip("Animator Override Controller used when this item is equipped.")]
        public AnimatorOverrideController overrideController;

        [Tooltip("Idle animation clip to play when the item is equipped.")]
        public AnimationClip itemEquippedIdleClip;

#if UNITY_EDITOR
        [HideInInspectorEnum(0,7)]
#endif
        [Tooltip("Specifies which body parts the equipped idle animation will affect.")]
        public Mask itemEquippedIdleClipMask = Mask.Arm;

        [Tooltip("Normalized time [0-1] indicating when the item becomes active during the equip animation.")]
        [Range(0, 1)]
        public float itemEnableTime = 0f;

        [Tooltip("Audio clip to play when the item is equipped.")]
        public AudioClip equipAudio;

        [Tooltip("Normalized time [0-1] indicating when the item becomes inactive during the unequip animation.")]
        [Range(0, 1)]
        public float itemDisableTime = 0f;

        [Tooltip("Audio clip to play when the item is unequipped.")]
        public AudioClip unEquipAudio;

        [Tooltip("Animation clip and settings used for the equip action.")]
        public AnimGraphClipInfo equipClip;

        [Tooltip("Animation clip and settings used for the unequip action.")]
        public AnimGraphClipInfo unEquipClip;
    }
}
