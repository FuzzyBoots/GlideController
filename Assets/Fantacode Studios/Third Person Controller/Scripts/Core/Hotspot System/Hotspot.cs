using TMPro;
using UnityEngine;

namespace FS_Core
{
    public class Hotspot : MonoBehaviour
    {
        [SerializeField] bool disableAfterInteraction = true;
        [SerializeField] GameObject interactUIPrefab;
        [SerializeField] float interactUIOffset = 1;

        protected GameObject interactUI;
        protected TMP_Text interactText;

        public virtual void OnEnable()
        {
            gameObject.layer = LayerMask.NameToLayer("Hotspot");
            if (interactUIPrefab != null && interactUI == null)
            {
                interactUI = Instantiate(interactUIPrefab, transform.position + Vector3.up * interactUIOffset, Quaternion.identity, transform);
                interactText = interactUI.GetComponentInChildren<TMP_Text>();
                interactUI.gameObject.SetActive(false);
            }
        }

        public virtual void ShowIndicator(bool state)
        {
            if (interactUI != null)
                interactUI.SetActive(state);


        }

        public virtual void Interact(HotspotDetector detector)
        {
            if (disableAfterInteraction)
                gameObject.SetActive(false);
        }
    }
}
