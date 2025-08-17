using UnityEngine;
using UnityEngine.EventSystems;
#if inputsystem
using UnityEngine.InputSystem.UI;
#endif
namespace FS_Core
{
    public class FSSettings : MonoBehaviour
    {
        public static FSSettings i { get; private set; }

        EventSystem eventSystem;
        private void Awake()
        {
            i = this;
            eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem != null && eventSystem.GetComponent<BaseInputModule>() == null)
            {
#if inputsystem
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
                 eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }
        }
    }
}


