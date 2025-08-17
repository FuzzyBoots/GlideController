using FS_ThirdPerson;
using UnityEditor;

namespace SD_GlidingSystem
{
    [CustomEditor(typeof(GliderItem))]
    public class GliderItemEditor : EquippableItemEditor
    {
        // private SerializedProperty gunShootClip;

        public override void OnEnable()
        {
            // gunShootClip = serializedObject.FindProperty("gunShootClip");

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            // EditorGUILayout.PropertyField(gunShootClip);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
